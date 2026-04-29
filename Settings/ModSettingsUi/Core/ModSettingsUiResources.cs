using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Shared UI assets and helper factories used by the mod settings experience.
    /// </summary>
    public static class ModSettingsUiResources
    {
        /// <summary>
        ///     Gets the line theme used by standard settings rows.
        /// </summary>
        public static Theme SettingsLineTheme =>
            PreloadManager.Cache.GetAsset<Theme>("res://themes/settings_screen_line_header.tres");

        /// <summary>
        ///     Gets the regular Kreon font used by settings text.
        /// </summary>
        public static Font KreonRegular =>
            PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_regular_shared.tres");

        /// <summary>
        ///     Gets the bold Kreon font used by emphasized settings labels and buttons.
        /// </summary>
        public static Font KreonBold =>
            PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_bold_shared.tres");

        /// <summary>
        ///     Gets the button-focused Kreon font variant used by standard settings buttons.
        /// </summary>
        public static Font KreonButton =>
            PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_bold_glyph_space_two.tres");

        /// <summary>
        ///     Gets the shared selection reticle scene used by interactive settings controls.
        /// </summary>
        public static PackedScene SelectionReticleScene =>
            PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/selection_reticle"));

        /// <summary>
        ///     Gets the standard textured button background used by settings action buttons.
        /// </summary>
        public static Texture2D SettingsButtonTexture =>
            PreloadManager.Cache.GetAsset<Texture2D>("res://images/ui/reward_screen/reward_skip_button.png");

        /// <summary>
        ///     Creates a shader material tinted for the requested button tone.
        /// </summary>
        /// <param name="tone">The semantic tone to apply.</param>
        /// <returns>A shader material configured for the requested tone.</returns>
        public static ShaderMaterial CreateToneMaterial(ModSettingsButtonTone tone)
        {
            return tone switch
            {
                ModSettingsButtonTone.Accent => MaterialUtils.CreateHsvShaderMaterial(0.82f, 1.4f, 0.8f),
                ModSettingsButtonTone.Danger => MaterialUtils.CreateHsvShaderMaterial(0.45f, 1.5f, 0.8f),
                _ => MaterialUtils.CreateHsvShaderMaterial(0.61f, 1.6f, 1.3f),
            };
        }

        /// <summary>
        ///     Gets the outline color associated with the requested button tone.
        /// </summary>
        /// <param name="tone">The semantic tone to resolve.</param>
        /// <returns>The outline color for the requested tone.</returns>
        public static Color GetToneOutlineColor(ModSettingsButtonTone tone)
        {
            return tone switch
            {
                ModSettingsButtonTone.Accent => new(0.1274f, 0.26f, 0.14066f),
                ModSettingsButtonTone.Danger => new(0.29f, 0.14703f, 0.1421f),
                _ => new(0.2f, 0.1575f, 0.098f),
            };
        }
    }
}
