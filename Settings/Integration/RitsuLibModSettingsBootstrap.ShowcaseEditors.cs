using Godot;

namespace STS2RitsuLib.Settings
{
    internal static partial class RitsuLibModSettingsBootstrap
    {
        private static Control CreateShowcaseListItemEditor(
            ModSettingsListItemContext<ModSettingsDebugShowcaseListItem> itemContext)
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
                ModSettingsStructuredData.List(ModSettingsStructuredData.Json<ModSettingsDebugShowcaseListDetail>()));

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
                    "Nested structured list editor for each item.")));

            return content;
        }

        private static Control CreateShowcaseDetailEditor(
            ModSettingsListItemContext<ModSettingsDebugShowcaseListDetail> itemContext)
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
    }
}
