using System.Text.RegularExpressions;

namespace STS2RitsuLib.Telemetry.Diagnostics
{
    internal static partial class EngineErrorSanitizer
    {
        internal static string Sanitize(string value)
        {
            try
            {
                return Paths().Replace(value, match => match.Groups["keep"].Success
                    ? match.Value
                    : "<absolute-path>");
            }
            catch (RegexMatchTimeoutException)
            {
                return "<redacted>";
            }
        }

        [GeneratedRegex(
            """(?<keep>["'](?:res|user)://[^"'\r\n]*["']|(?:res|user)://[^\s"'<>]+|(?:Node not found:\s*|relative to\s*)["'][^"'\r\n]*["'])|["'](?:file://)?(?:[A-Za-z]:[\\/]|\\\\|/)[^"'\r\n]*["']|\((?:[A-Za-z]:[\\/]|\\\\|/)[^)\r\n]*\)|(?<![\w:/\\])(?:file://)?(?:[A-Za-z]:[\\/]|\\\\|/)[^"'<>():\r\n]*?\.[A-Za-z0-9]{1,10}(?=[:)\s,;]|$)|(?<![\w:/\\])(?:file://)?(?:[A-Za-z]:[\\/]|\\\\|/)[^\s"'<>),;]*""",
            RegexOptions.CultureInvariant, 100)]
        private static partial Regex Paths();
    }
}
