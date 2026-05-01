using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    internal enum ModSettingsDebugShowcaseMode
    {
        Compact,
        Balanced,
        Detailed,
    }

    internal sealed record ModSettingsDebugShowcaseListDetail(string Label, string Value);

    internal sealed record ModSettingsDebugShowcaseListItem(
        string Name,
        int Weight,
        bool Enabled,
        string Tag,
        List<ModSettingsDebugShowcaseListDetail> Details);

    internal sealed class ModSettingsDebugShowcaseState
    {
        private int _nextItemIndex = 4;

        public bool ToggleValue { get; set; } = true;
        public double SliderValue { get; set; } = 35d;
        public int IntSliderValue { get; set; } = 2;
        public string ChoiceValue { get; set; } = "balanced";
        public string ChoiceDropdownValue { get; set; } = "wide";
        public ModSettingsDebugShowcaseMode ModeValue { get; set; } = ModSettingsDebugShowcaseMode.Balanced;
        public string StringValue { get; set; } = "Single line";
        public string StringMultiValue { get; set; } = "First line\nSecond line";
        public int ActionCount { get; set; }
        public int ToastCount { get; set; }

        public List<ModSettingsDebugShowcaseListItem> ListItems { get; set; } =
        [
            new("Sample A", 3, true, "alpha", [new("Author", "Ritsu"), new("Mode", "Default")]),
            new("Sample B", 1, false, "beta", [new("Author", "Debug")]),
            new("Sample C", 5, true, "gamma", [new("Mode", "Experimental"), new("Tier", "Rare")]),
        ];

        public ModSettingsDebugShowcaseListItem CreateListItem()
        {
            var index = _nextItemIndex++;
            return new($"Sample {index}", index, index % 2 == 0,
                $"tag-{index}", [new("Author", $"User {index}")]);
        }
    }

    internal sealed class ModSettingsDebugShowcaseBinding<TValue>(
        IModSettingsValueBinding<TValue> inner,
        Action<TValue> afterWrite)
        : IModSettingsValueBinding<TValue>, ITransientModSettingsBinding, IModSettingsUiRefreshEquivalence,
            IModSettingsUiRefreshPropagation
    {
        public IReadOnlyList<IModSettingsBinding> UiRefreshAlsoTreatAsDirty => [inner];
        public IEnumerable<IModSettingsBinding> ExtraBindingsToMarkDirtyForUi => [inner];
        public string ModId => inner.ModId;
        public string DataKey => inner.DataKey;
        public SaveScope Scope => inner.Scope;

        public TValue Read()
        {
            return inner.Read();
        }

        public void Write(TValue value)
        {
            inner.Write(value);
            afterWrite(value);
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        public void Save()
        {
        }
    }
}
