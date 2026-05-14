using STS2RitsuLib.Content;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Declarative card-pile row for content packs: register with <see cref="ModCardPileRegistry" /> in one call.
    ///     content pack 使用的声明式卡牌牌堆行：一次调用即可通过 <see cref="ModCardPileRegistry" /> 注册。
    /// </summary>
    public sealed record CardPileRegistrationEntry
    {
        /// <summary>
        ///     Creates a raw global card-pile registration row.
        ///     创建原始全局卡牌牌堆注册行。
        /// </summary>
        /// <param name="id">
        ///     Registered pile id.
        ///     已注册的牌堆 id。
        /// </param>
        /// <param name="spec">
        ///     Pile metadata.
        ///     牌堆元数据。
        /// </param>
        public CardPileRegistrationEntry(string id, ModCardPileSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(spec);

            Id = id;
            Spec = spec;
        }

        /// <summary>
        ///     Registered pile id.
        ///     已注册的牌堆 id。
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Pile metadata applied during registration.
        ///     注册期间应用的牌堆元数据。
        /// </summary>
        public ModCardPileSpec Spec { get; }

        /// <summary>
        ///     Registers this row against <paramref name="registry" />.
        ///     将此行注册到 <paramref name="registry" />。
        /// </summary>
        public void Register(ModCardPileRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);
            registry.Register(Id, Spec);
        }

        /// <summary>
        ///     Builds an owned pile id via <see cref="ModContentRegistry.GetQualifiedCardPileId" /> and registers it.
        ///     通过 <see cref="ModContentRegistry.GetQualifiedCardPileId" /> 构建归属牌堆 id 并注册。
        /// </summary>
        public static CardPileRegistrationEntry Owned(string modId, string localPileStem, ModCardPileSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localPileStem);
            ArgumentNullException.ThrowIfNull(spec);

            return new(ModContentRegistry.GetQualifiedCardPileId(modId, localPileStem), spec);
        }
    }
}
