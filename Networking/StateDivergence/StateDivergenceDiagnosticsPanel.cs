using System.Text;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Ui.Toast;

namespace STS2RitsuLib.Networking.StateDivergence
{
    internal sealed partial class StateDivergenceDiagnosticsPanel : Control, IScreenContext
    {
        private const int ControllerScrollStep = 72;
        private const string FocusRefreshAttachedMeta = "ritsu_state_divergence_focus_refresh_attached";
        private readonly List<Control> _focusChain = [];
        private readonly StateDivergenceDiagnosticReport _report = null!;
        private bool _focusRefreshScheduled;
        private ScrollContainer? _mainScroll;

        public StateDivergenceDiagnosticsPanel(StateDivergenceDiagnosticReport report)
        {
            _report = report;
            Name = "RitsuStateDivergenceDiagnosticsPanel";
            MouseFilter = MouseFilterEnum.Stop;
        }

        public StateDivergenceDiagnosticsPanel()
        {
        }

        public Control? DefaultFocusedControl { get; private set; }

        public override void _Ready()
        {
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            SetProcessUnhandledInput(true);
            Build();
            AttachControllerFocusChromeRecursive(this);
            ScheduleFocusRefresh();
            Callable.From(() =>
            {
                DefaultFocusedControl ??= _focusChain.FirstOrDefault();
                DefaultFocusedControl?.GrabFocus();
            }).CallDeferred();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (!@event.IsEcho() &&
                (@event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack)))
            {
                Close();
                GetViewport()?.SetInputAsHandled();
                return;
            }

            if (!@event.IsEcho() && TryScrollFromInput(@event))
            {
                GetViewport()?.SetInputAsHandled();
                return;
            }

            base._UnhandledInput(@event);
        }

