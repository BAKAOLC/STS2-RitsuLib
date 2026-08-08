using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Ui.Catalog;
using STS2RitsuLib.Ui.Overlay;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed record RitsuDebugCardCatalogEntry(
        RitsuCatalogItem Item,
        CardModel VisualCard,
        CardModel SourceCard,
        Func<Control> DetailFactory,
        int StateHash = 0);

    // ReSharper disable once Godot.MissingParameterlessConstructor
    internal sealed partial class RitsuDebugCardCatalog : Control
    {
        internal const string HolderMetaKey = "ritsulib_debug_card_catalog_holder";
        private const double SearchDelaySeconds = 0.14d;
        private const float CardWidth = 210f;
        private const float CardHeight = 295.4f;
        private const float CardHorizontalGap = 24f;
        private const float CardVerticalGap = 32f;
        private const float CardHorizontalPadding = 32f;
        private const float CardVerticalPadding = 36f;
        private const float CardSelectionFrameMargin = 10f;
        private const float DetailDrawerMinimumWidth = 400f;
        private const float DetailDrawerMaximumWidth = 640f;
        private const float DetailDrawerPreferredWidthFraction = 0.44f;
        private const float MinimumVisibleCatalogWidth = 300f;
        private const int OverscanRows = 2;
        internal static readonly Vector2 HolderScale = Vector2.One * 0.7f;
        internal static readonly Vector2 HolderHoverScale = HolderScale * 1.1f;
        internal static readonly Rect2 HolderVisualBounds = new(-166f, -227f, 330f, 450f);

        private static readonly List<(CardSortField Field, bool Ascending)> SortPriority =
        [
            (CardSortField.Rarity, true),
            (CardSortField.Type, true),
            (CardSortField.Cost, true),
            (CardSortField.Alphabet, true),
        ];

        private readonly Dictionary<string, int> _filterSelections = new(StringComparer.Ordinal);
        private readonly RitsuCatalogFilter[] _filters;
        private readonly Dictionary<NGridCardHolder, string> _holderItemIds = [];
        private readonly List<NGridCardHolder> _holders = [];
        private readonly Func<RitsuCatalogItem, bool>? _primaryAllMatches;
        private readonly string? _primaryFilterBreakBeforeOptionId;
        private readonly Dictionary<int, Button> _primaryFilterButtons = [];
        private readonly string? _primaryFilterId;
        private readonly HashSet<string> _primaryOverflowOptionIds;
        private readonly List<PanelContainer> _selectionFrames = [];
        private readonly Dictionary<CardSortField, Button> _sortButtons = [];
        private Control _canvas = null!;
        private ColorRect _detailBackdrop = null!;
        private VBoxContainer _detailHost = null!;
        private MarginContainer _detailScrollFrame = null!;
        private Control _detailSlideHost = null!;
        private Label _detailTitle = null!;
        private Tween? _detailTween;
        private Label _emptyLabel = null!;
        private RitsuDebugCardCatalogEntry[] _entries;
        private RitsuDebugCardCatalogEntry[] _filtered = [];
        private int _gridColumns = 1;
        private bool _gridRefreshQueued;
        private Dictionary<string, RitsuDebugCardCatalogEntry> _itemsById;
        private Label _resultCount = null!;
        private ScrollContainer _scroll = null!;
        private MarginContainer _scrollFrame = null!;
        private LineEdit _search = null!;
        private int _searchRevision;
        private string? _selectedItemId;
        private Dictionary<string, int> _sourceIndexes;
        private Control _workspace = null!;
        private RitsuDebugSearchableChoice? _primaryOverflowPicker;

        internal RitsuDebugCardCatalog(
            string searchPlaceholder,
            IReadOnlyList<RitsuDebugCardCatalogEntry> entries,
            IReadOnlyList<RitsuCatalogFilter>? filters = null,
            string? primaryFilterId = null,
            string? primaryDefaultOptionId = null,
            string? primaryFilterBreakBeforeOptionId = null,
            string? defaultFilterId = null,
            string? defaultFilterOptionId = null,
            bool primaryDefaultsToAll = false,
            Func<RitsuCatalogItem, bool>? primaryAllMatches = null,
            IReadOnlyCollection<string>? primaryOverflowOptionIds = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(searchPlaceholder);
            ValidateEntries(entries);

            SearchPlaceholder = searchPlaceholder;
            _entries = [.. entries];
            _itemsById = _entries.ToDictionary(static entry => entry.Item.Id, StringComparer.Ordinal);
            _sourceIndexes = _entries.Select((entry, index) => (entry.Item.Id, Index: index))
                .ToDictionary(static pair => pair.Id, static pair => pair.Index, StringComparer.Ordinal);
            _filters = filters == null ? [] : [.. filters];
            _primaryOverflowOptionIds = primaryOverflowOptionIds == null
                ? []
                : new(primaryOverflowOptionIds, StringComparer.Ordinal);
            foreach (var filter in _filters)
                _filterSelections.Add(filter.Id, -1);
            if (primaryFilterId != null)
            {
                var primaryFilter = _filters.SingleOrDefault(filter => filter.Id == primaryFilterId)
                                    ?? throw new ArgumentException(
                                        "The primary filter ID must identify one supplied filter.",
                                        nameof(primaryFilterId));
                _primaryFilterId = primaryFilter.Id;
                _primaryFilterBreakBeforeOptionId = primaryFilterBreakBeforeOptionId;
                _primaryAllMatches = primaryAllMatches;
                if (_primaryOverflowOptionIds.Any(id => primaryFilter.Options.All(option => option.Id != id)))
                    throw new ArgumentException(
                        "Every primary overflow option ID must identify an option in the primary filter.",
                        nameof(primaryOverflowOptionIds));
                var defaultIndex = primaryDefaultOptionId == null
                    ? 0
                    : primaryFilter.Options.ToList().FindIndex(option => option.Id == primaryDefaultOptionId);
                _filterSelections[primaryFilter.Id] = primaryDefaultsToAll ? -1 : Math.Max(0, defaultIndex);
            }

            if (defaultFilterId == null != (defaultFilterOptionId == null))
                throw new ArgumentException("The default filter ID and option ID must be supplied together.");
            if (defaultFilterId == null)
                return;

            var defaultFilter = _filters.SingleOrDefault(filter => filter.Id == defaultFilterId)
                                ?? throw new ArgumentException(
                                    "The default filter ID must identify one supplied filter.",
                                    nameof(defaultFilterId));
            var defaultFilterIndex = defaultFilter.Options.ToList()
                .FindIndex(option => option.Id == defaultFilterOptionId);
            if (defaultFilterIndex < 0)
                throw new ArgumentException(
                    "The default filter option ID must identify an option in the default filter.",
                    nameof(defaultFilterOptionId));
            _filterSelections[defaultFilter.Id] = defaultFilterIndex;
        }

        private string SearchPlaceholder { get; }

        internal static bool IsCatalogHolder(NCardHolder holder)
        {
            return holder is NGridCardHolder && holder.HasMeta(HolderMetaKey);
        }

        internal void UpdateEntries(IReadOnlyList<RitsuDebugCardCatalogEntry> entries)
        {
            ValidateEntries(entries);
            if (EntriesMatch(entries))
                return;
            var rebuildDetail = false;
            if (_selectedItemId != null)
            {
                var previous = _itemsById.GetValueOrDefault(_selectedItemId);
                var current = entries.FirstOrDefault(entry => entry.Item.Id == _selectedItemId);
                rebuildDetail = previous == null ||
                                current == null ||
                                !ReferenceEquals(previous.SourceCard, current.SourceCard) ||
                                !string.Equals(previous.Item.Subtitle, current.Item.Subtitle, StringComparison.Ordinal);
            }

            _entries = [.. entries];
            _itemsById = _entries.ToDictionary(static entry => entry.Item.Id, StringComparer.Ordinal);
            _sourceIndexes = _entries.Select((entry, index) => (entry.Item.Id, Index: index))
                .ToDictionary(static pair => pair.Id, static pair => pair.Index, StringComparer.Ordinal);
            ApplyFilter(rebuildDetail);
            RefreshState();
        }

        private bool EntriesMatch(IReadOnlyList<RitsuDebugCardCatalogEntry> entries)
        {
            if (_entries.Length != entries.Count)
                return false;
            for (var index = 0; index < _entries.Length; index++)
            {
                var previous = _entries[index];
                var current = entries[index];
                if (!string.Equals(previous.Item.Id, current.Item.Id, StringComparison.Ordinal) ||
                    !ReferenceEquals(previous.SourceCard, current.SourceCard) ||
                    previous.StateHash != current.StateHash ||
                    !string.Equals(previous.Item.Title, current.Item.Title, StringComparison.Ordinal) ||
                    !string.Equals(previous.Item.Subtitle, current.Item.Subtitle, StringComparison.Ordinal) ||
                    !string.Equals(previous.Item.Badge, current.Item.Badge, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        internal void RefreshState()
        {
            foreach (var holder in _holders)
            {
                if (!holder.Visible ||
                    !_holderItemIds.TryGetValue(holder, out var itemId) ||
                    !_itemsById.TryGetValue(itemId, out var entry))
                    continue;
                holder.ReassignToCard(entry.VisualCard, PileType.None, null, ModelVisibility.Visible);
            }

            _detailHost.GetChildren()
                .OfType<RitsuDebugLiveDetailContainer>()
                .FirstOrDefault()
                ?.RefreshState();
        }

        private static void ValidateEntries(IReadOnlyList<RitsuDebugCardCatalogEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);
            if (entries.Count > RitsuCatalogBrowser.MaximumItemCount)
                throw new ArgumentException("The card catalog contains too many entries.", nameof(entries));
            // ReSharper disable once MergeSequentialChecks
            if (entries.Any(static entry =>
                    ReferenceEquals(entry, null) ||
                    ReferenceEquals(entry.Item, null) ||
                    ReferenceEquals(entry.VisualCard, null) ||
                    ReferenceEquals(entry.SourceCard, null) ||
                    ReferenceEquals(entry.DetailFactory, null)))
                throw new ArgumentException("Card catalog entries cannot contain null.", nameof(entries));
            if (entries.Select(static entry => entry.Item.Id).Distinct(StringComparer.Ordinal).Count() != entries.Count)
                throw new ArgumentException("Card catalog item IDs must be unique.", nameof(entries));
        }

        public override void _Ready()
        {
            CustomMinimumSize = new(0f, 460f);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;
            MouseFilter = MouseFilterEnum.Pass;
            BuildUi();
            ApplyFilter();
        }

        public override void _ExitTree()
        {
            _detailTween?.Kill();
            _detailTween = null;
            for (var index = 0; index < _holders.Count; index++)
            {
                var holder = _holders[index];
                if (IsInstanceValid(holder))
                {
                    holder.RemoveMeta(HolderMetaKey);
                    if (index < _selectionFrames.Count && IsInstanceValid(_selectionFrames[index]))
                    {
                        holder.RemoveChildSafely(_selectionFrames[index]);
                        _selectionFrames[index].QueueFreeSafely();
                    }

                    holder.QueueFreeSafely();
                }
            }

            _holders.Clear();
            _selectionFrames.Clear();
            _holderItemIds.Clear();
            base._ExitTree();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (!_detailSlideHost.Visible ||
                @event.IsEcho() ||
                !(@event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack)))
                return;

            CloseDetailDrawer();
            GetViewport().SetInputAsHandled();
        }

        private void BuildUi()
        {
            _workspace = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                ClipContents = true,
            };
            AddChild(_workspace);
            _workspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _workspace.Resized += UpdateDetailDrawerWidth;

            var catalogPanel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            catalogPanel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListShellStyle());
            _workspace.AddChild(catalogPanel);
            catalogPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

            var catalog = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            catalog.AddThemeConstantOverride("separation", 7);
            catalogPanel.AddChild(catalog);

            AddPrimaryFilterControls(catalog);

            var tools = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            tools.AddThemeConstantOverride("separation", 10);
            catalog.AddChild(tools);
            AddSortControls(tools);

            _search = ModSettingsUiControlTheming.CreateStyledLineEdit(string.Empty, SearchPlaceholder);
            _search.ClearButtonEnabled = true;
            _search.MaxLength = RitsuCatalogBrowser.MaximumSearchTextLength;
            _search.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _search.TextChanged += _ => ScheduleSearch();
            tools.AddChild(_search);
            AddFilterControls(catalog);

            var summary = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var label = new Label
            {
                Text = SearchPlaceholder,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            label.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            summary.AddChild(label);
            _resultCount = new() { HorizontalAlignment = HorizontalAlignment.Right };
            _resultCount.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            _resultCount.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            summary.AddChild(_resultCount);
            catalog.AddChild(summary);

            _scroll = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            };
            ModSettingsUiControlTheming.ApplySettingsScrollContainerThemeForDropdownList(_scroll);
            catalog.AddChild(_scroll);
            _scrollFrame = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            _scroll.AddChild(_scrollFrame);
            _canvas = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _scrollFrame.AddChild(_canvas);
            _scroll.GetVScrollBar().ValueChanged += _ => QueueGridRefresh();
            _scroll.GetVScrollBar().VisibilityChanged += OnScrollbarVisibilityChanged;
            _scroll.Resized += QueueGridRefresh;
            _canvas.Resized += QueueGridRefresh;

            _emptyLabel = new()
            {
                Text = ModSettingsLocalization.Get("ritsulib.debugTools.noMatches", "No matching items"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _emptyLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            _emptyLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            catalogPanel.AddChild(_emptyLabel);
            _emptyLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _emptyLabel.OffsetTop = 100f;

            var detailPanel = new PanelContainer
            {
                MouseFilter = MouseFilterEnum.Stop,
            };
            detailPanel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateInsetSurfaceStyle());
            _detailBackdrop = new()
            {
                Color = new(0f, 0f, 0f, 0.2f),
                MouseFilter = MouseFilterEnum.Stop,
                Visible = false,
                ZIndex = 19,
            };
            _detailBackdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _detailBackdrop.GuiInput += OnDetailBackdropInput;
            _workspace.AddChild(_detailBackdrop);
            _detailSlideHost = new()
            {
                Visible = false,
                MouseFilter = MouseFilterEnum.Ignore,
                ZIndex = 20,
            };
            _detailSlideHost.AnchorLeft = 1f;
            _detailSlideHost.AnchorRight = 1f;
            _detailSlideHost.AnchorTop = 0f;
            _detailSlideHost.AnchorBottom = 1f;
            _detailSlideHost.OffsetLeft = -DetailDrawerMinimumWidth;
            _detailSlideHost.OffsetRight = 0f;
            _detailSlideHost.OffsetTop = 0f;
            _detailSlideHost.OffsetBottom = 0f;
            _workspace.AddChild(_detailSlideHost);
            detailPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _detailSlideHost.AddChild(detailPanel);

            var detailColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            detailColumn.AddThemeConstantOverride("separation", 0);
            detailPanel.AddChild(detailColumn);
            var detailHeader = new HBoxContainer
            {
                CustomMinimumSize = new(0f, 42f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            detailHeader.AddThemeConstantOverride("separation", 8);
            _detailTitle = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            _detailTitle.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            _detailTitle.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.RichTitle);
            detailHeader.AddChild(_detailTitle);
            var closeTooltip = ModSettingsLocalization.Get("ritsulib.catalog.closeDetails", "Close details");
            var closeButton = new RitsuDebugToolsIconButton(42f, 38f);
            closeButton.Configure(
                RitsuDebugToolsIcons.Get(
                    RitsuDebugToolsGlyph.Close,
                    18,
                    RitsuShellTheme.Current.Text.LabelPrimary),
                closeTooltip,
                ModSettingsButtonTone.Normal);
            closeButton.Pressed += () => CloseDetailDrawer(Sts2InputCompat.IsUsingDirectionalNavigation);
            detailHeader.AddChild(closeButton);
            detailColumn.AddChild(detailHeader);

            var detailScroll = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            };
            ModSettingsUiControlTheming.ApplySettingsScrollContainerThemeForDropdownList(detailScroll);
            detailColumn.AddChild(detailScroll);
            _detailScrollFrame = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            detailScroll.AddChild(_detailScrollFrame);
            _detailHost = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            _detailScrollFrame.AddChild(_detailHost);
            detailScroll.GetVScrollBar().VisibilityChanged += () =>
                SyncScrollGutter(detailScroll, _detailScrollFrame);
            Callable.From(() => SyncScrollGutter(detailScroll, _detailScrollFrame)).CallDeferred();
            Callable.From(UpdateDetailDrawerWidth).CallDeferred();
            SetProcessUnhandledInput(true);
        }

        private void AddSortControls(HBoxContainer tools)
        {
            var label = new Label
            {
                Text = ModSettingsLocalization.Get("ritsulib.debugTools.sort", "Sort"),
                VerticalAlignment = VerticalAlignment.Center,
            };
            label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            label.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            tools.AddChild(label);

            foreach (var (field, text) in new[]
                     {
                         (CardSortField.Type,
                             ModSettingsLocalization.Get("ritsulib.debugTools.filter.type", "Type")),
                         (CardSortField.Rarity,
                             ModSettingsLocalization.Get("ritsulib.debugTools.filter.rarity", "Rarity")),
                         (CardSortField.Cost,
                             ModSettingsLocalization.Get("ritsulib.debugTools.cost", "Cost")),
                         (CardSortField.Alphabet,
                             ModSettingsLocalization.Get("ritsulib.debugTools.sort.alphabet", "A-Z")),
                     })
            {
                var button = ModSettingsUiControlTheming.CreateCompactSettingsToggleButton(text, false);
                button.CustomMinimumSize = new(72f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight);
                button.Pressed += () => PromoteSort(field);
                _sortButtons.Add(field, button);
                tools.AddChild(button);
            }

            RefreshSortButtons();
        }

        private void PromoteSort(CardSortField field)
        {
            var index = SortPriority.FindIndex(priority => priority.Field == field);
            if (index == 0)
            {
                SortPriority[0] = (field, !SortPriority[0].Ascending);
            }
            else
            {
                if (index > 0)
                    SortPriority.RemoveAt(index);
                SortPriority.Insert(0, (field, true));
            }

            RefreshSortButtons();
            ApplyFilter();
        }

        private void RefreshSortButtons()
        {
            var primary = SortPriority[0];
            foreach (var (field, button) in _sortButtons)
            {
                button.ButtonPressed = field == primary.Field;
                button.Text = field == primary.Field
                    ? $"{SortLabel(field)} {(primary.Ascending ? '▲' : '▼')}"
                    : SortLabel(field);
                ModSettingsUiControlTheming.RefreshAdaptiveButtonText(button);
            }
        }

        private static string SortLabel(CardSortField field)
        {
            return field switch
            {
                CardSortField.Type => ModSettingsLocalization.Get("ritsulib.debugTools.filter.type", "Type"),
                CardSortField.Rarity => ModSettingsLocalization.Get("ritsulib.debugTools.filter.rarity", "Rarity"),
                CardSortField.Cost => ModSettingsLocalization.Get("ritsulib.debugTools.cost", "Cost"),
                CardSortField.Alphabet => ModSettingsLocalization.Get("ritsulib.debugTools.sort.alphabet", "A-Z"),
                _ => string.Empty,
            };
        }

        private void AddFilterControls(VBoxContainer catalog)
        {
            if (_filters.All(filter => filter.Id == _primaryFilterId))
                return;

            var row = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("h_separation", 8);
            row.AddThemeConstantOverride("v_separation", 6);
            catalog.AddChild(row);
            foreach (var filter in _filters)
            {
                if (filter.Id == _primaryFilterId)
                    continue;
                var options = new List<(int Value, string Label)> { (-1, $"{filter.Label}: {filter.AllLabel}") };
                options.AddRange(filter.Options.Select((option, index) =>
                    (index, $"{filter.Label}: {option.Label}")));
                var dropdown = new ModSettingsDropdownChoiceControl<int>(
                    options,
                    _filterSelections[filter.Id],
                    selected =>
                    {
                        _filterSelections[filter.Id] = selected;
                        ApplyFilter();
                    })
                {
                    CustomMinimumSize = new(190f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
                };
                row.AddChild(dropdown);
            }
        }

        private void AddPrimaryFilterControls(VBoxContainer catalog)
        {
            if (_primaryFilterId == null)
                return;
            var filter = _filters.Single(candidate => candidate.Id == _primaryFilterId);
            var row = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("h_separation", 4);
            row.AddThemeConstantOverride("v_separation", 6);
            catalog.AddChild(row);
            AddOptionButton(-1, filter.AllLabel);
            for (var index = 0; index < filter.Options.Count; index++)
            {
                var option = filter.Options[index];
                if (_primaryOverflowOptionIds.Contains(option.Id))
                    continue;
                if (option.Id == _primaryFilterBreakBeforeOptionId)
                {
                    var separator = new VSeparator { CustomMinimumSize = new(12f, 0f) };
                    row.AddChild(separator);
                }

                AddOptionButton(index, option.Label);
            }

            var overflowOptions = filter.Options
                .Where(option => _primaryOverflowOptionIds.Contains(option.Id))
                .Select(option => new RitsuDebugSearchableChoiceOption(option.Id, option.Label))
                .ToArray();
            if (overflowOptions.Length > 0)
            {
                _primaryOverflowPicker = new(
                    ModSettingsLocalization.Get("ritsulib.debugTools.filter.customPiles", "Custom piles"),
                    ModSettingsLocalization.Get("ritsulib.debugTools.search.customPiles", "Search custom piles"),
                    ModSettingsLocalization.Get("ritsulib.debugTools.empty.customPiles", "No matching custom piles"),
                    overflowOptions);
                _primaryOverflowPicker.CustomMinimumSize = new(260f, 0f);
                _primaryOverflowPicker.SelectionChanged += selectedId =>
                {
                    if (selectedId == null)
                        return;
                    var optionIndex = filter.Options.ToList().FindIndex(option => option.Id == selectedId);
                    if (optionIndex < 0)
                        return;
                    _filterSelections[filter.Id] = optionIndex;
                    RefreshPrimaryFilterButtons();
                    ApplyFilter();
                };
                row.AddChild(_primaryOverflowPicker);
            }

            return;

            void AddOptionButton(int optionIndex, string label)
            {
                var button = ModSettingsUiControlTheming.CreateCompactSettingsToggleButton(
                    label,
                    _filterSelections[filter.Id] == optionIndex);
                button.CustomMinimumSize = new(96f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight);
                button.TooltipText = label;
                button.Pressed += () =>
                {
                    _filterSelections[filter.Id] = _filterSelections[filter.Id] == optionIndex
                        ? -1
                        : optionIndex;
                    RefreshPrimaryFilterButtons();
                    ApplyFilter();
                };
                _primaryFilterButtons.Add(optionIndex, button);
                row.AddChild(button);
            }
        }

        private void RefreshPrimaryFilterButtons()
        {
            if (_primaryFilterId == null)
                return;
            var selected = _filterSelections[_primaryFilterId];
            foreach (var (index, button) in _primaryFilterButtons)
            {
                button.ButtonPressed = index == selected;
                ModSettingsUiControlTheming.ApplySettingsToggleButtonStyle(button, index == selected, false);
                ModSettingsUiControlTheming.RefreshAdaptiveButtonText(button);
            }

            var selectedOptionId = selected >= 0 && selected < _filters
                .Single(filter => filter.Id == _primaryFilterId)
                .Options.Count
                ? _filters.Single(filter => filter.Id == _primaryFilterId).Options[selected].Id
                : null;
            _primaryOverflowPicker?.SetSelectedId(
                selectedOptionId != null && _primaryOverflowOptionIds.Contains(selectedOptionId)
                    ? selectedOptionId
                    : null);
        }

        private async void ScheduleSearch()
        {
            if (!IsInsideTree())
                return;
            var revision = ++_searchRevision;
            await ToSignal(GetTree().CreateTimer(SearchDelaySeconds), SceneTreeTimer.SignalName.Timeout);
            if (IsInsideTree() && revision == _searchRevision)
                ApplyFilter();
        }

        private void ApplyFilter(bool rebuildDetail = true)
        {
            var terms = _search.Text.Split((char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            _filtered = [.. _entries.Where(entry => entry.Item.Matches(terms) && MatchesFilters(entry.Item))];
            Array.Sort(_filtered, CompareEntries);
            _resultCount.Text = _filtered.Length == _entries.Length
                ? _entries.Length.ToString()
                : $"{_filtered.Length} / {_entries.Length}";
            _emptyLabel.Visible = _filtered.Length == 0;

            if (_selectedItemId != null && _filtered.All(entry => entry.Item.Id != _selectedItemId))
                _selectedItemId = null;
            if (rebuildDetail || _selectedItemId == null)
                RebuildDetail();
            UpdateCanvasMinimumSize();
            QueueGridRefresh();
        }

        private int CompareEntries(RitsuDebugCardCatalogEntry left, RitsuDebugCardCatalogEntry right)
        {
            foreach (var (field, ascending) in SortPriority)
            {
                var comparison = field switch
                {
                    CardSortField.Type => left.VisualCard.Type.CompareTo(right.VisualCard.Type),
                    CardSortField.Rarity => GetRarityOrder(left.VisualCard.Rarity)
                        .CompareTo(GetRarityOrder(right.VisualCard.Rarity)),
                    CardSortField.Cost => GetCostOrder(left.VisualCard)
                        .CompareTo(GetCostOrder(right.VisualCard)),
                    CardSortField.Alphabet => LocManager.Instance.StringComparer.Compare(
                        left.Item.Title, right.Item.Title),
                    _ => 0,
                };
                if (comparison != 0)
                    return ascending ? comparison : -comparison;
            }

            return _sourceIndexes[left.Item.Id].CompareTo(_sourceIndexes[right.Item.Id]);
        }

        private static int GetRarityOrder(CardRarity rarity)
        {
            if (rarity <= CardRarity.Ancient)
                return (int)rarity;
            return rarity switch
            {
                CardRarity.Status => 6,
                CardRarity.Curse => 7,
                CardRarity.Event => 8,
                CardRarity.Quest => 9,
                CardRarity.Token => 10,
                _ => 99,
            };
        }

        private static int GetCostOrder(CardModel card)
        {
            if (card.EnergyCost.CostsX)
                return int.MaxValue - 2;
            if (card.EnergyCost.Canonical >= 0)
                return card.EnergyCost.Canonical;
            if (card.CanonicalStarCost >= 0)
                return int.MaxValue - 3;
            return int.MaxValue - 1;
        }

        private bool MatchesFilters(RitsuCatalogItem item)
        {
            foreach (var filter in _filters)
            {
                var index = _filterSelections.GetValueOrDefault(filter.Id, -1);
                switch (index)
                {
                    case < 0 when filter.Id == _primaryFilterId &&
                                  _primaryAllMatches != null && !_primaryAllMatches(item):
                    case >= 0 when index >= filter.Options.Count || !filter.Options[index].Matches(item):
                        return false;
                }
            }

            return true;
        }

        private void QueueGridRefresh()
        {
            if (!IsInsideTree() || _gridRefreshQueued)
                return;
            _gridRefreshQueued = true;
            Callable.From(() =>
            {
                _gridRefreshQueued = false;
                RefreshGrid();
            }).CallDeferred();
        }

        private void RefreshGrid()
        {
            if (!IsInstanceValid(_scroll) || !IsInstanceValid(_canvas))
                return;

            SyncScrollGutter(_scroll, _scrollFrame);
            var gutter = _scroll.GetVScrollBar().Visible
                ? ModSettingsUiControlTheming.ResolveSettingsScrollContentRightGutter(_scroll)
                : 0f;
            var width = Math.Max(1f, _scroll.Size.X - gutter);
            var availableWidth = Math.Max(1f, width - CardHorizontalPadding * 2f);
            var columns = Math.Max(1,
                Mathf.FloorToInt((availableWidth + CardHorizontalGap) / (CardWidth + CardHorizontalGap)));
            if (_gridColumns != columns)
            {
                _gridColumns = columns;
                UpdateCanvasMinimumSize();
            }

            const float rowHeight = CardHeight + CardVerticalGap;
            var totalRows = Mathf.CeilToInt(_filtered.Length / (float)_gridColumns);
            var firstVisibleRow = totalRows == 0
                ? 0
                : Math.Clamp(Mathf.FloorToInt(_scroll.ScrollVertical / rowHeight), 0, totalRows - 1);
            var visibleRows = Math.Max(1, Mathf.CeilToInt(Math.Max(rowHeight, _scroll.Size.Y) / rowHeight) + 1);
            var startRow = Math.Max(0, firstVisibleRow - OverscanRows);
            var endRow = Math.Min(totalRows, firstVisibleRow + visibleRows + OverscanRows);
            var start = startRow * _gridColumns;
            var end = Math.Min(_filtered.Length, endRow * _gridColumns);
            EnsureHolderPool(Math.Max(0, end - start));

            var gridWidth = _gridColumns * CardWidth + Math.Max(0, _gridColumns - 1) * CardHorizontalGap;
            var originX = Math.Max(CardHorizontalPadding, (width - gridWidth) * 0.5f);
            for (var slot = 0; slot < _holders.Count; slot++)
            {
                var holder = _holders[slot];
                var selectionFrame = _selectionFrames[slot];
                if (slot >= end - start)
                {
                    holder.Hide();
                    selectionFrame.Hide();
                    continue;
                }

                var itemIndex = start + slot;
                var entry = _filtered[itemIndex];
                if (!_holderItemIds.TryGetValue(holder, out var boundId) || boundId != entry.Item.Id)
                {
                    holder.ReassignToCard(entry.VisualCard, PileType.None, null, ModelVisibility.Visible);
                    _holderItemIds[holder] = entry.Item.Id;
                }

                var row = itemIndex / _gridColumns;
                var column = itemIndex % _gridColumns;
                holder.Position = new(
                    originX + column * (CardWidth + CardHorizontalGap) + CardWidth * 0.5f,
                    CardVerticalPadding + row * rowHeight + CardHeight * 0.5f);
                var selected = entry.Item.Id == _selectedItemId;
                holder.Modulate = selected
                    ? Colors.White
                    : new(0.9f, 0.9f, 0.93f);
                selectionFrame.Visible = selected;
                holder.Show();
            }
        }

        private void EnsureHolderPool(int required)
        {
            while (_holders.Count < required)
            {
                var seed = _filtered[Math.Min(_holders.Count, _filtered.Length - 1)].VisualCard;
                var card = NCard.Create(seed);
                if (card == null)
                    return;
                var holder = NGridCardHolder.Create(card);
                if (holder == null)
                {
                    card.QueueFreeSafely();
                    return;
                }

                holder.SetMeta(HolderMetaKey, true);
                holder.Scale = holder.SmallScale;
                holder.MouseFilter = MouseFilterEnum.Pass;
                holder.Pressed += OnHolderPressed;
                var selectionFrame = new PanelContainer
                {
                    MouseFilter = MouseFilterEnum.Ignore,
                    Visible = false,
                };
                selectionFrame.AddThemeStyleboxOverride("panel",
                    RitsuShellChromeStyles.CreateSelectedListItemCardStyle());
                var selectionBounds = HolderVisualBounds.Grow(CardSelectionFrameMargin);
                selectionFrame.Position = selectionBounds.Position;
                selectionFrame.Size = selectionBounds.Size;
                holder.AddChild(selectionFrame);
                holder.MoveChild(selectionFrame, 0);
                _canvas.AddChild(holder);
                card.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
                _selectionFrames.Add(selectionFrame);
                _holders.Add(holder);
            }
        }

        private void OnHolderPressed(NCardHolder holder)
        {
            if (holder is not NGridCardHolder gridHolder ||
                !_holderItemIds.TryGetValue(gridHolder, out var itemId) ||
                !_itemsById.ContainsKey(itemId))
                return;

            _selectedItemId = itemId;
            RebuildDetail();
            RefreshGrid();
        }

        private void UpdateCanvasMinimumSize()
        {
            if (!IsInstanceValid(_canvas))
                return;
            var rows = Mathf.CeilToInt(_filtered.Length / (float)Math.Max(1, _gridColumns));
            _canvas.CustomMinimumSize = new(0f,
                rows == 0
                    ? 0f
                    : CardVerticalPadding * 2f + rows * CardHeight + Math.Max(0, rows - 1) * CardVerticalGap);
        }

        private void RebuildDetail()
        {
            if (!IsInstanceValid(_detailHost))
                return;
            foreach (var child in _detailHost.GetChildren())
                child.QueueFreeSafely();

            if (_selectedItemId == null || !_itemsById.TryGetValue(_selectedItemId, out var selected))
            {
                AnimateDetailDrawer(false);
                return;
            }

            _detailTitle.Text = selected.Item.Title;
            try
            {
                var content = selected.DetailFactory();
                if (content == null || !IsInstanceValid(content) || content.GetParent() != null)
                    throw new InvalidOperationException("The card detail factory returned an invalid control.");
                content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                _detailHost.AddChild(content);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[DebugToolsUi] Card detail factory failed: {ex}");
                var label = new Label
                {
                    Text = ModSettingsLocalization.Get("ritsulib.debugTools.detailsUnavailable",
                        "Details are unavailable for this item."),
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                };
                label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
                label.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
                _detailHost.AddChild(label);
            }

            AnimateDetailDrawer(true);
        }

        private void CloseDetailDrawer(bool restoreFocus = true)
        {
            var itemId = _selectedItemId;
            _selectedItemId = null;
            RebuildDetail();
            RefreshGrid();
            if (!restoreFocus)
            {
                GetViewport().GuiReleaseFocus();
                return;
            }

            if (itemId == null)
                return;
            Callable.From(() =>
            {
                if (!IsInsideTree())
                    return;
                var holder = _holderItemIds.FirstOrDefault(pair => pair.Value == itemId).Key;
                if (holder != null && IsInstanceValid(holder) && holder.Visible)
                    holder.GrabFocus();
            }).CallDeferred();
        }

        private void AnimateDetailDrawer(bool show)
        {
            _detailTween?.Kill();
            _detailTween = null;
            var width = ResolveDetailDrawerWidth();
            if (show)
            {
                _detailBackdrop.Show();
                if (_detailSlideHost.Visible)
                {
                    SetDetailDrawerOffsets(-width, 0f);
                    return;
                }

                SetDetailDrawerOffsets(0f, width);
                _detailSlideHost.Show();
                _detailTween = _detailSlideHost.CreateTween();
                _detailTween.SetTrans(Tween.TransitionType.Cubic);
                _detailTween.SetEase(Tween.EaseType.Out);
                _detailTween.TweenProperty(_detailSlideHost, "offset_left", -width, 0.22f);
                _detailTween.Parallel().TweenProperty(_detailSlideHost, "offset_right", 0f, 0.22f);
                _detailTween.TweenCallback(Callable.From(() => _detailTween = null));
                return;
            }

            if (!_detailSlideHost.Visible)
                return;
            _detailTween = _detailSlideHost.CreateTween();
            _detailTween.SetTrans(Tween.TransitionType.Cubic);
            _detailTween.SetEase(Tween.EaseType.In);
            _detailTween.TweenProperty(_detailSlideHost, "offset_left", 0f, 0.16f);
            _detailTween.Parallel().TweenProperty(_detailSlideHost, "offset_right", width, 0.16f);
            _detailTween.TweenCallback(Callable.From(() =>
            {
                _detailTween = null;
                if (!IsInstanceValid(_detailSlideHost))
                    return;
                _detailSlideHost.Hide();
                SetDetailDrawerOffsets(-width, 0f);
                _detailBackdrop.Hide();
            }));
        }

        private void UpdateDetailDrawerWidth()
        {
            if (!IsInstanceValid(_detailSlideHost))
                return;
            _detailTween?.Kill();
            _detailTween = null;
            var width = ResolveDetailDrawerWidth();
            SetDetailDrawerOffsets(_detailSlideHost.Visible ? -width : 0f, _detailSlideHost.Visible ? 0f : width);
        }

        private float ResolveDetailDrawerWidth()
        {
            var availableWidth = _workspace.Size.X;
            if (availableWidth <= 0f || !float.IsFinite(availableWidth))
                return DetailDrawerMinimumWidth;

            var minimum = MathF.Min(DetailDrawerMinimumWidth, availableWidth);
            var maximum = MathF.Min(DetailDrawerMaximumWidth, availableWidth);
            if (availableWidth >= DetailDrawerMinimumWidth + MinimumVisibleCatalogWidth)
                maximum = MathF.Min(maximum, availableWidth - MinimumVisibleCatalogWidth);
            maximum = MathF.Max(minimum, maximum);
            return Math.Clamp(availableWidth * DetailDrawerPreferredWidthFraction, minimum, maximum);
        }

        private void SetDetailDrawerOffsets(float left, float right)
        {
            _detailSlideHost.OffsetLeft = left;
            _detailSlideHost.OffsetRight = right;
        }

        private void OnDetailBackdropInput(InputEvent inputEvent)
        {
            if (inputEvent is not InputEventMouseButton { Pressed: true })
                return;
            CloseDetailDrawer(false);
            GetViewport().SetInputAsHandled();
        }

        private void OnScrollbarVisibilityChanged()
        {
            SyncScrollGutter(_scroll, _scrollFrame);
            QueueGridRefresh();
        }

        private static void SyncScrollGutter(ScrollContainer scroll, MarginContainer frame)
        {
            if (!IsInstanceValid(scroll) || !IsInstanceValid(frame))
                return;
            var gutter = scroll.GetVScrollBar().Visible
                ? ModSettingsUiControlTheming.ResolveSettingsScrollContentRightGutter(scroll)
                : 0;
            if (frame.GetThemeConstant("margin_right") == gutter)
                return;
            frame.AddThemeConstantOverride("margin_right", gutter);
            frame.QueueSort();
        }

        private enum CardSortField
        {
            Type,
            Rarity,
            Cost,
            Alphabet,
        }
    }
}
