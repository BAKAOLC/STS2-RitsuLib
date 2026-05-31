using Godot;
using MegaCrit.Sts2.addons.mega_text;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal enum ModSettingsReusableEntryKind
    {
        Toggle,
        TextButton,
    }

    internal sealed class ModSettingsReusableEntryNodePool
    {
        private const int WarmRetainedPerKind = 24;
        private const ulong IdleReleaseMsec = 20_000;

        private readonly Dictionary<ModSettingsReusableEntryKind, Queue<ModSettingsUiFactory.ReusableSettingLine>>
            _buckets = new();

        internal bool IsWarm
        {
            get
            {
                return Enum.GetValues<ModSettingsReusableEntryKind>()
                    .All(kind => CountRetained(kind) >= WarmRetainedPerKind);
            }
        }

        internal ModSettingsUiFactory.ReusableSettingLine Rent(ModSettingsReusableEntryKind kind)
        {
            Sweep();

            if (!_buckets.TryGetValue(kind, out var bucket)) return new(kind);
            while (bucket.Count > 0)
            {
                var candidate = bucket.Dequeue();
                if (GodotObject.IsInstanceValid(candidate))
                    return candidate;
            }

            return new(kind);
        }

        internal void Return(ModSettingsUiFactory.ReusableSettingLine line)
        {
            if (!GodotObject.IsInstanceValid(line))
                return;

            line.ReleaseForPool();
            Sweep();

            if (!_buckets.TryGetValue(line.Kind, out var bucket))
            {
                bucket = new();
                _buckets[line.Kind] = bucket;
            }

            line.LastUsedMsec = Time.GetTicksMsec();
            bucket.Enqueue(line);
            Sweep();
        }

        internal bool TryPrewarmOne()
        {
            Sweep();
            foreach (var kind in Enum.GetValues<ModSettingsReusableEntryKind>())
            {
                if (CountRetained(kind) >= WarmRetainedPerKind)
                    continue;

                Return(new(kind));
                return true;
            }

            return false;
        }

        internal void Sweep()
        {
            var now = Time.GetTicksMsec();
            foreach (var pair in _buckets)
            {
                var valid = new List<ModSettingsUiFactory.ReusableSettingLine>(pair.Value.Count);
                while (pair.Value.Count > 0)
                {
                    var line = pair.Value.Dequeue();
                    if (!GodotObject.IsInstanceValid(line))
                        continue;

                    valid.Add(line);
                }

                valid.Sort((left, right) => left.LastUsedMsec.CompareTo(right.LastUsedMsec));
                var kept = new Queue<ModSettingsUiFactory.ReusableSettingLine>(valid.Count);
                var idleOverflow = Math.Max(0, valid.Count - WarmRetainedPerKind);
                for (var i = 0; i < valid.Count; i++)
                {
                    var line = valid[i];
                    var idleExpired = now >= line.LastUsedMsec && now - line.LastUsedMsec >= IdleReleaseMsec;
                    if (i < idleOverflow && idleExpired)
                    {
                        line.QueueFree();
                        continue;
                    }

                    kept.Enqueue(line);
                }

                _buckets[pair.Key] = kept;
            }
        }

        private int CountRetained(ModSettingsReusableEntryKind kind)
        {
            return !_buckets.TryGetValue(kind, out var bucket) ? 0 : bucket.Count(GodotObject.IsInstanceValid);
        }
    }

    internal static partial class ModSettingsUiFactory
    {
        internal sealed partial class ReusableSettingLine : MarginContainer
        {
            private readonly MegaRichTextLabel? _descriptionLabel;
            private readonly MegaRichTextLabel? _label;
            private readonly HBoxContainer? _row;
            private Control? _actionControl;
            private Control? _valueControl;

            public ReusableSettingLine(ModSettingsReusableEntryKind kind)
            {
                Kind = kind;
                Name = $"Reusable{kind}EntryLine";
                SizeFlagsHorizontal = SizeFlags.ExpandFill;
                MouseFilter = MouseFilterEnum.Ignore;

                var lineMargins = RitsuShellThemeLayoutResolver.ResolveEdges("components.entryLine.layout.margin", 8);
                lineMargins = new(
                    RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.margin.left",
                        lineMargins.Left),
                    RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.margin.top", 4),
                    RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.margin.right",
                        lineMargins.Right),
                    RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.margin.bottom", 4));
                AddThemeConstantOverride("margin_left", lineMargins.Left);
                AddThemeConstantOverride("margin_right", lineMargins.Right);
                AddThemeConstantOverride("margin_top", lineMargins.Top);
                AddThemeConstantOverride("margin_bottom", lineMargins.Bottom);

                var surface = new PanelContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                    ClipContents = false,
                };
                surface.AddThemeStyleboxOverride("panel", CreateEntrySurfaceStyle());
                AddChild(surface);

                _row = new()
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    MouseFilter = MouseFilterEnum.Ignore,
                    Alignment = BoxContainer.AlignmentMode.Center,
                };
                _row.AddThemeConstantOverride("separation",
                    RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.rowSeparation", 20));
                surface.AddChild(_row);

                var leftColumn = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                leftColumn.AddThemeConstantOverride("separation",
                    RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.leftColumnSeparation", 5));
                _row.AddChild(leftColumn);

                _label = CreateHeaderLabel(string.Empty, RitsuShellTheme.Current.Metric.FontSize.SettingLineTitle,
                    HorizontalAlignment.Left, null, RitsuShellTheme.Current.Text.RichTitle);
                _label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                leftColumn.AddChild(_label);

                _descriptionLabel = CreateDescriptionLabel(string.Empty);
                leftColumn.AddChild(_descriptionLabel);

                _valueControl = kind switch
                {
                    ModSettingsReusableEntryKind.Toggle => new ModSettingsToggleControl(false, null),
                    ModSettingsReusableEntryKind.TextButton => new ModSettingsTextButton(
                        string.Empty, ModSettingsButtonTone.Normal, null),
                    _ => null,
                };
                if (_valueControl != null)
                    _row.AddChild(_valueControl);
            }

            public ReusableSettingLine()
            {
            }

            internal ModSettingsReusableEntryKind Kind { get; }

            internal ulong LastUsedMsec { get; set; }

            internal int ReuseVersion { get; private set; }

            internal T GetValueControl<T>() where T : Control
            {
                return (T)_valueControl!;
            }

            internal int Bind(ModSettingsUiContext context, Func<string> labelProvider,
                Func<string> descriptionProvider, Control? actionControl,
                ModSettingsText? labelRefreshSource = null, ModSettingsText? descriptionRefreshSource = null)
            {
                ReuseVersion++;
                Visible = true;
                MouseFilter = MouseFilterEnum.Ignore;
                ProcessMode = ProcessModeEnum.Inherit;
                Modulate = Colors.White;

                SetLabel(labelProvider());
                SetDescription(descriptionProvider());

                ReplaceActionControl(actionControl);
                PrepareValueControl();

                var version = ReuseVersion;
                var labelSpec = labelRefreshSource?.GetUiRefreshSpec() ?? ModSettingsUiRefreshSpec.StaticDisplay;
                RegisterRefreshWhenAlive(context, this, () =>
                {
                    if (ReuseVersion != version)
                        return;
                    SetLabel(labelProvider());
                }, labelSpec);
                var descriptionSpec = descriptionRefreshSource?.GetUiRefreshSpec() ??
                                      ModSettingsUiRefreshSpec.StaticDisplay;
                RegisterRefreshWhenAlive(context, this, () =>
                {
                    if (ReuseVersion != version)
                        return;
                    SetDescription(descriptionProvider());
                }, descriptionSpec);
                AttachHostSurfaceReadOnlySync(context, _valueControl!, actionControl, () => ReuseVersion == version);
                return version;
            }

            internal void ReleaseForPool()
            {
                ReuseVersion++;
                this.ReleaseFocusIfInsideTree();
                Visible = false;
                ProcessMode = ProcessModeEnum.Inherit;
                Modulate = Colors.White;
                SetLabel(string.Empty);
                SetDescription(string.Empty);

                switch (_valueControl)
                {
                    case ModSettingsToggleControl toggle:
                        toggle.ClearBinding();
                        break;
                    case ModSettingsTextButton textButton:
                        textButton.ClearAction();
                        break;
                }

                ReplaceActionControl(null);
                if (GetParent() is { } parent)
                    parent.RemoveChild(this);
            }

            private void PrepareValueControl()
            {
                if (_valueControl == null)
                    return;

                _valueControl.CustomMinimumSize = new(
                    Math.Max(EntryControlWidth, _valueControl.CustomMinimumSize.X),
                    Mathf.Max(_valueControl.CustomMinimumSize.Y,
                        RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));
                _valueControl.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
                _valueControl.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                _valueControl.ProcessMode = ProcessModeEnum.Inherit;
                _valueControl.Modulate = Colors.White;
                if (_valueControl.GetParent() != _row)
                    _row?.AddChild(_valueControl);
            }

            private void ReplaceActionControl(Control? next)
            {
                if (_actionControl != null && IsInstanceValid(_actionControl))
                {
                    if (_actionControl.GetParent() == _row)
                        _row.RemoveChild(_actionControl);
                    _actionControl.QueueFree();
                }

                _actionControl = next;
                if (_actionControl == null)
                    return;

                _actionControl.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                _row?.AddChild(_actionControl);
            }

            private void SetLabel(string text)
            {
                _label?.SetTextAutoSize(string.IsNullOrWhiteSpace(text)
                    ? ModSettingsLocalization.Get("entry.label.empty", "—")
                    : text);
            }

            private void SetDescription(string text)
            {
                if (_descriptionLabel == null) return;
                _descriptionLabel.SetTextAutoSize(text);
                _descriptionLabel.Visible = !string.IsNullOrWhiteSpace(text);
            }
        }
    }
}
