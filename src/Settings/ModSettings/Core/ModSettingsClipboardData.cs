using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsClipboardData
    {
        private const string ClipboardKind = "ritsulib.settings.value";
        private static readonly ConcurrentDictionary<Type, string> SchemaSignatureCache = new();

        private static readonly ConcurrentDictionary<Type, ClipboardSerializableMember[]> SerializableMemberCache =
            new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Maps CLR full names accepted as numeric source types in
        ///         <see cref="ModSettingsClipboardEnvelope.TypeName" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         映射 <see cref="ModSettingsClipboardEnvelope.TypeName" /> 中可作为数值源类型接受的 CLR 全名。
        ///     </para>
        /// </summary>
        private static readonly FrozenDictionary<string, Type> NumericEnvelopeTypes = new[]
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long),
            typeof(ulong), typeof(float), typeof(double), typeof(decimal),
        }.ToFrozenDictionary(type => type.FullName!, StringComparer.Ordinal);

        public static void CopyValue<TValue>(IModSettingsBinding binding, ModSettingsClipboardScope scope,
            IStructuredModSettingsValueAdapter<TValue> adapter, TValue value)
        {
            WriteClipboardEnvelope(new(
                ClipboardKind,
                typeof(TValue).FullName ?? typeof(TValue).Name,
                CreateTargetSignature(binding),
                GetSchemaSignature(typeof(TValue)),
                scope,
                adapter.Serialize(value)));
        }

        internal static void WriteClipboardEnvelope(ModSettingsClipboardEnvelope envelope)
        {
            DisplayServer.ClipboardSet(JsonSerializer.Serialize(envelope));
            ModSettingsClipboardAccess.InvalidateCache();
        }

        public static bool TryReadValue<TValue>(IModSettingsBinding binding,
            IStructuredModSettingsValueAdapter<TValue> adapter, out TValue value)
        {
            return TryReadValue(binding, adapter, out value, false);
        }

        internal static string GetSchemaSignatureForType(Type type)
        {
            return GetSchemaSignature(type);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Reads a value captured in <see cref="ModSettingsChromeBindingSnapshot" /> for the target binding,
        ///         applying the single-value clipboard compatibility rules without requiring a matching source-binding
        ///         signature.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为目标绑定读取 <see cref="ModSettingsChromeBindingSnapshot" /> 中捕获的值，
        ///         采用单值剪贴板兼容规则，但不要求源绑定签名匹配。
        ///     </para>
        /// </summary>
        internal static bool TryApplySerializedValueToBinding<TValue>(
            IModSettingsValueBinding<TValue> binding,
            IStructuredModSettingsValueAdapter<TValue> adapter,
            ModSettingsChromeBindingSnapshot snap,
            out TValue value)
        {
            var envelope = new ModSettingsClipboardEnvelope(
                ClipboardKind,
                snap.TypeFullName,
                string.Empty,
                snap.SchemaSignature,
                ModSettingsClipboardScope.Self,
                snap.JsonPayload);

            if (!TryReadEnvelopePayloadForTarget<TValue>(binding, envelope, false, out var payload))
                return TryReadCoercedScalarFromEnvelope(binding, envelope, false, out value);
            if (adapter.TryDeserialize(payload, out value))
                return true;
            if (TryCoerceJsonPayloadToValue(payload, out value))
                return true;
            value = default!;
            return false;
        }

        internal static void AddChromeBindingSnapshot<T>(Dictionary<string, ModSettingsChromeBindingSnapshot> target,
            string entryId, IModSettingsValueBinding<T> binding)
        {
            var adapter = binding is IStructuredModSettingsValueBinding<T> structured
                ? structured.Adapter
                : ModSettingsStructuredData.Json<T>();

            var t = typeof(T);
            target[entryId] = new(
                t.FullName ?? t.Name,
                GetSchemaSignature(t),
                adapter.Serialize(binding.Read()));
        }

        internal static bool TryReadValue<TValue>(IModSettingsBinding binding,
            IStructuredModSettingsValueAdapter<TValue> adapter, out TValue value,
            bool requireMatchingSourceBinding)
        {
            if (!ModSettingsClipboardAccess.TryGetText(out var clipboard))
            {
                value = default!;
                return false;
            }

            if (TryDeserializeEnvelope(clipboard, out var envelope)
                && envelope != null
                && string.Equals(envelope.Kind, ClipboardKind, StringComparison.Ordinal))
            {
                if (TryReadEnvelopePayloadForTarget<TValue>(binding, envelope, requireMatchingSourceBinding,
                        out var payload))
                {
                    if (adapter.TryDeserialize(payload, out value))
                        return true;
                    if (TryCoerceJsonPayloadToValue(payload, out value))
                        return true;
                    value = default!;
                    return false;
                }

                if (TryReadCoercedScalarFromEnvelope(binding, envelope, requireMatchingSourceBinding, out value))
                    return true;

                value = default!;
                return false;
            }

            if (MatchesJsonShape(typeof(TValue), clipboard)) return adapter.TryDeserialize(clipboard, out value);
            value = default!;
            return false;
        }

        internal static bool TryDeserializeEnvelope(string clipboard, out ModSettingsClipboardEnvelope? envelope)
        {
            try
            {
                envelope = JsonSerializer.Deserialize<ModSettingsClipboardEnvelope>(clipboard);
                // Deserialization can violate nullable annotations when required JSON members are missing or null.
                // ReSharper disable RedundantAlwaysMatchSubpattern
                return envelope is
                {
                    Kind.Length: > 0,
                    TypeName: not null,
                    TargetSignature: not null,
                    SchemaSignature: not null,
                    Payload: not null,
                };
                // ReSharper restore RedundantAlwaysMatchSubpattern
            }
            catch (JsonException)
            {
                envelope = null;
                return false;
            }
        }

        internal static bool TryReadEnvelopePayloadForTarget<TValue>(IModSettingsBinding binding,
            ModSettingsClipboardEnvelope? envelope, bool requireMatchingSourceBinding, out string payload)
        {
            payload = string.Empty;

            if (envelope is not { Kind: var kind } || !string.Equals(kind, ClipboardKind, StringComparison.Ordinal))
                return false;

            if (!string.Equals(envelope.TypeName, typeof(TValue).FullName ?? typeof(TValue).Name,
                    StringComparison.Ordinal))
                return false;

            if (requireMatchingSourceBinding
                && !string.Equals(envelope.TargetSignature, CreateTargetSignature(binding), StringComparison.Ordinal))
                return false;

            if (!string.Equals(envelope.SchemaSignature, GetSchemaSignature(typeof(TValue)), StringComparison.Ordinal))
                return false;

            payload = envelope.Payload;
            return true;
        }

        private static string CreateTargetSignature(IModSettingsBinding binding)
        {
            return $"{binding.ModId}:{binding.Scope}:{NormalizeDataKey(binding.DataKey)}";
        }

        private static string NormalizeDataKey(string dataKey)
        {
            if (string.IsNullOrWhiteSpace(dataKey))
                return string.Empty;

            var builder = new StringBuilder(dataKey.Length);
            for (var index = 0; index < dataKey.Length; index++)
            {
                if (dataKey[index] != '[')
                {
                    builder.Append(dataKey[index]);
                    continue;
                }

                builder.Append("[]");
                while (index + 1 < dataKey.Length && dataKey[index + 1] != ']')
                    index++;
                if (index + 1 < dataKey.Length && dataKey[index + 1] == ']')
                    index++;
            }

            return builder.ToString();
        }

        private static string GetSchemaSignature(Type type)
        {
            return SchemaSignatureCache.GetOrAdd(type, static targetType => BuildSchemaSignature(targetType, []));
        }

        private static string BuildSchemaSignature(Type type, HashSet<Type> activeTypes)
        {
            var normalizedType = Nullable.GetUnderlyingType(type) ?? type;
            if (!activeTypes.Add(normalizedType))
                return normalizedType.FullName ?? normalizedType.Name;

            try
            {
                if (normalizedType == typeof(string))
                    return "string";
                if (normalizedType == typeof(bool))
                    return "bool";
                if (normalizedType.IsEnum)
                    return $"enum:{normalizedType.FullName}:{string.Join(',', Enum.GetNames(normalizedType))}";
                if (IsNumericType(normalizedType))
                    return $"number:{normalizedType.FullName}";
                if (TryGetCollectionElementType(normalizedType, out var elementType))
                    return $"array<{BuildSchemaSignature(elementType, activeTypes)}>";

                var members = GetSerializableMembers(normalizedType);
                if (members.Length == 0)
                    return normalizedType.FullName ?? normalizedType.Name;

                return
                    $"object:{normalizedType.FullName}{{{string.Join(',', members.Select(member => $"{member.JsonName}:{BuildSchemaSignature(member.ValueType, activeTypes)}"))}}}";
            }
            finally
            {
                activeTypes.Remove(normalizedType);
            }
        }

        private static bool MatchesJsonShape(Type type, string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                return MatchesElement(type, document.RootElement);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool MatchesElement(Type type, JsonElement element)
        {
            var normalizedType = Nullable.GetUnderlyingType(type) ?? type;
            if (element.ValueKind == JsonValueKind.Null)
                return CanBeNull(type);

            if (normalizedType == typeof(string))
                return element.ValueKind == JsonValueKind.String;
            if (normalizedType == typeof(bool))
                return element.ValueKind is JsonValueKind.True or JsonValueKind.False;
            if (normalizedType.IsEnum)
                return element.ValueKind is JsonValueKind.String or JsonValueKind.Number;
            if (IsNumericType(normalizedType))
                return element.ValueKind == JsonValueKind.Number;
            if (TryGetCollectionElementType(normalizedType, out var elementType))
                return element.ValueKind == JsonValueKind.Array &&
                       element.EnumerateArray().All(child => MatchesElement(elementType, child));

            if (element.ValueKind != JsonValueKind.Object)
                return false;

            var members = GetSerializableMembers(normalizedType);
            if (members.Length == 0)
                return false;

            var properties = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            // Preserve JsonElement's concrete enumerator and fail immediately on duplicate members.
            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var property in element.EnumerateObject())
                if (!properties.TryAdd(property.Name, property.Value))
                    return false;

            foreach (var member in members)
            {
                if (!properties.Remove(member.JsonName, out var propertyValue))
                    return false;
                if (!MatchesElement(member.ValueType, propertyValue))
                    return false;
            }

            return properties.Count == 0;
        }

        private static ClipboardSerializableMember[] GetSerializableMembers(Type type)
        {
            return SerializableMemberCache.GetOrAdd(type, static targetType =>
            {
                var members = new Dictionary<string, ClipboardSerializableMember>(StringComparer.Ordinal);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

                foreach (var property in targetType.GetProperties(flags))
                {
                    if (property.GetIndexParameters().Length > 0 || property.GetMethod == null ||
                        !property.GetMethod.IsPublic)
                        continue;
                    if (property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition == JsonIgnoreCondition.Always)
                        continue;

                    var jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
                    members[jsonName] = new(jsonName, property.PropertyType);
                }

                foreach (var field in targetType.GetFields(flags))
                {
                    if (field.IsStatic)
                        continue;
                    if (field.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition == JsonIgnoreCondition.Always)
                        continue;

                    var jsonName = field.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? field.Name;
                    members.TryAdd(jsonName, new(jsonName, field.FieldType));
                }

                return
                [
                    .. members.Values
                        .OrderBy(member => member.JsonName, StringComparer.Ordinal),
                ];
            });
        }

        private static bool CanBeNull(Type type)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }

        private static bool IsNumericType(Type type)
        {
            return Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16
                or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single
                or TypeCode.Double or TypeCode.Decimal;
        }

        private static bool TryGetCollectionElementType(Type type, out Type elementType)
        {
            if (type.IsArray)
            {
                elementType = type.GetElementType()!;
                return true;
            }

            if (type.IsGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                if (genericType == typeof(List<>) || genericType == typeof(IList<>) ||
                    genericType == typeof(IReadOnlyList<>))
                {
                    elementType = type.GetGenericArguments()[0];
                    return true;
                }
            }

            elementType = null!;
            return false;
        }

        private static bool TryReadCoercedScalarFromEnvelope<TValue>(IModSettingsBinding binding,
            ModSettingsClipboardEnvelope envelope, bool requireMatchingSourceBinding, out TValue value)
        {
            value = default!;
            if (!IsCoercibleScalarTarget(typeof(TValue)))
                return false;

            if (requireMatchingSourceBinding
                && !string.Equals(envelope.TargetSignature, CreateTargetSignature(binding), StringComparison.Ordinal))
                return false;

            return CanCoerceClipboardEnvelopeScalarTo(typeof(TValue), envelope.TypeName) &&
                   IsEnvelopeScalarPayloadValid(envelope.TypeName, envelope.Payload) &&
                   TryCoerceJsonPayloadToValue(envelope.Payload, out value);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Allows scalar fallback for the same declared CLR type, lossless numeric widening, and supported
        ///         numeric-to-enum or numeric-to-Boolean conversions. It deliberately rejects unrelated values such as a
        ///         copied <see cref="double" /> slider value becoming a <see cref="string" /> choice key.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         允许相同 CLR 声明类型、无损数值拓宽，以及受支持的数值到枚举或布尔值转换作为标量回退。
        ///         此规则会明确拒绝无关值，例如不会把复制的 <see cref="double" /> 滑块值转换成
        ///         <see cref="string" /> 选项键。
        ///     </para>
        /// </summary>
        private static bool CanCoerceClipboardEnvelopeScalarTo(Type targetValueType, string envelopeTypeName)
        {
            if (string.IsNullOrEmpty(envelopeTypeName))
                return false;

            var ut = Nullable.GetUnderlyingType(targetValueType) ?? targetValueType;
            var targetTn = ut.FullName ?? ut.Name;
            if (string.Equals(envelopeTypeName, targetTn, StringComparison.Ordinal))
                return true;

            if (ut == typeof(string))
                return false;

            if (IsNumericType(ut) && NumericEnvelopeTypes.TryGetValue(envelopeTypeName, out var sourceNumericType))
                return CanWidenNumericWithoutLoss(sourceNumericType, ut);

            if (ut.IsEnum)
                return NumericEnvelopeTypes.ContainsKey(envelopeTypeName) ||
                       string.Equals(envelopeTypeName, ut.FullName ?? ut.Name, StringComparison.Ordinal);

            if (ut == typeof(bool))
                return NumericEnvelopeTypes.ContainsKey(envelopeTypeName) ||
                       string.Equals(envelopeTypeName, typeof(bool).FullName, StringComparison.Ordinal);

            return false;
        }

        private static bool CanWidenNumericWithoutLoss(Type sourceType, Type targetType)
        {
            if (sourceType == targetType)
                return true;

            return Type.GetTypeCode(sourceType) switch
            {
                TypeCode.SByte => targetType == typeof(short) || targetType == typeof(int) ||
                                  targetType == typeof(long) || targetType == typeof(float) ||
                                  targetType == typeof(double) || targetType == typeof(decimal),
                TypeCode.Byte => targetType == typeof(short) || targetType == typeof(ushort) ||
                                 targetType == typeof(int) || targetType == typeof(uint) ||
                                 targetType == typeof(long) || targetType == typeof(ulong) ||
                                 targetType == typeof(float) || targetType == typeof(double) ||
                                 targetType == typeof(decimal),
                TypeCode.Int16 => targetType == typeof(int) || targetType == typeof(long) ||
                                  targetType == typeof(float) || targetType == typeof(double) ||
                                  targetType == typeof(decimal),
                TypeCode.UInt16 => targetType == typeof(int) || targetType == typeof(uint) ||
                                   targetType == typeof(long) || targetType == typeof(ulong) ||
                                   targetType == typeof(float) || targetType == typeof(double) ||
                                   targetType == typeof(decimal),
                TypeCode.Int32 => targetType == typeof(long) || targetType == typeof(double) ||
                                  targetType == typeof(decimal),
                TypeCode.UInt32 => targetType == typeof(long) || targetType == typeof(ulong) ||
                                   targetType == typeof(double) || targetType == typeof(decimal),
                TypeCode.Int64 or TypeCode.UInt64 => targetType == typeof(decimal),
                TypeCode.Single => targetType == typeof(double),
                _ => false,
            };
        }

        private static bool IsCoercibleScalarTarget(Type type)
        {
            var ut = Nullable.GetUnderlyingType(type) ?? type;
            return ut == typeof(string) || ut == typeof(bool) || ut.IsEnum || IsNumericType(ut);
        }

        private static bool IsEnvelopeScalarPayloadValid(string envelopeTypeName, string json)
        {
            if (!NumericEnvelopeTypes.TryGetValue(envelopeTypeName, out var numericType))
                return true;

            try
            {
                using var document = JsonDocument.Parse(json);
                return TryConvertNumericElement(document.RootElement, numericType, out _);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool TryCoerceJsonPayloadToValue<TValue>(string json, out TValue value)
        {
            value = default!;
            try
            {
                using var doc = JsonDocument.Parse(json);
                return TryCoerceJsonElement(doc.RootElement, out value);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool TryCoerceJsonElement<TValue>(JsonElement element, out TValue value)
        {
            value = default!;
            var type = typeof(TValue);
            var ut = Nullable.GetUnderlyingType(type) ?? type;

            try
            {
                if (ut == typeof(string))
                {
                    var s = element.ValueKind switch
                    {
                        JsonValueKind.String => element.GetString() ?? string.Empty,
                        JsonValueKind.Number => element.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.Null => string.Empty,
                        _ => element.GetRawText(),
                    };
                    value = (TValue)(object)s;
                    return true;
                }

                if (ut == typeof(bool))
                    switch (element.ValueKind)
                    {
                        case JsonValueKind.True:
                            value = (TValue)(object)true;
                            return true;
                        case JsonValueKind.False:
                            value = (TValue)(object)false;
                            return true;
                        case JsonValueKind.String:
                            if (!bool.TryParse(element.GetString(), out var b)) return false;
                            value = (TValue)(object)b;
                            return true;

                        case JsonValueKind.Number:
                            if (!element.TryGetInt64(out var n)) return false;
                            value = (TValue)(object)(n != 0);
                            return true;

                        default:
                            return false;
                    }

                if (IsNumericType(ut))
                {
                    if (!TryConvertNumericElement(element, ut, out var converted))
                        return false;

                    value = (TValue)converted;
                    return true;
                }

                if (!ut.IsEnum)
                    return false;

                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                    {
                        var name = element.GetString();
                        if (string.IsNullOrEmpty(name))
                            return false;
                        if (!Enum.TryParse(ut, name, true, out var ev)) return false;
                        value = (TValue)ev;
                        return true;
                    }
                    case JsonValueKind.Number:
                    {
                        if (!element.TryGetInt64(out var li))
                            return false;
                        value = (TValue)Enum.ToObject(ut, li);
                        return true;
                    }
                    default:
                        return false;
                }
            }
            catch (InvalidCastException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        private static bool TryConvertNumericElement(JsonElement element, Type targetType, out object converted)
        {
            converted = null!;
            try
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.Number:
                        converted = JsonSerializer.Deserialize(element.GetRawText(), targetType)!;
                        return converted != null;
                    case JsonValueKind.String:
                        var text = element.GetString();
                        if (string.IsNullOrWhiteSpace(text))
                            return false;

                        converted = Convert.ChangeType(text, targetType, CultureInfo.InvariantCulture);
                        return converted switch
                        {
                            float single => float.IsFinite(single),
                            double @double => double.IsFinite(@double),
                            _ => true,
                        };
                    default:
                        return false;
                }
            }
            catch (JsonException)
            {
                converted = null!;
                return false;
            }
            catch (FormatException)
            {
                converted = null!;
                return false;
            }
            catch (InvalidCastException)
            {
                converted = null!;
                return false;
            }
            catch (OverflowException)
            {
                converted = null!;
                return false;
            }
        }

        private sealed record ClipboardSerializableMember(string JsonName, Type ValueType);
    }
}
