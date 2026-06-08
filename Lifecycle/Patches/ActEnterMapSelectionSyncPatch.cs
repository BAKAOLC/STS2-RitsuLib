using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     After <see cref="RunManager.GenerateMap" /> finishes applying the generated map to the UI, optionally bumps
    ///     <see cref="MegaCrit.Sts2.Core.Multiplayer.Game.MapSelectionSynchronizer.MapGenerationCount" /> when act-enter logic
    ///     replaced the <see cref="ActModel" />, so votes and relic-driven layout changes stay
    ///     consistent (avoids patching <see cref="RunManager.SetActInternal" />, whose Harmony postfix can run before
    ///     <see cref="RunManager.GenerateMap" /> completes).
    ///     在 <see cref="RunManager.GenerateMap" /> 完成生成地图并应用到 UI 后，如果 act-enter 逻辑
    ///     替换了 <see cref="ActModel" />，则可选择增加
    ///     <see cref="MegaCrit.Sts2.Core.Multiplayer.Game.MapSelectionSynchronizer.MapGenerationCount" />，使投票和遗物驱动的布局变化保持
    ///     一致（避免 patch <see cref="RunManager.SetActInternal" />，因为其 Harmony postfix 可能在
    ///     <see cref="RunManager.GenerateMap" /> 完成前运行）。
    /// </summary>
    internal sealed class ActEnterMapSelectionSyncPatch : IPatchMethod
    {
        public static string PatchId => "act_enter_map_selection_sync";

        public static string Description =>
            "After RunManager.GenerateMap completes, call MapSelectionSynchronizer.BeforeMapGenerated when EnterAct replaced the act model";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RunManager), nameof(RunManager.GenerateMap), Type.EmptyTypes),
            ];
        }

        public static void Postfix(ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, BumpMapSelectionSynchronizerIfRequested);
        }

        private static void BumpMapSelectionSynchronizerIfRequested()
        {
            if (!ModContentRegistry.TryConsumeActEnterPostMapUiMapSyncBump())
                return;

            RunManager.Instance?.MapSelectionSynchronizer?.BeforeMapGenerated();
        }
    }
}
