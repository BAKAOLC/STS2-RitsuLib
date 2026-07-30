namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine
{
    /// <summary>
    ///     <para xml:lang="en">Represents one backend-agnostic animation state for an <see cref="IAnimationBackend" />.</para>
    ///     <para xml:lang="zh-CN">表示 <see cref="IAnimationBackend" /> 的一个与后端无关的动画状态。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         When the backend reports completion, <see cref="NextState" /> is entered if it is not <see langword="null" />.
    ///         <see cref="CallTrigger" /> chooses the first branch registered for the trigger whose optional predicate passes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         后端报告完成时，若 <see cref="NextState" /> 不为 <see langword="null" />，状态机会进入该状态。
    ///         <see cref="CallTrigger" /> 选择为触发器注册且可选谓词通过的第一个分支。
    ///     </para>
    /// </remarks>
    public sealed class ModAnimState
    {
        private readonly Dictionary<string, List<Branch>> _branches = new(StringComparer.Ordinal);

        /// <summary>
        ///     <para xml:lang="en">Creates a state bound to backend animation <paramref name="id" />.</para>
        ///     <para xml:lang="zh-CN">创建绑定到后端动画 <paramref name="id" /> 的状态。</para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">An animation ID accepted by <see cref="IAnimationBackend.HasAnimation" />.</para>
        ///     <para xml:lang="zh-CN">可由 <see cref="IAnimationBackend.HasAnimation" /> 接受的动画 ID。</para>
        /// </param>
        /// <param name="isLooping">
        ///     <para xml:lang="en">Whether the backend should be asked to loop playback.</para>
        ///     <para xml:lang="zh-CN">是否请求后端循环播放。</para>
        /// </param>
        public ModAnimState(string id, bool isLooping = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            Id = id;
            IsLooping = isLooping;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the backend animation ID, such as a Spine track, Godot animation name, cue key, or SpriteFrames animation name.</para>
        ///     <para xml:lang="zh-CN">获取后端动画 ID，例如 Spine 轨道、Godot 动画名、视觉提示键或 SpriteFrames 动画名。</para>
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether this state requests looping while active.</para>
        ///     <para xml:lang="zh-CN">获取此状态激活时是否请求循环播放。</para>
        /// </summary>
        public bool IsLooping { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the optional state entered after this state completes.</para>
        ///     <para xml:lang="zh-CN">获取或设置此状态完成后进入的可选状态。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">Leave this <see langword="null" /> for terminal states, so completion does not advance.</para>
        ///     <para xml:lang="zh-CN">终止状态应保持为 <see langword="null" />，使完成事件不再推进状态机。</para>
        /// </remarks>
        public ModAnimState? NextState { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets an optional bounds-container tag reported through <see cref="ModAnimStateMachine.BoundsUpdated" />.</para>
        ///     <para xml:lang="zh-CN">获取通过 <see cref="ModAnimStateMachine.BoundsUpdated" /> 报告的可选边界容器标签。</para>
        /// </summary>
        public string? BoundsContainer { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the state has completed at least one loop iteration.</para>
        ///     <para xml:lang="zh-CN">获取该状态是否已完成至少一次循环迭代。</para>
        /// </summary>
        public bool HasLooped { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Adds a branch to <paramref name="target" /> for <paramref name="trigger" />.</para>
        ///     <para xml:lang="zh-CN">为 <paramref name="trigger" /> 添加通向 <paramref name="target" /> 的分支。</para>
        /// </summary>
        /// <param name="trigger">
        ///     <para xml:lang="en">The trigger name compared ordinally by <see cref="CallTrigger" />.</para>
        ///     <para xml:lang="zh-CN">由 <see cref="CallTrigger" /> 按序数比较的触发器名称。</para>
        /// </param>
        /// <param name="target">
        ///     <para xml:lang="en">The state entered when the trigger fires and <paramref name="condition" /> passes.</para>
        ///     <para xml:lang="zh-CN">触发器触发且 <paramref name="condition" /> 通过时进入的状态。</para>
        /// </param>
        /// <param name="condition">
        ///     <para xml:lang="en">An optional predicate evaluated at trigger time; <see langword="null" /> always passes.</para>
        ///     <para xml:lang="zh-CN">在触发时求值的可选谓词；<see langword="null" /> 始终通过。</para>
        /// </param>
        public void AddBranch(string trigger, ModAnimState target, Func<bool>? condition = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(trigger);
            ArgumentNullException.ThrowIfNull(target);

            if (!_branches.TryGetValue(trigger, out var list))
            {
                list = [];
                _branches[trigger] = list;
            }

            list.Add(new(target, condition));
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the first branch for <paramref name="trigger" /> whose predicate passes, or <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="trigger" /> 的第一个谓词通过的分支；没有时返回 <see langword="null" />。</para>
        /// </summary>
        public ModAnimState? CallTrigger(string trigger)
        {
            return !_branches.TryGetValue(trigger, out var list)
                ? null
                : (from branch in list where branch.Condition == null || branch.Condition() select branch.Target)
                .FirstOrDefault();
        }

        /// <summary>
        ///     <para xml:lang="en">Returns whether at least one branch is registered for <paramref name="trigger" />.</para>
        ///     <para xml:lang="zh-CN">返回是否至少有一个分支注册到 <paramref name="trigger" />。</para>
        /// </summary>
        public bool HasTrigger(string trigger)
        {
            return _branches.ContainsKey(trigger);
        }

        /// <summary>
        ///     <para xml:lang="en">Marks the state as having completed one loop iteration.</para>
        ///     <para xml:lang="zh-CN">将该状态标记为已完成一次循环迭代。</para>
        /// </summary>
        public void MarkHasLooped()
        {
            HasLooped = true;
        }

        private readonly record struct Branch(ModAnimState Target, Func<bool>? Condition);
    }
}
