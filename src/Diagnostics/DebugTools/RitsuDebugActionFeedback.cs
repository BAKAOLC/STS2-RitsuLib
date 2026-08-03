using System.Globalization;
using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal readonly record struct RitsuDebugActionFeedback(
        string Code,
        string Fallback,
        string[] Arguments)
    {
        private const int MaxCodeLength = 96;
        private const int MaxFallbackLength = 1024;
        private const int MaxArgumentCount = 8;
        private const int MaxArgumentLength = 256;
        private const int MaxFormattedLength = 2048;
        private const string EnumArgumentPrefix = "@enum:";

        internal static RitsuDebugActionFeedback Create(
            string code,
            string fallback,
            params object?[] arguments)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(code);
            ArgumentException.ThrowIfNullOrWhiteSpace(fallback);
            ArgumentNullException.ThrowIfNull(arguments);
            if (code.Length > MaxCodeLength ||
                code.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '.' or '-')))
                throw new ArgumentException("Feedback codes must be compact ASCII identifiers.", nameof(code));
            if (fallback.Length > MaxFallbackLength)
                throw new ArgumentException("The feedback fallback is too long.", nameof(fallback));
            if (arguments.Length > MaxArgumentCount)
                throw new ArgumentException("The feedback contains too many arguments.", nameof(arguments));

            var serializedArguments = arguments
                .Select(SerializeArgument)
                .ToArray();
            if (serializedArguments.Any(static argument => argument.Length > MaxArgumentLength))
                throw new ArgumentException("A feedback argument is too long.", nameof(arguments));

            var feedback = new RitsuDebugActionFeedback(code, fallback, serializedArguments);
            if (feedback.GetEnglishText().Length > MaxFormattedLength)
                throw new ArgumentException("The formatted feedback is too long.", nameof(arguments));
            return feedback;
        }

        internal bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Code) || Code.Length > MaxCodeLength ||
                Code.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '.' or '-')) ||
                string.IsNullOrWhiteSpace(Fallback) || Fallback.Length > MaxFallbackLength ||
                Arguments == null || Arguments.Length > MaxArgumentCount ||
                Arguments.Any(static argument => argument == null || argument.Length > MaxArgumentLength))
                return false;

            try
            {
                return GetEnglishText().Length <= MaxFormattedLength;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        internal string GetLocalizedText()
        {
            var format = ModSettingsLocalization.Get($"ritsulib.debugTools.feedback.{Code}", Fallback);
            try
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    format,
                    [.. Arguments.Select(static object (argument) => ResolveArgument(argument, true))]);
            }
            catch (FormatException)
            {
                return GetEnglishText();
            }
        }

        internal string GetEnglishText()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                Fallback,
                [.. Arguments.Select(static object (argument) => ResolveArgument(argument, false))]);
        }

        private static string SerializeArgument(object? argument)
        {
            return argument is Enum enumValue
                ? $"{EnumArgumentPrefix}{enumValue.GetType().Name}:{enumValue}"
                : Convert.ToString(argument, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string ResolveArgument(string argument, bool localize)
        {
            if (!argument.StartsWith(EnumArgumentPrefix, StringComparison.Ordinal))
                return argument;

            var separatorIndex = argument.IndexOf(':', EnumArgumentPrefix.Length);
            if (separatorIndex < 0 || separatorIndex == argument.Length - 1)
                return argument;

            var typeName = argument[EnumArgumentPrefix.Length..separatorIndex];
            var valueName = argument[(separatorIndex + 1)..];
            return localize
                ? ModSettingsLocalization.Get(
                    $"ritsulib.debugTools.enum.{typeName}.{valueName}",
                    valueName)
                : valueName;
        }
    }
}
