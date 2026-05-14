namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine
{
    /// <summary>
    ///     Backend-agnostic animation state machine. Semantically aligned with
    ///     <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator" /> (<see cref="SetTrigger" /> evaluates
    ///     any-state branches first, then current-state branches; <see cref="ModAnimState.NextState" /> is queued
    ///     on entry and consumed on completion) but usable against any <see cref="IAnimationBackend" />.
    ///     与后端无关的动画状态机。语义上与
    ///     <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator" /> (<see cref="SetTrigger" /> 评估
    ///     先评估 any-状态 分支，再评估当前状态分支； <see cref="ModAnimState.NextState" /> 会入队
    ///     在进入时入队并在完成时消费），但可用于任何 <see cref="IAnimationBackend" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Terminal states (such as <c>die</c>) are represented by leaving <see cref="ModAnimState.NextState" />
    ///         as <see langword="null" />; on completion the machine stays on that state without advancing.
    ///     </para>
    ///     <para>
    ///         终止状态（如 <c>die</c>）通过将 <see cref="ModAnimState.NextState" /> 留为
    ///         <see langword="null" /> 表示；完成后机器会停留在该状态，不再推进。
    ///     </para>
    /// </remarks>
    public sealed class ModAnimStateMachine
    {
        private readonly ModAnimState _anyState = new("__anyState");
        private bool _disposed;

        /// <summary>
        ///     Wraps <paramref name="backend" />; subscribes to its event surface.
        ///     包装 <paramref name="backend" />，并订阅它的事件接口。
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
        ///     Currently active state, or <see langword="null" /> before <see cref="Start" /> or after <see cref="Dispose" />.
        ///     当前激活状态；在 <see cref="Start" /> 之前或 <see cref="Dispose" /> 之后为 <see langword="null" />。
        /// </summary>
        public ModAnimState? Current { get; private set; }

        /// <summary>
        ///     Underlying backend; exposed primarily for composite scenarios (e.g. merchant dual playback).
        ///     底层后端；主要为组合场景暴露（例如商人双重播放）。
        /// </summary>
        public IAnimationBackend Backend { get; }

        /// <summary>
        ///     Raised when <see cref="ModAnimState.BoundsContainer" /> should update (enter, completion, interruption).
        ///     当 <see cref="ModAnimState.BoundsContainer" /> 应更新时触发（进入、完成、中断）。
        /// </summary>
        public event Action<string>? BoundsUpdated;

        /// <summary>
        ///     Raised when the backend reports start for the current state's animation id.
        ///     当后端报告当前状态的动画 id 开始播放时触发。
        /// </summary>
        public event Action<ModAnimState>? AnimationStarted;

        /// <summary>
        ///     Raised when the backend reports completion for the current state's animation id.
        ///     当后端报告当前状态的动画 id 完成播放时触发。
        /// </summary>
        public event Action<ModAnimState>? AnimationCompleted;

        /// <summary>
        ///     Raised when the backend reports interruption for the current state's animation id.
        ///     当后端报告当前状态的动画 id 播放中断时触发。
        /// </summary>
        public event Action<ModAnimState>? AnimationInterrupted;

        /// <summary>
        ///     Registers a branch on the synthetic any-state, matching
        ///     <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator.AddAnyState" />.
        ///     在合成的任意状态上注册分支，对应
        /// </summary>
        public void AddAnyState(string trigger, ModAnimState state, Func<bool>? condition = null)
        {
            _anyState.AddBranch(trigger, state, condition);
        }

        /// <summary>
        ///     Enters <paramref name="initial" />; triggers backend playback and fires <see cref="BoundsUpdated" />.
        ///     进入 <paramref name="initial" />；触发后端播放并触发 <see cref="BoundsUpdated" />。
        /// </summary>
        public void Start(ModAnimState initial)
        {
            ArgumentNullException.ThrowIfNull(initial);
            if (_disposed)
                return;

            EnterState(initial);
        }

        /// <summary>
        ///     <see langword="true" /> when any-state has a branch for <paramref name="trigger" />.
        ///     当 any-state 拥有 <paramref name="trigger" /> 的分支时返回 <see langword="true" />。
        /// </summary>
        public bool HasTrigger(string trigger)
        {
            return _anyState.HasTrigger(trigger);
        }

        /// <summary>
        ///     Resolves <paramref name="trigger" /> against any-state first, then the current state; when matched,
        ///     transitions to the resolved target.
        ///     先用 <paramref name="trigger" /> 匹配任意状态，再匹配当前状态；匹配成功时，
        ///     转换到解析出的目标。
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
        ///     Detaches from backend events. Safe to call multiple times.
        ///     解绑后端事件。可安全多次调用。
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
            Backend.Play(state.Id, state.IsLooping);

            if (state.BoundsContainer != null)
                BoundsUpdated?.Invoke(state.BoundsContainer);

            if (state.NextState != null)
                QueueChain(state.NextState);
        }

        private void QueueChain(ModAnimState state)
        {
            while (true)
            {
                if (!Backend.HasAnimation(state.Id)) return;

                Backend.Queue(state.Id, state.IsLooping);

                if (state.NextState != null)
                {
                    state = state.NextState;
                    continue;
                }

                break;
            }
        }

        private void OnBackendStarted(string _)
        {
            if (Current is not { } state)
                return;

            if (state is { HasLooped: false, BoundsContainer: not null })
                BoundsUpdated?.Invoke(state.BoundsContainer);

            AnimationStarted?.Invoke(state);
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

            if (state.NextState != null)
                Current = state.NextState;
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
