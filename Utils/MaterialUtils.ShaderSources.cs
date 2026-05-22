namespace STS2RitsuLib.Utils
{
    public static partial class MaterialUtils
    {
        private const string ReplaceHueShaderSource = """
            shader_type canvas_item;

            const vec3 LUMA_WEIGHTS = vec3(0.2126, 0.7152, 0.0722);
            const float EPSILON = 1e-7;
            const float MAX_COLOR_GAIN = 1.12;

            uniform vec3 target_color : source_color = vec3(1.0);
            uniform float brightness : hint_range(0.0, 2.0) = 1.0;

            varying vec4 modulate_color;

            void vertex() {
                modulate_color = COLOR;
            }

            void fragment() {
                vec4 col = texture(TEXTURE, UV);

                float max_rgb = max(max(col.r, col.g), col.b);
                float min_rgb = min(min(col.r, col.g), col.b);
                float value = max_rgb * brightness;
                float saturation = (max_rgb - min_rgb) / (max_rgb + EPSILON);

                float target_value = max(max(target_color.r, target_color.g), target_color.b);
                vec3 target_hue = target_color / max(target_value, EPSILON);
                float color_gain = min(1.0 / max(dot(target_hue, LUMA_WEIGHTS), EPSILON), MAX_COLOR_GAIN);

                vec3 final = mix(vec3(value), target_color * value * color_gain, saturation);
                COLOR = vec4(final, col.a) * modulate_color;
            }
            """;
    }
}
