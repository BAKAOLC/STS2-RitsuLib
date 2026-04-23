using System.Reflection;
using Godot;

namespace STS2RitsuLib.Scaffolding.Godot
{
    internal static class RitsuThemeLookupCompat
    {
        private static readonly StringName LabelType = "Label";

        private static readonly MethodInfo? GetThemeFontOneArg = ResolveMethod("GetThemeFont", typeof(StringName));

        private static readonly MethodInfo? GetThemeFontTwoArg =
            ResolveMethod("GetThemeFont", typeof(StringName), typeof(StringName));

        private static readonly MethodInfo? GetThemeFontOneArgString = ResolveMethod("GetThemeFont", typeof(string));

        private static readonly MethodInfo? GetThemeFontTwoArgString =
            ResolveMethod("GetThemeFont", typeof(string), typeof(string));

        private static readonly MethodInfo? GetThemeColorOneArg = ResolveMethod("GetThemeColor", typeof(StringName));

        private static readonly MethodInfo? GetThemeColorTwoArg =
            ResolveMethod("GetThemeColor", typeof(StringName), typeof(StringName));

        private static readonly MethodInfo? GetThemeColorOneArgString = ResolveMethod("GetThemeColor", typeof(string));

        private static readonly MethodInfo? GetThemeColorTwoArgString =
            ResolveMethod("GetThemeColor", typeof(string), typeof(string));

        private static readonly MethodInfo? GetThemeConstantOneArg =
            ResolveMethod("GetThemeConstant", typeof(StringName));

        private static readonly MethodInfo? GetThemeConstantTwoArg =
            ResolveMethod("GetThemeConstant", typeof(StringName), typeof(StringName));

        private static readonly MethodInfo? GetThemeConstantOneArgString =
            ResolveMethod("GetThemeConstant", typeof(string));

        private static readonly MethodInfo? GetThemeConstantTwoArgString =
            ResolveMethod("GetThemeConstant", typeof(string), typeof(string));

        private static readonly MethodInfo? GetThemeFontSizeOneArg =
            ResolveMethod("GetThemeFontSize", typeof(StringName));

        private static readonly MethodInfo? GetThemeFontSizeTwoArg =
            ResolveMethod("GetThemeFontSize", typeof(StringName), typeof(StringName));

        private static readonly MethodInfo? GetThemeFontSizeOneArgString =
            ResolveMethod("GetThemeFontSize", typeof(string));

        private static readonly MethodInfo? GetThemeFontSizeTwoArgString =
            ResolveMethod("GetThemeFontSize", typeof(string), typeof(string));

        private static readonly MethodInfo? GetThemeDefaultFontMethod = ResolveMethod("GetThemeDefaultFont");

        public static Font? GetThemeDefaultFont(Control control)
        {
            return GetThemeDefaultFontMethod?.Invoke(control, null) as Font;
        }

        public static Font? GetThemeFont(Control control, StringName key)
        {
            if (GetThemeFontOneArg != null)
                return GetThemeFontOneArg.Invoke(control, [key]) as Font;
            if (GetThemeFontTwoArg != null)
                return GetThemeFontTwoArg.Invoke(control, [key, LabelType]) as Font;
            var keyText = key.ToString();
            if (GetThemeFontOneArgString != null)
                return GetThemeFontOneArgString.Invoke(control, [keyText]) as Font;
            return GetThemeFontTwoArgString?.Invoke(control, [keyText, LabelType.ToString()]) as Font;
        }

        public static Color GetThemeColor(Control control, StringName key)
        {
            if (GetThemeColorOneArg != null)
                return (Color)(GetThemeColorOneArg.Invoke(control, [key]) ?? default(Color));
            if (GetThemeColorTwoArg != null)
                return (Color)(GetThemeColorTwoArg.Invoke(control, [key, LabelType]) ?? default(Color));
            var keyText = key.ToString();
            if (GetThemeColorOneArgString != null)
                return (Color)(GetThemeColorOneArgString.Invoke(control, [keyText]) ?? default(Color));
            return (Color)(GetThemeColorTwoArgString?.Invoke(control, [keyText, LabelType.ToString()]) ??
                           default(Color));
        }

        public static int GetThemeConstant(Control control, StringName key)
        {
            if (GetThemeConstantOneArg != null)
                return (int)(GetThemeConstantOneArg.Invoke(control, [key]) ?? 0);
            if (GetThemeConstantTwoArg != null)
                return (int)(GetThemeConstantTwoArg.Invoke(control, [key, LabelType]) ?? 0);
            var keyText = key.ToString();
            if (GetThemeConstantOneArgString != null)
                return (int)(GetThemeConstantOneArgString.Invoke(control, [keyText]) ?? 0);
            return (int)(GetThemeConstantTwoArgString?.Invoke(control, [keyText, LabelType.ToString()]) ?? 0);
        }

        public static int GetThemeFontSize(Control control, StringName key)
        {
            if (GetThemeFontSizeOneArg != null)
                return (int)(GetThemeFontSizeOneArg.Invoke(control, [key]) ?? 0);
            if (GetThemeFontSizeTwoArg != null)
                return (int)(GetThemeFontSizeTwoArg.Invoke(control, [key, LabelType]) ?? 0);
            var keyText = key.ToString();
            if (GetThemeFontSizeOneArgString != null)
                return (int)(GetThemeFontSizeOneArgString.Invoke(control, [keyText]) ?? 0);
            return (int)(GetThemeFontSizeTwoArgString?.Invoke(control, [keyText, LabelType.ToString()]) ?? 0);
        }

        private static MethodInfo? ResolveMethod(string name, params Type[] parameterTypes)
        {
            return typeof(Control).GetMethod(name, BindingFlags.Public | BindingFlags.Instance, null, parameterTypes,
                null);
        }
    }
}
