using System.Text.Json.Nodes;

namespace STS2RitsuLib.RunData
{
    internal sealed class RunSavedDataDocument
    {
        public const int CurrentVersion = 1;
        public const string RootPropertyName = "_ritsulib";
        private const string VersionPropertyName = "version";
        private const string RunSavedDataPropertyName = "run_saved_data";

        private readonly Dictionary<string, Dictionary<string, JsonObject>> _entries =
            new(StringComparer.OrdinalIgnoreCase);

        public bool IsEmpty => _entries.Values.All(entries => entries.Count == 0);

        public static RunSavedDataDocument? FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                if (JsonNode.Parse(json) is not JsonObject root ||
                    root[RootPropertyName] is not JsonObject ritsuRoot ||
                    ritsuRoot[RunSavedDataPropertyName] is not JsonObject dataRoot)
                    return null;

                var document = new RunSavedDataDocument();
                foreach (var modNode in dataRoot)
                {
                    if (modNode.Value is not JsonObject modObject)
                        continue;

                    foreach (var entryNode in modObject)
                        if (entryNode.Value is JsonObject entryObject)
                            document.SetRaw(modNode.Key, entryNode.Key, entryObject.DeepClone().AsObject());
                }

                return document.IsEmpty ? null : document;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[RunSavedData] Failed to parse run extension data: {ex.Message}");
                return null;
            }
        }

        public JsonObject ToRootObject()
        {
            var dataRoot = new JsonObject();
            foreach (var (modId, entries) in _entries.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (entries.Count == 0)
                    continue;

                var modObject = new JsonObject();
                foreach (var (key, entry) in entries.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                    modObject[key] = entry.DeepClone();

                dataRoot[modId] = modObject;
            }

            return new()
            {
                [RootPropertyName] = new JsonObject
                {
                    [VersionPropertyName] = CurrentVersion,
                    [RunSavedDataPropertyName] = dataRoot,
                },
            };
        }

        public RunSavedDataDocument Clone()
        {
            var clone = new RunSavedDataDocument();
            foreach (var (modId, entries) in _entries)
            foreach (var (key, entry) in entries)
                clone.SetRaw(modId, key, entry.DeepClone().AsObject());
            return clone;
        }

        public bool TryGetRaw(string modId, string key, out JsonObject entry)
        {
            if (_entries.TryGetValue(modId, out var entries) && entries.TryGetValue(key, out entry!))
                return true;

            entry = null!;
            return false;
        }

        public void SetRaw(string modId, string key, JsonObject entry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(entry);

            if (!_entries.TryGetValue(modId, out var entries))
            {
                entries = new(StringComparer.OrdinalIgnoreCase);
                _entries[modId] = entries;
            }

            entries[key] = entry;
        }

        public void Remove(string modId, string key)
        {
            if (!_entries.TryGetValue(modId, out var entries))
                return;

            entries.Remove(key);
            if (entries.Count == 0)
                _entries.Remove(modId);
        }

        public IEnumerable<(string ModId, string Key, JsonObject Entry)> Entries()
        {
            foreach (var (modId, entries) in _entries)
            foreach (var (key, entry) in entries)
                yield return (modId, key, entry);
        }

        public static string InjectIntoJson(string json, RunSavedDataDocument? document)
        {
            if (document == null || document.IsEmpty)
                return json;

            try
            {
                if (JsonNode.Parse(json) is not JsonObject root)
                    return json;

                root[RootPropertyName] = document.ToRootObject()[RootPropertyName]!.DeepClone();
                return root.ToJsonString(new() { WriteIndented = true });
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[RunSavedData] Failed to inject run extension data: {ex.Message}");
                return json;
            }
        }
    }
}
