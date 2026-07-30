using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     <para xml:lang="en">Groups shell-theme colors by component, variant, and interaction state.</para>
    ///     <para xml:lang="zh-CN">按组件、变体及交互状态分类定义 Shell 主题颜色。</para>
    /// </summary>
    /// <param name="SidebarCard">
    ///     <para xml:lang="en">The default and selected sidebar mod-card states.</para>
    ///     <para xml:lang="zh-CN">侧边栏模组卡片的默认及选中状态。</para>
    /// </param>
    /// <param name="ChromeMenu">
    ///     <para xml:lang="en">The compact action-menu states.</para>
    ///     <para xml:lang="zh-CN">紧凑操作菜单的状态。</para>
    /// </param>
    /// <param name="PageToolbarTray">
    ///     <para xml:lang="en">The tray behind page-level toolbar controls.</para>
    ///     <para xml:lang="zh-CN">页面级工具栏控件后方的托盘。</para>
    /// </param>
    /// <param name="ListShell">
    ///     <para xml:lang="en">The outer container of scrollable lists.</para>
    ///     <para xml:lang="zh-CN">可滚动列表的外层容器。</para>
    /// </param>
    /// <param name="ListItem">
    ///     <para xml:lang="en">The list-item card variants.</para>
    ///     <para xml:lang="zh-CN">列表项卡片的变体。</para>
    /// </param>
    /// <param name="ListEditor">
    ///     <para xml:lang="en">The inline list-editor panel.</para>
    ///     <para xml:lang="zh-CN">内联列表编辑器面板。</para>
    /// </param>
    /// <param name="Pill">
    ///     <para xml:lang="en">The states of pill-shaped tags and compact buttons.</para>
    ///     <para xml:lang="zh-CN">胶囊形标签及紧凑按钮的状态。</para>
    /// </param>
    /// <param name="Toggle">
    ///     <para xml:lang="en">The settings-toggle states.</para>
    ///     <para xml:lang="zh-CN">设置开关的状态。</para>
    /// </param>
    /// <param name="Slider">
    ///     <para xml:lang="en">The slider thumb highlight and shadow colors.</para>
    ///     <para xml:lang="zh-CN">滑块柄的高光及阴影颜色。</para>
    /// </param>
    /// <param name="Dropdown">
    ///     <para xml:lang="en">The drop-down face interaction states.</para>
    ///     <para xml:lang="zh-CN">下拉框正面的交互状态。</para>
    /// </param>
    /// <param name="Stepper">
    ///     <para xml:lang="en">The stepper face states.</para>
    ///     <para xml:lang="zh-CN">步进器正面的状态。</para>
    /// </param>
    /// <param name="DragHandle">
    ///     <para xml:lang="en">The drag-handle states of reorderable lists.</para>
    ///     <para xml:lang="zh-CN">可重新排序列表中拖动手柄的状态。</para>
    /// </param>
    /// <param name="Collapsible">
    ///     <para xml:lang="en">The collapsible section-header states.</para>
    ///     <para xml:lang="zh-CN">可折叠分区标题栏的状态。</para>
    /// </param>
    /// <param name="SidebarBtn">
    ///     <para xml:lang="en">The sidebar tree-row kinds, depths, and interaction states.</para>
    ///     <para xml:lang="zh-CN">侧边栏树状行的类型、深度及交互状态。</para>
    /// </param>
    /// <param name="SidebarRail">
    ///     <para xml:lang="en">The section-navigation rail colors.</para>
    ///     <para xml:lang="zh-CN">分区导航轨道的颜色。</para>
    /// </param>
    /// <param name="TextButton">
    ///     <para xml:lang="en">The accent, danger, and neutral inline text-button tones.</para>
    ///     <para xml:lang="zh-CN">内联文本按钮的强调、危险及中性色调。</para>
    /// </param>
    /// <param name="StringValidation">
    ///     <para xml:lang="en">The neutral and invalid string-editor validation states.</para>
    ///     <para xml:lang="zh-CN">字符串编辑器验证边框的中性及无效状态。</para>
    /// </param>
    /// <param name="OverlayPanel">
    ///     <para xml:lang="en">The floating-overlay panel colors.</para>
    ///     <para xml:lang="zh-CN">浮动层面板的颜色。</para>
    /// </param>
    /// <param name="ChoiceCenter">
    ///     <para xml:lang="en">The highlight gradient of a choice control's center label.</para>
    ///     <para xml:lang="zh-CN">选择控件中央标签的高亮渐变。</para>
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
    ///     <para xml:lang="en">Groups sidebar mod-card colors.</para>
    ///     <para xml:lang="zh-CN">集中定义侧边栏模组卡片的颜色。</para>
    /// </summary>
    /// <param name="Default">
    ///     <para xml:lang="en">The default background and border.</para>
    ///     <para xml:lang="zh-CN">默认状态的背景及边框。</para>
    /// </param>
    /// <param name="Selected">
    ///     <para xml:lang="en">The selected-state background and border.</para>
    ///     <para xml:lang="zh-CN">选中状态的背景及边框。</para>
    /// </param>
    /// <param name="Shadow">
    ///     <para xml:lang="en">The drop-shadow color shared by both states.</para>
    ///     <para xml:lang="zh-CN">两种状态共享的投影颜色。</para>
    /// </param>
    public sealed record SidebarCardTokens(BgBorder Default, BgBorder Selected, Color Shadow);

    /// <summary>
    ///     <para xml:lang="en">Groups compact action-menu colors.</para>
    ///     <para xml:lang="zh-CN">集中定义紧凑操作菜单的颜色。</para>
    /// </summary>
    /// <param name="Default">
    ///     <para xml:lang="en">The resting background and border.</para>
    ///     <para xml:lang="zh-CN">静止状态的背景及边框。</para>
    /// </param>
    /// <param name="Hover">
    ///     <para xml:lang="en">The hover-state background and border.</para>
    ///     <para xml:lang="zh-CN">悬停状态的背景及边框。</para>
    /// </param>
    public sealed record ChromeMenuTokens(BgBorder Default, BgBorder Hover);

    /// <summary>
    ///     <para xml:lang="en">Groups page-toolbar tray colors.</para>
    ///     <para xml:lang="zh-CN">集中定义页面工具栏托盘的颜色。</para>
    /// </summary>
    /// <param name="Bg">
    ///     <para xml:lang="en">The tray background color.</para>
    ///     <para xml:lang="zh-CN">托盘背景颜色。</para>
    /// </param>
    /// <param name="Border">
    ///     <para xml:lang="en">The tray border color.</para>
    ///     <para xml:lang="zh-CN">托盘边框颜色。</para>
    /// </param>
    public sealed record PageToolbarTrayTokens(Color Bg, Color Border);

    /// <summary>
    ///     <para xml:lang="en">Groups colors used by a list's outer container.</para>
    ///     <para xml:lang="zh-CN">集中定义列表外层容器使用的颜色。</para>
    /// </summary>
    /// <param name="Bg">
    ///     <para xml:lang="en">The outer-container background color.</para>
    ///     <para xml:lang="zh-CN">外层容器的背景颜色。</para>
    /// </param>
    /// <param name="Border">
    ///     <para xml:lang="en">The outer-container border color.</para>
    ///     <para xml:lang="zh-CN">外层容器的边框颜色。</para>
    /// </param>
    /// <param name="Shadow">
    ///     <para xml:lang="en">The outer-container drop-shadow color.</para>
    ///     <para xml:lang="zh-CN">外层容器的投影颜色。</para>
    /// </param>
    public sealed record ListShellTokens(Color Bg, Color Border, Color Shadow);

    /// <summary>
    ///     <para xml:lang="en">Groups default and accent list-item card colors.</para>
    ///     <para xml:lang="zh-CN">集中定义列表项卡片的默认及强调颜色。</para>
    /// </summary>
    /// <param name="Default">
    ///     <para xml:lang="en">The resting background and border.</para>
    ///     <para xml:lang="zh-CN">静止状态的背景及边框。</para>
    /// </param>
    /// <param name="Accent">
    ///     <para xml:lang="en">The accent or selected-state background and border.</para>
    ///     <para xml:lang="zh-CN">强调或选中状态的背景及边框。</para>
    /// </param>
    /// <param name="Shadow">
    ///     <para xml:lang="en">The card drop-shadow color.</para>
    ///     <para xml:lang="zh-CN">卡片投影颜色。</para>
    /// </param>
    public sealed record ListItemTokens(BgBorder Default, BgBorder Accent, Color Shadow);

    /// <summary>
    ///     <para xml:lang="en">Groups inline list-editor panel colors.</para>
    ///     <para xml:lang="zh-CN">集中定义内联列表编辑器面板的颜色。</para>
    /// </summary>
    /// <param name="Bg">
    ///     <para xml:lang="en">The editor background color.</para>
    ///     <para xml:lang="zh-CN">编辑器背景颜色。</para>
    /// </param>
    /// <param name="Border">
    ///     <para xml:lang="en">The editor border color.</para>
    ///     <para xml:lang="zh-CN">编辑器边框颜色。</para>
    /// </param>
    public sealed record ListEditorTokens(Color Bg, Color Border);

    /// <summary>
    ///     <para xml:lang="en">Groups colors used by pill-shaped tags and compact buttons.</para>
    ///     <para xml:lang="zh-CN">集中定义胶囊形标签及紧凑按钮使用的颜色。</para>
    /// </summary>
    /// <param name="Default">
    ///     <para xml:lang="en">The resting background and border.</para>
    ///     <para xml:lang="zh-CN">静止状态的背景及边框。</para>
    /// </param>
    /// <param name="Hover">
    ///     <para xml:lang="en">The hover-state background and border.</para>
    ///     <para xml:lang="zh-CN">悬停状态的背景及边框。</para>
    /// </param>
    public sealed record PillTokens(BgBorder Default, BgBorder Hover);

    /// <summary>
    ///     <para xml:lang="en">Groups toggle-state colors and the shared resting shadow.</para>
    ///     <para xml:lang="zh-CN">集中定义开关各状态的颜色及共享的静止阴影。</para>
    /// </summary>
    /// <param name="On">
    ///     <para xml:lang="en">The enabled-state background and border.</para>
    ///     <para xml:lang="zh-CN">开启状态的背景及边框。</para>
    /// </param>
    /// <param name="Off">
    ///     <para xml:lang="en">The disabled-value background and border.</para>
    ///     <para xml:lang="zh-CN">关闭值状态的背景及边框。</para>
    /// </param>
    /// <param name="OffHover">
    ///     <para xml:lang="en">
    ///         The hovered off-state colors. Its border falls back to <see cref="Off" /> when omitted.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         关闭值悬停状态的颜色；省略边框时回退到 <see cref="Off" />。
    ///     </para>
    /// </param>
    /// <param name="Disabled">
    ///     <para xml:lang="en">The non-interactive background and border.</para>
    ///     <para xml:lang="zh-CN">不可交互状态的背景及边框。</para>
    /// </param>
    /// <param name="Shadow">
    ///     <para xml:lang="en">The resting, non-hover shadow color.</para>
    ///     <para xml:lang="zh-CN">静止且未悬停状态的阴影颜色。</para>
    /// </param>
    public sealed record ToggleTokens(
        BgBorder On,
        BgBorder Off,
        BgBorder OffHover,
        BgBorder Disabled,
        Color Shadow);

    /// <summary>
    ///     <para xml:lang="en">Groups the highlight and shadow colors of a slider thumb.</para>
    ///     <para xml:lang="zh-CN">集中定义滑块柄的高光及阴影颜色。</para>
    /// </summary>
    /// <param name="GrabHighlight">
    ///     <para xml:lang="en">The outer highlight color.</para>
    ///     <para xml:lang="zh-CN">外层高光颜色。</para>
    /// </param>
    /// <param name="GrabShadow">
    ///     <para xml:lang="en">The inner shadow color.</para>
    ///     <para xml:lang="zh-CN">内层阴影颜色。</para>
    /// </param>
    public sealed record SliderTokens(Color GrabHighlight, Color GrabShadow);

    /// <summary>
    ///     <para xml:lang="en">Groups the base, hover, pressed, and focus colors of drop-down faces.</para>
    ///     <para xml:lang="zh-CN">集中定义下拉框正面的基础、悬停、按下及聚焦颜色。</para>
    /// </summary>
    /// <param name="Open">
    ///     <para xml:lang="en">The base face background and border.</para>
    ///     <para xml:lang="zh-CN">默认正面状态的背景及边框。</para>
    /// </param>
    /// <param name="Hover">
    ///     <para xml:lang="en">
    ///         The hover-state colors. Its border falls back to <see cref="Open" /> when omitted.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         悬停状态的颜色；省略边框时回退到 <see cref="Open" />。
    ///     </para>
    /// </param>
    /// <param name="Pressed">
    ///     <para xml:lang="en">
    ///         The pressed-state colors. Its border falls back to <see cref="Open" /> when omitted.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         按下状态的颜色；省略边框时回退到 <see cref="Open" />。
    ///     </para>
    /// </param>
    /// <param name="Focus">
    ///     <para xml:lang="en">The focus-state background and border.</para>
    ///     <para xml:lang="zh-CN">聚焦状态的背景及边框。</para>
    /// </param>
    public sealed record DropdownTokens(
        BgBorder Open,
        BgBorder Hover,
        BgBorder Pressed,
        BgBorder Focus);

    /// <summary>
    ///     <para xml:lang="en">Groups stepper-face colors by interaction state.</para>
    ///     <para xml:lang="zh-CN">按交互状态分类定义步进器正面的颜色。</para>
    /// </summary>
    /// <param name="Default">
    ///     <para xml:lang="en">The default background and border.</para>
    ///     <para xml:lang="zh-CN">默认状态的背景及边框。</para>
    /// </param>
    /// <param name="Hover">
    ///     <para xml:lang="en">The hover-state background and border.</para>
    ///     <para xml:lang="zh-CN">悬停状态的背景及边框。</para>
    /// </param>
    /// <param name="Neutral">
    ///     <para xml:lang="en">The neutral colors used when the face has no visible affordance.</para>
    ///     <para xml:lang="zh-CN">正面没有可见操作提示时使用的中性颜色。</para>
    /// </param>
    public sealed record StepperTokens(BgBorder Default, BgBorder Hover, BgBorder Neutral);

    /// <summary>
    ///     <para xml:lang="en">Groups drag-handle colors by selection state.</para>
    ///     <para xml:lang="zh-CN">按选中状态分类定义拖动手柄的颜色。</para>
    /// </summary>
    /// <param name="Default">
    ///     <para xml:lang="en">The resting background and border.</para>
    ///     <para xml:lang="zh-CN">静止状态的背景及边框。</para>
    /// </param>
    /// <param name="Selected">
    ///     <para xml:lang="en">The selected background and border.</para>
    ///     <para xml:lang="zh-CN">选中状态的背景及边框。</para>
    /// </param>
    public sealed record DragHandleTokens(BgBorder Default, BgBorder Selected);

    /// <summary>
    ///     <para xml:lang="en">Groups collapsible section-header colors by interaction state.</para>
    ///     <para xml:lang="zh-CN">按交互状态分类定义可折叠分区标题栏的颜色。</para>
    /// </summary>
    /// <param name="Default">
    ///     <para xml:lang="en">The resting background and border.</para>
    ///     <para xml:lang="zh-CN">静止状态的背景及边框。</para>
    /// </param>
    /// <param name="Hover">
    ///     <para xml:lang="en">The hover-state background and border.</para>
    ///     <para xml:lang="zh-CN">悬停状态的背景及边框。</para>
    /// </param>
    /// <param name="Selected">
    ///     <para xml:lang="en">The selected or expanded background and border.</para>
    ///     <para xml:lang="zh-CN">选中或展开状态的背景及边框。</para>
    /// </param>
    /// <param name="Disabled">
    ///     <para xml:lang="en">
    ///         The state used when the section content is unavailable, even if the header remains interactive.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         分区内容不可用时使用的状态，即使标题栏本身仍可交互。
    ///     </para>
    /// </param>
    public sealed record CollapsibleTokens(BgBorder Default, BgBorder Hover, BgBorder Selected, BgBorder Disabled);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Groups the legacy color slots used to compose mod-group, page, section, and utility rows in the
    ///         current sidebar.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         集中定义当前侧边栏用于组合模组组、页面、分区及工具条目的旧版颜色槽位。
    ///     </para>
    /// </summary>
    /// <param name="Default">
    ///     <para xml:lang="en">The hovered section-row background and resting section-row border.</para>
    ///     <para xml:lang="zh-CN">悬停分区行的背景及静止分区行的边框。</para>
    /// </param>
    /// <param name="Hover">
    ///     <para xml:lang="en">The selected section-row background and border.</para>
    ///     <para xml:lang="zh-CN">选中分区行的背景及边框。</para>
    /// </param>
    /// <param name="Selected">
    ///     <para xml:lang="en">The hovered mod-group background and resting mod-group border.</para>
    ///     <para xml:lang="zh-CN">悬停模组组行的背景及静止模组组行的边框。</para>
    /// </param>
    /// <param name="SelectedHover">
    ///     <para xml:lang="en">The selected mod-group background and border.</para>
    ///     <para xml:lang="zh-CN">选中模组组行的背景及边框。</para>
    /// </param>
    /// <param name="UtilitySelected">
    ///     <para xml:lang="en">The selected utility-row background.</para>
    ///     <para xml:lang="zh-CN">选中工具行的背景。</para>
    /// </param>
    /// <param name="IdleDeep">
    ///     <para xml:lang="en">The resting background of section and utility rows.</para>
    ///     <para xml:lang="zh-CN">静止分区行及工具行的背景。</para>
    /// </param>
    /// <param name="IdleDeepHover">
    ///     <para xml:lang="en">The hovered utility-row background.</para>
    ///     <para xml:lang="zh-CN">悬停工具行的背景。</para>
    /// </param>
    /// <param name="IdleDeeper">
    ///     <para xml:lang="en">A reserved resting background for deeper sidebar rows.</para>
    ///     <para xml:lang="zh-CN">为更深层级侧边栏行保留的静止背景。</para>
    /// </param>
    /// <param name="IdleDeeperHover">
    ///     <para xml:lang="en">A reserved hover background for deeper sidebar rows.</para>
    ///     <para xml:lang="zh-CN">为更深层级侧边栏行保留的悬停背景。</para>
    /// </param>
    /// <param name="Mod">
    ///     <para xml:lang="en">The resting mod-group and hovered page-row background.</para>
    ///     <para xml:lang="zh-CN">静止模组组行及悬停页面行的背景。</para>
    /// </param>
    /// <param name="ModHover">
    ///     <para xml:lang="en">The selected page-row background.</para>
    ///     <para xml:lang="zh-CN">选中页面行的背景。</para>
    /// </param>
    /// <param name="ModDeep">
    ///     <para xml:lang="en">The resting page-row background.</para>
    ///     <para xml:lang="zh-CN">静止页面行的背景。</para>
    /// </param>
    /// <param name="DeepBorder">
    ///     <para xml:lang="en">The resting border color of page and utility rows.</para>
    ///     <para xml:lang="zh-CN">静止页面行及工具行的边框颜色。</para>
    /// </param>
    /// <param name="DeepBorderHover">
    ///     <para xml:lang="en">The selected border color of page and utility rows.</para>
    ///     <para xml:lang="zh-CN">选中页面行及工具行的边框颜色。</para>
    /// </param>
    /// <param name="Shadow">
    ///     <para xml:lang="en">The drop-shadow color shared by sidebar rows.</para>
    ///     <para xml:lang="zh-CN">侧边栏行共享的投影颜色。</para>
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
    ///     <para xml:lang="en">Groups the background and border colors of the sidebar section rail.</para>
    ///     <para xml:lang="zh-CN">集中定义侧边栏分区轨道的背景及边框颜色。</para>
    /// </summary>
    /// <param name="Bg">
    ///     <para xml:lang="en">The rail background color.</para>
    ///     <para xml:lang="zh-CN">轨道背景颜色。</para>
    /// </param>
    /// <param name="Border">
    ///     <para xml:lang="en">The rail border color.</para>
    ///     <para xml:lang="zh-CN">轨道边框颜色。</para>
    /// </param>
    public sealed record SidebarRailTokens(Color Bg, Color Border);

    /// <summary>
    ///     <para xml:lang="en">Groups inline text-button colors by semantic tone.</para>
    ///     <para xml:lang="zh-CN">按语义色调分类定义内联文本按钮的颜色。</para>
    /// </summary>
    /// <param name="Accent">
    ///     <para xml:lang="en">The accent tone.</para>
    ///     <para xml:lang="zh-CN">强调色调。</para>
    /// </param>
    /// <param name="Danger">
    ///     <para xml:lang="en">The danger tone.</para>
    ///     <para xml:lang="zh-CN">危险色调。</para>
    /// </param>
    /// <param name="Neutral">
    ///     <para xml:lang="en">The neutral tone.</para>
    ///     <para xml:lang="zh-CN">中性色调。</para>
    /// </param>
    public sealed record TextButtonTokens(
        TextButtonToneTokens Accent,
        TextButtonToneTokens Danger,
        TextButtonToneTokens Neutral);

    /// <summary>
    ///     <para xml:lang="en">Groups the foreground and state backgrounds of one inline text-button tone.</para>
    ///     <para xml:lang="zh-CN">集中定义一种内联文本按钮色调的前景及状态背景颜色。</para>
    /// </summary>
    /// <param name="Fg">
    ///     <para xml:lang="en">The label foreground color.</para>
    ///     <para xml:lang="zh-CN">标签前景色。</para>
    /// </param>
    /// <param name="Bg">
    ///     <para xml:lang="en">The tinted background used for the base emphasized state.</para>
    ///     <para xml:lang="zh-CN">基础强调状态使用的着色背景。</para>
    /// </param>
    /// <param name="BgHover">
    ///     <para xml:lang="en">The hover variant of the tinted background.</para>
    ///     <para xml:lang="zh-CN">着色背景的悬停变体。</para>
    /// </param>
    public sealed record TextButtonToneTokens(Color Fg, Color Bg, Color BgHover);

    /// <summary>
    ///     <para xml:lang="en">Groups neutral and invalid validation-frame colors for string editors.</para>
    ///     <para xml:lang="zh-CN">集中定义字符串编辑器验证边框的中性及无效状态颜色。</para>
    /// </summary>
    /// <param name="Neutral">
    ///     <para xml:lang="en">The neutral background and border.</para>
    ///     <para xml:lang="zh-CN">中性状态的背景及边框。</para>
    /// </param>
    /// <param name="Invalid">
    ///     <para xml:lang="en">The invalid-state background and border.</para>
    ///     <para xml:lang="zh-CN">无效状态的背景及边框。</para>
    /// </param>
    public sealed record StringValidationTokens(BgBorder Neutral, BgBorder Invalid);

    /// <summary>
    ///     <para xml:lang="en">Groups floating-overlay panel colors.</para>
    ///     <para xml:lang="zh-CN">集中定义浮动层面板的颜色。</para>
    /// </summary>
    /// <param name="Bg">
    ///     <para xml:lang="en">The panel background color.</para>
    ///     <para xml:lang="zh-CN">面板背景颜色。</para>
    /// </param>
    /// <param name="Border">
    ///     <para xml:lang="en">The panel border color.</para>
    ///     <para xml:lang="zh-CN">面板边框颜色。</para>
    /// </param>
    public sealed record OverlayPanelTokens(Color Bg, Color Border);

    /// <summary>
    ///     <para xml:lang="en">Groups the two-color highlight gradient of a choice control's center label.</para>
    ///     <para xml:lang="zh-CN">集中定义选择控件中央标签的双色高亮渐变。</para>
    /// </summary>
    /// <param name="HighlightTop">
    ///     <para xml:lang="en">The color at the top of the gradient.</para>
    ///     <para xml:lang="zh-CN">渐变顶部的颜色。</para>
    /// </param>
    /// <param name="HighlightBottom">
    ///     <para xml:lang="en">The color at the bottom of the gradient.</para>
    ///     <para xml:lang="zh-CN">渐变底部的颜色。</para>
    /// </param>
    public sealed record ChoiceCenterTokens(Color HighlightTop, Color HighlightBottom);

    /// <summary>
    ///     <para xml:lang="en">Stores the background and border colors shared by many component states.</para>
    ///     <para xml:lang="zh-CN">存储多个组件状态共用的背景及边框颜色。</para>
    /// </summary>
    /// <param name="Bg">
    ///     <para xml:lang="en">The background fill color.</para>
    ///     <para xml:lang="zh-CN">背景填充颜色。</para>
    /// </param>
    /// <param name="Border">
    ///     <para xml:lang="en">The border color.</para>
    ///     <para xml:lang="zh-CN">边框颜色。</para>
    /// </param>
    public sealed record BgBorder(Color Bg, Color Border);
}
