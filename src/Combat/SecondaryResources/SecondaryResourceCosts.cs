#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Models;
using STS2RitsuLib.Models.Capabilities;
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

        internal bool HasLayers => _costs.Count > 0;

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

        internal SecondaryResourceCostSet Clone()
        {
            var clone = new SecondaryResourceCostSet();
            foreach (var (resourceId, layers) in _costs)
                clone._costs[resourceId] = layers.ToList();

            return clone;
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
    public static partial class SecondaryResourceCardExtensions
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
            var changed = card.TryGetSecondaryCosts(out var costs) &&
                          costs.ClearDuration(SecondaryResourceCostDuration.UntilPlayed);
            return card.ClearSecondaryResourceUsesUntilPlayed() || changed;
        }

        /// <summary>
        ///     Clears this-turn secondary costs.
        ///     清除本回合次级费用。
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
    }

    internal static class SecondaryResourceCloneBridge
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            ModelCloneRegistry.For(Const.ModId)
                .Register<CardModel>("secondary_resource_costs", CopySecondaryCosts);
        }

        private static void CopySecondaryCosts(CardModel prototype, CardModel clone)
        {
            prototype.CopySecondaryCostsTo(clone);
            prototype.CopySecondaryResourceUsesTo(clone);
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
        public bool IsAffordable => IsPreview || IsFree || AmountAvailable >= Cost;

        /// <summary>
        ///     True when spend hooks allow this line to spend its resource.
        ///     spend hook 允许该行消耗资源时为 true。
        /// </summary>
        public bool SpendAllowed { get; init; } = true;

        /// <summary>
        ///     True when this line can execute its resource spend.
        ///     该行可以执行资源消耗时为 true。
        /// </summary>
        public bool CanSpend => IsPreview || IsFree || AmountToSpend <= 0 || SpendAllowed;

        /// <summary>
        ///     True when this line cannot block card play.
        ///     该行不会阻止卡牌打出时为 true。
        /// </summary>
        public bool IsOptional => !BlocksPlay;

        /// <summary>
        ///     True when this line allows the card play to proceed.
        ///     该行允许卡牌继续打出时为 true。
        /// </summary>
        public bool CanPlay => !BlocksPlay || (IsAffordable && CanSpend);

        /// <summary>
        ///     Stable play-use id for this line.
        ///     该行的稳定出牌条款 id。
        /// </summary>
        public string UseId { get; init; } = ResourceId;

        /// <summary>
        ///     Semantic role for this line.
        ///     该行的语义角色。
        /// </summary>
        public SecondaryResourceUseKind Kind { get; init; } = SecondaryResourceUseKind.RequiredCost;

        /// <summary>
        ///     True when this line can block card play if it cannot be paid.
        ///     该行无法支付时会阻止卡牌打出。
        /// </summary>
        public bool BlocksPlay { get; init; } = true;

        /// <summary>
        ///     True when this line is active for the current play plan.
        ///     该行在当前出牌计划中已激活。
        /// </summary>
        public bool Activated { get; init; }

        /// <summary>
        ///     True when the line was resolved without a player/combat owner and is only suitable for display.
        ///     没有玩家/战斗 owner 时解析出的展示用行；只适合用于 UI 展示。
        /// </summary>
        public bool IsPreview { get; init; }

        /// <summary>
        ///     Current unmodified fixed cost used for card-cost color comparison.
        ///     用于卡牌费用颜色比较的当前未修改固定费用。
        /// </summary>
        public int BaseCost { get; init; } = Cost;

        /// <summary>
        ///     Canonical fixed cost before upgrade-preview changes.
        ///     升级预览变化前的 canonical 固定费用。
        /// </summary>
        public int CanonicalCost { get; init; } = Cost;
    }

    /// <summary>
    ///     Resolved secondary-resource payment plan for a card play.
    ///     一次出牌的已解析次级资源支付计划。
    /// </summary>
    public sealed record SecondaryResourcePaymentPlan(
        CardModel Card,
        Player? Player,
        bool IsFree,
        IReadOnlyList<SecondaryResourcePaymentLine> Lines)
    {
        /// <summary>
        ///     True when every line can be paid.
        ///     每一行都可支付时为 true。
        /// </summary>
        public bool IsAffordable => Lines.All(static line => line.CanPlay);

        /// <summary>
        ///     True when at least one resource line exists.
        ///     至少存在一个资源行时为 true。
        /// </summary>
        public bool HasLines => Lines.Count > 0;

        /// <summary>
        ///     True when the plan was resolved without a player/combat owner and must not be committed.
        ///     没有玩家/战斗 owner 时解析出的展示用计划；不能提交消耗。
        /// </summary>
        public bool IsPreview => Player == null;

        /// <summary>
        ///     Empty plan with no secondary-resource work.
        ///     没有次级资源工作的空计划。
        /// </summary>
        public static SecondaryResourcePaymentPlan Empty(CardModel card, Player? player, bool isFree = false)
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
        private const string CardUseContributorSurface = "secondary-resource/card-uses";
        private const string CardCostContributorSurface = "secondary-resource/card-cost";

        /// <summary>
        ///     Resolves secondary-resource costs for a card.
        ///     解析卡牌的次级资源费用。
        /// </summary>
        public static SecondaryResourcePaymentPlan Plan(
            CardModel card,
            bool isFree = false,
            AbstractModel? source = null)
        {
            ArgumentNullException.ThrowIfNull(card);

            var player = TryGetOwner(card);
            if (!ModSecondaryResourceRegistry.HasAny)
                return SecondaryResourcePaymentPlan.Empty(card, player, isFree);

            var uses = SnapshotUses(card);
            if (uses.Count == 0)
                return SecondaryResourcePaymentPlan.Empty(card, player, isFree);

            if (player == null)
                return PlanPreview(card, uses, isFree);

            var combatState = card.CombatState ?? player.Creature?.CombatState;
            if (combatState == null)
                return PlanPreview(card, uses, isFree);

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

                var line = ResolveLine(combatState, player, card, definition, use, available, isFree, source);
                lines.Add(line);
                remainingByResource[definition.Id] = Math.Max(0, available - line.AmountToSpend);
            }

            return new(card, player, isFree, lines);
        }

        private static Player? TryGetOwner(CardModel card)
        {
            return card.IsCanonical ? null : card.Owner;
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
                if (!line.CanPlay)
                    throw new InvalidOperationException(
                        $"Cannot commit unplayable secondary resource payment for {line.ResourceId} on {plan.Card.Id.Entry}.");

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
            {
                var freeLine = line.Kind == SecondaryResourceUseKind.OptionalSpend
                    ? line with { IsFree = true, AmountToSpend = 0, Value = 0, Activated = true }
                    : line with { IsFree = true, AmountToSpend = 0, Activated = true };
                builder.Add(freeLine);
            }

            var ledger = builder.Build();
            SecondaryResourcePlayLedgerRuntime.SetPending(plan.Card, ledger);
            return ledger;
        }

        private static IReadOnlyList<SecondaryResourcePlayUse> SnapshotUses(CardModel card)
        {
            var uses = new List<SecondaryResourcePlayUse>();
            if (card.TryGetSecondaryCosts(out var costs))
                uses.AddRange(costs.Snapshot().Select(pair =>
                    new SecondaryResourcePlayUse(pair.Key, pair.Key, pair.Value,
                        SecondaryResourceUseKind.RequiredCost)));

            if (card.TryGetSecondaryResourceUses(out var playUses))
                uses.AddRange(playUses.Snapshot());

            uses.AddRange(GetCapabilityUses(card));

            return uses
                .Where(static use => use.IsMaterial)
                .OrderBy(static use => use.Kind == SecondaryResourceUseKind.RequiredCost ? 0 : 1)
                .ThenBy(static use => use.Id, StringComparer.Ordinal)
                .ToArray();
        }

        private static SecondaryResourcePaymentPlan PlanPreview(
            CardModel card,
            IReadOnlyList<SecondaryResourcePlayUse> uses,
            bool isFree)
        {
            var lines = new List<SecondaryResourcePaymentLine>();
            foreach (var use in uses)
            {
                if (!ModSecondaryResourceRegistry.TryGet(use.ResourceId, out var definition))
                    continue;

                lines.Add(ResolvePreviewLine(card, definition, use, isFree));
            }

            return new(card, null, isFree, lines);
        }

        private static SecondaryResourcePaymentLine ResolvePreviewLine(
            CardModel card,
            SecondaryResourceDefinition definition,
            SecondaryResourcePlayUse use,
            bool isFree)
        {
            var cost = use.Cost;
            var localCost = ModifyLocalCost(card, definition, use, cost.Amount);
            var baseCost = Math.Max(0, cost.Amount);
            var canonicalCost = ResolveCanonicalCost(card, use);
            var fixedCost = Math.Max(0, (int)Math.Ceiling(localCost));
            var displayCost = isFree ? 0 : fixedCost;
            if (!cost.CostsX)
                return new(definition.Id, definition, displayCost, 0, isFree ? 0 : fixedCost, fixedCost, false, isFree)
                {
                    UseId = use.Id,
                    Kind = use.Kind,
                    BlocksPlay = use.Kind == SecondaryResourceUseKind.RequiredCost,
                    Activated = use.Kind == SecondaryResourceUseKind.RequiredCost || isFree,
                    IsPreview = true,
                    BaseCost = baseCost,
                    CanonicalCost = canonicalCost,
                };

            return new(definition.Id, definition, fixedCost, 0, 0, 0, true, isFree)
            {
                UseId = use.Id,
                Kind = use.Kind,
                BlocksPlay = use.Kind == SecondaryResourceUseKind.RequiredCost,
                Activated = isFree,
                IsPreview = true,
                BaseCost = baseCost,
                CanonicalCost = canonicalCost,
            };
        }

        private static SecondaryResourcePaymentLine ResolveLine(
            CombatStateLike combatState,
            Player player,
            CardModel card,
            SecondaryResourceDefinition definition,
            SecondaryResourcePlayUse use,
            int available,
            bool isFree,
            AbstractModel? source)
        {
            var cost = use.Cost;
            var localCost = ModifyLocalCost(card, definition, use, cost.Amount);
            var baseCost = Math.Max(0, cost.Amount);
            var canonicalCost = ResolveCanonicalCost(card, use);
            var modifiedCost = SecondaryResourceHook.ModifyCost(
                new(combatState, player, card, definition, localCost),
                localCost);
            var fixedCost = Math.Max(0, (int)Math.Ceiling(modifiedCost));
            var displayCost = isFree ? 0 : fixedCost;
            var isRequired = use.Kind == SecondaryResourceUseKind.RequiredCost;

            if (!cost.CostsX)
            {
                var activated = isFree || available >= fixedCost;
                var amountToSpend = !isRequired && !activated
                    ? 0
                    : isFree
                        ? 0
                        : fixedCost;
                var spendAllowed = CanSpend(combatState, player, card, definition, amountToSpend, source);
                if (!isRequired && !spendAllowed)
                {
                    activated = false;
                    amountToSpend = 0;
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
                    CanonicalCost = canonicalCost,
                };
            }

            var xBase = Math.Max(0, available);
            var xValue = SecondaryResourceHook.ModifyXValue(
                new(combatState, player, card, definition, xBase),
                xBase);
            xValue = Math.Max(0, xValue) * cost.XMultiplier;
            var xActivated = isRequired || isFree || available > 0;
            var amountToSpendForX = isFree || !xActivated ? 0 : available;
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
                CanonicalCost = canonicalCost,
            };
        }

        private static IEnumerable<SecondaryResourcePlayUse> GetCapabilityUses(CardModel card)
        {
            foreach (var capability in ModelCapabilityHost.GetCapabilities<ICardSecondaryResourceUseContributor>(card))
            {
                IEnumerable<SecondaryResourcePlayUse> uses = [];
                ModelCapabilityHost.TryRun(
                    (IModelCapability)capability,
                    card,
                    CardUseContributorSurface,
                    () => uses = capability.GetSecondaryResourceUses(card) ?? []);

                foreach (var use in uses)
                    if (use.IsMaterial)
                        yield return use;
            }
        }

        private static int ResolveCanonicalCost(CardModel card, SecondaryResourcePlayUse use)
        {
            var fallback = Math.Max(0, use.Cost.Amount);
            if (card.CanonicalInstance == card)
                return fallback;

            return TryFindCanonicalUseCost(card.CanonicalInstance, use, out var canonicalCost)
                ? Math.Max(0, canonicalCost.Amount)
                : fallback;
        }

        private static bool TryFindCanonicalUseCost(
            CardModel canonicalCard,
            SecondaryResourcePlayUse use,
            out SecondaryResourceCost cost)
        {
            if (canonicalCard.TryGetSecondaryResourceUses(out var uses))
            {
                var canonicalUse = uses.Snapshot().FirstOrDefault(candidate =>
                    string.Equals(candidate.Id, use.Id, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(candidate.ResourceId, use.ResourceId, StringComparison.OrdinalIgnoreCase) &&
                    candidate.Kind == use.Kind);
                if (canonicalUse != null)
                {
                    cost = canonicalUse.Cost;
                    return true;
                }
            }

            if (use.Kind == SecondaryResourceUseKind.RequiredCost &&
                string.Equals(use.Id, use.ResourceId, StringComparison.OrdinalIgnoreCase) &&
                canonicalCard.TryGetSecondaryCosts(out var costs) &&
                costs.Snapshot().TryGetValue(use.ResourceId, out cost!))
                return true;

            cost = null!;
            return false;
        }

        private static decimal ModifyLocalCost(
            CardModel card,
            SecondaryResourceDefinition definition,
            SecondaryResourcePlayUse use,
            decimal cost)
        {
            var result = cost;
            var context = new SecondaryResourceCardCostContext(card, definition, use, cost);
            foreach (var capability in ModelCapabilityHost.GetCapabilities<ICardSecondaryResourceCostContributor>(card))
                ModelCapabilityHost.TryRun(
                    (IModelCapability)capability,
                    card,
                    CardCostContributorSurface,
                    () => result = capability.ModifySecondaryResourceCost(context, result));

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
}
