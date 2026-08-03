using Godot;

namespace STS2RitsuLib.Ui.Catalog
{
    /// <summary>
    ///     <para xml:lang="en">Selects the item layout used by a <see cref="RitsuCatalogBrowser" />.</para>
    ///     <para xml:lang="zh-CN">选择 <see cref="RitsuCatalogBrowser" /> 使用的目录项布局。</para>
    /// </summary>
    public enum RitsuCatalogPresentation
    {
        /// <summary>
        ///     <para xml:lang="en">Shows one full-width item per row.</para>
        ///     <para xml:lang="zh-CN">每行显示一个占满宽度的目录项。</para>
        /// </summary>
        List,

        /// <summary>
        ///     <para xml:lang="en">Shows compact icon tiles in an adaptive multi-column grid.</para>
        ///     <para xml:lang="zh-CN">以自适应多列网格显示紧凑图标卡片。</para>
        /// </summary>
        Grid,
    }

    /// <summary>
    ///     <para xml:lang="en">Selects how selected-item details are presented by a <see cref="RitsuCatalogBrowser" />.</para>
    ///     <para xml:lang="zh-CN">选择 <see cref="RitsuCatalogBrowser" /> 呈现所选目录项详情的方式。</para>
    /// </summary>
    public enum RitsuCatalogDetailPresentation
    {
        /// <summary>
        ///     <para xml:lang="en">Keeps a compact catalog beside an always-visible detail pane.</para>
        ///     <para xml:lang="zh-CN">在常驻详情面板旁保留紧凑目录。</para>
        /// </summary>
        Inline,

        /// <summary>
        ///     <para xml:lang="en">Uses the full browser for items and slides details over its right edge on demand.</para>
        ///     <para xml:lang="zh-CN">让目录占满浏览器，并在需要时从右侧滑入覆盖式详情抽屉。</para>
        /// </summary>
        Drawer,
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes one immutable item displayed by a <see cref="RitsuCatalogBrowser" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">描述由 <see cref="RitsuCatalogBrowser" /> 显示的一个不可变目录项。</para>
    /// </summary>
    public sealed class RitsuCatalogItem
    {
        /// <summary>
        ///     <para xml:lang="en">The maximum supported length of an item ID.</para>
        ///     <para xml:lang="zh-CN">目录项 ID 支持的最大长度。</para>
        /// </summary>
        public const int MaximumIdLength = 256;

        /// <summary>
        ///     <para xml:lang="en">The maximum supported length of item display text.</para>
        ///     <para xml:lang="zh-CN">目录项显示文本支持的最大长度。</para>
        /// </summary>
        public const int MaximumTextLength = 2048;

