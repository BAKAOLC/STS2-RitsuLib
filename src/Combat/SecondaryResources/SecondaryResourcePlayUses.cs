using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Semantic role for a secondary-resource card-play use.
    ///     次级资源出牌条款的语义角色。
    /// </summary>
    public enum SecondaryResourceUseKind
    {
        /// <summary>
        ///     Required payment. If the player cannot pay it, the card cannot be played.
        ///     必需支付；玩家无法支付时，卡牌不能打出。
        /// </summary>
        RequiredCost,

        /// <summary>
        ///     Optional payment. If the player can pay it, it is spent and activates its ledger line; otherwise the card
        ///     still plays.
        ///     可选支付；玩家可支付时消耗并激活 ledger 行，否则卡牌仍可打出。
        /// </summary>
        OptionalSpend,

        /// <summary>
        ///     Repeatable extra payment. After required payments are reserved, spends as many full stacks as possible.
        ///     可重复额外支付；必需支付预留后，按完整份数尽可能额外消耗。
        /// </summary>
        ExtraSpend,
    }

    /// <summary>
    ///     Attached secondary-resource card-play use.
    ///     附加在卡牌上的次级资源出牌条款。
    /// </summary>
    public sealed record SecondaryResourcePlayUse(
        string Id,
        string ResourceId,
        SecondaryResourceCost Cost,
        SecondaryResourceUseKind Kind)
    {
        /// <summary>
        ///     Lifetime of the active use layer that produced this descriptor.
        ///     产生该条款描述的当前生效层生命周期。
        /// </summary>
        public SecondaryResourceCostDuration Duration { get; init; } = SecondaryResourceCostDuration.Permanent;

        /// <summary>
        ///     Current permanent base cost used for cost-color comparison when this use is temporarily overridden.
        ///     该条款被临时覆盖时，用于费用颜色比较的当前永久基础费用。
        /// </summary>
        public SecondaryResourceCost BaseCost { get; init; } = Cost;

        /// <summary>
        ///     Optional per-use policy for required payments that are short on resource.
        ///     必需支付资源不足时的可选 per-use 策略。
        /// </summary>
        public SecondaryResourceInsufficientPayment? InsufficientPayment { get; init; }

        /// <summary>
        ///     Optional maximum stack count for repeatable extra spends.
        ///     可重复额外支付的可选最大层数。
        /// </summary>
        public int? MaxExtraStacks { get; init; }

        /// <summary>
        ///     True when this use can affect play/payment.
        ///     该条款可能影响出牌/支付时为 true。
        /// </summary>
        public bool IsMaterial => Cost.IsMaterial || Kind == SecondaryResourceUseKind.OptionalSpend;
    }

    /// <summary>
    ///     Attached secondary-resource card-play uses for one card.
    ///     单张卡牌的附加次级资源出牌条款集合。
    /// </summary>
    public sealed class SecondaryResourcePlayUseSet
    {
        private readonly Dictionary<string, List<SecondaryResourcePlayUseLayer>> _uses =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     True when at least one material use is attached.
        ///     至少附加了一个实际条款时为 true。
        /// </summary>
        public bool HasUses =>
            _uses.Values.SelectMany(static layers => layers).Any(static layer => layer.Use.IsMaterial);

        /// <summary>
        ///     Returns use ids that currently have attached layers.
        ///     返回当前具有附加层的条款 id。
        /// </summary>
        public IReadOnlyList<string> UseIds =>
            _uses.Keys.OrderBy(static id => id, StringComparer.Ordinal).ToArray();

        internal bool HasLayers => _uses.Count > 0;

        internal bool HasPermanentLayers =>
            _uses.Values
                .SelectMany(static layers => layers)
                .Any(static layer => layer.Duration == SecondaryResourceCostDuration.Permanent);

        /// <summary>
        ///     Raised after attached secondary-resource uses change.
        ///     在附加次级资源条款变化后触发。
        /// </summary>
        public event Action? Changed;

        /// <summary>
        ///     Attaches a permanent required cost.
        ///     附加一个永久必需费用。
        /// </summary>
        public SecondaryResourcePlayUseSet Require(string useId, string resourceId, int amount)
        {
            return Require(useId, resourceId, new SecondaryResourceCost(Math.Max(0, amount)));
        }

        /// <summary>
        ///     Attaches a required cost.
        ///     附加一个必需费用。
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
        ///     Attaches a required cost with an explicit shortfall policy.
        ///     附加一个带显式短缺策略的必需费用。
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
        ///     Attaches a required cost that can still be played with a shortfall.
        ///     附加一个资源不足时仍可打出的必需费用。
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
        ///     Attaches a required cost that can still be played with a shortfall.
        ///     附加一个资源不足时仍可打出的必需费用。
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
        ///     Attaches a permanent optional spend that activates only when it can be paid.
        ///     附加一个永久可选支付；仅在可支付时激活。
        /// </summary>
        public SecondaryResourcePlayUseSet SpendIfAvailable(string useId, string resourceId, int amount)
        {
            return SpendIfAvailable(useId, resourceId, new SecondaryResourceCost(Math.Max(0, amount)));
        }

        /// <summary>
        ///     Attaches an optional spend that activates only when it can be paid.
        ///     附加一个可选支付；仅在可支付时激活。
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
        ///     Attaches a repeatable extra spend that consumes full stacks after required payments are reserved.
        ///     附加一个可重复额外支付；必需支付预留后按完整份数消耗。
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
        ///     Sets a use descriptor for one use id and duration.
        ///     为单个条款 id 和持续时间设置条款描述。
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
        ///     Sets a use descriptor for one use id and duration with an explicit shortfall policy.
        ///     为单个条款 id 和持续时间设置带显式短缺策略的条款描述。
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
        ///     Sets a use descriptor for one use id and duration with explicit shortfall and extra-spend settings.
        ///     为单个条款 id 和持续时间设置带显式短缺与额外支付设置的条款描述。
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
        ///     Clears all layers for one use id.
        ///     清除单个条款 id 的所有层。
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
        ///     Clears layers for the specified duration.
        ///     清除指定持续时间的条款层。
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
        ///     Returns active uses in deterministic order.
        ///     按确定性顺序返回当前生效条款。
        /// </summary>
        public IReadOnlyList<SecondaryResourcePlayUse> Snapshot()
        {
            return _uses
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
                .ThenBy(static use => use.Id, StringComparer.Ordinal)
                .ToArray();
        }

        internal SecondaryResourcePlayUseSet Clone()
        {
            var clone = new SecondaryResourcePlayUseSet();
            foreach (var (useId, layers) in _uses)
                clone._uses[useId] = layers.ToList();

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
                        _uses[useId] = permanentLayers.ToList();
                    changed = true;
                }

            if (changed)
                Changed?.Invoke();

            return changed;
        }

        private List<SecondaryResourcePlayUseLayer> GetLayers(string useId)
        {
            if (_uses.TryGetValue(useId, out var layers)) return layers;
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
        ///     Gets this card's secondary-resource play-use set.
        ///     获取此卡牌的次级资源出牌条款集合。
        /// </summary>
        public static SecondaryResourcePlayUseSet SecondaryResourceUses(this CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            return UseSets.GetOrCreate(card);
        }

        /// <summary>
        ///     Attempts to read existing secondary-resource play uses without creating a set.
        ///     尝试读取已有次级资源出牌条款，不会创建集合。
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
                   (card.TryGetSecondaryResourceUses(out var uses) && uses.HasUses);
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
