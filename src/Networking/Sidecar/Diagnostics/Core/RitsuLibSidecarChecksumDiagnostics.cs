using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarChecksumDiagnostics
    {
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

        internal static void TryTriggerHostCoordinatedDump(ulong divergentPeerId, uint checksumId)
        {
            try
            {
                var rm = RunManager.Instance;
                if (rm?.NetService is not NetHostGameService)
                    return;
                if (!RitsuLibSidecarDiagnosticRelayGate.TryBeginHostSession(
                        divergentPeerId,
                        checksumId,
                        RitsuLibSidecarDiagnosticPolicy.DivergenceRelayTag,
                        out var session))
                    return;

                var payload = RitsuLibSidecarDiagnosticPayload.BuildFanoutPayload(session);
                RitsuLibSidecarHighLevelSend.TrySendAsHostBroadcast(
                    rm,
                    RitsuLibSidecarControlOpcodes.DiagnosticRelayDumpFanout,
                    payload,
                    RitsuLibSidecarDeliverySemantics.StableSync);
                RitsuLibFramework.Logger.Debug(
                    $"[Sidecar] Host divergence relay fanout sent peer={divergentPeerId}, checksum={checksumId}, tag={session.Tag}, nonce={session.Nonce}.");
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
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Sidecar] checksum divergence relay failed: {ex.Message}");
            }
        }
    }
}
