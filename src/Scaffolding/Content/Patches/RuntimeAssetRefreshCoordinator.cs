using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Runtime refresh categories for safe node-level visual reloads.
    ///     用于安全节点级视觉重载的运行时刷新类别。
    /// </summary>
    [Flags]
    public enum RuntimeAssetRefreshScope
    {
        /// <summary>
        ///     No refresh requested.
        ///     未请求刷新。
        /// </summary>
        None = 0,

        /// <summary>
        ///     Reload card visuals.
        ///     重新加载卡牌视觉。
        /// </summary>
        Cards = 1 << 0,

        /// <summary>
        ///     Reload relic visuals.
        ///     重新加载遗物视觉。
        /// </summary>
        Relics = 1 << 1,

        /// <summary>
        ///     Reload potion visuals.
        ///     重新加载药水视觉。
        /// </summary>
        Potions = 1 << 2,

        /// <summary>
        ///     Reload power visuals.
        ///     重新加载能力视觉。
        /// </summary>
        Powers = 1 << 3,

        /// <summary>
        ///     Reload orb visuals.
        ///     重新加载充能球视觉。
        /// </summary>
        Orbs = 1 << 4,

        /// <summary>
        ///     Refresh all currently supported safe runtime categories.
        /// </summary>
        AllSafe = Cards | Relics | Potions | Powers | Orbs,
    }

    /// <summary>
    ///     Coalesces runtime visual refresh requests for commonly safe node types.
    ///     合并常见安全节点类型的运行时视觉刷新请求。
    /// </summary>
    public static class RuntimeAssetRefreshCoordinator
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Action<NCard>? ReloadCard =
            AccessTools.Method(typeof(NCard), "Reload")?.CreateDelegate<Action<NCard>>();

        private static RuntimeAssetRefreshScope _pendingScope;
        private static bool _flushScheduled;
        private static readonly List<Predicate<CardModel>> PendingCardRules = [];
        private static readonly List<Predicate<RelicModel>> PendingRelicRules = [];
        private static readonly List<Predicate<PotionModel>> PendingPotionRules = [];
        private static readonly List<Predicate<PowerModel>> PendingPowerRules = [];
        private static readonly List<Predicate<OrbModel>> PendingOrbRules = [];

        /// <summary>
        ///     Requests a deferred refresh pass for the supplied <paramref name="scope" />.
        ///     为提供的 <paramref name="scope" /> 请求一次延迟刷新。
        /// </summary>
        public static void Request(RuntimeAssetRefreshScope scope = RuntimeAssetRefreshScope.AllSafe)
        {
            if (scope == RuntimeAssetRefreshScope.None)
                return;

            bool shouldSchedule;
            lock (SyncRoot)
            {
                _pendingScope |= scope;
                if (_flushScheduled)
                    return;
                _flushScheduled = true;
                shouldSchedule = true;
            }

            if (!shouldSchedule)
                return;

            Callable.From(FlushPending).CallDeferred();
        }

        /// <summary>
        ///     Requests card-node reloads for cards matched by <paramref name="rule" />.
        ///     为 <paramref name="rule" /> 匹配的卡牌请求卡牌节点重载。
        /// </summary>
        public static void RequestCardsWhere(Predicate<CardModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingCardRules, rule, RuntimeAssetRefreshScope.Cards);
        }

        /// <summary>
        ///     Requests relic-node reloads for relics matched by <paramref name="rule" />.
        ///     为 <paramref name="rule" /> 匹配的遗物请求遗物节点重载。
        /// </summary>
        public static void RequestRelicsWhere(Predicate<RelicModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingRelicRules, rule, RuntimeAssetRefreshScope.Relics);
        }

        /// <summary>
        ///     Requests potion-node reloads for potions matched by <paramref name="rule" />.
        ///     为 <paramref name="rule" /> 匹配的药水请求药水节点重载。
        /// </summary>
        public static void RequestPotionsWhere(Predicate<PotionModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingPotionRules, rule, RuntimeAssetRefreshScope.Potions);
        }

        /// <summary>
        ///     Requests power-node reloads for powers matched by <paramref name="rule" />.
        ///     为 <paramref name="rule" /> 匹配的能力请求能力节点重载。
        /// </summary>
        public static void RequestPowersWhere(Predicate<PowerModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingPowerRules, rule, RuntimeAssetRefreshScope.Powers);
        }

        /// <summary>
        ///     Requests orb-node visual updates for orbs matched by <paramref name="rule" />.
        ///     为 <paramref name="rule" /> 匹配的充能球请求充能球节点视觉更新。
        /// </summary>
        public static void RequestOrbsWhere(Predicate<OrbModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingOrbRules, rule, RuntimeAssetRefreshScope.Orbs);
        }

        private static void FlushPending()
        {
            RuntimeAssetRefreshScope scope;
            Predicate<CardModel>[] cardRules;
            Predicate<RelicModel>[] relicRules;
            Predicate<PotionModel>[] potionRules;
            Predicate<PowerModel>[] powerRules;
            Predicate<OrbModel>[] orbRules;
            lock (SyncRoot)
            {
                scope = _pendingScope;
                _pendingScope = RuntimeAssetRefreshScope.None;
                _flushScheduled = false;
                cardRules = [.. PendingCardRules];
                relicRules = [.. PendingRelicRules];
                potionRules = [.. PendingPotionRules];
                powerRules = [.. PendingPowerRules];
                orbRules = [.. PendingOrbRules];
                PendingCardRules.Clear();
                PendingRelicRules.Clear();
                PendingPotionRules.Clear();
                PendingPowerRules.Clear();
                PendingOrbRules.Clear();
            }

            if (scope == RuntimeAssetRefreshScope.None)
                return;

            if (Engine.GetMainLoop() is not SceneTree tree || !GodotObject.IsInstanceValid(tree.Root))
                return;

            foreach (var node in EnumerateDescendants(tree.Root))
            {
                if ((scope & RuntimeAssetRefreshScope.Cards) != 0 && node is NCard card)
                {
                    if (card.Model != null && ShouldApply(card.Model, cardRules))
                        ReloadCard?.Invoke(card);
                    continue;
                }

                if ((scope & RuntimeAssetRefreshScope.Relics) != 0 && node is NRelic relic)
                {
                    if (ShouldApply(relic.Model, relicRules))
                        relic.Model = relic.Model;
                    continue;
                }

                if ((scope & RuntimeAssetRefreshScope.Potions) != 0 && node is NPotion potion)
                {
                    if (ShouldApply(potion.Model, potionRules))
                        potion.Model = potion.Model;
                    continue;
                }

                if ((scope & RuntimeAssetRefreshScope.Powers) != 0 && node is NPower power)
                {
                    if (ShouldApply(power.Model, powerRules))
                        power.Model = power.Model;
                    continue;
                }

                // ReSharper disable once InvertIf
                if ((scope & RuntimeAssetRefreshScope.Orbs) != 0 && node is NOrb orb)
                    if (ShouldApply(orb.Model, orbRules))
                        orb.UpdateVisuals(false);
            }
        }

        private static void EnqueueRule<TModel>(List<Predicate<TModel>> bucket, Predicate<TModel> rule,
            RuntimeAssetRefreshScope scope)
            where TModel : class
        {
            bool shouldSchedule;
            lock (SyncRoot)
            {
                bucket.Add(rule);
                _pendingScope |= scope;
                if (_flushScheduled)
                    return;
                _flushScheduled = true;
                shouldSchedule = true;
            }

            if (!shouldSchedule)
                return;

            Callable.From(FlushPending).CallDeferred();
        }

        private static bool ShouldApply<TModel>(TModel? model, IReadOnlyList<Predicate<TModel>> rules)
            where TModel : class
        {
            if (model == null)
                return false;
            if (rules.Count == 0)
                return true;
            foreach (var rule in rules)
                try
                {
                    if (rule(model))
                        return true;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn($"[Assets] Refresh rule failed: {ex.Message}");
                }

            return false;
        }

        private static IEnumerable<Node> EnumerateDescendants(Node root)
        {
            var stack = new Stack<Node>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!GodotObject.IsInstanceValid(current))
                    continue;

                yield return current;

                for (var i = current.GetChildCount() - 1; i >= 0; i--)
                    if (current.GetChild(i) is { } child)
                        stack.Push(child);
            }
        }
    }
}
