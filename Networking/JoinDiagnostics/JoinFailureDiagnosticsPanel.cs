using System.Text;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Ui.Toast;

namespace STS2RitsuLib.Networking.JoinDiagnostics
{
    internal sealed partial class JoinFailureDiagnosticsPanel : Control, IScreenContext
    {
        private const int ControllerScrollStep = 72;
        private const string FocusRefreshAttachedMeta = "ritsu_join_diagnostics_focus_refresh_attached";
        private readonly List<Control> _focusChain = [];
        private readonly JoinFailureDiagnosticReport _report = null!;
        private bool _focusRefreshScheduled;
        private VBoxContainer? _issuesRoot;
        private ScrollContainer? _mainScroll;

        public JoinFailureDiagnosticsPanel(JoinFailureDiagnosticReport report)
        {
            _report = report;
            Name = "RitsuJoinFailureDiagnosticsPanel";
            MouseFilter = MouseFilterEnum.Stop;
        }

        public JoinFailureDiagnosticsPanel()
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
            root.AddChild(BuildIssues());
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

        private Control BuildIssues()
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
            _issuesRoot = box;

            box.AddChild(BuildSummarySection());
            foreach (var issue in _report.Issues)
                box.AddChild(BuildIssue(issue));

            return scroll;
        }

        private Control BuildSummarySection()
        {
            return new ModSettingsCollapsibleSection(
                T("section.summary", "Summary"),
                "join_summary",
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

            if (_report.Host != null)
                box.AddChild(BuildPeerSnapshotRow());

            return box;
        }

        private Control BuildIssue(JoinFailureIssue issue)
        {
            return new ModSettingsCollapsibleSection(
                issue.Title,
                "join_issue_" + issue.Kind,
                issue.Description,
                ShouldStartCollapsed(issue),
                [BuildIssueBody(issue)]);
        }

        private static bool ShouldStartCollapsed(JoinFailureIssue issue)
        {
            return issue.Kind is JoinFailureIssueKind.Network or JoinFailureIssueKind.Transport;
        }

        private Control BuildIssueBody(JoinFailureIssue issue)
        {
            var box = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            box.AddThemeConstantOverride("separation", 10);

            if (issue.Rows.Count == 0)
            {
                box.AddChild(CreateInfoCard(issue.Description, RitsuShellTheme.Current.Text.RichBody));
                return box;
            }

            box.AddChild(BuildDetailRows(issue.Rows));

            if (issue.Kind == JoinFailureIssueKind.ModOrder &&
                _report.Host is { } host &&
                host.GameplayMods.Count == _report.Local.GameplayMods.Count)
                box.AddChild(BuildModOrderLists(host.GameplayMods, _report.Local.GameplayMods));

            return box;
        }

        private Control BuildPeerSnapshotRow()
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 10);

