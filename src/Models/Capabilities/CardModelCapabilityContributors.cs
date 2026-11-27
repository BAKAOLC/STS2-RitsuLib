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
    ///     <para xml:lang="en">Context passed to card-description contributors.</para>
    ///     <para xml:lang="zh-CN">传给卡牌描述贡献者的上下文。</para>
    /// </summary>
    public readonly record struct CardDescriptionContext(
        CardModel Card,
        PileType PileType,
        Creature? Target,
        bool IsUpgradePreview);

    /// <summary>
    ///     <para xml:lang="en">Placement for capability-provided card description fragments.</para>
    ///     <para xml:lang="zh-CN">能力提供的卡牌描述片段插入位置。</para>
    /// </summary>
    public enum CardDescriptionFragmentPlacement
    {
        /// <summary>
        ///     <para xml:lang="en">Insert before the card's own description.</para>
        ///     <para xml:lang="zh-CN">插入到卡牌自身描述之前。</para>
        /// </summary>
        BeforeBase,

        /// <summary>
        ///     <para xml:lang="en">Insert after the card's own description.</para>
        ///     <para xml:lang="zh-CN">插入到卡牌自身描述之后。</para>
        /// </summary>
        AfterBase,
    }

    /// <summary>
    ///     <para xml:lang="en">Localized card description fragment contributed by a capability.</para>
    ///     <para xml:lang="zh-CN">由能力贡献的本地化卡牌描述片段。</para>
    /// </summary>
    public readonly record struct CardDescriptionFragment(
        LocString Text,
        CardDescriptionFragmentPlacement Placement = CardDescriptionFragmentPlacement.AfterBase,
        int Order = 0);

    /// <summary>
    ///     <para xml:lang="en">Optional model capability that contributes localized card-description fragments.</para>
    ///     <para xml:lang="zh-CN">可选能力：贡献本地化卡牌描述片段。</para>
    /// </summary>
    public interface ICardDescriptionContributor
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns extra localized description fragments. Fragments are formatted through the game
        ///         <see cref="LocString" /> formatting process with the owning card's dynamic vars.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回额外的本地化描述片段。片段会携带所属卡牌的动态变量，经由游戏原版
        ///         <see cref="LocString" /> 格式化流程处理。
        ///     </para>
        /// </summary>
        IEnumerable<CardDescriptionFragment> GetDescriptionFragments(CardDescriptionContext context);
    }

    /// <summary>
    ///     <para xml:lang="en">Optional model capability that contributes card hover tips.</para>
    ///     <para xml:lang="zh-CN">可选能力：贡献卡牌悬停提示。</para>
    /// </summary>
    public interface ICardHoverTipContributor
    {
        /// <summary>
        ///     <para xml:lang="en">Returns additional hover tips for <paramref name="card" />.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="card" /> 的额外悬停提示。</para>
        /// </summary>
        IEnumerable<IHoverTip> GetHoverTips(CardModel card);
    }

    /// <summary>
    ///     <para xml:lang="en">Runtime context for capability-provided card overlays.</para>
    ///     <para xml:lang="zh-CN">能力提供卡牌覆盖层时的运行时上下文。</para>
    /// </summary>
    public readonly record struct CardOverlayContext(
        CardModel Card,
        NCard CardNode,
        Node OverlayContainer);

    /// <summary>
    ///     <para xml:lang="en">
    ///         One visual overlay contributed by a card capability. Exactly one creation source should be set:
    ///         <see cref="ScenePath" />, <see cref="Scene" />, or <see cref="Factory" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         卡牌能力贡献的一个可视覆盖层。应且仅应设置一个创建来源：
    ///         <see cref="ScenePath" />、<see cref="Scene" /> 或 <see cref="Factory" />。
    ///     </para>
    /// </summary>
    public sealed record CardOverlayContribution
    {
        /// <summary>
        ///     <para xml:lang="en">Stable ID for diagnostics and ordering within the contributing capability.</para>
        ///     <para xml:lang="zh-CN">用于诊断以及贡献能力内部排序的稳定 ID。</para>
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Draw order among capability overlays. Larger values are added later and render above smaller
        ///         values.
        ///     </para>
        ///     <para xml:lang="zh-CN">能力覆盖层之间的绘制顺序。数值越大越晚加入，因此显示在更上层。</para>
        /// </summary>
        public int Order { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         When <see langword="true" />, RitsuLib applies a full-rectangle layout preset after creating the
        ///         overlay node.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <see langword="true" /> 时，RitsuLib 会在创建覆盖层节点后应用全矩形布局预设。
        ///     </para>
        /// </summary>
        public bool FullRect { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Packed-scene path loaded through the game's preload cache, matching base-game card-overlay creation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过游戏预加载缓存加载的 <see cref="PackedScene" /> 路径，与游戏原版卡牌覆盖层的创建行为一致。
        ///     </para>
        /// </summary>
        public string? ScenePath { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Already resolved packed scene.</para>
        ///     <para xml:lang="zh-CN">已解析的 PackedScene。</para>
        /// </summary>
        public PackedScene? Scene { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Factory for creating a fresh overlay control. It must return a new node each time because one
        ///         <see cref="CardModel" /> can be displayed by multiple <see cref="NCard" /> nodes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         用于创建全新覆盖层 <see cref="Control" /> 的工厂。每次必须返回新的节点，因为同一个
        ///         <see cref="CardModel" /> 可能同时由多个 <see cref="NCard" /> 节点显示。
        ///     </para>
        /// </summary>
        public Func<CardOverlayContext, Control?>? Factory { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Creates a contribution from a packed scene path.</para>
        ///     <para xml:lang="zh-CN">从 PackedScene 路径创建贡献项。</para>
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
        ///     <para xml:lang="en">Creates a contribution from an already resolved packed scene.</para>
        ///     <para xml:lang="zh-CN">从已解析的 PackedScene 创建贡献项。</para>
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
        ///     <para xml:lang="en">Creates a contribution from a control factory.</para>
        ///     <para xml:lang="zh-CN">从 <see cref="Control" /> 工厂创建贡献项。</para>
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
    ///     <para xml:lang="en">
    ///         Optional card model or capability hook that contributes visual overlays to the owning card node.
    ///         Contributions are added after base-game affliction and built-in overlay handling, so they stack without
    ///         replacing the base-game overlay slot.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可选的卡牌模型或能力钩子，用于向所属卡牌节点提供可视覆盖层。贡献项会在游戏原版的苦痛及内置
    ///         覆盖层处理完成后加入，因此会叠加显示，而不会替换原版覆盖层槽位。
    ///     </para>
    /// </summary>
    public interface ICardOverlayContributor
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns overlays for the current card node. Return an empty sequence when no overlay should be
        ///         shown.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回当前卡牌节点的覆盖层。不需要显示覆盖层时返回空序列。</para>
        /// </summary>
        IEnumerable<CardOverlayContribution> GetCardOverlays(CardOverlayContext context);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Optional card capability that contributes overlay asset paths for preload/diagnostics. Factory-created
    ///         overlays that load resources internally should report those paths here or through
    ///         <see cref="IModelAssetPathContributor{TModel}" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可选卡牌能力：为覆盖层贡献预加载/诊断用资源路径。通过工厂创建且内部加载资源的覆盖层，应在这里或通过
    ///         <see cref="IModelAssetPathContributor{TModel}" /> 报告这些路径。
    ///     </para>
    /// </summary>
    public interface ICardOverlayAssetPathContributor
    {
        /// <summary>
        ///     <para xml:lang="en">Returns additional card overlay asset paths.</para>
        ///     <para xml:lang="zh-CN">返回额外的卡牌覆盖层资源路径。</para>
        /// </summary>
        IEnumerable<string> GetCardOverlayAssetPaths(CardModel card);
    }

    /// <summary>
    ///     <para xml:lang="en">Context passed to card-title contributors.</para>
    ///     <para xml:lang="zh-CN">传给卡牌标题贡献者的上下文。</para>
    /// </summary>
    public readonly record struct CardTitleContext(
        CardModel Card,
        LocString BaseTitleLocString,
        string BaseTitle,
        string UpgradeSuffix,
        string VanillaTitle);

    /// <summary>
    ///     <para xml:lang="en">Placement for capability-provided card title fragments.</para>
    ///     <para xml:lang="zh-CN">能力提供的卡牌标题片段插入位置。</para>
    /// </summary>
    public enum CardTitleFragmentPlacement
    {
        /// <summary>
        ///     <para xml:lang="en">Insert before the card's base title.</para>
        ///     <para xml:lang="zh-CN">插入到卡牌基础标题之前。</para>
        /// </summary>
        BeforeBase,

        /// <summary>
        ///     <para xml:lang="en">Replace the card's base title. The first replacement wins.</para>
        ///     <para xml:lang="zh-CN">替换卡牌基础标题；第一个替换胜出。</para>
        /// </summary>
        ReplaceBase,

        /// <summary>
        ///     <para xml:lang="en">Insert after the selected base title and before the vanilla upgrade suffix.</para>
        ///     <para xml:lang="zh-CN">插入到选定基础标题之后、原版升级后缀之前。</para>
        /// </summary>
        AfterBase,

        /// <summary>
        ///     <para xml:lang="en">Insert after the vanilla upgrade suffix.</para>
        ///     <para xml:lang="zh-CN">插入到原版升级后缀之后。</para>
        /// </summary>
        AfterTitle,
    }

    /// <summary>
    ///     <para xml:lang="en">Localized card title fragment contributed by a capability.</para>
    ///     <para xml:lang="zh-CN">由能力贡献的本地化卡牌标题片段。</para>
    /// </summary>
    public readonly record struct CardTitleFragment(
        LocString Text,
        CardTitleFragmentPlacement Placement = CardTitleFragmentPlacement.AfterBase,
        int Order = 0);

    /// <summary>
    ///     <para xml:lang="en">Optional model capability that contributes localized card-title fragments.</para>
    ///     <para xml:lang="zh-CN">可选能力：贡献本地化卡牌标题片段。</para>
    /// </summary>
    public interface ICardTitleContributor
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns title fragments for the owning card. Fragments are formatted through the game
        ///         <see cref="LocString" /> formatting process with the owning card's dynamic vars. Multiple prefix/suffix
        ///         fragments compose by order; multiple base replacements are reported when
        ///         <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> is enabled.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回所属卡牌的标题片段。片段会携带所属卡牌的动态变量，经由游戏原版
        ///         <see cref="LocString" /> 格式化流程处理。
        ///         多个前缀/后缀片段按顺序组合；多个基础标题替换会按冲突诊断设置记录。
        ///     </para>
        /// </summary>
        IEnumerable<CardTitleFragment> GetTitleFragments(CardTitleContext context);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Optional card interface for visually modifying its own type text. Returned strings use the same composition
    ///         contract as BaseLib: entries containing <c>{Type}</c> wrap the selected base text, while entries without it
    ///         replace the base text.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可选卡牌接口：修改自身显示的类型文本。返回文本使用与 BaseLib 相同的组合契约：包含 <c>{Type}</c> 的条目
    ///         包裹选定的基础文本，不包含它的条目替换基础文本。
    ///     </para>
    /// </summary>
    public interface ICustomTypeTextCard
    {
        /// <summary>
        ///     <para xml:lang="en">Returns localized type text modifiers in application order.</para>
        ///     <para xml:lang="zh-CN">按应用顺序返回本地化类型文本修改器。</para>
        /// </summary>
        IEnumerable<LocString> GetTypeModifiers();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Optional model or model-capability hook for visually modifying cards' type text. The method signature and
    ///         composition contract match BaseLib's <c>ICardTypeTextModifier</c>.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可选的模型或模型能力钩子，用于修改卡牌显示的类型文本。方法签名与组合契约和 BaseLib 的
    ///         <c>ICardTypeTextModifier</c> 一致。
    ///     </para>
    /// </summary>
    public interface ICardTypeTextModifier
    {
        /// <summary>
        ///     <para xml:lang="en">Returns localized type text modifiers for <paramref name="card" /> in application order.</para>
        ///     <para xml:lang="zh-CN">按应用顺序返回 <paramref name="card" /> 的本地化类型文本修改器。</para>
        /// </summary>
        IEnumerable<LocString> GetTypeModifiers(CardModel card);
    }

    /// <summary>
    ///     <para xml:lang="en">Optional model capability that contributes hand glow predicates.</para>
    ///     <para xml:lang="zh-CN">可选能力：贡献手牌发光判定。</para>
    /// </summary>
    public interface ICardGlowContributor
    {
        /// <summary>
        ///     <para xml:lang="en">Returns <see langword="true" /> to make the card glow gold.</para>
        ///     <para xml:lang="zh-CN">返回 <see langword="true" /> 时让卡牌显示金色发光。</para>
        /// </summary>
        bool ShouldGlowGold(CardModel card)
        {
            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns <see langword="true" /> to make the card glow red.</para>
        ///     <para xml:lang="zh-CN">返回 <see langword="true" /> 时让卡牌显示红色发光。</para>
        /// </summary>
        bool ShouldGlowRed(CardModel card)
        {
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Optional card capability that contributes card-facing property overrides.</para>
    ///     <para xml:lang="zh-CN">可选卡牌能力：贡献卡牌侧属性覆盖。</para>
    /// </summary>
    public interface ICardPropertyContributor
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a card type override, or <see langword="null" /> to leave the current value unchanged. The first
        ///         non-null
        ///         contributor result wins; additional overrides are reported when
        ///         <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> is enabled.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回卡牌类型覆盖；返回 <see langword="null" /> 表示不修改当前值。首个非空结果生效；
        ///         启用 <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> 后会记录其他覆盖结果。
        ///     </para>
        /// </summary>
        CardType? GetCardType(CardModel card)
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a card rarity override, or <see langword="null" /> to leave the current value unchanged. The first
        ///         non-null
        ///         contributor result wins; additional overrides are reported when
        ///         <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> is enabled.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回卡牌稀有度覆盖；返回 <see langword="null" /> 表示不修改当前值。首个非空结果生效；
        ///         启用 <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> 后会记录其他覆盖结果。
        ///     </para>
        /// </summary>
        CardRarity? GetCardRarity(CardModel card)
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a target type override, or <see langword="null" /> to leave the current value unchanged. The first
        ///         non-null
        ///         contributor result wins; additional overrides are reported when
        ///         <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> is enabled.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回目标类型覆盖；返回 <see langword="null" /> 表示不修改当前值。首个非空结果生效；
        ///         启用 <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> 后会记录其他覆盖结果。
        ///     </para>
        /// </summary>
        TargetType? GetTargetType(CardModel card)
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns additional card tags. Tags from all contributors are appended, then deduplicated.</para>
        ///     <para xml:lang="zh-CN">返回额外卡牌标签。所有贡献者的标签会追加到现有标签后，再进行去重。</para>
        /// </summary>
        IEnumerable<CardTag> GetTags(CardModel card)
        {
            return [];
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Optional card capability that contributes local energy-cost modifications.</para>
    ///     <para xml:lang="zh-CN">可选卡牌能力：贡献卡牌本地能量费用修正。</para>
    /// </summary>
    public interface ICardEnergyCostContributor
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Modifies the owning card's current local energy cost. This runs when
        ///         <see cref="CostModifiers.Local" /> is requested, including normal
        ///         <see cref="CostModifiers.All" /> card display and playability checks.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         修改所属卡牌当前本地能量费用。当请求 <see cref="CostModifiers.Local" /> 时运行，
        ///         包括常规 <see cref="CostModifiers.All" /> 卡牌显示与可打出检查。
        ///     </para>
        /// </summary>
        int ModifyEnergyCost(CardModel card, int currentCost, CostModifiers modifiers)
        {
            return currentCost;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Optional card capability that contributes local star-cost modifications.</para>
    ///     <para xml:lang="zh-CN">可选卡牌能力：贡献卡牌本地星星费用修正。</para>
    /// </summary>
    public interface ICardStarCostContributor
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Modifies the owning card's current local star cost. Return a negative value to keep the
        ///         star cost hidden.
        ///     </para>
        ///     <para xml:lang="zh-CN">修改所属卡牌当前本地星星费用。返回负数会保持星星费用隐藏。</para>
        /// </summary>
        int ModifyStarCost(CardModel card, int currentCost)
        {
            return currentCost;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Optional card capability that contributes play-state decisions.</para>
    ///     <para xml:lang="zh-CN">可选卡牌能力：贡献出牌状态决策。</para>
    /// </summary>
    public interface ICardPlayStateContributor
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns <see langword="null" /> to leave the current playability unchanged; otherwise overrides it.
        ///         Contributors run in
        ///         capability order and later non-null results replace earlier results. Multiple overrides are reported
        ///         when <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> is enabled.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <see langword="null" /> 表示不修改当前可打出状态；否则覆盖该状态。贡献者按能力顺序运行，
        ///         后续非空结果会替换先前结果。启用 <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> 后会记录
        ///         多次覆盖。
        ///     </para>
        /// </summary>
        bool? CanPlay(CardModel card)
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns <see langword="true" /> when the owning card should receive turn-end-in-hand handling.
        ///         Results are combined using logical OR.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <see langword="true" /> 时，所属卡牌应接收手牌回合结束处理。所有结果按逻辑“或”合并。
        ///     </para>
        /// </summary>
        bool HasTurnEndInHandEffect(CardModel card)
        {
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Optional card capability that routes the owning card to a custom result pile after play.</para>
    ///     <para xml:lang="zh-CN">可选卡牌能力：自定义所属卡牌打出后的目标牌堆。</para>
    /// </summary>
    public interface ICardPlayResultContributor
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a result pile override, or <see langword="null" /> to keep the current result. The first non-null
        ///         contributor
        ///         result wins; additional overrides are reported when
        ///         <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> is enabled.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回目标牌堆覆盖；返回 <see langword="null" /> 表示保持当前结果。首个非空结果生效；
        ///         启用 <see cref="ModelCapabilityDiagnostics.ConflictLogs" /> 后会记录其他覆盖结果。
        ///     </para>
        /// </summary>
        PileType? GetResultPileTypeForCardPlay(CardModel card)
        {
            return null;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Optional card capability that can carry itself from an original card to a transform result.</para>
    ///     <para xml:lang="zh-CN">可选卡牌能力：可将自身从转化前卡牌携带到转化结果卡牌。</para>
    /// </summary>
    public interface ICardTransformCarryOverCapability
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns <see langword="true" /> when this capability should be copied to
        ///         <paramref name="replacement" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <see langword="true" /> 时，此能力会复制到 <paramref name="replacement" />。
        ///     </para>
        /// </summary>
        bool ShouldCarryOverToTransformResult(CardModel original, CardModel replacement)
        {
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the capability instance that should be attached to <paramref name="replacement" />.</para>
        ///     <para xml:lang="zh-CN">创建应附加到 <paramref name="replacement" /> 的能力实例。</para>
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
        private const string DynamicVarPreviewSurface = "card dynamic-var/preview";
        private const string DescriptionSurface = "card description/fragments";
        private const string EnergyCostSurface = "card energy-cost/local";
        private const string StarCostSurface = "card star-cost/local";
        private const string TitleSurface = "card display/title";
        private const string HoverTipsSurface = "card display/hover-tips";
        private const string OverlaySurface = "card display/overlay";
        private const string OverlayAssetPathsSurface = "card asset/overlay-paths";
        private const string GoldGlowSurface = "card display/gold-glow";
        private const string RedGlowSurface = "card display/red-glow";

        internal static bool HasTitleContributors(CardModel card)
        {
            return ModelCapabilityHost.HasCapabilities(card, ModelCapabilityInterfaceFlags.CardTitle);
        }

        internal static void ApplyTitleFragments(CardTitleContext context, ref string title)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    context.Card,
                    ModelCapabilityInterfaceFlags.CardTitle,
                    out var snapshot))
                return;

            List<OrderedTitleFragment> beforeFragments = [];
            List<OrderedTitleFragment> afterBaseFragments = [];
            List<OrderedTitleFragment> afterTitleFragments = [];
            OrderedTitleFragment? replacementFragment = null;
            ICardTitleContributor? replacementSource = null;
            var capabilityIndex = 0;
            foreach (var item in snapshot)
            {
                if (item is not ICardTitleContributor capability)
                    continue;

                var currentSourceIndex = capabilityIndex++;
                IReadOnlyList<CardTitleFragment> fragments;
                try
                {
                    fragments = [.. capability.GetTitleFragments(context) ?? []];
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
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardProperty,
                    out var snapshot))
                return current;

            var hasWinner = false;
            var winnerValue = default(CardType);
            ICardPropertyContributor? winner = null;

            foreach (var item in snapshot)
            {
                if (item is not ICardPropertyContributor capability)
                    continue;

                CardType? value;
                try
                {
                    value = capability.GetCardType(card);
                }
                catch (Exception ex)
                {
                    LogFailure(capability, card, CardTypeSurface, ex);
                    continue;
                }

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
                    CardTypeSurface,
                    card,
                    winner!,
                    winnerValue,
                    capability,
                    value.Value);
            }

            return hasWinner ? winnerValue : current;
        }

        internal static CardRarity ApplyCardRarity(CardModel card, CardRarity current)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardProperty,
                    out var snapshot))
                return current;

            var hasWinner = false;
            var winnerValue = default(CardRarity);
            ICardPropertyContributor? winner = null;

            foreach (var item in snapshot)
            {
                if (item is not ICardPropertyContributor capability)
                    continue;

                CardRarity? value;
                try
                {
                    value = capability.GetCardRarity(card);
                }
                catch (Exception ex)
                {
                    LogFailure(capability, card, CardRaritySurface, ex);
                    continue;
                }

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
                    CardRaritySurface,
                    card,
                    winner!,
                    winnerValue,
                    capability,
                    value.Value);
            }

            return hasWinner ? winnerValue : current;
        }

        internal static TargetType ApplyTargetType(CardModel card, TargetType current)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardProperty,
                    out var snapshot))
                return current;

            var hasWinner = false;
            var winnerValue = default(TargetType);
            ICardPropertyContributor? winner = null;

            foreach (var item in snapshot)
            {
                if (item is not ICardPropertyContributor capability)
                    continue;

                TargetType? value;
                try
                {
                    value = capability.GetTargetType(card);
                }
                catch (Exception ex)
                {
                    LogFailure(capability, card, TargetTypeSurface, ex);
                    continue;
                }

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
                    TargetTypeSurface,
                    card,
                    winner!,
                    winnerValue,
                    capability,
                    value.Value);
            }

            return hasWinner ? winnerValue : current;
        }

        internal static IEnumerable<CardTag> ApplyTags(CardModel card, IEnumerable<CardTag> current)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardProperty,
                    out var snapshot))
                return current;

            List<CardTag>? extraTags = null;
            foreach (var item in snapshot)
            {
                if (item is not ICardPropertyContributor capability)
                    continue;

                IReadOnlyList<CardTag> tags = [];
                try
                {
                    tags = capability.GetTags(card)?.ToArray() ?? [];
                }
                catch (Exception ex)
                {
                    LogFailure(capability, card, TagsSurface, ex);
                }

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
            return ModelCapabilityHost.HasCapabilities(card, ModelCapabilityInterfaceFlags.CardEnergyCost);
        }

        internal static bool HasStarCostContributors(CardModel card)
        {
            return ModelCapabilityHost.HasCapabilities(card, ModelCapabilityInterfaceFlags.CardStarCost);
        }

        internal static int ApplyEnergyCost(CardModel card, CostModifiers modifiers, int current)
        {
            if (!modifiers.HasFlag(CostModifiers.Local) ||
                card.EnergyCost.CostsX ||
                current < 0 ||
                !ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardEnergyCost,
                    out var snapshot))
                return current;

            var result = current;
            foreach (var item in snapshot)
            {
                if (item is not ICardEnergyCostContributor capability)
                    continue;

                try
                {
                    result = capability.ModifyEnergyCost(card, result, modifiers);
                }
                catch (Exception ex)
                {
                    LogFailure(capability, card, EnergyCostSurface, ex);
                }
            }

            return Math.Max(0, result);
        }

        internal static int ApplyStarCost(CardModel card, int current)
        {
            if (card.HasStarCostX ||
                !ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardStarCost,
                    out var snapshot))
                return current;

            var result = current;
            foreach (var item in snapshot)
            {
                if (item is not ICardStarCostContributor capability)
                    continue;

                try
                {
                    result = capability.ModifyStarCost(card, result);
                }
                catch (Exception ex)
                {
                    LogFailure(capability, card, StarCostSurface, ex);
                }
            }

            return result < 0 ? result : Math.Max(0, result);
        }

        internal static bool ApplyCanPlay(CardModel card, bool current)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardPlayState,
                    out var snapshot))
                return current;

            var result = current;
            var hasPrevious = false;
            var previousValue = false;
            ICardPlayStateContributor? previous = null;

            foreach (var item in snapshot)
            {
                if (item is not ICardPlayStateContributor capability)
                    continue;

                bool? value;
                try
                {
                    value = capability.CanPlay(card);
                }
                catch (Exception ex)
                {
                    LogFailure(capability, card, CanPlaySurface, ex);
                    continue;
                }

                if (!value.HasValue)
                    continue;

                if (hasPrevious)
                    ModelCapabilityDiagnostics.WarnSurfaceConflict(
                        CanPlaySurface,
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

        internal static bool HasTurnEndInHandEffect(CardModel card)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardPlayState,
                    out var snapshot))
                return false;

            foreach (var item in snapshot)
            {
                if (item is not ICardPlayStateContributor capability)
                    continue;

                try
                {
                    if (capability.HasTurnEndInHandEffect(card))
                        return true;
                }
                catch (Exception ex)
                {
                    LogFailure(capability, card, TurnEndInHandSurface, ex);
                }
            }

            return false;
        }

        internal static PileType ApplyResultPileTypeForCardPlay(CardModel card, PileType current)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardPlayResult,
                    out var snapshot))
                return current;

            var hasWinner = false;
            var winnerValue = default(PileType);
            ICardPlayResultContributor? winner = null;

            foreach (var item in snapshot)
            {
                if (item is not ICardPlayResultContributor capability)
                    continue;

                PileType? value;
                try
                {
                    value = capability.GetResultPileTypeForCardPlay(card);
                }
                catch (Exception ex)
                {
                    LogFailure(capability, card, ResultPileSurface, ex);
                    continue;
                }

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
                    ResultPileSurface,
                    card,
                    winner!,
                    winnerValue,
                    capability,
                    value.Value);
            }

            return hasWinner ? winnerValue : current;
        }

        internal static void CarryOverTransformCapabilities(CardModel original, CardModel? replacement)
        {
            if (replacement == null || ReferenceEquals(original, replacement))
                return;

            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    original,
                    ModelCapabilityInterfaceFlags.CardTransformCarryOver,
                    out var snapshot))
                return;

            foreach (var item in snapshot)
            {
                if (item is not ICardTransformCarryOverCapability capability)
                    continue;

                if (!capability.ShouldCarryOverToTransformResult(original, replacement))
                    continue;

                var carried = capability.CreateCarryOverCapability(original, replacement);
                if (carried.Owner != null)
                {
                    if (!ReferenceEquals(carried.Owner, replacement))
                        throw new InvalidOperationException(
                            $"Transform carry-over capability '{carried.CapabilityId}' was attached to an " +
                            "unexpected model.");

                    carried.Detach(true);
                }

                replacement.Capabilities().Apply(carried);
            }
        }

        internal static void UpdateDynamicVarPreviews(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.ModelDynamicVar,
                    out var snapshot))
                return;

            foreach (var item in snapshot)
            {
                if (item is not IModelDynamicVarContributor capability)
                    continue;

                DynamicVarSet? dynamicVars = null;
                try
                {
                    dynamicVars = capability.GetDynamicVars(card);
                }
                catch (Exception ex)
                {
                    LogFailure(capability, card, DynamicVarPreviewSurface, ex);
                }

                if (dynamicVars == null)
                    continue;

                dynamicVars.ClearPreview();
                card.UpdateDynamicVarPreview(previewMode, target, dynamicVars);
            }
        }

        internal static void ApplyDescriptionFragments(
            CardModel card,
            PileType pileType,
            object? previewType,
            Creature? target,
            List<string> lines)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardDescription,
                    out var snapshot))
                return;

            var context = new CardDescriptionContext(
                card,
                pileType,
                target,
                string.Equals(previewType?.ToString(), "Upgrade", StringComparison.Ordinal));

            List<OrderedDescriptionFragment> beforeFragments = [];
            List<OrderedDescriptionFragment> afterFragments = [];
            var capabilityIndex = 0;
            foreach (var item in snapshot)
            {
                if (item is not ICardDescriptionContributor capability)
                    continue;

                var currentSourceIndex = capabilityIndex++;
                IReadOnlyList<CardDescriptionFragment> fragments;
                try
                {
                    fragments = [.. capability.GetDescriptionFragments(context) ?? []];
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

        internal static void ResetOwnerCardDynamicVarsForDowngrade(CardModel card)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardCapability,
                    out var snapshot))
                return;

            foreach (var item in snapshot)
                if (item is CardCapability capability && IsStillAttachedToCard(capability, card))
                    capability.ResetDynamicVarsForDowngrade();
        }

        internal static void AfterOwnerCardUpgraded(CardModel card)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardCapability,
                    out var snapshot))
                return;

            foreach (var item in snapshot)
            {
                if (item is not CardCapability capability || !IsStillAttachedToCard(capability, card))
                    continue;

                capability.NotifyOwnerCardUpgraded(card);
                if (!IsStillAttachedToCard(capability, card))
                    continue;

                capability.RecalculateDynamicVarsForUpgradeOrEnchant();
            }
        }

        internal static void AfterOwnerCardUpgradeFinalized(CardModel card)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardCapability,
                    out var snapshot))
                return;

            foreach (var item in snapshot)
            {
                if (item is not CardCapability capability || !IsStillAttachedToCard(capability, card))
                    continue;

                capability.FinalizeDynamicVarUpgrade();
                if (!IsStillAttachedToCard(capability, card))
                    continue;

                capability.NotifyOwnerCardUpgradeFinalized(card);
            }
        }

        internal static void AfterOwnerCardDowngraded(CardModel card)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardCapability,
                    out var snapshot))
                return;

            foreach (var item in snapshot)
            {
                if (item is not CardCapability capability || !IsStillAttachedToCard(capability, card))
                    continue;

                capability.NotifyOwnerCardDowngraded(card);
            }
        }

        internal static void AfterOwnerCardTransformedFrom(CardModel card)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardCapability,
                    out var snapshot))
                return;

            foreach (var item in snapshot)
            {
                if (item is not CardCapability capability || !IsStillAttachedToCard(capability, card))
                    continue;

                capability.NotifyOwnerCardTransformedFrom(card);
            }
        }

        internal static void AfterOwnerCardTransformedTo(CardModel card)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardCapability,
                    out var snapshot))
                return;

            foreach (var item in snapshot)
            {
                if (item is not CardCapability capability || !IsStillAttachedToCard(capability, card))
                    continue;

                capability.NotifyOwnerCardTransformedTo(card);
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
            if (!ModelCapabilityHost.HasCapabilities(
                    card,
                    ModelCapabilityInterfaceFlags.ModelHoverTip | ModelCapabilityInterfaceFlags.CardHoverTip))
                yield break;

            foreach (var tip in ModelCapabilityHost.GetHoverTips(card))
                yield return tip;

            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardHoverTip,
                    out var snapshot))
                yield break;

            foreach (var item in snapshot)
            {
                if (item is not ICardHoverTipContributor capability)
                    continue;

                IReadOnlyList<IHoverTip> tips = [];
                try
                {
                    tips = capability.GetHoverTips(card)?.ToArray() ?? [];
                }
                catch (Exception ex)
                {
                    LogFailure(capability, card, HoverTipsSurface, ex);
                }

                foreach (var tip in tips)
                    yield return tip;
            }
        }

        internal static bool HasHoverTipContributors(CardModel card)
        {
            return ModelCapabilityHost.HasCapabilities(
                card,
                ModelCapabilityInterfaceFlags.ModelHoverTip | ModelCapabilityInterfaceFlags.CardHoverTip);
        }

        internal static IEnumerable<OrderedCardOverlayContribution> GetOverlayContributions(
            CardOverlayContext context)
        {
            var sourceIndex = 0;
            if (context.Card is ICardOverlayContributor cardContributor)
                foreach (var contribution in GetOverlayContributions(context, cardContributor, sourceIndex++))
                    yield return contribution;

            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    context.Card,
                    ModelCapabilityInterfaceFlags.CardOverlay,
                    out var snapshot))
                yield break;

            foreach (var item in snapshot)
                if (item is ICardOverlayContributor capability)
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
                () => contributions = [.. source.GetCardOverlays(context) ?? []]);

            foreach (var contribution in contributions)
            {
                if (contribution == null)
                {
                    LogFailure(
                        source,
                        context.Card,
                        OverlaySurface,
                        new InvalidOperationException("Card overlay contributors cannot return null entries."));
                    continue;
                }

                yield return new(source, contribution, sourceIndex);
            }
        }

        internal static IEnumerable<string> GetOverlayAssetPaths(CardModel card)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardOverlayAssetPath,
                    out var snapshot))
                yield break;

            foreach (var item in snapshot)
            {
                if (item is not ICardOverlayAssetPathContributor capability)
                    continue;

                IReadOnlyList<string> paths = [];
                try
                {
                    paths = capability.GetCardOverlayAssetPaths(card)?.ToArray() ?? [];
                }
                catch (Exception ex)
                {
                    LogFailure(capability, card, OverlayAssetPathsSurface, ex);
                }

                foreach (var path in paths)
                    yield return path;
            }
        }

        internal static bool ShouldGlowGold(CardModel card)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardGlow,
                    out var snapshot))
                return false;

            foreach (var item in snapshot)
            {
                if (item is not ICardGlowContributor capability)
                    continue;

                try
                {
                    if (capability.ShouldGlowGold(card))
                        return true;
                }
                catch (Exception ex)
                {
                    LogFailure(capability, card, GoldGlowSurface, ex);
                }
            }

            return false;
        }

        internal static bool ShouldGlowRed(CardModel card)
        {
            if (!ModelCapabilityHost.TryGetCapabilitySnapshot(
                    card,
                    ModelCapabilityInterfaceFlags.CardGlow,
                    out var snapshot))
                return false;

            foreach (var item in snapshot)
            {
                if (item is not ICardGlowContributor capability)
                    continue;

                try
                {
                    if (capability.ShouldGlowRed(card))
                        return true;
                }
                catch (Exception ex)
                {
                    LogFailure(capability, card, RedGlowSurface, ex);
                }
            }

            return false;
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
