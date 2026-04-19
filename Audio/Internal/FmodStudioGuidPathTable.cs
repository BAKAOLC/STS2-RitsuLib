using FileAccess = Godot.FileAccess;

namespace STS2RitsuLib.Audio.Internal
{
    internal static class FmodStudioGuidPathTable
    {
        private static readonly Lock Gate = new();
        private static Dictionary<string, string> _eventPathToGuid = [];

        internal static int EventMappingCount
        {
            get
            {
                lock (Gate)
                {
                    return _eventPathToGuid.Count;
                }
            }
        }

        internal static void Clear()
        {
            lock (Gate)
            {
                _eventPathToGuid = [];
            }
        }

        internal static bool TryLoadFromResourceFile(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath) || !FileAccess.FileExists(resourcePath))
                return false;

            using var file = FileAccess.Open(resourcePath, FileAccess.ModeFlags.Read);
            if (file is null)
                return false;

            ParseAndReplace(file.GetAsText(), resourcePath);
            return true;
        }

        internal static void ParseAndReplace(string text, string? sourceLabel = null)
        {
            var lines = text.Replace("\r\n", "\n").Split('\n');
            var next = new Dictionary<string, string>(StringComparer.Ordinal);
            var guidKeyToFirstPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var prefix = string.IsNullOrEmpty(sourceLabel) ? "[Audio] guids.txt" : $"[Audio] guids.txt ({sourceLabel})";

            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var raw = lines[lineIndex];
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;

                var close = line.IndexOf('}', StringComparison.Ordinal);
                if (close <= 1 || line[0] != '{')
                {
                    RitsuLibFramework.Logger.Warn(
                        $"{prefix} line {lineIndex + 1}: expected '{{guid}} …' format, skipped.");
                    continue;
                }

                var guidSpan = line.AsSpan(1, close - 1).Trim();
                if (guidSpan.IsEmpty)
                    continue;

                var guidFragment = guidSpan.ToString();
                if (!Guid.TryParse(guidFragment, out var parsed))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"{prefix} line {lineIndex + 1}: invalid GUID '{guidFragment}', skipped.");
                    continue;
                }

                var pathPart = close + 1 < line.Length ? line[(close + 1)..].TrimStart() : string.Empty;
                if (pathPart.Length == 0)
                    continue;

                if (!pathPart.StartsWith("event:", StringComparison.Ordinal))
                    continue;

                var braced = parsed.ToString("B");
                var dedupeKey = parsed.ToString("N");

                if (next.TryGetValue(pathPart, out var existingForPath) &&
                    !string.Equals(existingForPath, braced, StringComparison.OrdinalIgnoreCase))
                    RitsuLibFramework.Logger.Warn(
                        $"{prefix} line {lineIndex + 1}: duplicate event path '{pathPart}' was already mapped to " +
                        $"'{existingForPath}'; overwriting with '{braced}'.");

                if (guidKeyToFirstPath.TryGetValue(dedupeKey, out var firstPath) &&
                    !string.Equals(firstPath, pathPart, StringComparison.Ordinal))
                    RitsuLibFramework.Logger.Warn(
                        $"{prefix} line {lineIndex + 1}: GUID '{braced}' is also used for '{firstPath}'; " +
                        $"additional path '{pathPart}' (same GUID, multiple events — verify export).");
                else
                    guidKeyToFirstPath.TryAdd(dedupeKey, pathPart);

                next[pathPart] = braced;
            }

            lock (Gate)
            {
                _eventPathToGuid = next;
            }
        }

        internal static IReadOnlyList<KeyValuePair<string, string>> SnapshotEventMappings()
        {
            lock (Gate)
            {
                return [.. _eventPathToGuid];
            }
        }

        internal static bool TryGetStudioGuidForEventPath(string eventPath, out string guid)
        {
            guid = string.Empty;
            if (string.IsNullOrEmpty(eventPath))
                return false;

            lock (Gate)
            {
                if (!_eventPathToGuid.TryGetValue(eventPath, out var v) || v is null)
                    return false;

                guid = v;
                return true;
            }
        }
    }
}
