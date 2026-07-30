using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.Sidecar.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Validates required sidecar capabilities before the host starts a run. The <c>Fail</c> policy blocks the
    ///         run, while the <c>Warn</c> policy logs the missing capabilities and continues.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在主机开始一局游戏前验证所需的 sidecar 能力。<c>Fail</c> 策略会阻止游戏开始，
    ///         <c>Warn</c> 策略则记录缺失的能力并继续。
    ///     </para>
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
