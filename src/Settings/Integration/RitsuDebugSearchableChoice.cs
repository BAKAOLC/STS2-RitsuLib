using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed record RitsuDebugSearchableChoiceOption(
        string Id,
        string Label,
        string? SearchText = null);

    // ReSharper disable once Godot.MissingParameterlessConstructor
    internal sealed partial class RitsuDebugSearchableChoice : VBoxContainer
    {
        private const float MaximumListHeight = 220f;
        private readonly VBoxContainer _body;
        private readonly Label _emptyLabel;
        private readonly Button _header;
        private readonly Dictionary<string, RitsuDebugSearchableChoiceOption> _options;
        private readonly Dictionary<string, Button> _optionButtons = new(StringComparer.Ordinal);
        private readonly LineEdit _search;
        private readonly string _title;
        private bool _expanded;

        internal RitsuDebugSearchableChoice(
            string title,
            string searchPlaceholder,
            string emptyText,
            IReadOnlyList<RitsuDebugSearchableChoiceOption> options,
            string? selectedId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(searchPlaceholder);
            ArgumentException.ThrowIfNullOrWhiteSpace(emptyText);
            ArgumentNullException.ThrowIfNull(options);
            if (options.Any(static option => option == null ||
                                             string.IsNullOrWhiteSpace(option.Id) ||
                                             string.IsNullOrWhiteSpace(option.Label)))
                throw new ArgumentException("Searchable choices require non-empty IDs and labels.", nameof(options));
            if (options.Select(static option => option.Id).Distinct(StringComparer.Ordinal).Count() != options.Count)
                throw new ArgumentException("Searchable choice IDs must be unique.", nameof(options));

            _title = title;
            _options = options.ToDictionary(static option => option.Id, StringComparer.Ordinal);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddThemeConstantOverride("separation", 7);

            _header = new ModSettingsMiniButton(string.Empty, ToggleExpanded)
            {
                Alignment = HorizontalAlignment.Left,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new(220f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
            };
            AddChild(_header);

            _body = new()
            {
                Visible = false,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            _body.AddThemeConstantOverride("separation", 7);
            AddChild(_body);

            _search = ModSettingsUiControlTheming.CreateStyledLineEdit(string.Empty, searchPlaceholder);
            _search.ClearButtonEnabled = true;
            _search.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _search.TextChanged += _ => RefreshVisibleOptions();
            _body.AddChild(_search);

            var scroll = new ScrollContainer
            {
                CustomMinimumSize = new(0f, ResolveListHeight(options.Count)),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            };
            ModSettingsUiControlTheming.ApplySettingsScrollContainerThemeForDropdownList(scroll);
            _body.AddChild(scroll);
            var frame = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            scroll.AddChild(frame);
            var list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            list.AddThemeConstantOverride("separation", 5);
            frame.AddChild(list);

            foreach (var option in options)
            {
                var capturedId = option.Id;
                var button = ModSettingsUiControlTheming.CreateCompactSettingsToggleButton(option.Label, false);
                button.CustomMinimumSize = new(0f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight);
                button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                button.TooltipText = $"{option.Label}\n{option.Id}";
                button.Pressed += () => Select(capturedId);
                _optionButtons.Add(option.Id, button);
                list.AddChild(button);
            }

            _emptyLabel = new()
            {
                Text = emptyText,
                Visible = false,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            _emptyLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            _emptyLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.Hint);
            list.AddChild(_emptyLabel);
            SetSelectedId(selectedId);
        }

        internal string? SelectedId { get; private set; }

        internal event Action<string?>? SelectionChanged;

        internal void SetSelectedId(string? selectedId)
        {
            if (selectedId != null && !_options.ContainsKey(selectedId))
                selectedId = null;
            SelectedId = selectedId;
            RefreshSelection();
        }

        private void ToggleExpanded()
        {
            _expanded = !_expanded;
            _body.Visible = _expanded;
            RefreshHeader();
            if (_expanded)
                _search.GrabFocus();
        }

        private void Select(string id)
        {
            var changed = !string.Equals(SelectedId, id, StringComparison.Ordinal);
            SelectedId = id;
            _expanded = false;
            _body.Visible = false;
            _search.Text = string.Empty;
            RefreshSelection();
            if (changed)
                SelectionChanged?.Invoke(SelectedId);
        }

        private void RefreshSelection()
        {
            foreach (var (id, button) in _optionButtons)
            {
                var selected = string.Equals(SelectedId, id, StringComparison.Ordinal);
                button.ButtonPressed = selected;
                ModSettingsUiControlTheming.ApplySettingsToggleButtonStyle(button, selected, false);
                ModSettingsUiControlTheming.RefreshAdaptiveButtonText(button);
            }

            RefreshHeader();
        }

        private void RefreshHeader()
        {
            var selected = SelectedId != null && _options.TryGetValue(SelectedId, out var option)
                ? $" · {option.Label}"
                : $" ({_options.Count})";
            _header.Text = $"{_title}{selected}  {(_expanded ? "▾" : "▸")}";
            _header.TooltipText = SelectedId != null && _options.TryGetValue(SelectedId, out option)
                ? $"{_title}\n{option.Label}\n{option.Id}"
                : _title;
            ModSettingsUiControlTheming.RefreshAdaptiveButtonText(_header);
        }

        private void RefreshVisibleOptions()
        {
            var terms = _search.Text.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var visibleCount = 0;
            foreach (var (id, button) in _optionButtons)
            {
                var option = _options[id];
                var searchText = $"{option.Label} {option.Id} {option.SearchText}";
                var visible = terms.All(term => searchText.Contains(term, StringComparison.CurrentCultureIgnoreCase));
                button.Visible = visible;
                if (visible)
                    visibleCount++;
            }

            _emptyLabel.Visible = visibleCount == 0;
        }

        private static float ResolveListHeight(int optionCount)
        {
            var rowHeight = RitsuShellTheme.Current.Metric.Entry.ValueMinHeight + 5f;
            return Math.Min(MaximumListHeight, Math.Max(rowHeight, optionCount * rowHeight));
        }
    }
}
