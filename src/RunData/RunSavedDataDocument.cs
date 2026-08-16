using System.Text.Json;
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
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
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

        public static byte[] InjectIntoUtf8Json(byte[] json, RunSavedDataDocument? document)
        {
            if (document == null || document.IsEmpty)
                return json;

            try
            {
                if (!TryLocateRootProperty(
                        json,
                        out var insertionPoint,
                        out var hasRootProperties,
                        out var existingValueStart,
                        out var existingValueEnd))
                    return json;

                var value = document.ToRootObject()[RootPropertyName]!;
                var valueBytes = JsonSerializer.SerializeToUtf8Bytes(value);
                if (existingValueStart >= 0)
                    return ReplaceRange(json, existingValueStart, existingValueEnd, valueBytes);

                return InsertRootProperty(json, insertionPoint, hasRootProperties, valueBytes);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[RunSavedData] Failed to inject run extension data: {ex.Message}");
                return json;
            }
        }

        private static bool TryLocateRootProperty(
            ReadOnlySpan<byte> json,
            out int insertionPoint,
            out bool hasRootProperties,
            out int existingValueStart,
            out int existingValueEnd)
        {
            insertionPoint = -1;
            hasRootProperties = false;
            existingValueStart = -1;
            existingValueEnd = -1;

            var reader = new Utf8JsonReader(json);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                return false;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName when reader.CurrentDepth == 1:
                        hasRootProperties = true;
                        if (!reader.ValueTextEquals(RootPropertyName))
                            continue;

                        if (!reader.Read())
                            return false;

                        existingValueStart = checked((int)reader.TokenStartIndex);
                        reader.Skip();
                        existingValueEnd = checked((int)reader.BytesConsumed);
                        continue;

                    case JsonTokenType.EndObject when reader.CurrentDepth == 0:
                        insertionPoint = checked((int)reader.TokenStartIndex);
                        break;
                }
            }

            if (insertionPoint < 0)
                return false;

            while (insertionPoint > 0 && IsJsonWhitespace(json[insertionPoint - 1]))
                insertionPoint--;
            return true;
        }

        private static byte[] InsertRootProperty(
            byte[] json,
            int insertionPoint,
            bool hasRootProperties,
            byte[] valueBytes)
        {
            var propertyPrefix = "\"_ritsulib\":"u8;
            var separatorLength = hasRootProperties ? 1 : 0;
            var result = GC.AllocateUninitializedArray<byte>(
                checked(json.Length + separatorLength + propertyPrefix.Length + valueBytes.Length));
            var offset = insertionPoint;

            json.AsSpan(0, insertionPoint).CopyTo(result);
            if (hasRootProperties)
                result[offset++] = (byte)',';
            propertyPrefix.CopyTo(result.AsSpan(offset));
            offset += propertyPrefix.Length;
            valueBytes.CopyTo(result.AsSpan(offset));
            offset += valueBytes.Length;
            json.AsSpan(insertionPoint).CopyTo(result.AsSpan(offset));
            return result;
        }

        private static byte[] ReplaceRange(byte[] json, int start, int end, byte[] replacement)
        {
            var result = GC.AllocateUninitializedArray<byte>(checked(json.Length - (end - start) + replacement.Length));
            json.AsSpan(0, start).CopyTo(result);
            replacement.CopyTo(result.AsSpan(start));
            json.AsSpan(end).CopyTo(result.AsSpan(start + replacement.Length));
            return result;
        }

        private static bool IsJsonWhitespace(byte value)
        {
            return value is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n';
        }
    }
}
