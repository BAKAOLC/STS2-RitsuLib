using System.Text;
using STS2RitsuLib.Ui.Toast;

namespace STS2RitsuLib.Settings
{
    internal static partial class RitsuLibModSettingsBootstrap
    {
        private static void RegisterDebugShowcasePage(RitsuLibModSettingsUiBindings ui)
        {
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .AsChildOf(Const.ModId)
                    .WithTitle(T("ritsulib.showcase.title", "Control Preview"))
                    .WithDescription(T("ritsulib.showcase.description",
                        "Demonstrates supported controls and dynamic descriptions without persisting values."))
                    .AddSection("overview", section => section
                        .WithTitle(T("ritsulib.showcase.overview.title", "Overview"))
                        .AddHeader(
                            "showcase_header",
                            T("ritsulib.showcase.header", "Preview-only controls"),
                            T("ritsulib.showcase.header.description",
                                "Reference controls backed by preview-only bindings."))
                        .AddParagraph(
                            "showcase_paragraph",
                            ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.summary",
                                            "Toggle: {0} | Double: {1:0.##} | Int: {2} | Choice: {3} | Dropdown: {4} | Mode: {5} | Action Count: {6}"),
                                        ui.DebugShowcase.ToggleValue,
                                        ui.DebugShowcase.SliderValue,
                                        ui.DebugShowcase.IntSliderValue,
                                        ui.DebugShowcase.ChoiceValue,
                                        ui.DebugShowcase.ChoiceDropdownValue,
                                        ui.DebugShowcase.ModeValue,
                                        ui.DebugShowcase.ActionCount),
                                ui.PreviewToggle,
                                ui.PreviewSlider,
                                ui.PreviewIntSlider,
                                ui.PreviewChoice,
                                ui.PreviewChoiceDropdown,
                                ui.PreviewMode,
                                ui.PreviewActionCount))
                        .AddImage(
                            "showcase_image",
                            T("ritsulib.showcase.image.label", "Reference image"),
                            () => ModSettingsUiResources.SettingsButtonTexture,
                            120f,
                            ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.image.description",
                                            "Image previews can participate in dynamic descriptions. Current mode: {0}"),
                                        ui.DebugShowcase.ModeValue),
                                ui.PreviewMode))
                        .AddSubpage(
                            "showcase_spine_preview",
                            T("ritsulib.runtimeReflection.spine.page.title", "Spine preview (sample)"),
                            "runtime-reflection-spine-example",
                            T("button.open", "Open"),
                            T("ritsulib.runtimeReflection.spine.page.description",
                                "Try bindings and a simple spine preview.")))
                    .AddSection("inputs", section => section
                        .WithTitle(T("ritsulib.showcase.inputs.title", "Inputs"))
                        .WithDescription(T("ritsulib.showcase.inputs.description",
                            "Editing these controls updates the preview state only."))
                        .Collapsible()
                        .AddToggle(
                            "preview_toggle",
                            T("ritsulib.showcase.toggle.label", "Preview toggle"),
                            new ModSettingsDebugShowcaseBinding<bool>(ui.PreviewToggle,
                                value => ui.DebugShowcase.ToggleValue = value),
                            ModSettingsText.Dynamic(() =>
                                    string.Format(L("ritsulib.showcase.toggle.description", "Current value: {0}"),
                                        ui.DebugShowcase.ToggleValue),
                                ui.PreviewToggle))
                        .AddSlider(
                            "preview_slider",
                            T("ritsulib.showcase.slider.label", "Preview slider"),
                            new ModSettingsDebugShowcaseBinding<double>(ui.PreviewSlider,
                                value => ui.DebugShowcase.SliderValue = value),
                            0d,
                            100d,
                            0.25d,
                            value => value.ToString("0.##"),
                            ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.slider.description", "Current double value: {0:0.##}"),
                                        ui.DebugShowcase.SliderValue),
                                ui.PreviewSlider))
                        .AddIntSlider(
                            "preview_int_slider",
                            T("ritsulib.showcase.intSlider.label", "Preview integer slider"),
                            new ModSettingsDebugShowcaseBinding<int>(ui.PreviewIntSlider,
                                value => ui.DebugShowcase.IntSliderValue = value),
                            0,
                            5,
                            1,
                            value => value.ToString(),
                            ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.intSlider.description", "Current integer value: {0}"),
                                        ui.DebugShowcase.IntSliderValue),
                                ui.PreviewIntSlider))
                        .AddChoice(
                            "preview_choice",
                            T("ritsulib.showcase.choice.label", "Preview choice"),
                            new ModSettingsDebugShowcaseBinding<string>(ui.PreviewChoice,
                                value => ui.DebugShowcase.ChoiceValue = value),
                            [
                                new("compact", T("ritsulib.showcase.choice.compact", "Compact")),
                                new("balanced", T("ritsulib.showcase.choice.balanced", "Balanced")),
                                new("wide", T("ritsulib.showcase.choice.wide", "Wide")),
                            ],
                            ModSettingsText.Dynamic(() =>
                                    string.Format(L("ritsulib.showcase.choice.description", "Current choice: {0}"),
                                        ui.DebugShowcase.ChoiceValue),
                                ui.PreviewChoice))
                        .AddChoice(
                            "preview_choice_dropdown",
                            T("ritsulib.showcase.choiceDropdown.label", "Preview choice (dropdown)"),
                            new ModSettingsDebugShowcaseBinding<string>(ui.PreviewChoiceDropdown,
                                value => ui.DebugShowcase.ChoiceDropdownValue = value),
                            [
                                new("compact", T("ritsulib.showcase.choice.compact", "Compact")),
                                new("balanced", T("ritsulib.showcase.choice.balanced", "Balanced")),
                                new("wide", T("ritsulib.showcase.choice.wide", "Wide")),
                            ],
                            ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.choiceDropdown.description",
                                            "Same options as the stepper control, rendered as a dropdown. Current: {0}"),
                                        ui.DebugShowcase.ChoiceDropdownValue),
                                ui.PreviewChoiceDropdown),
                            ModSettingsChoicePresentation.Dropdown)
                        .AddEnumChoice(
                            "preview_mode",
                            T("ritsulib.showcase.mode.label", "Preview enum choice"),
                            new ModSettingsDebugShowcaseBinding<ModSettingsDebugShowcaseMode>(ui.PreviewMode,
                                value => ui.DebugShowcase.ModeValue = value),
                            mode => T($"ritsulib.showcase.mode.{mode}", mode.ToString()),
                            ModSettingsText.Dynamic(() =>
                                    string.Format(L("ritsulib.showcase.mode.description", "Current mode: {0}"),
                                        ui.DebugShowcase.ModeValue),
                                ui.PreviewMode))
                        .AddString(
                            "preview_string",
                            T("ritsulib.showcase.string.label", "Preview string field"),
                            new ModSettingsDebugShowcaseBinding<string>(ui.PreviewString,
                                value => ui.DebugShowcase.StringValue = value),
                            T("ritsulib.showcase.string.placeholder", "Single-line string binding"),
                            null,
                            ModSettingsText.Dynamic(() =>
                                    string.Format(L("ritsulib.showcase.string.description", "Current text: {0}"),
                                        ui.DebugShowcase.StringValue),
                                ui.PreviewString))
                        .AddMultilineString(
                            "preview_string_multi",
                            T("ritsulib.showcase.stringMulti.label", "Preview multiline field"),
                            new ModSettingsDebugShowcaseBinding<string>(ui.PreviewStringMulti,
                                value => ui.DebugShowcase.StringMultiValue = value),
                            T("ritsulib.showcase.stringMulti.placeholder",
                                "Multiple lines — Enter inserts a new line."),
                            null,
                            ModSettingsText.Dynamic(() =>
                                {
                                    var t = ui.DebugShowcase.StringMultiValue ?? string.Empty;
                                    var lineCount = string.IsNullOrEmpty(t) ? 0 : t.Split('\n').Length;
                                    return string.Format(
                                        L("ritsulib.showcase.stringMulti.description",
                                            "{0} characters, {1} lines."),
                                        t.Length,
                                        lineCount);
                                },
                                ui.PreviewStringMulti))
                        .AddKeyBinding(
                            "preview_hotkey",
                            T("ritsulib.showcase.hotkey.label", "Preview key binding"),
                            new ModSettingsDebugShowcaseBinding<string>(ui.PreviewHotkey, _ => { }),
                            true,
                            true,
                            false,
                            T("ritsulib.showcase.hotkey.description",
                                "Single-binding key capture preview."))
                        .AddKeyBinding(
                            "preview_hotkey_multi",
                            T("ritsulib.showcase.hotkeyMulti.label", "Preview multi key binding"),
                            new ModSettingsDebugShowcaseBinding<List<string>>(ui.PreviewHotkeyMulti, _ => { }),
                            true,
                            true,
                            true,
                            false,
                            T("ritsulib.showcase.hotkeyMulti.description",
                                "Explicit opt-in native multi-binding key capture preview.")))
                    .AddSection("actions", section => section
                        .WithTitle(T("ritsulib.showcase.actions.title", "Commands"))
                        .WithDescription(T("ritsulib.showcase.actions.description",
                            "Buttons can mutate preview state and refresh adjacent descriptions."))
                        .Collapsible()
                        .AddButton(
                            "preview_action",
                            T("ritsulib.showcase.action.label", "Preview command button"),
                            T("ritsulib.showcase.action.button", "Trigger"),
                            () =>
                            {
                                ui.DebugShowcase.ActionCount++;
                                ui.PreviewActionCount.Write(ui.DebugShowcase.ActionCount);
                            },
                            ModSettingsButtonTone.Accent,
                            ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.action.description", "Command invoked {0} times."),
                                        ui.DebugShowcase.ActionCount),
                                ui.PreviewActionCount))
                        .AddButton(
                            "preview_toast",
                            T("ritsulib.showcase.toast.label", "Toast preview"),
                            T("ritsulib.showcase.toast.button", "Show toast"),
                            () =>
                            {
                                ui.DebugShowcase.ToastCount++;
                                var includeImage = Random.Shared.Next(0, 2) == 0;
                                var level = Random.Shared.Next(0, 3) switch
                                {
                                    1 => RitsuToastLevel.Warning,
                                    2 => RitsuToastLevel.Error,
                                    _ => RitsuToastLevel.Info,
                                };
                                RitsuToastService.Show(new(
                                    BuildRandomToastBody(ui.DebugShowcase.ToastCount),
                                    L("ritsulib.showcase.toast.title", "Toast preview"),
                                    includeImage ? ModSettingsUiResources.SettingsButtonTexture : null,
                                    level));
                            },
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.showcase.toast.description",
                                "Shows a themed toast with randomized text length and optional image."))
                        .AddButton(
                            "preview_reset",
                            T("ritsulib.showcase.reset.label", "Reset preview bindings"),
                            T("ritsulib.showcase.reset.button", "Reset"),
                            host =>
                            {
                                ui.DebugShowcase.ToggleValue = true;
                                ui.DebugShowcase.SliderValue = 35d;
                                ui.DebugShowcase.IntSliderValue = 2;
                                ui.DebugShowcase.ChoiceValue = "balanced";
                                ui.DebugShowcase.ChoiceDropdownValue = "wide";
                                ui.DebugShowcase.ModeValue = ModSettingsDebugShowcaseMode.Balanced;
                                ui.DebugShowcase.ActionCount = 0;
                                ui.DebugShowcase.ToastCount = 0;
                                ui.DebugShowcase.StringValue = "Single line";
                                ui.DebugShowcase.StringMultiValue = "First line\nSecond line";
                                ui.DebugShowcase.ListItems =
                                [
                                    new("Sample A", 3, true, "alpha", [new("Author", "Ritsu"), new("Mode", "Default")]),
                                    new("Sample B", 1, false, "beta", [new("Author", "Debug")]),
                                    new("Sample C", 5, true, "gamma",
                                        [new("Mode", "Experimental"), new("Tier", "Rare")]),
                                ];
                                ui.PreviewToggle.Write(ui.DebugShowcase.ToggleValue);
                                ui.PreviewSlider.Write(ui.DebugShowcase.SliderValue);
                                ui.PreviewIntSlider.Write(ui.DebugShowcase.IntSliderValue);
                                ui.PreviewChoice.Write(ui.DebugShowcase.ChoiceValue);
                                ui.PreviewChoiceDropdown.Write(ui.DebugShowcase.ChoiceDropdownValue);
                                ui.PreviewMode.Write(ui.DebugShowcase.ModeValue);
                                ui.PreviewString.Write(ui.DebugShowcase.StringValue);
                                ui.PreviewStringMulti.Write(ui.DebugShowcase.StringMultiValue);
                                ui.PreviewActionCount.Write(ui.DebugShowcase.ActionCount);
                                ui.PreviewList.Write(ui.DebugShowcase.ListItems);
                                host.RequestRefreshAfterDataModelBatchChange();
                            },
                            ModSettingsButtonTone.Danger,
                            T("ritsulib.showcase.reset.description",
                                "Restore all preview bindings to default values without persisting data."))
                        .AddParagraph(
                            "showcase_footer",
                            T("ritsulib.showcase.footer",
                                "Use this page as a reference when implementing settings pages.")))
                    .AddSection("host_surface_reference", section => section
                        .WithTitle(T("ritsulib.section.hostSurfaceReference.title", "Where settings appear"))
                        .WithDescription(T("ritsulib.section.hostSurfaceReference.description",
                            "Examples: main menu, run pause, or mid-combat pause."))
                        .Collapsible()
                        .AddParagraph(
                            "host_surface_intro",
                            T("ritsulib.hostSurface.intro",
                                "Pages and sections can be limited to certain menus, or read-only in some of them."))
                        .AddParagraph(
                            "host_surface_tiers",
                            T("ritsulib.hostSurface.tiers",
                                "Typical split: global aids everywhere; sensitive values read-only in combat; debug-only rows only in combat pause."))
                        .AddToggle(
                            "host_surface_combat_readonly_demo",
                            T("ritsulib.hostSurface.demoToggle.label", "Read-only in combat pause (demo)"),
                            ui.HostSurfaceCombatReadOnlyDemo,
                            T("ritsulib.hostSurface.demoToggle.description",
                                "Editable on the main menu and run pause; locked while paused in a fight."))
                        .WithReadOnlyOnHostSurfaces(ModSettingsHostSurface.CombatPause))
                    .AddSection("host_surface_combat_only_demo", section => section
                        .WithTitle(T("ritsulib.section.hostSurfaceCombatOnly.title", "Combat pause only (demo)"))
                        .WithVisibleOnHostSurfaces(ModSettingsHostSurface.CombatPause)
                        .AddParagraph(
                            "host_surface_combat_only_body",
                            T("ritsulib.hostSurface.combatOnly.body",
                                "Shown only when mod settings are opened from a mid-combat pause.")))
                    .AddSection("list", section => section
                        .WithTitle(T("ritsulib.showcase.list.title", "Structured List"))
                        .WithDescription(T("ritsulib.showcase.list.description",
                            "Structured collections can be edited, reordered, added, and removed inside the settings UI."))
                        .Collapsible()
                        .AddList(
                            "preview_list",
                            T("ritsulib.showcase.list.label", "Preview structured collection"),
                            new ModSettingsDebugShowcaseBinding<List<ModSettingsDebugShowcaseListItem>>(ui.PreviewList,
                                value => ui.DebugShowcase.ListItems = value.ToList()),
                            ui.DebugShowcase.CreateListItem,
                            item => ModSettingsText.Literal($"{item.Name} ({item.Weight})"),
                            item => ModSettingsText.Literal(item.Enabled
                                ? $"Enabled item - tag: {item.Tag} - notes: {item.Details.Count}"
                                : $"Disabled item - tag: {item.Tag} - notes: {item.Details.Count}"),
                            CreateShowcaseListItemEditor,
                            ModSettingsStructuredData.Json<ModSettingsDebugShowcaseListItem>(),
                            T("ritsulib.showcase.list.add", "Add Item"),
                            ModSettingsText.Dynamic(() =>
                                    string.Format(L("ritsulib.showcase.list.summary", "Current item count: {0}"),
                                        ui.DebugShowcase.ListItems.Count),
                                ui.PreviewList),
                            true,
                            false,
                            null)),
                "debug-showcase");
        }

        private static string BuildRandomToastBody(int actionCount)
        {
            var summary = string.Format(
                L("ritsulib.showcase.toast.body",
                    "Queue and layout test entry from the control preview page. Count: {0}."),
                actionCount);
            var repeat = Random.Shared.Next(0, 5);
            if (repeat == 0)
                return summary;
            var chunk = L("ritsulib.showcase.toast.bodyChunk",
                "Overflow probe sentence for wrapping and height checks.");
            var builder = new StringBuilder(summary.Length + (chunk.Length + 1) * repeat);
            builder.Append(summary);
            for (var i = 0; i < repeat; i++)
            {
                builder.Append(' ');
                builder.Append(chunk);
            }

            return builder.ToString();
        }
    }
}
