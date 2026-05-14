namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Numeric metrics grouped by intent.
    ///     按意图分组的数值指标。
    /// </summary>
    /// <param name="Radius">
    ///     Corner radius scale.
    ///     圆角半径刻度。
    /// </param>
    /// <param name="BorderWidth">
    ///     Border width scale.
    ///     边框宽度刻度。
    /// </param>
    /// <param name="Entry">
    ///     Entry / row sizing.
    ///     条目/行尺寸。
    /// </param>
    /// <param name="Slider">
    ///     Slider sizing.
    ///     滑块尺寸。
    /// </param>
    /// <param name="Choice">
    ///     Choice / stepper row sizing.
    ///     选择/步进器行尺寸。
    /// </param>
    /// <param name="Color">
    ///     Color picker row sizing.
    ///     颜色选择器行尺寸。
    /// </param>
    /// <param name="StringEntry">
    ///     String editor sizing.
    ///     字符串编辑器尺寸。
    /// </param>
    /// <param name="Keybinding">
    ///     Keybinding capture sizing.
    ///     按键绑定捕获尺寸。
    /// </param>
    /// <param name="Overlay">
    ///     Floating overlay sizing.
    ///     浮动覆盖层尺寸。
    /// </param>
    /// <param name="Sidebar">
    ///     Sidebar navigation sizing and behavior.
    ///     侧边栏导航的尺寸和行为。
    /// </param>
    /// <param name="FontSize">
    ///     Font size scale.
    ///     字体大小刻度。
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
    ///     Corner radius scale.
    ///     圆角半径刻度。
    /// </summary>
    /// <param name="Default">
    ///     Shared StyleBox corner radius.
    ///     共享 StyleBox 的圆角半径。
    /// </param>
    /// <param name="Validation">
    ///     Corner radius for validation chrome.
    ///     校验装饰区域的圆角半径。
    /// </param>
    /// <param name="Overlay">
    ///     Corner radius for floating overlay panels.
    ///     浮动叠加面板的圆角半径。
    /// </param>
    public sealed record RadiusMetrics(int Default, int Validation, int Overlay);

    /// <summary>
    ///     Border width scale.
    ///     边框宽度刻度。
    /// </summary>
    /// <param name="Thin">
    ///     Hairline border.
    ///     细线边框。
    /// </param>
    /// <param name="Normal">
    ///     Default emphasis border.
    ///     默认强调边框。
    /// </param>
    /// <param name="Thick">
    ///     Strong emphasis border.
    ///     强强调边框。
    /// </param>
    /// <param name="Overlay">
    ///     Border width for floating overlay panels.
    ///     浮动叠加面板的边框宽度。
    /// </param>
    public sealed record BorderWidthMetrics(int Thin, int Normal, int Thick, int Overlay);

    /// <summary>
    ///     Entry / row sizing.
    ///     条目 / 行尺寸。
    /// </summary>
    /// <param name="ValueMinWidth">
    ///     Default minimum width for compact value widgets.
    ///     紧凑值控件的默认最小宽度。
    /// </param>
    /// <param name="ValueMinHeight">
    ///     Fixed vertical size for value column widgets.
    ///     值列控件的固定垂直尺寸。
    /// </param>
    /// <param name="MiniStepperButtonSize">
    ///     Square size for compact stepper buttons.
    ///     紧凑步进按钮的正方形尺寸。
    /// </param>
    public sealed record EntryMetrics(float ValueMinWidth, float ValueMinHeight, int MiniStepperButtonSize);

    /// <summary>
    ///     Slider sizing.
    ///     滑块尺寸。
    /// </summary>
    /// <param name="RowMinWidth">
    ///     Slider row minimum width.
    ///     滑块行的最小宽度。
    /// </param>
    /// <param name="TrackMinWidth">
    ///     Minimum width reserved for the HSlider track.
    ///     为 HSlider 轨道预留的最小宽度。
    /// </param>
    /// <param name="ValueFieldWidth">
    ///     Width of the inline numeric field next to sliders.
    ///     滑块旁内联数值字段的宽度。
    /// </param>
    /// <param name="ValueFieldHeight">
    ///     Height of the inline numeric field next to sliders.
    ///     滑块旁内联数值字段的高度。
    /// </param>
    public sealed record SliderMetrics(
        float RowMinWidth,
        float TrackMinWidth,
        float ValueFieldWidth,
        float ValueFieldHeight);

    /// <summary>
    ///     Choice (stepper) row sizing.
    ///     选择（步进器）行尺寸。
    /// </summary>
    /// <param name="RowMinWidth">
    ///     Stepper row total width.
    ///     步进器行总宽度。
    /// </param>
    /// <param name="CenterMinWidth">
    ///     Minimum width for the center label area.
    ///     中心标签区域的最小宽度。
    /// </param>
    public sealed record ChoiceMetrics(float RowMinWidth, float CenterMinWidth);

    /// <summary>
    ///     Color picker row sizing.
    ///     颜色选择器行尺寸。
    /// </summary>
    /// <param name="RowMinWidth">
    ///     Color row minimum width.
    ///     颜色行的最小宽度。
    /// </param>
    /// <param name="SwatchSize">
    ///     Swatch size.
    ///     色块尺寸。
    /// </param>
    public sealed record ColorRowMetrics(float RowMinWidth, float SwatchSize);

    /// <summary>
    ///     String editor sizing.
    ///     字符串编辑器尺寸。
    /// </summary>
    /// <param name="MinWidth">
    ///     Single-line string entry minimum width.
    ///     单行字符串条目的最小宽度。
    /// </param>
    /// <param name="MultilineMinHeight">
    ///     Multiline string entry minimum height.
    ///     多行字符串条目的最小高度。
    /// </param>
    public sealed record StringEntryMetrics(float MinWidth, float MultilineMinHeight);

    /// <summary>
    ///     Keybinding capture sizing.
    ///     按键绑定捕获尺寸。
    /// </summary>
    /// <param name="BlockWidth">
    ///     Keybinding block width.
    ///     按键绑定块宽度。
    /// </param>
    /// <param name="CaptureMinWidth">
    ///     Minimum width of the keybinding capture button.
    ///     按键绑定捕获按钮的最小宽度。
    /// </param>
    /// <param name="HintFontSize">
    ///     Font size for keybinding helper text.
    ///     按键绑定辅助文本的字体大小。
    /// </param>
    public sealed record KeybindingMetrics(float BlockWidth, float CaptureMinWidth, int HintFontSize);

    /// <summary>
    ///     Floating overlay panel sizing.
    ///     浮动叠加面板尺寸。
    /// </summary>
    /// <param name="PaddingH">
    ///     Horizontal padding.
    ///     水平内边距。
    /// </param>
    /// <param name="PaddingV">
    ///     Vertical padding.
    ///     垂直内边距。
    /// </param>
    public sealed record OverlayMetrics(int PaddingH, int PaddingV);

    /// <summary>
    ///     Sidebar navigation sizing and behavior.
    ///     侧边栏导航的尺寸和行为。
    /// </summary>
    /// <param name="Width">
    ///     Fixed sidebar column width.
    ///     固定侧边栏列宽。
    /// </param>
    /// <param name="PageRowMinHeight">
    ///     Minimum row height for a page tab.
    ///     页面标签的最小行高。
    /// </param>
    /// <param name="SectionRowMinHeight">
    ///     Minimum row height for a section jump row.
    ///     小节跳转行的最小行高。
    /// </param>
    /// <param name="ModListSeparation">
    ///     Vertical gap between mod cards.
    ///     mod 卡片之间的垂直间距。
    /// </param>
    /// <param name="ModCardInnerSeparation">
    ///     Vertical gap inside an expanded mod card.
    ///     展开的 mod 卡片内部垂直间距。
    /// </param>
    /// <param name="PageTreeSeparation">
    ///     Gap between stacked root page rows.
    ///     堆叠根页面行之间的间距。
    /// </param>
    /// <param name="SectionRailSeparation">
    ///     Gap between section rail rows.
    ///     小节导航栏行之间的间距。
    /// </param>
    /// <param name="CardInnerMargin">
    ///     Inner margin for compact sidebar mod cards.
    ///     紧凑侧边栏 mod 卡片的内侧边距。
    /// </param>
    /// <param name="ShowInlinePageCount">
    ///     When true, expanded mod cards show an inline "N pages" line.
    ///     为 true 时，展开的 mod 卡片会显示内联的 "N pages" 行。
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
    ///     Font size scale used across the shell.
    /// </summary>
    /// <param name="Button">
    ///     Default font size for standard buttons.
    ///     标准按钮的默认字体大小。
    /// </param>
    /// <param name="MiniButton">
    ///     Font size for compact (mini) buttons.
    ///     紧凑（迷你）按钮的字体大小。
    /// </param>
    /// <param name="ValueLabel">
    ///     Font size for dropdown faces and stepper center labels.
    ///     下拉框表面和步进器中心标签的字体大小。
    /// </param>
    /// <param name="PopupRow">
    ///     Font size for dropdown / popup rows.
    ///     下拉 / 弹出行的字体大小。
    /// </param>
    /// <param name="HintSmall">
    ///     Font size for small inline hints.
    ///     小型内联提示的字体大小。
    /// </param>
    /// <param name="Tooltip">
    ///     Font size for native control tooltips (TooltipLabel).
    ///     原生控件工具提示（TooltipLabel）的字体大小。
    /// </param>
    /// <param name="Grip">
    ///     Font size for glyph-style grip labels.
    ///     字形样式拖拽柄标签的字体大小。
    /// </param>
    /// <param name="PillCount">
    ///     Font size for list count badges.
    ///     列表计数徽标的字体大小。
    /// </param>
    /// <param name="Secondary">
    ///     Font size for secondary text.
    ///     次要文本的字体大小。
    /// </param>
    /// <param name="HeaderArrow">
    ///     Font size for header arrow glyphs.
    ///     标题箭头字形的字体大小。
    /// </param>
    /// <param name="HeaderTitle">
    ///     Font size for collapsible header titles.
    ///     可折叠标题栏标题的字体大小。
    /// </param>
    /// <param name="HeaderSubtitle">
    ///     Font size for header subtitles.
    ///     标题栏副标题的字体大小。
    /// </param>
    /// <param name="PageDescription">
    ///     Font size for page descriptions in the toolbar area.
    ///     工具栏区域页面说明的字体大小。
    /// </param>
    /// <param name="OverlayTitle">
    ///     Font size for floating overlay titles.
    ///     浮动叠加层标题的字体大小。
    /// </param>
    /// <param name="OverlayBody">
    ///     Font size for floating overlay body lines.
    ///     浮动叠加层正文行的字体大小。
    /// </param>
    /// <param name="OverlayPath">
    ///     Font size for floating overlay path labels.
    ///     浮动叠加层路径标签的字体大小。
    /// </param>
    /// <param name="SettingsEntryButton">
    ///     Font size for the vanilla settings entry button label.
    ///     原版设置条目按钮标签的字体大小。
    /// </param>
    /// <param name="SettingLineTitle">
    ///     Primary title font size for each settings entry row label.
    ///     每个设置条目行标签的主标题字体大小。
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
