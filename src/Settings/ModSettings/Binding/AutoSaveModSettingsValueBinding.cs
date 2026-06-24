using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    internal sealed class AutoSaveModSettingsValueBinding<TValue>(IModSettingsValueBinding<TValue> inner)
        : IModSettingsValueBinding<TValue>, IModSettingsUiRefreshEquivalence, IModSettingsBindingSaveDispatch
    {
        IReadOnlyList<IModSettingsBinding> IModSettingsBindingSaveDispatch.ImmediateSaveTargets => [inner];

        public IReadOnlyList<IModSettingsBinding> UiRefreshAlsoTreatAsDirty { get; } =
            BuildUiRefreshEquivalenceChain(inner);

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
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
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

        private static IReadOnlyList<IModSettingsBinding> BuildUiRefreshEquivalenceChain(
            IModSettingsValueBinding<TValue> root)
        {
            var list = new List<IModSettingsBinding>(8);
            Append(root);
            return list;

            void Append(IModSettingsBinding node)
            {
                if (list.Contains(node))
                    return;
                list.Add(node);
                if (node is not IModSettingsUiRefreshEquivalence eq)
                    return;
                foreach (var alias in eq.UiRefreshAlsoTreatAsDirty)
                    Append(alias);
            }
        }
    }
}
