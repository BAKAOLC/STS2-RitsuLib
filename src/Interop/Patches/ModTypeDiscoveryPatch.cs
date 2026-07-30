using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Diagnostics;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Interop.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Runs <see cref="ModTypeDiscoveryHub" /> once at the same lifecycle point used by BaseLib, before
    ///         later game systems consume localization data.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 BaseLib 使用的同一生命周期节点运行一次 <see cref="ModTypeDiscoveryHub" />，此时后续游戏系统
    ///         尚未使用本地化数据。
    ///     </para>
    /// </summary>
    internal sealed class ModTypeDiscoveryPatch : IPatchMethod
    {
        private static readonly Lock RunGate = new();
        private static bool _completed;
        public static string PatchId => "ritsulib_mod_type_discovery";

        public static string Description =>
            "Post-mod-load type discovery (ModInterop and extensible contributors)";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(LocManager), nameof(LocManager.Initialize))];
        }

        public static void Prefix()
        {
            lock (RunGate)
            {
                if (_completed)
                    return;
                _completed = true;
            }

            var harmony = new Harmony($"{Const.ModId}.mod_type_discovery");
            RitsuLibStartupAudit.Measure("modTypeDiscovery.runOnce",
                () => ModTypeDiscoveryHub.RunOnce(harmony));
            RitsuLibStartupAudit.Measure("flushDeferredContentPacks",
                RitsuLibFramework.FlushDeferredContentPacks);
        }
    }
}
