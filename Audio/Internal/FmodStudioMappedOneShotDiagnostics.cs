using System.Globalization;
using System.Text;
using Godot;

namespace STS2RitsuLib.Audio.Internal
{
    internal static class FmodStudioMappedOneShotDiagnostics
    {
        internal static string BuildMappedOneShotFailureDetail(string eventPath, string mappedGuidBraced)
        {
            try
            {
                var sb = new StringBuilder(768);
                sb.Append("path=").Append(eventPath).Append("; guidsTxtGuid=").Append(mappedGuidBraced);

                var guidRegisteredForPath = TryServerString(FmodStudioMethodNames.GetEventGuid, eventPath);
                sb.Append("; FMOD_get_event_guid(path)=");
                AppendGuidOrNone(sb, guidRegisteredForPath);

                if (FmodStudioGuidInterop.TryNormalizeForAddon(mappedGuidBraced, out var normalizedGuid))
                {
                    var pathRegisteredForGuid = TryServerString(FmodStudioMethodNames.GetEventPath, normalizedGuid);
                    sb.Append("; FMOD_get_event_path(guidsTxtGuid)=");
                    sb.Append(string.IsNullOrEmpty(pathRegisteredForGuid)
                        ? "(none — GUID not in loaded banks)"
                        : pathRegisteredForGuid);
                }

                if (!string.IsNullOrEmpty(guidRegisteredForPath) &&
                    !IsZeroLikeGuidString(guidRegisteredForPath) &&
                    GuidStringsEqualLoose(guidRegisteredForPath, mappedGuidBraced) == false)
                    sb.Append(
                        "; path→GUID mismatch: runtime cache maps this path to a different GUID than guids.txt — rebuild guids.txt from the same .bank files you ship.");

                sb.Append("; banks[eventCount]: ");
                sb.Append(SummarizeLoadedBanks());

                var leaf = LastPathSegment(eventPath);
                if (!string.IsNullOrEmpty(leaf))
                {
                    sb.Append("; cache scan paths containing '");
                    sb.Append(leaf);
                    sb.Append("': ");
                    sb.Append(ScanDescriptionsContaining(leaf));
                }

                sb.Append("; banks_still_loading=").Append(FmodStudioServer.TryBanksStillLoading()?.ToString() ?? "?");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"diagnostics failed: {ex.Message}";
            }
        }

        private static void AppendGuidOrNone(StringBuilder sb, string? guidFromServer)
        {
            if (string.IsNullOrEmpty(guidFromServer) || IsZeroLikeGuidString(guidFromServer))
                sb.Append("(none — path not in FMOD string table / no loaded description for path)");
            else
                sb.Append(guidFromServer);
        }

        private static bool IsZeroLikeGuidString(string s)
        {
            var span = s.AsSpan().Trim();
            if (span.Length >= 2 && span[0] == '{' && span[^1] == '}')
                span = span[1..^1];

            return Guid.TryParse(span, CultureInfo.InvariantCulture, out var g) && g == Guid.Empty;
        }

        private static bool? GuidStringsEqualLoose(string a, string b)
        {
            if (!Guid.TryParse(a.Trim().TrimStart('{').TrimEnd('}'), CultureInfo.InvariantCulture, out var ga))
                return null;

            if (!Guid.TryParse(b.Trim().TrimStart('{').TrimEnd('}'), CultureInfo.InvariantCulture, out var gb))
                return null;

            return ga.Equals(gb);
        }

        private static string? TryServerString(StringName method, string arg)
        {
            return !FmodStudioGateway.TryCall(out var v, method, arg)
                ? null
                : v.VariantType == Variant.Type.String
                    ? v.AsString()
                    : null;
        }

        private static string LastPathSegment(string eventPath)
        {
            if (string.IsNullOrEmpty(eventPath))
                return string.Empty;

            var t = eventPath.TrimEnd('/');
            var i = t.LastIndexOf('/');
            return i >= 0 && i + 1 < t.Length ? t[(i + 1)..] : t;
        }

        private static string SummarizeLoadedBanks()
        {
            if (!FmodStudioGateway.TryCall(out var banksVar, FmodStudioMethodNames.GetAllBanks) ||
                banksVar.VariantType != Variant.Type.Array)
                return "(unavailable)";

            var sb = new StringBuilder(256);
            foreach (var item in banksVar.AsGodotArray())
            {
                var bank = item.AsGodotObject();
                if (bank is null)
                    continue;

                string resPath;
                long n;
                try
                {
                    resPath = bank.Call("get_godot_res_path").AsString();
                    n = bank.Call("get_event_description_count").AsInt64();
                }
                catch
                {
                    continue;
                }

                if (sb.Length > 0)
                    sb.Append(' ');

                sb.Append(resPath).Append('=').Append(n);
            }

            return sb.Length == 0 ? "(none)" : sb.ToString();
        }

        private static string ScanDescriptionsContaining(string needle)
        {
            if (needle.Length == 0 ||
                !FmodStudioGateway.TryCall(out var allVar, FmodStudioMethodNames.GetAllEventDescriptions) ||
                allVar.VariantType != Variant.Type.Array)
                return "(unavailable)";

            var hits = new List<string>(6);
            foreach (var item in allVar.AsGodotArray())
            {
                var desc = item.AsGodotObject();
                if (desc is null)
                    continue;

                string p;
                string g;
                try
                {
                    p = desc.Call("get_path").AsString();
                    g = desc.Call("get_guid").AsString();
                }
                catch
                {
                    continue;
                }

                if (p.Contains(needle, StringComparison.OrdinalIgnoreCase))
                    hits.Add($"{p}→{g}");

                if (hits.Count >= 5)
                    break;
            }

            return hits.Count == 0 ? "(no matches)" : string.Join(" | ", hits);
        }
    }
}
