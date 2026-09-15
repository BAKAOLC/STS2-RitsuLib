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
    ///     <para xml:lang="en">
    ///         Accepts refresh requests from any thread and coalesces them for the next main-thread frame.
    ///         Filtered requests in a category are combined with OR; an unrestricted request overrides those filters.
    ///         Predicates run on the main thread. Recoverable predicate or node-refresh failures are logged
    ///         without stopping the remaining refreshes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         接受任意线程发出的刷新请求，并合并到主线程的下一帧。同一类别的条件按“或”组合，
    ///         整类刷新优先于这些条件。条件回调在主线程执行；条件或节点刷新中的可恢复异常会记录到日志，
    ///         不会阻止其余刷新。
    ///     </para>
    /// </summary>
    public static class RuntimeAssetRefreshCoordinator
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Action<NCard> ReloadCard =
            (AccessTools.Method(typeof(NCard), "Reload") ??
             throw new MissingMethodException(typeof(NCard).FullName, "Reload"))
            .CreateDelegate<Action<NCard>>();

        private static RuntimeAssetRefreshScope _pendingScope;
        private static RuntimeAssetRefreshScope _unfilteredScope;
        private static bool _flushScheduled;
        private static readonly List<Predicate<CardModel>> PendingCardRules = [];
        private static readonly List<Predicate<RelicModel>> PendingRelicRules = [];
        private static readonly List<Predicate<PotionModel>> PendingPotionRules = [];
        private static readonly List<Predicate<PowerModel>> PendingPowerRules = [];
        private static readonly List<Predicate<OrbModel>> PendingOrbRules = [];

        /// <summary>
        ///     <para xml:lang="en">
        ///         Requests a deferred main-thread refresh for every live, ready node in the specified categories.
        ///         An unrestricted category takes precedence over filtered requests in the same pending batch.
        ///         Requests made during a refresh are processed on a later frame.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         请求在主线程延迟刷新指定类别中仍有效且已就绪的所有节点。同一批次中的整类刷新优先于
        ///         条件刷新；刷新期间发出的请求在后续帧处理。
        ///     </para>
        /// </summary>
        /// <param name="scope">
        ///     <para xml:lang="en">The supported categories to refresh; None is a no-op.</para>
        ///     <para xml:lang="zh-CN">要刷新的受支持类别；None 不执行操作。</para>
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en">The scope contains unsupported flags.</para>
        ///     <para xml:lang="zh-CN">刷新范围包含不受支持的标志位。</para>
        /// </exception>
        public static void Request(RuntimeAssetRefreshScope scope = RuntimeAssetRefreshScope.AllSafe)
        {
            if ((scope & ~RuntimeAssetRefreshScope.AllSafe) != 0)
                throw new ArgumentOutOfRangeException(nameof(scope));
            if (scope == RuntimeAssetRefreshScope.None)
                return;

            lock (SyncRoot)
            {
                _pendingScope |= scope;
                _unfilteredScope |= scope;
                ClearFilteredRules(scope);
                if (_flushScheduled)
                    return;
                _flushScheduled = true;
            }

            Callable.From(ScheduleNextFrame).CallDeferred();
        }

        /// <summary>
        ///     <para xml:lang="en">Requests card-node reloads for cards matched by <paramref name="rule" />.</para>
        ///     <para xml:lang="zh-CN">请求重新加载 <paramref name="rule" /> 所匹配卡牌的节点。</para>
        /// </summary>
        /// <param name="rule">
        ///     <para xml:lang="en">A main-thread predicate selecting the models whose ready nodes should reload.</para>
        ///     <para xml:lang="zh-CN">在主线程选择应重新加载其就绪节点的模型的条件回调。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">The predicate is null.</para>
        ///     <para xml:lang="zh-CN">条件回调为 null。</para>
        /// </exception>
        public static void RequestCardsWhere(Predicate<CardModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingCardRules, rule, RuntimeAssetRefreshScope.Cards);
        }

        /// <summary>
        ///     <para xml:lang="en">Requests relic-node reloads for relics matched by <paramref name="rule" />.</para>
        ///     <para xml:lang="zh-CN">请求重新加载 <paramref name="rule" /> 所匹配遗物的节点。</para>
        /// </summary>
        /// <param name="rule">
        ///     <para xml:lang="en">A main-thread predicate selecting the models whose ready nodes should reload.</para>
        ///     <para xml:lang="zh-CN">在主线程选择应重新加载其就绪节点的模型的条件回调。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">The predicate is null.</para>
        ///     <para xml:lang="zh-CN">条件回调为 null。</para>
        /// </exception>
        public static void RequestRelicsWhere(Predicate<RelicModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingRelicRules, rule, RuntimeAssetRefreshScope.Relics);
        }

        /// <summary>
        ///     <para xml:lang="en">Requests potion-node reloads for potions matched by <paramref name="rule" />.</para>
        ///     <para xml:lang="zh-CN">请求重新加载 <paramref name="rule" /> 所匹配药水的节点。</para>
        /// </summary>
        /// <param name="rule">
        ///     <para xml:lang="en">A main-thread predicate selecting the models whose ready nodes should reload.</para>
        ///     <para xml:lang="zh-CN">在主线程选择应重新加载其就绪节点的模型的条件回调。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">The predicate is null.</para>
        ///     <para xml:lang="zh-CN">条件回调为 null。</para>
        /// </exception>
        public static void RequestPotionsWhere(Predicate<PotionModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingPotionRules, rule, RuntimeAssetRefreshScope.Potions);
        }

        /// <summary>
        ///     <para xml:lang="en">Requests power-node reloads for powers matched by <paramref name="rule" />.</para>
        ///     <para xml:lang="zh-CN">请求重新加载 <paramref name="rule" /> 所匹配能力的节点。</para>
        /// </summary>
        /// <param name="rule">
        ///     <para xml:lang="en">A main-thread predicate selecting the models whose ready nodes should reload.</para>
        ///     <para xml:lang="zh-CN">在主线程选择应重新加载其就绪节点的模型的条件回调。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">The predicate is null.</para>
        ///     <para xml:lang="zh-CN">条件回调为 null。</para>
        /// </exception>
        public static void RequestPowersWhere(Predicate<PowerModel> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnqueueRule(PendingPowerRules, rule, RuntimeAssetRefreshScope.Powers);
        }

        /// <summary>
        ///     <para xml:lang="en">Requests orb-node visual updates for orbs matched by <paramref name="rule" />.</para>
        ///     <para xml:lang="zh-CN">请求更新 <paramref name="rule" /> 所匹配充能球的节点视觉效果。</para>
        /// </summary>
        /// <param name="rule">
        ///     <para xml:lang="en">A main-thread predicate selecting the models whose ready nodes should reload.</para>
        ///     <para xml:lang="zh-CN">在主线程选择应重新加载其就绪节点的模型的条件回调。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">The predicate is null.</para>
        ///     <para xml:lang="zh-CN">条件回调为 null。</para>
        /// </exception>
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
                cardRules = (_unfilteredScope & RuntimeAssetRefreshScope.Cards) != 0 ? [] : [.. PendingCardRules];
                relicRules = (_unfilteredScope & RuntimeAssetRefreshScope.Relics) != 0 ? [] : [.. PendingRelicRules];
                potionRules = (_unfilteredScope & RuntimeAssetRefreshScope.Potions) != 0 ? [] : [.. PendingPotionRules];
                powerRules = (_unfilteredScope & RuntimeAssetRefreshScope.Powers) != 0 ? [] : [.. PendingPowerRules];
                orbRules = (_unfilteredScope & RuntimeAssetRefreshScope.Orbs) != 0 ? [] : [.. PendingOrbRules];
                _unfilteredScope = RuntimeAssetRefreshScope.None;
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
                    RefreshNode(card, cardRules, static node => node.Model, ReloadCard);
                    continue;
                }

                if ((scope & RuntimeAssetRefreshScope.Relics) != 0 && node is NRelic relic)
                {
                    RefreshNode(relic, relicRules, static value => value.Model,
                        static value => value.Model = value.Model);
                    continue;
                }

                if ((scope & RuntimeAssetRefreshScope.Potions) != 0 && node is NPotion potion)
                {
                    RefreshNode(potion, potionRules, static value => value.Model,
                        static value => value.Model = value.Model);
                    continue;
                }

                if ((scope & RuntimeAssetRefreshScope.Powers) != 0 && node is NPower power)
                {
                    RefreshNode(power, powerRules, static value => value.Model,
                        static value => value.Model = value.Model);
                    continue;
                }

                if ((scope & RuntimeAssetRefreshScope.Orbs) != 0 && node is NOrb orb)
                    RefreshNode(orb, orbRules, static value => value.Model, static value => value.UpdateVisuals(false));
            }
        }

        private static void RefreshNode<TNode, TModel>(TNode node, IReadOnlyList<Predicate<TModel>> rules,
            Func<TNode, TModel?> getModel, Action<TNode> reload) where TNode : Node where TModel : class
        {
            if (!CanRefresh(node))
                return;
            try
            {
                var model = getModel(node);
                if (ShouldApply(model, rules) && CanRefresh(node) && ReferenceEquals(model, getModel(node)))
                    reload(node);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[Assets] Refresh failed for '{typeof(TNode).Name}': {ex.Message}");
            }
        }

        private static bool CanRefresh(Node node)
        {
            return GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion() &&
                   node.IsInsideTree() && node.IsNodeReady();
        }

        private static void ClearFilteredRules(RuntimeAssetRefreshScope scope)
        {
            if ((scope & RuntimeAssetRefreshScope.Cards) != 0)
                PendingCardRules.Clear();
            if ((scope & RuntimeAssetRefreshScope.Relics) != 0)
                PendingRelicRules.Clear();
            if ((scope & RuntimeAssetRefreshScope.Potions) != 0)
                PendingPotionRules.Clear();
            if ((scope & RuntimeAssetRefreshScope.Powers) != 0)
                PendingPowerRules.Clear();
            if ((scope & RuntimeAssetRefreshScope.Orbs) != 0)
                PendingOrbRules.Clear();
        }

        private static void EnqueueRule<TModel>(List<Predicate<TModel>> bucket, Predicate<TModel> rule,
            RuntimeAssetRefreshScope scope)
            where TModel : class
        {
            lock (SyncRoot)
            {
                if ((_unfilteredScope & scope) != 0)
                    return;
                if (!bucket.Contains(rule))
                    bucket.Add(rule);
                _pendingScope |= scope;
                if (_flushScheduled)
                    return;
                _flushScheduled = true;
            }

            Callable.From(ScheduleNextFrame).CallDeferred();
        }

        private static void ScheduleNextFrame()
        {
            if (Engine.GetMainLoop() is SceneTree tree)
                tree.Connect(SceneTree.SignalName.ProcessFrame, Callable.From(FlushPending),
                    (uint)GodotObject.ConnectFlags.OneShot);
            else
                FlushPending();
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
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
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

                for (var i = current.GetChildCount() - 1; i >= 0; i--)
                    if (current.GetChild(i) is { } child)
                        stack.Push(child);

                yield return current;
            }
        }
    }
}
