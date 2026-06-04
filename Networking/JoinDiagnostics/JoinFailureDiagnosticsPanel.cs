using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Networking.JoinDiagnostics
{
    internal sealed partial class JoinFailureDiagnosticsPanel : Control, IScreenContext
    {
        private const int ControllerScrollStep = 72;
        private readonly JoinFailureDiagnosticReport _report = null!;
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
            Callable.From(() => DefaultFocusedControl?.GrabFocus()).CallDeferred();
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
            var center = new CenterContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(center);

            var panel = new PanelContainer
            {
                CustomMinimumSize = new(1080f, 720f),
                MouseFilter = MouseFilterEnum.Stop,
            };
            panel.AddThemeStyleboxOverride("panel",
                RitsuShellPanelStyles.CreateFramedSurface(RitsuShellTheme.Current.Surface.Content,
                    RitsuShellTheme.Current.Metric.Radius.Default));
            center.AddChild(panel);

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
            root.AddChild(BuildSummary());
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

        private Control BuildSummary()
        {
            var panel = new PanelContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            panel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateInsetSurfaceStyle());

            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 14);
            margin.AddThemeConstantOverride("margin_top", 10);
            margin.AddThemeConstantOverride("margin_right", 14);
            margin.AddThemeConstantOverride("margin_bottom", 10);
            panel.AddChild(margin);

            var box = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            box.AddThemeConstantOverride("separation", 6);
            margin.AddChild(box);

            box.AddChild(CreateLabel(T("section.summary", "Summary"), 19, RitsuShellTheme.Current.Text.RichTitle,
                true));
            box.AddChild(CreateLabel(_report.Summary, 18, RitsuShellTheme.Current.Text.RichBody));

            return panel;
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
            _mainScroll = scroll;

            var box = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            box.AddThemeConstantOverride("separation", 16);
            scroll.AddChild(box);

            foreach (var issue in _report.Issues)
                box.AddChild(BuildIssue(issue));

            return scroll;
        }

        private Control BuildIssue(JoinFailureIssue issue)
        {
            var box = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            box.AddThemeConstantOverride("separation", 9);

            box.AddChild(CreateLabel(issue.Title, 21, RitsuShellTheme.Current.Text.RichTitle, true));
            box.AddChild(CreateLabel(issue.Description, 17, RitsuShellTheme.Current.Text.RichBody));

            if (issue.Rows.Count == 0)
                return box;

            var table = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            table.AddThemeConstantOverride("separation", 3);
            box.AddChild(table);

            table.AddChild(BuildTableRow(
                T("column.item", "Item"),
                T("column.host", "Host"),
                T("column.local", "Local"),
                true));
            foreach (var row in issue.Rows)
                table.AddChild(BuildTableRow(row.Label, row.HostValue, row.LocalValue, false));

            if (issue.Kind == JoinFailureIssueKind.ModOrder &&
                _report.Host is { } host &&
                host.GameplayMods.Count == _report.Local.GameplayMods.Count)
                box.AddChild(BuildModOrderLists(host.GameplayMods, _report.Local.GameplayMods));

            return box;
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
            panel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListShellStyle());

            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 10);
            margin.AddThemeConstantOverride("margin_top", 8);
            margin.AddThemeConstantOverride("margin_right", 10);
            margin.AddThemeConstantOverride("margin_bottom", 8);
            panel.AddChild(margin);

            var box = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            box.AddThemeConstantOverride("separation", 4);
            margin.AddChild(box);

            box.AddChild(CreateLabel(title, 18, RitsuShellTheme.Current.Text.RichTitle, true));

            var entries = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            entries.AddThemeConstantOverride("separation", 5);
            box.AddChild(entries);

            for (var i = 0; i < mods.Count; i++)
            {
                var mod = mods[i];
                var matches = i < counterpart.Count &&
                              string.Equals(mod.Key, counterpart[i].Key, StringComparison.Ordinal);
                var color = matches
                    ? RitsuShellTheme.Current.Text.RichBody
                    : RitsuShellTheme.Current.Text.HoverHighlight;
                entries.AddChild(CreateLabel("#" + (i + 1) + "  " + FormatModLine(mod), 15, color));
            }

            return panel;
        }

        private Control BuildTableRow(string labelText, string hostText, string localText, bool header)
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 10);

            row.AddChild(CreateCell(labelText, 280f, header));
            row.AddChild(CreateCell(hostText, 370f, header));
            row.AddChild(CreateCell(localText, 370f, header));
            return row;
        }

        private Label CreateCell(string text, float width, bool header)
        {
            var label = CreateLabel(
                text,
                header ? 16 : 15,
                header ? RitsuShellTheme.Current.Text.RichTitle : RitsuShellTheme.Current.Text.RichBody,
                header);
            label.CustomMinimumSize = new(width, 0f);
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            return label;
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

        private void Close()
        {
            NModalContainer.Instance?.Clear();
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
