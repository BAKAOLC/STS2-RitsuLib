namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine
{
    /// <summary>
    ///     Backend-agnostic animation state, semantically equivalent to
    ///     <see cref="MegaCrit.Sts2.Core.Animation.AnimState" /> but usable from any
    ///     <see cref="IAnimationBackend" /> (Spine, Godot animation player, animated sprite, cue frame sequences).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Transitions follow the vanilla pattern:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <see cref="NextState" /> is consumed only when the current animation completes (non-looping) or
    ///                 when the backend signals completion; if <see langword="null" />, the state is preserved.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="CallTrigger" /> resolves branches added via <see cref="AddBranch" />; branches may
    ///                 declare an optional guard <see cref="System.Func{TResult}" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public sealed class ModAnimState
    {
        private readonly Dictionary<string, List<Branch>> _branches = new(StringComparer.Ordinal);

        /// <summary>
        ///     Creates a new state bound to backend animation <paramref name="id" />.
        /// </summary>
        /// <param name="id">Animation id resolved by <see cref="IAnimationBackend.HasAnimation" />.</param>
        /// <param name="isLooping">When <see langword="true" />, the backend is asked to loop playback.</param>
        public ModAnimState(string id, bool isLooping = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            Id = id;
            IsLooping = isLooping;
        }

        /// <summary>
        ///     Backend animation id (Spine track, Godot animation name, cue key, or sprite-frames animation name).
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Whether the state loops while active.
        /// </summary>
        public bool IsLooping { get; }

        /// <summary>
        ///     Optional follow-up state used by <see cref="ModAnimStateMachine" /> when this state completes.
        /// </summary>
        /// <remarks>
        ///     Keep <see langword="null" /> for terminal states (e.g. <c>die</c>) so completion does not advance.
        /// </remarks>
        public ModAnimState? NextState { get; set; }

        /// <summary>
        ///     Optional bounds-container tag forwarded through
        ///     <see cref="ModAnimStateMachine.BoundsUpdated" /> on start and completion.
        /// </summary>
        public string? BoundsContainer { get; init; }

        /// <summary>
        ///     <see langword="true" /> once a looping state has completed at least one full cycle.
        /// </summary>
        public bool HasLooped { get; private set; }

        /// <summary>
        ///     Adds a conditional branch to <paramref name="target" /> for trigger <paramref name="trigger" />.
        /// </summary>
        /// <param name="trigger">Trigger name compared verbatim during <see cref="CallTrigger" />.</param>
        /// <param name="target">State to transition to when the trigger fires and <paramref name="condition" /> passes.</param>
        /// <param name="condition">Optional guard evaluated at trigger time; <see langword="null" /> means always.</param>
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
        ///     Resolves the first matching branch for <paramref name="trigger" /> whose guard passes,
        ///     or <see langword="null" /> when no branch is eligible.
        /// </summary>
        public ModAnimState? CallTrigger(string trigger)
        {
            return !_branches.TryGetValue(trigger, out var list)
                ? null
                : (from branch in list where branch.Condition == null || branch.Condition() select branch.Target)
                .FirstOrDefault();
        }

        /// <summary>
        ///     <see langword="true" /> when at least one branch is registered for <paramref name="trigger" />.
        /// </summary>
        public bool HasTrigger(string trigger)
        {
            return _branches.ContainsKey(trigger);
        }

        /// <summary>
        ///     Marks the state as having completed one loop iteration (for bounds / debug logic).
        /// </summary>
        public void MarkHasLooped()
        {
            HasLooped = true;
        }

        private readonly record struct Branch(ModAnimState Target, Func<bool>? Condition);
    }
}