        /// <summary>
        ///     <para xml:lang="en">Creates an immutable catalog item.</para>
        ///     <para xml:lang="zh-CN">创建一个不可变目录项。</para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">A non-empty stable ID unique within the browser.</para>
        ///     <para xml:lang="zh-CN">在浏览器中唯一且非空的稳定 ID。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The primary display title.</para>
        ///     <para xml:lang="zh-CN">主要显示标题。</para>
        /// </param>
        /// <param name="subtitle">
        ///     <para xml:lang="en">Optional secondary display text.</para>
        ///     <para xml:lang="zh-CN">可选的次要显示文本。</para>
        /// </param>
        /// <param name="searchText">
        ///     <para xml:lang="en">Optional additional searchable text that is not rendered.</para>
        ///     <para xml:lang="zh-CN">不会显示、但可参与搜索的可选附加文本。</para>
        /// </param>
        /// <param name="icon">
        ///     <para xml:lang="en">Optional icon texture. The browser does not take ownership of the texture.</para>
        ///     <para xml:lang="zh-CN">可选图标纹理；浏览器不取得该纹理的所有权。</para>
        /// </param>
        /// <param name="badge">
        ///     <para xml:lang="en">Optional short badge displayed at the end of the row.</para>
        ///     <para xml:lang="zh-CN">显示在行尾的可选短徽标。</para>
        /// </param>
        /// <param name="iconFactory">
        ///     <para xml:lang="en">
        ///         Optional side-effect-free factory that supplies an icon when the item needs to be displayed. Do not
        ///         provide both <paramref name="icon" /> and this factory. The caller retains ownership of each returned
        ///         texture and must keep it valid while the item may be displayed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选且无副作用的图标工厂，在需要显示该项时提供图标。不得同时提供 <paramref name="icon" /> 和此工厂；
        ///         调用方仍持有每次返回的纹理，并须在该项可能显示期间保持纹理有效。
        ///     </para>
        /// </param>
        /// <param name="tooltip">
        ///     <para xml:lang="en">
        ///         Optional complete hover text. When omitted, the browser derives a hover tip from the title,
        ///         subtitle, and ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">可选的完整悬浮提示文本；省略时浏览器会根据标题、副标题和 ID 生成提示。</para>
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">Thrown when an ID or required title is invalid, or text exceeds its limit.</para>
        ///     <para xml:lang="zh-CN">ID、必需标题无效或文本超过限制时抛出。</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     <para xml:lang="en">Thrown when <paramref name="icon" /> is no longer a valid Godot object.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="icon" /> 不再是有效的 Godot 对象时抛出。</para>
        /// </exception>
        public RitsuCatalogItem(
            string id,
            string title,
            string? subtitle = null,
            string? searchText = null,
            Texture2D? icon = null,
            string? badge = null,
            Func<Texture2D?>? iconFactory = null,
            string? tooltip = null)
        {
            Id = ValidateRequired(id, nameof(id), MaximumIdLength);
            Title = ValidateRequired(title, nameof(title), MaximumTextLength);
            Subtitle = ValidateOptional(subtitle, nameof(subtitle));
            SearchText = ValidateOptional(searchText, nameof(searchText));
            Badge = ValidateOptional(badge, nameof(badge));
            Tooltip = ValidateOptional(tooltip, nameof(tooltip));
            if (icon != null && iconFactory != null)
                throw new ArgumentException("Provide either an icon or an icon factory, not both.",
                    nameof(iconFactory));
#pragma warning disable CA1513 // Preserve the public API's parameter-based ObjectName.
            if (icon != null && !GodotObject.IsInstanceValid(icon))
                throw new ObjectDisposedException(nameof(icon));
#pragma warning restore CA1513
            Icon = icon;
            IconFactory = iconFactory;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the stable item ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的目录项 ID。</para>
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the primary display title.</para>
        ///     <para xml:lang="zh-CN">获取主要显示标题。</para>
        /// </summary>
        public string Title { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional secondary display text.</para>
        ///     <para xml:lang="zh-CN">获取可选的次要显示文本。</para>
        /// </summary>
        public string? Subtitle { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets optional additional searchable text that is not rendered.</para>
        ///     <para xml:lang="zh-CN">获取不会显示、但可参与搜索的可选附加文本。</para>
        /// </summary>
        public string? SearchText { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional icon texture. The caller remains responsible for its lifetime.</para>
        ///     <para xml:lang="zh-CN">获取可选图标纹理；调用方仍负责其生命周期。</para>
        /// </summary>
        public Texture2D? Icon { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional lazy icon factory. The browser invokes it synchronously on the Godot main thread;
        ///         the caller retains ownership of returned textures.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取可选的惰性图标工厂；浏览器会在 Godot 主线程同步调用它，返回纹理仍由调用方持有。
        ///     </para>
        /// </summary>
        public Func<Texture2D?>? IconFactory { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional short row badge.</para>
        ///     <para xml:lang="zh-CN">获取可选的行尾短徽标。</para>
        /// </summary>
        public string? Badge { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional complete hover text.</para>
        ///     <para xml:lang="zh-CN">获取可选的完整悬浮提示文本。</para>
        /// </summary>
        public string? Tooltip { get; }

        internal bool Matches(string[] terms)
        {
            return terms.Length == 0 || terms.All(Matches);
        }

        private bool Matches(string term)
        {
            return Title.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                   (Subtitle?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
                   (SearchText?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
                   Id.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        private static string ValidateRequired(string value, string parameterName, int maximumLength)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
            if (value.Length > maximumLength)
                throw new ArgumentException($"Text cannot exceed {maximumLength} characters.", parameterName);
            return value;
        }

        private static string? ValidateOptional(string? value, string parameterName)
        {
            if (value == null)
                return null;
            if (value.Length > MaximumTextLength)
                throw new ArgumentException($"Text cannot exceed {MaximumTextLength} characters.", parameterName);
            return value;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes one selectable option in a catalog filter group.
    ///     </para>
    ///     <para xml:lang="zh-CN">描述目录筛选组中的一个可选项。</para>
    /// </summary>
    public sealed class RitsuCatalogFilterOption
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a filter option backed by a side-effect-free predicate.</para>
        ///     <para xml:lang="zh-CN">创建由无副作用谓词支持的筛选项。</para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">A non-empty stable ID unique within the filter group.</para>
        ///     <para xml:lang="zh-CN">在筛选组中唯一且非空的稳定 ID。</para>
        /// </param>
        /// <param name="label">
        ///     <para xml:lang="en">The option label shown to the user.</para>
        ///     <para xml:lang="zh-CN">向用户显示的选项标签。</para>
        /// </param>
        /// <param name="matches">
        ///     <para xml:lang="en">
        ///         A fast, side-effect-free predicate. Recoverable callback failures exclude the item and are logged.
        ///     </para>
        ///     <para xml:lang="zh-CN">快速且无副作用的谓词；可恢复的回调失败会排除该项并写入日志。</para>
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">Thrown when the ID or label is empty or exceeds the supported limit.</para>
        ///     <para xml:lang="zh-CN">ID 或标签为空或超过支持的限制时抛出。</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="matches" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="matches" /> 为 null 时抛出。</para>
        /// </exception>
        public RitsuCatalogFilterOption(string id, string label, Func<RitsuCatalogItem, bool> matches)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(label);
            ArgumentNullException.ThrowIfNull(matches);
            if (id.Length > RitsuCatalogItem.MaximumIdLength)
                throw new ArgumentException("Filter option ID is too long.", nameof(id));
            if (label.Length > RitsuCatalogItem.MaximumTextLength)
                throw new ArgumentException("Filter option label is too long.", nameof(label));
            Id = id;
            Label = label;
            Matches = matches;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the stable option ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的选项 ID。</para>
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the displayed option label.</para>
        ///     <para xml:lang="zh-CN">获取显示的选项标签。</para>
        /// </summary>
        public string Label { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the predicate used to include matching items.</para>
        ///     <para xml:lang="zh-CN">获取用于包含匹配目录项的谓词。</para>
        /// </summary>
        public Func<RitsuCatalogItem, bool> Matches { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes one single-choice filter group. The browser automatically adds an unfiltered first option.
    ///     </para>
    ///     <para xml:lang="zh-CN">描述一个单选筛选组；浏览器会自动添加不筛选的首个选项。</para>
    /// </summary>
    public sealed class RitsuCatalogFilter
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a filter group from an immutable snapshot of its options.</para>
        ///     <para xml:lang="zh-CN">根据筛选项的不可变快照创建筛选组。</para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">A non-empty stable ID unique within the browser.</para>
        ///     <para xml:lang="zh-CN">在浏览器中唯一且非空的稳定 ID。</para>
        /// </param>
        /// <param name="label">
        ///     <para xml:lang="en">The filter-group label.</para>
        ///     <para xml:lang="zh-CN">筛选组标签。</para>
        /// </param>
        /// <param name="allLabel">
        ///     <para xml:lang="en">The label used by the automatically supplied unfiltered option.</para>
        ///     <para xml:lang="zh-CN">自动提供的不筛选选项所使用的标签。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">One or more options with unique IDs.</para>
        ///     <para xml:lang="zh-CN">一个或多个具有唯一 ID 的筛选项。</para>
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">Thrown for invalid text, no options, too many options, or duplicate option IDs.</para>
        ///     <para xml:lang="zh-CN">文本无效、没有筛选项、筛选项过多或选项 ID 重复时抛出。</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="options" /> is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="options" /> 为 null 时抛出。</para>
        /// </exception>
        public RitsuCatalogFilter(
            string id,
            string label,
            string allLabel,
            IReadOnlyList<RitsuCatalogFilterOption> options)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(label);
            ArgumentException.ThrowIfNullOrWhiteSpace(allLabel);
            ArgumentNullException.ThrowIfNull(options);
            if (id.Length > RitsuCatalogItem.MaximumIdLength)
                throw new ArgumentException("Filter ID is too long.", nameof(id));
            if (label.Length > RitsuCatalogItem.MaximumTextLength)
                throw new ArgumentException("Filter label is too long.", nameof(label));
            if (allLabel.Length > RitsuCatalogItem.MaximumTextLength)
                throw new ArgumentException("Unfiltered-option label is too long.", nameof(allLabel));
            if (options.Count is <= 0 or > 64)
                throw new ArgumentException("A filter must contain between 1 and 64 options.", nameof(options));
            if (options.Any(static option => option == null))
                throw new ArgumentException("Filter options cannot contain null.", nameof(options));
            if (options.Select(static option => option.Id).Distinct(StringComparer.Ordinal).Count() != options.Count)
                throw new ArgumentException("Filter option IDs must be unique.", nameof(options));
            Id = id;
            Label = label;
            AllLabel = allLabel;
            Options = Array.AsReadOnly([.. options]);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the stable filter-group ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的筛选组 ID。</para>
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the displayed filter-group label.</para>
        ///     <para xml:lang="zh-CN">获取显示的筛选组标签。</para>
        /// </summary>
        public string Label { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the label for the unfiltered option.</para>
        ///     <para xml:lang="zh-CN">获取不筛选选项的标签。</para>
        /// </summary>
        public string AllLabel { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets an immutable snapshot of the filter options.</para>
        ///     <para xml:lang="zh-CN">获取筛选项的不可变快照。</para>
        /// </summary>
        public IReadOnlyList<RitsuCatalogFilterOption> Options { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">Configures presentation and callbacks for a <see cref="RitsuCatalogBrowser" />.</para>
    ///     <para xml:lang="zh-CN">配置 <see cref="RitsuCatalogBrowser" /> 的呈现和回调。</para>
    /// </summary>
    public sealed class RitsuCatalogBrowserOptions
    {
        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the item presentation; the default is a list.</para>
        ///     <para xml:lang="zh-CN">获取或初始化目录项呈现方式；默认为列表。</para>
        /// </summary>
        public RitsuCatalogPresentation Presentation { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes how details are presented. The default is an on-demand drawer that does not
        ///         reduce the catalog's layout width.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或初始化详情呈现方式；默认使用不会缩减目录布局宽度的按需抽屉。</para>
        /// </summary>
        public RitsuCatalogDetailPresentation DetailPresentation { get; init; } =
            RitsuCatalogDetailPresentation.Drawer;

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the search-field placeholder.</para>
        ///     <para xml:lang="zh-CN">获取或初始化搜索框占位文本。</para>
        /// </summary>
        public string SearchPlaceholder { get; init; } = "Search";

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the text displayed when no item matches.</para>
        ///     <para xml:lang="zh-CN">获取或初始化没有匹配项时显示的文本。</para>
        /// </summary>
        public string EmptyText { get; init; } = "No matching items";

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the text displayed before an item is selected.</para>
        ///     <para xml:lang="zh-CN">获取或初始化选择目录项前显示的文本。</para>
        /// </summary>
        public string DetailPlaceholderText { get; init; } = "Select an item to view details";

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the text displayed when an item's details cannot be shown.</para>
        ///     <para xml:lang="zh-CN">获取或初始化无法显示目录项详情时使用的文本。</para>
        /// </summary>
        public string DetailUnavailableText { get; init; } = "Details are unavailable for this item";

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the browser's minimum height, from 240 through 1200 pixels.</para>
        ///     <para xml:lang="zh-CN">获取或初始化浏览器最小高度，范围为 240 至 1200 像素。</para>
        /// </summary>
        public float MinimumHeight { get; init; } = 560f;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes the catalog width in list presentation, from 220 through 720 pixels. Grid
        ///         presentation instead expands the catalog to the available space.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或初始化列表呈现下的目录宽度，范围为 220 至 720 像素；网格呈现会改为占用全部可用空间。
        ///     </para>
        /// </summary>
        public float CatalogWidth { get; init; } = 380f;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes the inline detail-pane minimum width or drawer width, from 280 through 720 pixels.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或初始化常驻详情面板的最小宽度或抽屉宽度，范围为 280 至 720 像素。</para>
        /// </summary>
        public float DetailMinimumWidth { get; init; } = 360f;

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes each catalog item's row height, from 48 through 120 pixels.</para>
        ///     <para xml:lang="zh-CN">获取或初始化每个目录项的行高，范围为 48 至 120 像素。</para>
        /// </summary>
        public float RowHeight { get; init; } = 64f;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes the preferred minimum grid-tile width, from 72 through 320 pixels. The browser
        ///         derives the column count from the available viewport width.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或初始化网格卡片的首选最小宽度，范围为 72 至 320 像素；浏览器会按可用视口宽度计算列数。
        ///     </para>
        /// </summary>
        public float GridTileMinimumWidth { get; init; } = 112f;

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the grid-tile height, from 72 through 240 pixels.</para>
        ///     <para xml:lang="zh-CN">获取或初始化网格卡片高度，范围为 72 至 240 像素。</para>
        /// </summary>
        public float GridTileHeight { get; init; } = 104f;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes the optional factory for selected-item detail content. Each returned control must
        ///         be a live, unattached node; the browser owns and frees it after replacement.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或初始化可选的选中项详情工厂。每次返回的控件必须仍然有效且没有父节点；浏览器会取得所有权并在替换后释放。
        ///     </para>
        /// </summary>
        public Func<RitsuCatalogItem, Control>? DetailFactory { get; init; }

        internal void Validate()
        {
            ValidateText(SearchPlaceholder, nameof(SearchPlaceholder));
            ValidateText(EmptyText, nameof(EmptyText));
            ValidateText(DetailPlaceholderText, nameof(DetailPlaceholderText));
            ValidateText(DetailUnavailableText, nameof(DetailUnavailableText));
            if (MinimumHeight is < 240f or > 1200f || !float.IsFinite(MinimumHeight))
                throw new ArgumentOutOfRangeException(nameof(MinimumHeight));
            if (CatalogWidth is < 220f or > 720f || !float.IsFinite(CatalogWidth))
                throw new ArgumentOutOfRangeException(nameof(CatalogWidth));
            if (DetailMinimumWidth is < 280f or > 720f || !float.IsFinite(DetailMinimumWidth))
                throw new ArgumentOutOfRangeException(nameof(DetailMinimumWidth));
            if (RowHeight is < 48f or > 120f || !float.IsFinite(RowHeight))
                throw new ArgumentOutOfRangeException(nameof(RowHeight));
            if (!Enum.IsDefined(Presentation))
                throw new ArgumentOutOfRangeException(nameof(Presentation));
            if (!Enum.IsDefined(DetailPresentation))
                throw new ArgumentOutOfRangeException(nameof(DetailPresentation));
            if (GridTileMinimumWidth is < 72f or > 320f || !float.IsFinite(GridTileMinimumWidth))
                throw new ArgumentOutOfRangeException(nameof(GridTileMinimumWidth));
            if (GridTileHeight is < 72f or > 240f || !float.IsFinite(GridTileHeight))
                throw new ArgumentOutOfRangeException(nameof(GridTileHeight));
        }

        private static void ValidateText(string value, string parameterName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
            if (value.Length > RitsuCatalogItem.MaximumTextLength)
                throw new ArgumentException("Text is too long.", parameterName);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Provides data for a catalog selection change.</para>
    ///     <para xml:lang="zh-CN">提供目录选择变更的数据。</para>
    /// </summary>
    public sealed class RitsuCatalogSelectionChangedEventArgs : EventArgs
    {
        internal RitsuCatalogSelectionChangedEventArgs(RitsuCatalogItem? item)
        {
            Item = item;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the selected item, or null when the selection was cleared.</para>
        ///     <para xml:lang="zh-CN">获取选中的目录项；清除选择时为 null。</para>
        /// </summary>
        public RitsuCatalogItem? Item { get; }
    }
}
