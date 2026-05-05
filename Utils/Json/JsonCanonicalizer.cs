using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Json
{
    /// <summary>
    ///     JSON Canonicalization Scheme (JCS, RFC 8785) for <see cref="JsonNode" /> DOM.
    ///     https://www.rfc-editor.org/rfc/rfc8785
    /// </summary>
    public static class JsonCanonicalizer
    {
        private static readonly JsonWriterOptions WriterOptions = new()
        {
            Indented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            SkipValidation = false,
        };

        /// <summary>
        ///     Canonicalizes a JSON DOM node into a deterministic UTF-16 string representation.
        /// </summary>
        public static string Canonicalize(JsonNode? node)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8JsonWriter(buffer, WriterOptions);
            WriteCanonical(writer, node);
            writer.Flush();
            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        private static void WriteCanonical(Utf8JsonWriter writer, JsonNode? node)
        {
            if (node == null)
            {
                writer.WriteNullValue();
                return;
            }

            switch (node)
            {
                case JsonObject obj:
                    writer.WriteStartObject();
                    foreach (var kv in obj.OrderBy(static p => p.Key, StringComparer.Ordinal))
                    {
                        writer.WritePropertyName(kv.Key);
                        WriteCanonical(writer, kv.Value);
                    }

                    writer.WriteEndObject();
                    return;
                case JsonArray arr:
                    writer.WriteStartArray();
                    foreach (var e in arr)
                        WriteCanonical(writer, e);
                    writer.WriteEndArray();
                    return;
                case JsonValue:
                    WritePrimitiveCanonical(writer, node);
                    return;
                default:
                    WritePrimitiveCanonical(writer, node);
                    return;
            }
        }

        private static void WritePrimitiveCanonical(Utf8JsonWriter writer, JsonNode node)
        {
            // JsonNode's concrete storage varies. Serialize-to-element preserves numeric raw text and correct string escapes.
            var el = JsonSerializer.SerializeToElement(node);
            writer.WriteRawValue(el.GetRawText(), true);
        }
    }
}
