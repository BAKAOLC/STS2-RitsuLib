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
    /// </summary>
    [Flags]
    public enum RuntimeAssetRefreshScope
    {
        /// <summary>
        ///     No refresh requested.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Reload card visuals.
        /// </summary>
        Cards = 1 << 0,

        /// <summary>
        ///     Reload relic visuals.
        /// </summary>
        Relics = 1 << 1,

        /// <summary>
        ///     Reload potion visuals.
        /// </summary>
        Potions = 1 << 2,

        /// <summary>
        ///     Reload power visuals.
        /// </summary>
        Powers = 1 << 3,

        /// <summary>
        ///     Reload orb visuals.
        /// </summary>
        Orbs = 1 << 4,

        /// <summary>
        ///     Refresh all currently supported safe runtime categories.
        /// </summary>
        AllSafe = Cards | Relics | Potions | Powers | Orbs,
    }

    /// <summary>
    ///     Coalesces runtime visual refresh requests for commonly safe node types.
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
        /// </summary>
        public static void RequestCardsWhere(Predicate<CardModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingCardRules, rule, RuntimeAssetRefreshScope.Cards);
        }

        /// <summary>
        ///     Requests relic-node reloads for relics matched by <paramref name="rule" />.
        /// </summary>
        public static void RequestRelicsWhere(Predicate<RelicModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingRelicRules, rule, RuntimeAssetRefreshScope.Relics);
        }

        /// <summary>
        ///     Requests potion-node reloads for potions matched by <paramref name="rule" />.
        /// </summary>
        public static void RequestPotionsWhere(Predicate<PotionModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingPotionRules, rule, RuntimeAssetRefreshScope.Potions);
        }

        /// <summary>
        ///     Requests power-node reloads for powers matched by <paramref name="rule" />.
        /// </summary>
        public static void RequestPowersWhere(Predicate<PowerModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingPowerRules, rule, RuntimeAssetRefreshScope.Powers);
        }

        /// <summary>
        ///     Requests orb-node visual updates for orbs matched by <paramref name="rule" />.
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
                cardRules = PendingCardRules.ToArray();
                relicRules = PendingRelicRules.ToArray();
                potionRules = PendingPotionRules.ToArray();
                powerRules = PendingPowerRules.ToArray();
                orbRules = PendingOrbRules.ToArray();
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
