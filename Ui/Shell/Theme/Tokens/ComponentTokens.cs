using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Component tokens grouped by component, then variant/state.
    /// </summary>
    /// <param name="SidebarCard">Sidebar mod cards (default and selected).</param>
    /// <param name="ChromeMenu">Compact action / menu chrome.</param>
    /// <param name="PageToolbarTray">Tray behind per-page toolbar controls.</param>
    /// <param name="ListShell">Outer container for scrollable lists.</param>
    /// <param name="ListItem">List item card variants.</param>
    /// <param name="ListEditor">Inline list-editor surface.</param>
    /// <param name="Pill">Pill-shaped controls (tags, compact buttons).</param>
    /// <param name="Toggle">Settings toggle states.</param>
    /// <param name="Slider">Slider grab thumb tones.</param>
    /// <param name="Dropdown">Dropdown faces and rows.</param>
    /// <param name="Stepper">Stepper face states.</param>
    /// <param name="DragHandle">Drag handles for reorderable lists.</param>
    /// <param name="Collapsible">Collapsible section headers.</param>
    /// <param name="SidebarBtn">Sidebar tree buttons (page / section / mod / utility / depth variations).</param>
    /// <param name="SidebarRail">Section rail background.</param>
    /// <param name="TextButton">Inline text-button tones (accent / danger / neutral).</param>
    /// <param name="StringValidation">String editor validation chrome (neutral / invalid).</param>
    /// <param name="OverlayPanel">Floating overlay panel chrome.</param>
    /// <param name="ChoiceCenter">Choice-center label highlight gradient.</param>
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
    /// </summary>
    /// <param name="Default">Default state (background + border).</param>
    /// <param name="Selected">Selected state.</param>
    /// <param name="Shadow">Drop shadow tint shared by both states.</param>
    public sealed record SidebarCardTokens(BgBorder Default, BgBorder Selected, Color Shadow);

    /// <summary>
    ///     Chrome action menu tokens (hover toggles between two states).
    /// </summary>
    /// <param name="Default">Resting state.</param>
    /// <param name="Hover">Hover state.</param>
    public sealed record ChromeMenuTokens(BgBorder Default, BgBorder Hover);

    /// <summary>
    ///     Page toolbar tray tokens.
    /// </summary>
    /// <param name="Bg">Toolbar tray background.</param>
    /// <param name="Border">Toolbar tray border.</param>
    public sealed record PageToolbarTrayTokens(Color Bg, Color Border);

    /// <summary>
    ///     List shell (outer) tokens.
    /// </summary>
    /// <param name="Bg">Shell background.</param>
    /// <param name="Border">Shell border.</param>
    /// <param name="Shadow">Shell drop shadow.</param>
    public sealed record ListShellTokens(Color Bg, Color Border, Color Shadow);

    /// <summary>
    ///     List item card tokens (default vs accent emphasis).
    /// </summary>
    /// <param name="Default">Resting state.</param>
    /// <param name="Accent">Accent / selected emphasis.</param>
    /// <param name="Shadow">Card drop shadow tint.</param>
    public sealed record ListItemTokens(BgBorder Default, BgBorder Accent, Color Shadow);

    /// <summary>
    ///     Inline list editor surface tokens.
    /// </summary>
    /// <param name="Bg">Editor background.</param>
    /// <param name="Border">Editor border.</param>
    public sealed record ListEditorTokens(Color Bg, Color Border);

    /// <summary>
    ///     Pill / tag tokens.
    /// </summary>
    /// <param name="Default">Resting state.</param>
    /// <param name="Hover">Hover state.</param>
    public sealed record PillTokens(BgBorder Default, BgBorder Hover);

    /// <summary>
    ///     Toggle tokens. Borders inherit if a state omits them.
    /// </summary>
    /// <param name="On">Pressed/on state.</param>
    /// <param name="Off">Off state.</param>
    /// <param name="OffHover">Off + hover state (border falls back to <see cref="Off" />).</param>
    /// <param name="Disabled">Disabled state.</param>
    /// <param name="Shadow">Neutral (non-hover) shadow tint.</param>
    public sealed record ToggleTokens(
        BgBorder On,
        BgBorder Off,
        BgBorder OffHover,
        BgBorder Disabled,
        Color Shadow);

    /// <summary>
    ///     Slider grab tokens.
    /// </summary>
    /// <param name="GrabHighlight">Outer highlight tint.</param>
    /// <param name="GrabShadow">Inner shadow tint.</param>
    public sealed record SliderTokens(Color GrabHighlight, Color GrabShadow);

    /// <summary>
    ///     Dropdown face tokens covering the four interaction states.
    /// </summary>
    /// <param name="Open">Default open state (face + border).</param>
    /// <param name="Hover">Hover state. Border falls back to <see cref="Open" />.</param>
    /// <param name="Pressed">Pressed state. Border falls back to <see cref="Open" />.</param>
    /// <param name="Focus">Focus state (own border).</param>
    public sealed record DropdownTokens(
        BgBorder Open,
        BgBorder Hover,
        BgBorder Pressed,
        BgBorder Focus);

    /// <summary>
    ///     Stepper face tokens.
    /// </summary>
    /// <param name="Default">Default state (background + border).</param>
    /// <param name="Hover">Hover state.</param>
    /// <param name="Neutral">Hidden / neutral state used when the face has no visible affordance.</param>
    public sealed record StepperTokens(BgBorder Default, BgBorder Hover, BgBorder Neutral);

    /// <summary>
    ///     Drag handle tokens.
    /// </summary>
    /// <param name="Default">Resting state.</param>
    /// <param name="Selected">Selected (active) state.</param>
    public sealed record DragHandleTokens(BgBorder Default, BgBorder Selected);

    /// <summary>
    ///     Collapsible section header tokens.
    /// </summary>
    /// <param name="Default">Resting state.</param>
    /// <param name="Hover">Hover state.</param>
    /// <param name="Selected">Selected (expanded) state.</param>
    /// <param name="Disabled">Disabled state (content unavailable, but header may remain interactive).</param>
    public sealed record CollapsibleTokens(BgBorder Default, BgBorder Hover, BgBorder Selected, BgBorder Disabled);

    /// <summary>
    ///     Sidebar tree button tokens. Each variant covers a distinct depth or kind of row.
    /// </summary>
    /// <param name="Default">Default page row.</param>
    /// <param name="Hover">Default page hover.</param>
    /// <param name="Selected">Selected default page row.</param>
    /// <param name="SelectedHover">Selected default page hover.</param>
    /// <param name="UtilitySelected">Selected utility row (only background tinted).</param>
    /// <param name="IdleDeep">Idle page at deeper depth.</param>
    /// <param name="IdleDeepHover">Idle deep + hover.</param>
    /// <param name="IdleDeeper">Idle page at the deepest tracked depth.</param>
    /// <param name="IdleDeeperHover">Idle deeper + hover.</param>
    /// <param name="Mod">Mod row.</param>
    /// <param name="ModHover">Mod row hover.</param>
    /// <param name="ModDeep">Mod row at deeper depth.</param>
    /// <param name="DeepBorder">Border tint shared by deeper rows.</param>
    /// <param name="DeepBorderHover">Hover border tint for deeper rows.</param>
    /// <param name="Shadow">Drop shadow tint.</param>
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
    /// </summary>
    /// <param name="Bg">Rail background.</param>
    /// <param name="Border">Rail outline.</param>
    public sealed record SidebarRailTokens(Color Bg, Color Border);

    /// <summary>
    ///     Inline text button tokens, organised by tone.
    /// </summary>
    /// <param name="Accent">Accent (highlighted) tone.</param>
    /// <param name="Danger">Danger tone.</param>
    /// <param name="Neutral">Neutral tone.</param>
    public sealed record TextButtonTokens(
        TextButtonToneTokens Accent,
        TextButtonToneTokens Danger,
        TextButtonToneTokens Neutral);

    /// <summary>
    ///     Per-tone foreground + background pair for inline text buttons.
    /// </summary>
    /// <param name="Fg">Foreground (label) color.</param>
    /// <param name="Bg">Default tinted background (used for selected/hovered rows).</param>
    /// <param name="BgHover">Hover variation of the tinted background.</param>
    public sealed record TextButtonToneTokens(Color Fg, Color Bg, Color BgHover);

    /// <summary>
    ///     String editor validation chrome (background + border for neutral / invalid states).
    /// </summary>
    /// <param name="Neutral">Neutral state.</param>
    /// <param name="Invalid">Invalid state.</param>
    public sealed record StringValidationTokens(BgBorder Neutral, BgBorder Invalid);

    /// <summary>
    ///     Floating overlay panel chrome.
    /// </summary>
    /// <param name="Bg">Panel background.</param>
    /// <param name="Border">Panel border.</param>
    public sealed record OverlayPanelTokens(Color Bg, Color Border);

    /// <summary>
    ///     Highlight tones used by the choice-center widget.
    /// </summary>
    /// <param name="HighlightTop">Top of the gradient.</param>
    /// <param name="HighlightBottom">Bottom of the gradient.</param>
    public sealed record ChoiceCenterTokens(Color HighlightTop, Color HighlightBottom);

    /// <summary>
    ///     Common (background + border) pair shared by many component states.
    /// </summary>
    /// <param name="Bg">Background fill.</param>
    /// <param name="Border">Border tint.</param>
    public sealed record BgBorder(Color Bg, Color Border);
}
