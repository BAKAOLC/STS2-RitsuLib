using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.CardTags.Serialization
{
    /// <summary>
    ///     Serializes <see cref="CardTag" /> as the qualified string id for ritsulib-minted values, otherwise as the
    ///     vanilla enum name. Accepts string ids, enum names, or raw 32-bit integers on deserialize.
    /// </summary>
    public sealed class CardTagJsonConverter : JsonConverter<CardTag>
    {
        /// <inheritdoc />
        public override CardTag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                {
                    var s = reader.GetString();
                    if (string.IsNullOrWhiteSpace(s))
                        return CardTag.None;

                    if (ModCardTagRegistry.TryGetCardTag(s, out var minted))
                        return minted;

                    return Enum.TryParse(s, true, out CardTag parsed)
                        ? parsed
                        : throw new JsonException($"Unknown CardTag id or name: '{s}'.");
                }
                case JsonTokenType.Number:
                    return (CardTag)reader.GetInt32();
                default:
                    throw new JsonException($"Unexpected token for CardTag: {reader.TokenType}.");
            }
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, CardTag value, JsonSerializerOptions options)
        {
            if (ModCardTagRegistry.TryGetId(value, out var id))
            {
                writer.WriteStringValue(id);
                return;
            }

            var name = Enum.GetName(value);
            if (name != null)
            {
                writer.WriteStringValue(name);
                return;
            }

            writer.WriteNumberValue(Convert.ToInt32(value));
        }
    }
}
