using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">Specifies the payment role of a secondary-resource card-play use.</para>
    ///     <para xml:lang="zh-CN">指定次级资源出牌支付条款的用途。</para>
    /// </summary>
    public enum SecondaryResourceUseKind
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Represents a required payment that prevents card play when it cannot be satisfied, unless its
        ///         insufficient-payment policy permits the shortfall.
        ///     </para>
        ///     <para xml:lang="zh-CN">表示一项必需支付；无法满足时会阻止出牌，除非资源不足支付策略允许该缺口。</para>
        /// </summary>
        RequiredCost,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Represents an optional payment that activates when it can be paid and never prevents card play.
        ///     </para>
        ///     <para xml:lang="zh-CN">表示一项可选支付；可支付时激活，且永远不会阻止出牌。</para>
        /// </summary>
        OptionalSpend,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Represents a repeatable extra payment that buys as many complete units as possible after required
        ///         payments are reserved.
        ///     </para>
        ///     <para xml:lang="zh-CN">表示一项可重复额外支付；预留必需支付后，尽可能购买完整的额外支付单位。</para>
        /// </summary>
        ExtraSpend,
    }

    /// <summary>
    ///     <para xml:lang="en">Describes one secondary-resource payment use attached to a card.</para>
    ///     <para xml:lang="zh-CN">描述附加到卡牌上的一项次级资源支付条款。</para>
    /// </summary>
    public sealed record SecondaryResourcePlayUse(
        string Id,
        string ResourceId,
        SecondaryResourceCost Cost,
        SecondaryResourceUseKind Kind)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the duration of the active layer that produced this descriptor.</para>
        ///     <para xml:lang="zh-CN">获取生成该描述的当前生效层的持续时间。</para>
        /// </summary>
        public SecondaryResourceCostDuration Duration { get; init; } = SecondaryResourceCostDuration.Permanent;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the persistent baseline cost used for display-color comparison, or the active cost when no
        ///         persistent layer exists.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取用于比较显示颜色的持续生效基础费用；没有持续生效层时则为当前费用。</para>
        /// </summary>
        public SecondaryResourceCost BaseCost { get; init; } = Cost;

        /// <summary>
        ///     <para xml:lang="en">Gets the optional insufficient-payment policy for this required payment use.</para>
        ///     <para xml:lang="zh-CN">获取该必需支付条款专用的可选资源不足支付策略。</para>
        /// </summary>
        public SecondaryResourceInsufficientPayment? InsufficientPayment { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional maximum number of repeatable extra-payment units.</para>
        ///     <para xml:lang="zh-CN">获取可重复额外支付单位数的可选上限。</para>
        /// </summary>
        public int? MaxExtraStacks { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether this use participates in card-play payment planning.</para>
        ///     <para xml:lang="zh-CN">获取该条款是否参与出牌支付规划。</para>
        /// </summary>
        public bool IsMaterial => Cost.IsMaterial || Kind == SecondaryResourceUseKind.OptionalSpend;
    }

    /// <summary>
    ///     <para xml:lang="en">Stores layered secondary-resource payment uses attached to one card.</para>
    ///     <para xml:lang="zh-CN">存储附加到一张卡牌上的分层次级资源支付条款。</para>
    /// </summary>
    public sealed class SecondaryResourcePlayUseSet
    {
        private readonly Dictionary<string, List<SecondaryResourcePlayUseLayer>> _uses =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     <para xml:lang="en">Gets whether at least one active use participates in payment planning.</para>
        ///     <para xml:lang="zh-CN">获取是否至少有一项当前条款参与支付规划。</para>
        /// </summary>
        public bool HasUses =>
            _uses.Values.SelectMany(static layers => layers).Any(static layer => layer.Use.IsMaterial);

        /// <summary>
        ///     <para xml:lang="en">Gets the use identifiers that currently have attached layers.</para>
        ///     <para xml:lang="zh-CN">获取当前存在附加层的支付条款标识符。</para>
        /// </summary>
        public IReadOnlyList<string> UseIds =>
            [.. _uses.Keys.OrderBy(static id => id, StringComparer.Ordinal)];

        internal bool HasLayers => _uses.Count > 0;

        internal bool HasPermanentLayers =>
            _uses.Values
                .SelectMany(static layers => layers)
                .Any(static layer => layer.Duration == SecondaryResourceCostDuration.Permanent);

        /// <summary>
        ///     <para xml:lang="en">Occurs after the attached payment uses change.</para>
        ///     <para xml:lang="zh-CN">在附加支付条款发生变化后触发。</para>
        /// </summary>
        public event Action? Changed;

        /// <summary>
        ///     <para xml:lang="en">Attaches a persistent required payment with a fixed cost.</para>
        ///     <para xml:lang="zh-CN">附加一项具有固定费用且持续生效的必需支付。</para>
        /// </summary>
        public SecondaryResourcePlayUseSet Require(string useId, string resourceId, int amount)
        {
            return Require(useId, resourceId, new SecondaryResourceCost(Math.Max(0, amount)));
        }

        /// <summary>
        ///     <para xml:lang="en">Attaches a required payment with the specified cost and duration.</para>
        ///     <para xml:lang="zh-CN">附加一项具有指定费用及持续时间的必需支付。</para>
        /// </summary>
        public SecondaryResourcePlayUseSet Require(
            string useId,
            string resourceId,
            SecondaryResourceCost cost,
            SecondaryResourceCostDuration duration = SecondaryResourceCostDuration.Permanent)
        {
            return Set(useId, resourceId, cost, SecondaryResourceUseKind.RequiredCost, duration);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attaches a required payment with an explicit insufficient-payment policy.
        ///     </para>
        ///     <para xml:lang="zh-CN">附加一项具有显式资源不足支付策略的必需支付。</para>
        /// </summary>
        public SecondaryResourcePlayUseSet Require(
            string useId,
            string resourceId,
            SecondaryResourceCost cost,
            SecondaryResourceInsufficientPayment insufficientPayment,
            SecondaryResourceCostDuration duration = SecondaryResourceCostDuration.Permanent)
        {
            return Set(
                useId,
                resourceId,
                cost,
                SecondaryResourceUseKind.RequiredCost,
                duration,
                insufficientPayment);
        }

        /// <summary>
        ///     <para xml:lang="en">Attaches a fixed required payment whose shortfall does not prevent card play.</para>
        ///     <para xml:lang="zh-CN">附加一项费用缺口不会阻止出牌的固定必需支付。</para>
        /// </summary>
        public SecondaryResourcePlayUseSet RequireAllowingShortfall(
            string useId,
            string resourceId,
            int amount,
            SecondaryResourceShortfallPaymentHandler? onShortfall = null,
            bool spendAvailable = true,
            SecondaryResourceShortfallResolver? resolveShortfall = null)
        {
            return RequireAllowingShortfall(
                useId,
                resourceId,
                new SecondaryResourceCost(Math.Max(0, amount)),
                onShortfall,
                spendAvailable,
                resolveShortfall);
        }

        /// <summary>
        ///     <para xml:lang="en">Attaches a required payment whose shortfall does not prevent card play.</para>
        ///     <para xml:lang="zh-CN">附加一项费用缺口不会阻止出牌的必需支付。</para>
        /// </summary>
        public SecondaryResourcePlayUseSet RequireAllowingShortfall(
            string useId,
            string resourceId,
            SecondaryResourceCost cost,
            SecondaryResourceShortfallPaymentHandler? onShortfall = null,
            bool spendAvailable = true,
            SecondaryResourceShortfallResolver? resolveShortfall = null,
            SecondaryResourceCostDuration duration = SecondaryResourceCostDuration.Permanent)
        {
            return Require(
                useId,
                resourceId,
                cost,
                SecondaryResourceInsufficientPayment.AllowPlay(onShortfall, spendAvailable, resolveShortfall),
                duration);
        }

        /// <summary>
        ///     <para xml:lang="en">Attaches a persistent fixed optional payment that activates only when payable.</para>
        ///     <para xml:lang="zh-CN">附加一项持续生效的固定可选支付；仅在可以支付时激活。</para>
        /// </summary>
        public SecondaryResourcePlayUseSet SpendIfAvailable(string useId, string resourceId, int amount)
        {
            return SpendIfAvailable(useId, resourceId, new SecondaryResourceCost(Math.Max(0, amount)));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attaches an optional payment with the specified cost and duration that activates only when payable.
        ///     </para>
        ///     <para xml:lang="zh-CN">附加一项具有指定费用及持续时间的可选支付；仅在可以支付时激活。</para>
        /// </summary>
        public SecondaryResourcePlayUseSet SpendIfAvailable(
            string useId,
            string resourceId,
            SecondaryResourceCost cost,
            SecondaryResourceCostDuration duration = SecondaryResourceCostDuration.Permanent)
        {
            return Set(useId, resourceId, cost, SecondaryResourceUseKind.OptionalSpend, duration);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attaches a repeatable extra payment that buys complete units after required payments are reserved.
        ///     </para>
        ///     <para xml:lang="zh-CN">附加一项可重复额外支付；预留必需支付后按完整单位购买。</para>
        /// </summary>
        public SecondaryResourcePlayUseSet SpendExtra(
            string useId,
            string resourceId,
            int perStackAmount,
            int? maxStacks = null,
            SecondaryResourceCostDuration duration = SecondaryResourceCostDuration.Permanent)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(perStackAmount);
            if (maxStacks is < 0)
                throw new ArgumentOutOfRangeException(nameof(maxStacks));

            return Set(
                useId,
                resourceId,
                new(perStackAmount),
                SecondaryResourceUseKind.ExtraSpend,
                duration,
                null,
                maxStacks);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets a payment-use descriptor for one use identifier at the specified duration.
        ///     </para>
        ///     <para xml:lang="zh-CN">为一个支付条款标识符设置具有指定持续时间的条款描述。</para>
        /// </summary>
        public SecondaryResourcePlayUseSet Set(
            string useId,
            string resourceId,
            SecondaryResourceCost cost,
            SecondaryResourceUseKind kind,
            SecondaryResourceCostDuration duration = SecondaryResourceCostDuration.Permanent)
        {
            return Set(useId, resourceId, cost, kind, duration, null);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets a payment-use descriptor with the specified duration and insufficient-payment policy.
        ///     </para>
        ///     <para xml:lang="zh-CN">设置一项具有指定持续时间及资源不足支付策略的支付条款描述。</para>
        /// </summary>
        public SecondaryResourcePlayUseSet Set(
            string useId,
            string resourceId,
            SecondaryResourceCost cost,
            SecondaryResourceUseKind kind,
            SecondaryResourceCostDuration duration,
            SecondaryResourceInsufficientPayment? insufficientPayment)
        {
            return Set(useId, resourceId, cost, kind, duration, insufficientPayment, null);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets a payment-use descriptor with explicit duration, insufficient-payment, and extra-payment
        ///         settings.
        ///     </para>
        ///     <para xml:lang="zh-CN">设置一项具有显式持续时间、资源不足支付及额外支付配置的支付条款描述。</para>
        /// </summary>
        public SecondaryResourcePlayUseSet Set(
            string useId,
            string resourceId,
            SecondaryResourceCost cost,
            SecondaryResourceUseKind kind,
            SecondaryResourceCostDuration duration,
            SecondaryResourceInsufficientPayment? insufficientPayment,
            int? maxExtraStacks)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            ArgumentNullException.ThrowIfNull(cost);
            if (cost.CostsX && cost.XMultiplier <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(cost),
                    "An X secondary-resource cost must have a positive multiplier.");
            if (maxExtraStacks is < 0)
                throw new ArgumentOutOfRangeException(nameof(maxExtraStacks));
            if (kind == SecondaryResourceUseKind.ExtraSpend && cost.CostsX)
                throw new ArgumentException("Repeatable extra secondary-resource spends cannot use X costs.",
                    nameof(cost));

            var normalizedUseId = useId.Trim();
            var normalizedResourceId = resourceId.Trim();
            var layers = GetLayers(normalizedUseId);
            layers.RemoveAll(layer => layer.Duration == duration);
            layers.Add(new(
                new(normalizedUseId, normalizedResourceId, cost, kind)
                {
                    Duration = duration,
                    InsufficientPayment = insufficientPayment,
                    MaxExtraStacks = maxExtraStacks,
                },
                duration));
            Changed?.Invoke();
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Clears every layer for one payment-use identifier.</para>
        ///     <para xml:lang="zh-CN">清除一个支付条款标识符的所有附加层。</para>
        /// </summary>
        public bool Clear(string useId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            var removed = _uses.Remove(useId.Trim());
            if (removed)
                Changed?.Invoke();

            return removed;
        }

        /// <summary>
        ///     <para xml:lang="en">Clears every payment-use layer with the specified duration.</para>
        ///     <para xml:lang="zh-CN">清除所有具有指定持续时间的支付条款层。</para>
        /// </summary>
        public bool ClearDuration(SecondaryResourceCostDuration duration)
        {
            var changed = false;
            foreach (var useId in _uses.Keys.ToArray())
            {
                changed |= _uses[useId].RemoveAll(layer => layer.Duration == duration) > 0;
                if (_uses[useId].Count == 0)
                    _uses.Remove(useId);
            }

            if (changed)
                Changed?.Invoke();

            return changed;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the active payment uses in deterministic role and identifier order.</para>
        ///     <para xml:lang="zh-CN">按确定的支付用途及标识符顺序返回当前生效的支付条款。</para>
        /// </summary>
        public IReadOnlyList<SecondaryResourcePlayUse> Snapshot()
        {
            return
            [
                .. _uses
                    .Select(static pair =>
                    {
                        var layer = pair.Value[^1];
                        var permanentCost = pair.Value.LastOrDefault(static candidate =>
                            candidate.Duration == SecondaryResourceCostDuration.Permanent)?.Use.Cost ?? layer.Use.Cost;
                        return layer.Use with
                        {
                            Duration = layer.Duration,
                            BaseCost = permanentCost,
                        };
                    })
                    .Where(static use => use.IsMaterial)
                    .OrderBy(static use => use.Kind switch
                    {
                        SecondaryResourceUseKind.RequiredCost => 0,
                        SecondaryResourceUseKind.ExtraSpend => 1,
                        _ => 2,
                    })
                    .ThenBy(static use => use.Id, StringComparer.Ordinal),
            ];
        }

        internal SecondaryResourcePlayUseSet Clone()
        {
            var clone = new SecondaryResourcePlayUseSet();
            foreach (var (useId, layers) in _uses)
                clone._uses[useId] = [.. layers];

            return clone;
        }

        internal bool ResetPermanentLayersFrom(SecondaryResourcePlayUseSet? canonicalUses)
        {
            var changed = false;
            foreach (var useId in _uses.Keys.ToArray())
            {
                changed |= _uses[useId].RemoveAll(static layer =>
                    layer.Duration == SecondaryResourceCostDuration.Permanent) > 0;
                if (_uses[useId].Count == 0)
                    _uses.Remove(useId);
            }

            if (canonicalUses != null)
                foreach (var (useId, canonicalLayers) in canonicalUses._uses)
                {
                    var permanentLayers = canonicalLayers
                        .Where(static layer => layer.Duration == SecondaryResourceCostDuration.Permanent)
                        .ToArray();
                    if (permanentLayers.Length == 0)
                        continue;

                    if (_uses.TryGetValue(useId, out var layers))
                        layers.InsertRange(0, permanentLayers);
                    else
                        _uses[useId] = [.. permanentLayers];
                    changed = true;
                }

            if (changed)
                Changed?.Invoke();

            return changed;
        }

        private List<SecondaryResourcePlayUseLayer> GetLayers(string useId)
        {
            if (_uses.TryGetValue(useId, out var layers))
                return layers;

            layers = [];
            _uses[useId] = layers;

            return layers;
        }
    }

    internal sealed record SecondaryResourcePlayUseLayer(
        SecondaryResourcePlayUse Use,
        SecondaryResourceCostDuration Duration);

    public static partial class SecondaryResourceCardExtensions
    {
        private static readonly AttachedState<CardModel, SecondaryResourcePlayUseSet> UseSets = new(() => new());

        /// <summary>
        ///     <para xml:lang="en">Gets or creates the secondary-resource payment-use set attached to this card.</para>
        ///     <para xml:lang="zh-CN">获取或创建附加到此卡牌的次级资源支付条款集合。</para>
        /// </summary>
        public static SecondaryResourcePlayUseSet SecondaryResourceUses(this CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            return UseSets.GetOrCreate(card);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get the attached payment-use set without creating one.</para>
        ///     <para xml:lang="zh-CN">尝试获取已附加的支付条款集合，且不会创建新集合。</para>
        /// </summary>
        public static bool TryGetSecondaryResourceUses(this CardModel card, out SecondaryResourcePlayUseSet uses)
        {
            ArgumentNullException.ThrowIfNull(card);
            return UseSets.TryGetValue(card, out uses!);
        }

        internal static bool ClearSecondaryResourceUsesUntilPlayed(this CardModel card)
        {
            return card.TryGetSecondaryResourceUses(out var uses) &&
                   uses.ClearDuration(SecondaryResourceCostDuration.UntilPlayed);
        }

        internal static bool ClearSecondaryResourceUsesThisTurn(this CardModel card)
        {
            return card.TryGetSecondaryResourceUses(out var uses) &&
                   uses.ClearDuration(SecondaryResourceCostDuration.ThisTurn);
        }

        internal static bool HasMaterialSecondaryResourceWork(this CardModel card)
        {
            if (!ModSecondaryResourceRegistry.HasAny)
                return false;

            return (card.TryGetSecondaryCosts(out var costs) && costs.HasCosts) ||
                   (card.TryGetSecondaryResourceUses(out var uses) && uses.HasUses) ||
                   ModelCapabilityHost.GetCapabilities<ICardSecondaryResourceUseContributor>(card).Any();
        }

        internal static bool CopySecondaryResourceUsesTo(this CardModel source, CardModel destination)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);

            if (!source.TryGetSecondaryResourceUses(out var uses) || !uses.HasLayers)
                return false;

            UseSets.Set(destination, uses.Clone());
            return true;
        }

        internal static bool ResetSecondaryResourceUsesForDowngradeFrom(
            this CardModel canonical,
            CardModel card)
        {
            ArgumentNullException.ThrowIfNull(canonical);
            ArgumentNullException.ThrowIfNull(card);

            var hasCanonicalUses = canonical.TryGetSecondaryResourceUses(out var canonicalUses) &&
                                   canonicalUses.HasPermanentLayers;
            // ReSharper disable once InvertIf
            if (!card.TryGetSecondaryResourceUses(out var uses))
            {
                if (!hasCanonicalUses)
                    return false;

                uses = UseSets.Set(card, new());
            }

            return uses.ResetPermanentLayersFrom(hasCanonicalUses ? canonicalUses : null);
        }
    }
}
