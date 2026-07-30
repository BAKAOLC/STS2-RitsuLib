#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.FreePlay;
using STS2RitsuLib.Models;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">Specifies how long an attached secondary-resource card cost remains active.</para>
    ///     <para xml:lang="zh-CN">指定卡牌附加的次级资源费用保持生效的时长。</para>
    /// </summary>
    public enum SecondaryResourceCostDuration
    {
        /// <summary>
        ///     <para xml:lang="en">Persists until explicitly replaced or cleared.</para>
        ///     <para xml:lang="zh-CN">持续生效，直至被显式替换或清除。</para>
        /// </summary>
        Permanent,

        /// <summary>
        ///     <para xml:lang="en">Clears after the card is next played successfully.</para>
        ///     <para xml:lang="zh-CN">在卡牌下一次成功打出后清除。</para>
        /// </summary>
        UntilPlayed,

        /// <summary>
        ///     <para xml:lang="en">Clears at the end of the current turn.</para>
        ///     <para xml:lang="zh-CN">在当前回合结束时清除。</para>
        /// </summary>
        ThisTurn,

        /// <summary>
        ///     <para xml:lang="en">Lasts for the lifetime of the card's current combat instance.</para>
        ///     <para xml:lang="zh-CN">在卡牌当前战斗实例的生命周期内持续生效。</para>
        /// </summary>
        ThisCombat,
    }

    /// <summary>
    ///     <para xml:lang="en">Describes a fixed or X cost paid with one secondary resource.</para>
    ///     <para xml:lang="zh-CN">描述使用一种次级资源支付的固定费用或 X 费用。</para>
    /// </summary>
    public sealed record SecondaryResourceCost(
        int Amount,
        bool CostsX = false,
        int XMultiplier = 1)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets a fixed cost of zero.</para>
        ///     <para xml:lang="zh-CN">获取数值为零的固定费用。</para>
        /// </summary>
        public static SecondaryResourceCost Free { get; } = new(0);

        /// <summary>
        ///     <para xml:lang="en">Gets whether the descriptor represents a payment-bearing cost.</para>
        ///     <para xml:lang="zh-CN">获取该描述是否表示一项需要支付的费用。</para>
        /// </summary>
        public bool IsMaterial => CostsX || Amount > 0;

        /// <summary>
        ///     <para xml:lang="en">Creates an X-cost descriptor with the specified value multiplier.</para>
        ///     <para xml:lang="zh-CN">创建一项使用指定数值倍率的 X 费用描述。</para>
        /// </summary>
        public static SecondaryResourceCost X(int multiplier = 1)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(multiplier);
            return new(0, true, multiplier);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Stores layered secondary-resource costs attached to one card.</para>
    ///     <para xml:lang="zh-CN">存储附加到一张卡牌上的分层次级资源费用。</para>
    /// </summary>
    public sealed class SecondaryResourceCostSet
    {
        private readonly Dictionary<string, List<SecondaryResourceCostLayer>> _costs =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     <para xml:lang="en">Gets whether at least one active cost can require payment.</para>
        ///     <para xml:lang="zh-CN">获取是否至少有一项当前费用可能需要支付。</para>
        /// </summary>
        public bool HasCosts =>
            _costs.Values.SelectMany(static layers => layers).Any(static layer => layer.Cost.IsMaterial);

        /// <summary>
        ///     <para xml:lang="en">Gets the resource identifiers that currently have attached cost layers.</para>
        ///     <para xml:lang="zh-CN">获取当前存在附加费用层的资源标识符。</para>
        /// </summary>
        public IReadOnlyList<string> ResourceIds =>
            [.. _costs.Keys.OrderBy(static id => id, StringComparer.Ordinal)];

        internal bool HasLayers => _costs.Count > 0;

        internal bool HasPermanentLayers =>
            _costs.Values
                .SelectMany(static layers => layers)
                .Any(static layer => layer.Duration == SecondaryResourceCostDuration.Permanent);

        /// <summary>
        ///     <para xml:lang="en">Occurs after the attached costs change.</para>
        ///     <para xml:lang="zh-CN">在附加费用发生变化后触发。</para>
        /// </summary>
        public event Action? Changed;

        /// <summary>
        ///     <para xml:lang="en">Sets a persistent fixed cost for one resource.</para>
        ///     <para xml:lang="zh-CN">为一种资源设置持续生效的固定费用。</para>
        /// </summary>
        public SecondaryResourceCostSet Set(string resourceId, int amount)
        {
            return Set(resourceId, new SecondaryResourceCost(Math.Max(0, amount)));
        }

        /// <summary>
        ///     <para xml:lang="en">Sets a persistent cost descriptor for one resource.</para>
        ///     <para xml:lang="zh-CN">为一种资源设置持续生效的费用描述。</para>
        /// </summary>
        public SecondaryResourceCostSet Set(string resourceId, SecondaryResourceCost cost)
        {
            return Set(resourceId, cost, SecondaryResourceCostDuration.Permanent);
        }

        /// <summary>
        ///     <para xml:lang="en">Sets a cost descriptor for one resource at the specified duration.</para>
        ///     <para xml:lang="zh-CN">为一种资源设置具有指定持续时间的费用描述。</para>
        /// </summary>
        public SecondaryResourceCostSet Set(
            string resourceId,
            SecondaryResourceCost cost,
            SecondaryResourceCostDuration duration)
        {
            return Set(resourceId, cost, duration, null);
        }

        /// <summary>
        ///     <para xml:lang="en">Sets a persistent fixed cost whose shortfall does not prevent card play.</para>
        ///     <para xml:lang="zh-CN">设置一项持续生效且费用缺口不会阻止出牌的固定费用。</para>
        /// </summary>
        public SecondaryResourceCostSet SetAllowingShortfall(
            string resourceId,
            int amount,
            SecondaryResourceShortfallPaymentHandler? onShortfall = null,
            bool spendAvailable = true,
            SecondaryResourceShortfallResolver? resolveShortfall = null)
        {
            return SetAllowingShortfall(
                resourceId,
                new SecondaryResourceCost(Math.Max(0, amount)),
                onShortfall,
                spendAvailable,
                resolveShortfall);
        }

        /// <summary>
        ///     <para xml:lang="en">Sets a cost whose shortfall does not prevent card play.</para>
        ///     <para xml:lang="zh-CN">设置一项费用缺口不会阻止出牌的费用。</para>
        /// </summary>
        public SecondaryResourceCostSet SetAllowingShortfall(
            string resourceId,
            SecondaryResourceCost cost,
            SecondaryResourceShortfallPaymentHandler? onShortfall = null,
            bool spendAvailable = true,
            SecondaryResourceShortfallResolver? resolveShortfall = null,
            SecondaryResourceCostDuration duration = SecondaryResourceCostDuration.Permanent)
        {
            return Set(
                resourceId,
                cost,
                duration,
                SecondaryResourceInsufficientPayment.AllowPlay(onShortfall, spendAvailable, resolveShortfall));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets a cost descriptor for one resource with the specified duration and insufficient-payment
        ///         policy.
        ///     </para>
        ///     <para xml:lang="zh-CN">为一种资源设置具有指定持续时间及资源不足支付策略的费用描述。</para>
        /// </summary>
        public SecondaryResourceCostSet Set(
            string resourceId,
            SecondaryResourceCost cost,
            SecondaryResourceCostDuration duration,
            SecondaryResourceInsufficientPayment? insufficientPayment)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            ArgumentNullException.ThrowIfNull(cost);
            ValidateCost(cost);

            var layers = GetLayers(resourceId);
            layers.RemoveAll(layer => layer.Duration == duration);
            layers.Add(new(cost, duration, insufficientPayment));
            Changed?.Invoke();
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Clears every cost layer for one resource.</para>
        ///     <para xml:lang="zh-CN">清除一种资源的所有费用层。</para>
        /// </summary>
        public bool Clear(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            var removed = _costs.Remove(resourceId.Trim());
            if (removed)
                Changed?.Invoke();

            return removed;
        }

        /// <summary>
        ///     <para xml:lang="en">Clears every cost layer with the specified duration.</para>
        ///     <para xml:lang="zh-CN">清除所有具有指定持续时间的费用层。</para>
        /// </summary>
        public bool ClearDuration(SecondaryResourceCostDuration duration)
        {
            var changed = false;
            foreach (var resourceId in _costs.Keys.ToArray())
            {
                changed |= _costs[resourceId].RemoveAll(layer => layer.Duration == duration) > 0;
                if (_costs[resourceId].Count == 0)
                    _costs.Remove(resourceId);
            }

            if (changed)
                Changed?.Invoke();

            return changed;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the active cost descriptor for one resource.</para>
        ///     <para xml:lang="zh-CN">获取一种资源当前生效的费用描述。</para>
        /// </summary>
        public SecondaryResourceCost Get(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            if (!_costs.TryGetValue(resourceId.Trim(), out var layers) || layers.Count == 0)
                return SecondaryResourceCost.Free;

            return layers[^1].Cost;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the active payment-bearing costs in deterministic resource order.</para>
        ///     <para xml:lang="zh-CN">按确定的资源顺序返回当前需要支付的费用。</para>
        /// </summary>
        public IReadOnlyDictionary<string, SecondaryResourceCost> Snapshot()
        {
            return _costs
                .Select(pair => new KeyValuePair<string, SecondaryResourceCost>(pair.Key, pair.Value[^1].Cost))
                .Where(static pair => pair.Value.IsMaterial)
                .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }

        internal IReadOnlyList<SecondaryResourcePlayUse> SnapshotUses()
        {
            return
            [
                .. _costs
                    .Select(static pair =>
                    {
                        var layer = pair.Value[^1];
                        var permanentCost = pair.Value.LastOrDefault(static candidate =>
                            candidate.Duration == SecondaryResourceCostDuration.Permanent)?.Cost ?? layer.Cost;
                        return new SecondaryResourcePlayUse(
                            pair.Key,
                            pair.Key,
                            layer.Cost,
                            SecondaryResourceUseKind.RequiredCost)
                        {
                            Duration = layer.Duration,
                            BaseCost = permanentCost,
                            InsufficientPayment = layer.InsufficientPayment,
                        };
                    })
                    .Where(static use => use.IsMaterial)
                    .OrderBy(static use => use.Id, StringComparer.Ordinal),
            ];
        }

        internal SecondaryResourceCostSet Clone()
        {
            var clone = new SecondaryResourceCostSet();
            foreach (var (resourceId, layers) in _costs)
                clone._costs[resourceId] = [.. layers];

            return clone;
        }

        internal bool ResetPermanentLayersFrom(SecondaryResourceCostSet? canonicalCosts)
        {
            var changed = false;
            foreach (var resourceId in _costs.Keys.ToArray())
            {
                changed |= _costs[resourceId].RemoveAll(static layer =>
                    layer.Duration == SecondaryResourceCostDuration.Permanent) > 0;
                if (_costs[resourceId].Count == 0)
                    _costs.Remove(resourceId);
            }

            if (canonicalCosts != null)
                foreach (var (resourceId, canonicalLayers) in canonicalCosts._costs)
                {
                    var permanentLayers = canonicalLayers
                        .Where(static layer => layer.Duration == SecondaryResourceCostDuration.Permanent)
                        .ToArray();
                    if (permanentLayers.Length == 0)
                        continue;

                    if (_costs.TryGetValue(resourceId, out var layers))
                        layers.InsertRange(0, permanentLayers);
                    else
                        _costs[resourceId] = [.. permanentLayers];
                    changed = true;
                }

            if (changed)
                Changed?.Invoke();

            return changed;
        }

        private List<SecondaryResourceCostLayer> GetLayers(string resourceId)
        {
            var id = resourceId.Trim();
            if (_costs.TryGetValue(id, out var layers))
                return layers;

            layers = [];
            _costs[id] = layers;

            return layers;
        }

        private static void ValidateCost(SecondaryResourceCost cost)
        {
            if (cost is { CostsX: true, XMultiplier: <= 0 })
                throw new ArgumentOutOfRangeException(
                    nameof(cost),
                    "An X secondary-resource cost must have a positive multiplier.");
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Provides access to secondary-resource costs attached to cards.</para>
    ///     <para xml:lang="zh-CN">提供对卡牌附加次级资源费用的访问。</para>
    /// </summary>
    public static partial class SecondaryResourceCardExtensions
    {
        private static readonly AttachedState<CardModel, SecondaryResourceCostSet> CostSets = new(() => new());

        /// <summary>
        ///     <para xml:lang="en">Gets or creates the secondary-resource cost set attached to this card.</para>
        ///     <para xml:lang="zh-CN">获取或创建附加到此卡牌的次级资源费用集合。</para>
        /// </summary>
        public static SecondaryResourceCostSet SecondaryCosts(this CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            return CostSets.GetOrCreate(card);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get the attached cost set without creating one.</para>
        ///     <para xml:lang="zh-CN">尝试获取已附加的费用集合，且不会创建新集合。</para>
        /// </summary>
        public static bool TryGetSecondaryCosts(this CardModel card, out SecondaryResourceCostSet costs)
        {
            ArgumentNullException.ThrowIfNull(card);
            return CostSets.TryGetValue(card, out costs!);
        }

        /// <summary>
        ///     <para xml:lang="en">Clears costs that last until the card is played.</para>
        ///     <para xml:lang="zh-CN">清除持续到卡牌打出为止的费用。</para>
        /// </summary>
        public static bool ClearSecondaryCostsUntilPlayed(this CardModel card)
        {
            var changed = card.TryGetSecondaryCosts(out var costs) &&
                          costs.ClearDuration(SecondaryResourceCostDuration.UntilPlayed);
            return card.ClearSecondaryResourceUsesUntilPlayed() || changed;
        }

        /// <summary>
        ///     <para xml:lang="en">Clears costs that last for the current turn.</para>
        ///     <para xml:lang="zh-CN">清除仅在当前回合生效的费用。</para>
        /// </summary>
        public static bool ClearSecondaryCostsThisTurn(this CardModel card)
        {
            var changed = card.TryGetSecondaryCosts(out var costs) &&
                          costs.ClearDuration(SecondaryResourceCostDuration.ThisTurn);
            return card.ClearSecondaryResourceUsesThisTurn() || changed;
        }

        internal static bool HasMaterialSecondaryCosts(this CardModel card)
        {
            return card.HasMaterialSecondaryResourceWork();
        }

        internal static bool CopySecondaryCostsTo(this CardModel source, CardModel destination)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);

            if (!source.TryGetSecondaryCosts(out var costs) || !costs.HasLayers)
                return false;

            CostSets.Set(destination, costs.Clone());
            return true;
        }

        internal static void ResetSecondaryResourcesForDowngrade(this CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            var canonical = ModelDb.GetById<CardModel>(card.Id).ToMutable();
            canonical.ResetSecondaryCostsForDowngradeFrom(card);
            canonical.ResetSecondaryResourceUsesForDowngradeFrom(card);
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private static bool ResetSecondaryCostsForDowngradeFrom(
            this CardModel canonical,
            CardModel card)
        {
            ArgumentNullException.ThrowIfNull(canonical);
            ArgumentNullException.ThrowIfNull(card);

            var hasCanonicalCosts = canonical.TryGetSecondaryCosts(out var canonicalCosts) &&
                                    canonicalCosts.HasPermanentLayers;
            // ReSharper disable once InvertIf
            if (!card.TryGetSecondaryCosts(out var costs))
            {
                if (!hasCanonicalCosts)
                    return false;

                costs = CostSets.Set(card, new());
            }

            return costs.ResetPermanentLayersFrom(hasCanonicalCosts ? canonicalCosts : null);
        }
    }

    internal static class SecondaryResourceCloneBridge
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
                return;

            ModelCloneRegistry.For(Const.ModId)
                .Register<CardModel>("secondary_resource_costs", CopySecondaryCosts);
            _initialized = true;
        }

        private static void CopySecondaryCosts(CardModel prototype, CardModel clone)
        {
            prototype.CopySecondaryCostsTo(clone);
            prototype.CopySecondaryResourceUsesTo(clone);
        }
    }

    internal sealed record SecondaryResourceCostLayer(
        SecondaryResourceCost Cost,
        SecondaryResourceCostDuration Duration,
        SecondaryResourceInsufficientPayment? InsufficientPayment = null);

    /// <summary>
    ///     <para xml:lang="en">Describes one resolved secondary-resource payment entry in a card-play plan.</para>
    ///     <para xml:lang="zh-CN">描述出牌计划中一项已解析的次级资源支付条目。</para>
    /// </summary>
    public sealed record SecondaryResourcePaymentLine(
        string ResourceId,
        SecondaryResourceDefinition Definition,
        int Cost,
        int AmountAvailable,
        int AmountToSpend,
        int Value,
        bool CostsX,
        bool IsFree)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets whether the entry is a preview, is free, or has enough primary resource.</para>
        ///     <para xml:lang="zh-CN">获取该条目是否为预览、免费，或拥有足够的主要支付资源。</para>
        /// </summary>
        public bool IsAffordable => IsPreview || IsFree || AmountAvailable >= Cost;

        /// <summary>
        ///     <para xml:lang="en">Gets the shortfall remaining after planned replacement payments.</para>
        ///     <para xml:lang="zh-CN">获取规划的替代支付完成后仍剩余的费用缺口。</para>
        /// </summary>
        public int Shortfall { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the shortfall before any replacement payment is applied.</para>
        ///     <para xml:lang="zh-CN">获取应用任何替代支付前的原始费用缺口。</para>
        /// </summary>
        public int OriginalShortfall { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the amount of the original shortfall covered by replacement payments.</para>
        ///     <para xml:lang="zh-CN">获取替代支付在原始费用缺口中补足的数量。</para>
        /// </summary>
        public int CoveredShortfall { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the insufficient-payment policy selected for this entry.</para>
        ///     <para xml:lang="zh-CN">获取为该条目选定的资源不足支付策略。</para>
        /// </summary>
        public SecondaryResourceInsufficientPayment InsufficientPayment { get; init; } =
            SecondaryResourceInsufficientPayment.BlockPlay;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the resource-payment hooks permit this entry's planned spend.</para>
        ///     <para xml:lang="zh-CN">获取资源支付钩子是否允许该条目的规划消耗。</para>
        /// </summary>
        public bool SpendAllowed { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets whether this entry can execute its planned resource spend.</para>
        ///     <para xml:lang="zh-CN">获取该条目是否可以执行规划的资源消耗。</para>
        /// </summary>
        public bool CanSpend => IsPreview || IsFree || AmountToSpend <= 0 || SpendAllowed;

        /// <summary>
        ///     <para xml:lang="en">Gets whether this entry cannot by itself prevent the card from being played.</para>
        ///     <para xml:lang="zh-CN">获取该条目是否不会单独阻止卡牌打出。</para>
        /// </summary>
        public bool IsOptional => !BlocksPlay;

        /// <summary>
        ///     <para xml:lang="en">Gets whether this entry is a repeatable extra payment.</para>
        ///     <para xml:lang="zh-CN">获取该条目是否为可重复的额外支付。</para>
        /// </summary>
        public bool IsExtraSpend => Kind == SecondaryResourceUseKind.ExtraSpend;

        /// <summary>
        ///     <para xml:lang="en">Gets whether this entry permits the card play to proceed.</para>
        ///     <para xml:lang="zh-CN">获取该条目是否允许继续打出卡牌。</para>
        /// </summary>
        public bool CanPlay => !BlocksPlay || ((IsAffordable || IsShortfallPlayable || IsShortfallCovered) && CanSpend);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether this required entry has a resource shortfall that its policy permits.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取该必需条目是否存在其支付策略所允许的资源缺口。</para>
        /// </summary>
        public bool IsShortfallPlayable =>
            BlocksPlay &&
            Activated &&
            Shortfall > 0 &&
            InsufficientPayment.AllowsPlay;

        /// <summary>
        ///     <para xml:lang="en">Gets whether replacement payments cover the entire original shortfall.</para>
        ///     <para xml:lang="zh-CN">获取替代支付是否已补足全部原始费用缺口。</para>
        /// </summary>
        public bool IsShortfallCovered =>
            BlocksPlay &&
            Activated &&
            OriginalShortfall > 0 &&
            CoveredShortfall >= OriginalShortfall;

        /// <summary>
        ///     <para xml:lang="en">Gets the replacement-payment plan selected for this entry.</para>
        ///     <para xml:lang="zh-CN">获取为该条目选定的替代支付方案。</para>
        /// </summary>
        public SecondaryResourceShortfallResolution ShortfallResolution { get; init; } =
            SecondaryResourceShortfallResolution.None;

        /// <summary>
        ///     <para xml:lang="en">Gets the number of complete extra-payment units bought by this entry.</para>
        ///     <para xml:lang="zh-CN">获取该条目购买的完整额外支付单位数。</para>
        /// </summary>
        public int ExtraStacks { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the total resource amount spent on repeatable extra payments.</para>
        ///     <para xml:lang="zh-CN">获取用于可重复额外支付的资源总量。</para>
        /// </summary>
        public int ExtraAmountToSpend { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the stable payment-use identifier represented by this entry.</para>
        ///     <para xml:lang="zh-CN">获取该条目所表示的稳定支付条款标识符。</para>
        /// </summary>
        public string UseId { get; init; } = ResourceId;

        /// <summary>
        ///     <para xml:lang="en">Gets the payment role of this entry.</para>
        ///     <para xml:lang="zh-CN">获取该条目的支付用途。</para>
        /// </summary>
        public SecondaryResourceUseKind Kind { get; init; } = SecondaryResourceUseKind.RequiredCost;

        /// <summary>
        ///     <para xml:lang="en">Gets whether failure to pay this entry can prevent the card from being played.</para>
        ///     <para xml:lang="zh-CN">获取该条目无法支付时是否可能阻止卡牌打出。</para>
        /// </summary>
        public bool BlocksPlay { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets whether this entry is active in the current card-play plan.</para>
        ///     <para xml:lang="zh-CN">获取该条目是否已在当前出牌计划中激活。</para>
        /// </summary>
        public bool Activated { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether this entry was resolved without an owning player for display only.</para>
        ///     <para xml:lang="zh-CN">获取该条目是否在没有所属玩家的情况下解析，仅供显示使用。</para>
        /// </summary>
        public bool IsPreview { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the current unmodified fixed cost used for display-color comparison.</para>
        ///     <para xml:lang="zh-CN">获取用于比较显示颜色的当前未修正固定费用。</para>
        /// </summary>
        public int BaseCost { get; init; } = Cost;

        /// <summary>
        ///     <para xml:lang="en">Gets the fixed cost before the upgrade currently being previewed, if known.</para>
        ///     <para xml:lang="zh-CN">获取当前预览的升级生效前的固定费用（如果已知）。</para>
        /// </summary>
        public int? UpgradePreviewBaseCost { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether a runtime effect participates in this entry's displayed fixed cost.</para>
        ///     <para xml:lang="zh-CN">获取运行时效果是否参与计算该条目的显示固定费用。</para>
        /// </summary>
        public bool HasRuntimeCostModifier { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Describes the resolved secondary-resource payment plan for one card play.</para>
    ///     <para xml:lang="zh-CN">描述一次出牌所采用的已解析次级资源支付计划。</para>
    /// </summary>
    public sealed record SecondaryResourcePaymentPlan(
        CardModel Card,
        Player? Player,
        bool IsFree,
        IReadOnlyList<SecondaryResourcePaymentLine> Lines)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets whether every payment entry permits the card to be played.</para>
        ///     <para xml:lang="zh-CN">获取每项支付条目是否都允许打出卡牌。</para>
        /// </summary>
        public bool IsAffordable => Lines.All(static line => line.CanPlay);

        /// <summary>
        ///     <para xml:lang="en">Gets whether the plan contains at least one payment entry.</para>
        ///     <para xml:lang="zh-CN">获取该计划是否至少包含一项支付条目。</para>
        /// </summary>
        public bool HasLines => Lines.Count > 0;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the plan was resolved without an owning player and cannot be committed.</para>
        ///     <para xml:lang="zh-CN">获取该计划是否在没有所属玩家的情况下解析，因而无法提交。</para>
        /// </summary>
        public bool IsPreview => Player == null;

        /// <summary>
        ///     <para xml:lang="en">Creates a payment plan with no secondary-resource entries.</para>
        ///     <para xml:lang="zh-CN">创建一项不包含次级资源支付条目的计划。</para>
        /// </summary>
        public static SecondaryResourcePaymentPlan Empty(CardModel card, Player? player, bool isFree = false)
        {
            return new(card, player, isFree, []);
        }
    }

    internal readonly record struct SecondaryResourcePaymentFreeMode(
        bool FixedCostsFree,
        bool XCostsFree)
    {
        public static SecondaryResourcePaymentFreeMode None { get; } = new(false, false);
        public static SecondaryResourcePaymentFreeMode AllCosts { get; } = new(true, true);
        public static SecondaryResourcePaymentFreeMode AutoPlayCapture { get; } = new(true, false);

        public bool IsFree => FixedCostsFree || XCostsFree;

        public static SecondaryResourcePaymentFreeMode FromCardCostScope(FreePlayCardCostScope scope)
        {
            return new(scope.FixedSecondaryCostsFree, scope.XSecondaryCostsFree);
        }

        public bool AppliesTo(SecondaryResourceCost cost)
        {
            return cost.CostsX ? XCostsFree : FixedCostsFree;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Builds and commits secondary-resource payment plans for card plays.</para>
    ///     <para xml:lang="zh-CN">构建并提交出牌所需的次级资源支付计划。</para>
    /// </summary>
    public static class SecondaryResourcePaymentResolver
    {
        /// <summary>
        ///     <para xml:lang="en">Resolves the card's secondary-resource payment plan.</para>
        ///     <para xml:lang="zh-CN">解析该卡牌的次级资源支付计划。</para>
        /// </summary>
        public static SecondaryResourcePaymentPlan Plan(
            CardModel card,
            bool isFree = false,
            AbstractModel? source = null)
        {
            return Plan(
                card,
                isFree ? SecondaryResourcePaymentFreeMode.AllCosts : SecondaryResourcePaymentFreeMode.None,
                source);
        }

        internal static SecondaryResourcePaymentPlan Plan(
            CardModel card,
            SecondaryResourcePaymentFreeMode freeMode,
            AbstractModel? source = null)
        {
            ArgumentNullException.ThrowIfNull(card);
            return Plan(card, card.CombatState, freeMode, source);
        }

        internal static SecondaryResourcePaymentPlan PlanForCombat(
            CardModel card,
            CombatStateLike combatState,
            SecondaryResourcePaymentFreeMode freeMode,
            AbstractModel? source = null)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentNullException.ThrowIfNull(combatState);
            return Plan(card, combatState, freeMode, source);
        }

        private static SecondaryResourcePaymentPlan Plan(
            CardModel card,
            CombatStateLike? combatState,
            SecondaryResourcePaymentFreeMode freeMode,
            AbstractModel? source)
        {
            ArgumentNullException.ThrowIfNull(card);

            var player = TryGetOwner(card);
            if (!ModSecondaryResourceRegistry.HasAny)
                return SecondaryResourcePaymentPlan.Empty(card, player, freeMode.IsFree);

            var uses = SnapshotUses(card);
            if (uses.Count == 0)
                return SecondaryResourcePaymentPlan.Empty(card, player, freeMode.IsFree);

            if (player == null || combatState == null)
                return PlanPreview(card, uses, freeMode);

            var lines = new List<SecondaryResourcePaymentLine>();
            var remainingByResource = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var use in uses)
            {
                if (!ModSecondaryResourceRegistry.TryGet(use.ResourceId, out var definition))
                    continue;

                if (!remainingByResource.TryGetValue(definition.Id, out var available))
                {
                    available = SecondaryResourceCmd.Get(player, definition.Id);
                    remainingByResource[definition.Id] = available;
                }

                var line = ResolveLine(combatState, player, card, definition, use, available, freeMode, source);
                lines.Add(line);
                remainingByResource[definition.Id] = Math.Max(0, available - line.AmountToSpend);
            }

            return new(card, player, freeMode.IsFree, lines);
        }

        private static Player? TryGetOwner(CardModel card)
        {
            return card.IsCanonical ? null : card.Owner;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns whether every secondary-resource payment permits the card to be played.</para>
        ///     <para xml:lang="zh-CN">返回每项次级资源支付是否都允许打出该卡牌。</para>
        /// </summary>
        public static bool CanPay(CardModel card)
        {
            return Plan(card).IsAffordable;
        }

        /// <summary>
        ///     <para xml:lang="en">Commits a still-valid resolved payment plan and returns its play ledger.</para>
        ///     <para xml:lang="zh-CN">提交一项仍然有效的已解析支付计划，并返回本次出牌的支付记录。</para>
        /// </summary>
        public static async Task<SecondaryResourcePlayLedger> Commit(
            SecondaryResourcePaymentPlan plan,
            AbstractModel? source = null)
        {
            ArgumentNullException.ThrowIfNull(plan);
            ValidateCommitPlan(plan);

            if (plan.Player == null)
            {
                if (plan.HasLines)
                    throw new InvalidOperationException(
                        $"Cannot commit secondary resource payments for {plan.Card.Id.Entry} without a player owner.");

                var empty = SecondaryResourcePlayLedger.Empty(plan.Card, null, plan.IsFree);
                SecondaryResourcePlayLedgerRuntime.SetPending(plan.Card, empty);
                return empty;
            }

            var builder = new SecondaryResourcePlayLedgerBuilder(plan.Card, plan.Player, plan.IsFree);
            foreach (var line in plan.Lines)
            {
                if (line is { IsFree: false, AmountToSpend: > 0 })
                {
                    var spent = await SecondaryResourceCmd.SpendResolvedCardPayment(
                        plan.Player,
                        line.ResourceId,
                        line.AmountToSpend,
                        plan.Card,
                        source ?? plan.Card);
                    if (!spent)
                        throw new InvalidOperationException(
                            $"Secondary resource payment failed for {line.ResourceId} on {plan.Card.Id.Entry}.");
                }

                builder.Add(line);
            }

            var ledger = builder.Build();
            SecondaryResourcePlayLedgerRuntime.SetPending(plan.Card, ledger);
            await RunShortfallPayments(plan, ledger, source ?? plan.Card);
            return ledger;
        }

        private static void ValidateCommitPlan(SecondaryResourcePaymentPlan plan)
        {
            var duplicateUse = plan.Lines
                .GroupBy(static line => line.UseId, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(static group => group.Count() > 1);
            if (duplicateUse != null)
                throw new InvalidOperationException(
                    $"Secondary resource payment plan for {plan.Card.Id.Entry} contains duplicate use id '{duplicateUse.Key}'.");

            var unplayableLine = plan.Lines.FirstOrDefault(static line => !line.CanPlay);
            if (unplayableLine != null)
                throw new InvalidOperationException(
                    $"Cannot commit unplayable secondary resource payment for {unplayableLine.ResourceId} on {plan.Card.Id.Entry}.");

            if (plan.Player == null)
                return;

            foreach (var group in plan.Lines
                         .Where(static line => line is { IsFree: false, AmountToSpend: > 0 })
                         .GroupBy(static line => line.ResourceId, StringComparer.OrdinalIgnoreCase))
            {
                var required = group.Aggregate(0L, static (sum, line) => sum + line.AmountToSpend);
                if (required > SecondaryResourceCmd.Get(plan.Player, group.Key))
                    throw new InvalidOperationException(
                        $"Secondary resource payment plan for {plan.Card.Id.Entry} no longer has enough '{group.Key}'.");
            }
        }

        private static async Task RunShortfallPayments(
            SecondaryResourcePaymentPlan plan,
            SecondaryResourcePlayLedger ledger,
            AbstractModel? source)
        {
            if (plan.Player?.Creature?.CombatState == null)
                return;

            var combatState = plan.Player.Creature.CombatState;
            foreach (var line in plan.Lines)
            {
                if (line is not { Activated: true, OriginalShortfall: > 0, IsFree: false } ||
                    !line.InsufficientPayment.AllowsPlay)
                    continue;

                var context = new SecondaryResourceShortfallContext(
                    combatState,
                    plan.Player,
                    line.Definition,
                    plan.Card,
                    line.UseId,
                    line.Kind,
                    line.Cost,
                    line.AmountAvailable,
                    line.AmountToSpend,
                    line.OriginalShortfall,
                    line.CoveredShortfall,
                    line.Shortfall,
                    source,
                    ledger);

                if (line.CoveredShortfall > 0)
                    await line.ShortfallResolution.Commit(context);

                if (line.Shortfall <= 0)
                    continue;

                await line.InsufficientPayment.InvokeShortfall(context);
                await SecondaryResourceHook.AfterShortfallPayment(context);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Creates and queues a free-play ledger without changing resource amounts.</para>
        ///     <para xml:lang="zh-CN">创建并排队等待绑定一份免费出牌支付记录，不改变资源数量。</para>
        /// </summary>
        public static SecondaryResourcePlayLedger CommitFree(SecondaryResourcePaymentPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan);

            var builder = new SecondaryResourcePlayLedgerBuilder(plan.Card, plan.Player, true);
            foreach (var line in plan.Lines)
            {
                var freeLine = line.Kind == SecondaryResourceUseKind.OptionalSpend
                    ? line with
                    {
                        IsFree = true,
                        AmountToSpend = 0,
                        Value = 0,
                        Activated = true,
                        OriginalShortfall = 0,
                        CoveredShortfall = 0,
                        Shortfall = 0,
                        ShortfallResolution = SecondaryResourceShortfallResolution.None,
                        ExtraAmountToSpend = 0,
                        ExtraStacks = 0,
                    }
                    : line with
                    {
                        IsFree = true,
                        AmountToSpend = 0,
                        Activated = true,
                        OriginalShortfall = 0,
                        CoveredShortfall = 0,
                        Shortfall = 0,
                        ShortfallResolution = SecondaryResourceShortfallResolution.None,
                        ExtraAmountToSpend = 0,
                        ExtraStacks = 0,
                    };
                builder.Add(freeLine);
            }

            var ledger = builder.Build();
            SecondaryResourcePlayLedgerRuntime.SetPending(plan.Card, ledger);
            return ledger;
        }

        internal static SecondaryResourcePlayLedger CommitAutoPlayCapture(SecondaryResourcePaymentPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan);

            var builder = new SecondaryResourcePlayLedgerBuilder(plan.Card, plan.Player, true);
            foreach (var line in plan.Lines)
            {
                var capturedLine = line with
                {
                    IsFree = true,
                    AmountToSpend = 0,
                    Value = line is { CostsX: false, Kind: SecondaryResourceUseKind.OptionalSpend }
                        ? 0
                        : line.Value,
                    OriginalShortfall = 0,
                    CoveredShortfall = 0,
                    Shortfall = 0,
                    ShortfallResolution = SecondaryResourceShortfallResolution.None,
                    ExtraAmountToSpend = 0,
                    ExtraStacks = 0,
                };
                builder.Add(capturedLine);
            }

            var ledger = builder.Build();
            SecondaryResourcePlayLedgerRuntime.SetPending(plan.Card, ledger);
            return ledger;
        }

        private static IReadOnlyList<SecondaryResourcePlayUse> SnapshotUses(CardModel card)
        {
            var uses = new List<SecondaryResourcePlayUse>();
            if (card.TryGetSecondaryCosts(out var costs))
                uses.AddRange(costs.SnapshotUses());

            if (card.TryGetSecondaryResourceUses(out var playUses))
                uses.AddRange(playUses.Snapshot());

            uses.AddRange(GetCapabilityUses(card));

            var result = uses
                .Where(static use => use.IsMaterial)
                .OrderBy(static use => use.Kind switch
                {
                    SecondaryResourceUseKind.RequiredCost => 0,
                    SecondaryResourceUseKind.ExtraSpend => 1,
                    _ => 2,
                })
                .ThenBy(static use => use.Id, StringComparer.Ordinal)
                .ToArray();

            var duplicateUse = result
                .GroupBy(static use => use.Id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(static group => group.Count() > 1);
            if (duplicateUse != null)
                throw new InvalidOperationException(
                    $"Card {card.Id.Entry} has duplicate secondary-resource use id '{duplicateUse.Key}'.");

            return result;
        }

        internal static IReadOnlyList<SecondaryResourcePlayUse> SnapshotUsesForUpgradePreview(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            return SnapshotUses(card);
        }

        private static SecondaryResourcePaymentPlan PlanPreview(
            CardModel card,
            IReadOnlyList<SecondaryResourcePlayUse> uses,
            SecondaryResourcePaymentFreeMode freeMode)
        {
            var lines = new List<SecondaryResourcePaymentLine>();
            foreach (var use in uses)
            {
                if (!ModSecondaryResourceRegistry.TryGet(use.ResourceId, out var definition))
                    continue;

                lines.Add(ResolvePreviewLine(card, definition, use, freeMode));
            }

            return new(card, null, freeMode.IsFree, lines);
        }

        private static SecondaryResourcePaymentLine ResolvePreviewLine(
            CardModel card,
            SecondaryResourceDefinition definition,
            SecondaryResourcePlayUse use,
            SecondaryResourcePaymentFreeMode freeMode)
        {
            var cost = use.Cost;
            var isFree = freeMode.AppliesTo(cost);
            var localCost = ModifyLocalCost(card, definition, use, cost.Amount);
            var baseCost = Math.Max(0, use.BaseCost.Amount);
            var upgradePreviewBaseCost = SecondaryResourceUpgradePreviewCosts.GetBaseCost(card, use);
            var fixedCost = SecondaryResourceAmountMath.CeilingAndClamp(localCost, 0, int.MaxValue);
            var displayCost = isFree ? 0 : fixedCost;
            var insufficientPayment = use.InsufficientPayment ?? definition.DefaultInsufficientPayment;
            var hasRuntimeCostModifier = use.Duration != SecondaryResourceCostDuration.Permanent ||
                                         localCost != cost.Amount ||
                                         isFree;
            if (!cost.CostsX)
                return new(definition.Id, definition, displayCost, 0, isFree ? 0 : fixedCost, fixedCost, false, isFree)
                {
                    UseId = use.Id,
                    Kind = use.Kind,
                    BlocksPlay = use.Kind == SecondaryResourceUseKind.RequiredCost,
                    Activated = use.Kind == SecondaryResourceUseKind.RequiredCost || isFree,
                    IsPreview = true,
                    BaseCost = baseCost,
                    UpgradePreviewBaseCost = upgradePreviewBaseCost,
                    HasRuntimeCostModifier = hasRuntimeCostModifier,
                    InsufficientPayment = insufficientPayment,
                };

            return new(definition.Id, definition, fixedCost, 0, 0, 0, true, isFree)
            {
                UseId = use.Id,
                Kind = use.Kind,
                BlocksPlay = use.Kind == SecondaryResourceUseKind.RequiredCost,
                Activated = isFree,
                IsPreview = true,
                BaseCost = baseCost,
                UpgradePreviewBaseCost = upgradePreviewBaseCost,
                HasRuntimeCostModifier = hasRuntimeCostModifier,
                InsufficientPayment = insufficientPayment,
            };
        }

        private static SecondaryResourcePaymentLine ResolveLine(
            CombatStateLike combatState,
            Player player,
            CardModel card,
            SecondaryResourceDefinition definition,
            SecondaryResourcePlayUse use,
            int available,
            SecondaryResourcePaymentFreeMode freeMode,
            AbstractModel? source)
        {
            var cost = use.Cost;
            var isFree = freeMode.AppliesTo(cost);
            var localCost = ModifyLocalCost(card, definition, use, cost.Amount);
            var baseCost = Math.Max(0, use.BaseCost.Amount);
            var upgradePreviewBaseCost = SecondaryResourceUpgradePreviewCosts.GetBaseCost(card, use);
            var modifiedCost = SecondaryResourceHook.ModifyCost(
                new(combatState, player, card, definition, localCost),
                localCost);
            var fixedCost = SecondaryResourceAmountMath.CeilingAndClamp(modifiedCost, 0, int.MaxValue);
            var displayCost = isFree ? 0 : fixedCost;
            var hasRuntimeCostModifier = use.Duration != SecondaryResourceCostDuration.Permanent ||
                                         localCost != cost.Amount ||
                                         modifiedCost != localCost ||
                                         isFree;
            var isRequired = use.Kind == SecondaryResourceUseKind.RequiredCost;
            var baseInsufficientPayment = use.InsufficientPayment ?? definition.DefaultInsufficientPayment;

            if (!cost.CostsX)
            {
                var availableToSpend = Math.Max(0, available);
                if (use.Kind == SecondaryResourceUseKind.ExtraSpend)
                    return ResolveExtraSpendLine(
                        combatState,
                        player,
                        card,
                        definition,
                        use,
                        available,
                        availableToSpend,
                        fixedCost,
                        displayCost,
                        baseCost,
                        upgradePreviewBaseCost,
                        isFree,
                        source);

                var originalShortfall = isFree ? 0 : Math.Max(0, fixedCost - availableToSpend);
                var initialAmountToSpend = ResolveAmountToSpend(
                    isRequired,
                    true,
                    isFree,
                    fixedCost,
                    availableToSpend,
                    baseInsufficientPayment);
                var insufficientPayment = ResolveInsufficientPayment(
                    combatState,
                    player,
                    card,
                    definition,
                    use,
                    fixedCost,
                    available,
                    initialAmountToSpend,
                    originalShortfall,
                    source,
                    baseInsufficientPayment);
                var shortfallResolution = SecondaryResourceShortfallResolution.None;
                if (isRequired && originalShortfall > 0 && insufficientPayment.AllowsPlay)
                {
                    var shortfallContext = new SecondaryResourceShortfallResolutionContext(
                        combatState,
                        player,
                        definition,
                        card,
                        use.Id,
                        use.Kind,
                        fixedCost,
                        available,
                        ResolveAmountToSpend(true, true, isFree, fixedCost, availableToSpend, insufficientPayment),
                        originalShortfall,
                        source ?? card);
                    shortfallResolution = insufficientPayment.Resolve(shortfallContext);
                    shortfallResolution = SecondaryResourceHook.ResolveShortfall(
                        shortfallContext,
                        shortfallResolution);
                }

                var coveredShortfall = Math.Min(originalShortfall, Math.Max(0, shortfallResolution.CoveredAmount));
                var shortfall = originalShortfall - coveredShortfall;
                var shortfallAllowed = isRequired &&
                                       originalShortfall > 0 &&
                                       insufficientPayment.AllowsPlay &&
                                       (shortfall > 0 || coveredShortfall >= originalShortfall);
                var activated = isFree || availableToSpend >= fixedCost || shortfallAllowed;
                var amountToSpend = ResolveAmountToSpend(
                    isRequired,
                    activated,
                    isFree,
                    fixedCost,
                    availableToSpend,
                    insufficientPayment);
                var spendAllowed = CanSpend(combatState, player, card, definition, amountToSpend, source);
                if (!isRequired && !spendAllowed)
                {
                    activated = false;
                    amountToSpend = 0;
                }

                if (!activated)
                {
                    originalShortfall = 0;
                    coveredShortfall = 0;
                    shortfall = 0;
                    shortfallResolution = SecondaryResourceShortfallResolution.None;
                }

                var value = !isRequired && !activated ? 0 : fixedCost;
                return new(definition.Id, definition, displayCost, available, amountToSpend, value, false, isFree)
                {
                    UseId = use.Id,
                    Kind = use.Kind,
                    BlocksPlay = isRequired,
                    Activated = activated,
                    SpendAllowed = spendAllowed,
                    BaseCost = baseCost,
                    UpgradePreviewBaseCost = upgradePreviewBaseCost,
                    HasRuntimeCostModifier = hasRuntimeCostModifier,
                    OriginalShortfall = originalShortfall,
                    CoveredShortfall = coveredShortfall,
                    Shortfall = shortfall,
                    InsufficientPayment = insufficientPayment,
                    ShortfallResolution = shortfallResolution,
                };
            }

            var xBase = Math.Max(0, available);
            var nativeXValue = Hook.ModifyXValue(combatState, card, xBase);
            var xValue = SecondaryResourceHook.ModifyXValue(
                new(combatState, player, card, definition, xBase),
                nativeXValue);
            if (cost.XMultiplier <= 0)
                throw new InvalidOperationException(
                    $"Secondary-resource X cost '{use.Id}' on {card.Id.Entry} must have a positive multiplier.");

            xValue = SecondaryResourceAmountMath.MultiplyNonNegativeSaturating(xValue, cost.XMultiplier);
            var xActivated = isRequired || isFree || available > 0;
            var amountToSpendForX = isFree || !xActivated ? 0 : xBase;
            var xSpendAllowed = CanSpend(combatState, player, card, definition, amountToSpendForX, source);
            // ReSharper disable once InvertIf
            if (!isRequired && !xSpendAllowed)
            {
                xActivated = false;
                amountToSpendForX = 0;
            }

            var effectiveXValue = isFree && !isRequired ? 0 : xValue;
            return new(
                definition.Id,
                definition,
                fixedCost,
                available,
                amountToSpendForX,
                xActivated ? effectiveXValue : 0,
                true,
                isFree)
            {
                UseId = use.Id,
                Kind = use.Kind,
                BlocksPlay = isRequired,
                Activated = xActivated,
                SpendAllowed = xSpendAllowed,
                BaseCost = baseCost,
                UpgradePreviewBaseCost = upgradePreviewBaseCost,
                HasRuntimeCostModifier = hasRuntimeCostModifier,
                InsufficientPayment = baseInsufficientPayment,
            };
        }

        private static SecondaryResourcePaymentLine ResolveExtraSpendLine(
            CombatStateLike combatState,
            Player player,
            CardModel card,
            SecondaryResourceDefinition definition,
            SecondaryResourcePlayUse use,
            int available,
            int availableToSpend,
            int perStackAmount,
            int displayCost,
            int baseCost,
            int? upgradePreviewBaseCost,
            bool isFree,
            AbstractModel? source)
        {
            var maxStacks = use.MaxExtraStacks ?? int.MaxValue;
            var stacks = isFree || perStackAmount <= 0
                ? 0
                : Math.Min(maxStacks, availableToSpend / perStackAmount);
            var amountToSpend = stacks * perStackAmount;
            var spendAllowed = CanSpend(combatState, player, card, definition, amountToSpend, source);
            // ReSharper disable once InvertIf
            if (!spendAllowed)
            {
                stacks = 0;
                amountToSpend = 0;
            }

            return new(
                definition.Id,
                definition,
                displayCost,
                available,
                amountToSpend,
                stacks,
                false,
                isFree)
            {
                UseId = use.Id,
                Kind = use.Kind,
                BlocksPlay = false,
                Activated = stacks > 0,
                SpendAllowed = spendAllowed,
                BaseCost = baseCost,
                UpgradePreviewBaseCost = upgradePreviewBaseCost,
                HasRuntimeCostModifier = isFree || perStackAmount != baseCost,
                ExtraStacks = stacks,
                ExtraAmountToSpend = amountToSpend,
            };
        }

        private static int ResolveAmountToSpend(
            bool isRequired,
            bool activated,
            bool isFree,
            int fixedCost,
            int available,
            SecondaryResourceInsufficientPayment insufficientPayment)
        {
            if (!activated || isFree)
                return 0;

            if (!isRequired || available >= fixedCost)
                return fixedCost;

            return insufficientPayment is { AllowsPlay: true, SpendAvailable: true }
                ? available
                : 0;
        }

        private static SecondaryResourceInsufficientPayment ResolveInsufficientPayment(
            CombatStateLike combatState,
            Player player,
            CardModel card,
            SecondaryResourceDefinition definition,
            SecondaryResourcePlayUse use,
            int cost,
            int available,
            int amountToSpend,
            int shortfall,
            AbstractModel? source,
            SecondaryResourceInsufficientPayment payment)
        {
            if (use.Kind != SecondaryResourceUseKind.RequiredCost || shortfall <= 0)
                return payment;

            return SecondaryResourceHook.ModifyInsufficientPayment(
                new(
                    combatState,
                    player,
                    definition,
                    card,
                    use.Id,
                    use.Kind,
                    cost,
                    available,
                    amountToSpend,
                    shortfall,
                    source ?? card),
                payment);
        }

        private static IEnumerable<SecondaryResourcePlayUse> GetCapabilityUses(CardModel card)
        {
            foreach (var capability in ModelCapabilityHost.GetCapabilities<ICardSecondaryResourceUseContributor>(card))
            {
                var contributed = capability.GetSecondaryResourceUses(card)?.ToArray() ??
                                  throw new InvalidOperationException(
                                      $"Secondary-resource use contributor '{capability.GetType().FullName}' returned null.");
                foreach (var use in contributed)
                    ValidateCapabilityUse(use);

                foreach (var use in contributed.Select(static use => use with
                         {
                             Id = use.Id.Trim(),
                             ResourceId = use.ResourceId.Trim(),
                         }))
                    if (use.IsMaterial)
                        yield return use;
            }
        }

        private static void ValidateCapabilityUse(SecondaryResourcePlayUse? use)
        {
            if (use == null)
                throw new InvalidOperationException("A secondary-resource use contributor returned a null use.");
            if (string.IsNullOrWhiteSpace(use.Id))
                throw new InvalidOperationException(
                    "A secondary-resource use contributor returned a use without an id.");
            if (string.IsNullOrWhiteSpace(use.ResourceId))
                throw new InvalidOperationException(
                    $"Secondary-resource use '{use.Id}' does not specify a resource id.");
            // Ordered validation guards produce field-specific diagnostics.
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (use.Cost == null)
                throw new InvalidOperationException(
                    $"Secondary-resource use '{use.Id}' does not specify a cost.");
            if (use.Cost is { CostsX: true, XMultiplier: <= 0 })
                throw new InvalidOperationException(
                    $"Secondary-resource X use '{use.Id}' must have a positive multiplier.");
            if (use.MaxExtraStacks is < 0)
                throw new InvalidOperationException(
                    $"Secondary-resource use '{use.Id}' has a negative maximum stack count.");
            if (use is { Kind: SecondaryResourceUseKind.ExtraSpend, Cost.CostsX: true })
                throw new InvalidOperationException(
                    $"Repeatable extra secondary-resource use '{use.Id}' cannot have an X cost.");
        }

        private static decimal ModifyLocalCost(
            CardModel card,
            SecondaryResourceDefinition definition,
            SecondaryResourcePlayUse use,
            decimal cost)
        {
            var result = cost;
            var context = new SecondaryResourceCardCostContext(card, definition, use, cost);
            // Contributor order is significant and each result feeds the next contributor.
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var capability in ModelCapabilityHost.GetCapabilities<ICardSecondaryResourceCostContributor>(card))
                result = capability.ModifySecondaryResourceCost(context, result);

            return result;
        }

        private static bool CanSpend(
            CombatStateLike combatState,
            Player player,
            CardModel card,
            SecondaryResourceDefinition definition,
            int amount,
            AbstractModel? source)
        {
            return amount <= 0 ||
                   SecondaryResourceHook.ShouldSpend(
                       new(combatState, player, definition, card, amount, source ?? card));
        }
    }

    internal static class SecondaryResourceUpgradePreviewCosts
    {
        private static readonly AttachedState<CardModel, Dictionary<SecondaryResourcePlayUseKey, int>> BeforeUpgrade =
            new();

        internal static void Capture(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            var uses = SecondaryResourcePaymentResolver.SnapshotUsesForUpgradePreview(card);
            BeforeUpgrade.Set(
                card,
                uses.ToDictionary(
                    static use => SecondaryResourcePlayUseKey.From(use),
                    static use => Math.Max(0, use.BaseCost.Amount)));
        }

        internal static void Clear(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            BeforeUpgrade.Remove(card);
        }

        internal static int? GetBaseCost(CardModel card, SecondaryResourcePlayUse use)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentNullException.ThrowIfNull(use);
            return BeforeUpgrade.TryGetValue(card, out var costs) &&
                   costs.TryGetValue(SecondaryResourcePlayUseKey.From(use), out var cost)
                ? cost
                : null;
        }
    }

    internal readonly record struct SecondaryResourcePlayUseKey(
        string Id,
        string ResourceId,
        SecondaryResourceUseKind Kind)
    {
        public static SecondaryResourcePlayUseKey From(SecondaryResourcePlayUse use)
        {
            return new(use.Id, use.ResourceId, use.Kind);
        }
    }
}
