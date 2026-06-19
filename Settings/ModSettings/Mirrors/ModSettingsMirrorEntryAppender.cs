using System.Reflection;

namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsMirrorEntryAppender
    {
        public static void Append(ModSettingsSectionBuilder section, ModSettingsMirrorEntryDefinition entry)
        {
            switch (entry.Kind)
            {
                case ModSettingsMirrorEntryKind.Header:
                    section.AddHeader(entry.Id, entry.Label, InlineDescription(entry));
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.Paragraph:
                    section.AddParagraph(entry.Id, entry.Label, InlineDescription(entry), entry.MaxBodyHeight);
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.Toggle:
                    section.AddToggle(entry.Id, entry.Label, (IModSettingsValueBinding<bool>)entry.Binding!,
                        InlineDescription(entry));
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.Slider:
                    switch (entry.Binding)
                    {
                        case IModSettingsValueBinding<double> doubleBinding:
                        {
                            var numeric = entry.Numeric!;
                            section.AddSlider(entry.Id, entry.Label, doubleBinding, numeric.Min, numeric.Max,
                                numeric.Step,
                                numeric.FormatDouble, InlineDescription(entry));
                            ApplyEntryOptions(section, entry);
                            return;
                        }
                        case IModSettingsValueBinding<float> floatBinding:
                        {
                            var numeric = entry.Numeric!;
#pragma warning disable CS0618
                            section.AddSlider(entry.Id, entry.Label, floatBinding, (float)numeric.Min,
                                (float)numeric.Max,
                                (float)numeric.Step, numeric.FormatFloat, InlineDescription(entry));
#pragma warning restore CS0618
                            ApplyEntryOptions(section, entry);
                            return;
                        }
                    }

                    break;
                case ModSettingsMirrorEntryKind.IntSlider:
                {
                    var numeric = entry.Numeric!;
                    section.AddIntSlider(entry.Id, entry.Label, (IModSettingsValueBinding<int>)entry.Binding!,
                        (int)Math.Round(numeric.Min),
                        (int)Math.Round(numeric.Max),
                        Math.Max(1, (int)Math.Round(numeric.Step)),
                        null,
                        InlineDescription(entry));
                    ApplyEntryOptions(section, entry);
                    return;
                }
                case ModSettingsMirrorEntryKind.Choice:
                    section.AddChoice(entry.Id, entry.Label, (IModSettingsValueBinding<string>)entry.Binding!,
                        entry.ChoiceOptions!
                            .Select(option => new ModSettingsChoiceOption<string>(option.Value, option.Label))
                            .ToArray(),
                        InlineDescription(entry), entry.ChoicePresentation);
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.EnumChoice:
                    AppendEnumChoice(section, entry);
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.Color:
                    section.AddColor(entry.Id, entry.Label, (IModSettingsValueBinding<string>)entry.Binding!,
                        InlineDescription(entry),
                        entry.EditAlpha, entry.EditIntensity);
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.String:
                    section.AddString(entry.Id, entry.Label, (IModSettingsValueBinding<string>)entry.Binding!,
                        entry.Placeholder,
                        entry.MaxLength, InlineDescription(entry), entry.ValidationVisual, entry.ValidationCommit);
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.MultilineString:
                    section.AddMultilineString(entry.Id, entry.Label, (IModSettingsValueBinding<string>)entry.Binding!,
                        entry.Placeholder, entry.MaxLength, InlineDescription(entry));
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.KeyBinding:
                    section.AddKeyBinding(entry.Id, entry.Label, (IModSettingsValueBinding<string>)entry.Binding!,
                        entry.AllowModifierCombos, entry.AllowModifierOnly, entry.DistinguishModifierSides,
                        InlineDescription(entry));
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.InputBinding:
                    section.AddInputBinding(entry.Id, entry.Label, (IModSettingsValueBinding<string>)entry.Binding!,
                        entry.AllowModifierCombos, entry.AllowModifierOnly, entry.DistinguishModifierSides,
                        entry.AllowActionBindings, InlineDescription(entry));
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.MultiKeyBinding:
                    section.AddKeyBinding(entry.Id, entry.Label, (IModSettingsValueBinding<List<string>>)entry.Binding!,
                        true, entry.AllowModifierCombos, entry.AllowModifierOnly, entry.DistinguishModifierSides,
                        InlineDescription(entry));
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.InfoCard:
                    section.AddInfoCard(entry.Id, entry.Label, entry.Body ?? ModSettingsText.Literal(string.Empty),
                        InlineDescription(entry));
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.RuntimeHotkeySummary:
                    section.AddRuntimeHotkeySummary(entry.Id, entry.Label,
                        entry.Body ?? ModSettingsText.Literal(string.Empty),
                        entry.HotkeyBindings ?? [ModSettingsText.Literal(string.Empty)],
                        entry.HotkeyIdSuffix);
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.Image:
                    section.AddImage(entry.Id, entry.Label, entry.TextureProvider!, entry.PreviewHeight,
                        InlineDescription(entry));
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.Button:
                    section.AddButton(entry.Id, entry.Label, entry.ButtonLabel ?? entry.Label, entry.OnClick!,
                        entry.ButtonTone,
                        InlineDescription(entry));
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.HostContextButton:
                    section.AddButton(entry.Id, entry.Label, entry.ButtonLabel ?? entry.Label,
                        entry.HostContextOnClick!, entry.ButtonTone,
                        InlineDescription(entry));
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.Subpage:
                    section.AddSubpage(entry.Id, entry.Label, entry.TargetPageId!, entry.ButtonLabel,
                        InlineDescription(entry));
                    ApplyEntryOptions(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.Custom:
                    section.AddCustom(entry.Id, entry.Label, entry.CustomControlFactory!, InlineDescription(entry));
                    ApplyEntryOptions(section, entry);
                    return;
            }

            throw new InvalidOperationException($"Unsupported mirror entry kind '{entry.Kind}'.");
        }

        public static void AppendButton(ModSettingsSectionBuilder section, ModSettingsMirrorButtonDefinition button)
        {
            section.AddButton(button.Id, button.RowLabel, button.ButtonLabel, button.OnClick, button.Tone,
                button.Description);
        }

        private static void AppendEnumChoice(ModSettingsSectionBuilder section, ModSettingsMirrorEntryDefinition entry)
        {
            if (entry.EnumType == null)
                throw new InvalidOperationException($"EnumType is required for enum entry '{entry.Id}'.");

            typeof(ModSettingsMirrorEntryAppender)
                .GetMethod(nameof(AppendEnumChoiceGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entry.EnumType)
                .Invoke(null, [section, entry]);
        }

        private static void AppendEnumChoiceGeneric<TEnum>(ModSettingsSectionBuilder section,
            ModSettingsMirrorEntryDefinition entry)
            where TEnum : struct, Enum
        {
            Func<TEnum, ModSettingsText>? labelFactory = entry.EnumOptionLabel == null
                ? null
                : value => entry.EnumOptionLabel(value);
            section.AddEnumChoice(entry.Id, entry.Label, (IModSettingsValueBinding<TEnum>)entry.Binding!, labelFactory,
                InlineDescription(entry),
                entry.ChoicePresentation);
        }

        private static void ApplyEntryOptions(ModSettingsSectionBuilder section, ModSettingsMirrorEntryDefinition entry)
        {
            if (entry.VisibleWhen != null)
                section.WithEntryVisibleWhen(entry.Id, entry.VisibleWhen);
            if (entry.ReadOnlyOnHostSurfaces != ModSettingsHostSurface.None)
                section.WithEntryReadOnlyOnHostSurfaces(entry.Id, entry.ReadOnlyOnHostSurfaces);
            if (HoverTipDescription(entry) is { } hoverTipDescription)
                section.WithEntryHoverTip(entry.Id, entry.HoverTipTitle ?? entry.Label, hoverTipDescription);
        }

        private static ModSettingsText? InlineDescription(ModSettingsMirrorEntryDefinition entry)
        {
            return entry.DescriptionAsHoverTip ? null : entry.Description;
        }

        private static ModSettingsText? HoverTipDescription(ModSettingsMirrorEntryDefinition entry)
        {
            return entry.HoverTipDescription ?? (entry.DescriptionAsHoverTip ? entry.Description : null);
        }
    }
}
