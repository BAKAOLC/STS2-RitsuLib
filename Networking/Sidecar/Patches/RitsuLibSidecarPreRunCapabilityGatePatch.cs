using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.Sidecar.Patches
{
    /// <summary>
    ///     Runs required sidecar capability validation before begin-run. With <c>Fail</c> policy the patch blocks
    ///     begin-run; with <c>Warn</c> policy it only logs warnings.
    /// </summary>
    internal sealed class RitsuLibSidecarPreRunCapabilityGatePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_sidecar_pre_run_capability_gate";
        public static bool IsCritical => false;
        public static string Description => "Validates required sidecar capabilities before StartRunLobby begins run";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(StartRunLobby), "BeginRunForAllPlayers", [typeof(string), typeof(List<ModifierModel>)])];
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(StartRunLobby __instance)
        {
            if (__instance.NetService is not NetHostGameService host)
                return true;

            var peers = host.ConnectedPeers.Select(p => p.peerId);
            RitsuLibSidecarRequiredCapabilities.ValidatePeers(peers, out var misses);
            if (misses.Length == 0)
                return true;

            var detail = string.Join("; ", misses.Select(m =>
                $"peer={m.PeerNetId}, missing=[{string.Join(", ", m.MissingCapabilities)}]"));
            if (RitsuLibSidecarRequiredCapabilities.Policy == RitsuLibSidecarRequiredCapabilityPolicy.Fail)
            {
                RitsuLibFramework.Logger.Warn($"[Sidecar] BeginRun blocked by required capability check: {detail}");
                return false;
            }

            RitsuLibFramework.Logger.Warn($"[Sidecar] BeginRun continue with required capability warnings: {detail}");
            return true;
        }
    }
}
