using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Ui.Catalog;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed record RitsuDebugCardCatalogEntry(
        RitsuCatalogItem Item,
        CardModel VisualCard,
        CardModel SourceCard,
        Func<Control> DetailFactory);

    internal sealed partial class RitsuDebugCardCatalog : Control
    {
        internal const string HolderMetaKey = "ritsulib_debug_card_catalog_holder";
        private const double SearchDelaySeconds = 0.14d;
        private const float CardWidth = 210f;
        private const float CardHeight = 295.4f;
        private const float CardHorizontalGap = 24f;
        private const float CardVerticalGap = 32f;
        private const float CardHorizontalPadding = 12f;
        private const float CardVerticalPadding = 16f;
        private const float CardSelectionFrameMargin = 7f;
        private const float DetailDrawerWidth = 400f;
        private const int OverscanRows = 2;
        internal static readonly Vector2 HolderScale = Vector2.One * 0.7f;
        internal static readonly Vector2 HolderHoverScale = HolderScale * 1.1f;

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
        private readonly string? _primaryFilterBreakBeforeOptionId;
        private readonly Dictionary<int, Button> _primaryFilterButtons = [];
        private readonly string? _primaryFilterId;
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

        internal RitsuDebugCardCatalog(
            string searchPlaceholder,
            IReadOnlyList<RitsuDebugCardCatalogEntry> entries,
            IReadOnlyList<RitsuCatalogFilter>? filters = null,
            string? primaryFilterId = null,
            string? primaryDefaultOptionId = null,
            string? primaryFilterBreakBeforeOptionId = null,
            string? defaultFilterId = null,
            string? defaultFilterOptionId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(searchPlaceholder);
            ValidateEntries(entries);

            SearchPlaceholder = searchPlaceholder;
            _entries = [.. entries];
            _itemsById = _entries.ToDictionary(static entry => entry.Item.Id, StringComparer.Ordinal);
            _sourceIndexes = _entries.Select((entry, index) => (entry.Item.Id, Index: index))
                .ToDictionary(static pair => pair.Id, static pair => pair.Index, StringComparer.Ordinal);
            _filters = filters == null ? [] : [.. filters];
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
                var defaultIndex = primaryDefaultOptionId == null
                    ? 0
                    : primaryFilter.Options.ToList().FindIndex(option => option.Id == primaryDefaultOptionId);
                _filterSelections[primaryFilter.Id] = Math.Max(0, defaultIndex);
            }

            if (defaultFilterId == null != (defaultFilterOptionId == null))
                throw new ArgumentException("The default filter ID and option ID must be supplied together.");
            if (defaultFilterId != null)
            {
                var defaultFilter = _filters.SingleOrDefault(filter => filter.Id == defaultFilterId)
                                    ?? throw new ArgumentException(
                                        "The default filter ID must identify one supplied filter.",
                                        nameof(defaultFilterId));
                var defaultIndex = defaultFilter.Options.ToList()
                    .FindIndex(option => option.Id == defaultFilterOptionId);
                if (defaultIndex < 0)
                    throw new ArgumentException(
                        "The default filter option ID must identify an option in the default filter.",
                        nameof(defaultFilterOptionId));
                _filterSelections[defaultFilter.Id] = defaultIndex;
            }
        }

        private string SearchPlaceholder { get; }

        internal static bool IsCatalogHolder(NCardHolder holder)
        {
            return holder is NGridCardHolder && holder.HasMeta(HolderMetaKey);
        }

        internal void UpdateEntries(IReadOnlyList<RitsuDebugCardCatalogEntry> entries)
        {
            ValidateEntries(entries);
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
            if (entries.Any(static entry => entry == null ||
                                            entry.Item == null ||
                                            entry.VisualCard == null ||
                                            entry.SourceCard == null ||
                                            entry.DetailFactory == null))
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
            foreach (var holder in _holders)
                if (IsInstanceValid(holder))
                    holder.QueueFreeSafely();
            _holders.Clear();
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
            var workspace = new Control
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                ClipContents = true,
            };
            AddChild(workspace);
            workspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

            var catalogPanel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            catalogPanel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListShellStyle());
            workspace.AddChild(catalogPanel);
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
            workspace.AddChild(_detailBackdrop);
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
            _detailSlideHost.OffsetLeft = -DetailDrawerWidth;
            _detailSlideHost.OffsetRight = 0f;
            _detailSlideHost.OffsetTop = 0f;
            _detailSlideHost.OffsetBottom = 0f;
            workspace.AddChild(_detailSlideHost);
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
            detailHeader.AddChild(new ModSettingsTextButton(
                "×",
                ModSettingsButtonTone.Normal,
                CloseDetailDrawer)
            {
                TooltipText = ModSettingsLocalization.Get("ritsulib.catalog.closeDetails", "Close details"),
                CustomMinimumSize = new(42f, 38f),
            });
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
            SetProcessUnhandledInput(true);
        }

        private void AddSortControls(HBoxContainer tools)
        {
            var label = new Label
            {
                Text = ModSettingsLocalization.Get("ritsulib.debugTools.sort", "Sort"),
                VerticalAlignment = VerticalAlignment.Center,
            };
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
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("separation", 4);
            catalog.AddChild(row);
            AddOptionButton(-1, filter.AllLabel);
            for (var index = 0; index < filter.Options.Count; index++)
            {
                var option = filter.Options[index];
                if (option.Id == _primaryFilterBreakBeforeOptionId)
                {
                    var separator = new VSeparator { CustomMinimumSize = new(12f, 0f) };
                    row.AddChild(separator);
                }

                AddOptionButton(index, option.Label);
            }

            row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
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
            _filtered = _entries.Where(entry => entry.Item.Matches(terms) && MatchesFilters(entry.Item)).ToArray();
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
                    CardSortField.Cost => (left.VisualCard.EnergyCost?.Canonical ?? 0)
                        .CompareTo(right.VisualCard.EnergyCost?.Canonical ?? 0),
                    CardSortField.Alphabet => StringComparer.CurrentCultureIgnoreCase.Compare(
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

        private bool MatchesFilters(RitsuCatalogItem item)
        {
            foreach (var filter in _filters)
            {
                var index = _filterSelections.GetValueOrDefault(filter.Id, -1);
                if (index >= 0 && (index >= filter.Options.Count || !filter.Options[index].Matches(item)))
                    return false;
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

            var rowHeight = CardHeight + CardVerticalGap;
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
                selectionFrame.Position = holder.Position - new Vector2(
                    CardWidth * 0.5f + CardSelectionFrameMargin,
                    CardHeight * 0.5f + CardSelectionFrameMargin);
                selectionFrame.Size = new(
                    CardWidth + CardSelectionFrameMargin * 2f,
                    CardHeight + CardSelectionFrameMargin * 2f);
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
                _canvas.AddChild(selectionFrame);
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
                label.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
                _detailHost.AddChild(label);
            }

            AnimateDetailDrawer(true);
        }

        private void CloseDetailDrawer()
        {
            var itemId = _selectedItemId;
            _selectedItemId = null;
            RebuildDetail();
            RefreshGrid();
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
            if (show)
            {
                _detailBackdrop.Show();
                if (_detailSlideHost.Visible)
                {
                    SetDetailDrawerOffsets(-DetailDrawerWidth, 0f);
                    return;
                }

                SetDetailDrawerOffsets(0f, DetailDrawerWidth);
                _detailSlideHost.Show();
                _detailTween = _detailSlideHost.CreateTween();
                _detailTween.SetTrans(Tween.TransitionType.Cubic);
                _detailTween.SetEase(Tween.EaseType.Out);
                _detailTween.TweenProperty(_detailSlideHost, "offset_left", -DetailDrawerWidth, 0.22f);
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
            _detailTween.Parallel().TweenProperty(_detailSlideHost, "offset_right", DetailDrawerWidth, 0.16f);
            _detailTween.TweenCallback(Callable.From(() =>
            {
                _detailTween = null;
                if (!IsInstanceValid(_detailSlideHost))
                    return;
                _detailSlideHost.Hide();
                SetDetailDrawerOffsets(-DetailDrawerWidth, 0f);
                _detailBackdrop.Hide();
            }));
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
            CloseDetailDrawer();
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
