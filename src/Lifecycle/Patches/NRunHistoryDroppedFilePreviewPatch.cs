#if STS2_AT_LEAST_0_106_1
using System.Reflection;
using System.Text.Json.Nodes;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;
using GodotFileAccess = Godot.FileAccess;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Allows a user to preview a dropped <c>.run</c> or <c>.save</c> file in the vanilla run-history screen without
    ///     importing it into the real run-history save list.
    ///     允许用户在原版跑局历史界面预览拖入的 <c>.run</c> 或 <c>.save</c> 文件，而不会导入真实跑局历史列表。
    /// </summary>
    internal class NRunHistoryDroppedFilePreviewPatch : IPatchMethod
    {
        public static string PatchId => "nrun_history_dropped_file_preview";

        public static string Description =>
            "Run history: preview dropped SerializableRun or RunHistory .run/.save files in the history UI";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(FileDropHandler), nameof(FileDropHandler.OnFilesDropped), [typeof(string[])])];
        }

        public static bool Prefix(string[] files)
        {
            if (ActiveScreenContext.Instance.GetCurrentScreen() is not NRunHistory runHistory ||
                !runHistory.IsVisibleInTree())
                return true;

            if (files.Length != 1)
            {
                RitsuLibFramework.Logger.Warn(
                    "[Saves] Run history preview supports dropping exactly one .run or .save file.");
                return false;
            }

            var file = files[0];
            if (!IsSupportedPreviewFile(file))
            {
                RitsuLibFramework.Logger.Warn("[Saves] Run history preview only supports .run or .save files: " + file);
                return false;
            }

            TaskHelper.RunSafely(RunHistoryDroppedFilePreview.ShowDroppedFile(runHistory, file));
            return false;
        }

        private static bool IsSupportedPreviewFile(string file)
        {
            return file.EndsWith(".run", StringComparison.OrdinalIgnoreCase) ||
                   file.EndsWith(".save", StringComparison.OrdinalIgnoreCase);
        }
    }

    internal static class RunHistoryDroppedFilePreview
    {
        private static readonly MethodInfo? DisplayRunMethod =
            AccessTools.DeclaredMethod(typeof(NRunHistory), "DisplayRun", [typeof(RunHistory)]);

        private static readonly FieldInfo? OutOfDateVisualField =
            AccessTools.DeclaredField(typeof(NRunHistory), "_outOfDateVisual");

        private static readonly FieldInfo? PrevButtonField =
            AccessTools.DeclaredField(typeof(NRunHistory), "_prevButton");

        private static readonly FieldInfo? NextButtonField =
            AccessTools.DeclaredField(typeof(NRunHistory), "_nextButton");

        private static readonly FieldInfo? IndexField =
            AccessTools.DeclaredField(typeof(NRunHistory), "_index");

        internal static Task ShowDroppedFile(NRunHistory screen, string file)
        {
            if (DisplayRunMethod == null)
            {
                RitsuLibFramework.Logger.Warn("[Saves] Run history preview failed: DisplayRun method was not found.");
                return Task.CompletedTask;
            }

            using var access = GodotFileAccess.Open(file, GodotFileAccess.ModeFlags.Read);
            if (access == null)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Saves] Run history preview could not open '{file}': {GodotFileAccess.GetOpenError()}");
                return Task.CompletedTask;
            }

            var json = access.GetAsText();
            if (!TryReadRunHistory(json, out var history, out var error))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Saves] Run history preview could not read '{file}' as RunHistory or SerializableRun: {error}");
                return Task.CompletedTask;
            }

            DisplayRunMethod.Invoke(screen, [history]);
            HideOutOfDateVisual(screen);
            DisableNavigation(screen);
            ActiveScreenContext.Instance.FocusOnDefaultControl();
            RitsuLibFramework.Logger.Info("[Saves] Previewing dropped run history file: " + file);
            return Task.CompletedTask;
        }

        private static bool TryReadRunHistory(string json, out RunHistory history, out string error)
        {
            var root = TryParseJsonObject(json);
            if (root == null)
            {
                history = null!;
                error = "File is not a JSON object.";
                return false;
            }

            var serializableFirst = LooksLikeSerializableRun(root);
            var looksLikeRunHistory = LooksLikeRunHistory(root);
            if (!serializableFirst && !looksLikeRunHistory)
            {
                history = null!;
                error = "File is not a RunHistory or SerializableRun save.";
                return false;
            }

            var serializableRunError = "";
            var runHistoryError = "";

            if (serializableFirst)
                if (TryReadSerializableRun(json, root, out history, out serializableRunError))
                {
                    error = "";
                    return true;
                }

            if ((looksLikeRunHistory &&
                 TryReadVanillaRunHistory(json, out history, out runHistoryError)) || (!serializableFirst &&
                    TryReadSerializableRun(json, root, out history, out serializableRunError)))
            {
                error = "";
                return true;
            }

            error = serializableFirst
                ? $"SerializableRun parse: {serializableRunError}; RunHistory parse: {runHistoryError}"
                : $"RunHistory parse: {runHistoryError}; SerializableRun parse: {serializableRunError}";
            history = null!;
            return false;
        }

        private static bool TryReadVanillaRunHistory(string json, out RunHistory history, out string error)
        {
            var result = JsonSerializationUtility.FromJson<RunHistory>(json);
            if (!result.Success || result.SaveData == null)
            {
                history = null!;
                error = $"{result.ErrorMessage} ({result.Status})";
                return false;
            }

            history = result.SaveData;
            if (history.Players is { Count: > 0 } &&
                // ReSharper disable RedundantAlwaysMatchSubpattern
                history is { MapPointHistory: not null, Acts: not null, Modifiers: not null })
                // ReSharper restore RedundantAlwaysMatchSubpattern
            {
                error = "";
                return true;
            }

            history = null!;
            error = "RunHistory is missing required history fields.";
            return false;
        }

        private static bool TryReadSerializableRun(
            string json,
            JsonObject? root,
            out RunHistory history,
            out string error)
        {
            var result = JsonSerializationUtility.FromJson<SerializableRun>(json);
            if (!result.Success || result.SaveData == null)
            {
                history = null!;
                error = $"{result.ErrorMessage} ({result.Status})";
                return false;
            }

            var run = result.SaveData;
            if (run.Players == null || run.Players.Count == 0)
            {
                history = null!;
                error = "SerializableRun has no players.";
                return false;
            }

            var won = ReadBool(root, "win", "victory", "is_victory") ?? run.WinTime > 0;
            var abandoned = ReadBool(root, "was_abandoned", "is_abandoned", "abandoned") ?? false;
            history = CreatePreviewRunHistory(run, won, abandoned);
            error = "";
            return true;
        }

        private static RunHistory CreatePreviewRunHistory(SerializableRun run, bool victory, bool isAbandoned)
        {
            var killedByEncounter = ModelId.none;
            var killedByEvent = ModelId.none;
            var lastEntry = run.MapPointHistory?.LastOrDefault()?.LastOrDefault();
            var lastRoom = lastEntry?.Rooms.FirstOrDefault();

            // ReSharper disable once InvertIf
            if (!victory && lastRoom != null)
            {
                if (lastRoom.RoomType.IsCombatRoom())
                    killedByEncounter = lastRoom.ModelId ?? ModelId.none;
                else if (lastRoom.RoomType == RoomType.Event)
                    killedByEvent = lastRoom.ModelId ?? ModelId.none;
            }

            return new()
            {
                BuildId = ReleaseInfoManager.Instance.ReleaseInfo?.Version ?? "NON-RELEASE-VERSION",
                PlatformType = run.PlatformType,
                Players =
                [
                    .. run.Players.Select(player => new RunHistoryPlayer
                    {
                        Id = player.NetId,
                        Character = player.CharacterId ?? ModelId.none,
                        Deck = player.Deck ?? [],
                        Relics = player.Relics ?? [],
                        Potions = player.Potions ?? [],
                        Badges = GetBadgesForPlayer(run, player, victory, isAbandoned),
                        MaxPotionSlotCount = player.MaxPotionSlotCount,
                    }),
                ],
                GameMode = run.GameMode,
                Win = victory,
                KilledByEncounter = killedByEncounter,
                KilledByEvent = killedByEvent,
                WasAbandoned = isAbandoned,
                Seed = run.SerializableRng?.Seed ?? "",
                StartTime = run.StartTime,
                RunTime = run.WinTime > 0 ? run.WinTime : run.RunTime,
                MapPointHistory = run.MapPointHistory ?? [],
                Ascension = run.Ascension,
                Acts = run.Acts?.Select(act => act.Id ?? ModelId.none).ToList() ?? [],
                Modifiers = run.Modifiers ?? [],
            };
        }

        private static IEnumerable<SerializableBadge> GetBadgesForPlayer(
            SerializableRun run,
            SerializablePlayer player,
            bool won,
            bool isAbandoned)
        {
            if (isAbandoned)
                return [];

            try
            {
                return
                [
                    .. ScoreUtility.GetBadges(run, player.NetId, won)
                        .Select(static badge => badge.ToSerializable()),
                ];
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Saves] Failed to calculate preview badges for player {player.NetId}: {ex.Message}");
                return [];
            }
        }

        private static JsonObject? TryParseJsonObject(string json)
        {
            try
            {
                return JsonNode.Parse(json) as JsonObject;
            }
            catch
            {
                return null;
            }
        }

        private static bool LooksLikeSerializableRun(JsonObject? root)
        {
            return root != null &&
                   (root.ContainsKey("current_act_index") ||
                    root.ContainsKey("save_time") ||
                    root.ContainsKey("win_time") ||
                    root.ContainsKey("rng"));
        }

        private static bool LooksLikeRunHistory(JsonObject root)
        {
            return root.ContainsKey("win") &&
                   root.ContainsKey("players") &&
                   root.ContainsKey("map_point_history") &&
                   (root.ContainsKey("build_id") ||
                    root.ContainsKey("killed_by_encounter") ||
                    root.ContainsKey("killed_by_event") ||
                    root.ContainsKey("was_abandoned"));
        }

        private static bool? ReadBool(JsonObject? root, params string[] keys)
        {
            if (root == null)
                return null;

            foreach (var key in keys)
            {
                if (!root.TryGetPropertyValue(key, out var node) || node is not JsonValue value)
                    continue;

                if (value.TryGetValue<bool>(out var boolValue))
                    return boolValue;

                if (value.TryGetValue<string>(out var stringValue) &&
                    bool.TryParse(stringValue, out var parsedValue))
                    return parsedValue;
            }

            return null;
        }

        private static void HideOutOfDateVisual(NRunHistory screen)
        {
            if (OutOfDateVisualField?.GetValue(screen) is Control visual)
                visual.Visible = false;
        }

        private static void DisableNavigation(NRunHistory screen)
        {
            IndexField?.SetValue(screen, -1);
            DisableButton(PrevButtonField, screen);
            DisableButton(NextButtonField, screen);
        }

        private static void DisableButton(FieldInfo? field, NRunHistory screen)
        {
            if (field?.GetValue(screen) is not NRunHistoryArrowButton button)
                return;

            button.Disable();
            button.Visible = false;
        }
    }
}
#endif
