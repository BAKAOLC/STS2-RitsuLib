using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using STS2RitsuLib.Data;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    internal static class ContentSourceHoverTipPatchHelper
    {
        private const string EventBadgeNodeName = "RitsuLibContentSourceBadge";
        private const string EventDrawerHotZoneName = "RitsuLibContentSourceHotZone";
        private const string HoverTipScenePath = "res://scenes/ui/hover_tip.tscn";
        private const string HoverTipSetScenePath = "res://scenes/ui/hover_tip_set.tscn";
        private const float EventTipWidth = 360f;
        private const float EventTipRightMargin = 42f;
        private const float EventTipBottomMargin = 38f;
        private const float EventTipPeekWidth = 34f;
        private const float EventTipHotZoneWidth = 96f;
        private const float EventTipHotZoneMinHeight = 112f;
        private const double EventTipSlideDuration = 0.28;

        internal static void Append(ContentSourceHoverTipFactory.ContentSourceInfo source, ref HoverTip tip)
        {
            tip.Description = $"[purple]{source.Format()}[/purple]\n{tip.Description}";
        }

        internal static void Append(AbstractModel model, ref IEnumerable<IHoverTip> result)
        {
            if (!ContentSourceHoverTipFactory.TryCreate(model, out var tip))
                return;

            result = [tip, .. result];
        }

        internal static void AppendToFirstHoverTip(AbstractModel model, ref IEnumerable<IHoverTip> result)
        {
            if (!ContentSourceHoverTipFactory.TryResolve(model, out var source))
                return;

            var tips = result.ToList();
            for (var i = 0; i < tips.Count; i++)
            {
                if (tips[i] is not HoverTip tip)
                    continue;

                Append(source, ref tip);
                tips[i] = tip;
                result = tips;
                return;
            }

            result = [CreateSourceTip(source), .. tips];
        }

        private static HoverTip CreateSourceTip(ContentSourceHoverTipFactory.ContentSourceInfo source)
        {
            return new(ContentSourceHoverTipFactory.GetTitle(), source.Format())
            {
                Id = "ritsulib:content_source:" + source.Id,
            };
        }

        internal static void UpdateEventSourceBadge(NEventLayout layout, EventModel eventModel)
        {
            if (!IsNodeUsable(layout))
                return;

            var existing = layout.GetNodeOrNull<NHoverTipSet>(EventBadgeNodeName);
            var existingHotZone = layout.GetNodeOrNull<Control>(EventDrawerHotZoneName);
            if (!RitsuLibSettingsStore.IsModSourceHoverTipsEnabled() ||
                !RitsuLibSettingsStore.ShouldShowEventModSourceHoverTips())
            {
                RemoveBadge(layout, existing);
                RemoveBadge(layout, existingHotZone);
                return;
            }

            var source = ContentSourceHoverTipFactory.Resolve(eventModel.GetType());
            if (!ContentSourceHoverTipFactory.ShouldShow(source))
            {
                RemoveBadge(layout, existing);
                RemoveBadge(layout, existingHotZone);
                return;
            }

            RemoveBadge(layout, existing);
            RemoveBadge(layout, existingHotZone);
            var tipSet = CreateEventSourceTipSet(layout, source);
            var hotZone = CreateEventSourceHotZone();
            layout.AddChildSafely(tipSet);
            layout.AddChildSafely(hotZone);
            tipSet.Visible = true;
            Callable.From(() => PopulateAndPositionEventSourceTipSet(layout, tipSet, hotZone, source)).CallDeferred();
        }

        private static NHoverTipSet CreateEventSourceTipSet(Control owner,
            ContentSourceHoverTipFactory.ContentSourceInfo source)
        {
            var tipSet = PreloadManager.Cache
                .GetScene(HoverTipSetScenePath)
                .Instantiate<NHoverTipSet>();
            tipSet.Name = EventBadgeNodeName;
            tipSet.MouseFilter = Control.MouseFilterEnum.Ignore;
            tipSet.ZIndex = 0;
            tipSet._owner = owner;
            return tipSet;
        }

        private static void PopulateAndPositionEventSourceTipSet(
            Control layout,
            NHoverTipSet tipSet,
            Control hotZone,
            ContentSourceHoverTipFactory.ContentSourceInfo source)
        {
            if (!IsEventSourceBadgeUsable(layout, tipSet, hotZone))
                return;

            if (tipSet._textHoverTipContainer == null)
            {
                Callable.From(() => PopulateAndPositionEventSourceTipSet(layout, tipSet, hotZone, source))
                    .CallDeferred();
                return;
            }

            if (!IsNodeUsable(tipSet._textHoverTipContainer))
                return;

            AddHoverTipControl(tipSet, new(ContentSourceHoverTipFactory.GetTitle(), source.Format())
            {
                Id = "ritsulib:event_content_source:" + source.Id,
            });
            ConfigureEventSourceTipDrawer(layout, tipSet, hotZone);
            PositionEventSourceTipSet(layout, tipSet, false, false);
            PositionEventSourceHotZone(layout, tipSet, hotZone);
        }

        private static void AddHoverTipControl(NHoverTipSet tipSet, HoverTip hoverTip)
        {
            if (!IsNodeUsable(tipSet) || !IsNodeUsable(tipSet._textHoverTipContainer))
                return;

            var tipControl = PreloadManager.Cache
                .GetScene(HoverTipScenePath)
                .Instantiate<Control>();
            tipSet._textHoverTipContainer.AddChildSafely(tipControl);

            var titleLabel = tipControl.GetNode<MegaLabel>("%Title");
            if (string.IsNullOrEmpty(hoverTip.Title))
                titleLabel.Visible = false;
            else
                titleLabel.SetTextAutoSize(hoverTip.Title);

            tipControl.GetNode<MegaRichTextLabel>("%Description").Text = hoverTip.Description;
            tipControl.GetNode<TextureRect>("%Icon").Texture = hoverTip.Icon;
            tipControl.MouseFilter = Control.MouseFilterEnum.Ignore;
            tipControl.ResetSize();
            tipSet._textHoverTipContainer.Size = new(EventTipWidth, tipControl.Size.Y + 5f);
        }

        private static Control CreateEventSourceHotZone()
        {
            return new()
            {
                Name = EventDrawerHotZoneName,
                MouseFilter = Control.MouseFilterEnum.Stop,
                ZIndex = 1,
            };
        }

        private static void ConfigureEventSourceTipDrawer(Control layout, NHoverTipSet tipSet, Control hotZone)
        {
            if (!IsEventSourceBadgeUsable(layout, tipSet, hotZone) ||
                !IsNodeUsable(tipSet._textHoverTipContainer))
                return;

            var textContainer = tipSet._textHoverTipContainer;
            textContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
            var isExpanded = false;
            hotZone.Connect(
                Control.SignalName.MouseEntered,
                Callable.From(() =>
                {
                    if (!IsEventSourceBadgeUsable(layout, tipSet, hotZone))
                        return;

                    isExpanded = true;
                    PositionEventSourceTipSet(layout, tipSet, true, true);
                }));
            hotZone.Connect(
                Control.SignalName.MouseExited,
                Callable.From(() =>
                {
                    if (!IsEventSourceBadgeUsable(layout, tipSet, hotZone))
                        return;

                    if (isExpanded)
                        PositionEventSourceTipSet(layout, tipSet, false, true);
                    isExpanded = false;
                }));
            hotZone.GuiInput += inputEvent =>
            {
                if (inputEvent is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                    return;

                if (!IsEventSourceBadgeUsable(layout, tipSet, hotZone))
                    return;

                isExpanded = !isExpanded;
                PositionEventSourceTipSet(layout, tipSet, isExpanded, true);
            };
        }

        private static void PositionEventSourceHotZone(Control layout, NHoverTipSet tipSet, Control hotZone)
        {
            if (!IsEventSourceBadgeUsable(layout, tipSet, hotZone) ||
                !IsNodeUsable(tipSet._textHoverTipContainer))
                return;

            var viewportSize = NGame.Instance?.GetViewportRect().Size ?? layout.GetViewportRect().Size;
            var textContainer = tipSet._textHoverTipContainer;
            var height = Math.Max(EventTipHotZoneMinHeight,
                Math.Max(textContainer.Size.Y, textContainer.GetCombinedMinimumSize().Y));
            hotZone.GlobalPosition = new(viewportSize.X - EventTipHotZoneWidth,
                viewportSize.Y - height - EventTipBottomMargin);
            hotZone.Size = new(EventTipHotZoneWidth, height);
        }

        private static void PositionEventSourceTipSet(Control layout, NHoverTipSet tipSet, bool expanded, bool animate)
        {
            if (!IsNodeUsable(layout) || !IsNodeUsable(tipSet) || !IsNodeUsable(tipSet._textHoverTipContainer))
                return;

            var viewportSize = NGame.Instance?.GetViewportRect().Size ?? layout.GetViewportRect().Size;
            var textContainer = tipSet._textHoverTipContainer;
            var height = Math.Max(textContainer.Size.Y, textContainer.GetCombinedMinimumSize().Y);
            var target = new Vector2(
                expanded ? viewportSize.X - EventTipWidth - EventTipRightMargin : viewportSize.X - EventTipPeekWidth,
                viewportSize.Y - height - EventTipBottomMargin);

            if (!animate)
            {
                textContainer.GlobalPosition = target;
                return;
            }

            tipSet.CreateTween()
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine)
                .TweenProperty(textContainer, "global_position", target, EventTipSlideDuration);
        }

        private static void RemoveBadge(Node owner, Node? badge)
        {
            if (!IsNodeUsable(owner) || badge == null || !IsNodeUsable(badge))
                return;

            if (badge.GetParent() == owner)
                owner.RemoveChildSafely(badge);
            badge.QueueFreeSafely();
        }

        private static bool IsEventSourceBadgeUsable(Control layout, NHoverTipSet tipSet, Control hotZone)
        {
            return IsNodeUsable(layout) &&
                   IsNodeUsable(tipSet) &&
                   IsNodeUsable(hotZone) &&
                   tipSet.GetParent() == layout &&
                   hotZone.GetParent() == layout;
        }

        private static bool IsNodeUsable(Node? node)
        {
            return node != null &&
                   GodotObject.IsInstanceValid(node) &&
                   !node.IsQueuedForDeletion();
        }
    }

    internal sealed class ContentSourceKeywordHoverTipPatch : IPatchMethod
    {
        public static string PatchId => "content_source_keyword_hover_tip";

        public static string Description =>
            "Add content source hover tip to keyword hover tips, if available from ContentSourceHoverTipFactory";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(HoverTipFactory), "FromKeyword")];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(CardKeyword keyword, ref IHoverTip __result)
            // ReSharper restore once InconsistentNaming
        {
            if (!RitsuLibSettingsStore.IsModSourceHoverTipsEnabled() ||
                !RitsuLibSettingsStore.ShouldShowKeywordModSourceHoverTips())
                return;

            var info = ContentSourceHoverTipFactory.ResolveKeyword(keyword);
            if (!ContentSourceHoverTipFactory.ShouldShow(info))
                return;

            if (__result is not HoverTip tip) return;
            ContentSourceHoverTipPatchHelper.Append(info, ref tip);
            __result = tip;
        }
    }

    internal sealed class ContentSourceStaticHoverTipPatch : IPatchMethod
    {
        public static string PatchId => "content_source_static_hover_tip";

        public static string Description =>
            "Add vanilla content source to static hover tips such as block and fatal when enabled";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(HoverTipFactory), "Static", [typeof(StaticHoverTip), typeof(DynamicVar[])])];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(ref IHoverTip __result)
            // ReSharper restore once InconsistentNaming
        {
            if (!RitsuLibSettingsStore.IsModSourceHoverTipsEnabled() ||
                !RitsuLibSettingsStore.ShouldShowGameTermModSourceHoverTips())
                return;

            var info = ContentSourceHoverTipFactory.ContentSourceInfo.Vanilla;
            if (!ContentSourceHoverTipFactory.ShouldShow(info))
                return;

            if (__result is not HoverTip tip) return;
            ContentSourceHoverTipPatchHelper.Append(info, ref tip);
            __result = tip;
        }
    }

    internal sealed class ContentSourceEnergyHoverTipPatch : IPatchMethod
    {
        public static string PatchId => "content_source_energy_hover_tip";

        public static string Description =>
            "Add vanilla content source to energy hover tips when enabled";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(HoverTipFactory), "ForEnergyWithIconPath", [typeof(string)])];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(ref IHoverTip __result)
            // ReSharper restore once InconsistentNaming
        {
            if (!RitsuLibSettingsStore.IsModSourceHoverTipsEnabled() ||
                !RitsuLibSettingsStore.ShouldShowGameTermModSourceHoverTips())
                return;

            var info = ContentSourceHoverTipFactory.ContentSourceInfo.Vanilla;
            if (!ContentSourceHoverTipFactory.ShouldShow(info))
                return;

            if (__result is not HoverTip tip) return;
            ContentSourceHoverTipPatchHelper.Append(info, ref tip);
            __result = tip;
        }
    }

    internal sealed class ContentSourceModelHoverTipPatch : IPatchMethod
    {
        public static string PatchId => "content_source_model_hover_tip";

        public static string Description =>
            "Add content source hover tip to all models that have a source registered in ContentSourceHoverTipFactory";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PotionModel), "HoverTip", MethodType.Getter),