        private void Build()
        {
            var viewportMargin = new MarginContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            viewportMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            viewportMargin.AddThemeConstantOverride("margin_left", 32);
            viewportMargin.AddThemeConstantOverride("margin_top", 28);
            viewportMargin.AddThemeConstantOverride("margin_right", 32);
            viewportMargin.AddThemeConstantOverride("margin_bottom", 28);
            AddChild(viewportMargin);

            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Stop,
            };
            panel.AddThemeStyleboxOverride("panel",
                RitsuShellPanelStyles.CreateFramedSurface(RitsuShellTheme.Current.Surface.Content,
                    RitsuShellTheme.Current.Metric.Radius.Default));
            viewportMargin.AddChild(panel);

            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 24);
            margin.AddThemeConstantOverride("margin_top", 22);
            margin.AddThemeConstantOverride("margin_right", 24);
            margin.AddThemeConstantOverride("margin_bottom", 20);
            panel.AddChild(margin);

            var root = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddThemeConstantOverride("separation", 16);
            margin.AddChild(root);

            root.AddChild(BuildHeader());
            root.AddChild(BuildSections());
            root.AddChild(BuildFooter());
        }

        private Control BuildHeader()
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 16);

            var title = CreateLabel(_report.Title, 28, RitsuShellTheme.Current.Text.RichTitle, true);
            title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(title);

            var export = new ModSettingsTextButton(
                T("button.copyReport", "Copy report"),
                ModSettingsButtonTone.Normal,
                CopyReportToClipboard)
            {
                CustomMinimumSize = new(190f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
            };
            row.AddChild(export);

            var close = new ModSettingsTextButton(
                T("button.close", "Close"),
                ModSettingsButtonTone.Normal,
                Close)
            {
                CustomMinimumSize = new(150f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
            };
            row.AddChild(close);
            DefaultFocusedControl = close;
            return row;
        }

        private Control BuildSections()
        {
            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                MouseFilter = MouseFilterEnum.Stop,
                FocusMode = FocusModeEnum.None,
            };
            ModSettingsUiControlTheming.ApplySettingsScrollContainerTheme(scroll);
            _mainScroll = scroll;

            var box = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            box.AddThemeConstantOverride("separation", 12);

            var scrollMargin = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            scrollMargin.AddThemeConstantOverride("margin_right",
                ModSettingsUiControlTheming.ResolveSettingsScrollContentRightGutter(scroll));
            scroll.AddChild(scrollMargin);
            scrollMargin.AddChild(box);

            box.AddChild(BuildSummarySection());
            foreach (var section in _report.Sections.Where(section => section.Rows.Count > 0))
                box.AddChild(BuildSection(section));

            return scroll;
        }

        private Control BuildSummarySection()
        {
            return new ModSettingsCollapsibleSection(
                T("section.summary.title", "Summary"),
                "state_divergence_summary",
                _report.Summary,
                false,
                [BuildSummaryBody()]);
        }

        private Control BuildSummaryBody()
        {
            var box = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            box.AddThemeConstantOverride("separation", 8);
            box.AddChild(CreateInfoCard(_report.Summary, RitsuShellTheme.Current.Text.RichBody));
            box.AddChild(BuildChecksumCards());
            return box;
        }

        private Control BuildChecksumCards()
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 10);
            row.AddChild(CreateChecksumCard(T("column.local", "Local"), _report.LocalChecksum));
            row.AddChild(CreateChecksumCard(T("column.remote", "Remote"), _report.RemoteChecksum));
            return row;
        }

        private Control CreateChecksumCard(string title, StateDivergenceChecksumInfo info)
        {
            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListItemCardStyle());

            var box = CreateInsetVBox(panel, 10, 7, 10, 7, 4);
            box.AddChild(CreateLabel(title, 16, RitsuShellTheme.Current.Text.RichTitle, true));
            box.AddChild(CreateLabel(F("checksum.id", "ID: {0}", info.Id), 14,
                RitsuShellTheme.Current.Text.RichBody));
            box.AddChild(CreateLabel(F("checksum.value", "Checksum: {0}", info.Checksum), 14,
                RitsuShellTheme.Current.Text.RichBody));
            box.AddChild(CreateLabel(F("checksum.context", "Context: {0}", info.Context), 14,
                RitsuShellTheme.Current.Text.RichBody));
            return panel;
        }

        private Control BuildSection(StateDivergenceDiagnosticSection section)
        {
            return new ModSettingsCollapsibleSection(
                section.Title,
                "state_divergence_" + section.Title.GetHashCode(),
                section.Description,
                section.StartsCollapsed,
                [BuildSectionBody(section)]);
        }

        private Control BuildSectionBody(StateDivergenceDiagnosticSection section)
        {
            if (section.Rows.Count == 0)
                return CreateInfoCard(T("value.noDifferences", "No visible differences in this section."),
                    RitsuShellTheme.Current.Text.RichMuted);

            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListShellStyle());

            var box = CreateInsetVBox(panel, 10, 8, 10, 8, 6);
            foreach (var row in section.Rows)
                box.AddChild(BuildDetailRow(row));

            return panel;
        }

        private Control BuildDetailRow(StateDivergenceDiagnosticRow detail)
        {
            if (detail.Kind == StateDivergenceDiagnosticRowKind.ModelList)
                return BuildModelListDetailRow(detail);
            return IsExpandedDetail(detail) ? BuildExpandedDetailRow(detail) : BuildCompactDetailRow(detail);
        }

        private static bool IsExpandedDetail(StateDivergenceDiagnosticRow detail)
        {
            return !string.IsNullOrEmpty(detail.Detail) ||
                   detail.LocalValue.Contains('\n') ||
                   detail.RemoteValue.Contains('\n');
        }

        private Control BuildCompactDetailRow(StateDivergenceDiagnosticRow detail)
        {
            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListItemCardStyle(true));

            var box = CreateInsetVBox(panel, 10, 7, 10, 7, 6);
            box.AddChild(CreateLabel(detail.Path, 17, RitsuShellTheme.Current.Text.RichTitle, true));
            box.AddChild(BuildComparisonValues(detail.LocalValue, detail.RemoteValue));
            return panel;
        }

        private Control BuildComparisonValues(string localValue, string remoteValue)
        {
            var values = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            values.AddThemeConstantOverride("separation", 12);
            values.AddChild(BuildInlineValueColumn(T("column.local", "Local"), localValue,
                RitsuShellTheme.Current.Text.RichBody));
            values.AddChild(BuildInlineValueColumn(T("column.remote", "Remote"), remoteValue,
                RitsuShellTheme.Current.Text.HoverHighlight));
            return values;
        }

        private Control BuildInlineValueColumn(string title, string value, Color valueColor)
        {
            var box = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            box.AddThemeConstantOverride("separation", 2);
            box.AddChild(CreateLabel(title, 14, RitsuShellTheme.Current.Text.RichMuted, true));
            box.AddChild(CreateValueLabel(value, valueColor));
            return box;
        }

        private Control BuildExpandedDetailRow(StateDivergenceDiagnosticRow detail)
        {
            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListItemCardStyle(true));

            var box = CreateInsetVBox(panel, 10, 8, 10, 8, 8);
            box.AddChild(CreateLabel(detail.Path, 17, RitsuShellTheme.Current.Text.RichTitle, true));
            if (!string.IsNullOrWhiteSpace(detail.Detail))
                box.AddChild(CreateLabel(detail.Detail, 15, RitsuShellTheme.Current.Text.RichMuted));

            var values = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            values.AddThemeConstantOverride("separation", 14);
            values.AddChild(BuildExpandedValueColumn(T("column.local", "Local"), detail.LocalValue,
                RitsuShellTheme.Current.Text.RichBody));
            values.AddChild(BuildExpandedValueColumn(T("column.remote", "Remote"), detail.RemoteValue,
                RitsuShellTheme.Current.Text.HoverHighlight));
            box.AddChild(values);
            return panel;
        }

        private Control BuildModelListDetailRow(StateDivergenceDiagnosticRow detail)
        {
            var body = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            body.AddThemeConstantOverride("separation", 8);
            body.AddChild(BuildModelComparisonList(detail));

            return new ModSettingsCollapsibleSection(
                detail.Path,
                "state_divergence_model_list_" + detail.Path.GetHashCode(),
                detail.Detail,
                true,
                [body]);
        }

        private Control BuildModelComparisonList(StateDivergenceDiagnosticRow detail)
        {
            var items = detail.ModelItems ?? [];
            var count = items.Count;
            if (count == 0)
                return BuildComparisonValues(detail.LocalValue, detail.RemoteValue);

            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
                CustomMinimumSize = new(0f, Math.Min(420f, Math.Max(108f, 44f + count * 48f))),
                MouseFilter = MouseFilterEnum.Stop,
            };
            ModSettingsUiControlTheming.ApplySettingsScrollContainerTheme(scroll);

            var margin = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            margin.AddThemeConstantOverride("margin_right",
                ModSettingsUiControlTheming.ResolveSettingsScrollContentRightGutter(scroll));
            scroll.AddChild(margin);

            var table = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            table.AddThemeConstantOverride("separation", 4);
            margin.AddChild(table);

            table.AddChild(BuildPileHeaderRow());

            foreach (var item in items)
                table.AddChild(BuildModelListEntryRow(item));

            return scroll;
        }

        private Control BuildPileHeaderRow()
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(CreateFixedHeaderLabel("#", 42));
            row.AddChild(CreateHeaderLabel(T("column.local", "Local")));
            row.AddChild(CreateHeaderLabel(T("column.remote", "Remote")));
            return row;
        }

        private Control BuildModelListEntryRow(StateDivergenceDiagnosticModelListItem item)
        {
            var differs = item.Differences.Count > 0 ||
                          !string.Equals(item.LocalSummary, item.RemoteSummary, StringComparison.Ordinal);
            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListItemCardStyle(differs));

            var box = CreateInsetVBox(panel, 8, 6, 8, 6, 6);
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            row.AddThemeConstantOverride("separation", 8);
            box.AddChild(row);

            row.AddChild(CreateFixedValueLabel(item.Index, 42, RitsuShellTheme.Current.Text.Number, false));
            row.AddChild(CreateValueLabel(item.LocalSummary,
                differs ? RitsuShellTheme.Current.Text.HoverHighlight : RitsuShellTheme.Current.Text.RichBody));
            row.AddChild(CreateValueLabel(item.RemoteSummary,
                differs ? RitsuShellTheme.Current.Text.HoverHighlight : RitsuShellTheme.Current.Text.RichBody));

            if (item.Differences.Count > 0)
                box.AddChild(BuildModelFieldDifferences(item.Differences));

            return panel;
        }

        private Control BuildModelFieldDifferences(
            IReadOnlyList<StateDivergenceDiagnosticFieldDifference> differences)
        {
            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateInsetSurfaceStyle());

            var box = CreateInsetVBox(panel, 8, 6, 8, 6, 4);
            box.AddChild(BuildModelFieldHeaderRow());
            foreach (var difference in differences)
                box.AddChild(BuildModelFieldRow(difference));

            return panel;
        }

        private Control BuildModelFieldHeaderRow()
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(CreateFixedHeaderLabel(T("column.field", "Field"), 210));
            row.AddChild(CreateHeaderLabel(T("column.local", "Local")));
            row.AddChild(CreateHeaderLabel(T("column.remote", "Remote")));
            return row;
        }

        private Control BuildModelFieldRow(StateDivergenceDiagnosticFieldDifference difference)
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(CreateFixedMultilineValueLabel(difference.Path, 210, RitsuShellTheme.Current.Text.RichTitle,
                true));
            row.AddChild(CreateMultilineValueLabel(difference.LocalValue, RitsuShellTheme.Current.Text.RichBody));
            row.AddChild(CreateMultilineValueLabel(difference.RemoteValue,
                RitsuShellTheme.Current.Text.HoverHighlight));
            return row;
        }

        private Control BuildExpandedValueColumn(string title, string value, Color valueColor)
        {
            var box = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            box.AddThemeConstantOverride("separation", 4);
            box.AddChild(CreateLabel(title, 14, RitsuShellTheme.Current.Text.RichTitle, true));
            box.AddChild(CreateMultilineValueLabel(value, valueColor));
            return box;
        }

        private Control BuildFooter()
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 10);
            var label = CreateLabel(
                F("footer.context", "Role: {0}  Remote peer: {1}", _report.Role, _report.RemotePeerId),
                14,
                RitsuShellTheme.Current.Text.LabelSecondary);
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(label);
            return row;
        }

        private Label CreateLabel(string text, int fontSize, Color color, bool bold = false)
        {
            return new Label
            {
                Text = text,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            }.Also(label =>
            {
                label.AddThemeFontOverride("font",
                    bold ? RitsuShellTheme.Current.Font.BodyBold : RitsuShellTheme.Current.Font.Body);
                label.AddThemeFontSizeOverride("font_size", fontSize);
                label.AddThemeColorOverride("font_color", color);
            });
        }

        private Label CreateHeaderLabel(string text)
        {
            var label = CreateLabel(text, 17, RitsuShellTheme.Current.Text.RichTitle, true);
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            return label;
        }

        private Label CreateFixedHeaderLabel(string text, float width)
        {
            var label = CreateHeaderLabel(text);
            label.CustomMinimumSize = new(width, 24f);
            label.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            return label;
        }

        private Label CreateValueLabel(string text, Color color, bool bold = false)
        {
            var label = CreateLabel(text, 16, color, bold);
            label.CustomMinimumSize = new(0f, 30f);
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            return label;
        }

        private Label CreateFixedValueLabel(string text, float width, Color color, bool bold)
        {
            var label = CreateValueLabel(text, color, bold);
            label.CustomMinimumSize = new(width, 28f);
            label.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            return label;
        }

        private Label CreateFixedMultilineValueLabel(string text, float width, Color color, bool bold)
        {
            var label = CreateMultilineValueLabel(text, color, bold);
            label.CustomMinimumSize = new(width, 28f);
            label.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            return label;
        }

        private Label CreateMultilineValueLabel(string text, Color color, bool bold = false)
        {
            return new Label
            {
                Text = text,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            }.Also(label =>
            {
                label.AddThemeFontOverride("font",
                    bold ? RitsuShellTheme.Current.Font.BodyBold : RitsuShellTheme.Current.Font.Body);
                label.AddThemeFontSizeOverride("font_size", 16);
                label.AddThemeColorOverride("font_color", color);
            });
        }

        private static IReadOnlyList<IndexedLine> ParseIndexedLines(string text)
        {
            if (string.IsNullOrWhiteSpace(text) ||
                (!text.Contains('\n') && text.StartsWith("<", StringComparison.Ordinal)))
                return [];

            return text.Replace("\r\n", "\n")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseIndexedLine)
                .ToList();
        }

        private static IndexedLine ParseIndexedLine(string line)
        {
            var trimmed = line.Trim();
            var split = trimmed.IndexOf("  ", StringComparison.Ordinal);
            if (split <= 0)
                return new("", trimmed);

            return new(trimmed[..split], trimmed[(split + 2)..].TrimStart());
        }

        private Control CreateInfoCard(string text, Color color)
        {
            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateInsetSurfaceStyle());
            var box = CreateInsetVBox(panel, 12, 9, 12, 9, 4);
            box.AddChild(CreateLabel(text, 16, color));
            return panel;
        }

        private static VBoxContainer CreateInsetVBox(
            Container parent,
            int left,
            int top,
            int right,
            int bottom,
            int separation)
        {
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", left);
            margin.AddThemeConstantOverride("margin_top", top);
            margin.AddThemeConstantOverride("margin_right", right);
            margin.AddThemeConstantOverride("margin_bottom", bottom);
            parent.AddChild(margin);

            var box = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            box.AddThemeConstantOverride("separation", separation);
            margin.AddChild(box);
            return box;
        }

        private void Close()
        {
            NModalContainer.Instance?.Clear();
        }

        private void CopyReportToClipboard()
        {
            DisplayServer.ClipboardSet(BuildExportReport());
            ModSettingsClipboardAccess.InvalidateCache();
            RitsuToastService.ShowInfo(
                T("toast.reportCopied.body", "State divergence report copied to clipboard."),
                T("toast.reportCopied.title", "State divergence"));
        }

        private string BuildExportReport()
        {
            var builder = new StringBuilder();
            builder.AppendLine(_report.Title);
            builder.AppendLine(new('=', _report.Title.Length));
            builder.AppendLine();
            builder.AppendLine(_report.Summary);
            builder.AppendLine(F("footer.context", "Role: {0}  Remote peer: {1}", _report.Role,
                _report.RemotePeerId));
            builder.AppendLine();
            AppendChecksum(builder, T("column.local", "Local"), _report.LocalChecksum);
            AppendChecksum(builder, T("column.remote", "Remote"), _report.RemoteChecksum);

            foreach (var section in _report.Sections)
            {
                builder.AppendLine();
                builder.AppendLine(section.Title);
                builder.AppendLine(new('-', section.Title.Length));
                builder.AppendLine(section.Description);
                foreach (var row in section.Rows)
                    AppendReportRow(builder, row);
            }

            builder.AppendLine();
            builder.AppendLine("LOCAL STATE DUMP");
            builder.AppendLine(_report.LocalStateDump);
            builder.AppendLine("REMOTE STATE DUMP");
            builder.AppendLine(_report.RemoteStateDump);
            return builder.ToString();
        }

        private static void AppendChecksum(StringBuilder builder, string title, StateDivergenceChecksumInfo info)
        {
            builder.AppendLine(title);
            builder.AppendLine("  " + F("checksum.id", "ID: {0}", info.Id));
            builder.AppendLine("  " + F("checksum.value", "Checksum: {0}", info.Checksum));
            builder.AppendLine("  " + F("checksum.context", "Context: {0}", info.Context));
        }

        private static void AppendReportRow(StringBuilder builder, StateDivergenceDiagnosticRow row)
        {
            if (row is { Kind: StateDivergenceDiagnosticRowKind.ModelList, ModelItems: not null })
            {
                builder.AppendLine(row.Path);
                if (!string.IsNullOrWhiteSpace(row.Detail))
                    builder.AppendLine("  " + row.Detail);
                foreach (var item in row.ModelItems)
                {
                    builder.AppendLine($"  {item.Index}: local={item.LocalSummary}; remote={item.RemoteSummary}");
                    foreach (var difference in item.Differences)
                    {
                        builder.AppendLine($"    {difference.Path}");
                        builder.AppendLine($"      local: {difference.LocalValue.ReplaceLineEndings(" | ")}");
                        builder.AppendLine($"      remote: {difference.RemoteValue.ReplaceLineEndings(" | ")}");
                    }
                }

                return;
            }

            if (!IsExpandedDetail(row))
            {
                builder.AppendLine($"{row.Path}: local={row.LocalValue}; remote={row.RemoteValue}");
                return;
            }

            builder.AppendLine(row.Path);
            if (!string.IsNullOrWhiteSpace(row.Detail))
                builder.AppendLine("  " + row.Detail);
            builder.AppendLine("  local:");
            AppendIndentedLines(builder, row.LocalValue, "    ");
            builder.AppendLine("  remote:");
            AppendIndentedLines(builder, row.RemoteValue, "    ");
        }

        private static void AppendIndentedLines(StringBuilder builder, string text, string indent)
        {
            foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
                builder.AppendLine(indent + line);
        }

        private bool TryScrollFromInput(InputEvent @event)
        {
            var delta = 0;
            if (@event.IsActionPressed(MegaInput.down) || @event.IsActionPressed("ui_down"))
                delta = ControllerScrollStep;
            else if (@event.IsActionPressed(MegaInput.up) || @event.IsActionPressed("ui_up"))
                delta = -ControllerScrollStep;

            if (delta == 0 || _mainScroll == null || !IsInstanceValid(_mainScroll))
                return false;

            var next = Math.Max(0, _mainScroll.ScrollVertical + delta);
            if (next == _mainScroll.ScrollVertical)
                return true;

            _mainScroll.ScrollVertical = next;
            return true;
        }

        private void AttachControllerFocusChromeRecursive(Node node)
        {
            if (node is BaseButton button)
            {
                ModSettingsFocusChrome.AttachControllerSelectionReticle(button);
                if (!button.HasMeta(FocusRefreshAttachedMeta))
                {
                    button.SetMeta(FocusRefreshAttachedMeta, true);
                    button.Pressed += ScheduleFocusRefresh;
                    button.FocusEntered += () => EnsureMainScrollControlVisible(button);
                }
            }

            foreach (var child in node.GetChildren())
                AttachControllerFocusChromeRecursive(child);
        }

        private void EnsureMainScrollControlVisible(Control control)
        {
            var scroll = _mainScroll;
            if (scroll == null ||
                !IsInstanceValid(scroll) ||
                !IsInstanceValid(control) ||
                !scroll.IsAncestorOf(control))
                return;

            scroll.EnsureControlVisible(control);
        }

        private void ScheduleFocusRefresh()
        {
            if (_focusRefreshScheduled)
                return;

            _focusRefreshScheduled = true;
            Callable.From(RefreshFocusNavigationDeferred).CallDeferred();
        }

        private void RefreshFocusNavigationDeferred()
        {
            _focusRefreshScheduled = false;
            if (!IsInsideTree())
                return;

            _focusChain.Clear();
            CollectFocusChain(this, _focusChain);
            WireFocusChain(_focusChain);

            var owner = GetViewport()?.GuiGetFocusOwner();
            if (owner != null && IsAncestorOf(owner) && owner.IsVisibleInTree())
                return;

            DefaultFocusedControl ??= _focusChain.FirstOrDefault();
            DefaultFocusedControl?.GrabFocus();
        }

        private static void CollectFocusChain(Control root, ICollection<Control> chain)
        {
            if (root.IsVisibleInTree() &&
                root.FocusMode == FocusModeEnum.All &&
                root is BaseButton)
                chain.Add(root);

            foreach (var child in root.GetChildren())
            {
                if (child is not Control control || !control.IsVisibleInTree())
                    continue;

                CollectFocusChain(control, chain);
            }
        }

        private static void WireFocusChain(IReadOnlyList<Control> chain)
        {
            for (var i = 0; i < chain.Count; i++)
            {
                var current = chain[i];
                var self = current.GetPath();
                current.FocusNeighborLeft = self;
                current.FocusNeighborRight = self;
                current.FocusNeighborTop = i > 0 ? chain[i - 1].GetPath() : self;
                current.FocusNeighborBottom = i < chain.Count - 1 ? chain[i + 1].GetPath() : self;
            }
        }

        private static string T(string key, string fallback)
        {
            return StateDivergenceDiagnosticsLocalization.Get(key, fallback);
        }

        private static string F(string key, string fallback, params object?[] args)
        {
            return StateDivergenceDiagnosticsLocalization.Format(key, fallback, args);
        }

        private readonly record struct IndexedLine(string Index, string Value);
    }

    internal static class StateDivergenceDiagnosticsPanelExtensions
    {
        public static T Also<T>(this T value, Action<T> action)
        {
            action(value);
            return value;
        }
    }
}
