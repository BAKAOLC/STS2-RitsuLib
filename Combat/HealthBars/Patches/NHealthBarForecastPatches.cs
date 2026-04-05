using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.HealthBars.Patches
{
    /// <summary>
    ///     Standalone forecast overlay logic for <see cref="NHealthBar" /> when BaseLib interop is unavailable or has not
    ///     taken over rendering.
    /// </summary>
    internal static class NHealthBarForecastPatchHelper
    {
        // ReSharper disable InconsistentNaming

        private static readonly AttachedState<NHealthBar, HealthBarForecastUiState?> UiStates = new(() => null);

        private static readonly Color DoomLethalTextColor = new("FB8DFF");
        private static readonly Color DoomLethalOutlineColor = new("2D1263");

        public static void RefreshForegroundOverlay(NHealthBar healthBar)
        {
            BaseLibHealthBarForecastBridge.TryRegisterSecondary();
            if (BaseLibHealthBarForecastBridge.ShouldRitsuRendererStandDown())
                return;

            var creature = healthBar._creature;
            if (creature.CurrentHp <= 0 || creature.ShowsInfiniteHp)
            {
                HideAllCustomSegments(healthBar);
                return;
            }

            var customSegments = GetCustomSegments(creature);
            if (customSegments.Length == 0)
            {
                HideAllCustomSegments(healthBar);
                return;
            }

            EnsureUiState(healthBar);
            var state = UiStates[healthBar] ??
                        throw new InvalidOperationException("Missing health bar forecast UI state.");
            EnsureOverlayOrder(healthBar, state);

            var maxWidth = GetMaxFgWidth(healthBar);
            var hpForeground = healthBar._hpForeground;
            var baseHp = Math.Clamp(HpFromOffsetRight(healthBar, hpForeground.OffsetRight), 0, creature.CurrentHp);

            var rightSegments = customSegments
                .Where(segment => segment.Direction == HealthBarForecastGrowthDirection.FromRight)
                .OrderBy(segment => segment.Order)
                .ThenBy(segment => segment.SequenceOrder)
                .ToArray();

            var remainingHp = baseHp;
            var rightForecastEdgeOffsetRight = hpForeground.OffsetRight;
            Color? lethalRightColor = null;
            var rightIndex = 0;

            foreach (var segment in rightSegments)
            {
                if (remainingHp <= 0)
                    break;

                var visibleAmount = Math.Min(segment.Amount, remainingHp);
                if (visibleAmount <= 0)
                    continue;

                EnsureSegmentCount(state.RightSegments, state.RightContainer, rightIndex + 1, state.RightTemplate);
                var node = state.RightSegments[rightIndex];
                var previousHp = remainingHp;
                remainingHp -= visibleAmount;

                var leftWidth = GetFgWidth(healthBar, remainingHp);
                var rightWidth = GetFgWidth(healthBar, previousHp);
                node.Visible = true;
                ApplyForecastSegmentAppearance(
                    node,
                    segment.Color,
                    segment.OverlayMaterial,
                    segment.OverlaySelfModulate);
                node.OffsetLeft = remainingHp > 0 ? Math.Max(0f, leftWidth - node.PatchMarginLeft) : 0f;
                node.OffsetRight = rightWidth - maxWidth;

                if (rightIndex == 0)
                    rightForecastEdgeOffsetRight = node.OffsetRight;

                if (remainingHp <= 0)
                    lethalRightColor = segment.Color;

                rightIndex++;
            }

            HideSegments(state.RightSegments, rightIndex);

            if (rightIndex > 0)
            {
                if (remainingHp > 0)
                {
                    hpForeground.Visible = true;
                    hpForeground.OffsetRight = GetFgWidth(healthBar, remainingHp) - maxWidth;
                }
                else
                {
                    hpForeground.Visible = false;
                }

                var doomForeground = healthBar._doomForeground;
                if (doomForeground.Visible)
                {
                    if (remainingHp > 0)
                        doomForeground.OffsetRight = Math.Min(doomForeground.OffsetRight, hpForeground.OffsetRight);
                    else
                        doomForeground.Visible = false;
                }
            }

            if (remainingHp <= 0)
            {
                HideSegments(state.LeftSegments);
                state.LastRender = new(true, rightForecastEdgeOffsetRight, lethalRightColor, null);
                return;
            }

            var leftSegments = customSegments
                .Where(segment => segment.Direction == HealthBarForecastGrowthDirection.FromLeft)
                .OrderBy(segment => segment.Order)
                .ThenBy(segment => segment.SequenceOrder)
                .ToArray();

            var leftAccumulated = 0;
            Color? lethalLeftColor = null;
            var leftIndex = 0;

            foreach (var segment in leftSegments)
            {
                if (leftAccumulated >= remainingHp)
                    break;

                var segmentStart = leftAccumulated;
                leftAccumulated = Math.Min(remainingHp, leftAccumulated + segment.Amount);
                if (leftAccumulated <= segmentStart)
                    continue;

                EnsureSegmentCount(state.LeftSegments, state.LeftContainer, leftIndex + 1, state.LeftTemplate);
                var node = state.LeftSegments[leftIndex];
                var startWidth = GetFgWidth(healthBar, segmentStart);
                var endWidth = GetFgWidth(healthBar, leftAccumulated);

                node.Visible = true;
                ApplyForecastSegmentAppearance(
                    node,
                    segment.Color,
                    segment.OverlayMaterial,
                    segment.OverlaySelfModulate);
                node.OffsetLeft = segmentStart > 0 ? Math.Max(0f, startWidth - node.PatchMarginLeft) : 0f;
                var leftOffsetRight = Math.Min(0f, endWidth - maxWidth + node.PatchMarginRight);
                if (rightIndex > 0)
                    leftOffsetRight = Math.Min(leftOffsetRight, rightForecastEdgeOffsetRight);
                node.OffsetRight = leftOffsetRight;

                if (lethalLeftColor == null && leftAccumulated >= remainingHp)
                    lethalLeftColor = segment.Color;

                leftIndex++;
            }

            HideSegments(state.LeftSegments, leftIndex);
            state.LastRender = new(rightIndex > 0, rightForecastEdgeOffsetRight, lethalRightColor, lethalLeftColor);
        }

        public static void RefreshMiddlegroundOverlay(NHealthBar healthBar)
        {
            if (BaseLibHealthBarForecastBridge.ShouldRitsuRendererStandDown())
                return;

            var state = UiStates[healthBar];
            if (state == null || !state.LastRender.HasRightForecast)
                return;

            var creature = healthBar._creature;
            if (creature.CurrentHp <= 0 || creature.ShowsInfiniteHp)
                return;

            var hpMiddleground = healthBar._hpMiddleground;
            var targetOffsetRight = state.LastRender.RightForecastEdgeOffsetRight;
            var shouldAnimateImmediately = targetOffsetRight >= hpMiddleground.OffsetRight;
            hpMiddleground.OffsetRight += 1f;

            healthBar._middlegroundTween?.Kill();
            var tween = healthBar.CreateTween();
            tween.TweenProperty(hpMiddleground, "offset_right", targetOffsetRight - 2f, 1.0)
                .SetDelay(shouldAnimateImmediately ? 0.0 : 1.0)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
            healthBar._middlegroundTween = tween;
        }

        public static void RefreshTextOverlay(NHealthBar healthBar)
        {
            if (BaseLibHealthBarForecastBridge.ShouldRitsuRendererStandDown())
                return;

            var state = UiStates[healthBar];
            if (state == null)
                return;

            var creature = healthBar._creature;
            if (creature.CurrentHp <= 0 || creature.ShowsInfiniteHp)
                return;

            var lethalColor = state.LastRender.LethalRightColor ?? state.LastRender.LethalLeftColor;
            var hpLabel = healthBar._hpLabel;
            if (!lethalColor.HasValue)
            {
                if (!IsDoomLethalAfterRight(healthBar, creature))
                    return;
                hpLabel.AddThemeColorOverride("font_color", DoomLethalTextColor);
                hpLabel.AddThemeColorOverride("font_outline_color", DoomLethalOutlineColor);
                return;
            }

            hpLabel.AddThemeColorOverride("font_color", lethalColor.Value);
            hpLabel.AddThemeColorOverride("font_outline_color", DarkenForOutline(lethalColor.Value));
        }

        private static CustomSegment[] GetCustomSegments(Creature creature)
        {
            return HealthBarForecastRegistry.GetSegments(creature)
                .Select(registered => new CustomSegment(
                    registered.Segment.Amount,
                    registered.Segment.Color,
                    registered.Segment.Direction,
                    registered.Segment.Order,
                    registered.SequenceOrder,
                    registered.Segment.OverlayMaterial,
                    registered.Segment.OverlaySelfModulate))
                .Where(segment => segment.Amount > 0)
                .ToArray();
        }

        private static void HideAllCustomSegments(NHealthBar healthBar)
        {
            var state = UiStates[healthBar];
            if (state == null)
                return;

            HideSegments(state.RightSegments);
            HideSegments(state.LeftSegments);
            state.LastRender = HealthBarForecastRenderResult.Empty;
        }

        private static void EnsureUiState(NHealthBar healthBar)
        {
            if (UiStates[healthBar] != null)
                return;

            var poisonForeground = (NinePatchRect)healthBar._poisonForeground;
            var doomForeground = (NinePatchRect)healthBar._doomForeground;
            var mask = poisonForeground.GetParent<Control>();
            var rightContainer = CreateContainer("RitsuForecastRightContainer");
            var leftContainer = CreateContainer("RitsuForecastLeftContainer");

            mask.AddChild(rightContainer);
            mask.AddChild(leftContainer);

            UiStates[healthBar] = new(
                rightContainer,
                leftContainer,
                CreateSegmentTemplate(poisonForeground, "RitsuForecastRightTemplate"),
                CreateSegmentTemplate(doomForeground, "RitsuForecastLeftTemplate"),
                []);
        }

        private static Control CreateContainer(string name)
        {
            var container = new Control
            {
                Name = name,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };

            container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return container;
        }

        private static NinePatchRect CreateSegmentTemplate(NinePatchRect template, string name)
        {
            var duplicate = (NinePatchRect)template.Duplicate();
            duplicate.Name = name;
            duplicate.Visible = false;
            duplicate.SelfModulate = Colors.White;
            duplicate.Material = null;
            return duplicate;
        }

        private static void EnsureOverlayOrder(NHealthBar healthBar, HealthBarForecastUiState state)
        {
            if (healthBar._poisonForeground is not { } poisonForeground ||
                healthBar._hpForeground is not { } hpForeground ||
                healthBar._doomForeground is not { } doomForeground ||
                poisonForeground.GetParent() is not Control mask)
                return;

            var poisonIndex = poisonForeground.GetIndex();
            var hpIndex = hpForeground.GetIndex();
            var rightTargetIndex = Math.Clamp(poisonIndex + 1, 0, hpIndex);
            mask.MoveChild(state.RightContainer, rightTargetIndex);

            var doomIndex = doomForeground.GetIndex();
            var childCount = mask.GetChildCount();
            var leftTargetIndex = Math.Clamp(doomIndex + 1, 0, Math.Max(0, childCount - 1));
            mask.MoveChild(state.LeftContainer, leftTargetIndex);
        }

        private static void EnsureSegmentCount(
            List<NinePatchRect> segments,
            Control container,
            int requiredCount,
            NinePatchRect template)
        {
            while (segments.Count < requiredCount)
            {
                var segment = (NinePatchRect)template.Duplicate();
                segment.Name = $"RitsuForecastSegment{segments.Count}";
                segment.Visible = false;
                container.AddChild(segment);
                segments.Add(segment);
            }
        }

        private static void HideSegments(IEnumerable<NinePatchRect> segments, int startIndex = 0)
        {
            var index = 0;
            foreach (var segment in segments)
            {
                if (index++ < startIndex)
                    continue;

                segment.Visible = false;
                segment.Material = null;
                segment.SelfModulate = Colors.White;
            }
        }

        /// <summary>
        ///     Applies segment material and <see cref="CanvasItem.SelfModulate" />; overlay uses
        ///     <paramref name="overlaySelfModulate" /> when set, otherwise <paramref name="color" />.
        /// </summary>
        private static void ApplyForecastSegmentAppearance(
            NinePatchRect node,
            Color color,
            Material? overlayMaterial,
            Color? overlaySelfModulate)
        {
            node.Material = overlayMaterial;
            node.SelfModulate = overlaySelfModulate ?? color;
        }

        private static float GetMaxFgWidth(NHealthBar healthBar)
        {
            var expectedMaxFgWidth = healthBar._expectedMaxFgWidth;
            return expectedMaxFgWidth > 0f
                ? expectedMaxFgWidth
                : healthBar._hpForegroundContainer.Size.X;
        }

        private static float GetFgWidth(NHealthBar healthBar, int amount)
        {
            var creature = healthBar._creature;
            if (creature.MaxHp <= 0 || amount <= 0)
                return 0f;

            var width = (float)amount / creature.MaxHp * GetMaxFgWidth(healthBar);
            return Math.Max(width, creature.CurrentHp > 0 ? 12f : 0f);
        }

        private static int HpFromOffsetRight(NHealthBar healthBar, float offsetRight)
        {
            var creature = healthBar._creature;
            if (creature.MaxHp <= 0)
                return 0;

            var maxWidth = GetMaxFgWidth(healthBar);
            if (maxWidth <= 0f)
                return 0;

            var width = Math.Clamp(offsetRight + maxWidth, 0f, maxWidth);
            return (int)Math.Round(width / maxWidth * creature.MaxHp);
        }

        private static Color DarkenForOutline(Color color)
        {
            return new(
                Math.Clamp(color.R * 0.3f, 0f, 1f),
                Math.Clamp(color.G * 0.3f, 0f, 1f),
                Math.Clamp(color.B * 0.3f, 0f, 1f));
        }

        private static bool IsDoomLethalAfterRight(NHealthBar healthBar, Creature creature)
        {
            var doomAmount = creature.GetPowerAmount<DoomPower>();
            if (doomAmount <= 0)
                return false;

            var hpAfterRight = Math.Clamp(
                HpFromOffsetRight(healthBar, healthBar._hpForeground.OffsetRight),
                0,
                creature.CurrentHp);
            return hpAfterRight > 0 && doomAmount >= hpAfterRight;
        }

        private sealed class HealthBarForecastUiState(
            Control rightContainer,
            Control leftContainer,
            NinePatchRect rightTemplate,
            NinePatchRect leftTemplate,
            List<NinePatchRect> rightSegments)
        {
            public Control RightContainer { get; } = rightContainer;
            public Control LeftContainer { get; } = leftContainer;
            public NinePatchRect RightTemplate { get; } = rightTemplate;
            public NinePatchRect LeftTemplate { get; } = leftTemplate;
            public List<NinePatchRect> RightSegments { get; } = rightSegments;
            public List<NinePatchRect> LeftSegments { get; } = [];
            public HealthBarForecastRenderResult LastRender { get; set; } = HealthBarForecastRenderResult.Empty;
        }

        /// <summary>
        ///     Snapshot of one registry segment plus render order for layout and lethal text resolution.
        /// </summary>
        private readonly record struct CustomSegment(
            int Amount,
            Color Color,
            HealthBarForecastGrowthDirection Direction,
            int Order,
            long SequenceOrder,
            Material? OverlayMaterial,
            Color? OverlaySelfModulate);

        private readonly record struct HealthBarForecastRenderResult(
            bool HasRightForecast,
            float RightForecastEdgeOffsetRight,
            Color? LethalRightColor,
            Color? LethalLeftColor)
        {
            public static HealthBarForecastRenderResult Empty => new(false, 0f, null, null);
        }

        // ReSharper restore InconsistentNaming
    }

    internal sealed class NHealthBarReadyForecastPatch : IPatchMethod
    {
        public static string PatchId => "health_bar_forecast_ready";
        public static string Description => "Health bar forecast overlay bootstrap";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHealthBar), "_Ready")];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NHealthBar __instance)
        {
            BaseLibHealthBarForecastBridge.TryRegisterPrimary();
        }
    }

    internal sealed class NHealthBarRefreshForegroundForecastPatch : IPatchMethod
    {
        public static string PatchId => "health_bar_forecast_refresh_foreground";
        public static string Description => "Render custom forecast overlays after vanilla foreground";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHealthBar), "RefreshForeground")];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NHealthBar __instance)
        {
            NHealthBarForecastPatchHelper.RefreshForegroundOverlay(__instance);
        }
    }

    internal sealed class NHealthBarRefreshMiddlegroundForecastPatch : IPatchMethod
    {
        public static string PatchId => "health_bar_forecast_refresh_middleground";
        public static string Description => "Animate middleground for custom right-side forecasts";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHealthBar), "RefreshMiddleground")];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NHealthBar __instance)
        {
            NHealthBarForecastPatchHelper.RefreshMiddlegroundOverlay(__instance);
        }
    }

    internal sealed class NHealthBarRefreshTextForecastPatch : IPatchMethod
    {
        public static string PatchId => "health_bar_forecast_refresh_text";
        public static string Description => "Tint health bar text for custom lethal forecasts";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHealthBar), "RefreshText")];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NHealthBar __instance)
        {
            NHealthBarForecastPatchHelper.RefreshTextOverlay(__instance);
        }
    }
}
