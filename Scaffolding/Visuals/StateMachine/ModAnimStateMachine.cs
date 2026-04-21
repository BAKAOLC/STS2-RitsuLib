namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine
{
    /// <summary>
    ///     Backend-agnostic animation state machine. Semantically aligned with
    ///     <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator" /> (<see cref="SetTrigger" /> evaluates
    ///     any-state branches first, then current-state branches; <see cref="ModAnimState.NextState" /> is queued
    ///     on entry and consumed on completion) but usable against any <see cref="IAnimationBackend" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Terminal states (such as <c>die</c>) are represented by leaving <see cref="ModAnimState.NextState" />
    ///         as <see langword="null" />; on completion the machine stays on that state without advancing.
    ///     </para>
    /// </remarks>
    public sealed class ModAnimStateMachine
    {
        private readonly ModAnimState _anyState = new("__anyState");
        private bool _disposed;

        /// <summary>
        ///     Wraps <paramref name="backend" />; subscribes to its event surface.
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
        /// </summary>
        public ModAnimState? Current { get; private set; }

        /// <summary>
        ///     Underlying backend; exposed primarily for composite scenarios (e.g. merchant dual playback).
        /// </summary>
        public IAnimationBackend Backend { get; }

        /// <summary>
        ///     Raised when <see cref="ModAnimState.BoundsContainer" /> should update (enter, completion, interruption).
        /// </summary>
        public event Action<string>? BoundsUpdated;

        /// <summary>
        ///     Raised when the backend reports start for the current state's animation id.
        /// </summary>
        public event Action<ModAnimState>? AnimationStarted;

        /// <summary>
        ///     Raised when the backend reports completion for the current state's animation id.
        /// </summary>
        public event Action<ModAnimState>? AnimationCompleted;

        /// <summary>
        ///     Raised when the backend reports interruption for the current state's animation id.
        /// </summary>
        public event Action<ModAnimState>? AnimationInterrupted;

        /// <summary>
        ///     Registers a branch on the synthetic any-state, matching
        ///     <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator.AddAnyState" />.
        /// </summary>
        public void AddAnyState(string trigger, ModAnimState state, Func<bool>? condition = null)
        {
            _anyState.AddBranch(trigger, state, condition);
        }

        /// <summary>
        ///     Enters <paramref name="initial" />; triggers backend playback and fires <see cref="BoundsUpdated" />.
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
        /// </summary>
        public bool HasTrigger(string trigger)
        {
            return _anyState.HasTrigger(trigger);
        }

        /// <summary>
        ///     Resolves <paramref name="trigger" /> against any-state first, then the current state; when matched,
        ///     transitions to the resolved target.
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
