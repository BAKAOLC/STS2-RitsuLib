using Godot;

namespace STS2RitsuLib.Settings
{
    internal static partial class ModSettingsUiFactory
    {
        internal const float EntryControlWidth = ModSettingsUiMetrics.EntryValueMinWidth;
        private const float PageContentWidth = 0f;

        private const string ContextMenuAttachedMetaKey = "_ritsulib_context_menu_attached";

        internal static void RegisterRefreshWhenAlive(ModSettingsUiContext context, GodotObject? node, Action action,
            ModSettingsUiRefreshSpec spec)
        {
            if (node == null)
            {
                context.RegisterRefresh(action, spec);
                return;
            }

            context.RegisterRefresh(() =>
            {
                if (!GodotObject.IsInstanceValid(node))
                    return;
                action();
            }, spec);
        }

        public static Control CreatePageContent(ModSettingsUiContext context, ModSettingsPage page)
        {
            var container = CreatePageContentHost(page);
            foreach (var item in CreatePageBuildItems(context, page))
                container.AddChild(item.Control);
            return MaybeWrapDynamicVisibility(context, container, page.VisibleWhen);
        }

        internal static VBoxContainer CreatePageContentHost(ModSettingsPage page)
        {
            var container = new VBoxContainer
            {
                Name = $"Page_{SanitizeName(page.ModId)}_{SanitizeName(page.Id)}",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            container.AddThemeConstantOverride("separation", 8);
            return container;
        }

        internal static IEnumerable<PageBuildItem> CreatePageBuildItems(ModSettingsUiContext context,
            ModSettingsPage page)
        {
            for (var index = 0; index < page.Sections.Count; index++)
            {
                var section = page.Sections[index];
                if (index > 0)
                    yield return new(CreateDivider(), false);

                Control builtSection;
                try
                {
                    builtSection = CreateSection(context, page, section);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Settings] Failed to build section '{page.ModId}:{page.Id}:{section.Id}': {ex.Message}");
                    builtSection = CreateBuildErrorPlaceholder(
                        ModSettingsLocalization.Get("section.failed.title", "Section failed to load"),
                        string.Format(
                            ModSettingsLocalization.Get("section.failed.body", "Failed to build section '{0}'."),
                            section.Id));
                }

                yield return new(builtSection, true);
            }
        }

        public static Control CreateToggleEntry(ModSettingsUiContext context, ToggleModSettingsEntryDefinition entry)
        {
            var control = new ModSettingsToggleControl(
                entry.Binding.Read(),
                value =>
                {
                    entry.Binding.Write(value);
                    context.MarkDirty(entry.Binding);
                    context.RequestRefresh();
                });
            RegisterRefreshWhenAlive(context, control, () => control.SetValue(entry.Binding.Read()),
                ModSettingsUiRefreshSpec.ForBinding(entry.Binding));

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => ModSettingsUiControlFactoryHelper.ResolveDescription(entry.Description),
                control,
                entry.Binding,
                entry.MenuCapabilities,
                entry.Label,
                entry.Description);
        }

        public static Control CreateSliderEntry(ModSettingsUiContext context, SliderModSettingsEntryDefinition entry)
        {
            var control = new ModSettingsSliderControl(
                entry.Binding.Read(),
                entry.MinValue,
                entry.MaxValue,
                entry.Step,
                FormatValue,
                value =>
                {
                    entry.Binding.Write(value);
                    context.MarkDirty(entry.Binding);
                    context.RequestRefresh();
                });
            RegisterRefreshWhenAlive(context, control, () => control.SetValue(entry.Binding.Read()),
                ModSettingsUiRefreshSpec.ForBinding(entry.Binding));

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => ModSettingsUiControlFactoryHelper.ResolveDescription(entry.Description),
                control,
                entry.Binding,
                entry.MenuCapabilities,
                entry.Label,
                entry.Description);

