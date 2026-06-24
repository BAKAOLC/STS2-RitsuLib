using System.Globalization;
using Godot;
using Environment = System.Environment;

namespace STS2RitsuLib.Diagnostics.Logging
{
    internal sealed record RitsuDebugLogViewerCommandLineOptions(
        int? Port,
        int? PortFallbackCount,
        IReadOnlyList<string> Warnings);

    internal static class RitsuDebugLogViewerCommandLine
    {
        private static readonly string[] PortOptionNames =
        [
            "--ritsulib-log-viewer-port",
            "--ritsulib-debug-log-viewer-port",
        ];

        private static readonly string[] PortFallbackCountOptionNames =
        [
            "--ritsulib-log-viewer-port-fallback-count",
            "--ritsulib-debug-log-viewer-port-fallback-count",
        ];

        internal static RitsuDebugLogViewerCommandLineOptions ParseCurrentProcess()
        {
            var userArgs = TryGetGodotCmdlineUserArgs();
            var userOptions = Parse(userArgs);
            if (userOptions.Port.HasValue || userOptions.PortFallbackCount.HasValue || userOptions.Warnings.Count > 0)
                return userOptions;

            var godotArgs = TryGetGodotCmdlineArgs();
            var godotOptions = Parse(godotArgs);
            if (godotOptions.Port.HasValue || godotOptions.PortFallbackCount.HasValue ||
                godotOptions.Warnings.Count > 0)
                return godotOptions;

            return Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
        }

        internal static RitsuDebugLogViewerCommandLineOptions Parse(IReadOnlyList<string> args)
        {
            int? port = null;
            int? portFallbackCount = null;
            var warnings = new List<string>();

            for (var i = 0; i < args.Count; i++)
            {
                if (TryReadOptionValue(args, ref i, PortOptionNames, out var portOptionName, out var portValue))
                {
                    if (TryParsePort(portValue, out var parsedPort))
                        port = parsedPort;
                    else
                        warnings.Add(
                            $"Ignoring invalid {portOptionName} value '{portValue}'; expected a port from 1 to 65535.");

                    continue;
                }

                if (!TryReadOptionValue(
                        args,
                        ref i,
                        PortFallbackCountOptionNames,
                        out var fallbackOptionName,
                        out var fallbackValue)) continue;

                if (TryParsePortFallbackCount(fallbackValue, out var parsedFallbackCount))
                    portFallbackCount = parsedFallbackCount;
                else
                    warnings.Add(
                        $"Ignoring invalid {fallbackOptionName} value '{fallbackValue}'; expected a number from 0 to 100.");
            }

            return new(port, portFallbackCount, warnings);
        }

        private static bool TryReadOptionValue(
            IReadOnlyList<string> args,
            ref int index,
            IReadOnlyList<string> optionNames,
            out string optionName,
            out string value)
        {
            var arg = args[index];
            foreach (var candidate in optionNames)
            {
                if (string.Equals(arg, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    optionName = candidate;
                    value = index + 1 < args.Count ? args[++index] : "";
                    return true;
                }

                if (!arg.StartsWith(candidate, StringComparison.OrdinalIgnoreCase) ||
                    arg.Length <= candidate.Length ||
                    arg[candidate.Length] != '=') continue;

                optionName = candidate;
                value = arg[(candidate.Length + 1)..];
                return true;
            }

            optionName = "";
            value = "";
            return false;
        }

        private static bool TryParsePort(string value, out int port)
        {
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out port) &&
                   port is >= 1 and <= 65535;
        }

        private static bool TryParsePortFallbackCount(string value, out int portFallbackCount)
        {
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out portFallbackCount) &&
                   portFallbackCount is >= 0 and <= 100;
        }

        private static string[] TryGetGodotCmdlineUserArgs()
        {
            try
            {
                return OS.GetCmdlineUserArgs();
            }
            catch (Exception)
            {
                return [];
            }
        }

        private static string[] TryGetGodotCmdlineArgs()
        {
            try
            {
                return OS.GetCmdlineArgs();
            }
            catch (Exception)
            {
                return [];
            }
        }
    }
}
