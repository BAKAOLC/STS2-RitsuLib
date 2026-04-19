using System.Text;
using Godot;
using STS2RitsuLib.Audio.Internal;
using Array = Godot.Collections.Array;
using FileAccess = Godot.FileAccess;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     FMOD Studio bank and path probes. For gameplay sounds that should follow vanilla mixer settings, use
    ///     <see cref="GameFmod.Studio" /> instead.
    /// </summary>
    public static class FmodStudioServer
    {
        /// <summary>
        ///     The GDExtension <c>FmodBank</c> destructor calls <c>unload_bank</c>; callers must retain the returned ref
        ///     or the bank is unloaded when the Variant goes out of scope (see <c>studio/fmod_bank.cpp</c> destructor).
        /// </summary>
        private static readonly Lock LoadedBankPinsGate = new();

        private static readonly Dictionary<string, GodotObject> LoadedBankPins = [];

        private static readonly StringName[] GuidMappingInjectCandidates =
        [
            new("register_guid_path_mappings_from_file"),
            new("inject_guid_mappings_from_file"),
            new("register_strings_from_guid_file"),
            new("load_guid_mapping_file"),
        ];

        /// <summary>
        ///     Returns the Godot <c>FmodServer</c> singleton when present.
        /// </summary>
        public static GodotObject? TryGet()
        {
            return FmodStudioGateway.TryGetServer();
        }

        /// <summary>
        ///     Loads a bank from <paramref name="resourcePath" /> using <paramref name="mode" />.
        /// </summary>
        public static bool TryLoadBank(string resourcePath, FmodStudioLoadBankMode mode = FmodStudioLoadBankMode.Normal)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                RitsuLibFramework.Logger.Warn("[Audio] FMOD load_bank: empty path.");
                return false;
            }

            if (!FileAccess.FileExists(resourcePath))
            {
                RitsuLibFramework.Logger.Warn($"[Audio] FMOD load_bank: file not found: {resourcePath}");
                return false;
            }

            if (!FmodStudioGateway.TryCall(out var result, FmodStudioMethodNames.LoadBank, resourcePath, (int)mode))
                return false;

            switch (result.VariantType)
            {
                case Variant.Type.Bool:
                    return result.AsBool();
                case Variant.Type.Nil:
                    return false;
                default:
                {
                    var bank = result.AsGodotObject();
                    if (bank is null || !GodotObject.IsInstanceValid(bank))
                        return false;

                    lock (LoadedBankPinsGate)
                    {
                        LoadedBankPins[resourcePath] = bank;
                    }

                    return true;
                }
            }
        }

        /// <summary>
        ///     Unloads a previously loaded bank (releases any pin held by <see cref="TryLoadBank" />).
        /// </summary>
        public static bool TryUnloadBank(string resourcePath)
        {
            bool hadPin;
            lock (LoadedBankPinsGate)
            {
                hadPin = LoadedBankPins.Remove(resourcePath);
            }

            return hadPin || FmodStudioGateway.TryCall(FmodStudioMethodNames.UnloadBank, resourcePath);
        }

        /// <summary>
        ///     Blocks until non-blocking bank loads finish (matches <c>FmodServer.wait_for_all_loads</c>).
        /// </summary>
        public static void TryWaitForAllLoads()
        {
            FmodStudioGateway.TryCall(FmodStudioMethodNames.WaitForAllLoads);
        }

        /// <summary>
        ///     Null when the query fails; otherwise whether FMOD is still loading banks (see <c>FmodServer.banks_still_loading</c>
        ///     ).
        /// </summary>
        public static bool? TryBanksStillLoading()
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.BanksStillLoading))
                return null;

            return v.AsBool();
        }

        /// <summary>
        ///     Validates <paramref name="guidMapResourcePath" /> exists, loads guids.txt-style mappings, applies native
        ///     injection when available, and logs success (with event path count) or failure.
        /// </summary>
        /// <param name="guidMapResourcePath">
        ///     e.g. <c>res://Mod/banks/MyMod.guids.txt</c> — lines <c>{guid} bank:/…</c>, <c>bus:/…</c>,
        ///     <c>event:/…</c>.
        /// </param>
        /// <returns>
        ///     True when mappings were applied per <see cref="TryApplyStudioGuidMappingsCore" />.
        /// </returns>
        public static bool TryLoadStudioGuidMappings(string guidMapResourcePath)
        {
            if (string.IsNullOrWhiteSpace(guidMapResourcePath))
            {
                RitsuLibFramework.Logger.Warn("[Audio] FMOD guid map: empty path.");
                return false;
            }

            if (!FileAccess.FileExists(guidMapResourcePath))
            {
                RitsuLibFramework.Logger.Warn($"[Audio] FMOD guid map file not found: {guidMapResourcePath}");
                return false;
            }

            if (!TryApplyStudioGuidMappingsCore(guidMapResourcePath))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Audio] FMOD guid map failed (unreadable or no usable event:/ mappings): {guidMapResourcePath}");
                return false;
            }

            var n = FmodStudioGuidPathTable.EventMappingCount;
            RitsuLibFramework.Logger.Info($"[Audio] FMOD guid map OK: {guidMapResourcePath} ({n} event path(s))");
            return true;
        }

        /// <summary>
        ///     Parses an FMOD Studio <c>GUIDs.txt</c>-style listing (same shape as Celeste/Everest <c>IngestGUIDs</c> inputs),
        ///     registers <c>event:/…</c> path → GUID mappings for RitsuLib fallbacks, and attempts optional
        ///     <c>FmodServer</c> hooks when the runtime exposes them. Prefer <see cref="TryLoadStudioGuidMappings" /> for
        ///     existence checks and outcome logging.
        /// </summary>
        /// <param name="resourcePath">
        ///     Project path to the text file (e.g. <c>res://Mod/banks/MyMod.guids.txt</c>). Each non-empty line:
        ///     <c>{guid} bank:/…</c>, <c>{guid} bus:/…</c>, or <c>{guid} event:/…</c>.
        /// </param>
        /// <returns>
        ///     False when the file is missing or unparsable; otherwise true when at least one <c>event:/</c> mapping was
        ///     loaded and/or an addon injection call succeeded.
        /// </returns>
        public static bool TryInjectStudioGuidMappings(string resourcePath)
        {
            if (TryApplyStudioGuidMappingsCore(resourcePath)) return true;
            RitsuLibFramework.Logger.Warn($"[Audio] FMOD guid map: missing or unreadable file: {resourcePath}");
            return false;
        }

        private static bool TryApplyStudioGuidMappingsCore(string resourcePath)
        {
            if (!FmodStudioGuidPathTable.TryLoadFromResourceFile(resourcePath))
                return false;

            var injected = TryCallNativeGuidInject(resourcePath);
            WarnIfMappedEventGuidsUnresolved();
            return injected || FmodStudioGuidPathTable.EventMappingCount > 0;
        }

        private static void WarnIfMappedEventGuidsUnresolved()
        {
            foreach (var (path, guid) in FmodStudioGuidPathTable.SnapshotEventMappings())
            {
                if (TryCheckEventGuid(guid) != false)
                    continue;

                RitsuLibFramework.Logger.Warn(
                    "[Audio] guids.txt: GUID not found in loaded FMOD Studio data — " +
                    $"event '{path}', GUID '{guid}'. Load matching banks before injection and regenerate GUIDs.txt from the same build.");
            }
        }

        /// <summary>
        ///     Null when the probe fails; otherwise whether the event path exists in loaded data.
        /// </summary>
        public static bool? TryCheckEventPath(string eventPath)
        {
            if (FmodStudioGuidPathTable.TryGetStudioGuidForEventPath(eventPath, out _))
                return true;

            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CheckEventPath, eventPath))
                return null;

            return v.AsBool();
        }

        /// <summary>
        ///     Null when the probe fails; otherwise whether the bus path is valid.
        /// </summary>
        public static bool? TryCheckBusPath(string busPath)
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CheckBusPath, busPath))
                return null;

            return v.AsBool();
        }

        /// <summary>
        ///     Resolves a Studio event description from a GUID; null when missing or on failure.
        /// </summary>
        public static GodotObject? TryGetEventDescriptionFromGuid(string eventGuid)
        {
            if (string.IsNullOrWhiteSpace(eventGuid))
                return null;

            if (!FmodStudioGuidInterop.TryNormalizeForAddon(eventGuid, out var normalized))
                return null;

            return !FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetEventFromGuid, normalized)
                ? null
                : v.AsGodotObject();
        }

        /// <summary>
        ///     Null when the probe fails; otherwise whether the GUID resolves in the loaded Studio cache.
        /// </summary>
        public static bool? TryCheckEventGuid(string eventGuid)
        {
            if (!FmodStudioGuidInterop.TryNormalizeForAddon(eventGuid, out var normalized))
                return null;

            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CheckEventGuid, normalized))
                return null;

            return v.AsBool();
        }

        /// <summary>
        ///     Returns all buses currently exposed by FMOD (empty when unavailable).
        /// </summary>
        public static Array TryGetAllBuses()
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetAllBuses))
                return new();

            return v.VariantType == Variant.Type.Array ? v.AsGodotArray() : new();
        }

        /// <summary>
        ///     Count of loaded Studio banks; <c>-1</c> when <c>FmodServer.get_all_banks</c> is unavailable or fails.
        /// </summary>
        public static int TryGetLoadedBankCount()
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetAllBanks))
                return -1;

            return v.VariantType == Variant.Type.Array ? v.AsGodotArray().Count : -1;
        }

        /// <summary>
        ///     Count of event descriptions in the Studio cache; <c>-1</c> when
        ///     <c>FmodServer.get_all_event_descriptions</c> is unavailable or fails.
        /// </summary>
        public static int TryGetLoadedEventDescriptionCount()
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetAllEventDescriptions))
                return -1;

            return v.VariantType == Variant.Type.Array ? v.AsGodotArray().Count : -1;
        }

        /// <summary>
        ///     <c>FmodBank.get_event_description_count</c> for the bank loaded from <paramref name="bankResourcePath" />
        ///     (must match <c>get_godot_res_path</c> on the bank); <c>-1</c> when not found or on failure.
        /// </summary>
        public static long TryGetLoadedBankEventDescriptionCount(string bankResourcePath)
        {
            if (string.IsNullOrWhiteSpace(bankResourcePath))
                return -1;

            if (!FmodStudioGateway.TryCall(out var banksVar, FmodStudioMethodNames.GetAllBanks))
                return -1;

            if (banksVar.VariantType != Variant.Type.Array)
                return -1;

            foreach (var item in banksVar.AsGodotArray())
            {
                var bank = item.AsGodotObject();
                if (bank is null)
                    continue;

                string path;
                try
                {
                    path = bank.Call("get_godot_res_path").AsString();
                }
                catch
                {
                    continue;
                }

                if (!string.Equals(path, bankResourcePath, StringComparison.Ordinal))
                    continue;

                try
                {
                    return bank.Call("get_event_description_count").AsInt64();
                }
                catch
                {
                    return -1;
                }
            }

            return -1;
        }

        /// <summary>
        ///     Logs Studio <c>event:/…</c> paths reported by <c>FmodBank.get_description_list</c> for an already-loaded
        ///     bank (single framework info line). Does not log global cache totals.
        /// </summary>
        public static void TryLogLoadedStudioBankEvents(string bankResourcePath)
        {
            if (string.IsNullOrWhiteSpace(bankResourcePath))
                return;

            var paths = TryCollectLoadedBankEventPaths(bankResourcePath);
            if (paths is null)
            {
                RitsuLibFramework.Logger.Warn($"[Audio] FMOD bank not loaded or unreadable: {bankResourcePath}");
                return;
            }

            if (paths.Count == 0)
            {
                RitsuLibFramework.Logger.Warn(
                    "[Audio] FMOD bank has no events — rebuild banks from FMOD Studio or verify the exported .bank.");
                return;
            }

            const int maxListed = 40;
            var sb = new StringBuilder(256);
            var n = Math.Min(paths.Count, maxListed);
            for (var i = 0; i < n; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                sb.Append(paths[i]);
            }

            if (paths.Count > maxListed)
                sb.Append(" … (+").Append(paths.Count - maxListed).Append(" more)");

            RitsuLibFramework.Logger.Info(
                $"[Audio] FMOD bank {bankResourcePath} ({paths.Count} event{(paths.Count == 1 ? "" : "s")}): {sb}");
        }

        private static List<string>? TryCollectLoadedBankEventPaths(string bankResourcePath)
        {
            if (!FmodStudioGateway.TryCall(out var banksVar, FmodStudioMethodNames.GetAllBanks) ||
                banksVar.VariantType != Variant.Type.Array)
                return null;

            foreach (var item in banksVar.AsGodotArray())
            {
                var bank = item.AsGodotObject();
                if (bank is null)
                    continue;

                string resPath;
                try
                {
                    resPath = bank.Call("get_godot_res_path").AsString();
                }
                catch
                {
                    continue;
                }

                if (!string.Equals(resPath, bankResourcePath, StringComparison.Ordinal))
                    continue;

                var paths = new List<string>();
                try
                {
                    var listVar = bank.Call("get_description_list");
                    if (listVar.VariantType != Variant.Type.Array)
                        return paths;

                    paths.AddRange(listVar.AsGodotArray().Select(d => d.AsGodotObject())
                        .Select(desc => desc.Call("get_path").AsString()));
                }
                catch
                {
                    return null;
                }

                return paths;
            }

            return null;
        }

        private static bool TryCallNativeGuidInject(string resourcePath)
        {
            var server = FmodStudioGateway.TryGetServer();
            if (server is null)
                return false;

            foreach (var method in GuidMappingInjectCandidates)
            {
                if (!server.HasMethod(method))
                    continue;

                try
                {
                    var r = server.Call(method, resourcePath);
                    if (r.VariantType == Variant.Type.Bool && !r.AsBool())
                        continue;

                    return true;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Error($"[Audio] FMOD guid inject {method}: {ex.Message}");
                }
            }

            return false;
        }
    }
}
