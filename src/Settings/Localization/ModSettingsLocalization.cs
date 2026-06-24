using System.Reflection;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Embedded JSON i18n for mod settings UI plus mod/page name resolution helpers.
    ///     mod 设置 UI 的嵌入式 JSON i18n，以及 mod / 页面名称解析 helper。
    /// </summary>
    internal static class ModSettingsLocalization
    {
        private static readonly Lazy<I18N> InstanceFactory = new(() => new(
            "RitsuLib-ModSettings",
            resourceFolders: ["STS2RitsuLib.Settings.Localization.ModSettingsUi"],
            resourceAssembly: Assembly.GetExecutingAssembly()));

        public static I18N Instance => InstanceFactory.Value;

        public static string Get(string key, string fallback)
        {
            return Instance.Get(key, fallback);
        }

        public static ModSettingsText Text(string key, string fallback)
        {
            return ModSettingsText.DeferredI18N(() => Instance, key, fallback);
        }

        public static string ResolveModName(string modId, string fallback)
        {
            var configuredName = ModSettingsRegistry.GetModDisplayName(modId)?.Resolve();
            if (!string.IsNullOrWhiteSpace(configuredName))
                return configuredName;

            return Sts2ModManagerCompat.EnumerateModsForManifestLookup().FirstOrDefault(mod =>
                       string.Equals(mod.manifest?.id, modId, StringComparison.OrdinalIgnoreCase))?.manifest?.name
                   ?? fallback;
        }

        public static string ResolveModNameFallback(string modId, string fallback)
        {
            var configuredName = ModSettingsRegistry.GetModDisplayName(modId)?.FallbackText;
            if (!string.IsNullOrWhiteSpace(configuredName))
                return configuredName;

            return Sts2ModManagerCompat.EnumerateModsForManifestLookup().FirstOrDefault(mod =>
                       string.Equals(mod.manifest?.id, modId, StringComparison.OrdinalIgnoreCase))?.manifest?.name
                   ?? fallback;
        }


        public static string ResolvePageDisplayName(ModSettingsPage page)
        {
            var title = page.Title?.Resolve();
            return !string.IsNullOrWhiteSpace(title) ? title : page.Id;
        }
    }
}
