using System.Reflection;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
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

        public static string ResolveModName(string modId, string fallback)
        {
            var configuredName = ModSettingsRegistry.GetModDisplayName(modId)?.Resolve();
            if (!string.IsNullOrWhiteSpace(configuredName))
                return configuredName;

            return ModManager.AllMods.FirstOrDefault(mod =>
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
