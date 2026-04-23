namespace STS2RitsuLib.Settings
{
    internal enum ModSettingsMirrorEntryKind
    {
        Header,
        Paragraph,
        Toggle,
        Slider,
        IntSlider,
        Choice,
        EnumChoice,
        Color,
        String,
        KeyBinding,
        Button,
        Subpage,
    }

    internal sealed record ModSettingsMirrorPageDefinition(
        string ModId,
        string PageId,
        int SortOrder,
        IReadOnlyList<ModSettingsMirrorSectionDefinition> Sections,
        ModSettingsText? Title = null,
        ModSettingsText? Description = null,
        ModSettingsText? ModDisplayName = null,
        int? ModSidebarOrder = null,
        string? ParentPageId = null,
        ModSettingsMirrorButtonDefinition? RestoreDefaultsButton = null);

    internal sealed record ModSettingsMirrorSectionDefinition(
        string Id,
        IReadOnlyList<ModSettingsMirrorEntryDefinition> Entries,
        ModSettingsText? Title = null,
        ModSettingsText? Description = null,
        bool IsCollapsible = false,
        bool StartCollapsed = false,
        Func<bool>? VisibleWhen = null);

    internal sealed record ModSettingsMirrorChoiceOption(
        string Value,
        ModSettingsText Label);

    internal sealed record ModSettingsMirrorNumericOptions(
        double Min,
        double Max,
        double Step,
        Func<double, string>? FormatDouble = null,
        Func<float, string>? FormatFloat = null);

    internal sealed record ModSettingsMirrorButtonDefinition(
        string Id,
        ModSettingsText RowLabel,
        ModSettingsText ButtonLabel,
        Action OnClick,
        ModSettingsButtonTone Tone,
        ModSettingsText? Description = null);

    internal sealed record ModSettingsMirrorEntryDefinition(
        string Id,
        ModSettingsMirrorEntryKind Kind,
        ModSettingsText Label,
        object? Binding = null,
        ModSettingsText? Description = null,
        ModSettingsMirrorNumericOptions? Numeric = null,
        IReadOnlyList<ModSettingsMirrorChoiceOption>? ChoiceOptions = null,
        ModSettingsChoicePresentation ChoicePresentation = ModSettingsChoicePresentation.Stepper,
        ModSettingsText? Placeholder = null,
        int? MaxLength = null,
        Func<string, bool>? ValidationVisual = null,
        bool AllowModifierCombos = true,
        bool AllowModifierOnly = true,
        bool DistinguishModifierSides = false,
        ModSettingsText? ButtonLabel = null,
        Action? OnClick = null,
        ModSettingsButtonTone ButtonTone = ModSettingsButtonTone.Normal,
        string? TargetPageId = null,
        float? MaxBodyHeight = null,
        bool EditAlpha = true,
        bool EditIntensity = false,
        Type? EnumType = null,
        Func<bool>? VisibleWhen = null);
}
