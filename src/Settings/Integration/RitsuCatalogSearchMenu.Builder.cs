using Godot;
using MegaCrit.Sts2.Core.Helpers;
using STS2RitsuLib.Ui.Catalog;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class RitsuCatalogSearchMenu
    {
        private readonly List<QueryCondition> _conditions = [new()];
        private bool _matchAnyCondition;

        private void AddExpressionEditor(VBoxContainer parent)
        {
            parent.AddChild(Paragraph(L("mode.advancedHint",
                "Only this expression determines the results. Empty expressions include every item. Normal search settings do not apply.")));
            var editor = new TextEdit
            {
                Text = _input?.Text ?? string.Empty,
                CustomMinimumSize = new(0f, 180f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                WrapMode = TextEdit.LineWrappingMode.Boundary,
            };
            ModSettingsUiControlTheming.ApplyEntryTextEditValueFieldTheme(editor,
                RitsuShellTheme.Current.Font.Body, RitsuShellTheme.Current.Metric.FontSize.ValueLabel);
            parent.AddChild(editor);
            var status = Paragraph(string.Empty);
            parent.AddChild(status);
            var apply = new ModSettingsMiniButton(L("editor.apply", "Apply expression"), () =>
            {
                _advancedQuery = editor.Text.ReplaceLineEndings(" ");
                _advancedInput.Text = _advancedQuery;
                _applyQuery(_advancedQuery);
                Close();
            }) { CustomMinimumSize = new(0f, 44f), SizeFlagsHorizontal = SizeFlags.ExpandFill };
            parent.AddChild(apply);
            editor.TextChanged += Validate;
            Validate();
            AddQueryBuilder(Section(parent, "builder", L("builder.title", "Compose conditions")), value =>
            {
                editor.Text = string.IsNullOrWhiteSpace(editor.Text)
                    ? value
                    : $"({editor.Text}) AND ({value})";
                Validate();
            });
            var help = Section(parent, "help", L("query.helpTitle", "Search syntax"));
            help.AddChild(Paragraph(L("query.help",
                "Fields: name: id: desc: keyword: attr:\nAND: match all; OR: match any; NOT or -: exclude.\nUse quotes for phrases and parentheses for groups. Every condition names its field.")));
            return;

            void Validate()
            {
                var valid = RitsuCatalogQuery.TryParse(editor.Text.ReplaceLineEndings(" "),
                    RitsuCatalogSearchFields.None, _availableFields,
                    out _, out var error, out var position);
                status.Visible = !valid;
                status.Text = valid ? string.Empty : FormatQueryError(error, position);
                apply.Disabled = !valid;
            }
        }

        private void AddQueryBuilder(VBoxContainer parent, Action<string> insert)
        {
            var hint = Paragraph(L("builder.hint",
                "Empty rows are ignored. Insert the filled conditions as an AND group into the expression above."));
            parent.AddChild(hint);
            var preview = ModSettingsUiControlTheming.CreateStyledLineEdit(string.Empty,
                L("builder.preview", "Expression preview"), 0f);
            preview.Editable = false;
            preview.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var apply = new ModSettingsMiniButton(L("builder.insert", "Insert condition group"),
                () => { insert(preview.Text); });
            var mode = new ModSettingsDropdownChoiceControl<bool>(
                [(false, L("builder.all", "Match all (AND)")), (true, L("builder.any", "Match any (OR)"))],
                _matchAnyCondition, value =>
                {
                    _matchAnyCondition = value;
                    RefreshPreview();
                });
            parent.AddChild(mode);
            var rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            rows.AddThemeConstantOverride("separation", 12);
            parent.AddChild(rows);
            var add = new ModSettingsMiniButton(L("builder.add", "Add condition"), () => { _conditions.Add(new()); });
            add.Pressed += RebuildRows;
            parent.AddChild(add);
            parent.AddChild(preview);
            parent.AddChild(apply);
            RebuildRows();
            return;

            void RebuildRows()
            {
                CloseNestedPopups();
                foreach (var child in rows.GetChildren())
                {
                    rows.RemoveChild(child);
                    child.QueueFreeSafely();
                }

                foreach (var condition in _conditions)
                {
                    var controls = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                    controls.AddThemeConstantOverride("separation", 8);
                    rows.AddChild(controls);
                    var fields = new[]
                        {
                            RitsuCatalogSearchFields.Name, RitsuCatalogSearchFields.Id,
                            RitsuCatalogSearchFields.Description, RitsuCatalogSearchFields.Keywords,
                            RitsuCatalogSearchFields.Metadata,
                        }.Where(field => (_availableFields & field) != 0)
                        .Select(field => (Value: field, Label: FieldLabel(field))).ToArray();
                    if (fields.All(option => option.Value != condition.Field))
                        condition.Field = RitsuCatalogSearchFields.Name;
                    controls.AddChild(new ModSettingsDropdownChoiceControl<RitsuCatalogSearchFields>(fields,
                        condition.Field, value =>
                        {
                            condition.Field = value;
                            RefreshPreview();
                        }));
                    var exclude = ModSettingsUiControlTheming.CreateCompactSettingsToggleButton(
                        L("builder.exclude", "Exclude"), condition.Exclude);
                    exclude.CustomMinimumSize = new(80f, 44f);
                    exclude.Toggled += value =>
                    {
                        condition.Exclude = value;
                        RefreshPreview();
                    };
                    controls.AddChild(exclude);
                    var remove = new ModSettingsMiniButton(L("builder.remove", "Remove"), () =>
                    {
                        _conditions.Remove(condition);
                        RebuildRows();
                    })
                    {
                        CustomMinimumSize = new(80f, 44f),
                        SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                    };
                    var input = ModSettingsUiControlTheming.CreateStyledLineEdit(condition.Text,
                        L("builder.value", "Search text"), 0f);
                    input.MaxLength = 256;
                    input.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                    input.TextChanged += value =>
                    {
                        condition.Text = value;
                        RefreshPreview();
                    };
                    controls.AddChild(input);
                    controls.AddChild(remove);
                }

                add.Disabled = _conditions.Count >= 16;
                RefreshPreview();
            }

            void RefreshPreview()
            {
                preview.Text = string.Join(_matchAnyCondition ? " OR " : " AND ",
                    _conditions.Where(static condition => !string.IsNullOrWhiteSpace(condition.Text))
                        .Select(static condition => (condition.Exclude ? "NOT " : string.Empty) +
                                                    FieldPrefix(condition.Field) +
                                                    RitsuCatalogQuery.Quote(condition.Text)));
                apply.Disabled = preview.Text.Length is 0 or > RitsuCatalogQuery.MaximumLength;
                preview.TooltipText = preview.Text.Length > RitsuCatalogQuery.MaximumLength
                    ? L("query.tooLong", "Use at most 2048 characters.")
                    : preview.Text;
            }
        }

        private static string FieldPrefix(RitsuCatalogSearchFields field)
        {
            return field switch
            {
                RitsuCatalogSearchFields.Name => "name:",
                RitsuCatalogSearchFields.Id => "id:",
                RitsuCatalogSearchFields.Description => "desc:",
                RitsuCatalogSearchFields.Keywords => "keyword:",
                RitsuCatalogSearchFields.Metadata => "attr:",
                _ => string.Empty,
            };
        }

        private sealed class QueryCondition
        {
            internal RitsuCatalogSearchFields Field { get; set; } = RitsuCatalogSearchFields.Name;
            internal bool Exclude { get; set; }
            internal string Text { get; set; } = string.Empty;
        }
    }
}
