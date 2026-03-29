using Godot;
using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    internal static class RitsuLibModSettingsBootstrap
    {
        private static readonly Lock InitLock = new();
        private static bool _initialized;

        internal static void Initialize()
        {
            lock (InitLock)
            {
                if (_initialized)
                    return;

                var debugCompatibilityBinding = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.DebugCompatibilityMode,
                    (settings, value) => settings.DebugCompatibilityMode = value);

                var showcaseState = new DebugShowcaseState();
                var previewToggleBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_toggle", showcaseState.ToggleValue);
                var previewSliderBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_slider", showcaseState.SliderValue);
                var previewIntSliderBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_int_slider", showcaseState.IntSliderValue);
                var previewChoiceBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_choice", showcaseState.ChoiceValue);
                var previewChoiceDropdownBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_choice_dropdown",
                        showcaseState.ChoiceDropdownValue);
                var previewEnumBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_mode", showcaseState.ModeValue);
                var previewStringBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_string", showcaseState.StringValue);
                var previewStringMultiBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_string_multi", showcaseState.StringMultiValue);
                var previewListBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_list", showcaseState.ListItems.ToList());

                RitsuLibFramework.RegisterModSettings(Const.ModId, page => page
                    .WithModDisplayName(T("ritsulib.mod.displayName", "RitsuLib"))
                    .WithModSidebarOrder(-10_000)
                    .WithTitle(T("ritsulib.page.title", "Settings"))
                    .WithDescription(T("ritsulib.page.description",
                        "Shared framework options and API reference examples."))
                    .WithSortOrder(-1000)
                    .AddSection("general", section => section
                        .WithTitle(T("ritsulib.section.general.title", "General"))
                        .WithDescription(T("ritsulib.section.general.description",
                            "Player-facing framework options that are actually persisted."))
                        .AddToggle(
                            "debug_compatibility_mode",
                            T("ritsulib.debugCompatibility.label", "Debug compatibility mode"),
                            debugCompatibilityBinding,
                            T("ritsulib.debugCompatibility.description",
                                "Missing LocString keys fall back to placeholders and warnings instead of throwing immediately.")))
                    .AddSection("reference", section => section
                        .WithTitle(T("ritsulib.section.reference.title", "Reference"))
                        .WithDescription(T("ritsulib.section.reference.description",
                            "Preview all currently available settings controls."))
                        .Collapsible()
                        .AddParagraph(
                            "reference_intro",
                            T("ritsulib.reference.intro",
                                "Open the debug gallery below to inspect available controls and layout behavior."))
                        .AddSubpage(
                            "reference_gallery",
                            T("ritsulib.reference.gallery.label", "Component gallery"),
                            "debug-showcase",
                            T("button.open", "Open"),
                            T("ritsulib.reference.gallery.description",
                                "This page is for preview and reference only. None of its values are persisted."))));

                RitsuLibFramework.RegisterModSettings(
                    Const.ModId,
                    page => page
                        .AsChildOf(Const.ModId)
                        .WithTitle(T("ritsulib.showcase.title", "Debug Showcase"))
                        .WithDescription(T("ritsulib.showcase.description",
                            "Preview every control type and observe live description updates without saving any values."))
                        .AddSection("overview", section => section
                            .WithTitle(T("ritsulib.showcase.overview.title", "Overview"))
                            .AddHeader(
                                "showcase_header",
                                T("ritsulib.showcase.header", "Preview-only controls"),
                                T("ritsulib.showcase.header.description",
                                    "These examples are intentionally transient and exist only as implementation references."))
                            .AddParagraph(
                                "showcase_paragraph",
                                ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.summary",
                                            "Toggle: {0} | Double: {1:0.##} | Int: {2} | Choice: {3} | Dropdown: {4} | Mode: {5} | Action Count: {6}"),
                                        showcaseState.ToggleValue,
                                        showcaseState.SliderValue,
                                        showcaseState.IntSliderValue,
                                        showcaseState.ChoiceValue,
                                        showcaseState.ChoiceDropdownValue,
                                        showcaseState.ModeValue,
                                        showcaseState.ActionCount)))
                            .AddImage(
                                "showcase_image",
                                T("ritsulib.showcase.image.label", "Preview image"),
                                () => ModSettingsUiResources.SettingsButtonTexture,
                                120f,
                                ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.image.description",
                                            "Image preview updates can be paired with dynamic descriptive text. Current mode: {0}"),
                                        showcaseState.ModeValue))))
                        .AddSection("inputs", section => section
                            .WithTitle(T("ritsulib.showcase.inputs.title", "Inputs"))
                            .WithDescription(T("ritsulib.showcase.inputs.description",
                                "All values update the descriptive text immediately, but are never written to disk."))
                            .Collapsible()
                            .AddToggle(
                                "preview_toggle",
                                T("ritsulib.showcase.toggle.label", "Preview toggle"),
                                new ShowcaseBinding<bool>(previewToggleBinding,
                                    value => showcaseState.ToggleValue = value),
                                ModSettingsText.Dynamic(() =>
                                    string.Format(L("ritsulib.showcase.toggle.description", "Current value: {0}"),
                                        showcaseState.ToggleValue)))
                            .AddSlider(
                                "preview_slider",
                                T("ritsulib.showcase.slider.label", "Preview slider"),
                                new ShowcaseBinding<double>(previewSliderBinding,
                                    value => showcaseState.SliderValue = value),
                                0d,
                                100d,
                                0.25d,
                                value => value.ToString("0.##"),
                                ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.slider.description", "Current double value: {0:0.##}"),
                                        showcaseState.SliderValue)))
                            .AddIntSlider(
                                "preview_int_slider",
                                T("ritsulib.showcase.intSlider.label", "Preview integer slider"),
                                new ShowcaseBinding<int>(previewIntSliderBinding,
                                    value => showcaseState.IntSliderValue = value),
                                0,
                                5,
                                1,
                                value => value.ToString(),
                                ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.intSlider.description", "Current integer value: {0}"),
                                        showcaseState.IntSliderValue)))
                            .AddChoice(
                                "preview_choice",
                                T("ritsulib.showcase.choice.label", "Preview choice"),
                                new ShowcaseBinding<string>(previewChoiceBinding,
                                    value => showcaseState.ChoiceValue = value),
                                [
                                    new("compact", T("ritsulib.showcase.choice.compact", "Compact")),
                                    new("balanced", T("ritsulib.showcase.choice.balanced", "Balanced")),
                                    new("wide", T("ritsulib.showcase.choice.wide", "Wide")),
                                ],
                                ModSettingsText.Dynamic(() =>
                                    string.Format(L("ritsulib.showcase.choice.description", "Current choice: {0}"),
                                        showcaseState.ChoiceValue)))
                            .AddChoice(
                                "preview_choice_dropdown",
                                T("ritsulib.showcase.choiceDropdown.label", "Preview choice (dropdown)"),
                                new ShowcaseBinding<string>(previewChoiceDropdownBinding,
                                    value => showcaseState.ChoiceDropdownValue = value),
                                [
                                    new("compact", T("ritsulib.showcase.choice.compact", "Compact")),
                                    new("balanced", T("ritsulib.showcase.choice.balanced", "Balanced")),
                                    new("wide", T("ritsulib.showcase.choice.wide", "Wide")),
                                ],
                                ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.choiceDropdown.description",
                                            "Same options as the stepper above, using the custom dropdown list. Current: {0}"),
                                        showcaseState.ChoiceDropdownValue)),
                                ModSettingsChoicePresentation.Dropdown)
                            .AddEnumChoice(
                                "preview_mode",
                                T("ritsulib.showcase.mode.label", "Preview enum choice"),
                                new ShowcaseBinding<ShowcaseMode>(previewEnumBinding,
                                    value => showcaseState.ModeValue = value),
                                mode => T($"ritsulib.showcase.mode.{mode}", mode.ToString()),
                                ModSettingsText.Dynamic(() =>
                                    string.Format(L("ritsulib.showcase.mode.description", "Current mode: {0}"),
                                        showcaseState.ModeValue)))
                            .AddString(
                                "preview_string",
                                T("ritsulib.showcase.string.label", "Preview string field"),
                                new ShowcaseBinding<string>(previewStringBinding,
                                    value => showcaseState.StringValue = value),
                                T("ritsulib.showcase.string.placeholder", "Plain string binding (LineEdit)"),
                                null,
                                ModSettingsText.Dynamic(() =>
                                    string.Format(L("ritsulib.showcase.string.description", "Current text: {0}"),
                                        showcaseState.StringValue)))
                            .AddMultilineString(
                                "preview_string_multi",
                                T("ritsulib.showcase.stringMulti.label", "Preview multiline string"),
                                new ShowcaseBinding<string>(previewStringMultiBinding,
                                    value => showcaseState.StringMultiValue = value),
                                T("ritsulib.showcase.stringMulti.placeholder",
                                    "Multiple lines — Enter inserts a new line."),
                                null,
                                ModSettingsText.Dynamic(() =>
                                {
                                    var t = showcaseState.StringMultiValue ?? string.Empty;
                                    var lineCount = string.IsNullOrEmpty(t) ? 0 : t.Split('\n').Length;
                                    return string.Format(
                                        L("ritsulib.showcase.stringMulti.description",
                                            "{0} characters, {1} lines."),
                                        t.Length,
                                        lineCount);
                                })))
                        .AddSection("actions", section => section
                            .WithTitle(T("ritsulib.showcase.actions.title", "Actions"))
                            .WithDescription(T("ritsulib.showcase.actions.description",
                                "Buttons can mutate preview state and update the surrounding text immediately."))
                            .Collapsible()
                            .AddButton(
                                "preview_action",
                                T("ritsulib.showcase.action.label", "Preview action button"),
                                T("ritsulib.showcase.action.button", "Trigger"),
                                () => showcaseState.ActionCount++,
                                ModSettingsButtonTone.Accent,
                                ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.action.description", "Action triggered {0} times."),
                                        showcaseState.ActionCount)))
                            .AddButton(
                                "preview_reset",
                                T("ritsulib.showcase.reset.label", "Reset preview state"),
                                T("ritsulib.showcase.reset.button", "Reset"),
                                () =>
                                {
                                    showcaseState.ToggleValue = true;
                                    showcaseState.SliderValue = 35d;
                                    showcaseState.IntSliderValue = 2;
                                    showcaseState.ChoiceValue = "balanced";
                                    showcaseState.ChoiceDropdownValue = "wide";
                                    showcaseState.ModeValue = ShowcaseMode.Balanced;
                                    showcaseState.ActionCount = 0;
                                    showcaseState.StringValue = "Single line";
                                    showcaseState.StringMultiValue = "First line\nSecond line";
                                    previewToggleBinding.Write(showcaseState.ToggleValue);
                                    previewSliderBinding.Write(showcaseState.SliderValue);
                                    previewIntSliderBinding.Write(showcaseState.IntSliderValue);
                                    previewChoiceBinding.Write(showcaseState.ChoiceValue);
                                    previewChoiceDropdownBinding.Write(showcaseState.ChoiceDropdownValue);
                                    previewEnumBinding.Write(showcaseState.ModeValue);
                                    previewStringBinding.Write(showcaseState.StringValue);
                                    previewStringMultiBinding.Write(showcaseState.StringMultiValue);
                                },
                                ModSettingsButtonTone.Danger,
                                T("ritsulib.showcase.reset.description",
                                    "Restore all preview values to their defaults without persisting anything."))
                            .AddParagraph(
                                "showcase_footer",
                                T("ritsulib.showcase.footer",
                                    "Use this page as a quick implementation reference when building settings for other mods.")))
                        .AddSection("list", section => section
                            .WithTitle(T("ritsulib.showcase.list.title", "List Editor"))
                            .WithDescription(T("ritsulib.showcase.list.description",
                                "A structured list can be displayed, reordered, added, and deleted inside the settings UI."))
                            .Collapsible()
                            .AddList(
                                "preview_list",
                                T("ritsulib.showcase.list.label", "Preview structured list"),
                                new ShowcaseBinding<List<ShowcaseListItem>>(previewListBinding,
                                    value => showcaseState.ListItems = value.ToList()),
                                () => showcaseState.CreateListItem(),
                                item => ModSettingsText.Literal($"{item.Name} ({item.Weight})"),
                                item => ModSettingsText.Literal(item.Enabled
                                    ? $"Enabled item - tag: {item.Tag} - notes: {item.Details.Count}"
                                    : $"Disabled item - tag: {item.Tag} - notes: {item.Details.Count}"),
                                CreateShowcaseListItemEditor,
                                ModSettingsStructuredData.Json<ShowcaseListItem>(),
                                T("ritsulib.showcase.list.add", "Add Item"),
                                ModSettingsText.Dynamic(() =>
                                    string.Format(L("ritsulib.showcase.list.summary", "Current item count: {0}"),
                                        showcaseState.ListItems.Count)))),
                    "debug-showcase");

                _initialized = true;
            }
        }

        private static ModSettingsText T(string key, string fallback)
        {
            return ModSettingsText.I18N(ModSettingsLocalization.Instance, key, fallback);
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }

        private static Control CreateShowcaseListItemEditor(ModSettingsListItemContext<ShowcaseListItem> itemContext)
        {
            var content = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            content.AddThemeConstantOverride("separation", 12);

            var grid = new GridContainer
            {
                Columns = 2,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            grid.AddThemeConstantOverride("h_separation", 12);
            grid.AddThemeConstantOverride("v_separation", 12);
            content.AddChild(grid);

            var nameEdit = CreateListField(itemContext.Item.Name, value =>
                itemContext.Update(itemContext.Item with { Name = value }));
            nameEdit.CustomMinimumSize = new(220f, 44f);
            grid.AddChild(CreateListFieldGroup(ModSettingsLocalization.Get("ritsulib.showcase.list.field.name", "Name"),
                nameEdit));

            var tagEdit = CreateListField(itemContext.Item.Tag, value =>
                itemContext.Update(itemContext.Item with { Tag = value }));
            tagEdit.CustomMinimumSize = new(180f, 44f);
            grid.AddChild(CreateListFieldGroup(ModSettingsLocalization.Get("ritsulib.showcase.list.field.tag", "Tag"),
                tagEdit));

            var weightEdit = CreateListField(itemContext.Item.Weight.ToString(), value =>
            {
                if (int.TryParse(value, out var weight))
                    itemContext.Update(itemContext.Item with { Weight = weight });
                else
                    itemContext.RequestRefresh();
            });
            weightEdit.CustomMinimumSize = new(120f, 44f);
            grid.AddChild(CreateListFieldGroup(
                ModSettingsLocalization.Get("ritsulib.showcase.list.field.weight", "Weight"), weightEdit));

            var enabledButton = new ModSettingsToggleControl(itemContext.Item.Enabled,
                value => itemContext.Update(itemContext.Item with { Enabled = value }))
            {
                CustomMinimumSize = new(140f, 44f),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            };
            grid.AddChild(CreateListFieldGroup(
                ModSettingsLocalization.Get("ritsulib.showcase.list.field.enabled", "Enabled"), enabledButton, false));

            var nestedListBinding = itemContext.Project(
                "details",
                item => item.Details,
                (item, details) => item with { Details = details },
                ModSettingsStructuredData.List(ModSettingsStructuredData.Json<ShowcaseListDetail>()));

            content.AddChild(itemContext.CreateListEditor(
                "details",
                ModSettingsText.I18N(ModSettingsLocalization.Instance, "ritsulib.showcase.details.title",
                    "Detail Notes"),
                nestedListBinding,
                () => new(ModSettingsLocalization.Get("ritsulib.showcase.details.defaultLabel", "New note"), "value"),
                detail => ModSettingsText.Literal(detail.Label),
                detail => ModSettingsText.Literal(detail.Value),
                CreateShowcaseDetailEditor,
                ModSettingsText.I18N(ModSettingsLocalization.Instance, "ritsulib.showcase.details.add", "Add Detail"),
                ModSettingsText.I18N(ModSettingsLocalization.Instance, "ritsulib.showcase.details.description",
                    "Nested list editor example inside each item.")));

            return content;
        }

        private static Control CreateShowcaseDetailEditor(ModSettingsListItemContext<ShowcaseListDetail> itemContext)
        {
            var grid = new GridContainer
            {
                Columns = 2,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            grid.AddThemeConstantOverride("h_separation", 12);
            grid.AddThemeConstantOverride("v_separation", 10);

            var labelEdit = CreateListField(itemContext.Item.Label, value =>
                itemContext.Update(itemContext.Item with { Label = value }));
            labelEdit.CustomMinimumSize = new(180f, 42f);
            grid.AddChild(CreateListFieldGroup(
                ModSettingsLocalization.Get("ritsulib.showcase.details.field.label", "Label"), labelEdit));

            var valueEdit = CreateListField(itemContext.Item.Value, value =>
                itemContext.Update(itemContext.Item with { Value = value }));
            valueEdit.CustomMinimumSize = new(220f, 42f);
            grid.AddChild(CreateListFieldGroup(
                ModSettingsLocalization.Get("ritsulib.showcase.details.field.value", "Value"), valueEdit));
            return grid;
        }

        private static Control CreateListFieldGroup(string labelText, Control field, bool expand = true)
        {
            var group = new VBoxContainer
            {
                SizeFlagsHorizontal = expand ? Control.SizeFlags.ExpandFill : Control.SizeFlags.Fill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            group.AddThemeConstantOverride("separation", 6);

            var label = ModSettingsUiFactory.CreateInlineDescription(labelText);
            group.AddChild(label);

            if (!expand)
            {
                var fieldRow = new HBoxContainer
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                };
                fieldRow.AddChild(field);
                fieldRow.AddChild(new Control
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore,
                });
                group.AddChild(fieldRow);
            }
            else
            {
                group.AddChild(field);
            }

            return group;
        }

        private static LineEdit CreateListField(string initialValue, Action<string> commit)
        {
            var edit = new LineEdit
            {
                Text = initialValue,
                SelectAllOnFocus = true,
                Alignment = HorizontalAlignment.Left,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            edit.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
            edit.AddThemeFontSizeOverride("font_size", 18);
            edit.AddThemeColorOverride("font_color", new(1f, 0.964706f, 0.886275f));
            edit.AddThemeStyleboxOverride("normal", ModSettingsUiFactory.CreateInsetSurfaceStyle());
            edit.AddThemeStyleboxOverride("focus", ModSettingsUiFactory.CreateInsetSurfaceStyle());
            edit.TextSubmitted += value =>
            {
                commit(value);
                edit.ReleaseFocus();
            };
            edit.FocusExited += () => commit(edit.Text);
            ModSettingsFocusChrome.AttachControllerSelectionReticle(edit);
            return edit;
        }

        private sealed class DebugShowcaseState
        {
            private int _nextItemIndex = 4;
            public bool ToggleValue { get; set; } = true;
            public double SliderValue { get; set; } = 35d;
            public int IntSliderValue { get; set; } = 2;
            public string ChoiceValue { get; set; } = "balanced";
            public string ChoiceDropdownValue { get; set; } = "wide";
            public ShowcaseMode ModeValue { get; set; } = ShowcaseMode.Balanced;
            public string StringValue { get; set; } = "Single line";
            public string StringMultiValue { get; set; } = "First line\nSecond line";
            public int ActionCount { get; set; }

            public List<ShowcaseListItem> ListItems { get; set; } =
            [
                new("Sample A", 3, true, "alpha", [new("Author", "Ritsu"), new("Mode", "Default")]),
                new("Sample B", 1, false, "beta", [new("Author", "Debug")]),
                new("Sample C", 5, true, "gamma", [new("Mode", "Experimental"), new("Tier", "Rare")]),
            ];

            public ShowcaseListItem CreateListItem()
            {
                var index = _nextItemIndex++;
                return new($"Sample {index}", index, index % 2 == 0,
                    $"tag-{index}", [new("Author", $"User {index}")]);
            }
        }

        private sealed record ShowcaseListItem(
            string Name,
            int Weight,
            bool Enabled,
            string Tag,
            List<ShowcaseListDetail> Details);

        private sealed record ShowcaseListDetail(string Label, string Value);

        private enum ShowcaseMode
        {
            Compact,
            Balanced,
            Detailed,
        }

        private sealed class ShowcaseBinding<TValue>(IModSettingsValueBinding<TValue> inner, Action<TValue> afterWrite)
            : IModSettingsValueBinding<TValue>, ITransientModSettingsBinding
        {
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
            }

            public void Save()
            {
            }
        }
    }
}
