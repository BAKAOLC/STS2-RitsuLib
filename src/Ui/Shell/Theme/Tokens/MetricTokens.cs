namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     <para xml:lang="en">Groups shell-theme dimensions, spacing, typography sizes, and layout behavior.</para>
    ///     <para xml:lang="zh-CN">归组外壳主题的尺寸、间距、排版大小及布局行为。</para>
    /// </summary>
    /// <param name="Radius">
    ///     <para xml:lang="en">The corner-radius scale.</para>
    ///     <para xml:lang="zh-CN">圆角半径规格。</para>
    /// </param>
    /// <param name="BorderWidth">
    ///     <para xml:lang="en">The border-width scale.</para>
    ///     <para xml:lang="zh-CN">边框宽度规格。</para>
    /// </param>
    /// <param name="Entry">
    ///     <para xml:lang="en">The entry and row dimensions.</para>
    ///     <para xml:lang="zh-CN">条目及行的尺寸。</para>
    /// </param>
    /// <param name="Slider">
    ///     <para xml:lang="en">The slider dimensions.</para>
    ///     <para xml:lang="zh-CN">滑块尺寸。</para>
    /// </param>
    /// <param name="Choice">
    ///     <para xml:lang="en">The choice and stepper dimensions.</para>
    ///     <para xml:lang="zh-CN">选择控件及步进器的尺寸。</para>
    /// </param>
    /// <param name="Color">
    ///     <para xml:lang="en">The color-picker row dimensions.</para>
    ///     <para xml:lang="zh-CN">颜色选择器行的尺寸。</para>
    /// </param>
    /// <param name="StringEntry">
    ///     <para xml:lang="en">The string-editor dimensions.</para>
    ///     <para xml:lang="zh-CN">字符串编辑器尺寸。</para>
    /// </param>
    /// <param name="Keybinding">
    ///     <para xml:lang="en">The key-binding editor dimensions.</para>
    ///     <para xml:lang="zh-CN">按键绑定编辑器尺寸。</para>
    /// </param>
    /// <param name="Overlay">
    ///     <para xml:lang="en">The floating-overlay dimensions.</para>
    ///     <para xml:lang="zh-CN">浮动层尺寸。</para>
    /// </param>
    /// <param name="Sidebar">
    ///     <para xml:lang="en">The sidebar navigation dimensions and behavior.</para>
    ///     <para xml:lang="zh-CN">侧边栏导航的尺寸及行为。</para>
    /// </param>
    /// <param name="FontSize">
    ///     <para xml:lang="en">The font-size scale.</para>
    ///     <para xml:lang="zh-CN">字号规格。</para>
    /// </param>
    public sealed record MetricTokens(
        RadiusMetrics Radius,
        BorderWidthMetrics BorderWidth,
        EntryMetrics Entry,
        SliderMetrics Slider,
        ChoiceMetrics Choice,
        ColorRowMetrics Color,
        StringEntryMetrics StringEntry,
        KeybindingMetrics Keybinding,
        OverlayMetrics Overlay,
        SidebarMetrics Sidebar,
        FontSizeMetrics FontSize);

    /// <summary>
    ///     <para xml:lang="en">Groups fallback corner radii for common shell surfaces.</para>
    ///     <para xml:lang="zh-CN">归组常见外壳表面的备用圆角半径。</para>
    /// </summary>
    /// <param name="Default">
    ///     <para xml:lang="en">The default style-box corner radius.</para>
    ///     <para xml:lang="zh-CN">默认样式框圆角半径。</para>
    /// </param>
    /// <param name="Validation">
    ///     <para xml:lang="en">The corner radius of validation frames.</para>
    ///     <para xml:lang="zh-CN">验证边框的圆角半径。</para>
    /// </param>
    /// <param name="Overlay">
    ///     <para xml:lang="en">The corner radius of floating overlay panels.</para>
    ///     <para xml:lang="zh-CN">浮动层面板的圆角半径。</para>
    /// </param>
    public sealed record RadiusMetrics(int Default, int Validation, int Overlay);

    /// <summary>
    ///     <para xml:lang="en">Groups fallback border widths by visual emphasis.</para>
    ///     <para xml:lang="zh-CN">按视觉强调程度归组备用边框宽度。</para>
    /// </summary>
    /// <param name="Thin">
    ///     <para xml:lang="en">The thin border width.</para>
    ///     <para xml:lang="zh-CN">细边框宽度。</para>
    /// </param>
    /// <param name="Normal">
    ///     <para xml:lang="en">The normal emphasis border width.</para>
    ///     <para xml:lang="zh-CN">普通强调边框宽度。</para>
    /// </param>
    /// <param name="Thick">
    ///     <para xml:lang="en">The strong emphasis border width.</para>
    ///     <para xml:lang="zh-CN">强强调边框宽度。</para>
    /// </param>
    /// <param name="Overlay">
    ///     <para xml:lang="en">The border width of floating overlay panels.</para>
    ///     <para xml:lang="zh-CN">浮动层面板的边框宽度。</para>
    /// </param>
    public sealed record BorderWidthMetrics(int Thin, int Normal, int Thick, int Overlay);

    /// <summary>
    ///     <para xml:lang="en">Groups common entry and value-control dimensions.</para>
    ///     <para xml:lang="zh-CN">归组常见条目及值控件的尺寸。</para>
    /// </summary>
    /// <param name="ValueMinWidth">
    ///     <para xml:lang="en">The default minimum width of compact value controls.</para>
    ///     <para xml:lang="zh-CN">紧凑值控件的默认最小宽度。</para>
    /// </param>
    /// <param name="ValueMinHeight">
    ///     <para xml:lang="en">The default minimum height of value controls.</para>
    ///     <para xml:lang="zh-CN">值控件的默认最小高度。</para>
    /// </param>
    /// <param name="MiniStepperButtonSize">
    ///     <para xml:lang="en">The width and height of compact stepper buttons.</para>
    ///     <para xml:lang="zh-CN">紧凑步进按钮的宽度及高度。</para>
    /// </param>
    public sealed record EntryMetrics(float ValueMinWidth, float ValueMinHeight, int MiniStepperButtonSize);

    /// <summary>
    ///     <para xml:lang="en">Groups slider-row, track, and value-field dimensions.</para>
    ///     <para xml:lang="zh-CN">归组滑块行、轨道及数值字段的尺寸。</para>
    /// </summary>
    /// <param name="RowMinWidth">
    ///     <para xml:lang="en">The minimum width of a slider row.</para>
    ///     <para xml:lang="zh-CN">滑块行的最小宽度。</para>
    /// </param>
    /// <param name="TrackMinWidth">
    ///     <para xml:lang="en">The minimum width reserved for the horizontal slider track.</para>
    ///     <para xml:lang="zh-CN">为水平滑块轨道预留的最小宽度。</para>
    /// </param>
    /// <param name="ValueFieldWidth">
    ///     <para xml:lang="en">The width of the inline numeric field beside a slider.</para>
    ///     <para xml:lang="zh-CN">滑块旁内联数值字段的宽度。</para>
    /// </param>
    /// <param name="ValueFieldHeight">
    ///     <para xml:lang="en">The height of the inline numeric field beside a slider.</para>
    ///     <para xml:lang="zh-CN">滑块旁内联数值字段的高度。</para>
    /// </param>
    public sealed record SliderMetrics(
        float RowMinWidth,
        float TrackMinWidth,
        float ValueFieldWidth,
        float ValueFieldHeight);

    /// <summary>
    ///     <para xml:lang="en">Groups choice-stepper row dimensions.</para>
    ///     <para xml:lang="zh-CN">归组选择步进器行的尺寸。</para>
    /// </summary>
    /// <param name="RowMinWidth">
    ///     <para xml:lang="en">The minimum width of the complete stepper row.</para>
    ///     <para xml:lang="zh-CN">完整步进器行的最小宽度。</para>
    /// </param>
    /// <param name="CenterMinWidth">
    ///     <para xml:lang="en">The minimum width of the center label area.</para>
    ///     <para xml:lang="zh-CN">中央标签区域的最小宽度。</para>
    /// </param>
    public sealed record ChoiceMetrics(float RowMinWidth, float CenterMinWidth);

    /// <summary>
    ///     <para xml:lang="en">Groups color-picker row and swatch dimensions.</para>
    ///     <para xml:lang="zh-CN">归组颜色选择器行及色块的尺寸。</para>
    /// </summary>
    /// <param name="RowMinWidth">
    ///     <para xml:lang="en">The minimum width of a color-picker row.</para>
    ///     <para xml:lang="zh-CN">颜色选择器行的最小宽度。</para>
    /// </param>
    /// <param name="SwatchSize">
    ///     <para xml:lang="en">The width and height of the color swatch.</para>
    ///     <para xml:lang="zh-CN">颜色色块的宽度及高度。</para>
    /// </param>
    public sealed record ColorRowMetrics(float RowMinWidth, float SwatchSize);

    /// <summary>
    ///     <para xml:lang="en">Groups single-line and multiline string-editor dimensions.</para>
    ///     <para xml:lang="zh-CN">归组单行及多行字符串编辑器的尺寸。</para>
    /// </summary>
    /// <param name="MinWidth">
    ///     <para xml:lang="en">The minimum width of a single-line string entry.</para>
    ///     <para xml:lang="zh-CN">单行字符串条目的最小宽度。</para>
    /// </param>
    /// <param name="MultilineMinHeight">
    ///     <para xml:lang="en">The minimum height of a multiline string entry.</para>
    ///     <para xml:lang="zh-CN">多行字符串条目的最小高度。</para>
    /// </param>
    public sealed record StringEntryMetrics(float MinWidth, float MultilineMinHeight);

    /// <summary>
    ///     <para xml:lang="en">Groups key-binding editor dimensions and helper-text sizing.</para>
    ///     <para xml:lang="zh-CN">归组按键绑定编辑器的尺寸及辅助文本字号。</para>
    /// </summary>
    /// <param name="BlockWidth">
    ///     <para xml:lang="en">The width of the key-binding control block.</para>
    ///     <para xml:lang="zh-CN">按键绑定控件块的宽度。</para>
    /// </param>
    /// <param name="CaptureMinWidth">
    ///     <para xml:lang="en">The minimum width of the key-capture button.</para>
    ///     <para xml:lang="zh-CN">按键捕获按钮的最小宽度。</para>
    /// </param>
    /// <param name="HintFontSize">
    ///     <para xml:lang="en">The font size of key-binding helper text.</para>
    ///     <para xml:lang="zh-CN">按键绑定辅助文本的字号。</para>
    /// </param>
    public sealed record KeybindingMetrics(float BlockWidth, float CaptureMinWidth, int HintFontSize);

    /// <summary>
    ///     <para xml:lang="en">Groups the content padding of floating overlay panels.</para>
    ///     <para xml:lang="zh-CN">归组浮动层面板的内容内边距。</para>
    /// </summary>
    /// <param name="PaddingH">
    ///     <para xml:lang="en">The horizontal content padding.</para>
    ///     <para xml:lang="zh-CN">水平内容内边距。</para>
    /// </param>
    /// <param name="PaddingV">
    ///     <para xml:lang="en">The vertical content padding.</para>
    ///     <para xml:lang="zh-CN">垂直内容内边距。</para>
    /// </param>
    public sealed record OverlayMetrics(int PaddingH, int PaddingV);

    /// <summary>
    ///     <para xml:lang="en">Groups sidebar navigation dimensions, spacing, and optional metadata display.</para>
    ///     <para xml:lang="zh-CN">归组侧边栏导航的尺寸、间距及可选元数据显示行为。</para>
    /// </summary>
    /// <param name="Width">
    ///     <para xml:lang="en">The minimum width of the sidebar column.</para>
    ///     <para xml:lang="zh-CN">侧边栏列的最小宽度。</para>
    /// </param>
    /// <param name="PageRowMinHeight">
    ///     <para xml:lang="en">The minimum height of a page row.</para>
    ///     <para xml:lang="zh-CN">页面行的最小高度。</para>
    /// </param>
    /// <param name="SectionRowMinHeight">
    ///     <para xml:lang="en">The minimum height of a section-navigation row.</para>
    ///     <para xml:lang="zh-CN">分区导航行的最小高度。</para>
    /// </param>
    /// <param name="ModListSeparation">
    ///     <para xml:lang="en">The vertical separation between mod cards.</para>
    ///     <para xml:lang="zh-CN">模组卡片之间的垂直间距。</para>
    /// </param>
    /// <param name="ModCardInnerSeparation">
    ///     <para xml:lang="en">The vertical separation within an expanded mod card.</para>
    ///     <para xml:lang="zh-CN">展开的模组卡片内部的垂直间距。</para>
    /// </param>
    /// <param name="PageTreeSeparation">
    ///     <para xml:lang="en">The separation between stacked root-page rows.</para>
    ///     <para xml:lang="zh-CN">堆叠的根页面行之间的间距。</para>
    /// </param>
    /// <param name="SectionRailSeparation">
    ///     <para xml:lang="en">The separation between section-navigation rows.</para>
    ///     <para xml:lang="zh-CN">分区导航行之间的间距。</para>
    /// </param>
    /// <param name="CardInnerMargin">
    ///     <para xml:lang="en">The content margin of compact sidebar mod cards.</para>
    ///     <para xml:lang="zh-CN">紧凑侧边栏模组卡片的内容边距。</para>
    /// </param>
    /// <param name="ShowInlinePageCount">
    ///     <para xml:lang="en">
    ///         <see langword="true" /> to show the localized page-count line within expanded mod cards.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         若要在展开的模组卡片内显示本地化页面计数行，则为 <see langword="true" />。
    ///     </para>
    /// </param>
    public sealed record SidebarMetrics(
        float Width,
        float PageRowMinHeight,
        float SectionRowMinHeight,
        int ModListSeparation,
        int ModCardInnerSeparation,
        int PageTreeSeparation,
        int SectionRailSeparation,
        int CardInnerMargin,
        bool ShowInlinePageCount);

    /// <summary>
    ///     <para xml:lang="en">Groups font sizes used by shell controls, navigation, and overlays.</para>
    ///     <para xml:lang="zh-CN">归组外壳控件、导航及浮层使用的字号。</para>
    /// </summary>
    /// <param name="Button">
    ///     <para xml:lang="en">The default font size of standard buttons.</para>
    ///     <para xml:lang="zh-CN">标准按钮的默认字号。</para>
    /// </param>
    /// <param name="MiniButton">
    ///     <para xml:lang="en">The font size of compact buttons.</para>
    ///     <para xml:lang="zh-CN">紧凑按钮的字号。</para>
    /// </param>
    /// <param name="ValueLabel">
    ///     <para xml:lang="en">The font size of drop-down faces and center labels in steppers.</para>
    ///     <para xml:lang="zh-CN">下拉框表面及步进器中央标签的字号。</para>
    /// </param>
    /// <param name="PopupRow">
    ///     <para xml:lang="en">The font size of rows in drop-down and other pop-ups.</para>
    ///     <para xml:lang="zh-CN">下拉框及其他弹出框中各行的字号。</para>
    /// </param>
    /// <param name="HintSmall">
    ///     <para xml:lang="en">The font size of small inline hints.</para>
    ///     <para xml:lang="zh-CN">小型内联提示的字号。</para>
    /// </param>
    /// <param name="Tooltip">
    ///     <para xml:lang="en">The font size of native <c>TooltipLabel</c> controls.</para>
    ///     <para xml:lang="zh-CN">原生 <c>TooltipLabel</c> 控件的字号。</para>
    /// </param>
    /// <param name="Grip">
    ///     <para xml:lang="en">The font size of glyph-based drag grips.</para>
    ///     <para xml:lang="zh-CN">字形拖动握柄的字号。</para>
    /// </param>
    /// <param name="PillCount">
    ///     <para xml:lang="en">The font size of list count badges.</para>
    ///     <para xml:lang="zh-CN">列表计数徽标的字号。</para>
    /// </param>
    /// <param name="Secondary">
    ///     <para xml:lang="en">The font size of secondary text.</para>
    ///     <para xml:lang="zh-CN">次要文本的字号。</para>
    /// </param>
    /// <param name="HeaderArrow">
    ///     <para xml:lang="en">The font size of header arrow glyphs.</para>
    ///     <para xml:lang="zh-CN">标题栏箭头字形的字号。</para>
    /// </param>
    /// <param name="HeaderTitle">
    ///     <para xml:lang="en">The font size of collapsible-header titles.</para>
    ///     <para xml:lang="zh-CN">可折叠标题栏标题的字号。</para>
    /// </param>
    /// <param name="HeaderSubtitle">
    ///     <para xml:lang="en">The font size of header subtitles.</para>
    ///     <para xml:lang="zh-CN">标题栏副标题的字号。</para>
    /// </param>
    /// <param name="PageDescription">
    ///     <para xml:lang="en">The font size of page descriptions in the toolbar area.</para>
    ///     <para xml:lang="zh-CN">工具栏区域内页面说明的字号。</para>
    /// </param>
    /// <param name="OverlayTitle">
    ///     <para xml:lang="en">The font size of floating-overlay titles.</para>
    ///     <para xml:lang="zh-CN">浮动层标题的字号。</para>
    /// </param>
    /// <param name="OverlayBody">
    ///     <para xml:lang="en">The font size of floating-overlay body text.</para>
    ///     <para xml:lang="zh-CN">浮动层正文的字号。</para>
    /// </param>
    /// <param name="OverlayPath">
    ///     <para xml:lang="en">The font size of path labels in floating overlays.</para>
    ///     <para xml:lang="zh-CN">浮动层中路径标签的字号。</para>
    /// </param>
    /// <param name="SettingsEntryButton">
    ///     <para xml:lang="en">The font size of the vanilla settings-entry button label.</para>
    ///     <para xml:lang="zh-CN">原版设置入口按钮标签的字号。</para>
    /// </param>
    /// <param name="SettingLineTitle">
    ///     <para xml:lang="en">The primary title font size of a settings-entry row.</para>
    ///     <para xml:lang="zh-CN">设置条目行主标题的字号。</para>
    /// </param>
    public sealed record FontSizeMetrics(
        int Button,
        int MiniButton,
        int ValueLabel,
        int PopupRow,
        int HintSmall,
        int Tooltip,
        int Grip,
        int PillCount,
        int Secondary,
        int HeaderArrow,
        int HeaderTitle,
        int HeaderSubtitle,
        int PageDescription,
        int OverlayTitle,
        int OverlayBody,
        int OverlayPath,
        int SettingsEntryButton,
        int SettingLineTitle);
}
