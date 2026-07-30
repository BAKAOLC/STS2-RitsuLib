using Godot;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     <para xml:lang="en">Factory helpers for Godot materials based on game shaders and visual conventions.</para>
    ///     <para xml:lang="zh-CN">基于游戏着色器和视觉规范创建 Godot 材质的工厂辅助方法。</para>
    /// </summary>
    public static partial class MaterialUtils
    {
        private const string HsvShaderPath = "res://shaders/hsv.gdshader";
        private const string DoomBarShaderPath = "res://scenes/combat/doom_bar.gdshader";

        private static NoiseTexture2D? _vanillaDoomBarNoiseTexture;
        private static ShaderMaterial? _unmodulatedHsvMaterial;

        private static Shader? _replaceHueShader;

        private static Shader? GameHsvShader => (Shader?)GD.Load<Shader>(HsvShaderPath)?.Duplicate();

        private static Shader? GameDoomBarShader => (Shader?)GD.Load<Shader>(DoomBarShaderPath)?.Duplicate();

        private static Shader ReplaceHueShader => _replaceHueShader ??= new()
        {
            Code = ReplaceHueShaderSource,
        };

        private static NoiseTexture2D VanillaDoomBarNoiseTexture =>
            _vanillaDoomBarNoiseTexture ??= CreateVanillaDoomBarNoiseTexture();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a <c>ShaderMaterial</c> that recolors the input texture toward a supplied RGB color while
        ///         retaining its shading and using its original saturation as the blend strength. RGB components use
        ///         the range 0–1; brightness uses 0–2 and defaults to 1.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         构建一个 <c>ShaderMaterial</c>，使输入纹理趋向指定 RGB 颜色，同时保留其明暗层次，
        ///         并以原始饱和度作为混合强度。RGB 分量范围为 0–1；亮度范围为 0–2，默认值为 1。
        ///     </para>
        /// </summary>
        public static ShaderMaterial CreateReplaceHueShaderMaterial(float r, float g, float b, float brightness = 1f)
        {
            var material = new ShaderMaterial { Shader = ReplaceHueShader };
            material.SetShaderParameter("target_color", new Vector3(r, g, b));
            material.SetShaderParameter("brightness", brightness);
            return material;
        }

        /// <summary>
        ///     <para xml:lang="en">Builds a <c>ShaderMaterial</c> using the game's HSV shader with the given RGB parameters.</para>
        ///     <para xml:lang="zh-CN">使用游戏的 HSV 着色器和给定 RGB 参数构建 <c>ShaderMaterial</c>。</para>
        /// </summary>
        [Obsolete("Prefer MaterialUtils.CreateReplaceHueShaderMaterial instead.")]
        public static ShaderMaterial CreateRgbShaderMaterial(float r, float g, float b)
        {
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var delta = max - min;

            float h = 0;
            if (delta != 0)
            {
                if (Mathf.IsEqualApprox(max, r)) h = (g - b) / delta + (g < b ? 6 : 0);
                else if (Mathf.IsEqualApprox(max, g)) h = (b - r) / delta + 2;
                else h = (r - g) / delta + 4;
                h /= 6;
            }

            var s = max == 0 ? 0 : delta / max;
            return CreateHsvShaderMaterial(h, s, max);
        }

        /// <summary>
        ///     <para xml:lang="en">Builds a <c>ShaderMaterial</c> using the game's HSV shader with the given parameters.</para>
        ///     <para xml:lang="zh-CN">使用游戏的 HSV 着色器和给定参数构建 <c>ShaderMaterial</c>。</para>
        /// </summary>
        public static ShaderMaterial CreateHsvShaderMaterial(float h, float s, float v)
        {
            var shader = GameHsvShader ??
                         throw new InvalidOperationException($"Failed to load HSV shader ({HsvShaderPath}).");

            var material = new ShaderMaterial
            {
                Shader = shader,
            };

            material.SetShaderParameter("h", h);
            material.SetShaderParameter("s", s);
            material.SetShaderParameter("v", v);

            return material;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a <see cref="ShaderMaterial" /> built from the game's HSV shader configured to preserve the original colors (identity modulation: <c>h=0</c>, <c>s=1</c>, <c>v=1</c>).</para>
        ///     <para xml:lang="zh-CN">返回由游戏 HSV 着色器构建的 <see cref="ShaderMaterial" />，配置为保留原始颜色（恒等调制：<c>h=0</c>、<c>s=1</c>、<c>v=1</c>）。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">Use this to override a card frame's <c>FrameMaterial</c> without additional color modulation while retaining the vanilla shader pipeline.</para>
        ///     <para xml:lang="zh-CN">可在保留原版着色器管线的同时覆盖卡牌框的 <c>FrameMaterial</c>，而不引入额外颜色调制。</para>
        /// </remarks>
        public static ShaderMaterial CreateUnmodulatedHsvShaderMaterial()
        {
            _unmodulatedHsvMaterial ??= CreateHsvShaderMaterial(0f, 1f, 1f);
            return (ShaderMaterial)_unmodulatedHsvMaterial.Duplicate();
        }

        /// <summary>
        ///     <para xml:lang="en">Builds a <c>ShaderMaterial</c> using the game's Doom health bar shader (<c>doom_bar.gdshader</c>) with the same noise settings as <c>health_bar.tscn</c> and a caller-supplied gradient.</para>
        ///     <para xml:lang="zh-CN">使用游戏的灾厄生命条着色器（<c>doom_bar.gdshader</c>）构建 <c>ShaderMaterial</c>，并采用与 <c>health_bar.tscn</c> 相同的噪声设置以及调用方提供的渐变。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">Use it as <see cref="Combat.HealthBars.HealthBarForecastSegment.OverlayMaterial" /> on a custom forecast overlay to resemble the vanilla Doom strip; see also <c>CreateVanillaDoomBarGradientTexture</c>.</para>
        ///     <para xml:lang="zh-CN">可将其用作自定义预测叠加层的 <see cref="Combat.HealthBars.HealthBarForecastSegment.OverlayMaterial" />，以呈现原版灾厄条效果；另见 <c>CreateVanillaDoomBarGradientTexture</c>。</para>
        /// </remarks>
        public static ShaderMaterial CreateDoomBarShaderMaterial(GradientTexture1D gradientTexture)
        {
            ArgumentNullException.ThrowIfNull(gradientTexture);

            var shader = GameDoomBarShader;
            if (shader == null)
                throw new InvalidOperationException($"Failed to load doom bar shader ({DoomBarShaderPath}).");

            var material = new ShaderMaterial { Shader = shader };
            material.SetShaderParameter("noise_tex", VanillaDoomBarNoiseTexture);
            material.SetShaderParameter("gradient_tex", gradientTexture);
            return material;
        }

        /// <summary>
        ///     <para xml:lang="en">Gradient texture matching the vanilla Doom bar segment in <c>health_bar.tscn</c>.</para>
        ///     <para xml:lang="zh-CN">与 <c>health_bar.tscn</c> 中原版灾厄生命条片段匹配的渐变纹理。</para>
        /// </summary>
        public static GradientTexture1D CreateVanillaDoomBarGradientTexture()
        {
            var gradient = new Gradient();
            gradient.AddPoint(0f, new(0.300863f, 0.162626f, 0.528347f));
            gradient.AddPoint(0.514583f, new(0.513726f, 0.254902f, 0.505882f));
            gradient.AddPoint(1f, new(0.354657f, 0.0421873f, 0.437114f));
            return new() { Gradient = gradient };
        }

        /// <summary>
        ///     <para xml:lang="en">Noise texture matching <c>health_bar.tscn</c> (Perlin, frequency 0.0383).</para>
        ///     <para xml:lang="zh-CN">与 <c>health_bar.tscn</c> 匹配的噪声纹理（Perlin，频率 0.0383）。</para>
        /// </summary>
        public static NoiseTexture2D CreateVanillaDoomBarNoiseTexture()
        {
            var noise = new FastNoiseLite
            {
                NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
                Frequency = 0.0383f,
            };

            return new() { Noise = noise };
        }
    }
}
