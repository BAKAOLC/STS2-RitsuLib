using System.Security.Cryptography;
using System.Text;
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
            return $"{Root}/applicants/{BuildUniqueSegment(applicantId)}/queue.json";
        }

        internal static string StatePath(string applicantId)
        {
            return $"{Root}/applicants/{BuildUniqueSegment(applicantId)}/state.json";
        }

        internal static string LegacyQueuePath(string applicantId)
        {
            return $"{Root}/applicants/{BuildLegacySegment(applicantId)}/queue.json";
        }

        internal static string LegacyStatePath(string applicantId)
        {
            return $"{Root}/applicants/{BuildLegacySegment(applicantId)}/state.json";
        }

        internal static string BuildUniqueSegment(string value)
        {
            var result = BuildLegacySegment(value);
            if (string.Equals(result, value, StringComparison.Ordinal))
                return result;

            var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..12]
                .ToLowerInvariant();
            return $"{result}-{digest}";
        }

        private static string BuildLegacySegment(string value)
        {
            var chars = value
                .Select(ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_' ? ch : '_')
                .ToArray();
            var result = new string(chars).Trim('_');
            return string.IsNullOrWhiteSpace(result) ? "unknown" : result;
        }
    }
}
