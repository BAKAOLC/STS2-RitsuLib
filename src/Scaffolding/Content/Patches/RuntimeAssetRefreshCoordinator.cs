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
    ///     <para xml:lang="en">Defines runtime refresh categories for supported node-level visual reloads.</para>
    ///     <para xml:lang="zh-CN">定义运行时支持按节点重新加载视觉效果的刷新类别。</para>
    /// </summary>
    [Flags]
    public enum RuntimeAssetRefreshScope
    {
        /// <summary>
        ///     <para xml:lang="en">Requests no refresh.</para>
        ///     <para xml:lang="zh-CN">不请求刷新。</para>
        /// </summary>
        None = 0,

        /// <summary>
        ///     <para xml:lang="en">Reloads card visuals.</para>
        ///     <para xml:lang="zh-CN">重新加载卡牌视觉效果。</para>
        /// </summary>
        Cards = 1 << 0,

        /// <summary>
        ///     <para xml:lang="en">Reloads relic visuals.</para>
        ///     <para xml:lang="zh-CN">重新加载遗物视觉效果。</para>
        /// </summary>
        Relics = 1 << 1,

        /// <summary>
        ///     <para xml:lang="en">Reloads potion visuals.</para>
        ///     <para xml:lang="zh-CN">重新加载药水视觉效果。</para>
        /// </summary>
        Potions = 1 << 2,

        /// <summary>
        ///     <para xml:lang="en">Reloads power visuals.</para>
        ///     <para xml:lang="zh-CN">重新加载能力视觉效果。</para>
        /// </summary>
        Powers = 1 << 3,

        /// <summary>
        ///     <para xml:lang="en">Reloads orb visuals.</para>
        ///     <para xml:lang="zh-CN">重新加载充能球视觉效果。</para>
        /// </summary>
        Orbs = 1 << 4,

        /// <summary>
        ///     <para xml:lang="en">Refreshes every category currently supported by the runtime coordinator.</para>
        ///     <para xml:lang="zh-CN">刷新运行时协调器当前支持的所有类别。</para>
        /// </summary>
        AllSafe = Cards | Relics | Potions | Powers | Orbs,
    }

    /// <summary>
    ///     <para xml:lang="en">Coalesces runtime visual refresh requests for supported node types.</para>
    ///     <para xml:lang="zh-CN">合并针对受支持节点类型的运行时视觉刷新请求。</para>
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
        ///     <para xml:lang="en">Requests a deferred refresh pass for the specified <paramref name="scope" />.</para>
        ///     <para xml:lang="zh-CN">为指定的 <paramref name="scope" /> 请求一次延迟刷新。</para>
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
        ///     <para xml:lang="en">Requests card-node reloads for cards matched by <paramref name="rule" />.</para>
        ///     <para xml:lang="zh-CN">请求重新加载 <paramref name="rule" /> 所匹配卡牌的节点。</para>
        /// </summary>
        public static void RequestCardsWhere(Predicate<CardModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingCardRules, rule, RuntimeAssetRefreshScope.Cards);
        }

        /// <summary>
        ///     <para xml:lang="en">Requests relic-node reloads for relics matched by <paramref name="rule" />.</para>
        ///     <para xml:lang="zh-CN">请求重新加载 <paramref name="rule" /> 所匹配遗物的节点。</para>
        /// </summary>
        public static void RequestRelicsWhere(Predicate<RelicModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingRelicRules, rule, RuntimeAssetRefreshScope.Relics);
        }

        /// <summary>
        ///     <para xml:lang="en">Requests potion-node reloads for potions matched by <paramref name="rule" />.</para>
        ///     <para xml:lang="zh-CN">请求重新加载 <paramref name="rule" /> 所匹配药水的节点。</para>
        /// </summary>
        public static void RequestPotionsWhere(Predicate<PotionModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingPotionRules, rule, RuntimeAssetRefreshScope.Potions);
        }

        /// <summary>
        ///     <para xml:lang="en">Requests power-node reloads for powers matched by <paramref name="rule" />.</para>
        ///     <para xml:lang="zh-CN">请求重新加载 <paramref name="rule" /> 所匹配能力的节点。</para>
        /// </summary>
        public static void RequestPowersWhere(Predicate<PowerModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingPowerRules, rule, RuntimeAssetRefreshScope.Powers);
        }

        /// <summary>
        ///     <para xml:lang="en">Requests orb-node visual updates for orbs matched by <paramref name="rule" />.</para>
        ///     <para xml:lang="zh-CN">请求更新 <paramref name="rule" /> 所匹配充能球的节点视觉效果。</para>
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
