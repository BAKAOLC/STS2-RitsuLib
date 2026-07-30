using STS2RitsuLib.Content;

namespace STS2RitsuLib.CardTags
{
    /// <summary>
    ///     <para xml:lang="en">Represents a declarative card-tag registration entry for content packs.</para>
    ///     <para xml:lang="zh-CN">表示内容包使用的声明式卡牌标签注册条目。</para>
    /// </summary>
    public sealed record CardTagRegistrationEntry(string Id)
    {
        /// <summary>
        ///     <para xml:lang="en">Registers this entry with <paramref name="registry" />.</para>
        ///     <para xml:lang="zh-CN">将此条目注册到 <paramref name="registry" />。</para>
        /// </summary>
        public void Register(ModCardTagRegistry registry)
        {
            registry.Register(Id);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an entry whose ID is qualified with
        ///         <see cref="ModContentRegistry.GetQualifiedCardTagId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建通过 <see cref="ModContentRegistry.GetQualifiedCardTagId" /> 限定 ID 的条目。
        ///     </para>
        /// </summary>
        public static CardTagRegistrationEntry Owned(string modId, string localTagStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localTagStem);

            return new(ModContentRegistry.GetQualifiedCardTagId(modId, localTagStem));
        }
    }
}
