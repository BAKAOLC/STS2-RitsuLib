using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Wraps a value binding with <see cref="ModSettingsEditPolicy" /> for session-based UI locking.
    /// </summary>
    public sealed class ModSettingsPolicyGatedValueBinding<TValue>(
        IModSettingsValueBinding<TValue> inner,
        ModSettingsEditPolicy policy) : IModSettingsValueBinding<TValue>, IModSettingsSessionEditGate
    {
        /// <summary>
        ///     Wrapped binding.
        /// </summary>
        public IModSettingsValueBinding<TValue> Inner { get; } = inner;

        /// <summary>
        ///     Edit policy for the current session.
        /// </summary>
        public ModSettingsEditPolicy Policy { get; } = policy;

        /// <inheritdoc />
        public bool IsReadOnlyInCurrentSession
        {
            get
            {
                if (Inner is IModSettingsSessionEditGate { IsReadOnlyInCurrentSession: true })
                    return true;

                return Policy switch
                {
                    ModSettingsEditPolicy.OutOfRunOnly => ModSettingsSessionState.IsInActiveRun,
                    ModSettingsEditPolicy.InRunOnly => !ModSettingsSessionState.IsInActiveRun,
                    _ => false,
                };
            }
        }

        /// <inheritdoc />
        public string ModId => Inner.ModId;

        /// <inheritdoc />
        public string DataKey => Inner.DataKey;

        /// <inheritdoc />
        public SaveScope Scope => Inner.Scope;

        /// <inheritdoc />
        public TValue Read()
        {
            return Inner.Read();
        }

        /// <inheritdoc />
        public void Write(TValue value)
        {
            if (IsReadOnlyInCurrentSession)
                return;

            Inner.Write(value);
        }

        /// <inheritdoc />
        public void Save()
        {
            Inner.Save();
        }
    }
}
