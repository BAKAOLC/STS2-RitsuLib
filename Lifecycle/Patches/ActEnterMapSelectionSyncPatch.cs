using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     After the map UI finishes applying <see cref="NMapScreen.SetMap" />, optionally bumps
    ///     <see cref="MegaCrit.Sts2.Core.Multiplayer.Game.MapSelectionSynchronizer.MapGenerationCount" /> when act-enter logic
    ///     replaced the <see cref="ActModel" />, so votes and relic-driven layout changes stay
    ///     consistent (avoids patching <see cref="RunManager.SetActInternal" />, whose Harmony postfix can run before
    ///     <see cref="RunManager.GenerateMap" /> completes).
    ///     在地图 UI 完成应用 <see cref="NMapScreen.SetMap" /> 后，如果 act-enter 逻辑
    ///     替换了 <see cref="ActModel" />，则可选择增加
    ///     <see cref="MegaCrit.Sts2.Core.Multiplayer.Game.MapSelectionSynchronizer.MapGenerationCount" />，使投票和遗物驱动的布局变化保持
    ///     一致（避免 patch <see cref="RunManager.SetActInternal" />，因为其 Harmony postfix 可能在
    ///     <see cref="RunManager.GenerateMap" /> 完成前运行）。
    /// </summary>
    public sealed class ActEnterMapSelectionSyncPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "act_enter_map_selection_sync";

        /// <inheritdoc />
        public static string Description =>
            "After NMapScreen.SetMap, call MapSelectionSynchronizer.BeforeMapGenerated when EnterAct replaced the act model";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NMapScreen), nameof(NMapScreen.SetMap), [typeof(ActMap), typeof(uint), typeof(bool)]),
            ];
        }

        /// <summary>
        ///     Harmony postfix: synchronizer bump after the visible map matches <see cref="RunManager.GenerateMap" /> output.
        ///     Harmony postfix：在可见地图与 <see cref="RunManager.GenerateMap" /> 输出一致后递增同步器。
        /// </summary>
        public static void Postfix()
        {
            if (!ModContentRegistry.TryConsumeActEnterPostMapUiMapSyncBump())
                return;

            RunManager.Instance?.MapSelectionSynchronizer?.BeforeMapGenerated();
        }
    }
}