            row.AddChild(CreateSnapshotCard(
                T("column.host", "Host"),
                _report.Host!.GameVersion,
                _report.Host.ModelDbHash,
                _report.Host.GameplayMods.Count));
            row.AddChild(CreateSnapshotCard(
                T("column.local", "Local"),
                _report.Local.GameVersion,
                _report.Local.ModelDbHash,
                _report.Local.GameplayMods.Count));
            return row;
        }

        private Control CreateSnapshotCard(string title, string version, uint modelDbHash, int modCount)
        {
            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListItemCardStyle());

            var box = CreateInsetVBox(panel, 10, 7, 10, 7, 4);
            box.AddChild(CreateLabel(title, 16, RitsuShellTheme.Current.Text.RichTitle, true));
            box.AddChild(CreateLabel(
                F("snapshot.version", "Version: {0}", version),
                14,
                RitsuShellTheme.Current.Text.RichBody));
            box.AddChild(CreateLabel(
                F("snapshot.modelDb", "ModelDb: {0}", modelDbHash),
                14,
                RitsuShellTheme.Current.Text.RichBody));
            box.AddChild(CreateLabel(
                F("snapshot.mods", "Gameplay mods: {0}", modCount),
                14,
                RitsuShellTheme.Current.Text.RichBody));
            return panel;
        }

        private Control BuildDetailRows(IReadOnlyList<JoinFailureDetailRow> rows)
        {
            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListShellStyle());

            var box = CreateInsetVBox(panel, 10, 8, 10, 8, 6);
            box.AddChild(BuildDetailHeaderRow());
            foreach (var row in rows)
                box.AddChild(BuildDetailRow(row));

            return panel;
        }

        private Control BuildDetailHeaderRow()
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 10);
            row.AddChild(CreateFixedHeaderLabel(T("column.item", "Item"), 250));
            row.AddChild(CreateHeaderLabel(T("column.host", "Host")));
            row.AddChild(CreateHeaderLabel(T("column.local", "Local")));
            return row;
        }

        private Control BuildDetailRow(JoinFailureDetailRow detail)
        {
            var differs = !string.Equals(detail.HostValue, detail.LocalValue, StringComparison.Ordinal);
            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new(0f, 44f),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListItemCardStyle(differs));

            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            row.AddThemeConstantOverride("separation", 10);
            panel.AddChild(row);

            row.AddChild(CreateFixedValueLabel(detail.Label, 250, RitsuShellTheme.Current.Text.RichTitle, true));
            row.AddChild(CreateValueLabel(detail.HostValue, ValueColor(detail.HostValue, differs)));
            row.AddChild(CreateValueLabel(detail.LocalValue, ValueColor(detail.LocalValue, differs)));
            return panel;
        }

        private Control BuildModOrderLists(
            IReadOnlyList<JoinDiagnosticsModEntry> hostMods,
            IReadOnlyList<JoinDiagnosticsModEntry> localMods)
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 14);
            row.AddChild(BuildModOrderList(T("column.hostOrder", "Host order"), hostMods, localMods));
            row.AddChild(BuildModOrderList(T("column.localOrder", "Local order"), localMods, hostMods));
            return row;
        }

        private Control BuildModOrderList(
            string title,
            IReadOnlyList<JoinDiagnosticsModEntry> mods,
            IReadOnlyList<JoinDiagnosticsModEntry> counterpart)
        {
            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListShellStyle());

            var box = CreateInsetVBox(panel, 10, 8, 10, 8, 6);

            box.AddChild(CreateLabel(title, 18, RitsuShellTheme.Current.Text.RichTitle, true));

            var entries = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            entries.AddThemeConstantOverride("separation", 6);
            box.AddChild(entries);

            for (var i = 0; i < mods.Count; i++)
            {
                var mod = mods[i];
                var matches = i < counterpart.Count &&
                              string.Equals(mod.Key, counterpart[i].Key, StringComparison.Ordinal);
                entries.AddChild(BuildModOrderRow(i, mod, matches));
            }

            return panel;
        }

        private Control BuildModOrderRow(int index, JoinDiagnosticsModEntry mod, bool matches)
        {
            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new(0f, 38f),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListItemCardStyle(!matches));

            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            row.AddThemeConstantOverride("separation", 8);
            panel.AddChild(row);

            row.AddChild(CreateFixedValueLabel("#" + (index + 1).ToString("00"), 46,
                RitsuShellTheme.Current.Text.Number, false));
            row.AddChild(CreateValueLabel(FormatModLine(mod),
                matches ? RitsuShellTheme.Current.Text.RichBody : RitsuShellTheme.Current.Text.HoverHighlight));
            row.AddChild(CreateFixedValueLabel(
                matches ? T("value.same", "same") : T("value.differs", "differs"),
                76,
                matches ? RitsuShellTheme.Current.Text.RichMuted : RitsuShellTheme.Current.Text.HoverHighlight,
                false));
            return panel;
        }

        private Control BuildFooter()
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 10);

            var reason = F("footer.networkReason", "Network reason: {0}", _report.NetworkReason);
            if (!string.IsNullOrWhiteSpace(_report.NetworkInfo))
                reason += "  " + F("footer.networkInfo", "Info: {0}", _report.NetworkInfo);

            var label = CreateLabel(reason, 14, RitsuShellTheme.Current.Text.LabelSecondary);
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
            var label = CreateLabel(text, 15, RitsuShellTheme.Current.Text.RichTitle, true);
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
            var label = CreateLabel(text, 14, color, bold);
            label.CustomMinimumSize = new(0f, 28f);
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
                T("toast.reportCopied.body", "Join failure report copied to clipboard."),
                T("toast.reportCopied.title", "Join diagnostics"));
        }

        private string BuildExportReport()
        {
            var builder = new StringBuilder();
            builder.AppendLine(_report.Title);
            builder.AppendLine(new('=', _report.Title.Length));
            builder.AppendLine();
            builder.AppendLine(T("section.summary", "Summary"));
            builder.AppendLine(_report.Summary);
            builder.AppendLine();
            builder.AppendLine(F("footer.networkReason", "Network reason: {0}", _report.NetworkReason));
            if (!string.IsNullOrWhiteSpace(_report.NetworkInfo))
                builder.AppendLine(F("footer.networkInfo", "Info: {0}", _report.NetworkInfo));

            builder.AppendLine();
            AppendPeerSnapshot(builder, T("column.host", "Host"), _report.Host);
            AppendPeerSnapshot(builder, T("column.local", "Local"), _report.Local);

            foreach (var issue in _report.Issues)
            {
                builder.AppendLine();
                builder.AppendLine(issue.Title);
                builder.AppendLine(new('-', issue.Title.Length));
                builder.AppendLine(issue.Description);

                foreach (var row in issue.Rows)
                    builder.AppendLine(
                        $"{row.Label}: {T("column.host", "Host")}={row.HostValue}; {T("column.local", "Local")}={row.LocalValue}");

                if (issue.Kind != JoinFailureIssueKind.ModOrder ||
                    _report.Host is not { } host ||
                    host.GameplayMods.Count != _report.Local.GameplayMods.Count) continue;
                builder.AppendLine();
                AppendModOrder(builder, T("column.hostOrder", "Host order"), host.GameplayMods,
                    _report.Local.GameplayMods);
                builder.AppendLine();
                AppendModOrder(builder, T("column.localOrder", "Local order"), _report.Local.GameplayMods,
                    host.GameplayMods);
            }

            return builder.ToString();
        }

        private static void AppendPeerSnapshot(StringBuilder builder, string title, JoinPeerSnapshot? snapshot)
        {
            builder.AppendLine(title);
            if (snapshot == null)
            {
                builder.AppendLine("  <unknown>");
                return;
            }

            builder.AppendLine("  " + F("snapshot.version", "Version: {0}", snapshot.GameVersion));
            builder.AppendLine("  " + F("snapshot.modelDb", "ModelDb: {0}", snapshot.ModelDbHash));
            builder.AppendLine("  " + F("snapshot.mods", "Gameplay mods: {0}", snapshot.GameplayMods.Count));
        }

        private static void AppendModOrder(
            StringBuilder builder,
            string title,
            IReadOnlyList<JoinDiagnosticsModEntry> mods,
            IReadOnlyList<JoinDiagnosticsModEntry> counterpart)
        {
            builder.AppendLine(title);
            for (var i = 0; i < mods.Count; i++)
            {
                var matches = i < counterpart.Count &&
                              string.Equals(mods[i].Key, counterpart[i].Key, StringComparison.Ordinal);
                builder.AppendLine(
                    $"  #{i + 1:00} [{(matches ? T("value.same", "same") : T("value.differs", "differs"))}] {FormatModLine(mods[i])}");
            }
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

        private static Color ValueColor(string value, bool differs)
        {
            if (!differs)
                return RitsuShellTheme.Current.Text.RichBody;

            return string.Equals(value, T("value.missing", "Missing"), StringComparison.Ordinal)
                ? RitsuShellTheme.Current.Text.HoverHighlight
                : RitsuShellTheme.Current.Text.RichBody;
        }

        private static string FormatModLine(JoinDiagnosticsModEntry mod)
        {
            if (!string.IsNullOrWhiteSpace(mod.Name) &&
                !string.Equals(mod.Name, mod.Id, StringComparison.Ordinal))
                return mod.Name + " (" + mod.Key + ")";

            return mod.Key;
        }

        private static string T(string key, string fallback)
        {
            return JoinFailureDiagnosticsLocalization.Get(key, fallback);
        }

        private static string F(string key, string fallback, params object?[] args)
        {
            return JoinFailureDiagnosticsLocalization.Format(key, fallback, args);
        }
    }

    internal static class JoinFailureDiagnosticsPanelExtensions
    {
        public static T Also<T>(this T value, Action<T> action)
        {
            action(value);
            return value;
        }
    }
}
