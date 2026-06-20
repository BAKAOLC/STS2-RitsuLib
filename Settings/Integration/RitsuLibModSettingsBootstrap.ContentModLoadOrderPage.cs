using Godot;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal static partial class RitsuLibModSettingsBootstrap
    {
        private static event Action? ContentModLoadOrderPreviewRefreshRequested;

        private static void RegisterContentModLoadOrderPage()
        {
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .AsChildOf(Const.ModId)
                    .WithSidebarVisibleOnlyWhenActive()
                    .WithSortOrder(-875)
                    .WithTitle(T("ritsulib.page.contentModLoadOrder.title", "Content mod load order"))
                    .WithDescription(T("ritsulib.page.contentModLoadOrder.description",
                        "Save, copy, and apply deterministic ordering for relevant content mods, framework libraries, and installed dependencies."))
                    .AddSection("content_mod_load_order_actions", section => section
                        .WithTitle(T("ritsulib.section.contentModLoadOrder.actions.title", "Actions"))
                        .WithDescription(T("ritsulib.section.contentModLoadOrder.actions.description",
                            "These actions rewrite the saved mod list only. Restart the game before starting a run or joining multiplayer."))
                        .AddButton(
                            "content_mod_load_order_sort",
                            T("ritsulib.contentModLoadOrder.sort.label", "Deterministic sort"),
                            T("ritsulib.contentModLoadOrder.sort.button", "Sort relevant mods"),
                            host =>
                            {
                                ContentModLoadOrderCoordinator.SortDeterministically();
                                RequestContentModLoadOrderPreviewRefresh();
                            },
                            ModSettingsButtonTone.Accent,
                            T("ritsulib.contentModLoadOrder.sort.description",
                                "Orders relevant mods by dependency topology, then by mod id for ties. Other mods keep their relative slots."))
                        .AddButton(
                            "content_mod_load_order_copy",
                            T("ritsulib.contentModLoadOrder.copy.label", "Copy current order"),
                            T("ritsulib.contentModLoadOrder.copy.button", "Copy id list"),
                            ContentModLoadOrderCoordinator.CopyCurrentOrder,
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.contentModLoadOrder.copy.description",
                                "Copies the current relevant mod id list as one id per line."))
                        .AddButton(
                            "content_mod_load_order_paste",
                            T("ritsulib.contentModLoadOrder.paste.label", "Apply copied order"),
                            T("ritsulib.contentModLoadOrder.paste.button", "Apply from clipboard"),
                            host =>
                            {
                                ContentModLoadOrderCoordinator.ApplyClipboardOrder();
                                RequestContentModLoadOrderPreviewRefresh();
                            },
                            ModSettingsButtonTone.Accent,
                            T("ritsulib.contentModLoadOrder.paste.description",
                                "Applies matching installed ids from the clipboard. Missing and unlisted mods are ignored.")))
                    .AddSection("content_mod_load_order_preview", section => section
                        .WithTitle(T("ritsulib.section.contentModLoadOrder.preview.title", "Preview"))
                        .WithDescription(T("ritsulib.section.contentModLoadOrder.preview.description",
                            "Shows content-affecting mods, framework libraries, and installed dependencies. Other mods keep their relative slots."))
                        .AddCustom(
                            "content_mod_load_order_compare",
                            T("ritsulib.contentModLoadOrder.compare.label", "Order comparison"),
                            _ => CreateContentModLoadOrderPreview())),
                "content-mod-load-order");
        }

        private static void RequestContentModLoadOrderPreviewRefresh()
        {
            ContentModLoadOrderPreviewRefreshRequested?.Invoke();
        }

        private static Control CreateContentModLoadOrderPreview()
        {
            return new ContentModLoadOrderPreviewControl();
        }

        private static Control CreateContentModLoadOrderPreviewBody()
        {
            var preview = ContentModLoadOrderCoordinator.BuildPreview();
            var root = new MarginContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                CustomMinimumSize = new(0f, 520f),
            };
            root.AddThemeConstantOverride("margin_left", 4);
            root.AddThemeConstantOverride("margin_right", 4);
            root.AddThemeConstantOverride("margin_top", 4);
            root.AddThemeConstantOverride("margin_bottom", 4);

            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListShellStyle());
            root.AddChild(panel);

            var columns = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            columns.AddThemeConstantOverride("separation", 12);
            panel.AddChild(columns);

            columns.AddChild(CreateOrderColumn(
                L("ritsulib.contentModLoadOrder.current.label", "Current relevant order"),
                preview.Current,
                entry => entry.RelatedPosition == entry.Position
                    ? L("ritsulib.contentModLoadOrder.preview.same", "same")
                    : string.Format(L("ritsulib.contentModLoadOrder.preview.movesTo", "to #{0:00}"),
                        entry.RelatedPosition)));
            columns.AddChild(CreateOrderColumn(
                L("ritsulib.contentModLoadOrder.target.label", "After deterministic sort"),
                preview.Target,
                entry => entry.RelatedPosition == entry.Position
                    ? L("ritsulib.contentModLoadOrder.preview.same", "same")
                    : string.Format(L("ritsulib.contentModLoadOrder.preview.movesFrom", "from #{0:00}"),
                        entry.RelatedPosition)));
            return root;
        }

        private static Control CreateOrderColumn(
            string title,
            IReadOnlyList<ContentModLoadOrderPreviewEntry> entries,
            Func<ContentModLoadOrderPreviewEntry, string> relationText)
        {
            var column = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            column.AddThemeConstantOverride("separation", 8);
            column.AddChild(CreatePreviewLabel(title, RitsuShellTheme.Current.Text.RichTitle, 18, true));

            if (entries.Count == 0)
            {
                var empty = new PanelContainer
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                };
                empty.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateInsetSurfaceStyle());
                empty.CustomMinimumSize = new(0f, 56f);
                empty.AddChild(CreatePreviewLabel(
                    L("ritsulib.contentModLoadOrder.preview.empty", "No relevant mods were found."),
                    RitsuShellTheme.Current.Text.RichSecondary,
                    15));
                column.AddChild(empty);
                return column;
            }

            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new(0f, 450f),
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                MouseFilter = Control.MouseFilterEnum.Pass,
            };
            ModSettingsUiControlTheming.ApplySettingsScrollContainerTheme(scroll);
            var body = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            body.AddThemeConstantOverride("separation", 6);
            scroll.AddChild(body);

            foreach (var entry in entries)
                body.AddChild(CreateOrderRow(entry, relationText(entry)));

            column.AddChild(scroll);
            return column;
        }

        private static Control CreateOrderRow(ContentModLoadOrderPreviewEntry entry, string relation)
        {
            var moved = !string.Equals(relation, L("ritsulib.contentModLoadOrder.preview.same", "same"),
                StringComparison.Ordinal);
            var rowPanel = new PanelContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new(0f, 38f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            rowPanel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListItemCardStyle(moved));

            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 8);
            rowPanel.AddChild(row);

            row.AddChild(CreateDependencyRail(entry.IsDependency));
            row.AddChild(CreateSourceIconSlot(entry.Source));
            row.AddChild(CreateFixedLabel($"#{entry.Position:00}", 42, RitsuShellTheme.Current.Text.Number, 15));

            var textRow = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            textRow.AddThemeConstantOverride("separation", 8);
            textRow.AddChild(CreatePreviewLabel(
                $"{entry.DisplayName} ({entry.Id})",
                RitsuShellTheme.Current.Text.RichBody,
                15,
                false,
                26f));
            row.AddChild(textRow);

            if (entry.IsDependency)
                row.AddChild(CreateDependencyPill());
            row.AddChild(CreateRelationPill(relation, moved));
            return rowPanel;
        }

        private static ColorRect CreateDependencyRail(bool dependency)
        {
            return new()
            {
                Color = dependency ? RitsuShellTheme.Current.Text.Number : RitsuShellTheme.Current.Color.Transparent,
                CustomMinimumSize = new(4f, 24f),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
        }

        private static Label CreateDependencyPill()
        {
            var label = CreateFixedLabel(
                L("ritsulib.contentModLoadOrder.preview.dependency", "前置"),
                86,
                RitsuShellTheme.Current.Text.Number,
                13);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            return label;
        }

        private static Control CreateSourceIconSlot(string source)
        {
            var slot = new CenterContainer
            {
                CustomMinimumSize = new(24f, 24f),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };

            if (TryParseModSource(source, out var modSource))
                slot.AddChild(new TextureRect
                {
                    Texture = NModMenuRow.GetPlatformIcon(modSource),
                    CustomMinimumSize = new(22f, 22f),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                });

            return slot;
        }

        private static bool TryParseModSource(string source, out ModSource modSource)
        {
            return Enum.TryParse(source, out modSource) && Enum.IsDefined(modSource);
        }

        private static Label CreateRelationPill(string text, bool highlighted)
        {
            var label = CreateFixedLabel(text, 78,
                highlighted ? RitsuShellTheme.Current.Text.HoverHighlight : RitsuShellTheme.Current.Text.RichMuted,
                13);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            return label;
        }

        private static Label CreateFixedLabel(string text, float width, Color color, int fontSize)
        {
            var label = CreatePreviewLabel(text, color, fontSize);
            label.CustomMinimumSize = new(width, 26f);
            label.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
            label.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            label.VerticalAlignment = VerticalAlignment.Center;
            return label;
        }

        private static Label CreatePreviewLabel(
            string text,
            Color color,
            int fontSize,
            bool bold = false,
            float minHeight = 24f)
        {
            var label = new Label
            {
                Text = text,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                CustomMinimumSize = new(0f, minHeight),
                MouseFilter = Control.MouseFilterEnum.Ignore,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
            };
            label.AddThemeFontOverride("font", bold
                ? RitsuShellTheme.Current.Font.BodyBold
                : RitsuShellTheme.Current.Font.Body);
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeColorOverride("font_color", color);
            return label;
        }

        private sealed partial class ContentModLoadOrderPreviewControl : MarginContainer
        {
            public ContentModLoadOrderPreviewControl()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill;
                MouseFilter = MouseFilterEnum.Ignore;
                RefreshPreview();
            }

            public override void _EnterTree()
            {
                ContentModLoadOrderPreviewRefreshRequested += RefreshPreview;
            }

            public override void _ExitTree()
            {
                ContentModLoadOrderPreviewRefreshRequested -= RefreshPreview;
            }

            private void RefreshPreview()
            {
                foreach (var child in GetChildren())
                {
                    RemoveChild(child);
                    child.QueueFree();
                }

                AddChild(CreateContentModLoadOrderPreviewBody());
            }
        }
    }
}
