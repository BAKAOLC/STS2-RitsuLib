using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Saves.RawProgress
{
    internal static class RawProgressJsonPreservation
    {
        private static readonly ConditionalWeakTable<ProgressState, PreservationState> States = [];

        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false,
        };

        private static readonly string[] IdentityPropertyNames =
        [
            "id",
            "character",
            "achievement",
            "key",
            "name",
            "player_id",
            "slot",
        ];

        internal static bool TryAttach(ProgressState progress, string rawJson, string knownJson)
        {
            ArgumentNullException.ThrowIfNull(progress);

            try
            {
                var state = PreservationState.Create(rawJson, knownJson);
                if (!state.CanReconstructRawDocument())
                    return false;

                States.Remove(progress);
                States.Add(progress, state);
                return true;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RawProgress] Failed to install unknown-property preservation: {ex.Message}");
                return false;
            }
        }

        internal static string PreserveAndAdvance(ProgressState progress, string knownJson)
        {
            ArgumentNullException.ThrowIfNull(progress);
            ArgumentNullException.ThrowIfNull(knownJson);

            if (!States.TryGetValue(progress, out var state))
                return knownJson;

            try
            {
                return state.PreserveAndAdvance(knownJson);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RawProgress] Failed to preserve unknown properties during an ordinary save: {ex.Message}");
                States.Remove(progress);
                return knownJson;
            }
        }

        private static void MergeUnknown(JsonNode? raw, JsonNode? baseline, JsonNode? current)
        {
            if (raw is JsonObject rawObject && baseline is JsonObject baselineObject &&
                current is JsonObject currentObject)
            {
                MergeUnknownObject(rawObject, baselineObject, currentObject);
                return;
            }

            if (raw is JsonArray rawArray && baseline is JsonArray baselineArray && current is JsonArray currentArray)
                MergeUnknownArray(rawArray, baselineArray, currentArray);
        }

        private static void MergeUnknownObject(
            JsonObject raw,
            JsonObject baseline,
            JsonObject current)
        {
            foreach (var (propertyName, rawValue) in raw)
            {
                if (!baseline.TryGetPropertyValue(propertyName, out var baselineValue))
                {
                    if (!current.ContainsKey(propertyName))
                        current[propertyName] = rawValue?.DeepClone();
                    continue;
                }

                if (current.TryGetPropertyValue(propertyName, out var currentValue))
                    MergeUnknown(rawValue, baselineValue, currentValue);
            }
        }

        private static void MergeUnknownArray(JsonArray raw, JsonArray baseline, JsonArray current)
        {
            foreach (var currentItem in current)
            {
                if (!TryGetIdentity(currentItem, out var identityProperty, out var identityValue))
                    continue;

                var baselineItem = FindByIdentity(baseline, identityProperty, identityValue);
                var rawItem = FindByIdentity(raw, identityProperty, identityValue);
                if (baselineItem != null && rawItem != null)
                    MergeUnknown(rawItem, baselineItem, currentItem);
            }
        }

        private static bool AreUnknownPropertiesPreserved(JsonNode? raw, JsonNode? baseline, JsonNode? current)
        {
            if (raw is JsonObject rawObject && baseline is JsonObject baselineObject)
            {
                if (current is not JsonObject currentObject)
                    return !ContainsUnknownProperties(rawObject, baselineObject);

                foreach (var (propertyName, rawValue) in rawObject)
                {
                    if (!baselineObject.TryGetPropertyValue(propertyName, out var baselineValue))
                    {
                        if (!currentObject.TryGetPropertyValue(propertyName, out var currentValue) ||
                            !JsonNode.DeepEquals(rawValue, currentValue))
                            return false;
                        continue;
                    }

                    currentObject.TryGetPropertyValue(propertyName, out var currentKnownValue);
                    if (!AreUnknownPropertiesPreserved(rawValue, baselineValue, currentKnownValue))
                        return false;
                }

                return true;
            }

            if (raw is not JsonArray rawArray || baseline is not JsonArray baselineArray)
                return true;
            if (current is not JsonArray currentArray)
                return !ContainsUnknownProperties(rawArray, baselineArray);

            for (var index = 0; index < rawArray.Count; index++)
            {
                var rawItem = rawArray[index];
                if (!TryGetIdentity(rawItem, out var identityProperty, out var identityValue))
                {
                    var baselineItemAtIndex = index < baselineArray.Count ? baselineArray[index] : null;
                    if (ContainsUnknownProperties(rawItem, baselineItemAtIndex))
                        return false;
                    continue;
                }

                var baselineItem = FindByIdentity(baselineArray, identityProperty, identityValue);
                if (baselineItem == null)
                    return false;

                var currentItem = FindByIdentity(currentArray, identityProperty, identityValue);
                if (!AreUnknownPropertiesPreserved(rawItem, baselineItem, currentItem))
                    return false;
            }

            return true;
        }

        private static bool ContainsUnknownProperties(JsonNode? raw, JsonNode? baseline)
        {
            if (raw is JsonObject rawObject)
            {
                if (baseline is not JsonObject baselineObject)
                    return rawObject.Count > 0;

                foreach (var (propertyName, rawValue) in rawObject)
                {
                    if (!baselineObject.TryGetPropertyValue(propertyName, out var baselineValue) ||
                        ContainsUnknownProperties(rawValue, baselineValue))
                        return true;
                }

                return false;
            }

            if (raw is not JsonArray rawArray)
                return false;
            if (baseline is not JsonArray baselineArray)
                return rawArray.Count > 0;

            for (var index = 0; index < rawArray.Count; index++)
            {
                var rawItem = rawArray[index];
                if (!TryGetIdentity(rawItem, out var identityProperty, out var identityValue))
                {
                    var baselineItemAtIndex = index < baselineArray.Count ? baselineArray[index] : null;
                    if (ContainsUnknownProperties(rawItem, baselineItemAtIndex))
                        return true;
                    continue;
                }

                var baselineItem = FindByIdentity(baselineArray, identityProperty, identityValue);
                if (baselineItem == null || ContainsUnknownProperties(rawItem, baselineItem))
                    return true;
            }

            return false;
        }

        private static JsonNode? FindByIdentity(JsonArray array, string propertyName, string identityValue)
        {
            JsonNode? match = null;
            foreach (var item in array)
            {
                if (!TryReadIdentity(item, propertyName, out var candidate) || candidate != identityValue)
                    continue;

                if (match != null)
                    return null;

                match = item;
            }

            return match;
        }

        private static bool TryGetIdentity(JsonNode? node, out string propertyName, out string identityValue)
        {
            foreach (var candidate in IdentityPropertyNames)
                if (TryReadIdentity(node, candidate, out identityValue))
                {
                    propertyName = candidate;
                    return true;
                }

            propertyName = string.Empty;
            identityValue = string.Empty;
            return false;
        }

        private static bool TryReadIdentity(JsonNode? node, string propertyName, out string identityValue)
        {
            identityValue = string.Empty;
            if (node is not JsonObject obj ||
                !obj.TryGetPropertyValue(propertyName, out var value) ||
                value is not JsonValue jsonValue)
                return false;

            if (jsonValue.TryGetValue<string>(out var stringValue) && !string.IsNullOrWhiteSpace(stringValue))
            {
                identityValue = $"s:{stringValue}";
                return true;
            }

            if (jsonValue.TryGetValue<long>(out var longValue))
            {
                identityValue = $"n:{longValue}";
                return true;
            }

            return false;
        }

        private sealed class PreservationState
        {
            private readonly object _syncRoot = new();
            private JsonNode _knownBaseline;
            private JsonNode _rawDocument;

            private PreservationState(JsonNode rawDocument, JsonNode knownBaseline)
            {
                _rawDocument = rawDocument;
                _knownBaseline = knownBaseline;
            }

            internal static PreservationState Create(string rawJson, string knownJson)
            {
                var rawDocument = JsonNode.Parse(rawJson) ??
                                  throw new JsonException("The raw progress document parsed as null.");
                var knownBaseline = JsonNode.Parse(knownJson) ??
                                    throw new JsonException("The known progress projection parsed as null.");
                return new(rawDocument, knownBaseline);
            }

            internal bool CanReconstructRawDocument()
            {
                lock (_syncRoot)
                {
                    var reconstructed = _knownBaseline.DeepClone();
                    MergeUnknown(_rawDocument, _knownBaseline, reconstructed);
                    return AreUnknownPropertiesPreserved(_rawDocument, _knownBaseline, reconstructed);
                }
            }

            internal string PreserveAndAdvance(string knownJson)
            {
                var currentKnown = JsonNode.Parse(knownJson) ??
                                   throw new JsonException("The current known progress projection parsed as null.");

                lock (_syncRoot)
                {
                    var merged = currentKnown.DeepClone();
                    MergeUnknown(_rawDocument, _knownBaseline, merged);
                    _rawDocument = merged.DeepClone();
                    _knownBaseline = currentKnown;
                    return merged.ToJsonString(WriteOptions);
                }
            }
        }
    }
}
