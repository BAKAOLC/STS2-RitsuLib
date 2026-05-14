using STS2RitsuLib.Content;

namespace STS2RitsuLib.CardTags
{
    /// <summary>
    ///     Declarative card-tag row for content packs: register with <see cref="ModCardTagRegistry" /> in one call.
    ///     内容包使用的声明式卡牌标签行：可通过一次调用注册到 <see cref="ModCardTagRegistry" />。
    /// </summary>
    public sealed record CardTagRegistrationEntry(string Id)
    {
        /// <summary>
        ///     Registers this entry on <paramref name="registry" />.
        ///     将此条目注册到 <paramref name="registry" />。
        /// </summary>
        public void Register(ModCardTagRegistry registry)
        {
            registry.Register(Id);
        }

        /// <summary>
        ///     Builds an owned tag id via <see cref="ModContentRegistry.GetQualifiedCardTagId" /> and registers it.
        ///     通过 <see cref="ModContentRegistry.GetQualifiedCardTagId" /> 构建归属标签 ID 并注册。
        /// </summary>
        public static CardTagRegistrationEntry Owned(string modId, string localTagStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localTagStem);

            return new(ModContentRegistry.GetQualifiedCardTagId(modId, localTagStem));
        }
    }
}
