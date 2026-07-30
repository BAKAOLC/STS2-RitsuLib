using System.Reflection;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides embedded JSON localization for the mod settings UI and resolves mod and page display names.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为模组设置界面提供嵌入式 JSON 本地化，并解析模组和页面的显示名称。
    ///     </para>
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
