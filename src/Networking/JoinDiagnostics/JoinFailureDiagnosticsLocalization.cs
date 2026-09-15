using System.Globalization;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Networking.JoinDiagnostics
{
    internal static class JoinFailureDiagnosticsLocalization
    {
        private static readonly Lazy<I18N> InstanceFactory = new(() => I18N.CreateForAssets(
            "RitsuLib-JoinDiagnostics", "localization/join-diagnostics"));

        public static string Get(string key, string fallback)
        {
            return InstanceFactory.Value.Get(key, fallback);
        }

        public static string Format(string key, string fallback, params object?[] args)
        {
            var template = Get(key, fallback);
            try
            {
                return string.Format(CultureInfo.InvariantCulture, template, args);
            }
            catch (FormatException)
            {
                return string.Format(CultureInfo.InvariantCulture, fallback, args);
            }
        }
    }
}
