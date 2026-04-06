using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Keeps a per-run overlay on top of a persisted <see cref="IModSettingsValueBinding{TValue}" /> so values can stay
    ///     fixed for the run, commit or discard at run end, and optionally restrict writes on multiplayer clients.
    /// </summary>
    public sealed class ModSettingsRunScopedValueBinding<TValue> : IModSettingsValueBinding<TValue>,
        IModSettingsRunOverlayBinding, IRunScopedSettingsParticipant, IModSettingsSessionEditGate
    {
        private bool _hasOverlay;
        private TValue _overlay = default!;

        /// <summary>
        ///     Creates a run-scoped overlay over <paramref name="inner" /> and registers with
        ///     <see cref="ModSettingsRunSessionCoordinator" />.
        /// </summary>
        /// <param name="inner">Persisted (or callback) binding to shadow during runs.</param>
        /// <param name="commitMode">Whether overlay merges back into persistence when the run ends.</param>
        /// <param name="authority">Independent vs host-authoritative overlay writes.</param>
        public ModSettingsRunScopedValueBinding(
            IModSettingsValueBinding<TValue> inner,
            ModSettingsRunOverlayCommitMode commitMode,
            ModSettingsRunOverlayAuthority authority = ModSettingsRunOverlayAuthority.Independent)
        {
            Inner = inner;
            CommitMode = commitMode;
            Authority = authority;
            OverlaySlotKey = ModSettingsRunSessionCoordinator.MakeOverlaySlotKey(inner.ModId, inner.DataKey);
            ModSettingsRunSessionCoordinator.RegisterParticipant(this);
            if (authority == ModSettingsRunOverlayAuthority.HostAuthoritative)
                ModSettingsRunSessionCoordinator.RegisterHostApplier(new HostApplier(this));
        }

        /// <summary>
        ///     Inner binding (global/profile persistence).
        /// </summary>
        public IModSettingsValueBinding<TValue> Inner { get; }

        /// <summary>
        ///     Overlay commit behavior when the run ends.
        /// </summary>
        public ModSettingsRunOverlayCommitMode CommitMode { get; }

        /// <summary>
        ///     Multiplayer write policy for this overlay.
        /// </summary>
        public ModSettingsRunOverlayAuthority Authority { get; }

        /// <inheritdoc />
        public string OverlaySlotKey { get; }

        /// <inheritdoc />
        public bool IsReadOnlyInCurrentSession =>
            Authority == ModSettingsRunOverlayAuthority.HostAuthoritative &&
            ModSettingsSessionState.IsNetClient &&
            ModSettingsSessionState.IsInActiveRun;

        /// <inheritdoc />
        public string ModId => Inner.ModId;

        /// <inheritdoc />
        public string DataKey => Inner.DataKey;

        /// <inheritdoc />
        public SaveScope Scope => Inner.Scope;

        /// <inheritdoc />
        public TValue Read()
        {
            if (ModSettingsSessionState.IsInActiveRun && _hasOverlay)
                return CloneForOverlay(_overlay);

            return Inner.Read();
        }

        /// <inheritdoc />
        public void Write(TValue value)
        {
            if (!ModSettingsSessionState.IsInActiveRun)
            {
                Inner.Write(value);
                return;
            }

            if (Authority == ModSettingsRunOverlayAuthority.HostAuthoritative && ModSettingsSessionState.IsNetClient)
                return;

            EnsureOverlayInitializedFromInnerIfNeeded();
            _overlay = CloneForOverlay(value);
            _hasOverlay = true;
        }

        /// <inheritdoc />
        public void Save()
        {
            if (!ModSettingsSessionState.IsInActiveRun) Inner.Save();

            // During a run we keep mutations in the overlay; inner disk stays at pre-run snapshot until run end
            // (CommitToPersistenceOnRunEnd) unless the mod explicitly ends the run.
        }

        void IRunScopedSettingsParticipant.OnRunSnapshot()
        {
            _overlay = CloneForOverlay(Inner.Read());
            _hasOverlay = true;
        }

        void IRunScopedSettingsParticipant.OnRunEnded()
        {
            if (!_hasOverlay)
                return;

            try
            {
                if (CommitMode != ModSettingsRunOverlayCommitMode.CommitToPersistenceOnRunEnd) return;
                Inner.Write(CloneForOverlay(_overlay));
                Inner.Save();
            }
            finally
            {
                _hasOverlay = false;
                _overlay = default!;
            }
        }

        /// <summary>
        ///     Applies a value received from the host (multiplayer). No-op if not host-authoritative or not in a run.
        /// </summary>
        public void ApplyHostAuthoritativeOverlay(TValue value)
        {
            if (Authority != ModSettingsRunOverlayAuthority.HostAuthoritative)
                return;

            if (!ModSettingsSessionState.IsInActiveRun)
                return;

            _overlay = CloneForOverlay(value);
            _hasOverlay = true;
        }

        private void EnsureOverlayInitializedFromInnerIfNeeded()
        {
            if (_hasOverlay)
                return;

            _overlay = CloneForOverlay(Inner.Read());
            _hasOverlay = true;
        }

        private TValue CloneForOverlay(TValue value)
        {
            if (Inner is IStructuredModSettingsValueBinding<TValue> structured)
                return structured.Adapter.Clone(value);

            if (value is ICloneable cloneable)
                return (TValue)cloneable.Clone()!;

            return value;
        }

        private sealed class HostApplier(ModSettingsRunScopedValueBinding<TValue> target) : IRunOverlayHostApplier
        {
            public string SlotKey => target.OverlaySlotKey;

            public bool TryApply(object value)
            {
                if (value is not TValue typed) return false;
                target.ApplyHostAuthoritativeOverlay(typed);
                return true;
            }
        }
    }
}
