using System.Runtime.CompilerServices;
using Godot;
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
    ///     在 <see cref="NAncientEventLayout.InitializeVisuals" /> 之后，当设置了
    ///     <see cref="AncientEventPresentationAssetProfile.StageProcedural" /> 时，
    ///     用程序化舞台图层替换已实例化的背景子树。
    /// </summary>
    public class NAncientEventLayoutProceduralStagePatch : IPatchMethod
    {
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
        ///     当设置了 <c>StageProcedural</c> 时，用程序化 sprite 替换已实例化的背景子树。
        /// </summary>
        public static void Postfix(NAncientEventLayout __instance)
        {
            var ancient = AncientEvent(__instance);
            if (ancient is not IModAncientEventAssetOverrides mod)
                return;

            var stage = mod.AncientPresentationAssetProfile?.StageProcedural;
            if (stage == null)
                return;

            var container = AncientBgContainer(__instance);
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

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_ancientEvent")]
        private static extern ref AncientEventModel AncientEvent(NAncientEventLayout instance);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_ancientBgContainer")]
        private static extern ref NAncientBgContainer AncientBgContainer(NAncientEventLayout instance);
    }
}
