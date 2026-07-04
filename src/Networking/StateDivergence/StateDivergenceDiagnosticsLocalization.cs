using System.Globalization;
using System.Reflection;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Networking.StateDivergence
{
    internal static class StateDivergenceDiagnosticsLocalization
    {
        private static readonly AsyncLocal<bool> ForceEnglish = new();

        private static readonly Lazy<I18N> InstanceFactory = new(() => new(
            "RitsuLib-StateDivergenceDiagnostics",
            resourceFolders: ["STS2RitsuLib.Settings.Localization.StateDivergence"],
            resourceAssembly: Assembly.GetExecutingAssembly()));

        public static string Get(string key, string fallback)
        {
            return ForceEnglish.Value ? fallback : InstanceFactory.Value.Get(key, fallback);
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

        public static IDisposable UseEnglish()
        {
            var previous = ForceEnglish.Value;
            ForceEnglish.Value = true;
            return new EnglishScope(previous);
        }

        private sealed class EnglishScope(bool previous) : IDisposable
        {
            public void Dispose()
            {
                ForceEnglish.Value = previous;
            }
        }
    }
}
