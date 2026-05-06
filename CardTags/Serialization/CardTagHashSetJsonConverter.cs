using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.CardTags.Serialization
{
    /// <summary>
    ///     JSON array converter for <see cref="HashSet{T}" /> of <see cref="CardTag" /> using
    ///     <see cref="CardTagJsonConverter" /> rules per element.
    /// </summary>
    public sealed class CardTagHashSetJsonConverter : JsonConverter<HashSet<CardTag>>
    {
        private static readonly CardTagJsonConverter ElementConverter = new();

        /// <inheritdoc />
        public override HashSet<CardTag>? Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected start of array for HashSet<CardTag>.");

            var set = new HashSet<CardTag>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    return set;

                set.Add(ElementConverter.Read(ref reader, typeof(CardTag), options));
            }

            throw new JsonException("Unterminated JSON array for HashSet<CardTag>.");
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, HashSet<CardTag> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var tag in value.OrderBy(t => Convert.ToInt32(t)))
                ElementConverter.Write(writer, tag, options);

            writer.WriteEndArray();
        }
    }
}
