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
                    section.AddHeader(entry.Id, entry.Label, entry.Description);
                    ApplyVisibility(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.Paragraph:
                    section.AddParagraph(entry.Id, entry.Label, entry.Description, entry.MaxBodyHeight);
                    ApplyVisibility(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.Toggle:
                    section.AddToggle(entry.Id, entry.Label, (IModSettingsValueBinding<bool>)entry.Binding!,
                        entry.Description);
                    ApplyVisibility(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.Slider:
                    switch (entry.Binding)
                    {
                        case IModSettingsValueBinding<double> doubleBinding:
                        {
                            var numeric = entry.Numeric!;
                            section.AddSlider(entry.Id, entry.Label, doubleBinding, numeric.Min, numeric.Max,
                                numeric.Step,
                                numeric.FormatDouble, entry.Description);
                            ApplyVisibility(section, entry);
                            return;
                        }
                        case IModSettingsValueBinding<float> floatBinding:
                        {
                            var numeric = entry.Numeric!;
#pragma warning disable CS0618
                            section.AddSlider(entry.Id, entry.Label, floatBinding, (float)numeric.Min,
                                (float)numeric.Max,
                                (float)numeric.Step, numeric.FormatFloat, entry.Description);
#pragma warning restore CS0618
                            ApplyVisibility(section, entry);
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
                        entry.Description);
                    ApplyVisibility(section, entry);
                    return;
                }
                case ModSettingsMirrorEntryKind.Choice:
                    section.AddChoice(entry.Id, entry.Label, (IModSettingsValueBinding<string>)entry.Binding!,
                        entry.ChoiceOptions!
                            .Select(option => new ModSettingsChoiceOption<string>(option.Value, option.Label))
                            .ToArray(),
                        entry.Description, entry.ChoicePresentation);
                    ApplyVisibility(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.EnumChoice:
                    AppendEnumChoice(section, entry);
                    ApplyVisibility(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.Color:
                    section.AddColor(entry.Id, entry.Label, (IModSettingsValueBinding<string>)entry.Binding!,
                        entry.Description,
                        entry.EditAlpha, entry.EditIntensity);
                    ApplyVisibility(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.String:
                    section.AddString(entry.Id, entry.Label, (IModSettingsValueBinding<string>)entry.Binding!,
                        entry.Placeholder,
                        entry.MaxLength, entry.Description, entry.ValidationVisual);
                    ApplyVisibility(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.KeyBinding:
                    section.AddKeyBinding(entry.Id, entry.Label, (IModSettingsValueBinding<string>)entry.Binding!,
                        entry.AllowModifierCombos, entry.AllowModifierOnly, entry.DistinguishModifierSides,
                        entry.Description);
                    ApplyVisibility(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.Button:
                    section.AddButton(entry.Id, entry.Label, entry.ButtonLabel ?? entry.Label, entry.OnClick!,
                        entry.ButtonTone,
                        entry.Description);
                    ApplyVisibility(section, entry);
                    return;
                case ModSettingsMirrorEntryKind.Subpage:
                    section.AddSubpage(entry.Id, entry.Label, entry.TargetPageId!, entry.ButtonLabel,
                        entry.Description);
                    ApplyVisibility(section, entry);
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
            section.AddEnumChoice(entry.Id, entry.Label, (IModSettingsValueBinding<TEnum>)entry.Binding!, null,
                entry.Description,
                entry.ChoicePresentation);
        }

        private static void ApplyVisibility(ModSettingsSectionBuilder section, ModSettingsMirrorEntryDefinition entry)
        {
            if (entry.VisibleWhen != null)
                section.WithEntryVisibleWhen(entry.Id, entry.VisibleWhen);
        }
    }
}
