using System.Text.Json.Nodes;

namespace STS2RitsuLib.Models.Capabilities
{
    internal sealed class ModelSavedDataDocument
    {
        public const int CurrentVersion = 1;
        private const string VersionPropertyName = "version";
        private const string ModelSavedDataPropertyName = "model_saved_data";

        private readonly Dictionary<string, Dictionary<string, JsonObject>> _entries =
            new(StringComparer.OrdinalIgnoreCase);

        public bool IsEmpty => _entries.Values.All(static entries => entries.Count == 0);

        public static ModelSavedDataDocument? FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                if (JsonNode.Parse(json) is not JsonObject root ||
                    root[ModelSavedDataPropertyName] is not JsonObject dataRoot)
                    return null;

                var document = new ModelSavedDataDocument();
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
                RitsuLibFramework.Logger.Warn($"[ModelSavedData] Failed to parse model extension data: {ex.Message}");
                return null;
            }
        }

        public JsonObject ToRootObject()
        {
            var dataRoot = new JsonObject();
            foreach (var (modId, entries) in
                     _entries.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (entries.Count == 0)
                    continue;

                var modObject = new JsonObject();
                foreach (var (key, entry) in entries.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                    modObject[key] = entry.DeepClone();

                dataRoot[modId] = modObject;
            }

            return new()
            {
                [VersionPropertyName] = CurrentVersion,
                [ModelSavedDataPropertyName] = dataRoot,
            };
        }

        public ModelSavedDataDocument Clone()
        {
            var clone = new ModelSavedDataDocument();
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

        public string ToJsonString()
        {
            return ToRootObject().ToJsonString(ModelSavedDataJson.Options);
        }
    }
}
