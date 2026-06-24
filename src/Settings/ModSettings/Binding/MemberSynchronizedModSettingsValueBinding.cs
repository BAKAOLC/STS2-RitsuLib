using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    internal sealed class MemberSynchronizedModSettingsValueBinding<TValue>(
        IModSettingsValueBinding<TValue> inner,
        Action<TValue> writeMember) : IModSettingsValueBinding<TValue>, IModSettingsUiRefreshEquivalence,
        IModSettingsBindingSaveDispatch
    {
        IReadOnlyList<IModSettingsBinding> IModSettingsBindingSaveDispatch.ImmediateSaveTargets => [inner];
        public IReadOnlyList<IModSettingsBinding> UiRefreshAlsoTreatAsDirty => [inner];

        public string ModId => inner.ModId;
        public string DataKey => inner.DataKey;
        public SaveScope Scope => inner.Scope;

        public TValue Read()
        {
            var value = inner.Read();
            writeMember(value);
            return value;
        }

        public void Write(TValue value)
        {
            writeMember(value);
            inner.Write(value);
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        public void Save()
        {
            if (inner is ITransientModSettingsBinding)
                return;
            inner.Save();
        }
    }
}
