using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.HealthBars.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Renders RitsuLib and imported legacy BaseLib forecasts on <see cref="NHealthBar" /> while BaseLib's
    ///         current renderer has not taken ownership.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 BaseLib 当前版本的渲染器尚未接管时，于 <see cref="NHealthBar" /> 上渲染 RitsuLib 预测，
    ///         以及从旧版 BaseLib 导入的预测。
    ///     </para>
    /// </summary>
    internal static class NHealthBarForecastPatchHelper
    {
        private const long BaseLibImportedSequenceOrderOffset = 500_000_000L;
        private static readonly AttachedState<NHealthBar, HealthBarForecastUiState?> UiStates = new(() => null);

        private static readonly Color DoomLethalTextColor = new("FB8DFF");
        private static readonly Color DoomLethalOutlineColor = new("2D1263");

        public static void RefreshForegroundOverlay(NHealthBar healthBar)
        {
            BaseLibHealthBarForecastBridge.TryRegisterSecondary();
            BaseLibVisualGraftBridge.TryRegisterSecondary();
            if (BaseLibHealthBarForecastBridge.ShouldRitsuRendererStandDown())
            {
                HideAllCustomSegments(healthBar);
                return;
            }

            var suppressBaseLibRenderer = BaseLibHealthBarForecastBridge.ShouldSuppressBaseLibRenderer();
            var creature = healthBar._creature;
            if (creature.CurrentHp <= 0 || creature.IsInfiniteHpDisplayed())
            {
                if (suppressBaseLibRenderer)
                    HideBaseLibForecastContainers(healthBar);
                HideAllCustomSegments(healthBar);
                return;
            }

            var customSegments = GetCustomSegments(creature);
            if (customSegments.Length == 0)
            {
                if (suppressBaseLibRenderer)
                    HideBaseLibForecastContainers(healthBar);
                HideAllCustomSegments(healthBar);
                return;
            }

            if (!EnsureUiState(healthBar))
                return;

            var state = UiStates[healthBar];
            if (state == null)
                return;

            EnsureOverlayOrder(healthBar, state);

            var graftAgg = HealthBarVisualGraftRegistry.Aggregate(creature);
            var graftHp = Math.Max(0, graftAgg.GraftHp);
            var visualDenom = Math.Max(creature.MaxHp, SaturatingAddNonNegative(creature.CurrentHp, graftHp));

            var maxWidth = GetMaxFgWidth(healthBar);
            var hpForeground = healthBar._hpForeground;
            var poisonDamage = creature.HasPower<PoisonPower>()
                ? Math.Max(0, creature.GetPower<PoisonPower>()?.CalculateTotalDamageNextTurn() ?? 0)
                : 0;
            var baseHp = Math.Max(0, creature.CurrentHp - poisonDamage);

            var rightSegments = customSegments
                .Where(segment => segment.Direction == HealthBarForecastGrowthDirection.FromRight)
                .OrderBy(segment => segment.Order)
                .ThenBy(segment => segment.SequenceOrder)
                .ToArray();

            var remainingHp = SaturatingAddNonNegative(baseHp, graftHp);
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

                var leftWidth = GetFgWidth(healthBar, remainingHp, visualDenom);
                var rightWidth = GetFgWidth(healthBar, previousHp, visualDenom);
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
                    lethalRightColor = segment.AffectsHpLabel ? segment.Color : null;

                rightIndex++;
            }

            HideSegments(state.RightSegments, rightIndex);

            if (rightIndex > 0)
            {
                if (remainingHp > 0)
                {
                    hpForeground.Visible = true;
                    hpForeground.OffsetRight = GetFgWidth(healthBar, remainingHp, visualDenom) - maxWidth;
                }
                else
                {
                    hpForeground.Visible = false;
                }

                var doomForeground = healthBar._doomForeground;
                if (doomForeground.Visible)
                {
                    if (remainingHp > 0)
                        doomForeground.OffsetRight =
                            Math.Min(doomForeground.OffsetRight, hpForeground.OffsetRight);
                    else
                        doomForeground.Visible = false;
                }
            }

            if (remainingHp <= 0)
            {
                HideSegments(state.LeftSegments);
                state.LastRender = new(true, rightForecastEdgeOffsetRight, lethalRightColor, null, 0);
                if (suppressBaseLibRenderer)
                    HideBaseLibForecastContainers(healthBar);
                return;
            }

            var leftSegments = customSegments
                .Where(segment => segment.Direction == HealthBarForecastGrowthDirection.FromLeft)
                .OrderBy(segment => segment.Order)
                .ThenBy(segment => segment.SequenceOrder)
                .ToArray();

            state.OverlapLeftZ.Clear();
            var leftIndex = 0;
            var chainedLeft = leftSegments
                .Where(s => s.LeftOriginLayout == HealthBarForecastLeftOriginLayout.Chained)
                .ToArray();
            PlaceChainedLeftSegments(
                healthBar,
                state,
                chainedLeft,
                remainingHp,
                maxWidth,
                rightIndex,
                rightForecastEdgeOffsetRight,
                visualDenom,
                ref leftIndex);

            var overlapLeft = leftSegments
                .Where(s => s.LeftOriginLayout == HealthBarForecastLeftOriginLayout.OverlapFromOrigin)
                .ToArray();
            PlaceOverlapLeftSegments(
                healthBar,
                state,
                overlapLeft,
                remainingHp,
                maxWidth,
                rightIndex,
                rightForecastEdgeOffsetRight,
                visualDenom,
                ref leftIndex);

            HideSegments(state.LeftSegments, leftIndex);
            var lethalLeftColor = ResolveLeftLethalColor(creature, remainingHp, leftSegments, state.OverlapLeftZ);
            state.LastRender =
                new(rightIndex > 0, rightForecastEdgeOffsetRight, lethalRightColor, lethalLeftColor, remainingHp);

            if (suppressBaseLibRenderer)
                HideBaseLibForecastContainers(healthBar);
        }

        public static void RefreshMiddlegroundOverlay(NHealthBar healthBar)
        {
            if (BaseLibHealthBarForecastBridge.ShouldRitsuRendererStandDown())
                return;

            var state = UiStates[healthBar];
            if (state == null)
                return;

            if (!state.LastRender.HasRightForecast)
            {
                state.MiddlegroundTweenTarget = null;
                return;
            }

            var creature = healthBar._creature;
            if (creature.CurrentHp <= 0 || creature.IsInfiniteHpDisplayed())
                return;

            var hpMiddleground = healthBar._hpMiddleground;
            var targetOffsetRight = state.LastRender.RightForecastEdgeOffsetRight;
            var hpChanged = creature.CurrentHp != state.MiddlegroundHpOnLastTween ||
                            creature.MaxHp != state.MiddlegroundMaxHpOnLastTween;
            var targetChanged = state.MiddlegroundTweenTarget is not { } lastTarget ||
                                !Mathf.IsEqualApprox(lastTarget, targetOffsetRight);
            if (!hpChanged && !targetChanged)
                return;

            state.MiddlegroundHpOnLastTween = creature.CurrentHp;
            state.MiddlegroundMaxHpOnLastTween = creature.MaxHp;
            state.MiddlegroundTweenTarget = targetOffsetRight;

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

        /// <summary>
        ///     <para xml:lang="en">
        ///         Repositions the delayed-damage layer without animation after the bar container is resized. The base
        ///         method aligns it to the HP edge before custom forecasts are applied.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         生命条容器调整尺寸后，无动画地重新定位延迟扣血层。原版方法会先将其对齐到应用自定义预测前的
        ///         生命值边缘。
        ///     </para>
        /// </summary>
        /// <param name="healthBar">
        ///     <para xml:lang="en">The health-bar node to update.</para>
        ///     <para xml:lang="zh-CN">要更新的生命条节点。</para>
        /// </param>
        public static void SnapMiddlegroundToForecast(NHealthBar healthBar)
        {
            if (BaseLibHealthBarForecastBridge.ShouldRitsuRendererStandDown())
                return;

            var state = UiStates[healthBar];
            if (state == null || !state.LastRender.HasRightForecast)
                return;

            var creature = healthBar._creature;
            if (creature.CurrentHp <= 0 || creature.IsInfiniteHpDisplayed())
                return;

            var targetOffsetRight = state.LastRender.RightForecastEdgeOffsetRight;
            healthBar._middlegroundTween?.Kill();
            healthBar._hpMiddleground.OffsetRight = targetOffsetRight - 2f;
            state.MiddlegroundHpOnLastTween = creature.CurrentHp;
            state.MiddlegroundMaxHpOnLastTween = creature.MaxHp;
            state.MiddlegroundTweenTarget = targetOffsetRight;
        }

        public static void RefreshTextOverlay(NHealthBar healthBar)
        {
            if (BaseLibHealthBarForecastBridge.ShouldRitsuRendererStandDown())
                return;

            var state = UiStates[healthBar];
            if (state == null)
                return;

            var creature = healthBar._creature;
            if (creature.CurrentHp <= 0 || creature.IsInfiniteHpDisplayed())
                return;

            var lethalColor = state.LastRender.LethalRightColor ?? state.LastRender.LethalLeftColor;
            var hpLabel = healthBar._hpLabel;
            if (!lethalColor.HasValue)
            {
                if (!IsDoomLethalAfterRight(healthBar, creature))
                    return;
                hpLabel.AddThemeColorOverride(ThemeConstants.Label.FontColor, DoomLethalTextColor);
                hpLabel.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, DoomLethalOutlineColor);
                return;
            }

            hpLabel.AddThemeColorOverride(ThemeConstants.Label.FontColor, lethalColor.Value);
            hpLabel.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor,
                DarkenForOutline(lethalColor.Value));
        }

        private static void PlaceChainedLeftSegments(
            NHealthBar healthBar,
            HealthBarForecastUiState state,
            CustomSegment[] chainedOrdered,
            int remainingHp,
            float maxWidth,
            int rightIndex,
            float rightForecastEdgeOffsetRight,
            int visualDenom,
            ref int leftIndex)
        {
            var leftAccumulated = 0;
            foreach (var segment in chainedOrdered)
            {
                if (leftAccumulated >= remainingHp)
                    break;

                var segmentStart = leftAccumulated;
                leftAccumulated = Math.Min(remainingHp, SaturatingAddNonNegative(leftAccumulated, segment.Amount));
                if (leftAccumulated <= segmentStart)
                    continue;

                EnsureSegmentCount(state.LeftSegments, state.LeftContainer, leftIndex + 1, state.LeftTemplate);
                var node = state.LeftSegments[leftIndex];
                var startWidth = GetFgWidth(healthBar, segmentStart, visualDenom);
                var endWidth = GetFgWidth(healthBar, leftAccumulated, visualDenom);

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

                leftIndex++;
            }
        }

        private static void PlaceOverlapLeftSegments(
            NHealthBar healthBar,
            HealthBarForecastUiState state,
            CustomSegment[] overlapSegments,
            int remainingHp,
            float maxWidth,
            int rightIndex,
            float rightForecastEdgeOffsetRight,
            int visualDenom,
            ref int leftIndex)
        {
            if (overlapSegments.Length == 0)
                return;

            foreach (var grp in overlapSegments.GroupBy(s => s.LeftExclusiveZGroup).OrderBy(g => g.Key))
            {
                // Larger amounts draw first (bottom); equal amounts stack deterministically by Order then
                // registration order, later on top. No time-based reordering: the health bar only refreshes
                // on game events, so a timer could never animate reliably and would flip at random refreshes.
                var sorted = grp
                    .OrderByDescending(s => s.Amount)
                    .ThenBy(s => s.Order)
                    .ThenBy(s => s.SequenceOrder)
                    .ToArray();

                foreach (var segment in sorted)
                {
                    var visibleAmount = Math.Min(segment.Amount, remainingHp);
                    if (visibleAmount <= 0)
                        continue;

                    EnsureSegmentCount(state.LeftSegments, state.LeftContainer, leftIndex + 1, state.LeftTemplate);
                    var node = state.LeftSegments[leftIndex];
                    var endWidth = GetFgWidth(healthBar, visibleAmount, visualDenom);
                    state.OverlapLeftZ.Add((segment, leftIndex));

                    node.Visible = true;
                    ApplyForecastSegmentAppearance(
                        node,
                        segment.Color,
                        segment.OverlayMaterial,
                        segment.OverlaySelfModulate);
                    node.OffsetLeft = 0f;
                    var leftOffsetRight = Math.Min(0f, endWidth - maxWidth + node.PatchMarginRight);
                    if (rightIndex > 0)
                        leftOffsetRight = Math.Min(leftOffsetRight, rightForecastEdgeOffsetRight);
                    node.OffsetRight = leftOffsetRight;

                    leftIndex++;
                }
            }
        }

        private static CustomSegment[] GetCustomSegments(Creature creature)
        {
            var ritsuSegments = HealthBarForecastRegistry.GetSegments(creature)
                .Select(registered => new CustomSegment(
                    registered.Segment.Amount,
                    registered.Segment.Color,
                    registered.Segment.Direction,
                    registered.Segment.Order,
                    registered.SequenceOrder,
                    registered.Segment.OverlayMaterial,
                    registered.Segment.OverlaySelfModulate,
                    registered.Segment.LeftOriginLayout,
                    registered.Segment.LeftExclusiveZGroup,
                    registered.Segment.AffectsHpLabel));

            var baseLibSegments = BaseLibHealthBarForecastBridge.GetImportedSegments(creature)
                .Select(segment => new CustomSegment(
                    segment.Amount,
                    segment.Color,
                    segment.Direction,
                    segment.Order,
                    OffsetBaseLibSequenceOrder(segment.SequenceOrder),
                    segment.OverlayMaterial,
                    segment.OverlaySelfModulate,
                    segment.LeftOriginLayout,
                    segment.LeftExclusiveZGroup,
                    segment.AffectsHpLabel));

            return
            [
                .. ritsuSegments
                    .Concat(baseLibSegments)
                    .Where(segment => segment.Amount > 0),
            ];
        }

        private static void HideBaseLibForecastContainers(NHealthBar healthBar)
        {
            if (healthBar._poisonForeground?.GetParent() is not Control mask)
                return;

            HideBaseLibForecastContainer(mask.GetNodeOrNull<Control>("BaseLibForecastRightContainer"));
            HideBaseLibForecastContainer(mask.GetNodeOrNull<Control>("BaseLibForecastLeftContainer"));
        }

        private static void HideBaseLibForecastContainer(Control? container)
        {
            if (container == null)
                return;

            container.Visible = false;
        }

        private static void HideAllCustomSegments(NHealthBar healthBar)
        {
            var state = UiStates[healthBar];
            if (state == null)
                return;

            HideSegments(state.RightSegments);
            HideSegments(state.LeftSegments);
            state.OverlapLeftZ.Clear();
            state.LastRender = HealthBarForecastRenderResult.Empty;
        }

        private static bool EnsureUiState(NHealthBar healthBar)
        {
            if (UiStates[healthBar] != null)
                return true;

            if (healthBar._poisonForeground is not NinePatchRect poisonForeground)
                return false;

            if (healthBar._doomForeground is not NinePatchRect doomForeground)
                return false;

            if (poisonForeground.GetParent() is not Control mask)
                return false;

            var rightContainer = CreateContainer("RitsuForecastRightContainer");
            var leftContainer = CreateContainer("RitsuForecastLeftContainer");

            mask.AddChild(rightContainer);
            mask.AddChild(leftContainer);

            var rightTemplate = CreateSegmentTemplate(poisonForeground, "RitsuForecastRightTemplate");
            var leftTemplate = CreateSegmentTemplate(doomForeground, "RitsuForecastLeftTemplate");
            rightContainer.AddChild(rightTemplate);
            leftContainer.AddChild(leftTemplate);

            UiStates[healthBar] = new(
                rightContainer,
                leftContainer,
                rightTemplate,
                leftTemplate,
                []);
            return true;
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
            duplicate.Modulate = Colors.White;
            duplicate.SelfModulate = Colors.White;
            duplicate.Material = null;
            duplicate.ZIndex = 0;
            duplicate.MouseFilter = Control.MouseFilterEnum.Ignore;
            duplicate.OffsetLeft = 0f;
            duplicate.OffsetRight = 0f;
            return duplicate;
        }

        private static void EnsureOverlayOrder(NHealthBar healthBar, HealthBarForecastUiState state)
        {
            if (healthBar._poisonForeground is not { } poisonForeground ||
                healthBar._hpForeground is not { } hpForeground ||
                healthBar._doomForeground is not { } doomForeground ||
                poisonForeground.GetParent() is not Control mask)
                return;

            if (poisonForeground.GetIndex() < hpForeground.GetIndex())
                MoveChildAfter(mask, state.RightContainer, poisonForeground);
            else
                MoveChildBefore(mask, state.RightContainer, hpForeground);

            MoveChildBefore(mask, state.LeftContainer, doomForeground);
        }

        private static void MoveChildAfter(Control parent, Control node, Control anchor)
        {
            if (node.GetParent() != parent || anchor.GetParent() != parent)
                return;

            var nodeIndex = node.GetIndex();
            var anchorIndex = anchor.GetIndex();
            var targetIndex = nodeIndex > anchorIndex ? anchorIndex + 1 : anchorIndex;
            if (nodeIndex != targetIndex)
                parent.MoveChild(node, targetIndex);
        }

        private static void MoveChildBefore(Control parent, Control node, Control anchor)
        {
            if (node.GetParent() != parent || anchor.GetParent() != parent)
                return;

            var nodeIndex = node.GetIndex();
            var anchorIndex = anchor.GetIndex();
            var targetIndex = nodeIndex > anchorIndex ? anchorIndex : Math.Max(0, anchorIndex - 1);
            if (nodeIndex != targetIndex)
                parent.MoveChild(node, targetIndex);
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
                segment.ZIndex = 0;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies a segment's material and modulation color. The explicit
        ///         <paramref name="overlaySelfModulate" /> takes precedence over <paramref name="color" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         应用片段的材质和调制色。显式指定的 <paramref name="overlaySelfModulate" /> 优先于
        ///         <paramref name="color" />。
        ///     </para>
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

        private static float GetFgWidth(NHealthBar healthBar, int amount, int visualDenom)
        {
            var creature = healthBar._creature;
            if (visualDenom <= 0 || amount <= 0)
                return 0f;

            var width = (float)amount / visualDenom * GetMaxFgWidth(healthBar);
            return Math.Max(width, creature.CurrentHp > 0 ? 12f : 0f);
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

            var state = UiStates[healthBar];
            if (state == null || !state.LastRender.HasRightForecast)
                return false;

            var remainingHp = state.LastRender.RemainingHpAfterRight;
            return remainingHp > 0 && doomAmount >= remainingHp;
        }

        private static Color? ResolveLeftLethalColor(
            Creature creature,
            int remainingHp,
            IReadOnlyList<CustomSegment> leftSegments,
            List<(CustomSegment seg, int drawIndex)> overlapZ)
        {
            if (remainingHp <= 0)
                return null;

            Color? overlapLethal = null;
            var hasOverlapLethal = false;
            var bestDrawIndex = int.MinValue;
            foreach (var (seg, drawIndex) in overlapZ)
            {
                if (seg.Amount < remainingHp)
                    continue;
                if (drawIndex < bestDrawIndex)
                    continue;
                bestDrawIndex = drawIndex;
                hasOverlapLethal = true;
                overlapLethal = seg.AffectsHpLabel ? seg.Color : null;
            }

            if (hasOverlapLethal)
                return overlapLethal;

            List<LethalCandidate> candidates =
            [
                .. from segment in leftSegments
                where segment is
                {
                    Amount: > 0, Direction: HealthBarForecastGrowthDirection.FromLeft,
                    LeftOriginLayout: HealthBarForecastLeftOriginLayout.Chained,
                }
                select new LethalCandidate(segment.Amount, segment.AffectsHpLabel ? segment.Color : null, segment.Order,
                    segment.SequenceOrder),
            ];

            var doomAmount = creature.GetPowerAmount<DoomPower>();
            if (doomAmount > 0)
                candidates.Add(new(doomAmount, DoomLethalTextColor, 0, long.MinValue / 4));

            if (candidates.Count == 0)
                return null;

            var ordered = candidates
                .OrderBy(candidate => candidate.Order)
                .ThenBy(candidate => candidate.SequenceOrder);

            var accumulated = 0;
            foreach (var candidate in ordered)
            {
                accumulated = Math.Min(remainingHp, SaturatingAddNonNegative(accumulated, candidate.Amount));
                if (accumulated >= remainingHp)
                    return candidate.Color;
            }

            return null;
        }

        private static int SaturatingAddNonNegative(int left, int right)
        {
            return (int)Math.Min(int.MaxValue, (long)Math.Max(0, left) + Math.Max(0, right));
        }

        private static long OffsetBaseLibSequenceOrder(long sequenceOrder)
        {
            return sequenceOrder > long.MaxValue - BaseLibImportedSequenceOrderOffset
                ? long.MaxValue
                : sequenceOrder + BaseLibImportedSequenceOrderOffset;
        }

        private readonly record struct LethalCandidate(
            int Amount,
            Color? Color,
            int Order,
            long SequenceOrder);

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
            public List<(CustomSegment seg, int drawIndex)> OverlapLeftZ { get; } = [];
            public HealthBarForecastRenderResult LastRender { get; set; } = HealthBarForecastRenderResult.Empty;
            public float? MiddlegroundTweenTarget { get; set; }
            public int MiddlegroundHpOnLastTween { get; set; } = -1;
            public int MiddlegroundMaxHpOnLastTween { get; set; } = -1;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Stores one registry segment and its stable render order for layout and lethal-label resolution.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         存储一个注册表片段及其稳定渲染顺序，用于布局和致命标签判定。
        ///     </para>
        /// </summary>
        private readonly record struct CustomSegment(
            int Amount,
            Color Color,
            HealthBarForecastGrowthDirection Direction,
            int Order,
            long SequenceOrder,
            Material? OverlayMaterial,
            Color? OverlaySelfModulate,
            HealthBarForecastLeftOriginLayout LeftOriginLayout,
            int LeftExclusiveZGroup,
            bool AffectsHpLabel);

        private readonly record struct HealthBarForecastRenderResult(
            bool HasRightForecast,
            float RightForecastEdgeOffsetRight,
            Color? LethalRightColor,
            Color? LethalLeftColor,
            int RemainingHpAfterRight)
        {
            public static HealthBarForecastRenderResult Empty => new(false, 0f, null, null, 0);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Runs visual extension, forecast, and post-forecast layout in a deterministic order. A reentrancy guard
    ///         prevents the extension's container resize from entering the refresh chain again.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         以确定顺序执行视觉扩展、预测和预测后的布局调整。重入守卫会阻止视觉扩展调整容器尺寸时再次进入
    ///         刷新调用链。
    ///     </para>
    /// </summary>
    internal static class NHealthBarOverlayRefreshChain
    {
        [ThreadStatic] private static bool _isRunning;

        public static bool Run(NHealthBar healthBar)
        {
            if (_isRunning)
                return false;

            _isRunning = true;
            try
            {
                NHealthBarGraftUiPatchHelper.RefreshGraftOverlay(healthBar);
                NHealthBarForecastPatchHelper.RefreshForegroundOverlay(healthBar);
                NHealthBarGraftUiPatchHelper.AfterForecastTouchup(healthBar);
                return true;
            }
            finally
            {
                _isRunning = false;
            }
        }
    }

    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal sealed class NHealthBarReadyForecastPatch : IPatchMethod
    {
        public static string PatchId => "health_bar_forecast_ready";
        public static string Description => "Health bar forecast overlay bootstrap";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHealthBar), "_Ready")];
        }

        public static void Postfix(NHealthBar __instance)
        {
            BaseLibHealthBarForecastBridge.TryRegisterPrimary();
            BaseLibVisualGraftBridge.TryRegisterPrimary();
        }
    }

    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal sealed class NHealthBarRefreshForegroundOrderedPatch : IPatchMethod
    {
        public static string PatchId => "health_bar_refresh_foreground_ordered";

        public static string Description =>
            "Run visual graft, forecast overlay, then graft touchup in a single deterministic order";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHealthBar), "RefreshForeground")];
        }

        public static void Postfix(NHealthBar __instance)
        {
            NHealthBarOverlayRefreshChain.Run(__instance);
        }
    }

    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal sealed class NHealthBarContainerResizeForecastPatch : IPatchMethod
    {
        public static string PatchId => "health_bar_forecast_container_resize";

        public static string Description =>
            "Re-run health bar overlays after the bar container is resized so segment offsets track the new width";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHealthBar), "SetHpBarContainerSizeWithOffsetsImmediately", true)];
        }

        public static void Postfix(NHealthBar __instance)
        {
            if (__instance._creature == null)
                return;

            if (!NHealthBarOverlayRefreshChain.Run(__instance))
                return;

            NHealthBarForecastPatchHelper.SnapMiddlegroundToForecast(__instance);
        }
    }

    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal sealed class NHealthBarRefreshMiddlegroundForecastPatch : IPatchMethod
    {
        public static string PatchId => "health_bar_forecast_refresh_middleground";
        public static string Description => "Animate middleground for custom right-side forecasts";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHealthBar), "RefreshMiddleground")];
        }

        public static void Postfix(NHealthBar __instance)
        {
            NHealthBarForecastPatchHelper.RefreshMiddlegroundOverlay(__instance);
        }
    }

    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal sealed class NHealthBarRefreshTextForecastPatch : IPatchMethod
    {
        public static string PatchId => "health_bar_forecast_refresh_text";
        public static string Description => "Tint health bar text for custom lethal forecasts";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHealthBar), "RefreshText")];
        }

        public static void Postfix(NHealthBar __instance)
        {
            NHealthBarForecastPatchHelper.RefreshTextOverlay(__instance);
        }
    }
}
