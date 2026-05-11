using STS2RitsuLib.Content;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Declarative card-pile row for content packs: register with <see cref="ModCardPileRegistry" /> in one call.
    /// </summary>
    public sealed record CardPileRegistrationEntry
    {
        /// <summary>
        ///     Creates a raw global card-pile registration row.
        /// </summary>
        /// <param name="id">Registered pile id.</param>
        /// <param name="spec">Pile metadata.</param>
        public CardPileRegistrationEntry(string id, ModCardPileSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(spec);

            Id = id;
            Spec = spec;
        }

        /// <summary>
        ///     Registered pile id.
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Pile metadata applied during registration.
        /// </summary>
        public ModCardPileSpec Spec { get; }

        /// <summary>
        ///     Registers this row against <paramref name="registry" />.
        /// </summary>
        public void Register(ModCardPileRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);
            registry.Register(Id, Spec);
        }

        /// <summary>
        ///     Builds an owned pile id via <see cref="ModContentRegistry.GetQualifiedCardPileId" /> and registers it.
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
