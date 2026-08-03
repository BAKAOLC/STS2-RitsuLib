using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Models;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class RitsuDebugStatePresetEditor
    {
        private static PanelContainer CreatePane(float width, bool expand = false)
        {
            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = expand ? SizeFlags.ExpandFill : SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                CustomMinimumSize = new(width, 0f),
            };
            panel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListShellStyle());
            return panel;
        }

        private static VBoxContainer CreatePaneBody(PanelContainer panel)
        {
            var margin = CreateMargin(panel, 10, 10);
            var body = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            body.AddThemeConstantOverride("separation", 8);
            margin.AddChild(body);
            return body;
        }

        private static MarginContainer CreateMargin(Control parent, int horizontal, int vertical)
        {
            var margin = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            margin.AddThemeConstantOverride("margin_left", horizontal);
            margin.AddThemeConstantOverride("margin_right", horizontal);
            margin.AddThemeConstantOverride("margin_top", vertical);
            margin.AddThemeConstantOverride("margin_bottom", vertical);
            parent.AddChild(margin);
            return margin;
        }

        private static ScrollContainer CreateScroll()
        {
            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            };
            ModSettingsUiControlTheming.ApplySettingsScrollContainerThemeForDropdownList(scroll);
            return scroll;
        }

        private static Label SectionTitle(string text)
        {
            var label = new Label
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
            };
            label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            label.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.SettingLineTitle);
            label.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            return label;
        }

        private static Label SecondaryLabel(string text)
        {
            var label = new Label
            {
                Text = text,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            label.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
            label.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            return label;
        }

        private static Label Hint(string text)
        {
            var label = SecondaryLabel(text);
            label.HorizontalAlignment = HorizontalAlignment.Left;
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            return label;
        }

        private static ColorRect Divider()
        {
            return new()
            {
                Color = RitsuShellTheme.Current.Color.Divider,
                CustomMinimumSize = new(0f, 1f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
        }

        private static Button CompactButton(
            string text,
            ModSettingsButtonTone tone,
            Action action,
            float width = 0f)
        {
            var button = new ModSettingsTextButton(text, tone, action)
            {
                CustomMinimumSize = new(width, 32f),
                SizeFlagsHorizontal = width > 0f ? SizeFlags.ShrinkBegin : SizeFlags.ExpandFill,
            };
            button.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
            return button;
        }

        private static void ApplySelectionStyle(Button button, bool selected)
        {
            var normal = RitsuShellChromeStyles.CreateListItemCardStyle(selected);
            var active = RitsuShellChromeStyles.CreateListItemCardStyle(true);
            button.AddThemeStyleboxOverride("normal", normal);
            button.AddThemeStyleboxOverride("hover", active);
            button.AddThemeStyleboxOverride("pressed", active);
            button.AddThemeStyleboxOverride("focus", active);
        }

        private static Control IntegerField(
            string label,
            int value,
            int minimum,
            int maximum,
            Action<int> changed)
        {
            var committed = value;
            var edit = IntegerEdit(committed);
            edit.TextSubmitted += _ => Commit();
            edit.FocusExited += Commit;
            return Field(label, edit);

            void Commit()
            {
                if (int.TryParse(edit.Text, out var parsed) && parsed >= minimum && parsed <= maximum)
                {
                    committed = parsed;
                    changed(parsed);
                    return;
                }

                edit.Text = committed.ToString();
            }
        }

        private static Control OptionalIntegerField(
            string label,
            int? value,
            int minimum,
            int maximum,
            Action<int?> changed)
        {
            var committed = value ?? minimum;
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new(0f, 38f),
            };
            row.AddThemeConstantOverride("separation", 8);
            var enabled = ModSettingsUiControlTheming.CreateCompactSettingsToggleButton(
                label,
                value.HasValue);
            enabled.CustomMinimumSize = new(150f, 34f);
            row.AddChild(enabled);
            var edit = IntegerEdit(committed);
            edit.Editable = value.HasValue;
            edit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(edit);
            enabled.Toggled += on =>
            {
                edit.Editable = on;
                if (!on)
                {
                    changed(null);
                }
                else
                {
                    Commit();
                    edit.GrabFocus();
                }
            };
            edit.TextSubmitted += _ => Commit();
            edit.FocusExited += Commit;
            return row;

            void Commit()
            {
                if (!enabled.ButtonPressed)
                    return;
                if (int.TryParse(edit.Text, out var parsed) && parsed >= minimum && parsed <= maximum)
                {
                    committed = parsed;
                    changed(parsed);
                    return;
                }

                edit.Text = committed.ToString();
            }
        }

        private static Control NullableBoolField(string label, bool? value, Action<bool?> changed)
        {
            return Field(
                label,
                new ModSettingsDropdownChoiceControl<int>(
                    [
                        (-1, L("ritsulib.debugTools.statePresets.keepDefault", "Keep card default")),
                        (1, L("ritsulib.debugTools.statePresets.enabled", "Enabled")),
                        (0, L("ritsulib.debugTools.statePresets.disabled", "Disabled")),
                    ],
                    value switch
                    {
                        true => 1,
                        false => 0,
                        null => -1,
                    },
                    selected => changed(selected < 0 ? null : selected > 0))
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    CustomMinimumSize = new(220f, 34f),
                });
        }

        private static LineEdit IntegerEdit(int value)
        {
            var edit = new LineEdit
            {
                Text = value.ToString(),
                CustomMinimumSize = new(100f, 34f),
            };
            ModSettingsUiControlTheming.ApplyEntryLineEditValueFieldTheme(
                edit,
                RitsuShellTheme.Current.Font.Body,
                RitsuShellTheme.Current.Metric.FontSize.ValueLabel);
            return edit;
        }

        private static HBoxContainer Field(string label, Control editor)
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new(0f, 38f),
            };
            row.AddThemeConstantOverride("separation", 8);
            var text = SecondaryLabel(label);
            text.CustomMinimumSize = new(150f, 0f);
            text.VerticalAlignment = VerticalAlignment.Center;
            row.AddChild(text);
            editor.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(editor);
            return row;
        }

        private void CacheModelTitles<TModel>(IEnumerable<TModel> models) where TModel : AbstractModel
        {
            foreach (var model in models)
                _modelTitles[model.Id.ToString()] = SafeTitle(model);
        }

        private string ModelLabel(string modelId)
        {
            return _modelTitles.GetValueOrDefault(modelId, modelId);
        }

        private static string SafeTitle(AbstractModel model)
        {
            try
            {
                if (!model.TryResolveTitle(out var title))
                    return model.Id.Entry;
                var formatted = title.GetFormattedText()?.Trim();
                return string.IsNullOrWhiteSpace(formatted) ? model.Id.Entry : formatted;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                return model.Id.Entry;
            }
        }

        private static Texture2D? SafeTexture(Func<Texture2D?> factory)
        {
            try
            {
                var texture = factory();
                return texture != null && IsInstanceValid(texture) ? texture : null;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                return null;
            }
        }

        private static string PileLabel(PileType pileType)
        {
            return L($"ritsulib.debugTools.enum.PileType.{pileType}", pileType.ToString());
        }

        private static string PresetSummary(RitsuDebugStatePreset preset)
        {
            var parts = new List<string>();
            if (preset.CardPiles.Count > 0)
                parts.Add(string.Format(
                    L("ritsulib.debugTools.statePresets.summaryCards", "{0} cards"),
                    preset.CardPiles.Sum(static pile => pile.Cards.Sum(static card => card.Count))));
            if (preset.Relics != null)
                parts.Add(string.Format(
                    L("ritsulib.debugTools.statePresets.summaryRelics", "{0} relics"),
                    preset.Relics.ModelIds.Count));
            if (preset.Potions != null)
                parts.Add(string.Format(
                    L("ritsulib.debugTools.statePresets.summaryPotions", "{0} potions"),
                    preset.Potions.Items.Count));
            if (preset.Powers != null)
                parts.Add(string.Format(
                    L("ritsulib.debugTools.statePresets.summaryPowers", "{0} powers"),
                    preset.Powers.Items.Count));
            if (preset.Player != null)
                parts.Add(L("ritsulib.debugTools.statePresets.summaryPlayer", "player values"));
            return parts.Count == 0 ? "—" : string.Join(" · ", parts);
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }

        private static void ClearChildren(Node node)
        {
            foreach (var child in node.GetChildren())
            {
                node.RemoveChild(child);
                child.QueueFree();
            }
        }
    }
}
