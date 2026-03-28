using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Theme colors. Layout min sizes, radii, and control heights live in <see cref="ModSettingsUiMetrics" />.
    /// </summary>
    internal static class ModSettingsUiPalette
    {
        public const string ScopeHintBbColor = "#C9BEA6";

        public static readonly Color RichTextTitle = new(0.98f, 0.965f, 0.93f);
        public static readonly Color RichTextBody = new(0.93f, 0.895f, 0.82f);
        public static readonly Color RichTextSecondary = new(0.84f, 0.805f, 0.735f);
        public static readonly Color RichTextMuted = new(0.70f, 0.665f, 0.60f);

        public static readonly Color LabelPrimary = new(0.99f, 0.975f, 0.94f);
        public static readonly Color LabelSecondary = new(0.86f, 0.825f, 0.755f, 0.98f);

        public static readonly Color SidebarSection = new(0.88f, 0.855f, 0.795f);
    }
}
