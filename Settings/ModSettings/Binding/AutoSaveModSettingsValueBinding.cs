using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    internal sealed class AutoSaveModSettingsValueBinding<TValue>(IModSettingsValueBinding<TValue> inner)
        : IModSettingsValueBinding<TValue>
    {
        public SaveScope Scope => inner.Scope;
        public string ModId => inner.ModId;
        public string DataKey => inner.DataKey;

        public TValue Read()
        {
            return inner.Read();
        }

        public void Write(TValue value)
        {
            inner.Write(value);
            if (inner is ITransientModSettingsBinding)
                return;
            inner.Save();
        }

        public void Save()
        {
            if (inner is ITransientModSettingsBinding)
                return;
            inner.Save();
        }
    }
}
