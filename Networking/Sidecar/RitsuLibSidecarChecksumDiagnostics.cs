using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarChecksumDiagnostics
    {
        private const ushort DivergenceRelayTag = 1;

        private static ChecksumTracker? _tracker;

        internal static void EnsureSubscribed()
        {
            var next = RunManager.Instance?.ChecksumTracker;
            if (next == null)
                return;

            if (ReferenceEquals(_tracker, next))
                return;

            Unsubscribe();
            _tracker = next;
            _tracker.StateDiverged += OnClientStateDiverged;
        }

        internal static void Unsubscribe()
        {
            if (_tracker != null)
                _tracker.StateDiverged -= OnClientStateDiverged;

            _tracker = null;
        }

        internal static void TryLogLocalCombatDump(string reason, ushort tag)
        {
            try
            {
                var rm = RunManager.Instance;
                if (rm?.State == null)
                    return;

                var snapshot = NetFullCombatState.FromRun(rm.State, null);
                Log.Error(
                    $"[Sidecar diagnostic dump tag={tag}] {reason}\n{snapshot}");
            }
            catch (Exception ex)
            {
                Log.Warn($"[Sidecar diagnostic dump failed: {ex.Message}");
            }
        }

        internal static void TryTriggerHostCoordinatedDump(ulong divergentPeerId)
        {
            try
            {
                var rm = RunManager.Instance;
                if (rm?.NetService is not NetHostGameService)
                    return;

                TryLogLocalCombatDump(
                    $"Sidecar host divergence dump trigger (peer={divergentPeerId})",
                    DivergenceRelayTag);
                var payload = RitsuLibSidecarDiagnosticPayload.BuildFanoutPayload(divergentPeerId, DivergenceRelayTag);
                RitsuLibSidecarHighLevelSend.TrySendAsHostBroadcast(
                    rm,
                    RitsuLibSidecarControlOpcodes.DiagnosticRelayDumpFanout,
                    payload,
                    RitsuLibSidecarDeliverySemantics.StableSync);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Sidecar] host divergence relay failed: {ex.Message}");
            }
        }

        private static void OnClientStateDiverged(NetFullCombatState _)
        {
            try
            {
                RitsuLibSidecarProtocol.EnsureDefaultHandlers();
                var rm = RunManager.Instance;
                RitsuLibSidecarHighLevelSend.TrySendAsClient(
                    rm,
                    RitsuLibSidecarControlOpcodes.DiagnosticRelayDumpRequest,
                    ReadOnlySpan<byte>.Empty,
                    RitsuLibSidecarDeliverySemantics.StableSync);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Sidecar] checksum divergence relay failed: {ex.Message}");
            }
        }
    }
}
