using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Screens;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content.Visuals;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     After <see cref="NAncientEventLayout.InitializeVisuals" />, replaces the instantiated background subtree with
    ///     procedural stage layers when <see cref="AncientEventPresentationAssetProfile.StageProcedural" /> is set.
    ///     在 <c>NAncientEventLayout.InitializeVisuals</c> 之后，当
    ///     <c>AncientEventPresentationAssetProfile.StageProcedural</c> 已设置时，用程序化舞台图层替换已实例化的背景子树。
    /// </summary>
    public class NAncientEventLayoutProceduralStagePatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NAncientEventLayout, AncientEventModel> AncientEventRef =
            AccessTools.FieldRefAccess<NAncientEventLayout, AncientEventModel>("_ancientEvent");

        private static readonly AccessTools.FieldRef<NAncientEventLayout, NAncientBgContainer> BgContainerRef =
            AccessTools.FieldRefAccess<NAncientEventLayout, NAncientBgContainer>("_ancientBgContainer");

        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "n_ancient_event_layout_procedural_stage";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Mount AncientEventStageProceduralVisualSet layers on NAncientBgContainer after layout init";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NAncientEventLayout), "InitializeVisuals")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Replaces the instantiated background subtree with procedural sprites when <c>StageProcedural</c> is set.
        ///     当 <c>StageProcedural</c> 已设置时，用程序化 sprite 图层替换已实例化的背景子树。
        /// </summary>
        public static void Postfix(NAncientEventLayout __instance)
        {
            var ancient = AncientEventRef(__instance);
            if (ancient is not IModAncientEventAssetOverrides mod)
                return;

            var stage = mod.AncientPresentationAssetProfile?.StageProcedural;
            if (stage == null)
                return;

            var container = BgContainerRef(__instance);
            if (container == null || !GodotObject.IsInstanceValid(container))
            {
                RitsuLibFramework.Logger.Warn(
                    "[AncientStage] Could not mount StageProcedural because NAncientEventLayout._ancientBgContainer is not available.");
                return;
            }

            foreach (var child in container.GetChildren().ToList())
            {
                container.RemoveChildSafely(child);
                child.QueueFreeSafely();
            }

            AncientStageProceduralRootFactory.BuildAndMount(container, stage);
        }
    }
}
