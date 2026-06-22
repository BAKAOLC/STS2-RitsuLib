using Godot;
using MegaCrit.Sts2.Core.Assets;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Shared UI assets and helper factories used by the mod settings experience.
    ///     mod 设置体验使用的共享 UI 资源和辅助工厂。
    /// </summary>
    public static class ModSettingsUiResources
    {
        /// <summary>
        ///     Gets the line theme used by standard settings rows.
        ///     获取标准设置行使用的行主题。
        /// </summary>
        public static Theme SettingsLineTheme =>
            PreloadManager.Cache.GetAsset<Theme>("res://themes/settings_screen_line_header.tres");

        /// <summary>
        ///     Gets the regular Kreon font used by settings text.
        ///     获取设置文本使用的常规 Kreon 字体。
        /// </summary>
        public static Font KreonRegular =>
            PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_regular_shared.tres");

        /// <summary>
        ///     Gets the bold Kreon font used by emphasized settings labels and buttons.
        ///     获取强调设置标签和按钮使用的粗体 Kreon 字体。
        /// </summary>
        public static Font KreonBold =>
            PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_bold_shared.tres");

        /// <summary>
        ///     Gets the button-focused Kreon font variant used by standard settings buttons.
        ///     获取标准设置按钮使用的按钮焦点 Kreon 字体变体。
        /// </summary>
        public static Font KreonButton =>
            PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_bold_glyph_space_two.tres");

        /// <summary>
        ///     Gets the standard textured button background used by settings action buttons.
        ///     获取设置动作按钮使用的标准纹理按钮背景。
        /// </summary>
        public static Texture2D SettingsButtonTexture =>
            PreloadManager.Cache.GetAsset<Texture2D>("res://images/ui/reward_screen/reward_skip_button.png");

        /// <summary>
        ///     Creates a shader material tinted for the requested button tone.
        ///     创建按请求的按钮色调着色的着色器材质。
        /// </summary>
        /// <param name="tone">
        ///     The semantic tone to apply.
        ///     指定 semantic tone to apply。
        /// </param>
        /// <returns>
        ///     A shader material configured for the requested tone.
        ///     为请求的色调配置的着色器材质。
        /// </returns>
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
        ///     获取与请求的按钮色调关联的轮廓颜色。
        /// </summary>
        /// <param name="tone">
        ///     The semantic tone to resolve.
        ///     指定 semantic tone to resolve。
        /// </param>
        /// <returns>
        ///     The outline color for the requested tone.
        ///     请求色调对应的轮廓颜色。
        /// </returns>
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
