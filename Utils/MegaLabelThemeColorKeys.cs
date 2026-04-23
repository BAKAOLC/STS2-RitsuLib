using System.Reflection;
using Godot;
using MegaCrit.Sts2.addons.mega_text;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Resolves <see cref="StringName" /> theme keys for MegaLabel font color overrides across game versions
    ///     (0.99.1 <c>fontColor</c> / <c>fontOutlineColor</c> vs 0.102+ <c>FontColor</c> / <c>FontOutlineColor</c>).
    /// </summary>
    internal static class MegaLabelThemeColorKeys
    {
        private static readonly Lazy<StringName> FontColor = new(ResolveFontColor);
        private static readonly Lazy<StringName> FontOutlineColor = new(ResolveFontOutlineColor);

        public static StringName FontColorKey => FontColor.Value;

        public static StringName FontOutlineColorKey => FontOutlineColor.Value;

        private static StringName ResolveFontColor()
        {
            return ResolveLabelKey("FontColor", "fontColor") ?? new StringName("font_color");
        }

        private static StringName ResolveFontOutlineColor()
        {
            return ResolveLabelKey("FontOutlineColor", "fontOutlineColor") ?? new StringName("font_outline_color");
        }

        private static StringName? ResolveLabelKey(params string[] names)
        {
            var labelType = typeof(ThemeConstants.Label);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            foreach (var name in names)
            {
                var field = labelType.GetField(name, flags);
                if (field?.GetValue(null) is StringName fromField)
                    return fromField;

                var prop = labelType.GetProperty(name, flags);
                if (prop?.GetValue(null) is StringName fromProp)
                    return fromProp;
            }

            return null;
        }
    }
}