            string FormatValue(double value)
            {
                return entry.ValueFormatter?.Invoke(value) ?? value.ToString("0.##");
            }
        }

        public static Control CreateFloatSliderEntry(ModSettingsUiContext context,
            FloatSliderModSettingsEntryDefinition entry)
        {
            var control = new ModSettingsFloatSliderControl(
                entry.Binding.Read(),
                entry.MinValue,
                entry.MaxValue,
                entry.Step,
                FormatValue,
                value =>
                {
                    entry.Binding.Write(value);
                    context.MarkDirty(entry.Binding);
                    context.RequestRefresh();
                });
            RegisterRefreshWhenAlive(context, control, () => control.SetValue(entry.Binding.Read()),
                ModSettingsUiRefreshSpec.ForBinding(entry.Binding));

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => ModSettingsUiControlFactoryHelper.ResolveDescription(entry.Description),
                control,
                entry.Binding,
                entry.MenuCapabilities,
                entry.Label,
                entry.Description);

            string FormatValue(float value)
            {
                return entry.ValueFormatter?.Invoke(value) ?? value.ToString("0.##");
            }
        }

        public static Control CreateChoiceEntry<TValue>(ModSettingsUiContext context,
            ChoiceModSettingsEntryDefinition<TValue> entry)
        {
            var resolvedOptions = entry.Options
                .Select(option => (option.Value, Label: ModSettingsUiContext.Resolve(option.Label)))
                .ToArray();

            Control control;
            Action refreshRegistration;

            if (entry.Presentation == ModSettingsChoicePresentation.Dropdown)
            {
                var dropdown = new ModSettingsDropdownChoiceControl<TValue>(
                    resolvedOptions,
                    entry.Binding.Read(),
                    value =>
                    {
                        entry.Binding.Write(value);
                        context.MarkDirty(entry.Binding);
                        context.RequestRefresh();
                    });
                control = dropdown;
                refreshRegistration = () => dropdown.SetValue(entry.Binding.Read());
            }
            else
            {
                var stepper = new ModSettingsChoiceControl<TValue>(
                    resolvedOptions,
                    entry.Binding.Read(),
                    value =>
                    {
                        entry.Binding.Write(value);
                        context.MarkDirty(entry.Binding);
                        context.RequestRefresh();
                    });
                control = stepper;
                refreshRegistration = () => stepper.SetValue(entry.Binding.Read());
            }

            RegisterRefreshWhenAlive(context, control, refreshRegistration,
                ModSettingsUiRefreshSpec.ForBinding(entry.Binding));

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => ModSettingsUiControlFactoryHelper.ResolveDescription(entry.Description),
                control,
                entry.Binding,
                entry.MenuCapabilities,
                entry.Label,
                entry.Description);
        }

        public static Control CreateColorEntry(ModSettingsUiContext context, ColorModSettingsEntryDefinition entry)
        {
            var control = new ModSettingsColorControl(
                entry.Binding.Read(),
                value =>
                {
                    entry.Binding.Write(value ?? string.Empty);
                    context.MarkDirty(entry.Binding);
                    context.RequestRefresh();
                },
                entry.EditAlpha,
                entry.EditIntensity);
            RegisterRefreshWhenAlive(context, control, () => control.SetValue(entry.Binding.Read()),
                ModSettingsUiRefreshSpec.ForBinding(entry.Binding));

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => ModSettingsUiControlFactoryHelper.ResolveDescription(entry.Description),
                control,
                entry.Binding,
                entry.MenuCapabilities,
                entry.Label,
                entry.Description);
        }

        public static Control CreateStringLineEntry(ModSettingsUiContext context,
            StringModSettingsEntryDefinition entry)
        {
            var placeholder = ResolveStringFieldPlaceholder(entry);
            var control = new ModSettingsStringLineControl(
                entry.Binding.Read(),
                placeholder,
                entry.MaxLength,
                CreateStringFieldCommitHandler(context, entry),
                entry.ValueValidationVisual);
            RegisterRefreshWhenAlive(context, control, () => control.SetValue(entry.Binding.Read()),
                ModSettingsUiRefreshSpec.ForBinding(entry.Binding));

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => ModSettingsUiControlFactoryHelper.ResolveDescription(entry.Description),
                control,
                entry.Binding,
                entry.MenuCapabilities,
                entry.Label,
                entry.Description);
        }

        public static Control CreateStringMultilineEntry(ModSettingsUiContext context,
            MultilineStringModSettingsEntryDefinition entry)
        {
            var placeholder = ResolveStringFieldPlaceholder(entry);
            var control = new ModSettingsStringMultilineControl(
                entry.Binding.Read(),
                placeholder,
                entry.MaxLength,
                CreateStringFieldCommitHandler(context, entry));
            RegisterRefreshWhenAlive(context, control, () => control.SetValue(entry.Binding.Read()),
                ModSettingsUiRefreshSpec.ForBinding(entry.Binding));

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => ModSettingsUiControlFactoryHelper.ResolveDescription(entry.Description),
                control,
                entry.Binding,
                entry.MenuCapabilities,
                entry.Label,
                entry.Description);
        }

        private static string? ResolveStringFieldPlaceholder(StringFieldModSettingsEntryDefinition entry)
        {
            return entry.Placeholder != null ? ModSettingsUiContext.Resolve(entry.Placeholder) : null;
        }

        private static Action<string> CreateStringFieldCommitHandler(ModSettingsUiContext context,
            StringFieldModSettingsEntryDefinition entry)
        {
            return value =>
            {
                entry.Binding.Write(value);
                context.MarkDirty(entry.Binding);
                context.RequestRefresh();
            };
        }

        public static Control CreateKeyBindingEntry(ModSettingsUiContext context,
            KeyBindingModSettingsEntryDefinition entry)
        {
            var control = new ModSettingsKeyBindingControl(
                entry.Binding.Read(),
                entry.AllowModifierCombos,
                entry.AllowModifierOnly,
                entry.DistinguishModifierSides,
                value =>
                {
                    entry.Binding.Write(value);
                    context.MarkDirty(entry.Binding);
                    context.RequestRefresh();
                });
            RegisterRefreshWhenAlive(context, control, () => control.SetValue(entry.Binding.Read()),
                ModSettingsUiRefreshSpec.ForBinding(entry.Binding));

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => ModSettingsUiControlFactoryHelper.ResolveDescription(entry.Description),
                control,
                entry.Binding,
                entry.MenuCapabilities,
                entry.Label,
                entry.Description);
        }

        public static Control CreateMultiKeyBindingEntry(ModSettingsUiContext context,
            MultiKeyBindingModSettingsEntryDefinition entry)
        {
            var control = new ModSettingsMultiKeyBindingControl(
                entry.Binding.Read(),
                entry.AllowModifierCombos,
                entry.AllowModifierOnly,
                entry.DistinguishModifierSides,
                value =>
                {
                    entry.Binding.Write(value);
                    context.MarkDirty(entry.Binding);
                });
            RegisterRefreshWhenAlive(context, control, () => control.SetValue(entry.Binding.Read()),
                ModSettingsUiRefreshSpec.ForBinding(entry.Binding));

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => ModSettingsUiControlFactoryHelper.ResolveDescription(entry.Description),
                control,
                entry.Binding,
                entry.MenuCapabilities,
                entry.Label,
                entry.Description);
        }

        public static Control CreateButtonEntry(ModSettingsUiContext context, ButtonModSettingsEntryDefinition entry)
        {
            var control = new ModSettingsTextButton(
                ModSettingsUiContext.Resolve(entry.ButtonText),
                entry.Tone,
                () =>
                {
                    entry.Action();
                    context.RequestRefresh();
                });

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => ModSettingsUiContext.Resolve(entry.Description),
                control,
                null,
                null,
                entry.Label,
                entry.Description);
        }

        public static Control CreateHostContextButtonEntry(ModSettingsUiContext context,
            HostContextButtonModSettingsEntryDefinition entry)
        {
            var control = new ModSettingsTextButton(
                ModSettingsUiContext.Resolve(entry.ButtonText),
                entry.Tone,
                () =>
                {
                    entry.Action(context);
                    context.RequestRefresh();
                });

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => ModSettingsUiContext.Resolve(entry.Description),
                control,
                null,
                null,
                entry.Label,
                entry.Description);
        }

        public static Control CreateHeaderEntry(ModSettingsUiContext context, HeaderModSettingsEntryDefinition entry)
        {
            var container = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            container.AddThemeConstantOverride("separation", 6);
            container.AddChild(CreateRefreshableSectionTitle(context, entry.Label,
                () => ResolveEntryLabelDisplay(entry.Label)));
            if (entry.Description != null)
                container.AddChild(CreateRefreshableDescriptionLabel(context, entry.Description,
                    () => ModSettingsUiContext.Resolve(entry.Description)));
            return container;
        }

        public static Control CreateParagraphEntry(ModSettingsUiContext context,
            ParagraphModSettingsEntryDefinition entry)
        {
            var cap = entry.MaxBodyHeight ?? ModSettingsUiPresentation.ParagraphMaxBodyHeight;
            return CreateRefreshableParagraphBlock(context, entry.Label,
                () => ModSettingsUiContext.Resolve(entry.Label), cap);
        }

        public static Control CreateInfoCardEntry(ModSettingsUiContext context,
            InfoCardModSettingsEntryDefinition entry)
        {
            var line = new MarginContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            line.AddThemeConstantOverride("margin_left", 8);
            line.AddThemeConstantOverride("margin_right", 8);
            line.AddThemeConstantOverride("margin_top", 6);
            line.AddThemeConstantOverride("margin_bottom", 6);

            var surface = new PanelContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            surface.AddThemeStyleboxOverride("panel", CreateInsetSurfaceStyle());
            line.AddChild(surface);

            var stack = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            stack.AddThemeConstantOverride("separation", 6);
            surface.AddChild(stack);

            stack.AddChild(CreateRefreshableSectionTitle(context, entry.Label,
                () => ResolveEntryLabelDisplay(entry.Label)));

            if (entry.Description != null)
                stack.AddChild(CreateRefreshableDescriptionLabel(context, entry.Description,
                    () => ModSettingsUiContext.Resolve(entry.Description)));

            stack.AddChild(CreateRefreshableParagraphBlock(context, entry.Body,
                () => ModSettingsUiContext.Resolve(entry.Body),
                ModSettingsUiPresentation.ParagraphMaxBodyHeight));
            return line;
        }

        public static Control CreateRuntimeHotkeySummaryEntry(ModSettingsUiContext context,
            RuntimeHotkeySummaryModSettingsEntryDefinition entry)
        {
            var line = new MarginContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            line.AddThemeConstantOverride("margin_left", 8);
            line.AddThemeConstantOverride("margin_right", 8);
            line.AddThemeConstantOverride("margin_top", 6);
            line.AddThemeConstantOverride("margin_bottom", 6);

            var surface = new PanelContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            surface.AddThemeStyleboxOverride("panel", CreateEntrySurfaceStyle());
            line.AddChild(surface);

            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            row.AddThemeConstantOverride("separation", 20);
            surface.AddChild(row);

            var left = new VBoxContainer
            {
                CustomMinimumSize = new(220f, 0f),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            left.AddThemeConstantOverride("separation", 6);
            row.AddChild(left);

            var titleRow = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Begin,
            };
            titleRow.AddThemeConstantOverride("separation", 8);
            left.AddChild(titleRow);

            var title = CreateRefreshableHeaderLabel(context, entry.Label,
                () => ResolveEntryLabelDisplay(entry.Label), 24, HorizontalAlignment.Left,
                ModSettingsUiPalette.RichTextTitle);
            title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            titleRow.AddChild(title);

            var idLabel = new Label
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,
                VerticalAlignment = VerticalAlignment.Center,
                Visible = entry.Description != null,
            };
            idLabel.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
            idLabel.AddThemeFontSizeOverride("font_size", 16);
            idLabel.AddThemeColorOverride("font_color", ModSettingsUiPalette.RichTextMuted);
            titleRow.AddChild(idLabel);
            var idLabelSpec = entry.Description?.GetUiRefreshSpec() ?? ModSettingsUiRefreshSpec.StaticDisplay;
            RegisterRefreshWhenAlive(context, idLabel, () =>
            {
                var idText = entry.Description == null ? string.Empty : ModSettingsUiContext.Resolve(entry.Description);
                idLabel.Text = string.IsNullOrWhiteSpace(idText) ? string.Empty : $"({idText})";
                idLabel.Visible = !string.IsNullOrWhiteSpace(idText);
            }, idLabelSpec);

            left.AddChild(CreateRefreshableDescriptionLabel(context, entry.Body,
                () => ModSettingsUiContext.Resolve(entry.Body)));

            var bindingsColumn = new VBoxContainer
            {
                CustomMinimumSize = new(ModSettingsUiMetrics.KeybindingBlockWidth, 0f),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            bindingsColumn.AddThemeConstantOverride("separation", 6);
            row.AddChild(bindingsColumn);

            RegisterRefreshWhenAlive(context, bindingsColumn, () =>
            {
                foreach (var child in bindingsColumn.GetChildren())
                    child.QueueFree();

                foreach (var binding in entry.Bindings)
                {
                    var chip = new PanelContainer
                    {
                        MouseFilter = Control.MouseFilterEnum.Ignore,
                        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    };
                    chip.AddThemeStyleboxOverride("panel", CreateInsetSurfaceStyle());
                    var chipLabel = new Label
                    {
                        Text = ModSettingsUiContext.Resolve(binding),
                        MouseFilter = Control.MouseFilterEnum.Ignore,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        AutowrapMode = TextServer.AutowrapMode.Off,
                        ClipText = true,
                    };
                    chipLabel.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
                    chipLabel.AddThemeFontSizeOverride("font_size", 16);
                    chipLabel.AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelPrimary);
                    chip.AddChild(chipLabel);
                    bindingsColumn.AddChild(chip);
                }
            }, ModSettingsUiRefreshSpec.AnyBindingDirty);

            return line;
        }

        public static Control CreateImageEntry(ModSettingsUiContext context, ImageModSettingsEntryDefinition entry)
        {
            var container = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            container.AddThemeConstantOverride("separation", 8);
            container.AddChild(CreateRefreshableSectionTitle(context, entry.Label,
                () => ResolveEntryLabelDisplay(entry.Label)));

            if (entry.Description != null)
                container.AddChild(CreateRefreshableDescriptionLabel(context, entry.Description,
                    () => ModSettingsUiContext.Resolve(entry.Description)));

            var surface = new PanelContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new(0f, entry.PreviewHeight),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            surface.AddThemeStyleboxOverride("panel", CreateEntrySurfaceStyle());

            var preview = new TextureRect
            {
                Texture = entry.TextureProvider(),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new(0f, entry.PreviewHeight),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            RegisterRefreshWhenAlive(context, preview, () => preview.Texture = entry.TextureProvider(),
                ModSettingsUiRefreshSpec.StaticDisplay);
            surface.AddChild(preview);
            container.AddChild(surface);
            return container;
        }

        public static Control CreateCustomEntry(ModSettingsUiContext context, CustomModSettingsEntryDefinition entry)
        {
            return entry.ControlFactory(context);
        }

        public static Control CreateListEntry<TItem>(ModSettingsUiContext context,
            ListModSettingsEntryDefinition<TItem> entry)
        {
            return new ModSettingsListControl<TItem>(context, entry);
        }

        public static Control CreateIntSliderEntry(ModSettingsUiContext context,
            IntSliderModSettingsEntryDefinition entry)
        {
            var control = new ModSettingsSliderControl(
                entry.Binding.Read(),
                entry.MinValue,
                entry.MaxValue,
                entry.Step,
                FormatValue,
                value =>
                {
                    entry.Binding.Write(Mathf.RoundToInt(value));
                    context.MarkDirty(entry.Binding);
                    context.RequestRefresh();
                });
            RegisterRefreshWhenAlive(context, control, () => control.SetValue(entry.Binding.Read()),
                ModSettingsUiRefreshSpec.ForBinding(entry.Binding));

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => ModSettingsUiControlFactoryHelper.ResolveDescription(entry.Description),
                control,
                entry.Binding,
                entry.MenuCapabilities,
                entry.Label,
                entry.Description);

            string FormatValue(double value)
            {
                var intValue = Mathf.RoundToInt(value);
                return entry.ValueFormatter?.Invoke(intValue) ?? intValue.ToString();
            }
        }

        public static Control CreateSubpageEntry(ModSettingsUiContext context, SubpageModSettingsEntryDefinition entry)
        {
            var control = new ModSettingsTextButton(
                ModSettingsUiContext.Resolve(entry.ButtonText, ModSettingsLocalization.Get("button.open", "Open")),
                ModSettingsButtonTone.Accent,
                () => context.NavigateToPage(entry.TargetPageId));
            control.CustomMinimumSize = new(EntryControlWidth, ModSettingsUiMetrics.EntryValueMinHeight);

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => ModSettingsUiContext.Resolve(entry.Description),
                control,
                null,
                null,
                entry.Label,
                entry.Description);
        }

        internal sealed record PageBuildItem(Control Control, bool YieldAfter);
    }
}
