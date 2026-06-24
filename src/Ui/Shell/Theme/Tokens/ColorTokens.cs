using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Top-level palette of primitive colors that are used directly without component-level scoping.
    ///     顶层调色板 of primitive 颜色 that are 直接使用 与out 组件级作用域。
    /// </summary>
    /// <param name="White">
    ///     Plain white tint used by overlays and active accents.
    ///     覆盖层和活动强调色使用的纯白色调。
    /// </param>
    /// <param name="Transparent">
    ///     Fully transparent color (<c>#00000000</c>).
    ///     完全透明颜色 (<c>#00000000</c>)。
    /// </param>
    /// <param name="Divider">
    ///     Hairline divider color used between sections.
    /// </param>
    /// <param name="UnsetPreview">
    ///     Highlight tint used to indicate values not yet committed.
    /// </param>
    /// <param name="ModalBackdrop">
    ///     Dim color used as the backdrop behind modal panels.
    /// </param>
    /// <param name="Shadow">
    ///     Shared shadow tones.
    ///     共享阴影色调。
    /// </param>
    public sealed record ColorTokens(
        Color White,
        Color Transparent,
        Color Divider,
        Color UnsetPreview,
        Color ModalBackdrop,
        ShadowTokens Shadow);

    /// <summary>
    ///     Generic shadow colors not bound to a specific component.
    ///     未绑定到特定组件的通用阴影颜色。
    /// </summary>
    /// <param name="Ambient">
    ///     Soft ambient drop shadow used by elevated controls.
    ///     柔和环境投影 used by 抬升控件。
    /// </param>
    public sealed record ShadowTokens(Color Ambient);

    /// <summary>
    ///     Colors used by typography (rich text, labels, hints).
    ///     排版使用的颜色 (富文本, 标签, 提示)。
    /// </summary>
    /// <param name="RichTitle">
    ///     Rich text title tone.
    ///     Rich 文本 标题色调。
    /// </param>
    /// <param name="RichBody">
    ///     Rich text body tone.
    ///     Rich 文本 正文字调。
    /// </param>
    /// <param name="RichSecondary">
    ///     Secondary rich text (descriptions, sub-titles).
    ///     次级 富文本 (描述, 副标题)。
    /// </param>
    /// <param name="RichMuted">
    ///     Muted rich text (low-importance hints).
    ///     弱化 富文本 (低重要性提示)。
    /// </param>
    /// <param name="LabelPrimary">
    ///     Primary label color.
    ///     主标签颜色。
    /// </param>
    /// <param name="LabelSecondary">
    ///     Secondary label color (used by subtitles).
    ///     次级标签颜色 (used by subtitles)。
    /// </param>
    /// <param name="SidebarSection">
    ///     Section header label tone in the sidebar.
    ///     侧边栏中的分区标题标签色调。
    /// </param>
    /// <param name="HoverHighlight">
    ///     Foreground color used by buttons in hover/pressed/focus.
    ///     前景色 used by 按钮 in 悬停/按下/聚焦。
    /// </param>
    /// <param name="Number">
    ///     Numeric value label tone.
    ///     数值标签色调。
    /// </param>
    /// <param name="Grip">
    ///     Drag-handle grip glyph tone.
    ///     拖拽句柄握持字形色调。
    /// </param>
    /// <param name="Hint">
    ///     Inline hint tone.
    ///     内联提示色调。
    /// </param>
    /// <param name="DropdownRow">
    ///     Dropdown popup row foreground.
    ///     下拉弹出行前景色。
    /// </param>
    public sealed record TextTokens(
        Color RichTitle,
        Color RichBody,
        Color RichSecondary,
        Color RichMuted,
        Color LabelPrimary,
        Color LabelSecondary,
        Color SidebarSection,
        Color HoverHighlight,
        Color Number,
        Color Grip,
        Color Hint,
        Color DropdownRow);

    /// <summary>
    ///     Surface backgrounds shared by panes and entry chrome.
    ///     由窗格和条目 chrome 共享的表面背景。
    /// </summary>
    /// <param name="Sidebar">
    ///     Settings sidebar pane background.
    ///     设置侧边栏窗格背景。
    /// </param>
    /// <param name="Content">
    ///     Settings content pane background.
    ///     设置内容窗格背景。
    /// </param>
    /// <param name="Entry">
    ///     Standard entry surface (background + border + shadow).
    ///     标准条目表面 (背景 + 边框 + shadow)。
    /// </param>
    /// <param name="Inset">
    ///     Recessed surface for nested content.
    ///     用于嵌套内容的凹陷表面。
    /// </param>
    /// <param name="Framed">
    ///     Large pane chrome (border + shadow).
    ///     大窗格 chrome (边框 + shadow)。
    /// </param>
    public sealed record SurfaceTokens(
        Color Sidebar,
        Color Content,
        EntrySurfaceTokens Entry,
        InsetSurfaceTokens Inset,
        FramedSurfaceTokens Framed);

    /// <summary>
    ///     Chrome surface used by entry containers (toggle backgrounds, dropdown faces, ...).
    ///     chrome 表面 used by 条目容器 (toggle 背景, dropdown 正面,...)。
    /// </summary>
    /// <param name="Bg">
    ///     Background fill.
    ///     背景填充。
    /// </param>
    /// <param name="Border">
    ///     Border tint.
    ///     边框色调。
    /// </param>
    /// <param name="Shadow">
    ///     Drop shadow tint.
    ///     投影色调。
    /// </param>
    public sealed record EntrySurfaceTokens(Color Bg, Color Border, Color Shadow);

    /// <summary>
    ///     Recessed (inset) surface chrome for nested content.
    ///     用于嵌套内容的凹陷（内嵌）表面 chrome。
    /// </summary>
    /// <param name="Bg">
    ///     Inset background fill.
    ///     内嵌背景填充。
    /// </param>
    /// <param name="Border">
    ///     Inset border tint.
    ///     内嵌边框色调。
    /// </param>
    public sealed record InsetSurfaceTokens(Color Bg, Color Border);

    /// <summary>
    ///     Framed pane chrome shared by the large settings panes.
    ///     大型设置窗格共享的带框窗格 chrome。
    /// </summary>
    /// <param name="Border">
    ///     Frame border tint.
    ///     框架边框色调。
    /// </param>
    /// <param name="Shadow">
    ///     Frame shadow tint.
    ///     框架阴影色调。
    /// </param>
    public sealed record FramedSurfaceTokens(Color Border, Color Shadow);
}
