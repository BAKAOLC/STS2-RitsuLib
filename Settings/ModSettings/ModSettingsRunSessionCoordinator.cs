using System.Collections.Concurrent;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Settings
{
    internal interface IRunScopedSettingsParticipant
    {
        void OnRunSnapshot();
        void OnRunEnded();
    }

    internal interface IRunOverlayHostApplier
    {
        string SlotKey { get; }

        bool TryApply(object value);
    }

    /// <summary>
    ///     Hooks run lifecycle (via patches) to snapshot / flush run-scoped settings overlays.
    /// </summary>
    internal static class ModSettingsRunSessionCoordinator
    {
        private static readonly Lock Sync = new();
        private static readonly List<IRunScopedSettingsParticipant> Participants = [];

        private static readonly ConcurrentDictionary<string, IRunOverlayHostApplier> HostAppliers =
            new(StringComparer.OrdinalIgnoreCase);

        internal static string MakeOverlaySlotKey(string modId, string dataKey)
        {
            return $"{modId}|{dataKey}";
        }

        internal static void RegisterParticipant(IRunScopedSettingsParticipant participant)
        {
            lock (Sync)
            {
                if (!Participants.Contains(participant))
                    Participants.Add(participant);
            }
        }

        internal static void RegisterHostApplier(IRunOverlayHostApplier applier)
        {
            HostAppliers[applier.SlotKey] = applier;
        }

        internal static void UnregisterParticipant(IRunScopedSettingsParticipant participant)
        {
            lock (Sync)
            {
                Participants.Remove(participant);
            }
        }

        internal static void UnregisterHostApplier(string slotKey)
        {
            HostAppliers.TryRemove(slotKey, out _);
        }

        internal static bool TryApplyHostOverlay(string slotKey, object value)
        {
            return HostAppliers.TryGetValue(slotKey, out var applier) && applier.TryApply(value);
        }

        /// <summary>
        ///     Called from <see cref="STS2RitsuLib.Lifecycle.Patches.RunLifecyclePatch" /> after a new or loaded run exists.
        /// </summary>
        internal static void NotifyRunStarted(RunManager runManager, bool isMultiplayer, bool isDaily)
        {
            ArgumentNullException.ThrowIfNull(runManager);

            ModSettingsSessionState.IsInActiveRun = true;
            ModSettingsSessionState.IsMultiplayerRun = isMultiplayer;
            var net = runManager.NetService;
            ModSettingsSessionState.IsNetClient = net is { Type: NetGameType.Client };

            IRunScopedSettingsParticipant[] copy;
            lock (Sync)
            {
                copy = [.. Participants];
            }

            foreach (var p in copy)
                try
                {
                    p.OnRunSnapshot();
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn($"[Settings] Run overlay snapshot failed: {ex.Message}");
                }

            _ = isDaily;
        }

        /// <summary>
        ///     Called from <see cref="STS2RitsuLib.Lifecycle.Patches.RunEndedLifecyclePatch" />.
        /// </summary>
        internal static void NotifyRunEnded()
        {
            IRunScopedSettingsParticipant[] copy;
            lock (Sync)
            {
                copy = [.. Participants];
            }

            foreach (var p in copy)
                try
                {
                    p.OnRunEnded();
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn($"[Settings] Run overlay end flush failed: {ex.Message}");
                }

            ModSettingsSessionState.ClearRunFlags();
        }
    }
}
