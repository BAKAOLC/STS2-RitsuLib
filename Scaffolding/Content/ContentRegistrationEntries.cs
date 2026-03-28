using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Declarative manifest entry that registers content with a <see cref="ModContentRegistry" /> when applied.
    /// </summary>
    public interface IContentRegistrationEntry
    {
        /// <summary>
        ///     Performs the registration for this entry against <paramref name="registry" />.
        /// </summary>
        void Register(ModContentRegistry registry);
    }

    /// <summary>
    ///     Registers a mod character model type.
    /// </summary>
    /// <typeparam name="TCharacter">Concrete <see cref="CharacterModel" /> to register.</typeparam>
    public sealed class CharacterRegistrationEntry<TCharacter> : IContentRegistrationEntry
        where TCharacter : CharacterModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCharacter<TCharacter>();
        }
    }

    /// <summary>
    ///     Registers a mod act model type.
    /// </summary>
    /// <typeparam name="TAct">Concrete <see cref="ActModel" /> to register.</typeparam>
    public sealed class ActRegistrationEntry<TAct> : IContentRegistrationEntry
        where TAct : ActModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterAct<TAct>();
        }
    }

    /// <summary>
    ///     Registers a card type with its pool and optional public entry options.
    /// </summary>
    /// <typeparam name="TPool">Card pool model type.</typeparam>
    /// <typeparam name="TCard">Card model type.</typeparam>
    /// <param name="publicEntry">Optional stable entry / visibility options.</param>
    public sealed class CardRegistrationEntry<TPool, TCard>(ModelPublicEntryOptions publicEntry = default)
        : IContentRegistrationEntry
        where TPool : CardPoolModel
        where TCard : CardModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCard<TPool, TCard>(publicEntry);
        }
    }

    /// <summary>
    ///     Registers a relic type with its pool and optional public entry options.
    /// </summary>
    /// <typeparam name="TPool">Relic pool model type.</typeparam>
    /// <typeparam name="TRelic">Relic model type.</typeparam>
    /// <param name="publicEntry">Optional stable entry / visibility options.</param>
    public sealed class RelicRegistrationEntry<TPool, TRelic>(ModelPublicEntryOptions publicEntry = default)
        : IContentRegistrationEntry
        where TPool : RelicPoolModel
        where TRelic : RelicModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterRelic<TPool, TRelic>(publicEntry);
        }
    }

    /// <summary>
    ///     Registers a potion type with its pool and optional public entry options.
    /// </summary>
    /// <typeparam name="TPool">Potion pool model type.</typeparam>
    /// <typeparam name="TPotion">Potion model type.</typeparam>
    /// <param name="publicEntry">Optional stable entry / visibility options.</param>
    public sealed class PotionRegistrationEntry<TPool, TPotion>(ModelPublicEntryOptions publicEntry = default)
        : IContentRegistrationEntry
        where TPool : PotionPoolModel
        where TPotion : PotionModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPotion<TPool, TPotion>(publicEntry);
        }
    }

    /// <summary>
    ///     Registers a standalone power model type.
    /// </summary>
    /// <typeparam name="TPower">Concrete <see cref="PowerModel" />.</typeparam>
    public sealed class PowerRegistrationEntry<TPower> : IContentRegistrationEntry
        where TPower : PowerModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPower<TPower>();
        }
    }

    /// <summary>
    ///     Registers a shared card pool type (not tied to a single character registration here).
    /// </summary>
    /// <typeparam name="TPool">Concrete <see cref="CardPoolModel" />.</typeparam>
    public sealed class SharedCardPoolRegistrationEntry<TPool> : IContentRegistrationEntry
        where TPool : CardPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterSharedCardPool<TPool>();
        }
    }
}
