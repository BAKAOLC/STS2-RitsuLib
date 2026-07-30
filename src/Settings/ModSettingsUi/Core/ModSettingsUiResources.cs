using Godot;
using MegaCrit.Sts2.Core.Assets;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides shared assets and material factories used by the mod settings screen.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供模组设置界面使用的共享资源和材质工厂。
    ///     </para>
    /// </summary>
    public static class ModSettingsUiResources
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the native line theme used by standard settings rows.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取标准设置行使用的原版行主题。
        ///     </para>
        /// </summary>
        public static Theme SettingsLineTheme =>
            PreloadManager.Cache.GetAsset<Theme>("res://themes/settings_screen_line_header.tres");

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the regular Kreon font used by settings text.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取设置文本使用的常规 Kreon 字体。
        ///     </para>
        /// </summary>
        public static Font KreonRegular =>
            PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_regular_shared.tres");

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the bold Kreon font used by emphasized settings labels and buttons.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取强调设置标签和按钮使用的粗体 Kreon 字体。
        ///     </para>
        /// </summary>
        public static Font KreonBold =>
            PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_bold_shared.tres");

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the Kreon font variant used by standard settings buttons.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取标准设置按钮使用的 Kreon 字体变体。
        ///     </para>
        /// </summary>
        public static Font KreonButton =>
            PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_bold_glyph_space_two.tres");

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the native textured background used by settings action buttons.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取设置操作按钮使用的原版纹理背景。
        ///     </para>
        /// </summary>
        public static Texture2D SettingsButtonTexture =>
            PreloadManager.Cache.GetAsset<Texture2D>("res://images/ui/reward_screen/reward_skip_button.png");

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a shader material tinted for the requested button tone.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建按指定按钮色调着色的着色器材质。
        ///     </para>
        /// </summary>
        /// <param name="tone">
        ///     <para xml:lang="en">
        ///         Semantic tone to apply.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         要应用的语义色调。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         A shader material configured for the requested tone.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按指定色调配置的着色器材质。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Gets the outline color associated with the requested button tone.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取与指定按钮色调关联的轮廓颜色。
        ///     </para>
        /// </summary>
        /// <param name="tone">
        ///     <para xml:lang="en">
        ///         Semantic tone to resolve.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         要解析的语义色调。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The outline color for the requested tone.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         指定色调对应的轮廓颜色。
        ///     </para>
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