#if STS2_AT_LEAST_0_106_0
                new(typeof(PowerModel), "GetDumbHoverTip", [typeof(int?)]),
#else
                new(typeof(PowerModel), "DumbHoverTip", MethodType.Getter),
#endif
                new(typeof(RelicModel), "HoverTip", MethodType.Getter),
                new(typeof(OrbModel), "DumbHoverTip", MethodType.Getter),
                new(typeof(EnchantmentModel), "HoverTip", MethodType.Getter),
                new(typeof(AfflictionModel), "HoverTip", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(AbstractModel __instance, ref HoverTip __result)
            // ReSharper restore InconsistentNaming
        {
            if (!RitsuLibSettingsStore.IsModSourceHoverTipsEnabled())
                return;

            if (!ContentSourceHoverTipFactory.TryResolve(__instance, out var info))
                return;

            ContentSourceHoverTipPatchHelper.Append(info, ref __result);
        }
    }

    internal sealed class ContentSourceCardHoverTipsPatch : IPatchMethod
    {
        public static string PatchId => "content_source_card_hover_tips";

        public static string Description =>
            "Add content source hover tip to card hover tip collections outside inspect/detail screens when enabled";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "HoverTips", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
            // ReSharper restore InconsistentNaming
        {
            if (!RitsuLibSettingsStore.IsModSourceHoverTipsEnabled() ||
                !RitsuLibSettingsStore.ShouldIncludeNonDetailModSourceHoverTips())
                return;

            ContentSourceHoverTipPatchHelper.Append(__instance, ref __result);
        }
    }

    internal sealed class ContentSourcePowerHoverTipsPatch : IPatchMethod
    {
        public static string PatchId => "content_source_power_hover_tips";

        public static string Description =>
            "Add content source hover tip to power hover tip collections shown by combat power icons";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PowerModel), "HoverTips", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(PowerModel __instance, ref IEnumerable<IHoverTip> __result)
            // ReSharper restore InconsistentNaming
        {
            ContentSourceHoverTipPatchHelper.AppendToFirstHoverTip(__instance, ref __result);
        }
    }

    internal sealed class ContentSourceNHoverTipSetShowPatch : IPatchMethod
    {
        public static string PatchId => "nhover_tip_set_inspect_screen_show_source";

        public static string Description =>
            "Show content source in inspect screens, card preview hovers, and relic option hovers";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NHoverTipSet), "CreateAndShow",
                    [typeof(Control), typeof(IEnumerable<IHoverTip>), typeof(HoverTipAlignment)]),
            ];
        }

        private static readonly FieldInfo? CardsField = AccessTools.Field(typeof(NInspectCardScreen), "_cards");
        private static readonly FieldInfo? CardsIndexField = AccessTools.Field(typeof(NInspectCardScreen), "_index");
        private static readonly FieldInfo? RelicsField = AccessTools.Field(typeof(NInspectRelicScreen), "_relics");
        private static readonly FieldInfo? RelicsIndexField = AccessTools.Field(typeof(NInspectRelicScreen), "_index");

        private static void AppendModelSourceTipIfModelTipMissing(
            AbstractModel model,
            ref IEnumerable<IHoverTip> hoverTips)
        {
            var tips = hoverTips.ToArray();
            if (tips.Any(tip => tip.CanonicalModel?.GetType() == model.GetType() &&
                                tip.CanonicalModel.Id == model.Id))
            {
                hoverTips = tips;
                return;
            }

            if (!ContentSourceHoverTipFactory.TryCreate(model, out var sourceTip))
                return;

            hoverTips = [sourceTip, .. tips];
        }

        private static void AppendCardHoverTipSources(ref IEnumerable<IHoverTip> hoverTips)
        {
            var tips = hoverTips.ToArray();
            List<IHoverTip>? sourceTips = null;
            HashSet<string>? seenSourceTipIds = null;

            foreach (var cardTip in tips.OfType<CardHoverTip>())
            {
                if (!ContentSourceHoverTipFactory.TryCreate(cardTip.Card, out var sourceTip))
                    continue;

                seenSourceTipIds ??= [];
                if (!seenSourceTipIds.Add(sourceTip.Id))
                    continue;

                sourceTips ??= [];
                sourceTips.Add(sourceTip);
            }

            if (sourceTips is null)
                return;

            hoverTips = [.. sourceTips, .. tips];
        }

        private static void AppendTip<T>(FieldInfo? listField, FieldInfo? indexField, Control screen,
            ref IEnumerable<IHoverTip> hoverTips) where T : AbstractModel
        {
            if (listField == null || indexField == null)
                return;

            var index = (int)indexField.GetValue(screen)!;
            if (listField.GetValue(screen) is not IReadOnlyList<T> list || index < 0 || index >= list.Count)
                return;

            if (list[index] is not AbstractModel model)
                return;

            ContentSourceHoverTipPatchHelper.Append(model, ref hoverTips);
        }

        // ReSharper disable once InconsistentNaming
        public static void Prefix(Control owner, ref IEnumerable<IHoverTip> hoverTips)
            // ReSharper restore once InconsistentNaming
        {
            if (!RitsuLibSettingsStore.IsModSourceHoverTipsEnabled() ||
                !RitsuLibSettingsStore.ShouldShowCreatureModSourceHoverTips())
                return;

            switch (owner)
            {
                case NInspectCardScreen cardScreen:
                    AppendTip<CardModel>(CardsField, CardsIndexField, cardScreen, ref hoverTips);
                    break;
                case NInspectRelicScreen relicScreen:
                    AppendTip<RelicModel>(RelicsField, RelicsIndexField, relicScreen, ref hoverTips);
                    break;
            }

            if (!RitsuLibSettingsStore.ShouldIncludeNonDetailModSourceHoverTips())
                return;

            if (owner is NEventOptionButton { Option.Relic: { } relic })
                AppendModelSourceTipIfModelTipMissing(relic, ref hoverTips);

            AppendCardHoverTipSources(ref hoverTips);
        }
    }

    internal sealed class ContentSourceCreatureHoverTipsPatch : IPatchMethod
    {
        public static string PatchId => "content_source_creature_hover_tips";

        public static string Description =>
            "Add content source hover tip to enemy creature hover tips in combat when enabled";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Creature), "HoverTips", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(Creature __instance, ref IEnumerable<IHoverTip> __result)
            // ReSharper restore InconsistentNaming
        {
            if (!__instance.IsMonster)
                return;

            ContentSourceHoverTipPatchHelper.Append(__instance.Monster!, ref __result);
        }
    }

    internal sealed class ContentSourceEventLayoutBadgePatch : IPatchMethod
    {
        public static string PatchId => "content_source_event_layout_badge";

        public static string Description => "Show event and ancient source mod as a fixed lower-right label";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NEventLayout), nameof(NEventLayout.SetEvent), [typeof(EventModel)])];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(NEventLayout __instance, EventModel eventModel)
            // ReSharper restore InconsistentNaming
        {
            ContentSourceHoverTipPatchHelper.UpdateEventSourceBadge(__instance, eventModel);
        }
    }
}
