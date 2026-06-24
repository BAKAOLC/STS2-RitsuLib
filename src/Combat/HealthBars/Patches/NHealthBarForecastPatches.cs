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
    ///     Standalone forecast overlay logic for <see cref="NHealthBar" /> when BaseLib interop is unavailable or has not
    ///     taken over rendering.
    ///     当 BaseLib interop 不可用或尚未接管渲染时，提供 <see cref="NHealthBar" /> 的独立 forecast 覆盖逻辑。
    /// </summary>
    internal static class NHealthBarForecastPatchHelper
    {
        private static readonly AttachedState<NHealthBar, HealthBarForecastUiState?> UiStates = new(() => null);

        private static readonly Color DoomLethalTextColor = new("FB8DFF");
        private static readonly Color DoomLethalOutlineColor = new("2D1263");

        public static void RefreshForegroundOverlay(NHealthBar healthBar)
        {
            BaseLibHealthBarForecastBridge.TryRegisterSecondary();
            BaseLibVisualGraftBridge.TryRegisterSecondary();
            if (BaseLibHealthBarForecastBridge.ShouldRitsuRendererStandDown())
                return;

            var creature = healthBar._creature;
            if (creature.CurrentHp <= 0 || creature.IsInfiniteHpDisplayed())
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

            if (!EnsureUiState(healthBar))
                return;

            var state = UiStates[healthBar];
            if (state == null)
                return;

            EnsureOverlayOrder(healthBar, state);

            var graftAgg = HealthBarVisualGraftRegistry.Aggregate(creature);
            var visualDenom = Math.Max(creature.MaxHp, creature.CurrentHp + Math.Max(0, graftAgg.GraftHp));

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

            var remainingHp = baseHp + Math.Max(0, graftAgg.GraftHp);
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
        ///     Repositions the middleground without animation after the bar container was resized; vanilla snapped it to
        ///     the un-forecasted HP edge inside <c>SetHpBarContainerSizeWithOffsetsImmediately</c>.
        ///     在血条容器尺寸变化后无动画地复位 middleground；原版在
        ///     <c>SetHpBarContainerSizeWithOffsetsImmediately</c> 中把它对齐到了未应用 forecast 的 HP 边缘。
        /// </summary>
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
                leftAccumulated = Math.Min(remainingHp, leftAccumulated + segment.Amount);
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
            return HealthBarForecastRegistry.GetSegments(creature)
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
                    registered.Segment.AffectsHpLabel))
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
        ///     Applies segment material and <see cref="CanvasItem.SelfModulate" />; overlay uses
        ///     <paramref name="overlaySelfModulate" /> when set, otherwise <paramref name="color" />.
        ///     应用片段 material 和 <see cref="CanvasItem.SelfModulate" />；设置后覆盖层使用
        ///     <paramref name="overlaySelfModulate" />，否则使用 <paramref name="color" />。
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

            List<LethalCandidate> candidates = [];
            candidates.AddRange(from segment in leftSegments
                where segment is
                {
                    Amount: > 0, Direction: HealthBarForecastGrowthDirection.FromLeft,
                    LeftOriginLayout: HealthBarForecastLeftOriginLayout.Chained,
                }
                select new LethalCandidate(segment.Amount, segment.AffectsHpLabel ? segment.Color : null, segment.Order,
                    segment.SequenceOrder));

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
                accumulated = Math.Min(remainingHp, accumulated + candidate.Amount);
                if (accumulated >= remainingHp)
                    return candidate.Color;
            }

            return null;
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
        ///     Snapshot of one registry segment plus render order for layout and lethal text resolution.
        ///     一个注册表片段加渲染顺序的快照，用于布局和致命文本解析。
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
    ///     Runs visual graft, forecast overlay, then graft touchup in one deterministic order with a reentrancy guard:
    ///     the graft pass resizes the bar container, whose resize patch would otherwise re-enter this chain.
    ///     以确定顺序运行 visual graft、forecast 覆盖层和 graft touchup，并带重入守卫：
    ///     graft 阶段会调整血条容器尺寸，否则其 resize 补丁会重入此链。
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
