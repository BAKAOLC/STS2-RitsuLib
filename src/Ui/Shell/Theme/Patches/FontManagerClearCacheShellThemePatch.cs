using MegaCrit.Sts2.Core.Localization.Fonts;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Ui.Shell.Theme.Patches
{
    internal sealed class FontManagerClearCacheShellThemePatch : IPatchMethod
    {
        public static string PatchId => "font_manager_clear_cache_shell_theme_refresh";

        public static bool IsCritical => false;

        public static string Description => "Refresh RitsuLib shell theme fonts when the game font cache is cleared";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(FontManager), nameof(FontManager.ClearCache), Type.EmptyTypes)];
        }

        public static void Postfix()
        {
            RitsuShellThemeRuntime.NotifyExternalFontCacheCleared();
        }
    }
}
