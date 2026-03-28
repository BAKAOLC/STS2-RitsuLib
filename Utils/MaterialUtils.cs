using Godot;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Factory helpers for Godot materials that mirror vanilla game shaders.
    /// </summary>
    public class MaterialUtils
    {
        private static Shader? GameHsvShader => (Shader?)GD.Load<Shader>("res://shaders/hsv.gdshader")?.Duplicate();

        /// <summary>
        ///     Builds a <c>ShaderMaterial</c> using the game's HSV shader with the given parameters.
        /// </summary>
        public static ShaderMaterial CreateHsvShaderMaterial(float h, float s, float v)
        {
            var shader = GameHsvShader;
            if (shader == null)
                throw new("Failed to load HSV shader.");

            var material = new ShaderMaterial
            {
                Shader = shader,
            };

            material.SetShaderParameter("h", h);
            material.SetShaderParameter("s", s);
            material.SetShaderParameter("v", v);

            return material;
        }
    }
}
