using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Godot;

namespace STS2RitsuLib.Models.Capabilities.Patches
{
    /// <summary>
    ///     Bridges card overlay capabilities into <see cref="NCard" /> after vanilla overlay reloads.
    ///     在原版覆盖层重载后，将卡牌覆盖层能力桥接到 <see cref="NCard" />。
    /// </summary>
    internal static class CardOverlayCapabilityPatches
    {
        internal sealed class ReloadOverlayPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_overlays";

            public static string Description => "Add model-capability card overlays after vanilla card overlays";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NCard), "ReloadOverlay")];
            }

            public static void Postfix(NCard __instance)
            {
                CardOverlayCapabilityPatchHelper.Refresh(__instance);
            }
        }
    }

    internal static class CardOverlayCapabilityPatchHelper
    {
        private const string ManagedContainerName = "RitsuLibCapabilityOverlays";
        private const string OverlaySurface = "card display/overlay";

        internal static void Refresh(NCard cardNode)
        {
            if (!GodotObject.IsInstanceValid(cardNode))
                return;

            var overlayContainer = cardNode.GetNodeOrNull<Node>("%OverlayContainer");
            if (overlayContainer == null || !GodotObject.IsInstanceValid(overlayContainer))
                return;

            RemoveManagedContainer(overlayContainer);

            if (cardNode.Model is not { } card)
                return;

            var context = new CardOverlayContext(card, cardNode, overlayContainer);
            var contributions = CardModelCapabilityHost.GetOverlayContributions(context)
                .OrderBy(static contribution => contribution.Contribution.Order)
                .ThenBy(static contribution => contribution.SourceIndex)
                .ThenBy(static contribution => contribution.Contribution.Id, StringComparer.Ordinal)
                .ToArray();
            if (contributions.Length == 0)
                return;

            var managedContainer = CreateManagedContainer();
            foreach (var contribution in contributions)
            {
                var overlay = TryCreateOverlay(context, contribution);
                if (overlay == null)
                    continue;

                PrepareOverlay(overlay, contribution.Contribution);
                RitsuGodotTreeCompat.AddChildSafely(managedContainer, overlay);
            }

            if (managedContainer.GetChildCount() == 0)
            {
                managedContainer.QueueFreeSafely();
                return;
            }

            RitsuGodotTreeCompat.AddChildSafely(overlayContainer, managedContainer);
        }

        private static Control CreateManagedContainer()
        {
            var container = new Control
            {
                Name = ManagedContainerName,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return container;
        }

        private static void RemoveManagedContainer(Node overlayContainer)
        {
            var existing = overlayContainer.GetNodeOrNull<Control>(ManagedContainerName);
            if (existing == null || !GodotObject.IsInstanceValid(existing))
                return;

            overlayContainer.RemoveChildSafely(existing);
            existing.QueueFreeSafely();
        }

        private static Control? TryCreateOverlay(
            CardOverlayContext context,
            CardModelCapabilityHost.OrderedCardOverlayContribution ordered)
        {
            try
            {
                return CreateOverlay(context, ordered);
            }
            catch (Exception ex)
            {
                ModelCapabilityDiagnostics.WarnFailure(OverlaySurface, context.Card, ordered.Source, ex);
                return null;
            }
        }

        private static Control? CreateOverlay(
            CardOverlayContext context,
            CardModelCapabilityHost.OrderedCardOverlayContribution ordered)
        {
            var contribution = ordered.Contribution;
            if (string.IsNullOrWhiteSpace(contribution.Id))
            {
                WarnInvalid(context.Card, ordered.Source, contribution, "overlay id is empty");
                return null;
            }

            var sourceCount = CountCreationSources(contribution);
            if (sourceCount != 1)
            {
                WarnInvalid(context.Card, ordered.Source, contribution,
                    $"expected exactly one creation source, found {sourceCount}");
                return null;
            }

            if (!string.IsNullOrWhiteSpace(contribution.ScenePath))
            {
                if (!ResourceLoader.Exists(contribution.ScenePath))
                {
                    WarnInvalid(context.Card, ordered.Source, contribution,
                        $"scene path does not exist: '{contribution.ScenePath}'");
                    return null;
                }

                return PreloadManager.Cache
                    .GetScene(contribution.ScenePath)
                    .Instantiate<Control>();
            }

            if (contribution.Scene != null)
                return contribution.Scene.Instantiate<Control>();

            var control = contribution.Factory!(context);
            if (control == null)
                WarnInvalid(context.Card, ordered.Source, contribution, "factory returned null");
            return control;
        }

        private static void PrepareOverlay(Control overlay, CardOverlayContribution contribution)
        {
            overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
            if (!contribution.FullRect)
                return;

            overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }

        private static int CountCreationSources(CardOverlayContribution contribution)
        {
            var count = 0;
            if (!string.IsNullOrWhiteSpace(contribution.ScenePath))
                count++;
            if (contribution.Scene != null)
                count++;
            if (contribution.Factory != null)
                count++;

            return count;
        }

        private static void WarnInvalid(
            CardModel card,
            IModelCapability source,
            CardOverlayContribution contribution,
            string reason)
        {
            RitsuLibFramework.Logger.Warn(
                $"[ModelCapabilities] Surface='{OverlaySurface}' ignored invalid overlay. " +
                $"ModelId='{card.Id}' CapabilityId='{source.CapabilityId}' " +
                $"CapabilityType='{source.GetType().FullName}' OverlayId='{contribution.Id}' Reason='{reason}'");
        }
    }
}
