using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Component tokens grouped by component, then variant/state.
    ///     按组件再按变体/状态分组的组件令牌。
    /// </summary>
    /// <param name="SidebarCard">
    ///     Sidebar mod cards (default and selected).
    ///     侧边栏 mod 卡片 (默认和选中)。
    /// </param>
    /// <param name="ChromeMenu">
    ///     Compact action / menu chrome.
    ///     紧凑动作/菜单 chrome。
    /// </param>
    /// <param name="PageToolbarTray">
    ///     Tray behind per-page toolbar controls.
    ///     Tray behind 每页工具栏控件。
    /// </param>
    /// <param name="ListShell">
    ///     Outer container for scrollable lists.
    ///     可滚动列表的外层容器。
    /// </param>
    /// <param name="ListItem">
    ///     List item card variants.
    ///     列表项卡片变体。
    /// </param>
    /// <param name="ListEditor">
    ///     Inline list-editor surface.
    ///     内联列表编辑器表面。
    /// </param>
    /// <param name="Pill">
    ///     Pill-shaped controls (tags, compact buttons).
    /// </param>
    /// <param name="Toggle">
    ///     Settings toggle states.
    ///     设置 toggle 状态。
    /// </param>
    /// <param name="Slider">
    ///     Slider grab thumb tones.
    ///     滑块抓取拇指色调。
    /// </param>
    /// <param name="Dropdown">
    ///     Dropdown faces and rows.
    ///     下拉框正面和行。
    /// </param>
    /// <param name="Stepper">
    ///     Stepper face states.
    ///     步进器正面状态。
    /// </param>
    /// <param name="DragHandle">
    ///     Drag handles for reorderable lists.
    ///     可重排序列表的拖拽句柄。
    /// </param>
    /// <param name="Collapsible">
    ///     Collapsible section headers.
    ///     可折叠分区标题。
    /// </param>
    /// <param name="SidebarBtn">
    ///     Sidebar tree buttons (page / section / mod / utility / depth variations).
    ///     Sidebar 树按钮 (页面 / 分区 / mod / 工具 / 深度变体)。
    /// </param>
    /// <param name="SidebarRail">
    ///     Section rail background.
    ///     分区轨道背景。
    /// </param>
    /// <param name="TextButton">
    ///     Inline text-button tones (accent / danger / neutral).
    ///     内联文本按钮色调 (强调 / 危险 / 中性)。
    /// </param>
    /// <param name="StringValidation">
    ///     String editor validation chrome (neutral / invalid).
    ///     字符串编辑器校验 chrome (中性 / invalid)。
    /// </param>
    /// <param name="OverlayPanel">
    ///     Floating overlay panel chrome.
    ///     浮动覆盖面板 chrome。
    /// </param>
    /// <param name="ChoiceCenter">
    ///     Choice-center label highlight gradient.
    ///     choice-center 标签高亮渐变。
    /// </param>
    public sealed record ComponentTokens(
        SidebarCardTokens SidebarCard,
        ChromeMenuTokens ChromeMenu,
        PageToolbarTrayTokens PageToolbarTray,
        ListShellTokens ListShell,
        ListItemTokens ListItem,
        ListEditorTokens ListEditor,
        PillTokens Pill,
        ToggleTokens Toggle,
        SliderTokens Slider,
        DropdownTokens Dropdown,
        StepperTokens Stepper,
        DragHandleTokens DragHandle,
        CollapsibleTokens Collapsible,
        SidebarBtnTokens SidebarBtn,
        SidebarRailTokens SidebarRail,
        TextButtonTokens TextButton,
        StringValidationTokens StringValidation,
        OverlayPanelTokens OverlayPanel,
        ChoiceCenterTokens ChoiceCenter);

    /// <summary>
    ///     Sidebar mod card tokens.
    ///     侧边栏 mod 卡牌令牌。
    /// </summary>
    /// <param name="Default">
    ///     Default state (background + border).
    ///     默认状态 (背景 + 边框)。
    /// </param>
    /// <param name="Selected">
    ///     Selected state.
    ///     选中状态。
    /// </param>
    /// <param name="Shadow">
    ///     Drop shadow tint shared by both states.
    ///     投影色调 共享于 both st位于es。
    /// </param>
    public sealed record SidebarCardTokens(BgBorder Default, BgBorder Selected, Color Shadow);

    /// <summary>
    ///     Chrome action menu tokens (hover toggles between two states).
    /// </summary>
    /// <param name="Default">
    ///     Resting state.
    ///     静止状态。
    /// </param>
    /// <param name="Hover">
    ///     Hover state.
    ///     悬停状态。
    /// </param>
    public sealed record ChromeMenuTokens(BgBorder Default, BgBorder Hover);

    /// <summary>
    ///     Page toolbar tray tokens.
    ///     页面工具栏托盘令牌。
    /// </summary>
    /// <param name="Bg">
    ///     Toolbar tray background.
    ///     工具栏托盘背景。
    /// </param>
    /// <param name="Border">
    ///     Toolbar tray border.
    ///     工具栏托盘边框。
    /// </param>
    public sealed record PageToolbarTrayTokens(Color Bg, Color Border);

    /// <summary>
    ///     List shell (outer) tokens.
    ///     列表外壳（外层）令牌。
    /// </summary>
    /// <param name="Bg">
    ///     Shell background.
    ///     外壳背景。
    /// </param>
    /// <param name="Border">
    ///     Shell border.
    ///     外壳边框。
    /// </param>
    /// <param name="Shadow">
    ///     Shell drop shadow.
    ///     外壳投影。
    /// </param>
    public sealed record ListShellTokens(Color Bg, Color Border, Color Shadow);

    /// <summary>
    ///     List item card tokens (default vs accent emphasis).
    ///     列表项卡牌令牌（默认与强调重点）。
    /// </summary>
    /// <param name="Default">
    ///     Resting state.
    ///     静止状态。
    /// </param>
    /// <param name="Accent">
    ///     Accent / selected emphasis.
    ///     强调/选中重点。
    /// </param>
    /// <param name="Shadow">
    ///     Card drop shadow tint.
    ///     卡片投影色调。
    /// </param>
    public sealed record ListItemTokens(BgBorder Default, BgBorder Accent, Color Shadow);

    /// <summary>
    ///     Inline list editor surface tokens.
    ///     内联列表编辑器表面令牌。
    /// </summary>
    /// <param name="Bg">
    ///     Editor background.
    ///     编辑器背景。
    /// </param>
    /// <param name="Border">
    ///     Editor border.
    ///     编辑器边框。
    /// </param>
    public sealed record ListEditorTokens(Color Bg, Color Border);

    /// <summary>
    ///     Pill / tag tokens.
    ///     胶囊/标签令牌。
    /// </summary>
    /// <param name="Default">
    ///     Resting state.
    ///     静止状态。
    /// </param>
    /// <param name="Hover">
    ///     Hover state.
    ///     悬停状态。
    /// </param>
    public sealed record PillTokens(BgBorder Default, BgBorder Hover);

    /// <summary>
    ///     Toggle tokens. Borders inherit if a state omits them.
    ///     开关令牌。如果某个状态省略边框，则会继承边框。
    /// </summary>
    /// <param name="On">
    ///     Pressed/on state.
    ///     按下/开启状态。
    /// </param>
    /// <param name="Off">
    ///     Off state.
    ///     关闭状态。
    /// </param>
    /// <param name="OffHover">
    ///     Off + hover state (border falls back to <see cref="Off" />).
    ///     关闭 + 悬停状态 (边框回退到 <see cref="Off" />)。
    /// </param>
    /// <param name="Disabled">
    ///     Disabled state.
    ///     禁用状态。
    /// </param>
    /// <param name="Shadow">
    ///     Neutral (non-hover) shadow tint.
    ///     中性（非悬停）阴影色调。
    /// </param>
    public sealed record ToggleTokens(
        BgBorder On,
        BgBorder Off,
        BgBorder OffHover,
        BgBorder Disabled,
        Color Shadow);

    /// <summary>
    ///     Slider grab tokens.
    ///     滑块抓取令牌。
    /// </summary>
    /// <param name="GrabHighlight">
    ///     Outer highlight tint.
    ///     外层高亮色调。
    /// </param>
    /// <param name="GrabShadow">
    ///     Inner shadow tint.
    ///     内层阴影色调。
    /// </param>
    public sealed record SliderTokens(Color GrabHighlight, Color GrabShadow);

    /// <summary>
    ///     Dropdown face tokens covering the four interaction states.
    /// </summary>
    /// <param name="Open">
    ///     Default open state (face + border).
    ///     默认打开状态 (正面 + 边框)。
    /// </param>
    /// <param name="Hover">
    ///     Hover state. Border falls back to <see cref="Open" />.
    ///     悬停状态. Border 回退到 <see cref="Open" />。
    /// </param>
    /// <param name="Pressed">
    ///     Pressed state. Border falls back to <see cref="Open" />.
    ///     按下状态. Border 回退到 <see cref="Open" />。
    /// </param>
    /// <param name="Focus">
    ///     Focus state (own border).
    ///     聚焦状态 (自有边框)。
    /// </param>
    public sealed record DropdownTokens(
        BgBorder Open,
        BgBorder Hover,
        BgBorder Pressed,
        BgBorder Focus);

    /// <summary>
    ///     Stepper face tokens.
    ///     步进器表面令牌。
    /// </summary>
    /// <param name="Default">
    ///     Default state (background + border).
    ///     默认状态 (背景 + 边框)。
    /// </param>
    /// <param name="Hover">
    ///     Hover state.
    ///     悬停状态。
    /// </param>
    /// <param name="Neutral">
    ///     Hidden / neutral state used when the face has no visible affordance.
    ///     表面没有可见操作提示时使用的隐藏/中性状态。
    /// </param>
    public sealed record StepperTokens(BgBorder Default, BgBorder Hover, BgBorder Neutral);

    /// <summary>
    ///     Drag handle tokens.
    ///     拖拽句柄令牌。
    /// </summary>
    /// <param name="Default">
    ///     Resting state.
    ///     静止状态。
    /// </param>
    /// <param name="Selected">
    ///     Selected (active) state.
    ///     Selected (active) state.
    /// </param>
    public sealed record DragHandleTokens(BgBorder Default, BgBorder Selected);

    /// <summary>
    ///     Collapsible section header tokens.
    ///     可折叠分区标题令牌。
    /// </summary>
    /// <param name="Default">
    ///     Resting state.
    ///     静止状态。
    /// </param>
    /// <param name="Hover">
    ///     Hover state.
    ///     悬停状态。
    /// </param>
    /// <param name="Selected">
    ///     Selected (expanded) state.
    ///     选中（展开）状态。
    /// </param>
    /// <param name="Disabled">
    ///     Disabled state (content unavailable, but header may remain interactive).
    ///     禁用状态 (内容不可用, but 标题可能仍可交互)。
    /// </param>
    public sealed record CollapsibleTokens(BgBorder Default, BgBorder Hover, BgBorder Selected, BgBorder Disabled);

    /// <summary>
    ///     Sidebar tree button tokens. Each variant covers a distinct depth or kind of row.
    ///     侧边栏树按钮令牌。每个变体覆盖不同深度或不同类型的行。
    /// </summary>
    /// <param name="Default">
    ///     Default page row.
    ///     默认页面行。
    /// </param>
    /// <param name="Hover">
    ///     Default page hover.
    ///     默认页面悬停。
    /// </param>
    /// <param name="Selected">
    ///     Selected default page row.
    ///     选中的默认页面行。
    /// </param>
    /// <param name="SelectedHover">
    ///     Selected default page hover.
    ///     选中的默认页面悬停。
    /// </param>
    /// <param name="UtilitySelected">
    ///     Selected utility row (only background tinted).
    ///     选中的工具行 (仅背景着色)。
    /// </param>
    /// <param name="IdleDeep">
    ///     Idle page at deeper depth.
    ///     更深层级的空闲页面。
    /// </param>
    /// <param name="IdleDeepHover">
    ///     Idle deep + hover.
    ///     深层空闲 + 悬停。
    /// </param>
    /// <param name="IdleDeeper">
    ///     Idle page at the deepest tracked depth.
    ///     已跟踪最深层级的空闲页面。
    /// </param>
    /// <param name="IdleDeeperHover">
    ///     Idle deeper + hover.
    ///     更深层空闲 + 悬停。
    /// </param>
    /// <param name="Mod">
    ///     Mod row.
    ///     Mod 行。
    /// </param>
    /// <param name="ModHover">
    ///     Mod row hover.
    ///     Mod 行悬停。
    /// </param>
    /// <param name="ModDeep">
    ///     Mod row at deeper depth.
    ///     更深层级的 mod 行。
    /// </param>
    /// <param name="DeepBorder">
    ///     Border tint shared by deeper rows.
    ///     边框色调 共享于 较深层级行。
    /// </param>
    /// <param name="DeepBorderHover">
    ///     Hover border tint for deeper rows.
    ///     较深层级行的悬停边框色调。
    /// </param>
    /// <param name="Shadow">
    ///     Drop shadow tint.
    ///     投影色调。
    /// </param>
    public sealed record SidebarBtnTokens(
        BgBorder Default,
        BgBorder Hover,
        BgBorder Selected,
        BgBorder SelectedHover,
        BgBorder UtilitySelected,
        BgBorder IdleDeep,
        BgBorder IdleDeepHover,
        BgBorder IdleDeeper,
        BgBorder IdleDeeperHover,
        BgBorder Mod,
        BgBorder ModHover,
        BgBorder ModDeep,
        Color DeepBorder,
        Color DeepBorderHover,
        Color Shadow);

    /// <summary>
    ///     Sidebar section rail (background + outline).
    ///     侧边栏分区轨道 (背景 + 轮廓)。
    /// </summary>
    /// <param name="Bg">
    ///     Rail background.
    ///     轨道背景。
    /// </param>
    /// <param name="Border">
    ///     Rail outline.
    ///     轨道轮廓。
    /// </param>
    public sealed record SidebarRailTokens(Color Bg, Color Border);

    /// <summary>
    ///     Inline text button tokens, organised by tone.
    ///     按色调组织的内联文本按钮令牌。
    /// </summary>
    /// <param name="Accent">
    ///     Accent (highlighted) tone.
    ///     强调（高亮）色调。
    /// </param>
    /// <param name="Danger">
    ///     Danger tone.
    ///     危险色调。
    /// </param>
    /// <param name="Neutral">
    ///     Neutral tone.
    ///     中性色调。
    /// </param>
    public sealed record TextButtonTokens(
        TextButtonToneTokens Accent,
        TextButtonToneTokens Danger,
        TextButtonToneTokens Neutral);

    /// <summary>
    ///     Per-tone foreground + background pair for inline text buttons.
    ///     内联文本按钮按色调区分的前景 + 背景配对。
    /// </summary>
    /// <param name="Fg">
    ///     Foreground (label) color.
    ///     前景（标签）颜色。
    /// </param>
    /// <param name="Bg">
    ///     Default tinted background (used for selected/hovered rows).
    ///     默认着色背景（用于选中/悬停行）。
    /// </param>
    /// <param name="BgHover">
    ///     Hover variation of the tinted background.
    ///     着色背景的悬停变体。
    /// </param>
    public sealed record TextButtonToneTokens(Color Fg, Color Bg, Color BgHover);

    /// <summary>
    ///     String editor validation chrome (background + border for neutral / invalid states).
    ///     字符串编辑器校验 chrome（中性/无效状态的背景 + 边框）。
    /// </summary>
    /// <param name="Neutral">
    ///     Neutral state.
    ///     中性状态。
    /// </param>
    /// <param name="Invalid">
    ///     Invalid state.
    ///     无效状态。
    /// </param>
    public sealed record StringValidationTokens(BgBorder Neutral, BgBorder Invalid);

    /// <summary>
    ///     Floating overlay panel chrome.
    ///     浮动覆盖面板 chrome。
    /// </summary>
    /// <param name="Bg">
    ///     Panel background.
    ///     面板背景。
    /// </param>
    /// <param name="Border">
    ///     Panel border.
    ///     面板边框。
    /// </param>
    public sealed record OverlayPanelTokens(Color Bg, Color Border);

    /// <summary>
    ///     Highlight tones used by the choice-center widget.
    ///     choice-center 小部件使用的高亮色调。
    /// </summary>
    /// <param name="HighlightTop">
    ///     Top of the gradient.
    ///     渐变顶部。
    /// </param>
    /// <param name="HighlightBottom">
    ///     Bottom of the gradient.
    ///     渐变底部。
    /// </param>
    public sealed record ChoiceCenterTokens(Color HighlightTop, Color HighlightBottom);

    /// <summary>
    ///     Common (background + border) pair shared by many component states.
    ///     通用 (背景 + 边框) pair 共享于 多个组件状态。
    /// </summary>
    /// <param name="Bg">
    ///     Background fill.
    ///     背景填充。
    /// </param>
    /// <param name="Border">
    ///     Border tint.
    ///     边框色调。
    /// </param>
    public sealed record BgBorder(Color Bg, Color Border);
}
