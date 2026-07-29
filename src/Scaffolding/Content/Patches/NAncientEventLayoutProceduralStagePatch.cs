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
    internal class NAncientEventLayoutProceduralStagePatch : IPatchMethod
    {
        public static string PatchId => "n_ancient_event_layout_procedural_stage";

        public static string Description =>
            "Mount AncientEventStageProceduralVisualSet layers on NAncientBgContainer after layout init";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NAncientEventLayout), "InitializeVisuals")];
        }

        public static void Postfix(NAncientEventLayout __instance)
        {
            var ancient = AncientEvent(__instance);
            if (ancient is not IModAncientEventAssetOverrides mod)
                return;

            var stage = mod.AncientPresentationAssetProfile?.StageProcedural;
            if (stage == null)
                return;

            if (string.IsNullOrWhiteSpace(stage.BackgroundVideoPath) && stage.BackgroundCueSet == null)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[AncientStage] Could not mount StageProcedural for '{ancient.Id}' because no background video or cue set was configured.");
                return;
            }

            var container = AncientBgContainer(__instance);
            if (container == null || !GodotObject.IsInstanceValid(container))
            {
                RitsuLibFramework.Logger.Warn(
                    "[AncientStage] Could not mount StageProcedural because NAncientEventLayout._ancientBgContainer is not available.");
                return;
            }

            var originalChildren = container.GetChildren().ToList();
            var originalInstanceIds = originalChildren
                .Where(GodotObject.IsInstanceValid)
                .Select(static child => child.GetInstanceId())
                .ToHashSet();

            try
            {
                AncientStageProceduralRootFactory.BuildAndMount(container, stage);
            }
            catch (Exception ex)
            {
                RemoveNewChildren(container, originalInstanceIds);
                RitsuLibFramework.Logger.Warn(
                    $"[AncientStage] Failed to mount StageProcedural for '{ancient.Id}': {ex.Message}. Keeping the existing background.");
                return;
            }

            foreach (var child in originalChildren)
            {
                if (!GodotObject.IsInstanceValid(child) || child.GetParent() != container)
                    continue;

                container.RemoveChildSafely(child);
                child.QueueFreeSafely();
            }
        }

        private static void RemoveNewChildren(NAncientBgContainer container, HashSet<ulong> originalInstanceIds)
        {
            foreach (var child in container.GetChildren().ToList())
            {
                if (!GodotObject.IsInstanceValid(child) || originalInstanceIds.Contains(child.GetInstanceId()))
                    continue;

                container.RemoveChildSafely(child);
                child.QueueFreeSafely();
            }
        }

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_ancientEvent")]
        private static extern ref AncientEventModel AncientEvent(NAncientEventLayout instance);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_ancientBgContainer")]
        private static extern ref NAncientBgContainer AncientBgContainer(NAncientEventLayout instance);
    }
}
