using STS2RitsuLib.Content;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">Represents a declarative card-pile registration for a content pack.</para>
    ///     <para xml:lang="zh-CN">表示内容包中的声明式卡牌牌堆注册项。</para>
    /// </summary>
    public sealed record CardPileRegistrationEntry
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a registration entry for a global card-pile ID.</para>
        ///     <para xml:lang="zh-CN">为全局卡牌牌堆 ID 创建注册项。</para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">The global card-pile ID to register.</para>
        ///     <para xml:lang="zh-CN">要注册的全局卡牌牌堆 ID。</para>
        /// </param>
        /// <param name="spec">
        ///     <para xml:lang="en">The card-pile behavior and presentation settings.</para>
        ///     <para xml:lang="zh-CN">卡牌牌堆的行为和显示设置。</para>
        /// </param>
        public CardPileRegistrationEntry(string id, ModCardPileSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(spec);

            Id = id;
            Spec = spec;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the global card-pile ID.</para>
        ///     <para xml:lang="zh-CN">获取全局卡牌牌堆 ID。</para>
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the settings applied during registration.</para>
        ///     <para xml:lang="zh-CN">获取注册时应用的设置。</para>
        /// </summary>
        public ModCardPileSpec Spec { get; }

        /// <summary>
        ///     <para xml:lang="en">Registers this entry with <paramref name="registry" />.</para>
        ///     <para xml:lang="zh-CN">使用 <paramref name="registry" /> 注册此项。</para>
        /// </summary>
        /// <param name="registry">
        ///     <para xml:lang="en">The owning mod's card-pile registry.</para>
        ///     <para xml:lang="zh-CN">所属模组的卡牌牌堆注册表。</para>
        /// </param>
        public void Register(ModCardPileRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);
            registry.Register(Id, Spec);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an entry whose ID is qualified with
        ///         <see cref="ModContentRegistry.GetQualifiedCardPileId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建使用 <see cref="ModContentRegistry.GetQualifiedCardPileId" /> 限定 ID 的注册项。
        ///     </para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The mod ID included in the qualified card-pile ID.</para>
        ///     <para xml:lang="zh-CN">写入限定卡牌牌堆 ID 的模组 ID。</para>
        /// </param>
        /// <param name="localPileStem">
        ///     <para xml:lang="en">The card-pile ID stem local to the mod.</para>
        ///     <para xml:lang="zh-CN">模组内使用的卡牌牌堆 ID 主体。</para>
        /// </param>
        /// <param name="spec">
        ///     <para xml:lang="en">The card-pile behavior and presentation settings.</para>
        ///     <para xml:lang="zh-CN">卡牌牌堆的行为和显示设置。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The registration entry containing the qualified ID.</para>
        ///     <para xml:lang="zh-CN">包含限定 ID 的注册项。</para>
        /// </returns>
        public static CardPileRegistrationEntry Owned(string modId, string localPileStem, ModCardPileSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localPileStem);
            ArgumentNullException.ThrowIfNull(spec);

            return new(ModContentRegistry.GetQualifiedCardPileId(modId, localPileStem), spec);
        }
    }
}
