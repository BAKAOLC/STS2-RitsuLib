using STS2RitsuLib.Utils;

namespace STS2RitsuLib
{
    internal static class RitsuModuleLocalization
    {
        private static readonly Lazy<I18N> Localization = new(() => I18N.CreateForAssets(
            "RitsuLib-ModSettings", "localization/settings"));

        internal static I18N Instance => Localization.Value;

        internal static string Get(string key, string fallback)
        {
            return Instance.Get(key, fallback);
        }
    }
}
