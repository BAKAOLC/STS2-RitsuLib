using Godot;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Overlay;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Files
{
    public sealed partial class RitsuFileDialog
    {
        private readonly List<ModSettingsTextButton> _buttons = [];
        private readonly List<PanelContainer> _panels = [];
        private readonly List<(SplitContainer Split, Label Grip)> _splits = [];
        private ColorRect _backdrop = null!;
        private Texture2D? _fileIcon;
        private Texture2D? _folderIcon;
        private Texture2D? _refreshIcon;
        private Texture2D? _favoriteIcon;
        private readonly List<ModSettingsTextButton> _iconButtons = [];

        private void BuildLayout()
        {
            var preferences = RitsuFileDialogPreferences.Current;
            _fileIcon = LoadIcon("images/file.svg");
            _folderIcon = LoadIcon("images/folder.svg");
            _refreshIcon = LoadIcon("images/refresh.svg");
            _favoriteIcon = LoadIcon("images/favorite.svg");
            _backdrop = new() { MouseFilter = MouseFilterEnum.Stop };
            _backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(_backdrop);
            _window = new(new()
            {
                Title = _options.Title ?? Text(_options.Mode switch
                {
                    FileDialog.FileModeEnum.OpenDir => "title.directory",
                    FileDialog.FileModeEnum.SaveFile => "title.save",
                    FileDialog.FileModeEnum.OpenFiles => "title.multiple",
                    FileDialog.FileModeEnum.OpenAny => "title.any",
                    _ => "title.file",
                }),
                InitialSize = GetViewportRect().Size * 0.74f,
                MinimumSize = new(820, 560),
                TitleFontSize = 20,
                FitInitialSizeToContent = false,
                Movable = true,
                Resizable = true,
            });
            _window.Closed += (_, _) => Cancel();
            _window.Resized += () => Callable.From(RebuildFocusGraph).CallDeferred();
            var body = new VBoxContainer
                { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            body.AddThemeConstantOverride("separation", 10);
            _window.SetContent(body);
            AddChild(_window);

            var toolbar = Row(body);
            _back = IconButton(toolbar, Glyph(RitsuDebugToolsGlyph.ChevronLeft), () => TraverseHistory(-1), "back");
            _forward = IconButton(toolbar, Glyph(RitsuDebugToolsGlyph.ChevronRight), () => TraverseHistory(1),
                "forward");
            IconButton(toolbar, Glyph(RitsuDebugToolsGlyph.ChevronUp), GoUp, "up");
            toolbar.AddChild(Label("path"));
            var drives = DriveInfo.GetDrives().Select(drive => (Value: drive.Name, Label: drive.Name)).ToArray();
            _drives = new(drives, drives.FirstOrDefault().Value ?? "", value => Navigate(value))
            {
                CustomMinimumSize = new(80, 36),
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                TooltipText = Text("driveHint"),
            };
            toolbar.AddChild(_drives);
            _path = Edit("", "path", 120);
            toolbar.AddChild(_path);
            _path.TextSubmitted += value => Navigate(value);
            _regions.Add(_path);
            IconButton(toolbar, _refreshIcon, () => Navigate(_directory, _historyPosition), "refresh");
            _favorite = IconButton(toolbar, _favoriteIcon, ToggleFavorite, "favorite");
            if (_options.Mode is not (FileDialog.FileModeEnum.OpenFile or FileDialog.FileModeEnum.OpenFiles))
                IconButton(toolbar, Glyph(RitsuDebugToolsGlyph.Plus), CreateDirectoryPrompt, "newFolder");

            var split = new HSplitContainer
                { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            body.AddChild(split);
            AddSplitGrip(split);
            var sidebar = new VSplitContainer { CustomMinimumSize = new(240, 0) };
            split.AddChild(sidebar);
            AddSplitGrip(sidebar);
            var favoriteBox = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
            sidebar.AddChild(favoriteBox);
            var favoriteHeader = Row(favoriteBox);
            favoriteHeader.AddChild(Label("favorites", true));
            IconButton(favoriteHeader, Glyph(RitsuDebugToolsGlyph.ChevronUp), () => MoveFavorite(-1), "favoriteUp");
            IconButton(favoriteHeader, Glyph(RitsuDebugToolsGlyph.ChevronDown), () => MoveFavorite(1), "favoriteDown");
            Button(favoriteHeader, "…", () => ShowFavoriteActions(_favorites.Cursor), 44, "actions");
            _favorites = new() { Name = "Favorites" };
            _favorites.SetGrid(false);
            favoriteBox.AddChild(_favorites);
            _favorites.ItemSelected += index => NavigateFavorite((int)index);
            _favorites.ItemActivated += index => NavigateFavorite((int)index);
            _favorites.ItemClicked += (index, _, button) =>
            {
                if (button == (long)MouseButton.Right)
                    ShowFavoriteActions((int)index);
            };
            _regions.Add(_favorites);
            var recentBox = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
            sidebar.AddChild(recentBox);
            recentBox.AddChild(Label("recent"));
            _recent = new() { Name = "RecentDirectories" };
            _recent.SetGrid(false);
            recentBox.AddChild(_recent);
            _recent.ItemSelected += index => NavigateRecent((int)index);
            _recent.ItemActivated += index => NavigateRecent((int)index);
            _regions.Add(_recent);

            var fileBox = new VBoxContainer
                { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            split.AddChild(fileBox);
            var tools = Row(fileBox);
            tools.AddChild(Label("contents", true));
            _hidden = Button(tools, Text("hidden"), ToggleHidden, 56, "hiddenHint");
            _grid = Button(tools, Text("grid"), () => SetDisplayMode(true), 48, "gridHint");
            _list = Button(tools, Text("list"), () => SetDisplayMode(false), 48, "listHint");
            _filterToggle = Button(tools, Text("filter"), ToggleNameFilter, 56, "filterHint");
            var sort = new ModSettingsDropdownChoiceControl<int>(
                [.. Enumerable.Range(0, 6).Select(index => (index, Text("sort." + index)))],
                preferences.SortOrder, index =>
                {
                    preferences.SortOrder = index;
                    preferences.Save();
                    PopulateFiles();
                }) { CustomMinimumSize = new(150, 36), TooltipText = Text("sortHint") };
            tools.AddChild(sort);
            Button(tools, "…", () => ShowFileActions(_files.GetSelectedItems().FirstOrDefault(-1)), 44, "actions");
            _files = new()
            {
                Name = "Files",
                SelectMode = _options.Mode == FileDialog.FileModeEnum.OpenFiles
                    ? ItemList.SelectModeEnum.Multi
                    : ItemList.SelectModeEnum.Single,
            };
            _files.SetGrid(preferences.Thumbnails);
            fileBox.AddChild(_files);
            _files.ItemSelected += index => OnSelection((int)index);
            _files.MultiSelected += (index, _) => OnSelection((int)index);
            _files.ItemActivated += index => ActivateEntry((int)index);
            _files.ControllerActivated += index => ActivateEntry(index, true);
            _files.ItemClicked += (index, _, button) =>
            {
                if (button == (long)MouseButton.Right)
                    ShowFileActions((int)index);
            };
            _files.EmptyClicked += (_, button) =>
            {
                if (button == (long)MouseButton.Right)
                    ShowFileActions(-1);
            };
            _regions.Add(_files);
            _nameFilterRow = Row(fileBox);
            _nameFilterRow.AddChild(Label("filter"));
            _nameFilter = Edit("", "filter", 80);
            _nameFilter.ClearButtonEnabled = true;
            _nameFilterRow.AddChild(_nameFilter);
            _nameFilter.TextChanged += _ => PopulateFiles();
            _nameFilterRow.Visible = preferences.ShowNameFilter;
            var nameRow = Row(fileBox);
            nameRow.Visible = _options.Mode != FileDialog.FileModeEnum.OpenDir;
            nameRow.AddChild(Label("file"));
            _filename = Edit(_options.InitialFile, "filePlaceholder", 120);
            nameRow.AddChild(_filename);
            _filename.TextSubmitted += _ => ConfirmSelection();
            _filename.TextChanged += _ => RefreshAccept();
            _regions.Add(_filename);
            _filterPatterns =
            [
                .. _options.Filters.Select(filter => filter.Split(';')[0].Split(',',
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)),
            ];
            var choices = new List<(int Value, string Label)>();
            if (_filterPatterns.Length > 1)
                choices.Add((-1, Text("allRecognized")));
            for (var i = 0; i < _options.Filters.Count; i++)
            {
                var parts = _options.Filters[i].Split(';', 2);
                choices.Add((i, parts.Length > 1 ? parts[1].Trim() + " (" + parts[0].Trim() + ")" : parts[0]));
            }

            choices.Add((-2, Text("allFiles")));
            _filterIndex = choices[0].Value;
            var filters = new ModSettingsDropdownChoiceControl<int>(choices, _filterIndex, index =>
            {
                _filterIndex = index;
                PopulateFiles();
            })
            {
                CustomMinimumSize = new(190, 36), SizeFlagsHorizontal = SizeFlags.ExpandFill,
                TooltipText = Text("fileTypeHint"),
            };
            nameRow.AddChild(filters);
            _regions.Add(filters);
            _status = Label("loading");
            _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            body.AddChild(_status);
            var footer = Row(body);
            _inputHint = Label("keyboardHint", true);
            footer.AddChild(_inputHint);
            InitializeInputMode();
            Button(footer, Text("cancel"), Cancel, 100);
            _accept = Button(footer, Text(_options.Mode switch
            {
                FileDialog.FileModeEnum.SaveFile => "save",
                FileDialog.FileModeEnum.OpenDir => "selectFolder",
                _ => "open",
            }), ConfirmSelection, 130);
            _regions.Add(_accept);
            RefreshSidebar();
            RefreshToggleButtons();
        }

        private static HBoxContainer Row(Node parent)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("separation", 6);
            parent.AddChild(row);
            return row;
        }

        private Label Label(string key, bool expand = false)
        {
            var label = new Label
            {
                Text = Text(key),
                SizeFlagsHorizontal = expand ? SizeFlags.ExpandFill : SizeFlags.Fill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                TextOverrunBehavior =
                    expand ? TextServer.OverrunBehavior.TrimEllipsis : TextServer.OverrunBehavior.NoTrimming,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _labels.Add(label);
            return label;
        }

        private LineEdit Edit(string value, string placeholder, float width)
        {
            var edit = ModSettingsUiControlTheming.CreateStyledLineEdit(value, Text(placeholder), width, 44, 14);
            edit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            edit.TooltipText = Text(placeholder switch
            {
                "path" => "pathHint",
                "filter" => "filterHint",
                _ => placeholder,
            });
            edit.FocusExited += CloseKeyboard;
            _edits.Add(edit);
            return edit;
        }

        private ModSettingsTextButton Button(Node parent, string text, Action action, float width,
            string? tooltip = null)
        {
            var button = new ModSettingsTextButton(text, ModSettingsButtonTone.Normal, action)
            {
                CustomMinimumSize = new(Math.Max(width, RitsuShellTheme.Current.Font.BodyBold.GetStringSize(text,
                    fontSize: 14).X + 32), 44),
                TooltipText = tooltip == null ? text : Text(tooltip),
            };
            _buttons.Add(button);
            parent.AddChild(button);
            return button;
        }

        private void ApplyTheme()
        {
            if (_closing || !_attached)
                return;
            _backdrop.Color = RitsuShellTheme.Current.Color.ModalBackdrop;
            RitsuShellTooltipTheme.ApplyToTreeRoot(this);
            foreach (var label in _labels.Where(IsInstanceValid))
            {
                label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
                label.AddThemeFontSizeOverride("font_size", 14);
                label.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            }

            foreach (var edit in _edits.Where(IsInstanceValid))
            {
                ModSettingsUiControlTheming.ApplyEntryLineEditValueFieldTheme(edit,
                    RitsuShellTheme.Current.Font.BodyBold, 14);
                ModSettingsUiControlTheming.ApplyPopupMenuListTheme(edit.GetMenu(), 14);
                edit.AddThemeIconOverride("clear", Glyph(RitsuDebugToolsGlyph.Close));
            }

            foreach (var button in _buttons.Where(IsInstanceValid))
            {
                button.Notification((int)NotificationThemeChanged);
                button.AddThemeFontSizeOverride("font_size", 14);
            }

            foreach (var panel in _panels.Where(IsInstanceValid))
                panel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateSurfaceStyle());
            foreach (var (split, grip) in _splits)
            {
                var style = (StyleBoxFlat)RitsuShellChromeStyles.CreateInsetSurfaceStyle().Duplicate();
                style.BgColor = RitsuShellTheme.Current.Component.DragHandle.Default.Bg;
                style.BorderColor = RitsuShellTheme.Current.Component.DragHandle.Default.Border;
                split.AddThemeStyleboxOverride("split_bar_background", style);
                grip.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
                grip.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Grip);
                grip.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.Grip);
            }

            _files.ApplyTheme();
            _favorites.ApplyTheme();
            _recent.ApplyTheme();
            ApplyIconButtonStyles();
        }

        private ModSettingsTextButton IconButton(Node parent, Texture2D? icon, Action action, string tooltip)
        {
            var button = Button(parent, "", action, 44, tooltip);
            button.Icon = icon;
            button.IconAlignment = HorizontalAlignment.Center;
            button.ExpandIcon = true;
            button.AddThemeConstantOverride("icon_max_width", 20);
            _iconButtons.Add(button);
            return button;
        }

        private void AddSplitGrip(SplitContainer split)
        {
            split.DraggerVisibility = SplitContainer.DraggerVisibilityEnum.Hidden;
            var grip = new Label
            {
                Text = split.Vertical ? "::::" : ":\n:\n:",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            split.GetDragAreaControl().AddChild(grip);
            grip.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _splits.Add((split, grip));
        }

        private void ApplyIconButtonStyles()
        {
            foreach (var button in _iconButtons)
            {
                foreach (var state in new[] { "normal", "hover", "pressed", "focus", "disabled" })
                {
                    var style = (StyleBox)button.GetThemeStylebox(state).Duplicate();
                    style.ContentMarginLeft = 4;
                    style.ContentMarginRight = 4;
                    button.AddThemeStyleboxOverride(state, style);
                }
            }
        }

        private static Texture2D? Glyph(RitsuDebugToolsGlyph glyph) =>
            RitsuDebugToolsIcons.Get(glyph, 20, Colors.White);

        private static ImageTexture LoadIcon(string path)
        {
            using var image = new Image();
            var error = image.LoadSvgFromString(RitsuAssetStore.Pack.ReadAllText(path), 2f);
            if (error != Error.Ok)
                throw new InvalidDataException($"Could not load file picker icon '{path}': {error}");
            return ImageTexture.CreateFromImage(image);
        }
    }
}
