using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    internal readonly record struct BoxEdges(int Left, int Top, int Right, int Bottom);

    internal static class RitsuShellThemeLayoutResolver
    {
        internal static int ResolveInt(string path, int fallback)
        {
            return RitsuShellTheme.Current.TryGetNumber(path, out var value)
                ? (int)Math.Round(value)
                : fallback;
        }

        internal static float ResolveFloat(string path, float fallback)
        {
            return RitsuShellTheme.Current.TryGetNumber(path, out var value)
                ? (float)value
                : fallback;
        }

        internal static BoxEdges ResolveEdges(string basePath, int fallbackAll)
        {
            var all = ResolveInt(basePath, fallbackAll);
            all = ResolveInt(basePath + ".all", all);
            var left = ResolveInt(basePath + ".left", all);
            var top = ResolveInt(basePath + ".top", all);
            var right = ResolveInt(basePath + ".right", all);
            var bottom = ResolveInt(basePath + ".bottom", all);
            return new(left, top, right, bottom);
        }

        internal static Vector2 ResolveMinSize(string basePath, Vector2 fallback, bool allowOverride = true)
        {
            if (!allowOverride)
                return fallback;

            var width = ResolveFloat(basePath + ".width", fallback.X);
            width = ResolveFloat(basePath + ".minWidth", width);
            var height = ResolveFloat(basePath + ".height", fallback.Y);
            height = ResolveFloat(basePath + ".minHeight", height);
            return new(width, height);
        }
    }
}
