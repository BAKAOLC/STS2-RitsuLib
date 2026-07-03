#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.FreePlay;
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

        internal bool HasPermanentLayers =>
            _costs.Values
                .SelectMany(static layers => layers)
                .Any(static layer => layer.Duration == SecondaryResourceCostDuration.Permanent);

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
            return Set(resourceId, cost, duration, null);
        }

        /// <summary>
        ///     Sets a permanent fixed cost that can still be played with a shortfall.
        ///     设置资源不足时仍可打出的永久固定费用。
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
        ///     Sets a cost that can still be played with a shortfall.
        ///     设置资源不足时仍可打出的费用。
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
        ///     Sets a cost descriptor for one resource and duration with an explicit shortfall policy.
        ///     为单个资源和持续时间设置带显式短缺策略的费用描述。
        /// </summary>
        public SecondaryResourceCostSet Set(
            string resourceId,
            SecondaryResourceCost cost,
            SecondaryResourceCostDuration duration,
            SecondaryResourceInsufficientPayment? insufficientPayment)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            ArgumentNullException.ThrowIfNull(cost);

            var layers = GetLayers(resourceId);
            layers.RemoveAll(layer => layer.Duration == duration);
            layers.Add(new(cost, duration, insufficientPayment));
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

        internal IReadOnlyList<SecondaryResourcePlayUse> SnapshotUses()
        {
            return _costs
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
                .OrderBy(static use => use.Id, StringComparer.Ordinal)
                .ToArray();
        }

        internal SecondaryResourceCostSet Clone()
        {
            var clone = new SecondaryResourceCostSet();
            foreach (var (resourceId, layers) in _costs)
                clone._costs[resourceId] = layers.ToList();

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
                        _costs[resourceId] = permanentLayers.ToList();
                    changed = true;
                }

            if (changed)
                Changed?.Invoke();

            return changed;
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
        SecondaryResourceCostDuration Duration,
        SecondaryResourceInsufficientPayment? InsufficientPayment = null);

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
        ///     Unpaid amount for this line after applying the selected spend behavior.
        ///     应用所选消耗行为后该行未支付的数量。
        /// </summary>
        public int Shortfall { get; init; }

        /// <summary>
        ///     Shortfall before replacement payments cover any part of it.
        ///     替代支付覆盖前的原始短缺数量。
        /// </summary>
        public int OriginalShortfall { get; init; }

        /// <summary>
        ///     Shortfall amount covered by a replacement payment.
        ///     由替代支付覆盖的短缺数量。
        /// </summary>
        public int CoveredShortfall { get; init; }

        /// <summary>
        ///     Shortfall policy resolved for this line.
        ///     该行解析出的短缺策略。
        /// </summary>
        public SecondaryResourceInsufficientPayment InsufficientPayment { get; init; } =
            SecondaryResourceInsufficientPayment.BlockPlay;

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
        ///     True when this line is a repeatable extra spend.
        ///     该行为可重复额外支付时为 true。
        /// </summary>
        public bool IsExtraSpend => Kind == SecondaryResourceUseKind.ExtraSpend;

        /// <summary>
        ///     True when this line allows the card play to proceed.
        ///     该行允许卡牌继续打出时为 true。
        /// </summary>
        public bool CanPlay => !BlocksPlay || ((IsAffordable || IsShortfallPlayable || IsShortfallCovered) && CanSpend);

        /// <summary>
        ///     True when this required line is short on resource but its policy allows the play.
        ///     必需行资源不足但策略允许出牌时为 true。
        /// </summary>
        public bool IsShortfallPlayable =>
            BlocksPlay &&
            Activated &&
            Shortfall > 0 &&
            InsufficientPayment.AllowsPlay;

        /// <summary>
        ///     True when a replacement payment fully covered the resource shortfall.
        ///     替代支付已完全覆盖资源短缺时为 true。
        /// </summary>
        public bool IsShortfallCovered =>
            BlocksPlay &&
            Activated &&
            OriginalShortfall > 0 &&
            CoveredShortfall >= OriginalShortfall;

        /// <summary>
        ///     Replacement-payment resolution chosen for this line.
        ///     该行选中的替代支付解析结果。
        /// </summary>
        public SecondaryResourceShortfallResolution ShortfallResolution { get; init; } =
            SecondaryResourceShortfallResolution.None;

        /// <summary>
        ///     Number of full extra-spend stacks paid by this line.
        ///     该行支付的完整额外消耗层数。
        /// </summary>
        public int ExtraStacks { get; init; }

        /// <summary>
        ///     Amount spent as repeatable extra payment by this line.
        ///     该行作为可重复额外支付消耗的数量。
        /// </summary>
        public int ExtraAmountToSpend { get; init; }

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
        ///     Fixed cost before the current upgrade preview, when this line is being shown as an upgrade preview.
        ///     当前行作为升级预览显示时，升级前的固定费用。
        /// </summary>
        public int? UpgradePreviewBaseCost { get; init; }

        /// <summary>
        ///     True when this line's displayed fixed cost differs because of a runtime cost effect.
        ///     该行显示的固定费用因运行时费用效果而变化时为 true。
        /// </summary>
        public bool HasRuntimeCostModifier { get; init; }
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

            var player = TryGetOwner(card);
            if (!ModSecondaryResourceRegistry.HasAny)
                return SecondaryResourcePaymentPlan.Empty(card, player, freeMode.IsFree);

            var uses = SnapshotUses(card);
            if (uses.Count == 0)
                return SecondaryResourcePaymentPlan.Empty(card, player, freeMode.IsFree);

            if (player == null)
                return PlanPreview(card, uses, freeMode);

            var combatState = card.CombatState ?? player.Creature?.CombatState;
            if (combatState == null)
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
            await RunShortfallPayments(plan, ledger, source ?? plan.Card);
            return ledger;
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

            return uses
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
            var fixedCost = Math.Max(0, (int)Math.Ceiling(localCost));
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
            var fixedCost = Math.Max(0, (int)Math.Ceiling(modifiedCost));
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
