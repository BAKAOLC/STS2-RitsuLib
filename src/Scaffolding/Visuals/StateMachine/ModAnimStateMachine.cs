namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine
{
    /// <summary>
    ///     <para xml:lang="en">Drives <see cref="ModAnimState" /> transitions through any <see cref="IAnimationBackend" />.</para>
    ///     <para xml:lang="zh-CN">通过任意 <see cref="IAnimationBackend" /> 驱动 <see cref="ModAnimState" /> 转换。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Triggers prefer any-state branches to current-state branches. A terminal state leaves
    ///         <see cref="ModAnimState.NextState" /> <see langword="null" />, so completion does not advance.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         触发器优先匹配任意状态分支，再匹配当前状态分支。终止状态的 <see cref="ModAnimState.NextState" /> 保持为
    ///         <see langword="null" />，因此完成时不会推进。
    ///     </para>
    /// </remarks>
    public sealed class ModAnimStateMachine
    {
        private readonly ModAnimState _anyState = new("__anyState");
        private bool _disposed;
        private bool _nextStateQueued;
        private ModAnimState? _queuedFromState;

        /// <summary>
        ///     <para xml:lang="en">Wraps <paramref name="backend" /> and subscribes to its playback events.</para>
        ///     <para xml:lang="zh-CN">包装 <paramref name="backend" /> 并订阅其播放事件。</para>
        /// </summary>
        public ModAnimStateMachine(IAnimationBackend backend)
        {
            ArgumentNullException.ThrowIfNull(backend);
            Backend = backend;
            Backend.Started += OnBackendStarted;
            Backend.Completed += OnBackendCompleted;
            Backend.Interrupted += OnBackendInterrupted;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the active state, or <see langword="null" /> before <see cref="Start" /> and after
        ///         <see cref="Dispose" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取活动状态；在 <see cref="Start" /> 前和 <see cref="Dispose" /> 后为 <see langword="null" />。</para>
        /// </summary>
        public ModAnimState? Current { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Gets the backend driven by this state machine.</para>
        ///     <para xml:lang="zh-CN">获取由此状态机驱动的后端。</para>
        /// </summary>
        public IAnimationBackend Backend { get; }

        /// <summary>
        ///     <para xml:lang="en">Occurs when the current state's bounds-container tag should be applied.</para>
        ///     <para xml:lang="zh-CN">当应应用当前状态的边界容器标签时发生。</para>
        /// </summary>
        public event Action<string>? BoundsUpdated;

        /// <summary>
        ///     <para xml:lang="en">Occurs when the backend reports playback started for the current state.</para>
        ///     <para xml:lang="zh-CN">当后端报告当前状态开始播放时发生。</para>
        /// </summary>
        public event Action<ModAnimState>? AnimationStarted;

        /// <summary>
        ///     <para xml:lang="en">Occurs when the backend reports playback completed for the current state.</para>
        ///     <para xml:lang="zh-CN">当后端报告当前状态播放完成时发生。</para>
        /// </summary>
        public event Action<ModAnimState>? AnimationCompleted;

        /// <summary>
        ///     <para xml:lang="en">Occurs when the backend reports playback interrupted for the current state.</para>
        ///     <para xml:lang="zh-CN">当后端报告当前状态播放中断时发生。</para>
        /// </summary>
        public event Action<ModAnimState>? AnimationInterrupted;

        /// <summary>
        ///     <para xml:lang="en">Registers a branch on the synthetic any-state, which is evaluated before the current state.</para>
        ///     <para xml:lang="zh-CN">在合成的任意状态上注册分支；该状态会先于当前状态求值。</para>
        /// </summary>
        public void AddAnyState(string trigger, ModAnimState state, Func<bool>? condition = null)
        {
            _anyState.AddBranch(trigger, state, condition);
        }

        /// <summary>
        ///     <para xml:lang="en">Enters <paramref name="initial" /> and starts backend playback unless the machine is disposed.</para>
        ///     <para xml:lang="zh-CN">进入 <paramref name="initial" /> 并启动后端播放，除非状态机已释放。</para>
        /// </summary>
        public void Start(ModAnimState initial)
        {
            ArgumentNullException.ThrowIfNull(initial);
            if (_disposed)
                return;

            EnterState(initial);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns whether the synthetic any-state has a branch for <paramref name="trigger" />.</para>
        ///     <para xml:lang="zh-CN">返回合成的任意状态是否有 <paramref name="trigger" /> 的分支。</para>
        /// </summary>
        public bool HasTrigger(string trigger)
        {
            return _anyState.HasTrigger(trigger);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get the current state's total duration when the backend provides timing.</para>
        ///     <para xml:lang="zh-CN">当后端提供计时时，尝试获取当前状态的总时长。</para>
        /// </summary>
        public bool TryGetCurrentAnimationDuration(out float seconds)
        {
            seconds = 0f;
            return Current is { } state &&
                   Backend is IAnimationTimingProvider timing &&
                   timing.TryGetAnimationDuration(state.Id, out seconds);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get the current animation's remaining duration when the backend provides timing.</para>
        ///     <para xml:lang="zh-CN">当后端提供计时时，尝试获取当前动画的剩余时长。</para>
        /// </summary>
        public bool TryGetCurrentAnimationRemaining(out float seconds)
        {
            seconds = 0f;
            return Backend is IAnimationTimingProvider timing &&
                   timing.TryGetCurrentAnimationRemaining(out seconds);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Evaluates <paramref name="trigger" /> against any-state, then the current state, and enters the
        ///         first matching target.
        ///     </para>
        ///     <para xml:lang="zh-CN">先对任意状态、再对当前状态求值 <paramref name="trigger" />，并进入第一个匹配的目标。</para>
        /// </summary>
        public void SetTrigger(string trigger)
        {
            if (_disposed || string.IsNullOrWhiteSpace(trigger))
                return;

            var target = _anyState.CallTrigger(trigger) ?? Current?.CallTrigger(trigger);
            if (target == null)
                return;

            EnterState(target);
        }

        /// <summary>
        ///     <para xml:lang="en">Detaches from backend events. Repeated calls are safe.</para>
        ///     <para xml:lang="zh-CN">断开后端事件；可安全地重复调用。</para>
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Backend.Started -= OnBackendStarted;
            Backend.Completed -= OnBackendCompleted;
            Backend.Interrupted -= OnBackendInterrupted;
            Current = null;
        }

        private void EnterState(ModAnimState state)
        {
            if (!Backend.HasAnimation(state.Id))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModAnimStateMachine] Backend has no animation '{state.Id}' (owner={Backend.OwnerNode?.Name})");
                return;
            }

            Current = state;
            _nextStateQueued = false;
            _queuedFromState = null;
            Backend.Play(state.Id, state.IsLooping);
            if (Current != state)
                return;

            if (state.BoundsContainer != null)
                BoundsUpdated?.Invoke(state.BoundsContainer);

            QueueNextState(state);
        }

        private void QueueNextState(ModAnimState state)
        {
            if (ReferenceEquals(_queuedFromState, state))
                return;

            _queuedFromState = state;
            if (state.NextState is not { } next)
                return;

            if (!Backend.HasAnimation(next.Id))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModAnimStateMachine] Backend has no queued animation '{next.Id}' " +
                    $"(owner={Backend.OwnerNode?.Name})");
                return;
            }

            Backend.Queue(next.Id, next.IsLooping);
            _nextStateQueued = true;
        }

        private void OnBackendStarted(string _)
        {
            if (Current is not { } state)
                return;

            if (state is { HasLooped: false, BoundsContainer: not null })
                BoundsUpdated?.Invoke(state.BoundsContainer);

            AnimationStarted?.Invoke(state);

            if (Current == state)
                QueueNextState(state);
        }

        private void OnBackendCompleted(string _)
        {
            if (Current is not { } state)
                return;

            if (state is { HasLooped: false, BoundsContainer: not null })
                BoundsUpdated?.Invoke(state.BoundsContainer);

            if (state is { IsLooping: true, HasLooped: false })
                state.MarkHasLooped();

            AnimationCompleted?.Invoke(state);

            if (Current != state)
                return;

            if (state.NextState == null ||
                !ReferenceEquals(_queuedFromState, state) ||
                !_nextStateQueued)
                return;

            Current = state.NextState;
            _nextStateQueued = false;
            _queuedFromState = null;
        }

        private void OnBackendInterrupted(string _)
        {
            if (Current is not { } state)
                return;

            if (state.BoundsContainer != null)
                BoundsUpdated?.Invoke(state.BoundsContainer);

            AnimationInterrupted?.Invoke(state);
        }
    }
}
