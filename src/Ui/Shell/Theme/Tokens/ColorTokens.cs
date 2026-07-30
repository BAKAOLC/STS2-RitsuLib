using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     <para xml:lang="en">Groups primitive colors used without component-level state or variant scoping.</para>
    ///     <para xml:lang="zh-CN">归组不受组件状态或变体作用域限制的基础颜色。</para>
    /// </summary>
    /// <param name="White">
    ///     <para xml:lang="en">The plain white tint used by active controls and overlays.</para>
    ///     <para xml:lang="zh-CN">活动控件及浮层使用的纯白色调。</para>
    /// </param>
    /// <param name="Transparent">
    ///     <para xml:lang="en">The fully transparent color.</para>
    ///     <para xml:lang="zh-CN">完全透明的颜色。</para>
    /// </param>
    /// <param name="Divider">
    ///     <para xml:lang="en">The thin divider color used between sections.</para>
    ///     <para xml:lang="zh-CN">分隔各区域所用的细分隔线颜色。</para>
    /// </param>
    /// <param name="UnsetPreview">
    ///     <para xml:lang="en">The preview tint for a value that has not been committed.</para>
    ///     <para xml:lang="zh-CN">尚未提交的值所用的预览色调。</para>
    /// </param>
    /// <param name="ModalBackdrop">
    ///     <para xml:lang="en">The dimming color behind modal panels.</para>
    ///     <para xml:lang="zh-CN">模态面板后方的变暗背景色。</para>
    /// </param>
    /// <param name="Shadow">
    ///     <para xml:lang="en">The shared shadow colors.</para>
    ///     <para xml:lang="zh-CN">共享的阴影颜色。</para>
    /// </param>
    public sealed record ColorTokens(
        Color White,
        Color Transparent,
        Color Divider,
        Color UnsetPreview,
        Color ModalBackdrop,
        ShadowTokens Shadow);

    /// <summary>
    ///     <para xml:lang="en">Groups shadow colors that are not bound to a specific component.</para>
    ///     <para xml:lang="zh-CN">归组不与特定组件绑定的阴影颜色。</para>
    /// </summary>
    /// <param name="Ambient">
    ///     <para xml:lang="en">The soft ambient shadow used by elevated controls.</para>
    ///     <para xml:lang="zh-CN">抬升控件使用的柔和环境阴影。</para>
    /// </param>
    public sealed record ShadowTokens(Color Ambient);

    /// <summary>
    ///     <para xml:lang="en">Groups colors used by rich text, labels, hints, and control text.</para>
    ///     <para xml:lang="zh-CN">归组富文本、标签、提示及控件文本所用的颜色。</para>
    /// </summary>
    /// <param name="RichTitle">
    ///     <para xml:lang="en">The rich-text title color.</para>
    ///     <para xml:lang="zh-CN">富文本标题颜色。</para>
    /// </param>
    /// <param name="RichBody">
    ///     <para xml:lang="en">The rich-text body color.</para>
    ///     <para xml:lang="zh-CN">富文本正文颜色。</para>
    /// </param>
    /// <param name="RichSecondary">
    ///     <para xml:lang="en">The secondary rich-text color used by descriptions and subtitles.</para>
    ///     <para xml:lang="zh-CN">描述及副标题使用的次要富文本颜色。</para>
    /// </param>
    /// <param name="RichMuted">
    ///     <para xml:lang="en">The muted rich-text color used for low-priority information.</para>
    ///     <para xml:lang="zh-CN">低优先级信息使用的弱化富文本颜色。</para>
    /// </param>
    /// <param name="LabelPrimary">
    ///     <para xml:lang="en">The primary label color.</para>
    ///     <para xml:lang="zh-CN">主要标签颜色。</para>
    /// </param>
    /// <param name="LabelSecondary">
    ///     <para xml:lang="en">The secondary label color used by subtitles and disabled text.</para>
    ///     <para xml:lang="zh-CN">副标题及禁用文本使用的次要标签颜色。</para>
    /// </param>
    /// <param name="SidebarSection">
    ///     <para xml:lang="en">The color of section headings in the sidebar.</para>
    ///     <para xml:lang="zh-CN">侧边栏分区标题的颜色。</para>
    /// </param>
    /// <param name="HoverHighlight">
    ///     <para xml:lang="en">The foreground color for highlighted, hovered, pressed, or focused controls.</para>
    ///     <para xml:lang="zh-CN">突出、悬停、按下或聚焦控件的前景色。</para>
    /// </param>
    /// <param name="Number">
    ///     <para xml:lang="en">The color of numeric value labels.</para>
    ///     <para xml:lang="zh-CN">数值标签的颜色。</para>
    /// </param>
    /// <param name="Grip">
    ///     <para xml:lang="en">The color of drag-handle grip glyphs.</para>
    ///     <para xml:lang="zh-CN">拖动手柄握持字形的颜色。</para>
    /// </param>
    /// <param name="Hint">
    ///     <para xml:lang="en">The inline hint and tooltip text color.</para>
    ///     <para xml:lang="zh-CN">内联提示及工具提示文本的颜色。</para>
    /// </param>
    /// <param name="DropdownRow">
    ///     <para xml:lang="en">The foreground color of rows in a drop-down pop-up.</para>
    ///     <para xml:lang="zh-CN">下拉弹出框中各行的前景色。</para>
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
    ///     <para xml:lang="en">Groups colors shared by panes, entries, and framed surfaces.</para>
    ///     <para xml:lang="zh-CN">归组窗格、条目及带框表面共享的颜色。</para>
    /// </summary>
    /// <param name="Sidebar">
    ///     <para xml:lang="en">The settings sidebar background.</para>
    ///     <para xml:lang="zh-CN">设置侧边栏的背景色。</para>
    /// </param>
    /// <param name="Content">
    ///     <para xml:lang="en">The settings content-pane background.</para>
    ///     <para xml:lang="zh-CN">设置内容窗格的背景色。</para>
    /// </param>
    /// <param name="Entry">
    ///     <para xml:lang="en">The background, border, and shadow colors of standard entry surfaces.</para>
    ///     <para xml:lang="zh-CN">标准条目表面的背景、边框及阴影颜色。</para>
    /// </param>
    /// <param name="Inset">
    ///     <para xml:lang="en">The colors of recessed surfaces used for nested content.</para>
    ///     <para xml:lang="zh-CN">嵌套内容所用凹陷表面的颜色。</para>
    /// </param>
    /// <param name="Framed">
    ///     <para xml:lang="en">The border and shadow colors of large framed panes.</para>
    ///     <para xml:lang="zh-CN">大型带框窗格的边框及阴影颜色。</para>
    /// </param>
    public sealed record SurfaceTokens(
        Color Sidebar,
        Color Content,
        EntrySurfaceTokens Entry,
        InsetSurfaceTokens Inset,
        FramedSurfaceTokens Framed);

    /// <summary>
    ///     <para xml:lang="en">Groups colors used by standard entry containers.</para>
    ///     <para xml:lang="zh-CN">归组标准条目容器使用的颜色。</para>
    /// </summary>
    /// <param name="Bg">
    ///     <para xml:lang="en">The background fill color.</para>
    ///     <para xml:lang="zh-CN">背景填充颜色。</para>
    /// </param>
    /// <param name="Border">
    ///     <para xml:lang="en">The border color.</para>
    ///     <para xml:lang="zh-CN">边框颜色。</para>
    /// </param>
    /// <param name="Shadow">
    ///     <para xml:lang="en">The drop-shadow color.</para>
    ///     <para xml:lang="zh-CN">投影颜色。</para>
    /// </param>
    public sealed record EntrySurfaceTokens(Color Bg, Color Border, Color Shadow);

    /// <summary>
    ///     <para xml:lang="en">Groups colors used by recessed surfaces for nested content.</para>
    ///     <para xml:lang="zh-CN">归组嵌套内容所用凹陷表面的颜色。</para>
    /// </summary>
    /// <param name="Bg">
    ///     <para xml:lang="en">The recessed background fill color.</para>
    ///     <para xml:lang="zh-CN">凹陷背景的填充颜色。</para>
    /// </param>
    /// <param name="Border">
    ///     <para xml:lang="en">The recessed-surface border color.</para>
    ///     <para xml:lang="zh-CN">凹陷表面的边框颜色。</para>
    /// </param>
    public sealed record InsetSurfaceTokens(Color Bg, Color Border);

    /// <summary>
    ///     <para xml:lang="en">Groups colors shared by large framed panes.</para>
    ///     <para xml:lang="zh-CN">归组大型带框窗格共享的颜色。</para>
    /// </summary>
    /// <param name="Border">
    ///     <para xml:lang="en">The frame border color.</para>
    ///     <para xml:lang="zh-CN">外框边框颜色。</para>
    /// </param>
    /// <param name="Shadow">
    ///     <para xml:lang="en">The frame shadow color.</para>
    ///     <para xml:lang="zh-CN">外框阴影颜色。</para>
    /// </param>
    public sealed record FramedSurfaceTokens(Color Border, Color Shadow);
}
