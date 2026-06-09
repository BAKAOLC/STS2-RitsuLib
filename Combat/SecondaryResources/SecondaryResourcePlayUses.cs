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
        ///     True when this use can affect play/payment.
        ///     该条款可能影响出牌/支付时为 true。
        /// </summary>
        public bool IsMaterial => Cost.IsMaterial;
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
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            ArgumentNullException.ThrowIfNull(cost);

            var normalizedUseId = useId.Trim();
            var normalizedResourceId = resourceId.Trim();
            var layers = GetLayers(normalizedUseId);
            layers.RemoveAll(layer => layer.Duration == duration);
            layers.Add(new(
                new(normalizedUseId, normalizedResourceId, cost, kind),
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
                .Select(static pair => pair.Value[^1].Use)
                .Where(static use => use.IsMaterial)
                .OrderBy(static use => use.Kind == SecondaryResourceUseKind.RequiredCost ? 0 : 1)
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
    }
}
