using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Top-level palette of primitive colors that are used directly without component-level scoping.
    /// </summary>
    /// <param name="White">Plain white tint used by overlays and active accents.</param>
    /// <param name="Transparent">Fully transparent color (<c>#00000000</c>).</param>
    /// <param name="Divider">Hairline divider color used between sections.</param>
    /// <param name="UnsetPreview">Highlight tint used to indicate values not yet committed.</param>
    /// <param name="ModalBackdrop">Dim color used as the backdrop behind modal panels.</param>
    /// <param name="Shadow">Shared shadow tones.</param>
    public sealed record ColorTokens(
        Color White,
        Color Transparent,
        Color Divider,
        Color UnsetPreview,
        Color ModalBackdrop,
        ShadowTokens Shadow);

    /// <summary>
    ///     Generic shadow colors not bound to a specific component.
    /// </summary>
    /// <param name="Ambient">Soft ambient drop shadow used by elevated controls.</param>
    public sealed record ShadowTokens(Color Ambient);

    /// <summary>
    ///     Colors used by typography (rich text, labels, hints).
    /// </summary>
    /// <param name="RichTitle">Rich text title tone.</param>
    /// <param name="RichBody">Rich text body tone.</param>
    /// <param name="RichSecondary">Secondary rich text (descriptions, sub-titles).</param>
    /// <param name="RichMuted">Muted rich text (low-importance hints).</param>
    /// <param name="LabelPrimary">Primary label color.</param>
    /// <param name="LabelSecondary">Secondary label color (used by subtitles).</param>
    /// <param name="SidebarSection">Section header label tone in the sidebar.</param>
    /// <param name="HoverHighlight">Foreground color used by buttons in hover/pressed/focus.</param>
    /// <param name="Number">Numeric value label tone.</param>
    /// <param name="Grip">Drag-handle grip glyph tone.</param>
    /// <param name="Hint">Inline hint tone.</param>
    /// <param name="DropdownRow">Dropdown popup row foreground.</param>
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
    /// </summary>
    /// <param name="Sidebar">Settings sidebar pane background.</param>
    /// <param name="Content">Settings content pane background.</param>
    /// <param name="ContentBuildOverlay">Translucent overlay during content rebuilds.</param>
    /// <param name="Entry">Standard entry surface (background + border + shadow).</param>
    /// <param name="Inset">Recessed surface for nested content.</param>
    /// <param name="Framed">Large pane chrome (border + shadow).</param>
    public sealed record SurfaceTokens(
        Color Sidebar,
        Color Content,
        Color ContentBuildOverlay,
        EntrySurfaceTokens Entry,
        InsetSurfaceTokens Inset,
        FramedSurfaceTokens Framed);

    /// <summary>
    ///     Chrome surface used by entry containers (toggle backgrounds, dropdown faces, ...).
    /// </summary>
    /// <param name="Bg">Background fill.</param>
    /// <param name="Border">Border tint.</param>
    /// <param name="Shadow">Drop shadow tint.</param>
    public sealed record EntrySurfaceTokens(Color Bg, Color Border, Color Shadow);

    /// <summary>
    ///     Recessed (inset) surface chrome for nested content.
    /// </summary>
    /// <param name="Bg">Inset background fill.</param>
    /// <param name="Border">Inset border tint.</param>
    public sealed record InsetSurfaceTokens(Color Bg, Color Border);

    /// <summary>
    ///     Framed pane chrome shared by the large settings panes.
    /// </summary>
    /// <param name="Border">Frame border tint.</param>
    /// <param name="Shadow">Frame shadow tint.</param>
    public sealed record FramedSurfaceTokens(Color Border, Color Shadow);
}
