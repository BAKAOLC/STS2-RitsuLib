using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Theme colors. Layout min sizes, radii, and control heights live in <see cref="ModSettingsUiMetrics" />.
    /// </summary>
    public static class ModSettingsUiPalette
    {
        /// <summary>
        ///     BBCode color used for scope hints and related inline metadata.
        /// </summary>
        public const string ScopeHintBbColor = "#C9BEA6";

        /// <summary>
        ///     Primary rich-text title color.
        /// </summary>
        public static readonly Color RichTextTitle = new(0.98f, 0.965f, 0.93f);

        /// <summary>
        ///     Default rich-text body color.
        /// </summary>
        public static readonly Color RichTextBody = new(0.93f, 0.895f, 0.82f);

        /// <summary>
        ///     Secondary rich-text color for supporting information.
        /// </summary>
        public static readonly Color RichTextSecondary = new(0.84f, 0.805f, 0.735f);

        /// <summary>
        ///     Muted rich-text color for subdued information.
        /// </summary>
        public static readonly Color RichTextMuted = new(0.70f, 0.665f, 0.60f);

        /// <summary>
        ///     Primary label color for interactive controls.
        /// </summary>
        public static readonly Color LabelPrimary = new(0.99f, 0.975f, 0.94f);

        /// <summary>
        ///     Secondary label color for less prominent control text.
        /// </summary>
        public static readonly Color LabelSecondary = new(0.86f, 0.825f, 0.755f, 0.98f);

        /// <summary>
        ///     Accent color used by section labels in the settings sidebar.
        /// </summary>
        public static readonly Color SidebarSection = new(0.88f, 0.855f, 0.795f);
    }
}
