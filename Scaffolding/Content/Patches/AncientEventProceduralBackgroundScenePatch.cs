using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content.Visuals;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Supplies a tiny packed placeholder scene for ancient events that use
    ///     <see cref="AncientEventPresentationAssetProfile.StageProcedural" />, so
    ///     <see cref="EventModel.CreateBackgroundScene" /> does not require a real background <c>.tscn</c>.
    ///     对使用 <see cref="AncientEventPresentationAssetProfile.StageProcedural" /> 的远古事件提供一个极小的 packed scene
    ///     占位符，使 <see cref="EventModel.CreateBackgroundScene" /> 不再需要真实的背景 <c>.tscn</c>。
    /// </summary>
    public class AncientEventProceduralBackgroundScenePatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "ancient_event_procedural_background_scene";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Return placeholder PackedScene for CreateBackgroundScene when ancient StageProcedural is defined";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EventModel), nameof(EventModel.CreateBackgroundScene))];
        }

        /// <summary>
        ///     Short-circuits background scene load when the ancient uses procedural stage layers.
        ///     当远古事件使用程序化舞台图层时，跳过真实背景场景加载。
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
