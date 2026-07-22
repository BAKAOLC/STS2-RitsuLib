using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Context passed to card-description contributors.
    ///     传给卡牌描述贡献者的上下文。
    /// </summary>
    public readonly record struct CardDescriptionContext(
        CardModel Card,
        PileType PileType,
        Creature? Target,
        bool IsUpgradePreview);

    /// <summary>
    ///     Placement for capability-provided card description fragments.
    ///     能力提供的卡牌描述片段插入位置。
    /// </summary>
    public enum CardDescriptionFragmentPlacement
    {
        /// <summary>
        ///     Insert before the card's own description.
        ///     插入到卡牌自身描述之前。
        /// </summary>
        BeforeBase,

        /// <summary>
        ///     Insert after the card's own description.
        ///     插入到卡牌自身描述之后。
        /// </summary>
        AfterBase,
    }

    /// <summary>
    ///     Localized card description fragment contributed by a capability.
    ///     由能力贡献的本地化卡牌描述片段。
    /// </summary>
    public readonly record struct CardDescriptionFragment(
        LocString Text,
        CardDescriptionFragmentPlacement Placement = CardDescriptionFragmentPlacement.AfterBase,
        int Order = 0);

    /// <summary>
    ///     Optional model capability that contributes localized card-description fragments.
    ///     可选能力：贡献本地化卡牌描述片段。
    /// </summary>
    public interface ICardDescriptionContributor
    {
        /// <summary>
        ///     Returns extra localized description fragments. Fragments are formatted through the game
        ///     <see cref="LocString" /> pipeline with the owning card's dynamic vars.
        ///     返回额外的本地化描述片段。片段会带所属卡牌动态变量进入游戏原生 <see cref="LocString" /> 格式化管线。
        /// </summary>
        IEnumerable<CardDescriptionFragment> GetDescriptionFragments(CardDescriptionContext context);
    }

    /// <summary>
    ///     Optional model capability that contributes card hover tips.
    ///     可选能力：贡献卡牌悬停提示。
    /// </summary>
    public interface ICardHoverTipContributor
    {
        /// <summary>
        ///     Returns additional hover tips for <paramref name="card" />.
        ///     返回 <paramref name="card" /> 的额外悬停提示。
        /// </summary>
        IEnumerable<IHoverTip> GetHoverTips(CardModel card);
    }

    /// <summary>
    ///     Runtime context for capability-provided card overlays.
    ///     能力提供卡牌覆盖层时的运行时上下文。
    /// </summary>
    public readonly record struct CardOverlayContext(
        CardModel Card,
        NCard CardNode,
        Node OverlayContainer);

    /// <summary>
    ///     One visual overlay contributed by a card capability. Exactly one creation source should be set:
    ///     <see cref="ScenePath" />, <see cref="Scene" />, or <see cref="Factory" />.
    ///     卡牌能力贡献的一个可视覆盖层。应且仅应设置一个创建来源：
    ///     <see cref="ScenePath" />、<see cref="Scene" /> 或 <see cref="Factory" />。
    /// </summary>
    public sealed record CardOverlayContribution
    {
        /// <summary>
        ///     Stable id for diagnostics and ordering within the contributing capability.
        ///     用于诊断以及贡献能力内部排序的稳定 ID。
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        ///     Draw order among capability overlays. Larger values are added later and render above smaller values.
        ///     能力覆盖层之间的绘制顺序。数值越大越晚加入，因此显示在更上层。
        /// </summary>
        public int Order { get; init; }

        /// <summary>
        ///     When true, RitsuLib applies a full-rect layout preset after the overlay node is created.
        ///     为 true 时，RitsuLib 会在覆盖层节点创建后应用 full-rect 布局预设。
        /// </summary>
        public bool FullRect { get; init; } = true;

        /// <summary>
        ///     Packed scene path loaded through the game preload cache, matching vanilla card overlay creation.
        ///     通过游戏 preload cache 加载的 PackedScene 路径，与原版卡牌覆盖层创建行为一致。
        /// </summary>
        public string? ScenePath { get; init; }

        /// <summary>
        ///     Already resolved packed scene.
        ///     已解析的 PackedScene。
        /// </summary>
        public PackedScene? Scene { get; init; }

        /// <summary>
        ///     Factory for creating a fresh overlay control. It must return a new node each time because one
        ///     <see cref="CardModel" /> can be displayed by multiple <see cref="NCard" /> nodes.
        ///     用于创建全新覆盖层 Control 的工厂。每次必须返回新的节点，因为同一个
        ///     <see cref="CardModel" /> 可能同时由多个 <see cref="NCard" /> 节点显示。
        /// </summary>
        public Func<CardOverlayContext, Control?>? Factory { get; init; }

        /// <summary>
        ///     Creates a contribution from a packed scene path.
        ///     从 PackedScene 路径创建贡献项。
        /// </summary>
        public static CardOverlayContribution FromScenePath(
            string id,
            string scenePath,
            int order = 0,
            bool fullRect = true)
        {
            return new()
            {
                Id = id,
                ScenePath = scenePath,
                Order = order,
                FullRect = fullRect,
            };
        }

        /// <summary>
        ///     Creates a contribution from an already resolved packed scene.
        ///     从已解析的 PackedScene 创建贡献项。
        /// </summary>
        public static CardOverlayContribution FromScene(
            string id,
            PackedScene scene,
            int order = 0,
            bool fullRect = true)
        {
            return new()
            {
                Id = id,
                Scene = scene,
                Order = order,
                FullRect = fullRect,
            };
        }

        /// <summary>
        ///     Creates a contribution from a control factory.
        ///     从 Control 工厂创建贡献项。
        /// </summary>
        public static CardOverlayContribution FromFactory(
            string id,
            Func<CardOverlayContext, Control?> factory,
            int order = 0,
            bool fullRect = true)
        {
            return new()
            {
                Id = id,
                Factory = factory,
                Order = order,
                FullRect = fullRect,
            };
        }
    }

    /// <summary>
    ///     Optional card model or capability hook that contributes visual overlays to the owning card node.
    ///     Contributions are added after vanilla affliction/built-in overlay handling, so they stack without replacing
    ///     the vanilla overlay slot.
    ///     可选卡牌模型或能力钩子：向所属卡牌节点贡献可视覆盖层。贡献项会在原版 affliction / built-in 覆盖层处理后加入，
    ///     因此会叠加显示，而不会替换原版覆盖层槽位。
    /// </summary>
    public interface ICardOverlayContributor
    {
        /// <summary>
        ///     Returns overlays for the current card node. Return an empty sequence when no overlay should be shown.
        ///     返回当前卡牌节点的覆盖层。不需要显示覆盖层时返回空序列。
        /// </summary>
        IEnumerable<CardOverlayContribution> GetCardOverlays(CardOverlayContext context);
    }

    /// <summary>
    ///     Optional card capability that contributes overlay asset paths for preload/diagnostics. Factory-created
    ///     overlays that load resources internally should report those paths here or through
    ///     <see cref="IModelAssetPathContributor{TModel}" />.
    ///     可选卡牌能力：为覆盖层贡献预加载/诊断用资源路径。通过工厂创建且内部加载资源的覆盖层，应在这里或通过
    ///     <see cref="IModelAssetPathContributor{TModel}" /> 报告这些路径。
    /// </summary>
    public interface ICardOverlayAssetPathContributor
    {
        /// <summary>
        ///     Returns additional card overlay asset paths.
        ///     返回额外的卡牌覆盖层资源路径。
        /// </summary>
        IEnumerable<string> GetCardOverlayAssetPaths(CardModel card);
    }

    /// <summary>
    ///     Context passed to card-title contributors.
    ///     传给卡牌标题贡献者的上下文。
    /// </summary>
    public readonly record struct CardTitleContext(
        CardModel Card,
        LocString BaseTitleLocString,
        string BaseTitle,
        string UpgradeSuffix,
        string VanillaTitle);

    /// <summary>
    ///     Placement for capability-provided card title fragments.
    ///     能力提供的卡牌标题片段插入位置。
    /// </summary>
    public enum CardTitleFragmentPlacement
    {
        /// <summary>
        ///     Insert before the card's base title.
        ///     插入到卡牌基础标题之前。
        /// </summary>
        BeforeBase,

        /// <summary>
        ///     Replace the card's base title. The first replacement wins.
        ///     替换卡牌基础标题；第一个替换胜出。
        /// </summary>
        ReplaceBase,

        /// <summary>
        ///     Insert after the selected base title and before the vanilla upgrade suffix.
        ///     插入到选定基础标题之后、原版升级后缀之前。
        /// </summary>
        AfterBase,

        /// <summary>
        ///     Insert after the vanilla upgrade suffix.
        ///     插入到原版升级后缀之后。
        /// </summary>
        AfterTitle,
    }

    /// <summary>
    ///     Localized card title fragment contributed by a capability.
    ///     由能力贡献的本地化卡牌标题片段。
    /// </summary>
    public readonly record struct CardTitleFragment(
        LocString Text,
        CardTitleFragmentPlacement Placement = CardTitleFragmentPlacement.AfterBase,
        int Order = 0);

    /// <summary>
    ///     Optional model capability that contributes localized card-title fragments.
    ///     可选能力：贡献本地化卡牌标题片段。
    /// </summary>
    public interface ICardTitleContributor
    {
        /// <summary>
        ///     Returns title fragments for the owning card. Fragments are formatted through the game
        ///     <see cref="LocString" /> pipeline with the owning card's dynamic vars. Multiple prefix/suffix
        ///     fragments compose by order; multiple base replacements are reported when
        ///     <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> is enabled.
        ///     返回所属卡牌的标题片段。片段会带所属卡牌动态变量进入游戏原生 <see cref="LocString" /> 格式化管线。
        ///     多个前缀/后缀片段按顺序组合；多个基础标题替换会按冲突诊断设置记录。
        /// </summary>
        IEnumerable<CardTitleFragment> GetTitleFragments(CardTitleContext context);
    }

    /// <summary>
    ///     Optional card interface for visually modifying its own type text. Returned strings use the same composition
    ///     contract as BaseLib: entries containing <c>{Type}</c> wrap the selected base text, while entries without it
    ///     replace the base text.
    ///     可选卡牌接口：修改自身显示的类型文本。返回文本使用与 BaseLib 相同的组合契约：包含 <c>{Type}</c> 的条目
    ///     包裹选定的基础文本，不包含它的条目替换基础文本。
    /// </summary>
    public interface ICustomTypeTextCard
    {
        /// <summary>
        ///     Returns localized type text modifiers in application order.
        ///     按应用顺序返回本地化类型文本修改器。
        /// </summary>
        IEnumerable<LocString> GetTypeModifiers();
    }

    /// <summary>
    ///     Optional model or model-capability hook for visually modifying cards' type text. The method signature and
    ///     composition contract match BaseLib's <c>ICardTypeTextModifier</c>.
    ///     可选模型或模型能力 hook：修改卡牌显示的类型文本。方法签名与组合契约和 BaseLib 的
    ///     <c>ICardTypeTextModifier</c> 一致。
    /// </summary>
    public interface ICardTypeTextModifier
    {
        /// <summary>
        ///     Returns localized type text modifiers for <paramref name="card" /> in application order.
        ///     按应用顺序返回 <paramref name="card" /> 的本地化类型文本修改器。
        /// </summary>
        IEnumerable<LocString> GetTypeModifiers(CardModel card);
    }

    /// <summary>
    ///     Optional model capability that contributes hand glow predicates.
    ///     可选能力：贡献手牌发光判定。
    /// </summary>
    public interface ICardGlowContributor
    {
        /// <summary>
        ///     Returns true to make the card glow gold.
        ///     返回 true 时让卡牌显示金色发光。
        /// </summary>
        bool ShouldGlowGold(CardModel card)
        {
            return false;
        }

        /// <summary>
        ///     Returns true to make the card glow red.
        ///     返回 true 时让卡牌显示红色发光。
        /// </summary>
        bool ShouldGlowRed(CardModel card)
        {
            return false;
        }
    }

    /// <summary>
    ///     Optional card capability that contributes card-facing property overrides.
    ///     可选卡牌能力：贡献卡牌侧属性覆盖。
    /// </summary>
    public interface ICardPropertyContributor
    {
        /// <summary>
        ///     Returns a card type override, or null to leave the current value unchanged. The first non-null
        ///     contributor result wins; additional overrides are reported when
        ///     <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> is enabled.
        ///     返回卡牌类型覆盖；返回 null 表示不修改当前值。
        /// </summary>
        CardType? GetCardType(CardModel card)
        {
            return null;
        }

        /// <summary>
        ///     Returns a card rarity override, or null to leave the current value unchanged. The first non-null
        ///     contributor result wins; additional overrides are reported when
        ///     <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> is enabled.
        ///     返回卡牌稀有度覆盖；返回 null 表示不修改当前值。
        /// </summary>
        CardRarity? GetCardRarity(CardModel card)
        {
            return null;
        }

        /// <summary>
        ///     Returns a target type override, or null to leave the current value unchanged. The first non-null
        ///     contributor result wins; additional overrides are reported when
        ///     <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> is enabled.
        ///     返回目标类型覆盖；返回 null 表示不修改当前值。
        /// </summary>
        TargetType? GetTargetType(CardModel card)
        {
            return null;
        }

        /// <summary>
        ///     Returns additional card tags. Tags from all contributors are appended, then de-duplicated.
        ///     返回额外卡牌标签。
        /// </summary>
        IEnumerable<CardTag> GetTags(CardModel card)
        {
            return [];
        }
    }

    /// <summary>
    ///     Optional card capability that contributes local energy-cost modifications.
    ///     可选卡牌能力：贡献卡牌本地能量费用修正。
    /// </summary>
    public interface ICardEnergyCostContributor
    {
        /// <summary>
        ///     Modifies the owning card's current local energy cost. This runs when
        ///     <see cref="CostModifiers.Local" /> is requested, including normal
        ///     <see cref="CostModifiers.All" /> card display and playability checks.
        ///     修改所属卡牌当前本地能量费用。当请求 <see cref="CostModifiers.Local" /> 时运行，
        ///     包括常规 <see cref="CostModifiers.All" /> 卡牌显示与可打出检查。
        /// </summary>
        int ModifyEnergyCost(CardModel card, int currentCost, CostModifiers modifiers)
        {
            return currentCost;
        }
    }

    /// <summary>
    ///     Optional card capability that contributes local star-cost modifications.
    ///     可选卡牌能力：贡献卡牌本地星星费用修正。
    /// </summary>
    public interface ICardStarCostContributor
    {
        /// <summary>
        ///     Modifies the owning card's current local star cost. Return a negative value to keep the
        ///     star cost hidden.
        ///     修改所属卡牌当前本地星星费用。返回负数会保持星星费用隐藏。
        /// </summary>
        int ModifyStarCost(CardModel card, int currentCost)
        {
            return currentCost;
        }
    }

    /// <summary>
    ///     Optional card capability that contributes play-state decisions.
    ///     可选卡牌能力：贡献出牌状态决策。
    /// </summary>
    public interface ICardPlayStateContributor
    {
        /// <summary>
        ///     Returns null to leave the current playability unchanged; otherwise overrides it. Contributors run in
        ///     capability order and later non-null results replace earlier results. Multiple overrides are reported
        ///     when <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> is enabled.
        ///     返回 null 表示不修改当前可打出状态；否则覆盖该状态。
        /// </summary>
        bool? CanPlay(CardModel card)
        {
            return null;
        }

        /// <summary>
        ///     Returns true when the owning card should receive turn-end-in-hand handling. Results are combined with OR.
        ///     返回 true 时，所属卡牌应接收手牌回合结束处理。
        /// </summary>
        bool HasTurnEndInHandEffect(CardModel card)
        {
            return false;
        }
    }

    /// <summary>
    ///     Optional card capability that routes the owning card to a custom result pile after play.
    ///     可选卡牌能力：自定义所属卡牌打出后的目标牌堆。
    /// </summary>
    public interface ICardPlayResultContributor
    {
        /// <summary>
        ///     Returns a result pile override, or null to keep the current result. The first non-null contributor
        ///     result wins; additional overrides are reported when
        ///     <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> is enabled.
        ///     返回目标牌堆覆盖；返回 null 表示保持当前结果。
        /// </summary>
        PileType? GetResultPileTypeForCardPlay(CardModel card)
        {
            return null;
        }
    }

    /// <summary>
    ///     Optional card capability that can carry itself from an original card to a transform result.
    ///     可选卡牌能力：可将自身从转化前卡牌携带到转化结果卡牌。
    /// </summary>
    public interface ICardTransformCarryOverCapability
    {
        /// <summary>
        ///     Returns true when this capability should be copied to <paramref name="replacement" />.
        ///     返回 true 时，此能力会复制到 <paramref name="replacement" />。
        /// </summary>
        bool ShouldCarryOverToTransformResult(CardModel original, CardModel replacement)
        {
            return true;
        }

        /// <summary>
        ///     Creates the capability instance that should be attached to <paramref name="replacement" />.
        ///     创建应附加到 <paramref name="replacement" /> 的能力实例。
        /// </summary>
        IModelCapability CreateCarryOverCapability(CardModel original, CardModel replacement)
        {
            if (this is IModelCapabilityCloneHandler cloneHandler)
                return cloneHandler.CloneFor(replacement);

            throw new InvalidOperationException(
                $"Capability '{GetType().FullName}' must implement IModelCapabilityCloneHandler to carry over through transform.");
        }
    }

    internal static class CardModelCapabilityHost
    {
        private const string CardTypeSurface = "card property/type";
        private const string CardRaritySurface = "card property/rarity";
        private const string TargetTypeSurface = "card property/target-type";
        private const string TagsSurface = "card property/tags";
        private const string CanPlaySurface = "card play/can-play";
        private const string TurnEndInHandSurface = "card play/turn-end-in-hand";
        private const string ResultPileSurface = "card play/result-pile";
        private const string TransformCarryOverSurface = "card transform/carry-over";
        private const string DynamicVarPreviewSurface = "card dynamic-var/preview";
        private const string DescriptionSurface = "card description/fragments";
        private const string EnergyCostSurface = "card energy-cost/local";
        private const string StarCostSurface = "card star-cost/local";
        private const string UpgradeLifecycleSurface = "card lifecycle/upgraded";
        private const string FinalizeUpgradeLifecycleSurface = "card lifecycle/finalize-upgrade";
        private const string DowngradeLifecycleSurface = "card lifecycle/downgraded";
        private const string TransformFromLifecycleSurface = "card lifecycle/transform-from";
        private const string TransformToLifecycleSurface = "card lifecycle/transform-to";
        private const string TitleSurface = "card display/title";
        private const string HoverTipsSurface = "card display/hover-tips";
        private const string OverlaySurface = "card display/overlay";
        private const string OverlayAssetPathsSurface = "card asset/overlay-paths";
        private const string GoldGlowSurface = "card display/gold-glow";
        private const string RedGlowSurface = "card display/red-glow";

        internal static void ApplyTitleFragments(CardTitleContext context, ref string title)
        {
            List<OrderedTitleFragment> beforeFragments = [];
            List<OrderedTitleFragment> afterBaseFragments = [];
            List<OrderedTitleFragment> afterTitleFragments = [];
            OrderedTitleFragment? replacementFragment = null;
            ICardTitleContributor? replacementSource = null;
            var capabilityIndex = 0;
            foreach (var capability in GetCapabilities<ICardTitleContributor>(context.Card))
            {
                var currentSourceIndex = capabilityIndex++;
                IReadOnlyList<CardTitleFragment> fragments;
                try
                {
                    fragments = (capability.GetTitleFragments(context) ?? []).ToArray();
                }
                catch (Exception ex)
                {
                    LogFailure(capability, context.Card, TitleSurface, ex);
                    continue;
                }

                foreach (var (locString, placement, order) in fragments)
                    try
                    {
                        PrepareTitleFragment(context, locString, capability);
                        var text = locString.GetFormattedText();
                        if (text.Length == 0) continue;

                        var orderedFragment = new OrderedTitleFragment(
                            text,
                            order,
                            currentSourceIndex);
                        switch (placement)
                        {
                            case CardTitleFragmentPlacement.BeforeBase:
                                beforeFragments.Add(orderedFragment);
                                break;
                            case CardTitleFragmentPlacement.ReplaceBase:
                                if (replacementFragment == null)
                                {
                                    replacementFragment = orderedFragment;
                                    replacementSource = capability;
                                    break;
                                }

                                ModelCapabilityDiagnostics.WarnSurfaceConflict(
                                    TitleSurface,
                                    context.Card,
                                    replacementSource!,
                                    replacementFragment.Value.Text,
                                    capability,
                                    text);
                                break;
                            case CardTitleFragmentPlacement.AfterTitle:
                                afterTitleFragments.Add(orderedFragment);
                                break;
                            default:
                                afterBaseFragments.Add(orderedFragment);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogFailure(capability, context.Card, TitleSurface, ex);
                    }
            }

            if (beforeFragments.Count == 0 &&
                replacementFragment == null &&
                afterBaseFragments.Count == 0 &&
                afterTitleFragments.Count == 0)
                return;

            title = string.Concat(
                beforeFragments
                    .OrderBy(static fragment => fragment.Order)
                    .ThenBy(static fragment => fragment.SourceIndex)
                    .Select(static fragment => fragment.Text)
                    .Concat([replacementFragment?.Text ?? context.BaseTitle])
                    .Concat(afterBaseFragments
                        .OrderBy(static fragment => fragment.Order)
                        .ThenBy(static fragment => fragment.SourceIndex)
                        .Select(static fragment => fragment.Text))
                    .Concat([context.UpgradeSuffix])
                    .Concat(afterTitleFragments
                        .OrderBy(static fragment => fragment.Order)
                        .ThenBy(static fragment => fragment.SourceIndex)
                        .Select(static fragment => fragment.Text)));
        }

        internal static CardType ApplyCardType(CardModel card, CardType current)
        {
            return ApplyFirstOverride<CardType, ICardPropertyContributor>(
                card,
                CardTypeSurface,
                current,
                capability => capability.GetCardType(card));
        }

        internal static CardRarity ApplyCardRarity(CardModel card, CardRarity current)
        {
            return ApplyFirstOverride<CardRarity, ICardPropertyContributor>(
                card,
                CardRaritySurface,
                current,
                capability => capability.GetCardRarity(card));
        }

        internal static TargetType ApplyTargetType(CardModel card, TargetType current)
        {
            return ApplyFirstOverride<TargetType, ICardPropertyContributor>(
                card,
                TargetTypeSurface,
                current,
                capability => capability.GetTargetType(card));
        }

        internal static IEnumerable<CardTag> ApplyTags(CardModel card, IEnumerable<CardTag> current)
        {
            List<CardTag>? extraTags = null;
            foreach (var capability in GetCapabilities<ICardPropertyContributor>(card))
            {
                IEnumerable<CardTag> tags = [];
                TryRun(capability, card, TagsSurface, () => tags = capability.GetTags(card));
                foreach (var tag in tags)
                {
                    extraTags ??= [];
                    extraTags.Add(tag);
                }
            }

            return extraTags is not { Count: > 0 } ? current : MergeTags(current, extraTags);
        }

        internal static bool HasEnergyCostContributors(CardModel card)
        {
            return GetCapabilities<ICardEnergyCostContributor>(card).Any();
        }

        internal static bool HasStarCostContributors(CardModel card)
        {
            return GetCapabilities<ICardStarCostContributor>(card).Any();
        }

        internal static int ApplyEnergyCost(CardModel card, CostModifiers modifiers, int current)
        {
            if (!modifiers.HasFlag(CostModifiers.Local) ||
                card.EnergyCost.CostsX ||
                current < 0)
                return current;

            var result = current;
            foreach (var capability in GetCapabilities<ICardEnergyCostContributor>(card))
                TryRun(capability, card, EnergyCostSurface,
                    () => result = capability.ModifyEnergyCost(card, result, modifiers));

            return Math.Max(0, result);
        }

        internal static int ApplyStarCost(CardModel card, int current)
        {
            if (card.HasStarCostX)
                return current;

            var result = current;
            foreach (var capability in GetCapabilities<ICardStarCostContributor>(card))
                TryRun(capability, card, StarCostSurface,
                    () => result = capability.ModifyStarCost(card, result));

            return result < 0 ? result : Math.Max(0, result);
        }

        internal static bool ApplyCanPlay(CardModel card, bool current)
        {
            return ApplyLastOverride<bool, ICardPlayStateContributor>(
                card,
                CanPlaySurface,
                current,
                capability => capability.CanPlay(card));
        }

        internal static bool HasTurnEndInHandEffect(CardModel card)
        {
            foreach (var capability in GetCapabilities<ICardPlayStateContributor>(card))
            {
                var value = false;
                TryRun(capability, card, TurnEndInHandSurface, () => value = capability.HasTurnEndInHandEffect(card));
                if (value)
                    return true;
            }

            return false;
        }

        internal static PileType ApplyResultPileTypeForCardPlay(CardModel card, PileType current)
        {
            return ApplyFirstOverride<PileType, ICardPlayResultContributor>(
                card,
                ResultPileSurface,
                current,
                capability => capability.GetResultPileTypeForCardPlay(card));
        }

        internal static void CarryOverTransformCapabilities(CardModel original, CardModel? replacement)
        {
            if (replacement == null || ReferenceEquals(original, replacement))
                return;

            foreach (var capability in GetCapabilities<ICardTransformCarryOverCapability>(original))
                TryRun(capability, original, TransformCarryOverSurface, () =>
                {
                    if (!capability.ShouldCarryOverToTransformResult(original, replacement))
                        return;

                    var carried = capability.CreateCarryOverCapability(original, replacement);
                    replacement.Capabilities().Apply(carried);
                });
        }

        internal static void UpdateDynamicVarPreviews(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target)
        {
            foreach (var capability in GetCapabilities<IModelDynamicVarContributor>(card))
            {
                DynamicVarSet? dynamicVars = null;
                TryRun(capability, card, DynamicVarPreviewSurface, () => dynamicVars = capability.GetDynamicVars(card));
                if (dynamicVars == null)
                    continue;

                TryRun(capability, card, DynamicVarPreviewSurface, () =>
                {
                    dynamicVars.ClearPreview();
                    card.UpdateDynamicVarPreview(previewMode, target, dynamicVars);
                });
            }
        }

        internal static void ApplyDescriptionFragments(
            CardModel card,
            PileType pileType,
            object? previewType,
            Creature? target,
            List<string> lines)
        {
            var context = new CardDescriptionContext(
                card,
                pileType,
                target,
                string.Equals(previewType?.ToString(), "Upgrade", StringComparison.Ordinal));

            List<OrderedDescriptionFragment> beforeFragments = [];
            List<OrderedDescriptionFragment> afterFragments = [];
            var capabilityIndex = 0;
            foreach (var capability in GetCapabilities<ICardDescriptionContributor>(context.Card))
            {
                var currentSourceIndex = capabilityIndex++;
                IReadOnlyList<CardDescriptionFragment> fragments;
                try
                {
                    fragments = (capability.GetDescriptionFragments(context) ?? []).ToArray();
                }
                catch (Exception ex)
                {
                    LogFailure(capability, context.Card, DescriptionSurface, ex);
                    continue;
                }

                foreach (var (locString, cardDescriptionFragmentPlacement, order) in fragments)
                    try
                    {
                        PrepareDescriptionFragment(context, locString, capability);

                        var text = locString.GetFormattedText();
                        if (string.IsNullOrWhiteSpace(text)) continue;
                        var orderedFragment = new OrderedDescriptionFragment(
                            text,
                            order,
                            currentSourceIndex);
                        if (cardDescriptionFragmentPlacement == CardDescriptionFragmentPlacement.BeforeBase)
                            beforeFragments.Add(orderedFragment);
                        else
                            afterFragments.Add(orderedFragment);
                    }
                    catch (Exception ex)
                    {
                        LogFailure(capability, context.Card, DescriptionSurface, ex);
                    }
            }

            if (beforeFragments.Count == 0 && afterFragments.Count == 0)
                return;

            var beforeText = beforeFragments
                .OrderBy(static fragment => fragment.Order)
                .ThenBy(static fragment => fragment.SourceIndex)
                .Select(static fragment => fragment.Text)
                .ToArray();
            var afterText = afterFragments
                .OrderBy(static fragment => fragment.Order)
                .ThenBy(static fragment => fragment.SourceIndex)
                .Select(static fragment => fragment.Text)
                .ToArray();

            if (beforeText.Length > 0)
                lines.InsertRange(0, beforeText);

            if (afterText.Length <= 0)
                return;

            var afterBaseIndex = lines.Count == 0 ? 0 : beforeText.Length + 1;
            lines.InsertRange(Math.Min(afterBaseIndex, lines.Count), afterText);
        }

        internal static void AfterOwnerCardUpgraded(CardModel card)
        {
            foreach (var capability in GetCapabilities<CardCapability>(card))
            {
                if (!IsStillAttachedToCard(capability, card))
                    continue;

                TryRun(capability, card, UpgradeLifecycleSurface, () =>
                {
                    if (!IsStillAttachedToCard(capability, card))
                        return;

                    capability.NotifyOwnerCardUpgraded(card);
                    if (!IsStillAttachedToCard(capability, card))
                        return;

                    capability.RecalculateDynamicVarsForUpgradeOrEnchant();
                });
            }
        }

        internal static void AfterOwnerCardUpgradeFinalized(CardModel card)
        {
            foreach (var capability in GetCapabilities<CardCapability>(card))
            {
                if (!IsStillAttachedToCard(capability, card))
                    continue;

                TryRun(capability, card, FinalizeUpgradeLifecycleSurface, () =>
                {
                    if (!IsStillAttachedToCard(capability, card))
                        return;

                    capability.FinalizeDynamicVarUpgrade();
                    if (!IsStillAttachedToCard(capability, card))
                        return;

                    capability.NotifyOwnerCardUpgradeFinalized(card);
                });
            }
        }

        internal static void AfterOwnerCardDowngraded(CardModel card)
        {
            foreach (var capability in GetCapabilities<CardCapability>(card))
            {
                if (!IsStillAttachedToCard(capability, card))
                    continue;

                TryRun(capability, card, DowngradeLifecycleSurface, () =>
                {
                    if (!IsStillAttachedToCard(capability, card))
                        return;

                    capability.NotifyOwnerCardDowngraded(card);
                });
            }
        }

        internal static void AfterOwnerCardTransformedFrom(CardModel card)
        {
            foreach (var capability in GetCapabilities<CardCapability>(card))
            {
                if (!IsStillAttachedToCard(capability, card))
                    continue;

                TryRun(capability, card, TransformFromLifecycleSurface,
                    () => capability.NotifyOwnerCardTransformedFrom(card));
            }
        }

        internal static void AfterOwnerCardTransformedTo(CardModel card)
        {
            foreach (var capability in GetCapabilities<CardCapability>(card))
            {
                if (!IsStillAttachedToCard(capability, card))
                    continue;

                TryRun(capability, card, TransformToLifecycleSurface,
                    () => capability.NotifyOwnerCardTransformedTo(card));
            }
        }

        private static void PrepareDescriptionFragment(
            CardDescriptionContext context,
            LocString locString,
            ICardDescriptionContributor source)
        {
            var card = context.Card;

            card.DynamicVars.AddTo(locString);
            if (source is IModelDynamicVarContributor dynamicVarCapability and IModelCapability capability)
                ModelCapabilityHost.AddDynamicVarsTo(
                    card,
                    capability,
                    dynamicVarCapability.LocStringVariableScope,
                    dynamicVarCapability.GetDynamicVars(card),
                    locString);

            var upgradeDisplay = context.IsUpgradePreview
                ? UpgradeDisplay.UpgradePreview
                : card.IsUpgraded
                    ? UpgradeDisplay.Upgraded
                    : UpgradeDisplay.Normal;
            locString.Add(new IfUpgradedVar(upgradeDisplay));

            var onTable = context.PileType is PileType.Hand or PileType.Play;
            locString.Add("OnTable", onTable);
            locString.Add("InCombat",
                CombatManager.Instance.IsInProgress && (card.Pile?.IsCombatPile ?? context.PileType.IsCombatPile()));
            locString.Add("IsTargeting", context.Target != null);
            locString.Add("TargetType", card.TargetType.ToString());
            locString.Add("GainsBlock", card.GainsBlock);
            locString.Add("energyPrefix", EnergyIconHelper.GetPrefix(card));
            locString.Add("singleStarIcon", "[img]res://images/packed/sprite_fonts/star_icon.png[/img]");

            var energyPrefix = EnergyIconHelper.GetPrefix(card);
            foreach (var value in locString.Variables.Values)
                if (value is EnergyVar energyVar)
                    energyVar.ColorPrefix = energyPrefix;
        }

        private static void PrepareTitleFragment(
            CardTitleContext context,
            LocString locString,
            ICardTitleContributor source)
        {
            var card = context.Card;

            card.DynamicVars.AddTo(locString);
            if (source is IModelDynamicVarContributor dynamicVarCapability and IModelCapability capability)
                ModelCapabilityHost.AddDynamicVarsTo(
                    card,
                    capability,
                    dynamicVarCapability.LocStringVariableScope,
                    dynamicVarCapability.GetDynamicVars(card),
                    locString);

            var upgradeDisplay = card.IsUpgraded
                ? UpgradeDisplay.Upgraded
                : UpgradeDisplay.Normal;
            locString.Add(new IfUpgradedVar(upgradeDisplay));
            locString.Add("BaseTitle", context.BaseTitle);
            locString.Add("UpgradeSuffix", context.UpgradeSuffix);
            locString.Add("VanillaTitle", context.VanillaTitle);
        }

        internal static IEnumerable<IHoverTip> GetHoverTips(CardModel card)
        {
            foreach (var tip in ModelCapabilityHost.GetHoverTips(card))
                yield return tip;

            foreach (var capability in GetCapabilities<ICardHoverTipContributor>(card))
            {
                IEnumerable<IHoverTip> tips = [];
                TryRun(capability, card, HoverTipsSurface, () => tips = capability.GetHoverTips(card));

                foreach (var tip in tips)
                    yield return tip;
            }
        }

        internal static IEnumerable<OrderedCardOverlayContribution> GetOverlayContributions(
            CardOverlayContext context)
        {
            var sourceIndex = 0;
            if (context.Card is ICardOverlayContributor cardContributor)
                foreach (var contribution in GetOverlayContributions(context, cardContributor, sourceIndex++))
                    yield return contribution;

            foreach (var capability in GetCapabilities<ICardOverlayContributor>(context.Card))
            foreach (var contribution in GetOverlayContributions(context, capability, sourceIndex++))
                yield return contribution;
        }

        private static IEnumerable<OrderedCardOverlayContribution> GetOverlayContributions(
            CardOverlayContext context,
            ICardOverlayContributor source,
            int sourceIndex)
        {
            IReadOnlyList<CardOverlayContribution> contributions = [];
            TryRun(source, context.Card, OverlaySurface,
                () => contributions = (source.GetCardOverlays(context) ?? []).ToArray());

            foreach (var contribution in contributions)
                yield return new(source, contribution, sourceIndex);
        }

        internal static IEnumerable<string> GetOverlayAssetPaths(CardModel card)
        {
            foreach (var capability in GetCapabilities<ICardOverlayAssetPathContributor>(card))
            {
                IEnumerable<string> paths = [];
                TryRun(capability, card, OverlayAssetPathsSurface,
                    () => paths = capability.GetCardOverlayAssetPaths(card));

                foreach (var path in paths)
                    yield return path;
            }
        }

        internal static bool ShouldGlowGold(CardModel card)
        {
            foreach (var capability in GetCapabilities<ICardGlowContributor>(card))
            {
                var shouldGlow = false;
                TryRun(capability, card, GoldGlowSurface, () => shouldGlow = capability.ShouldGlowGold(card));
                if (shouldGlow)
                    return true;
            }

            return false;
        }

        internal static bool ShouldGlowRed(CardModel card)
        {
            foreach (var capability in GetCapabilities<ICardGlowContributor>(card))
            {
                var shouldGlow = false;
                TryRun(capability, card, RedGlowSurface, () => shouldGlow = capability.ShouldGlowRed(card));
                if (shouldGlow)
                    return true;
            }

            return false;
        }

        private static T ApplyFirstOverride<T, TCapability>(
            CardModel card,
            string surface,
            T current,
            Func<TCapability, T?> getValue)
            where T : struct
            where TCapability : class
        {
            var hasWinner = false;
            var winnerValue = default(T);
            TCapability? winner = null;

            foreach (var capability in GetCapabilities<TCapability>(card))
            {
                T? value = null;
                TryRun(capability, card, surface, () => value = getValue(capability));
                if (!value.HasValue)
                    continue;

                if (!hasWinner)
                {
                    hasWinner = true;
                    winnerValue = value.Value;
                    winner = capability;
                    if (!ModelCapabilityDiagnostics.ShouldInspectConflicts)
                        return winnerValue;
                    continue;
                }

                ModelCapabilityDiagnostics.WarnSurfaceConflict(
                    surface,
                    card,
                    winner!,
                    winnerValue,
                    capability,
                    value.Value);
            }

            return hasWinner ? winnerValue : current;
        }

        private static T ApplyLastOverride<T, TCapability>(
            CardModel card,
            string surface,
            T current,
            Func<TCapability, T?> getValue)
            where T : struct
            where TCapability : class
        {
            var result = current;
            var hasPrevious = false;
            var previousValue = default(T);
            TCapability? previous = null;

            foreach (var capability in GetCapabilities<TCapability>(card))
            {
                T? value = null;
                TryRun(capability, card, surface, () => value = getValue(capability));
                if (!value.HasValue)
                    continue;

                if (hasPrevious)
                    ModelCapabilityDiagnostics.WarnSurfaceConflict(
                        surface,
                        card,
                        previous!,
                        previousValue,
                        capability,
                        value.Value);

                result = value.Value;
                previousValue = result;
                previous = capability;
                hasPrevious = true;
            }

            return result;
        }

        private static IEnumerable<TCapability> GetCapabilities<TCapability>(CardModel card)
            where TCapability : class
        {
            return ModelCapabilityHost.GetCapabilities<TCapability>(card);
        }

        private static IEnumerable<CardTag> MergeTags(
            IEnumerable<CardTag> current,
            IReadOnlyList<CardTag> extraTags)
        {
            HashSet<CardTag> seen = [];
            foreach (var tag in current)
                if (seen.Add(tag))
                    yield return tag;

            foreach (var tag in extraTags)
                if (seen.Add(tag))
                    yield return tag;
        }

        private static void TryRun<TCapability>(
            TCapability capability,
            CardModel card,
            string surface,
            Action action)
            where TCapability : class
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                LogFailure(capability, card, surface, ex);
            }
        }

        private static void LogFailure<TCapability>(
            TCapability capability,
            CardModel card,
            string surface,
            Exception ex)
            where TCapability : class
        {
            ModelCapabilityDiagnostics.WarnFailure(surface, card, capability, ex);
        }

        private static bool IsStillAttachedToCard(CardCapability capability, CardModel card)
        {
            return ReferenceEquals(capability.Owner, card);
        }

        private readonly record struct OrderedTitleFragment(string Text, int Order, int SourceIndex);

        private readonly record struct OrderedDescriptionFragment(string Text, int Order, int SourceIndex);

        internal readonly record struct OrderedCardOverlayContribution(
            ICardOverlayContributor Source,
            CardOverlayContribution Contribution,
            int SourceIndex);
    }
}
