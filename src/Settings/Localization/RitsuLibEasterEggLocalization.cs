using System.Reflection;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
    internal static class RitsuLibEasterEggLocalization
    {
        private static readonly Lazy<I18N> InstanceFactory = new(() => new(
            "RitsuLib-EasterEggs",
            resourceFolders: ["STS2RitsuLib.Settings.Localization.EasterEggs"],
            resourceAssembly: Assembly.GetExecutingAssembly()));

        public static string Get(string key, string fallback)
        {
            return InstanceFactory.Value.Get(key, fallback);
        }
    }
}
