using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Updates multiplayer map-selection synchronization after <see cref="RunManager.GenerateMap" /> finishes
    ///         when Act-entry logic replaced the generated <see cref="ActModel" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         当章节进入逻辑替换了生成的 <see cref="ActModel" /> 时，在 <see cref="RunManager.GenerateMap" />
    ///         完成后更新多人地图选择同步状态。
    ///     </para>
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
