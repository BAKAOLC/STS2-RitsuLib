#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Lifetime for temporary secondary-resource card costs.
    ///     临时次级资源卡牌费用的生命周期。
    /// </summary>
    public enum SecondaryResourceCostDuration
    {
        /// <summary>
        ///     Canonical or manually persistent attached cost.
        ///     固有费用或手动持久附加费用。
        /// </summary>
        Permanent,

        /// <summary>
        ///     Clears after the next successful play.
        ///     下一次成功打出后清除。
        /// </summary>
        UntilPlayed,

        /// <summary>
        ///     Clears at end of turn.
        ///     回合结束时清除。
        /// </summary>
        ThisTurn,

        /// <summary>
        ///     Clears at combat end with the card object.
        ///     随卡牌对象在战斗结束时清除。
        /// </summary>
        ThisCombat,
    }

    /// <summary>
    ///     Secondary-resource cost descriptor for a single resource.
    ///     单个次级资源的费用描述。
    /// </summary>
    public sealed record SecondaryResourceCost(
        int Amount,
        bool CostsX = false,
        int XMultiplier = 1)
    {
        /// <summary>
        ///     Zero fixed cost.
        ///     固定零费用。
        /// </summary>
        public static SecondaryResourceCost Free { get; } = new(0);

        /// <summary>
        ///     Returns true when this cost can require payment.
        ///     当该费用可能需要支付时返回 true。
        /// </summary>
        public bool IsMaterial => CostsX || Amount > 0;

        /// <summary>
        ///     Creates an X cost descriptor.
        ///     创建一个 X 费用描述。
        /// </summary>
        public static SecondaryResourceCost X(int multiplier = 1)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(multiplier);
            return new(0, true, multiplier);
        }
    }

    /// <summary>
    ///     Attached cost set for one card.
    ///     单张卡牌的附加费用集合。
    /// </summary>
    public sealed class SecondaryResourceCostSet
    {
        private readonly Dictionary<string, List<SecondaryResourceCostLayer>> _costs =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     True when at least one material cost is attached.
        ///     至少附加了一个实际费用时为 true。
        /// </summary>
        public bool HasCosts =>
            _costs.Values.SelectMany(static layers => layers).Any(static layer => layer.Cost.IsMaterial);

        /// <summary>
        ///     Returns resource ids that currently have attached layers.
        ///     返回当前具有附加层的资源 id。
        /// </summary>
        public IReadOnlyList<string> ResourceIds =>
            _costs.Keys.OrderBy(static id => id, StringComparer.Ordinal).ToArray();

        /// <summary>
        ///     Raised after attached secondary costs change.
        ///     在附加次级费用变化后触发。
        /// </summary>
        public event Action? Changed;

        /// <summary>
        ///     Sets the permanent fixed cost for one resource.
        ///     设置单个资源的永久固定费用。
        /// </summary>
        public SecondaryResourceCostSet Set(string resourceId, int amount)
        {
            return Set(resourceId, new SecondaryResourceCost(Math.Max(0, amount)));
        }

        /// <summary>
        ///     Sets a permanent cost descriptor for one resource.
        ///     设置单个资源的永久费用描述。
        /// </summary>
        public SecondaryResourceCostSet Set(string resourceId, SecondaryResourceCost cost)
        {
            return Set(resourceId, cost, SecondaryResourceCostDuration.Permanent);
        }

        /// <summary>
        ///     Sets a cost descriptor for one resource and duration.
        ///     为单个资源和持续时间设置费用描述。
        /// </summary>
        public SecondaryResourceCostSet Set(
            string resourceId,
            SecondaryResourceCost cost,
            SecondaryResourceCostDuration duration)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            ArgumentNullException.ThrowIfNull(cost);

            var layers = GetLayers(resourceId);
            layers.RemoveAll(layer => layer.Duration == duration);
            layers.Add(new(cost, duration));
            Changed?.Invoke();
            return this;
        }

        /// <summary>
        ///     Clears all layers for one resource.
        ///     清除单个资源的所有费用层。
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
        ///     Clears layers for the specified duration.
        ///     清除指定持续时间的费用层。
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
        ///     Gets the active cost descriptor for a resource.
        ///     获取某个资源当前生效的费用描述。
        /// </summary>
        public SecondaryResourceCost Get(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            if (!_costs.TryGetValue(resourceId.Trim(), out var layers) || layers.Count == 0)
                return SecondaryResourceCost.Free;

            return layers[^1].Cost;
        }

        /// <summary>
        ///     Returns active costs in deterministic order.
        ///     按确定性顺序返回当前生效费用。
        /// </summary>
        public IReadOnlyDictionary<string, SecondaryResourceCost> Snapshot()
        {
            return _costs
                .Select(pair => new KeyValuePair<string, SecondaryResourceCost>(pair.Key, pair.Value[^1].Cost))
                .Where(static pair => pair.Value.IsMaterial)
                .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }

        private List<SecondaryResourceCostLayer> GetLayers(string resourceId)
        {
            var id = resourceId.Trim();
            if (_costs.TryGetValue(id, out var layers)) return layers;
            layers = [];
            _costs[id] = layers;

            return layers;
        }
    }

    /// <summary>
    ///     Extension helpers for card-attached secondary costs.
    ///     卡牌附加次级费用的扩展辅助工具。
    /// </summary>
    public static class SecondaryResourceCardExtensions
    {
        private static readonly AttachedState<CardModel, SecondaryResourceCostSet> CostSets = new(() => new());

        /// <summary>
        ///     Gets this card's secondary-resource cost set.
        ///     获取此卡牌的次级资源费用集合。
        /// </summary>
        public static SecondaryResourceCostSet SecondaryCosts(this CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            return CostSets.GetOrCreate(card);
        }

        /// <summary>
        ///     Attempts to read existing secondary costs without creating a cost set.
        ///     尝试读取已有次级费用，不会创建费用集合。
        /// </summary>
        public static bool TryGetSecondaryCosts(this CardModel card, out SecondaryResourceCostSet costs)
        {
            ArgumentNullException.ThrowIfNull(card);
            return CostSets.TryGetValue(card, out costs!);
        }

        /// <summary>
        ///     Clears until-played secondary costs.
        ///     清除持续到打出为止的次级费用。
        /// </summary>
        public static bool ClearSecondaryCostsUntilPlayed(this CardModel card)
        {
            return card.TryGetSecondaryCosts(out var costs) &&
                   costs.ClearDuration(SecondaryResourceCostDuration.UntilPlayed);
        }

        /// <summary>
        ///     Clears this-turn secondary costs.
        ///     清除本回合次级费用。
        /// </summary>
        public static bool ClearSecondaryCostsThisTurn(this CardModel card)
        {
            return card.TryGetSecondaryCosts(out var costs) &&
                   costs.ClearDuration(SecondaryResourceCostDuration.ThisTurn);
        }

        internal static bool HasMaterialSecondaryCosts(this CardModel card)
        {
            return ModSecondaryResourceRegistry.HasAny &&
                   card.TryGetSecondaryCosts(out var costs) &&
                   costs.HasCosts;
        }
    }

    internal sealed record SecondaryResourceCostLayer(
        SecondaryResourceCost Cost,
        SecondaryResourceCostDuration Duration);

    /// <summary>
    ///     Resolved payment line for a single resource.
    ///     单个资源的已解析支付行。
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
        ///     True when the player has enough resource for this line.
        ///     玩家拥有足够资源支付该行时为 true。
        /// </summary>
        public bool IsAffordable => IsFree || AmountAvailable >= AmountToSpend;
    }

    /// <summary>
    ///     Resolved secondary-resource payment plan for a card play.
    ///     一次出牌的已解析次级资源支付计划。
    /// </summary>
    public sealed record SecondaryResourcePaymentPlan(
        CardModel Card,
        Player Player,
        bool IsFree,
        IReadOnlyList<SecondaryResourcePaymentLine> Lines)
    {
        /// <summary>
        ///     True when every line can be paid.
        ///     每一行都可支付时为 true。
        /// </summary>
        public bool IsAffordable => Lines.All(static line => line.IsAffordable);

        /// <summary>
        ///     True when at least one resource line exists.
        ///     至少存在一个资源行时为 true。
        /// </summary>
        public bool HasLines => Lines.Count > 0;

        /// <summary>
        ///     Empty plan with no secondary-resource work.
        ///     没有次级资源工作的空计划。
        /// </summary>
        public static SecondaryResourcePaymentPlan Empty(CardModel card, Player player, bool isFree = false)
        {
            return new(card, player, isFree, []);
        }
    }

    /// <summary>
    ///     Builds and commits secondary-resource card payment plans.
    ///     构建并提交卡牌的次级资源支付计划。
    /// </summary>
    public static class SecondaryResourcePaymentResolver
    {
        /// <summary>
        ///     Resolves secondary-resource costs for a card.
        ///     解析卡牌的次级资源费用。
        /// </summary>
        public static SecondaryResourcePaymentPlan Plan(CardModel card, bool isFree = false)
        {
            ArgumentNullException.ThrowIfNull(card);

            var player = card.Owner;
            if (!ModSecondaryResourceRegistry.HasAny ||
                player == null ||
                !card.TryGetSecondaryCosts(out var costs))
                return SecondaryResourcePaymentPlan.Empty(card, player!, isFree);

            var combatState = card.CombatState ?? player.Creature?.CombatState;
            if (combatState == null)
                return SecondaryResourcePaymentPlan.Empty(card, player, isFree);

            var lines = new List<SecondaryResourcePaymentLine>();
            foreach (var pair in costs.Snapshot())
            {
                if (!ModSecondaryResourceRegistry.TryGet(pair.Key, out var definition))
                    continue;

                lines.Add(ResolveLine(combatState, player, card, definition, pair.Value, isFree));
            }

            return new(card, player, isFree, lines);
        }

        /// <summary>
        ///     Returns whether a card can pay all secondary-resource costs.
        ///     返回卡牌是否可以支付所有次级资源费用。
        /// </summary>
        public static bool CanPay(CardModel card)
        {
            return Plan(card).IsAffordable;
        }

        /// <summary>
        ///     Commits spending for a resolved plan and returns its ledger.
        ///     提交已解析计划的消耗，并返回 ledger。
        /// </summary>
        public static async Task<SecondaryResourcePlayLedger> Commit(
            SecondaryResourcePaymentPlan plan,
            AbstractModel? source = null)
        {
            ArgumentNullException.ThrowIfNull(plan);

            var builder = new SecondaryResourcePlayLedgerBuilder(plan.Card, plan.Player, plan.IsFree);
            foreach (var line in plan.Lines)
            {
                if (!line.IsFree && line.AmountToSpend > 0)
                {
                    var spent = await SecondaryResourceCmd.Spend(
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
            return ledger;
        }

        /// <summary>
        ///     Creates and queues a free-play ledger without mutating resource amounts.
        ///     创建并排队免费打出的 ledger，不修改资源数量。
        /// </summary>
        public static SecondaryResourcePlayLedger CommitFree(SecondaryResourcePaymentPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan);

            var builder = new SecondaryResourcePlayLedgerBuilder(plan.Card, plan.Player, true);
            foreach (var line in plan.Lines)
                builder.Add(line with { IsFree = true, AmountToSpend = 0 });

            var ledger = builder.Build();
            SecondaryResourcePlayLedgerRuntime.SetPending(plan.Card, ledger);
            return ledger;
        }

        private static SecondaryResourcePaymentLine ResolveLine(
            CombatStateLike combatState,
            Player player,
            CardModel card,
            SecondaryResourceDefinition definition,
            SecondaryResourceCost cost,
            bool isFree)
        {
            var available = SecondaryResourceCmd.Get(player, definition.Id);
            var modifiedCost = SecondaryResourceHook.ModifyCost(
                new(combatState, player, card, definition, cost.Amount),
                cost.Amount);
            var fixedCost = Math.Max(0, (int)Math.Ceiling(modifiedCost));

            if (!cost.CostsX)
                return new(definition.Id, definition, fixedCost, available, isFree ? 0 : fixedCost, fixedCost,
                    false, isFree);

            var xBase = Math.Max(0, available);
            var xValue = SecondaryResourceHook.ModifyXValue(
                new(combatState, player, card, definition, xBase),
                xBase);
            xValue = Math.Max(0, xValue) * cost.XMultiplier;
            return new(definition.Id, definition, fixedCost, available, isFree ? 0 : available, xValue, true, isFree);
        }
    }
}
