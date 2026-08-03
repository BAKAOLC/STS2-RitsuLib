using Godot;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed record RitsuDebugEnchantmentOption(
        string Id,
        string Title,
        Func<Texture2D?> IconFactory);

    internal sealed partial class RitsuDebugEnchantmentPicker : VBoxContainer
    {
        private const float TileMinimumWidth = 80f;
        private const float TileHeight = 94f;
        private const float IconSize = 42f;
        private const float GridGap = 10f;
        private const float MaximumGridHeight = 220f;
        private readonly VBoxContainer _expandedBody;
        private readonly GridContainer _grid;
        private readonly MarginContainer _gridFrame;
        private readonly ScrollContainer _gridScroll;
        private readonly Button _header;
        private readonly Dictionary<string, Button> _tiles = new(StringComparer.Ordinal);
        private readonly string _title;
        private readonly Dictionary<string, string> _titles = new(StringComparer.Ordinal);
        private bool _expanded;
        private bool _gridLayoutQueued;

        internal RitsuDebugEnchantmentPicker(
            string title,
            IReadOnlyList<RitsuDebugEnchantmentOption> options,
            string? selectedId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentNullException.ThrowIfNull(options);
            _title = title;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddThemeConstantOverride("separation", 8);

            _header = new ModSettingsMiniButton(string.Empty, ToggleExpanded)
            {
                Alignment = HorizontalAlignment.Left,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new(0f, 36f),
            };
            AddChild(_header);

            _expandedBody = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Visible = false,
            };
            _expandedBody.AddThemeConstantOverride("separation", 8);
            AddChild(_expandedBody);

            _gridScroll = new()
            {
                CustomMinimumSize = new(0f, ResolveGridHeight(options.Count)),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            };
            ModSettingsUiControlTheming.ApplySettingsScrollContainerThemeForDropdownList(_gridScroll);
            _expandedBody.AddChild(_gridScroll);

            _gridFrame = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            _gridScroll.AddChild(_gridFrame);
            _grid = new()
            {
                Columns = 3,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            _grid.AddThemeConstantOverride("h_separation", Mathf.RoundToInt(GridGap));
            _grid.AddThemeConstantOverride("v_separation", Mathf.RoundToInt(GridGap));
            _gridFrame.AddChild(_grid);

            foreach (var option in options)
            {
                if (option == null || string.IsNullOrWhiteSpace(option.Id) || _tiles.ContainsKey(option.Id))
                    continue;
                _titles.Add(option.Id, option.Title);
                var tile = CreateTile(option);
                _tiles.Add(option.Id, tile);
                _grid.AddChild(tile);
            }

            SelectedId = selectedId != null && _tiles.ContainsKey(selectedId) ? selectedId : null;
            RefreshSelection();
            _gridFrame.Resized += QueueGridLayout;
            _gridScroll.GetVScrollBar().VisibilityChanged += OnScrollbarVisibilityChanged;
        }

        internal string? SelectedId { get; private set; }

        internal event Action<string?>? SelectionChanged;

        public override void _Ready()
        {
            QueueGridLayout();
        }

        internal void AddExpandedControl(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);
            if (control.GetParent() != null)
                throw new InvalidOperationException("Expanded enchantment controls must not already have a parent.");
            _expandedBody.AddChild(control);
        }

        private Button CreateTile(RitsuDebugEnchantmentOption option)
        {
            var tile = new Button
            {
                CustomMinimumSize = new(TileMinimumWidth, TileHeight),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Stop,
                TooltipText = $"{option.Title}\n{option.Id}",
            };
            var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
            margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            margin.AddThemeConstantOverride("margin_left", 6);
            margin.AddThemeConstantOverride("margin_top", 6);
            margin.AddThemeConstantOverride("margin_right", 6);
            margin.AddThemeConstantOverride("margin_bottom", 5);
            tile.AddChild(margin);
            var column = new VBoxContainer
            {
                Alignment = AlignmentMode.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            column.AddThemeConstantOverride("separation", 4);
            margin.AddChild(column);

            Texture2D? icon = null;
            try
            {
                icon = option.IconFactory();
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugToolsUi] Could not load enchantment icon for '{option.Id}': {ex.Message}");
            }

            var image = new TextureRect
            {
                Texture = icon,
                CustomMinimumSize = new(IconSize, IconSize),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
                Visible = icon != null,
            };
            column.AddChild(image);
            var label = new Label
            {
                Text = option.Title,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                CustomMinimumSize = new(0f, 30f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            label.AddThemeFontSizeOverride("font_size", 12);
            label.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            column.AddChild(label);
            tile.Pressed += () => Select(option.Id);
            return tile;
        }

        private void ToggleExpanded()
        {
            _expanded = !_expanded;
            _expandedBody.Visible = _expanded;
            RefreshHeader();
            if (_expanded)
            {
                QueueGridLayout();
                Callable.From(ScrollExpandedBodyIntoView).CallDeferred();
            }
        }

        private void ScrollExpandedBodyIntoView()
        {
            if (!_expanded || !IsInsideTree())
                return;
            for (var ancestor = GetParent(); ancestor != null; ancestor = ancestor.GetParent())
            {
                if (ancestor is not ScrollContainer scroll || !scroll.IsAncestorOf(_expandedBody))
                    continue;
                scroll.EnsureControlVisible(_expandedBody);
                return;
            }
        }

        private void Select(string id)
        {
            if (!_tiles.ContainsKey(id) || string.Equals(SelectedId, id, StringComparison.Ordinal))
                return;
            SelectedId = id;
            RefreshSelection();
            SelectionChanged?.Invoke(SelectedId);
        }

        private void RefreshSelection()
        {
            foreach (var (id, tile) in _tiles)
            {
                var selected = string.Equals(id, SelectedId, StringComparison.Ordinal);
                var normal = RitsuShellChromeStyles.CreateListItemCardStyle(selected);
                var highlighted = RitsuShellChromeStyles.CreateListItemCardStyle(true);
                tile.AddThemeStyleboxOverride("normal", normal);
                tile.AddThemeStyleboxOverride("hover", highlighted);
                tile.AddThemeStyleboxOverride("pressed", highlighted);
                tile.AddThemeStyleboxOverride("focus", highlighted);
                tile.AddThemeStyleboxOverride("disabled", normal);
            }

            RefreshHeader();
        }

        private void RefreshHeader()
        {
            var selection = SelectedId != null && _titles.TryGetValue(SelectedId, out var selectedTitle)
                ? selectedTitle
                : ModSettingsLocalization.Get("ritsulib.debugTools.noEnchantment", "None selected");
            _header.Text = $"{_title} · {selection}  {(_expanded ? "▾" : "▸")}";
            _header.TooltipText = selection;
            ModSettingsUiControlTheming.RefreshAdaptiveButtonText(_header);
        }

        private void QueueGridLayout()
        {
            if (!IsInsideTree() || _gridLayoutQueued)
                return;
            _gridLayoutQueued = true;
            Callable.From(() =>
            {
                _gridLayoutQueued = false;
                UpdateGridColumns();
            }).CallDeferred();
        }

        private void UpdateGridColumns()
        {
            if (!_expanded || !IsInstanceValid(_gridFrame))
                return;
            var width = Math.Max(1f, _gridFrame.Size.X);
            _grid.Columns = Math.Max(1, Mathf.FloorToInt((width + GridGap) / (TileMinimumWidth + GridGap)));
        }

        private void OnScrollbarVisibilityChanged()
        {
            _gridFrame.AddThemeConstantOverride("margin_right",
                _gridScroll.GetVScrollBar().Visible
                    ? ModSettingsUiControlTheming.ResolveSettingsScrollContentRightGutter(_gridScroll)
                    : 0);
            QueueGridLayout();
        }

        private static float ResolveGridHeight(int optionCount)
        {
            var rows = Math.Max(1, Mathf.CeilToInt(optionCount / 3f));
            return Math.Min(MaximumGridHeight, rows * TileHeight + Math.Max(0, rows - 1) * GridGap);
        }
    }
}
