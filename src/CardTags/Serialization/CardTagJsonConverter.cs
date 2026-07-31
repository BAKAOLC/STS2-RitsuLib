using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.CardTags.Serialization
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Converts card tags between JSON strings or integers and <see cref="CardTag" /> values.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 JSON 字符串或整数与 <see cref="CardTag" /> 值之间转换卡牌标签。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Registered mod tags are written as their IDs. Named vanilla values are written as enum names,
    ///         and unnamed values are written as 32-bit integers.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         已注册的模组标签写为其 ID；有名称的原版值写为枚举名，未命名值写为 32 位整数。
    ///     </para>
    /// </remarks>
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

                    return ModCardTagRegistry.TryResolveCardTag(s, out var parsed)
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
