using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    internal sealed class MemberSynchronizedModSettingsValueBinding<TValue>(
        IModSettingsValueBinding<TValue> inner,
        Action<TValue> writeMember) : IModSettingsValueBinding<TValue>
    {
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
        }

        public void Save()
        {
            if (inner is ITransientModSettingsBinding)
                return;
            inner.Save();
        }
    }
}
