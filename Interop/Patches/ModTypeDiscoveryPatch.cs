using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Diagnostics;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Interop.Patches
{
    /// <summary>
    ///     Runs the <see cref="ModTypeDiscoveryHub" /> pipeline once, at the same lifecycle point BaseLib uses
    ///     (before heavy game systems consume localization).
    ///     在 BaseLib 使用的同一生命周期点运行一次 <see cref="ModTypeDiscoveryHub" /> 管线
    ///     （重型游戏系统消费本地化之前）。
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
