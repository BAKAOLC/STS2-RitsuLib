using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content.Visuals;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Supplies a minimal placeholder <see cref="PackedScene" /> for Ancient events that use
    ///         <see cref="AncientEventPresentationAssetProfile.StageProcedural" />, avoiding the need for a background
    ///         <c>.tscn</c> file.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为使用 <see cref="AncientEventPresentationAssetProfile.StageProcedural" /> 的先古事件提供最小化的
    ///         <see cref="PackedScene" /> 占位场景，无需另备背景 <c>.tscn</c> 文件。
    ///     </para>
    /// </summary>
    internal class AncientEventProceduralBackgroundScenePatch : IPatchMethod
    {
        public static string PatchId => "ancient_event_procedural_background_scene";

        public static string Description =>
            "Return placeholder PackedScene for CreateBackgroundScene when ancient StageProcedural is defined";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EventModel), nameof(EventModel.CreateBackgroundScene))];
        }

        /// <summary>
        ///     <para xml:lang="en">Skips background-scene loading when an Ancient event uses procedural stage layers.</para>
        ///     <para xml:lang="zh-CN">先古事件使用程序化舞台图层时，跳过背景场景加载。</para>
        /// </summary>
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(EventModel __instance, ref PackedScene __result)
        {
            if (__instance is not AncientEventModel)
                return true;

            if (__instance is not IModAncientEventAssetOverrides mod)
                return true;

            if (mod.AncientPresentationAssetProfile?.StageProcedural == null)
                return true;

            __result = AncientStageProceduralRootFactory.PlaceholderBackgroundPackedScene;
            return false;
        }
    }
}
