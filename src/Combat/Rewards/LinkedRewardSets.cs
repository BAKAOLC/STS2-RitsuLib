using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;

namespace STS2RitsuLib.Combat.Rewards
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Creates and configures base-game <see cref="LinkedRewardSet" /> instances with deterministic selection,
    ///         multiplayer synchronization, and combat-room persistence supplied by RitsuLib.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         创建并配置原版 <see cref="LinkedRewardSet" /> 实例，由 RitsuLib 提供确定性的选择流程、多人同步及战斗房间持久化。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         The returned object remains the base-game reward type and uses the base-game linked-reward scene.
    ///         Linked sets cannot be nested. Every child must belong to the same player, must appear only once, and
    ///         must not already belong to another linked set. A displayed reward set may contain at most
    ///         <see cref="MaximumEncodedChildren" /> linked children in total so the choice fits the base-game
    ///         reward-selection message.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         返回对象仍是原版奖励类型，并使用原版关联奖励场景。关联集合不可嵌套；每个子奖励必须属于同一玩家、
    ///         只能出现一次，且不得已经属于其他关联集合。一个正在显示的奖励集合总计最多可包含
    ///         <see cref="MaximumEncodedChildren" /> 个关联子奖励，以确保选择信息可装入原版奖励选择消息。
    ///     </para>
    /// </remarks>
    public static class LinkedRewardSets
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Maximum number of linked child choices that can be synchronized within one displayed reward set.
        ///     </para>
        ///     <para xml:lang="zh-CN">一个正在显示的奖励集合内可同步的关联子奖励选项总数上限。</para>
        /// </summary>
        public const int MaximumEncodedChildren = 128;

        /// <summary>
        ///     <para xml:lang="en">Maximum serialized character count for one linked child and its extension data.</para>
        ///     <para xml:lang="zh-CN">单个关联子奖励及其扩展数据的序列化字符数上限。</para>
        /// </summary>
        public const int MaximumSerializedChildCharacters = 256 * 1024;

        /// <summary>
        ///     <para xml:lang="en">Maximum combined serialized character count for one linked reward set.</para>
        ///     <para xml:lang="zh-CN">单个关联奖励集合的序列化字符总数上限。</para>
        /// </summary>
        public const int MaximumSerializedSetCharacters = 4 * 1024 * 1024;

        /// <summary>
        ///     <para xml:lang="en">Creates a validated base-game linked reward set.</para>
        ///     <para xml:lang="zh-CN">创建一个经过验证的原版关联奖励集合。</para>
        /// </summary>
        /// <param name="rewards">
        ///     <para xml:lang="en">Child rewards in display order.</para>
        ///     <para xml:lang="zh-CN">按显示顺序排列的子奖励。</para>
        /// </param>
        /// <param name="player">
        ///     <para xml:lang="en">Player who owns the set and every child reward.</para>
        ///     <para xml:lang="zh-CN">拥有该集合及其全部子奖励的玩家。</para>
        /// </param>
        /// <param name="mode">
        ///     <para xml:lang="en">Selection behavior applied when a child reward is chosen.</para>
        ///     <para xml:lang="zh-CN">选择子奖励时应用的结算行为。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A configured base-game linked reward set.</para>
        ///     <para xml:lang="zh-CN">配置完成的原版关联奖励集合。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="rewards" />, <paramref name="player" />, or a child is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="rewards" />、<paramref name="player" /> 或某个子奖励为 null。</para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en"><paramref name="mode" /> is not defined.</para>
        ///     <para xml:lang="zh-CN"><paramref name="mode" /> 不是已定义的模式。</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">
        ///         The child collection is empty or too large, contains duplicates or nested sets, contains a reward
        ///         owned by another player, or contains a reward that already belongs to a linked set.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         子奖励集合为空或过大、包含重复项或嵌套集合、包含属于其他玩家的奖励，或某个奖励已经属于关联集合。
        ///     </para>
        /// </exception>
        public static LinkedRewardSet Create(
            IEnumerable<Reward> rewards,
            Player player,
            LinkedRewardSelectionMode mode = LinkedRewardSelectionMode.ChooseOne)
        {
            ArgumentNullException.ThrowIfNull(rewards);
            ArgumentNullException.ThrowIfNull(player);
            ValidateMode(mode);

            var children = rewards.ToList();
            ValidateChildren(children, player);

            var linkedRewardSet = new LinkedRewardSet(children, player);
            LinkedRewardSetRuntime.Configure(linkedRewardSet, mode);
            return linkedRewardSet;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Configures an existing base-game linked reward set. Unconfigured base-game sets use
        ///         <see cref="LinkedRewardSelectionMode.ChooseOne" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         配置现有的原版关联奖励集合。未配置的原版集合使用
        ///         <see cref="LinkedRewardSelectionMode.ChooseOne" />。
        ///     </para>
        /// </summary>
        /// <param name="linkedRewardSet">
        ///     <para xml:lang="en">The base-game linked reward set to configure.</para>
        ///     <para xml:lang="zh-CN">要配置的原版关联奖励集合。</para>
        /// </param>
        /// <param name="mode">
        ///     <para xml:lang="en">Selection behavior applied when a child reward is chosen.</para>
        ///     <para xml:lang="zh-CN">选择子奖励时应用的结算行为。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><paramref name="linkedRewardSet" /> for fluent use.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="linkedRewardSet" />，便于链式调用。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="linkedRewardSet" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="linkedRewardSet" /> 为 null。</para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en"><paramref name="mode" /> is not defined.</para>
        ///     <para xml:lang="zh-CN"><paramref name="mode" /> 不是已定义的模式。</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">
        ///         The instance is a derived linked reward type, or its existing child collection violates the same
        ///         constraints as <see cref="Create" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         该实例是派生的关联奖励类型，或其现有子奖励集合违反了与 <see cref="Create" /> 相同的约束。
        ///     </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">Selection has started or the set has already completed.</para>
        ///     <para xml:lang="zh-CN">该集合已经开始选择或已经结算完成。</para>
        /// </exception>
        public static LinkedRewardSet Configure(
            LinkedRewardSet linkedRewardSet,
            LinkedRewardSelectionMode mode)
        {
            ArgumentNullException.ThrowIfNull(linkedRewardSet);
            if (linkedRewardSet.GetType() != typeof(LinkedRewardSet))
                throw new ArgumentException(
                    "Derived linked reward types own their selection behavior and cannot be configured.",
                    nameof(linkedRewardSet));
            ValidateMode(mode);
            ValidateChildren(linkedRewardSet.Rewards, linkedRewardSet.Player, linkedRewardSet);
            LinkedRewardSetRuntime.Configure(linkedRewardSet, mode);
            return linkedRewardSet;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the configured selection mode. Unconfigured base-game sets return
        ///         <see cref="LinkedRewardSelectionMode.ChooseOne" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取已配置的选择模式。未配置的原版集合返回
        ///         <see cref="LinkedRewardSelectionMode.ChooseOne" />。
        ///     </para>
        /// </summary>
        /// <param name="linkedRewardSet">
        ///     <para xml:lang="en">The linked reward set to inspect.</para>
        ///     <para xml:lang="zh-CN">要查询的关联奖励集合。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The configured or default selection mode.</para>
        ///     <para xml:lang="zh-CN">已配置的选择模式或默认模式。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="linkedRewardSet" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="linkedRewardSet" /> 为 null。</para>
        /// </exception>
        public static LinkedRewardSelectionMode GetSelectionMode(LinkedRewardSet linkedRewardSet)
        {
            ArgumentNullException.ThrowIfNull(linkedRewardSet);
            return LinkedRewardSetRuntime.GetMode(linkedRewardSet);
        }

        private static void ValidateChildren(
            IReadOnlyCollection<Reward> rewards,
            Player player,
            LinkedRewardSet? existingSet = null)
        {
            if (rewards.Count is < 1 or > MaximumEncodedChildren)
                throw new ArgumentException(
                    $"Linked reward sets require between 1 and {MaximumEncodedChildren} children.",
                    nameof(rewards));

            var seen = new HashSet<Reward>(ReferenceEqualityComparer.Instance);
            foreach (var reward in rewards)
            {
                ArgumentNullException.ThrowIfNull(reward);
                if (!seen.Add(reward))
                    throw new ArgumentException("A linked reward child cannot appear more than once.", nameof(rewards));
                if (reward is LinkedRewardSet)
                    throw new ArgumentException("Linked reward sets cannot be nested.", nameof(rewards));
                if (!ReferenceEquals(reward.Player, player))
                    throw new ArgumentException(
                        "Every linked reward child must belong to the linked set's player.",
                        nameof(rewards));
                if (reward.ParentRewardSet != null && !ReferenceEquals(reward.ParentRewardSet, existingSet))
                    throw new ArgumentException(
                        "A reward that already belongs to a linked reward set cannot be reused.",
                        nameof(rewards));
            }
        }

        private static void ValidateMode(LinkedRewardSelectionMode mode)
        {
            if (!Enum.IsDefined(mode))
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown linked reward selection mode.");
        }
    }

    internal static class LinkedRewardSetRuntime
    {
        private static readonly ConditionalWeakTable<LinkedRewardSet, RuntimeState> States = [];

        internal static void Configure(LinkedRewardSet linkedRewardSet, LinkedRewardSelectionMode mode)
        {
            var state = States.GetValue(linkedRewardSet, static _ => new());
            lock (state)
            {
                if (state.IsResolving || state.IsCompleted)
                    throw new InvalidOperationException(
                        "A linked reward set cannot be reconfigured after selection has started.");

                state.Mode = mode;
            }
        }

        internal static LinkedRewardSelectionMode GetMode(LinkedRewardSet linkedRewardSet)
        {
            return States.TryGetValue(linkedRewardSet, out var state)
                ? state.Mode
                : LinkedRewardSelectionMode.ChooseOne;
        }

        internal static bool TryPrepareSelection(LinkedRewardSet linkedRewardSet, Reward selectedReward)
        {
            if (!linkedRewardSet.Rewards.Any(reward => ReferenceEquals(reward, selectedReward)))
                return false;

            var state = States.GetValue(linkedRewardSet, static _ => new());
            lock (state)
            {
                if (state.IsResolving || state.IsCompleted)
                    return false;

                state.PendingSelection = selectedReward;
                return true;
            }
        }

        internal static bool HasPendingSelection(LinkedRewardSet linkedRewardSet)
        {
            return States.TryGetValue(linkedRewardSet, out var state) && state.PendingSelection != null;
        }

        internal static async Task<bool> ResolveSelection(LinkedRewardSet linkedRewardSet)
        {
            var state = States.GetValue(linkedRewardSet, static _ => new());
            Reward selectedReward;
            LinkedRewardSelectionMode mode;
            lock (state)
            {
                selectedReward = state.PendingSelection
                                 ?? throw new InvalidOperationException(
                                     "A linked reward set was resolved without a pending child selection.");
                state.PendingSelection = null;
                state.IsResolving = true;
                mode = state.Mode;
            }

            var completed = false;
            try
            {
                completed = mode switch
                {
                    LinkedRewardSelectionMode.ChooseOne =>
                        await ResolveChooseOne(linkedRewardSet, selectedReward),
                    LinkedRewardSelectionMode.TakeAll =>
                        await ResolveTakeAll(linkedRewardSet, selectedReward),
                    _ => throw new ArgumentOutOfRangeException(nameof(mode), mode,
                        "Unknown linked reward selection mode."),
                };
                return completed;
            }
            finally
            {
                lock (state)
                {
                    state.IsResolving = false;
                    state.IsCompleted |= completed;
                }
            }
        }

        private static async Task<bool> ResolveChooseOne(
            LinkedRewardSet linkedRewardSet,
            Reward selectedReward)
        {
            if (!await SelectDetached(linkedRewardSet, selectedReward))
                return false;

            linkedRewardSet.RemoveReward(selectedReward);
            foreach (var reward in linkedRewardSet.Rewards)
                SkipAndRemove(linkedRewardSet, reward);
            return true;
        }

        private static async Task<bool> ResolveTakeAll(
            LinkedRewardSet linkedRewardSet,
            Reward selectedReward)
        {
            var ordered = linkedRewardSet.Rewards.ToList();
            ordered.Remove(selectedReward);
            ordered.Insert(0, selectedReward);

            var receivedAny = false;
            foreach (var reward in ordered)
            {
                if (reward.SuccessfullySelected || await SelectDetached(linkedRewardSet, reward))
                {
                    reward.ParentRewardSet = null;
                    linkedRewardSet.RemoveReward(reward);
                    receivedAny = true;
                    continue;
                }

                if (!receivedAny)
                    return false;

                SkipAndRemove(linkedRewardSet, reward);
            }

            return true;
        }

        private static async Task<bool> SelectDetached(LinkedRewardSet linkedRewardSet, Reward reward)
        {
            reward.ParentRewardSet = null;
            try
            {
                var selected = await reward.SelectUnsynchronized();
                if (!selected)
                    reward.ParentRewardSet = linkedRewardSet;
                return selected;
            }
            catch
            {
                reward.ParentRewardSet = linkedRewardSet;
                throw;
            }
        }

        private static void SkipAndRemove(LinkedRewardSet linkedRewardSet, Reward reward)
        {
            reward.ParentRewardSet = null;
            if (!reward.SuccessfullySelected)
                reward.OnSkipped();
            linkedRewardSet.RemoveReward(reward);
        }

        private sealed class RuntimeState
        {
            internal LinkedRewardSelectionMode Mode { get; set; } = LinkedRewardSelectionMode.ChooseOne;
            internal Reward? PendingSelection { get; set; }
            internal bool IsResolving { get; set; }
            internal bool IsCompleted { get; set; }
        }
    }
}
