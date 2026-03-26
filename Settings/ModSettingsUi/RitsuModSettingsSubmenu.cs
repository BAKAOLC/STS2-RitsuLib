using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace STS2RitsuLib.Settings
{
    public partial class RitsuModSettingsSubmenu : NSubmenu
    {
        private const float SidebarWidth = 324f;
        private const double AutosaveDelaySeconds = 0.35;
        private const int ScrollContentRightGutter = 12;

        private readonly HashSet<IModSettingsBinding> _dirtyBindings = [];
        private readonly HashSet<string> _expandedModIds = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ModSettingsSidebarButton> _modButtons =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ModSettingsSidebarButton> _pageButtons =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly List<Action> _refreshActions = [];

        private readonly Dictionary<string, ModSettingsSidebarButton> _sectionButtons =
            new(StringComparer.OrdinalIgnoreCase);

        private VBoxContainer _contentList = null!;
        private bool _focusSelectedPageButtonOnNextRefresh;
        private Control? _initialFocusedControl;
        private bool _localeSubscribed;
        private VBoxContainer _modButtonList = null!;
        private HBoxContainer _pageTabRow = null!;
        private AcceptDialog? _pasteErrorDialog;
        private bool _refreshQueued;
        private double _saveTimer = -1;
        private ScrollContainer _scrollContainer = null!;
        private string? _selectedModId;
        private string? _selectedPageId;
        private string? _selectedSectionId;
        private MegaRichTextLabel _subtitleLabel;
        private bool _suppressScrollSync;
        private MegaRichTextLabel _titleLabel;

        public RitsuModSettingsSubmenu()
        {
            AnchorRight = 1f;
            AnchorBottom = 1f;
            GrowHorizontal = GrowDirection.Both;
            GrowVertical = GrowDirection.Both;

            var frame = new MarginContainer
            {
                Name = "Frame",
                AnchorRight = 1f,
                AnchorBottom = 1f,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            frame.AddThemeConstantOverride("margin_left", 160);
            frame.AddThemeConstantOverride("margin_top", 72);
            frame.AddThemeConstantOverride("margin_right", 160);
            frame.AddThemeConstantOverride("margin_bottom", 72);
            AddChild(frame);

            var root = new VBoxContainer
            {
                Name = "Root",
                AnchorRight = 1f,
                AnchorBottom = 1f,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddThemeConstantOverride("separation", 18);
            frame.AddChild(root);

            var header = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            header.AddThemeConstantOverride("separation", 6);
            root.AddChild(header);

            _titleLabel = CreateTitleLabel(32, HorizontalAlignment.Left);
            _titleLabel.CustomMinimumSize = new(0f, 42f);
            _titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            header.AddChild(_titleLabel);

            _subtitleLabel = CreateTitleLabel(16, HorizontalAlignment.Left);
            _subtitleLabel.CustomMinimumSize = new(0f, 24f);
            _subtitleLabel.Modulate = new(0.82f, 0.79f, 0.72f, 0.92f);
            _subtitleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            header.AddChild(_subtitleLabel);

            var body = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            body.AddThemeConstantOverride("separation", 20);
            root.AddChild(body);

            body.AddChild(CreateSidebarPanel());
            body.AddChild(CreateContentPanel());
        }

        protected override Control? InitialFocusedControl => _initialFocusedControl;

        public override void _Ready()
        {
            var backButton = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/back_button"))
                .Instantiate<Control>();
            backButton.Name = "BackButton";
            AddChild(backButton);

            ConnectSignals();
            _scrollContainer.GetVScrollBar().ValueChanged += OnContentScrollChanged;
            SubscribeLocaleChanges();
            Rebuild();
            ProcessMode = ProcessModeEnum.Disabled;
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            FlushDirtyBindings();
            UnsubscribeLocaleChanges();
        }

        public override void OnSubmenuOpened()
        {
            base.OnSubmenuOpened();
            ProcessMode = ProcessModeEnum.Inherit;
            Rebuild();
        }

        public override void OnSubmenuClosed()
        {
            FlushDirtyBindings();
            ProcessMode = ProcessModeEnum.Disabled;
            base.OnSubmenuClosed();
        }

        protected override void OnSubmenuHidden()
        {
            FlushDirtyBindings();
            ProcessMode = ProcessModeEnum.Disabled;
            base.OnSubmenuHidden();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (_saveTimer < 0)
                return;

            _saveTimer -= delta;
            if (_saveTimer <= 0)
                FlushDirtyBindings();
        }

        internal void MarkDirty(IModSettingsBinding binding)
        {
            _dirtyBindings.Add(binding);
            _saveTimer = AutosaveDelaySeconds;
        }

        internal void RequestRefresh()
        {
            if (_refreshQueued)
                return;

            _refreshQueued = true;
            Callable.From(FlushRefreshActions).CallDeferred();
        }

        internal void RegisterRefreshAction(Action action)
        {
            _refreshActions.Add(action);
        }

        internal void ShowPasteFailure(ModSettingsPasteFailureReason reason)
        {
            if (reason == ModSettingsPasteFailureReason.None)
                return;

            var key = reason switch
            {
                ModSettingsPasteFailureReason.ClipboardEmpty => "clipboard.pasteFailedEmpty",
                ModSettingsPasteFailureReason.PasteRuleDenied => "clipboard.pasteFailedBlocked",
                _ => "clipboard.pasteFailedIncompatible",
            };

            var fallback = reason switch
            {
                ModSettingsPasteFailureReason.ClipboardEmpty => "Clipboard is empty or unavailable.",
                ModSettingsPasteFailureReason.PasteRuleDenied => "Paste was blocked by a custom rule.",
                _ => "Clipboard contents are not compatible with this setting.",
            };

            EnsurePasteErrorDialog();
            _pasteErrorDialog!.Title =
                ModSettingsLocalization.Get("clipboard.pasteFailedTitle", "Paste failed");
            _pasteErrorDialog.OkButtonText = ModSettingsLocalization.Get("clipboard.pasteErrorOk", "OK");
            _pasteErrorDialog.DialogText = ModSettingsLocalization.Get(key, fallback);
            _pasteErrorDialog.PopupCentered();
        }

        private void EnsurePasteErrorDialog()
        {
            if (_pasteErrorDialog != null)
                return;

            _pasteErrorDialog = new() { Name = "PasteErrorDialog" };
            AddChild(_pasteErrorDialog);
        }

        private void FlushRefreshActions()
        {
            _refreshQueued = false;
            foreach (var action in _refreshActions.ToArray())
                action();
        }

        public void SelectMod(string modId, string? pageId = null)
        {
            _selectedModId = modId;
            _selectedPageId = pageId;
            _selectedSectionId = null;
            ExpandOnlyMod(modId);
            _focusSelectedPageButtonOnNextRefresh = true;
            Rebuild();
        }

        public void NavigateToPage(string pageId)
        {
            if (string.IsNullOrWhiteSpace(_selectedModId))
                return;

            _selectedPageId = pageId;
            _selectedSectionId = null;
            Rebuild();
        }

        public void NavigateToSection(string pageId, string sectionId)
        {
            if (string.IsNullOrWhiteSpace(_selectedModId))
                return;

            _selectedPageId = pageId;
            _selectedSectionId = sectionId;
            Rebuild();
        }

        private Control CreateSidebarPanel()
        {
            var panel = new Panel
            {
                CustomMinimumSize = new(SidebarWidth, 0f),
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new(0.10f, 0.115f, 0.145f, 0.96f)));

            var frame = new MarginContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            frame.AddThemeConstantOverride("margin_left", 16);
            frame.AddThemeConstantOverride("margin_top", 16);
            frame.AddThemeConstantOverride("margin_right", 16);
            frame.AddThemeConstantOverride("margin_bottom", 16);
            panel.AddChild(frame);

            var root = new VBoxContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddThemeConstantOverride("separation", 14);
            frame.AddChild(root);

            var headerCard = new PanelContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            headerCard.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateInsetSurfaceStyle());
            root.AddChild(headerCard);

            var headerBox = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            headerBox.AddThemeConstantOverride("separation", 4);
            headerCard.AddChild(headerBox);

            var headerTitle =
                ModSettingsUiFactory.CreateSectionTitle(ModSettingsLocalization.Get("sidebar.title", "Mods"));
            headerTitle.CustomMinimumSize = new(0f, 30f);
            headerBox.AddChild(headerTitle);

            headerBox.AddChild(ModSettingsUiFactory.CreateInlineDescription(
                ModSettingsLocalization.Get("sidebar.subtitle", "Browse mods, pages, and sections.")));

            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            };
            root.AddChild(scroll);

            var sidebarScrollFrame = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            sidebarScrollFrame.AddThemeConstantOverride("margin_right", ScrollContentRightGutter);
            scroll.AddChild(sidebarScrollFrame);

            _modButtonList = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _modButtonList.AddThemeConstantOverride("separation", 12);
            sidebarScrollFrame.AddChild(_modButtonList);
            return panel;
        }

        private Control CreateContentPanel()
        {
            var panel = new Panel
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new(0.08f, 0.095f, 0.125f, 0.98f)));

            var frame = new MarginContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            frame.AddThemeConstantOverride("margin_left", 18);
            frame.AddThemeConstantOverride("margin_top", 18);
            frame.AddThemeConstantOverride("margin_right", 18);
            frame.AddThemeConstantOverride("margin_bottom", 18);
            panel.AddChild(frame);

            var root = new VBoxContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddThemeConstantOverride("separation", 10);
            frame.AddChild(root);

            _pageTabRow = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _pageTabRow.AddThemeConstantOverride("separation", 8);
            root.AddChild(_pageTabRow);

            _scrollContainer = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            };
            root.AddChild(_scrollContainer);

            var contentScrollFrame = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            contentScrollFrame.AddThemeConstantOverride("margin_right", ScrollContentRightGutter);
            _scrollContainer.AddChild(contentScrollFrame);

            _contentList = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _contentList.AddThemeConstantOverride("separation", 8);
            contentScrollFrame.AddChild(_contentList);

            return panel;
        }

        private void Rebuild()
        {
            ApplyStaticTexts();
            RebuildSidebar();
            RebuildContent();
        }

        private void RebuildSidebar()
        {
            _modButtonList.FreeChildren();
            _modButtons.Clear();
            _pageButtons.Clear();
            _sectionButtons.Clear();

            var rootPages = ModSettingsRegistry.GetPages()
                .Where(page => string.IsNullOrWhiteSpace(page.ParentPageId))
                .GroupBy(page => page.ModId, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Min(page => page.SortOrder))
                .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (rootPages.Length == 0)
            {
                _selectedModId = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectedModId) || rootPages.All(group =>
                    !string.Equals(group.Key, _selectedModId, StringComparison.OrdinalIgnoreCase)))
                _selectedModId = rootPages[0].Key;

            ExpandOnlyMod(_selectedModId);

            foreach (var group in rootPages)
            {
                var modId = group.Key;
                var pages = ModSettingsRegistry.GetPages()
                    .Where(page => string.Equals(page.ModId, modId, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(page => page.SortOrder)
                    .ThenBy(page => page.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var section = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                section.AddThemeConstantOverride("separation", 8);

                var card = new PanelContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                card.AddThemeStyleboxOverride("panel", CreateSidebarGroupStyle(
                    string.Equals(modId, _selectedModId, StringComparison.OrdinalIgnoreCase)));
                section.AddChild(card);

                var cardContent = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                cardContent.AddThemeConstantOverride("separation", 8);
                card.AddChild(cardContent);

                var button = ModSettingsUiFactory.CreateSidebarButton(
                    ResolveSidebarModTitle(group.ToArray()),
                    () =>
                    {
                        _selectedModId = modId;
                        _selectedPageId = pages.FirstOrDefault(page => string.IsNullOrWhiteSpace(page.ParentPageId))
                            ?.Id;
                        _selectedSectionId = null;
                        ExpandOnlyMod(modId);
                        _focusSelectedPageButtonOnNextRefresh = true;
                        Rebuild();
                    },
                    ModSettingsSidebarItemKind.ModGroup,
                    _expandedModIds.Contains(modId) ? "▼" : "▶");
                button.Name = $"Mod_{modId}";
                cardContent.AddChild(button);

                var isExpanded = _expandedModIds.Contains(modId);
                if (isExpanded)
                {
                    var meta = ModSettingsUiFactory.CreateInlineDescription(string.Format(
                        ModSettingsLocalization.Get("sidebar.modMeta", "{0} pages"),
                        pages.Length));
                    cardContent.AddChild(meta);

                    var navStack = new VBoxContainer
                    {
                        SizeFlagsHorizontal = SizeFlags.ExpandFill,
                        MouseFilter = MouseFilterEnum.Ignore,
                    };
                    navStack.AddThemeConstantOverride("separation", 6);
                    cardContent.AddChild(navStack);

                    foreach (var page in pages.Where(page => string.IsNullOrWhiteSpace(page.ParentPageId)))
                        navStack.AddChild(CreateSidebarPageTreeButton(pages, page, 1));
                }

                _modButtonList.AddChild(section);
                _modButtons[modId] = button;
            }
        }

        private void RebuildContent()
        {
            _pageTabRow.FreeChildren();
            _pageTabRow.Visible = false;
            _contentList.FreeChildren();
            _refreshActions.Clear();

            foreach (var pair in _modButtons)
                pair.Value.SetSelected(string.Equals(pair.Key, _selectedModId, StringComparison.OrdinalIgnoreCase));

            foreach (var pair in _pageButtons)
                pair.Value.SetSelected(string.Equals(pair.Key, _selectedPageId, StringComparison.OrdinalIgnoreCase));

            foreach (var pair in _sectionButtons)
                pair.Value.SetSelected(string.Equals(pair.Key, _selectedSectionId, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(_selectedModId))
            {
                _contentList.AddChild(CreateEmptyStateLabel(ModSettingsLocalization.Get("empty.none",
                    "No mod settings pages are currently registered.")));
                RefreshFocusNavigation();
                return;
            }

            var rootPages = ModSettingsRegistry.GetPages()
                .Where(page => string.Equals(page.ModId, _selectedModId, StringComparison.OrdinalIgnoreCase) &&
                               string.IsNullOrWhiteSpace(page.ParentPageId))
                .OrderBy(page => page.SortOrder)
                .ThenBy(page => page.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (rootPages.Length == 0)
            {
                _contentList.AddChild(CreateEmptyStateLabel(ModSettingsLocalization.Get("empty.mod",
                    "This mod does not currently expose a settings page.")));
                RefreshFocusNavigation();
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectedPageId) ||
                (rootPages.All(page => !string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase)) &&
                 ModSettingsRegistry.GetPages().All(page =>
                     !string.Equals(page.ModId, _selectedModId, StringComparison.OrdinalIgnoreCase) ||
                     !string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase))))
                _selectedPageId = rootPages[0].Id;

            var pageToRender = ResolveSelectedPage();
            if (pageToRender == null)
            {
                _contentList.AddChild(CreateEmptyStateLabel(ModSettingsLocalization.Get("empty.page",
                    "The selected settings page could not be found.")));
                RefreshFocusNavigation();
                return;
            }

            var context = new ModSettingsUiContext(this);
            var isChildPage = !string.IsNullOrWhiteSpace(pageToRender.ParentPageId);
            Action onBack = isChildPage
                ? () =>
                {
                    _selectedPageId = pageToRender.ParentPageId!;
                    RebuildContent();
                }
                : static () => { };

            _pageTabRow.Visible = true;
            var pageHeader = ModSettingsUiFactory.CreateModSettingsPageHeaderBar(context, pageToRender, isChildPage,
                onBack);
            pageHeader.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _pageTabRow.AddChild(pageHeader);

            _contentList.AddChild(ModSettingsUiFactory.CreatePageContent(context, pageToRender));
            RefreshFocusNavigation();
            Callable.From(ScrollToSelectedAnchor).CallDeferred();
        }

        private Control CreateSidebarPageTreeButton(IReadOnlyList<ModSettingsPage> pages, ModSettingsPage page,
            int depth)
        {
            var button = ModSettingsUiFactory.CreateSidebarButton(
                ResolvePageTabTitle(page), () =>
                {
                    _selectedModId = page.ModId;
                    _selectedPageId = page.Id;
                    _selectedSectionId = null;
                    ExpandOnlyMod(page.ModId);
                    Rebuild();
                },
                ModSettingsSidebarItemKind.Page,
                "◦",
                Math.Max(0, depth - 1));
            button.CustomMinimumSize = new(0f, 48f);
            button.SetSelected(string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase));
            _pageButtons[page.Id] = button;

            var container = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            container.AddThemeConstantOverride("separation", 4);
            container.AddChild(button);

            if (string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase))
            {
                var sectionRail = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                sectionRail.AddThemeConstantOverride("separation", 4);
                foreach (var section in page.Sections)
                {
                    var sectionButton = ModSettingsUiFactory.CreateSidebarButton(ResolveSectionTitle(section), () =>
                        {
                            _selectedModId = page.ModId;
                            NavigateToSection(page.Id, section.Id);
                        },
                        ModSettingsSidebarItemKind.Section,
                        "·",
                        depth + 1);
                    sectionButton.CustomMinimumSize = new(0f, 40f);
                    sectionButton.SetSelected(string.Equals(section.Id, _selectedSectionId,
                        StringComparison.OrdinalIgnoreCase));
                    _sectionButtons[section.Id] = sectionButton;
                    sectionRail.AddChild(sectionButton);
                }

                container.AddChild(sectionRail);
            }

            foreach (var child in pages.Where(candidate =>
                             string.Equals(candidate.ParentPageId, page.Id, StringComparison.OrdinalIgnoreCase))
                         .OrderBy(candidate => candidate.SortOrder)
                         .ThenBy(candidate => candidate.Id, StringComparer.OrdinalIgnoreCase))
                container.AddChild(CreateSidebarPageTreeButton(pages, child, depth + 1));

            return container;
        }

        private ModSettingsPage? ResolveSelectedPage()
        {
            return ModSettingsRegistry.GetPages().FirstOrDefault(page =>
                string.Equals(page.ModId, _selectedModId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase));
        }

        private static string ResolvePageTabTitle(ModSettingsPage page)
        {
            return ModSettingsLocalization.ResolvePageDisplayName(page);
        }

        private static string ResolveSidebarModTitle(IReadOnlyList<ModSettingsPage> pages)
        {
            var modId = pages[0].ModId;
            return ModSettingsLocalization.ResolveModName(modId, modId);
        }

        private static string ResolveSectionTitle(ModSettingsSection section)
        {
            return section.Title?.Resolve() ?? ModSettingsLocalization.Get("section.default", "Section");
        }

        private void ScrollToSelectedAnchor()
        {
            _suppressScrollSync = true;
            if (!string.IsNullOrWhiteSpace(_selectedSectionId))
                if (_contentList.FindChild($"Section_{_selectedSectionId}", true, false) is Control target)
                {
                    _scrollContainer.ScrollVertical = Mathf.RoundToInt(target.GlobalPosition.Y -
                        _scrollContainer.GlobalPosition.Y + _scrollContainer.ScrollVertical - 12f);
                    Callable.From(() => _suppressScrollSync = false).CallDeferred();
                    return;
                }

            _scrollContainer.ScrollVertical = 0;
            Callable.From(() => _suppressScrollSync = false).CallDeferred();
        }

        private void OnContentScrollChanged(double value)
        {
            if (_suppressScrollSync)
                return;

            var page = ResolveSelectedPage();
            if (page == null || page.Sections.Count == 0)
                return;

            var viewportTop = _scrollContainer.GlobalPosition.Y + 24f;
            var bestSectionId = page.Sections[0].Id;
            var bestDistance = float.MaxValue;

            foreach (var section in page.Sections)
            {
                if (_contentList.FindChild($"Section_{section.Id}", true, false) is not Control target)
                    continue;

                var distance = MathF.Abs(target.GlobalPosition.Y - viewportTop);
                if (!(distance < bestDistance)) continue;
                bestDistance = distance;
                bestSectionId = section.Id;
            }

            if (string.Equals(bestSectionId, _selectedSectionId, StringComparison.OrdinalIgnoreCase))
                return;

            _selectedSectionId = bestSectionId;
            foreach (var pair in _sectionButtons)
                pair.Value.SetSelected(string.Equals(pair.Key, _selectedSectionId, StringComparison.OrdinalIgnoreCase));
        }

        private void RefreshFocusNavigation()
        {
            var controls = new List<Control>();
            CollectFocusableControls(this, controls);
            _initialFocusedControl = ResolvePreferredFocusControl() ?? controls.FirstOrDefault();

            for (var index = 0; index < controls.Count; index++)
            {
                var current = controls[index];
                current.FocusNeighborLeft = current.GetPath();
                current.FocusNeighborRight = current.GetPath();
                current.FocusNeighborTop = (index > 0 ? controls[index - 1] : current).GetPath();
                current.FocusNeighborBottom = (index < controls.Count - 1 ? controls[index + 1] : current).GetPath();
            }

            Callable.From(() => _initialFocusedControl?.GrabFocus()).CallDeferred();
        }

        private Control? ResolvePreferredFocusControl()
        {
            if (!_focusSelectedPageButtonOnNextRefresh)
                return null;

            _focusSelectedPageButtonOnNextRefresh = false;
            if (!string.IsNullOrWhiteSpace(_selectedPageId)
                && _pageButtons.TryGetValue(_selectedPageId, out var pageButton)
                && pageButton.Visible)
                return pageButton;

            if (!string.IsNullOrWhiteSpace(_selectedModId)
                && _modButtons.TryGetValue(_selectedModId, out var modButton)
                && modButton.Visible)
                return modButton;

            return null;
        }

        private static void CollectFocusableControls(Node node, ICollection<Control> controls)
        {
            foreach (var child in node.GetChildren())
                if (child is Control control)
                {
                    if (control is { FocusMode: FocusModeEnum.All, Visible: true })
                        controls.Add(control);

                    CollectFocusableControls(control, controls);
                }
        }

        private void ApplyStaticTexts()
        {
            _titleLabel.SetTextAutoSize(ModSettingsLocalization.Get("entry.title", "Mod Settings (RitsuLib)"));
            _subtitleLabel.SetTextAutoSize(ModSettingsLocalization.Get("entry.subtitle",
                "Edit player-facing mod options here."));
        }

        private void ExpandOnlyMod(string? modId)
        {
            _expandedModIds.Clear();
            if (!string.IsNullOrWhiteSpace(modId))
                _expandedModIds.Add(modId);
        }

        private void FlushDirtyBindings()
        {
            if (_dirtyBindings.Count == 0)
            {
                _saveTimer = -1;
                return;
            }

            foreach (var binding in _dirtyBindings.ToArray())
                try
                {
                    binding.Save();
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Settings] Failed to save '{binding.ModId}:{binding.DataKey}': {ex.Message}");
                }

            _dirtyBindings.Clear();
            _saveTimer = -1;
        }

        private void SubscribeLocaleChanges()
        {
            if (_localeSubscribed)
                return;

            try
            {
                LocManager.Instance.SubscribeToLocaleChange(OnLocaleChanged);
                _localeSubscribed = true;
            }
            catch
            {
                // ignored
            }
        }

        private void UnsubscribeLocaleChanges()
        {
            if (!_localeSubscribed)
                return;

            try
            {
                LocManager.Instance.UnsubscribeToLocaleChange(OnLocaleChanged);
            }
            catch
            {
                // ignored
            }

            _localeSubscribed = false;
        }

        private void OnLocaleChanged()
        {
            FlushDirtyBindings();
            Callable.From(Rebuild).CallDeferred();
        }

        private static MegaRichTextLabel CreateTitleLabel(int fontSize, HorizontalAlignment alignment)
        {
            var label = new MegaRichTextLabel
            {
                Theme = ModSettingsUiResources.SettingsLineTheme,
                BbcodeEnabled = true,
                AutoSizeEnabled = false,
                ScrollActive = false,
                HorizontalAlignment = alignment,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
                FocusMode = FocusModeEnum.None,
            };

            label.AddThemeFontOverride("normal_font", ModSettingsUiResources.KreonRegular);
            label.AddThemeFontOverride("bold_font", ModSettingsUiResources.KreonBold);
            label.AddThemeFontSizeOverride("normal_font_size", fontSize);
            label.AddThemeFontSizeOverride("bold_font_size", fontSize);
            label.AddThemeFontSizeOverride("italics_font_size", fontSize);
            label.AddThemeFontSizeOverride("bold_italics_font_size", fontSize);
            label.AddThemeFontSizeOverride("mono_font_size", fontSize);
            label.MinFontSize = Math.Min(fontSize, 16);
            label.MaxFontSize = fontSize;
            return label;
        }

        private static MegaRichTextLabel CreateEmptyStateLabel(string text)
        {
            var label = CreateTitleLabel(24, HorizontalAlignment.Center);
            label.CustomMinimumSize = new(0f, 120f);
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            label.SetTextAutoSize(text);
            return label;
        }

        private static StyleBoxFlat CreatePanelStyle(Color bg)
        {
            return new()
            {
                BgColor = bg,
                BorderColor = new(0.44f, 0.68f, 0.80f, 0.36f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 18,
                CornerRadiusTopRight = 18,
                CornerRadiusBottomRight = 18,
                CornerRadiusBottomLeft = 18,
                ShadowColor = new(0f, 0f, 0f, 0.32f),
                ShadowSize = 12,
                ContentMarginLeft = 0,
                ContentMarginTop = 0,
                ContentMarginRight = 0,
                ContentMarginBottom = 0,
            };
        }

        private static StyleBoxFlat CreateSidebarGroupStyle(bool selected)
        {
            return new()
            {
                BgColor = selected
                    ? new(0.085f, 0.125f, 0.165f, 0.97f)
                    : new Color(0.07f, 0.095f, 0.13f, 0.94f),
                BorderColor = selected
                    ? new(0.58f, 0.80f, 0.90f, 0.58f)
                    : new Color(0.30f, 0.44f, 0.54f, 0.36f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 18,
                CornerRadiusTopRight = 18,
                CornerRadiusBottomRight = 18,
                CornerRadiusBottomLeft = 18,
                ShadowColor = new(0f, 0f, 0f, 0.16f),
                ShadowSize = 4,
                ContentMarginLeft = 10,
                ContentMarginTop = 10,
                ContentMarginRight = 10,
                ContentMarginBottom = 10,
            };
        }
    }
}
