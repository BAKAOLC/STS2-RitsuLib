using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Telemetry
{
    internal static class TelemetryPaths
    {
        internal static string Root => $"{ProfileManager.GetAccountBasePath()}/telemetry";

        internal static string ConsentPath => $"{Root}/consent.json";

        internal static string IdentityPath => $"{Root}/identity.json";

        internal static string QueuePath(string applicantId)
        {
            return $"{Root}/applicants/{SanitizeSegment(applicantId)}/queue.json";
        }

        internal static string StatePath(string applicantId)
        {
            return $"{Root}/applicants/{SanitizeSegment(applicantId)}/state.json";
        }

        private static string SanitizeSegment(string value)
        {
            var chars = value
                .Select(ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_' ? ch : '_')
                .ToArray();
            var result = new string(chars).Trim('_');
            return string.IsNullOrWhiteSpace(result) ? "unknown" : result;
        }
    }
}
