using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Catalog
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         A themed, searchable catalog with single-choice filters, efficient support for large item sets,
    ///         stable selection, and an optional detail pane.
    ///     </para>
    ///     <para xml:lang="zh-CN">带主题的可搜索目录，支持单选筛选、高效展示大型目录、稳定选择和可选详情面板。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">Create and mutate this Godot control only on the main thread.</para>
    ///     <para xml:lang="zh-CN">仅可在 Godot 主线程创建及修改此控件。</para>
    /// </remarks>
    public sealed partial class RitsuCatalogBrowser : Control
    {
        private const float GridGap = 12f;
        /// <summary>
        ///     <para xml:lang="en">The maximum number of items accepted by one browser.</para>
        ///     <para xml:lang="zh-CN">单个浏览器可接受的最大目录项数量。</para>
        /// </summary>
        public const int MaximumItemCount = 16384;

        /// <summary>
        ///     <para xml:lang="en">The maximum number of characters accepted by the search field.</para>
        ///     <para xml:lang="zh-CN">搜索框可接受的最大字符数。</para>
        /// </summary>
        public const int MaximumSearchTextLength = 512;

        private const double SearchDelaySeconds = 0.16d;
        private const int VirtualOverscanRows = 3;
        private readonly HashSet<string> _filterFailureKeys = new(StringComparer.Ordinal);

        private readonly Dictionary<string, int> _filterSelections = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Texture2D?> _iconCache = new(StringComparer.Ordinal);
        private readonly RitsuCatalogBrowserOptions _options = new();
        private readonly List<CatalogRow> _rowPool = [];
        private readonly List<CatalogTile> _tilePool = [];
        private Control? _detailContent;
        private ColorRect? _detailBackdrop;
        private VBoxContainer? _detailHost;
        private Control? _detailSlideHost;
        private MarginContainer? _detailScrollFrame;
        private Label? _detailTitle;
        private Tween? _detailTween;
        private Label? _emptyLabel;
        private RitsuCatalogItem[] _filteredItems = [];
        private RitsuCatalogFilter[] _filters = [];
        private RitsuCatalogItem[] _items = [];
        private Label? _resultCount;
        private Control? _rowCanvas;
        private ScrollContainer? _scroll;
        private MarginContainer? _scrollFrame;
        private LineEdit? _search;
        private int _gridColumns = 1;
        private int _searchRevision;
        private string? _selectedItemId;
        private bool _uiBuilt;
        private bool _virtualRefreshQueued;

        /// <summary>
        ///     <para xml:lang="en">Creates a browser with default presentation options and no filters.</para>
        ///     <para xml:lang="zh-CN">使用默认呈现选项且不含筛选组地创建浏览器。</para>
        /// </summary>
        public RitsuCatalogBrowser()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a browser with validated presentation options and optional filters.</para>
        ///     <para xml:lang="zh-CN">使用经校验的呈现选项和可选筛选组创建浏览器。</para>
        /// </summary>
        /// <param name="options">
        ///     <para xml:lang="en">Presentation text, sizing, and optional detail factory.</para>
        ///     <para xml:lang="zh-CN">呈现文本、尺寸和可选详情工厂。</para>
        /// </param>
        /// <param name="filters">
        ///     <para xml:lang="en">Optional filter groups with unique IDs; at most eight groups are supported.</para>
        ///     <para xml:lang="zh-CN">ID 唯一的可选筛选组；最多支持八组。</para>
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">Thrown when options or filters are invalid.</para>
        ///     <para xml:lang="zh-CN">选项或筛选组无效时抛出。</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="options" /> is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="options" /> 为 null 时抛出。</para>
        /// </exception>
        public RitsuCatalogBrowser(
            RitsuCatalogBrowserOptions options,
            IReadOnlyList<RitsuCatalogFilter>? filters = null)
        {
            ArgumentNullException.ThrowIfNull(options);
            options.Validate();
            _options = options;
            SetFiltersCore(filters ?? []);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets an immutable snapshot of all items supplied to the browser.</para>
        ///     <para xml:lang="zh-CN">获取提供给浏览器的全部目录项的不可变快照。</para>
        /// </summary>
        public IReadOnlyList<RitsuCatalogItem> Items { get; private set; } = Array.Empty<RitsuCatalogItem>();

        /// <summary>
        ///     <para xml:lang="en">Gets the selected item, or null when no current item has the selected stable ID.</para>
        ///     <para xml:lang="zh-CN">获取选中的目录项；当前没有对应稳定 ID 的目录项时为 null。</para>
        /// </summary>
        public RitsuCatalogItem? SelectedItem => FindItem(_selectedItemId);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Raised synchronously after the selected stable item ID changes. Recoverable subscriber failures are
        ///         isolated and logged so one subscriber cannot prevent later subscribers from running.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         选中的稳定目录项 ID 发生变化后同步触发。可恢复的订阅者异常会被隔离并记录，单个订阅者不会阻止后续订阅者运行。
        ///     </para>
        /// </summary>
        public event EventHandler<RitsuCatalogSelectionChangedEventArgs>? SelectionChanged;

        /// <inheritdoc />
        public override void _Ready()
        {
            _options.Validate();
            BuildUi();
            ApplyFilter();
        }

        /// <inheritdoc />
        public override void _UnhandledInput(InputEvent @event)
        {
            if (_options.DetailPresentation != RitsuCatalogDetailPresentation.Drawer ||
                _detailSlideHost is not { Visible: true } ||
                @event.IsEcho() ||
                !(@event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack)))
                return;

            CloseDetailDrawer(true);
            GetViewport().SetInputAsHandled();
        }

        /// <inheritdoc />
        public override void _ExitTree()
        {
            _detailTween?.Kill();
            _detailTween = null;
            base._ExitTree();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Replaces all items from an immutable snapshot. IDs must be unique; the current selection is retained
        ///         when its ID remains visible under the current search and filters.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用不可变快照替换全部目录项。ID 必须唯一；对应 ID 在当前搜索及筛选条件下仍可见时保留当前选择。
        ///     </para>
        /// </summary>
        /// <param name="items">
        ///     <para xml:lang="en">Up to <see cref="MaximumItemCount" /> non-null items.</para>
        ///     <para xml:lang="zh-CN">不超过 <see cref="MaximumItemCount" /> 个非 null 目录项。</para>
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">Thrown for too many items, null entries, or duplicate IDs.</para>
        ///     <para xml:lang="zh-CN">目录项过多、包含 null 或 ID 重复时抛出。</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="items" /> is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="items" /> 为 null 时抛出。</para>
        /// </exception>
        public void SetItems(IReadOnlyList<RitsuCatalogItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            if (items.Count > MaximumItemCount)
                throw new ArgumentException($"A catalog cannot contain more than {MaximumItemCount} items.",
                    nameof(items));
            if (items.Any(static item => item == null))
                throw new ArgumentException("Catalog items cannot contain null.", nameof(items));
            if (items.Select(static item => item.Id).Distinct(StringComparer.Ordinal).Count() != items.Count)
                throw new ArgumentException("Catalog item IDs must be unique.", nameof(items));

            _items = [.. items];
            Items = Array.AsReadOnly(_items);
            _filterFailureKeys.Clear();
            _iconCache.Clear();
            if (_uiBuilt)
                ApplyFilter();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Replaces all filter groups and resets their selections to the unfiltered option.
        ///     </para>
        ///     <para xml:lang="zh-CN">替换全部筛选组，并将各组重置为不筛选选项。</para>
        /// </summary>
        /// <param name="filters">
        ///     <para xml:lang="en">Up to eight non-null filter groups with unique IDs.</para>
        ///     <para xml:lang="zh-CN">不超过八个、非 null 且 ID 唯一的筛选组。</para>
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">Thrown for too many groups, null entries, or duplicate IDs.</para>
        ///     <para xml:lang="zh-CN">筛选组过多、包含 null 或 ID 重复时抛出。</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="filters" /> is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="filters" /> 为 null 时抛出。</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">Thrown when filters are replaced after the control enters the scene tree.</para>
        ///     <para xml:lang="zh-CN">控件进入场景树后尝试替换筛选组时抛出。</para>
        /// </exception>
        public void SetFilters(IReadOnlyList<RitsuCatalogFilter> filters)
        {
            ArgumentNullException.ThrowIfNull(filters);
            if (_uiBuilt)
                throw new InvalidOperationException("Catalog filters cannot be replaced after the UI is built.");
            SetFiltersCore(filters);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Selects an item by stable ID without changing the search or filters. A null ID clears the selection;
        ///         an unknown ID is rejected without changing it.
        ///     </para>
        ///     <para xml:lang="zh-CN">按稳定 ID 选择目录项且不更改搜索或筛选。null 会清除选择；未知 ID 会被拒绝且不改变当前选择。</para>
        /// </summary>
        /// <param name="itemId">
        ///     <para xml:lang="en">The stable item ID, or null to clear the selection.</para>
        ///     <para xml:lang="zh-CN">稳定目录项 ID；传入 null 可清除选择。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">True when the requested selection was accepted; otherwise false.</para>
        ///     <para xml:lang="zh-CN">请求的选择被接受时为 true，否则为 false。</para>
        /// </returns>
        public bool SelectItem(string? itemId)
        {
            if (itemId != null && FindItem(itemId) == null)
                return false;
            SetSelection(itemId, true);
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Re-evaluates search text, filter predicates, row presentation, and selected-item details.
        ///     </para>
        ///     <para xml:lang="zh-CN">重新计算搜索文本、筛选谓词、行呈现和选中项详情。</para>
        /// </summary>
        public void Refresh()
        {
            if (_uiBuilt)
                ApplyFilter();
        }

        private void SetFiltersCore(IReadOnlyList<RitsuCatalogFilter> filters)
        {
            ArgumentNullException.ThrowIfNull(filters);
            if (filters.Count > 8)
                throw new ArgumentException("A catalog cannot contain more than eight filter groups.", nameof(filters));
            if (filters.Any(static filter => filter == null))
                throw new ArgumentException("Catalog filters cannot contain null.", nameof(filters));
            if (filters.Select(static filter => filter.Id).Distinct(StringComparer.Ordinal).Count() != filters.Count)
                throw new ArgumentException("Catalog filter IDs must be unique.", nameof(filters));
            _filters = [.. filters];
            _filterSelections.Clear();
            _filterFailureKeys.Clear();
            foreach (var filter in _filters)
                _filterSelections.Add(filter.Id, -1);
        }

        private void BuildUi()
        {
            if (_uiBuilt)
                return;
            _uiBuilt = true;
            CustomMinimumSize = new(0f, _options.MinimumHeight);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;
            MouseFilter = MouseFilterEnum.Pass;

            var workspace = new Control
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            AddChild(workspace);
            workspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

            var browserRow = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            browserRow.AddThemeConstantOverride("separation", 12);
            workspace.AddChild(browserRow);
            browserRow.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

            var inlineDetails = _options.DetailPresentation == RitsuCatalogDetailPresentation.Inline;

            var catalogPanel = new PanelContainer
            {
                CustomMinimumSize = new(inlineDetails ? _options.CatalogWidth : 0f, 0f),
                SizeFlagsHorizontal = inlineDetails ? SizeFlags.ShrinkBegin : SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            catalogPanel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListShellStyle());
            browserRow.AddChild(catalogPanel);
            var catalog = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            catalog.AddThemeConstantOverride("separation", 10);
            catalogPanel.AddChild(catalog);

            _search = ModSettingsUiControlTheming.CreateStyledLineEdit(
                string.Empty,
                _options.SearchPlaceholder,
                0f);
            _search.ClearButtonEnabled = true;
            _search.MaxLength = MaximumSearchTextLength;
            _search.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _search.TextChanged += _ => ScheduleSearch();
            catalog.AddChild(_search);
            AddFilterControls(catalog);

            var summary = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var summaryTitle = new Label
            {
                Text = _options.SearchPlaceholder,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            summaryTitle.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            summaryTitle.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
            summaryTitle.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            summary.AddChild(summaryTitle);
            _resultCount = new()
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
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
            _rowCanvas = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _scrollFrame.AddChild(_rowCanvas);
            _scroll.GetVScrollBar().ValueChanged += _ => RefreshVirtualRows();
            _scroll.Resized += QueueVirtualRefresh;
            _scroll.GetVScrollBar().VisibilityChanged += OnCatalogScrollbarVisibilityChanged;
            _rowCanvas.Resized += QueueVirtualRefresh;

            _emptyLabel = new()
            {
                Text = _options.EmptyText,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _emptyLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            catalogPanel.AddChild(_emptyLabel);
            _emptyLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _emptyLabel.OffsetTop = 112f;

            var detailPanel = new PanelContainer
            {
                CustomMinimumSize = new(_options.DetailMinimumWidth, 0f),
                SizeFlagsHorizontal = inlineDetails ? SizeFlags.ExpandFill : SizeFlags.ShrinkEnd,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Stop,
            };
            detailPanel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateInsetSurfaceStyle());
            if (inlineDetails)
                browserRow.AddChild(detailPanel);
            else
            {
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
                _detailSlideHost.OffsetLeft = -_options.DetailMinimumWidth;
                _detailSlideHost.OffsetRight = 0f;
                _detailSlideHost.OffsetTop = 0f;
                _detailSlideHost.OffsetBottom = 0f;
                workspace.AddChild(_detailSlideHost);
                detailPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                _detailSlideHost.AddChild(detailPanel);
            }

            var detailColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            detailColumn.AddThemeConstantOverride("separation", 0);
            detailPanel.AddChild(detailColumn);
            if (!inlineDetails)
            {
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
                    () => CloseDetailDrawer(true))
                {
                    TooltipText = ModSettingsLocalization.Get("ritsulib.catalog.closeDetails", "Close details"),
                    CustomMinimumSize = new(42f, 38f),
                });
                detailColumn.AddChild(detailHeader);
            }

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
            RebuildDetail();
            SetProcessUnhandledInput(!inlineDetails);
        }

        private void AddFilterControls(VBoxContainer catalog)
        {
            if (_filters.Length == 0)
                return;

            var filterRow = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            filterRow.AddThemeConstantOverride("h_separation", 8);
            filterRow.AddThemeConstantOverride("v_separation", 6);
            catalog.AddChild(filterRow);
            foreach (var filter in _filters)
            {
                var options = new List<(int Value, string Label)> { (-1, $"{filter.Label}: {filter.AllLabel}") };
                options.AddRange(filter.Options.Select((option, index) => (index, $"{filter.Label}: {option.Label}")));
                var dropdown = new ModSettingsDropdownChoiceControl<int>(options, -1, selected =>
                {
                    _filterSelections[filter.Id] = selected;
                    ApplyFilter();
                })
                {
                    CustomMinimumSize = new(190f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
                };
                filterRow.AddChild(dropdown);
            }
        }

        private async void ScheduleSearch()
        {
            if (!IsInsideTree())
                return;
            var revision = ++_searchRevision;
            await ToSignal(GetTree().CreateTimer(SearchDelaySeconds), SceneTreeTimer.SignalName.Timeout);
            if (!IsInsideTree() || revision != _searchRevision)
                return;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var terms = (_search?.Text ?? string.Empty).Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            _filteredItems = _items.Where(item => item.Matches(terms) && MatchesFilters(item)).ToArray();
            if (_resultCount != null)
                _resultCount.Text = _filteredItems.Length == _items.Length
                    ? _items.Length.ToString()
                    : $"{_filteredItems.Length} / {_items.Length}";
            if (_emptyLabel != null)
                _emptyLabel.Visible = _filteredItems.Length == 0;
            UpdateCanvasMinimumSize();

            if (_selectedItemId != null && _filteredItems.All(item => item.Id != _selectedItemId))
                SetSelection(
                    _options.DetailPresentation == RitsuCatalogDetailPresentation.Inline
                        ? _filteredItems.FirstOrDefault()?.Id
                        : null,
                    true);
            else if (_selectedItemId == null &&
                     _options.DetailPresentation == RitsuCatalogDetailPresentation.Inline &&
                     _filteredItems.Length > 0)
                SetSelection(_filteredItems[0].Id, true);
            else
                RebuildDetail();
            QueueVirtualRefresh();
        }

        private bool MatchesFilters(RitsuCatalogItem item)
        {
            foreach (var filter in _filters)
            {
                var optionIndex = _filterSelections.GetValueOrDefault(filter.Id, -1);
                if (optionIndex < 0)
                    continue;
                if (optionIndex >= filter.Options.Count)
                    return false;
                try
                {
                    if (!filter.Options[optionIndex].Matches(item))
                        return false;
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    var failureKey = $"{filter.Id}\0{filter.Options[optionIndex].Id}\0{item.Id}";
                    if (_filterFailureKeys.Add(failureKey))
                        RitsuLibFramework.Logger.Warn(
                            $"[Catalog] Filter '{filter.Id}/{filter.Options[optionIndex].Id}' failed for '{item.Id}': {ex}");
                    return false;
                }
            }

            return true;
        }

        private void QueueVirtualRefresh()
        {
            if (!IsInsideTree() || _virtualRefreshQueued)
                return;
            _virtualRefreshQueued = true;
            Callable.From(() =>
            {
                _virtualRefreshQueued = false;
                RefreshVirtualRows();
            }).CallDeferred();
        }

        private void RefreshVirtualRows()
        {
            if (_scroll == null || _rowCanvas == null || !IsInstanceValid(_scroll))
                return;

            SyncScrollGutter(_scroll, _scrollFrame);
            if (_options.Presentation == RitsuCatalogPresentation.Grid)
            {
                RefreshVirtualTiles();
                return;
            }

            var viewportHeight = Math.Max(_options.RowHeight, _scroll.Size.Y);
            var firstVisible = _filteredItems.Length == 0
                ? 0
                : Math.Clamp((int)MathF.Floor(_scroll.ScrollVertical / _options.RowHeight),
                    0, _filteredItems.Length - 1);
            var visibleCount = Math.Max(1, (int)MathF.Ceiling(viewportHeight / _options.RowHeight) + 1);
            var start = Math.Max(0, firstVisible - VirtualOverscanRows);
            var end = Math.Min(_filteredItems.Length, firstVisible + visibleCount + VirtualOverscanRows);
            var required = Math.Max(0, end - start);
            EnsureRowPool(required);
            for (var slot = 0; slot < _rowPool.Count; slot++)
            {
                var row = _rowPool[slot];
                if (slot >= required)
                {
                    row.Hide();
                    continue;
                }

                var itemIndex = start + slot;
                row.Position = new(0f, itemIndex * _options.RowHeight);
                row.Size = new(Math.Max(0f, _rowCanvas.Size.X), _options.RowHeight - 4f);
                var item = _filteredItems[itemIndex];
                row.Bind(item, ResolveIcon(item), item.Id == _selectedItemId);
                row.Show();
            }

            foreach (var tile in _tilePool)
                tile.Hide();
        }

        private void RefreshVirtualTiles()
        {
            if (_scroll == null || _rowCanvas == null)
                return;

            var width = Math.Max(1f, _rowCanvas.Size.X > 1f ? _rowCanvas.Size.X : _scroll.Size.X);
            var columns = Math.Max(1,
                Mathf.FloorToInt((width + GridGap) / (_options.GridTileMinimumWidth + GridGap)));
            if (_gridColumns != columns)
            {
                _gridColumns = columns;
                UpdateCanvasMinimumSize();
            }

            var rowHeight = _options.GridTileHeight + GridGap;
            var totalRows = Mathf.CeilToInt(_filteredItems.Length / (float)_gridColumns);
            var firstVisibleRow = totalRows == 0
                ? 0
                : Math.Clamp(Mathf.FloorToInt(_scroll.ScrollVertical / rowHeight), 0, totalRows - 1);
            var visibleRows = Math.Max(1, Mathf.CeilToInt(Math.Max(rowHeight, _scroll.Size.Y) / rowHeight) + 1);
            var startRow = Math.Max(0, firstVisibleRow - VirtualOverscanRows);
            var endRow = Math.Min(totalRows, firstVisibleRow + visibleRows + VirtualOverscanRows);
            var start = startRow * _gridColumns;
            var end = Math.Min(_filteredItems.Length, endRow * _gridColumns);
            var required = Math.Max(0, end - start);
            EnsureTilePool(required);

            var tileWidth = Math.Max(1f,
                (width - Math.Max(0, _gridColumns - 1) * GridGap) / _gridColumns);
            for (var slot = 0; slot < _tilePool.Count; slot++)
            {
                var tile = _tilePool[slot];
                if (slot >= required)
                {
                    tile.Hide();
                    continue;
                }

                var itemIndex = start + slot;
                var row = itemIndex / _gridColumns;
                var column = itemIndex % _gridColumns;
                tile.Position = new(column * (tileWidth + GridGap), row * rowHeight);
                tile.Size = new(tileWidth, _options.GridTileHeight);
                var item = _filteredItems[itemIndex];
                tile.Bind(item, ResolveIcon(item), item.Id == _selectedItemId);
                tile.Show();
            }

            foreach (var row in _rowPool)
                row.Hide();
        }

        private void UpdateCanvasMinimumSize()
        {
            if (_rowCanvas == null)
                return;

            if (_options.Presentation == RitsuCatalogPresentation.List)
            {
                _rowCanvas.CustomMinimumSize = new(0f, _filteredItems.Length * _options.RowHeight);
                return;
            }

            var rows = Mathf.CeilToInt(_filteredItems.Length / (float)Math.Max(1, _gridColumns));
            _rowCanvas.CustomMinimumSize = new(0f,
                rows == 0
                    ? 0f
                    : rows * _options.GridTileHeight + Math.Max(0, rows - 1) * GridGap);
        }

        private void EnsureRowPool(int required)
        {
            if (_rowCanvas == null)
                return;
            while (_rowPool.Count < required)
            {
                var row = new CatalogRow();
                row.ItemPressed += itemId => SetSelection(itemId, true);
                _rowCanvas.AddChild(row);
                _rowPool.Add(row);
            }
        }

        private void EnsureTilePool(int required)
        {
            if (_rowCanvas == null)
                return;
            while (_tilePool.Count < required)
            {
                var tile = new CatalogTile();
                tile.ItemPressed += itemId => SetSelection(itemId, true);
                _rowCanvas.AddChild(tile);
                _tilePool.Add(tile);
            }
        }

        private void SetSelection(string? itemId, bool notify)
        {
            if (string.Equals(_selectedItemId, itemId, StringComparison.Ordinal))
            {
                RebuildDetail();
                RefreshVirtualRows();
                return;
            }

            _selectedItemId = itemId;
            RebuildDetail();
            RefreshVirtualRows();
            if (notify && SelectionChanged is { } handlers)
            {
                var eventArgs = new RitsuCatalogSelectionChangedEventArgs(SelectedItem);
                foreach (var handler in handlers.GetInvocationList()
                             .OfType<EventHandler<RitsuCatalogSelectionChangedEventArgs>>())
                    try
                    {
                        handler(this, eventArgs);
                    }
                    catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                    {
                        RitsuLibFramework.Logger.Warn($"[Catalog] Selection callback failed: {ex}");
                    }
            }
        }

        private void CloseDetailDrawer(bool notify)
        {
            if (_options.DetailPresentation != RitsuCatalogDetailPresentation.Drawer)
                return;
            var focusItemId = _selectedItemId;
            SetSelection(null, notify);
            if (focusItemId == null)
                return;
            Callable.From(() => RestoreItemFocus(focusItemId)).CallDeferred();
        }

        private void RestoreItemFocus(string itemId)
        {
            if (!IsInsideTree())
                return;
            Control? control = _options.Presentation == RitsuCatalogPresentation.Grid
                ? _tilePool.FirstOrDefault(tile => tile.ItemId == itemId)
                : _rowPool.FirstOrDefault(row => row.ItemId == itemId);
            control?.GrabFocus();
        }

        private void OnCatalogScrollbarVisibilityChanged()
        {
            SyncScrollGutter(_scroll, _scrollFrame);
            QueueVirtualRefresh();
        }

        private static void SyncScrollGutter(ScrollContainer? scroll, MarginContainer? frame)
        {
            if (scroll == null || frame == null || !IsInstanceValid(scroll) || !IsInstanceValid(frame))
                return;

            var gutter = scroll.GetVScrollBar().Visible
                ? ModSettingsUiControlTheming.ResolveSettingsScrollContentRightGutter(scroll)
                : 0;
            if (frame.GetThemeConstant("margin_right") == gutter)
                return;

            frame.AddThemeConstantOverride("margin_right", gutter);
            frame.QueueSort();
        }

        private void RebuildDetail()
        {
            if (_detailHost == null)
                return;
            if (_detailContent != null && IsInstanceValid(_detailContent))
            {
                if (ReferenceEquals(_detailContent.GetParent(), _detailHost))
                    _detailHost.RemoveChild(_detailContent);
                _detailContent.QueueFree();
            }

            var selected = SelectedItem;
            if (_options.DetailPresentation == RitsuCatalogDetailPresentation.Drawer && selected == null)
            {
                AnimateDetailDrawer(false);
                _detailContent = null;
                return;
            }

            _detailContent = CreateDetailContent(selected);
            _detailContent.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _detailHost.AddChild(_detailContent);
            if (_detailTitle != null)
                _detailTitle.Text = selected?.Title ?? string.Empty;
            if (_options.DetailPresentation == RitsuCatalogDetailPresentation.Drawer)
                AnimateDetailDrawer(true);
        }

        private void AnimateDetailDrawer(bool show)
        {
            if (_detailSlideHost == null)
                return;
            _detailTween?.Kill();
            _detailTween = null;
            var width = _options.DetailMinimumWidth;
            if (show)
            {
                _detailBackdrop?.Show();
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
                if (_detailSlideHost == null || !IsInstanceValid(_detailSlideHost))
                    return;
                _detailSlideHost.Hide();
                SetDetailDrawerOffsets(-width, 0f);
                _detailBackdrop?.Hide();
            }));
        }

        private void SetDetailDrawerOffsets(float left, float right)
        {
            if (_detailSlideHost == null)
                return;
            _detailSlideHost.OffsetLeft = left;
            _detailSlideHost.OffsetRight = right;
        }

        private void OnDetailBackdropInput(InputEvent inputEvent)
        {
            if (inputEvent is not InputEventMouseButton { Pressed: true })
                return;
            CloseDetailDrawer(true);
            GetViewport().SetInputAsHandled();
        }

        private Control CreateDetailContent(RitsuCatalogItem? item)
        {
            if (item == null || _options.DetailFactory == null)
                return CreatePlaceholder(_options.DetailPlaceholderText);
            try
            {
                var content = _options.DetailFactory(item);
                if (content == null || !IsInstanceValid(content))
                    throw new InvalidOperationException("The catalog detail factory returned an invalid control.");
                if (content.GetParent() != null)
                    throw new InvalidOperationException("The catalog detail factory returned an attached control.");
                return content;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[Catalog] Detail factory failed for '{item.Id}': {ex}");
                return CreatePlaceholder($"{item.Title}\n{_options.DetailUnavailableText}", true);
            }
        }

        private static Control CreatePlaceholder(string text, bool error = false)
        {
            var margin = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            margin.AddThemeConstantOverride("margin_left", 18);
            margin.AddThemeConstantOverride("margin_top", 18);
            margin.AddThemeConstantOverride("margin_right", 18);
            margin.AddThemeConstantOverride("margin_bottom", 18);
            var label = new Label
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new(0f, 180f),
            };
            label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            label.AddThemeColorOverride("font_color", error
                ? RitsuShellTheme.Current.Component.TextButton.Danger.Fg
                : RitsuShellTheme.Current.Text.LabelSecondary);
            margin.AddChild(label);
            return margin;
        }

        private RitsuCatalogItem? FindItem(string? itemId)
        {
            return itemId == null
                ? null
                : _items.FirstOrDefault(item => string.Equals(item.Id, itemId, StringComparison.Ordinal));
        }

        private Texture2D? ResolveIcon(RitsuCatalogItem item)
        {
            if (item.Icon != null)
                return IsInstanceValid(item.Icon) ? item.Icon : null;
            if (item.IconFactory == null)
                return null;
            if (_iconCache.TryGetValue(item.Id, out var cached))
                return cached;
            try
            {
                var resolved = item.IconFactory();
                if (resolved != null && !IsInstanceValid(resolved))
                    resolved = null;
                _iconCache.Add(item.Id, resolved);
                return resolved;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[Catalog] Icon factory failed for '{item.Id}': {ex}");
                _iconCache.Add(item.Id, null);
                return null;
            }
        }

        private sealed partial class CatalogTile : Button
        {
            private readonly Label _badge;
            private readonly TextureRect _icon;
            private readonly Label _title;
            private string? _itemId;

            internal CatalogTile()
            {
                FocusMode = FocusModeEnum.All;
                MouseFilter = MouseFilterEnum.Stop;
                Text = string.Empty;
                Pressed += () =>
                {
                    if (_itemId != null)
                        ItemPressed?.Invoke(_itemId);
                };

                var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
                margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                margin.AddThemeConstantOverride("margin_left", 9);
                margin.AddThemeConstantOverride("margin_top", 8);
                margin.AddThemeConstantOverride("margin_right", 9);
                margin.AddThemeConstantOverride("margin_bottom", 8);
                AddChild(margin);

                var column = new VBoxContainer
                {
                    Alignment = BoxContainer.AlignmentMode.Center,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                column.AddThemeConstantOverride("separation", 4);
                margin.AddChild(column);

                _icon = new()
                {
                    CustomMinimumSize = new(48f, 48f),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                column.AddChild(_icon);

                _title = new()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    ClipText = true,
                    TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                _title.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
                _title.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
                _title.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
                column.AddChild(_title);

                _badge = new()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    ClipText = true,
                    TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                _badge.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
                _badge.AddThemeFontSizeOverride("font_size", 12);
                _badge.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
                column.AddChild(_badge);
            }

            internal event Action<string>? ItemPressed;

            internal string? ItemId => _itemId;

            internal void Bind(RitsuCatalogItem item, Texture2D? icon, bool selected)
            {
                _itemId = item.Id;
                _icon.Texture = icon;
                _icon.Visible = icon != null;
                _title.Text = item.Title;
                _badge.Text = item.Badge ?? item.Subtitle ?? string.Empty;
                _badge.Visible = !string.IsNullOrWhiteSpace(_badge.Text);
                TooltipText = ResolveTooltip(item);
                var normal = RitsuShellChromeStyles.CreateListItemCardStyle(selected);
                var emphasis = RitsuShellChromeStyles.CreateListItemCardStyle(true);
                AddThemeStyleboxOverride("normal", normal);
                AddThemeStyleboxOverride("hover", emphasis);
                AddThemeStyleboxOverride("pressed", emphasis);
                AddThemeStyleboxOverride("focus", emphasis);
                AddThemeStyleboxOverride("disabled", normal);
            }
        }

        private sealed partial class CatalogRow : Button
        {
            private readonly Label _badge;
            private readonly TextureRect _icon;
            private readonly Label _subtitle;
            private readonly Label _title;
            private string? _itemId;

            internal CatalogRow()
            {
                FocusMode = FocusModeEnum.All;
                MouseFilter = MouseFilterEnum.Stop;
                SizeFlagsHorizontal = SizeFlags.ExpandFill;
                Text = string.Empty;
                Pressed += () =>
                {
                    if (_itemId != null)
                        ItemPressed?.Invoke(_itemId);
                };

                var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
                margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                margin.AddThemeConstantOverride("margin_left", 8);
                margin.AddThemeConstantOverride("margin_top", 5);
                margin.AddThemeConstantOverride("margin_right", 8);
                margin.AddThemeConstantOverride("margin_bottom", 5);
                AddChild(margin);
                var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
                row.AddThemeConstantOverride("separation", 9);
                margin.AddChild(row);
                _icon = new()
                {
                    CustomMinimumSize = new(44f, 44f),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                row.AddChild(_icon);
                var identity = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                identity.AddThemeConstantOverride("separation", -2);
                _title = new()
                {
                    ClipText = true,
                    TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                _title.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
                _title.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
                identity.AddChild(_title);
                _subtitle = new()
                {
                    ClipText = true,
                    TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                _subtitle.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
                _subtitle.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
                _subtitle.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
                identity.AddChild(_subtitle);
                row.AddChild(identity);
                _badge = new()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                _badge.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
                _badge.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
                _badge.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
                row.AddChild(_badge);
            }

            internal event Action<string>? ItemPressed;

            internal string? ItemId => _itemId;

            internal void Bind(RitsuCatalogItem item, Texture2D? icon, bool selected)
            {
                _itemId = item.Id;
                _icon.Texture = icon;
                _icon.Visible = icon != null;
                _title.Text = item.Title;
                _subtitle.Text = item.Subtitle ?? string.Empty;
                _subtitle.Visible = !string.IsNullOrWhiteSpace(item.Subtitle);
                _badge.Text = item.Badge ?? string.Empty;
                _badge.Visible = !string.IsNullOrWhiteSpace(item.Badge);
                TooltipText = ResolveTooltip(item);
                var normal = RitsuShellChromeStyles.CreateListItemCardStyle(selected);
                var emphasis = RitsuShellChromeStyles.CreateListItemCardStyle(true);
                AddThemeStyleboxOverride("normal", normal);
                AddThemeStyleboxOverride("hover", emphasis);
                AddThemeStyleboxOverride("pressed", emphasis);
                AddThemeStyleboxOverride("focus", emphasis);
                AddThemeStyleboxOverride("disabled", normal);
            }
        }

        private static string ResolveTooltip(RitsuCatalogItem item)
        {
            if (!string.IsNullOrWhiteSpace(item.Tooltip))
                return item.Tooltip;
            if (string.IsNullOrWhiteSpace(item.Subtitle))
                return $"{item.Title}\n{item.Id}";
            if (item.Subtitle.Contains(item.Id, StringComparison.OrdinalIgnoreCase))
                return $"{item.Title}\n{item.Subtitle}";
            return $"{item.Title}\n{item.Subtitle}\n{item.Id}";
        }
    }
}
