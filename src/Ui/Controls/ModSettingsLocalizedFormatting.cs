using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsLocalizedFormatting
    {
        private static readonly ConcurrentDictionary<string, CompositeFormat> Cache =
            new(StringComparer.Ordinal);

        internal static string Format(string format, object? argument)
        {
            var compositeFormat = Cache.GetOrAdd(format, static value => CompositeFormat.Parse(value));
            return string.Format(CultureInfo.CurrentCulture, compositeFormat, argument);
        }
    }
}
