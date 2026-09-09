using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
    internal static class RitsuLibEasterEggLocalization
    {
        private static readonly Lazy<I18N> InstanceFactory = new(() => I18N.CreateForAssets(
            "RitsuLib-EasterEggs", "localization/easter-eggs"));

        public static string Get(string key, string fallback)
        {
            return InstanceFactory.Value.Get(key, fallback);
        }
    }
}
