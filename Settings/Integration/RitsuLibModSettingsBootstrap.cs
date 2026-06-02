namespace STS2RitsuLib.Settings
{
    internal static partial class RitsuLibModSettingsBootstrap
    {
        private static readonly Lock InitLock = new();

        private static readonly IReadOnlyList<string> RuntimeHotkeyCategoryOrder =
        [
            "Gameplay",
            "UI",
            "Debug",
            "Developer tools",
            "Other",
        ];

        private static bool _initialized;

        internal static void Initialize()
        {
            lock (InitLock)
            {
                if (_initialized)
                    return;

                var ui = RitsuLibModSettingsUiBindings.Create();
                RegisterMainSettingsPage(ui);
                RegisterContentSourceHoverTipsPage(ui);
                RegisterContentModLoadOrderPage();
                RegisterDebugLogViewerPage(ui);
                RegisterHarmonySelfCheckAndCompendiumPages(ui);
                RegisterImagePngExportPage(ui);
                RefreshDynamicPages();
                RegisterDebugShowcasePage(ui);
                _initialized = true;
            }
        }

        internal static void EnsureFrameworkPagesRegistered()
        {
            if (ModSettingsRegistry.TryGetPage(Const.ModId, Const.ModId, out _))
                return;

            lock (InitLock)
            {
                if (ModSettingsRegistry.TryGetPage(Const.ModId, Const.ModId, out _))
                    return;

                var ui = RitsuLibModSettingsUiBindings.Create();
                RegisterMainSettingsPage(ui);
                RegisterContentSourceHoverTipsPage(ui);
                RegisterContentModLoadOrderPage();
                RegisterDebugLogViewerPage(ui);
                RegisterHarmonySelfCheckAndCompendiumPages(ui);
                RegisterImagePngExportPage(ui);
                RefreshDynamicPages();
                RegisterDebugShowcasePage(ui);
                _initialized = true;
            }
        }

        internal static void RefreshDynamicPages()
        {
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => ConfigureRuntimeHotkeysPage(page, RuntimeHotkeyCategoryOrder),
                "runtime-hotkeys");
        }

        private static ModSettingsText T(string key, string fallback)
        {
            return ModSettingsLocalization.Text(key, fallback);
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }
    }
}
