using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Scaffolding.Content
{
    public interface IContentRegistrationEntry
    {
        void Register(ModContentRegistry registry);
    }

    public sealed class CharacterRegistrationEntry<TCharacter> : IContentRegistrationEntry
        where TCharacter : CharacterModel
    {
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCharacter<TCharacter>();
        }
    }

    public sealed class ActRegistrationEntry<TAct> : IContentRegistrationEntry
        where TAct : ActModel
    {
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterAct<TAct>();
        }
    }

    public sealed class CardRegistrationEntry<TPool, TCard> : IContentRegistrationEntry
        where TPool : CardPoolModel
        where TCard : CardModel
    {
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCard<TPool, TCard>();
        }
    }

    public sealed class RelicRegistrationEntry<TPool, TRelic> : IContentRegistrationEntry
        where TPool : RelicPoolModel
        where TRelic : RelicModel
    {
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterRelic<TPool, TRelic>();
        }
    }

    public sealed class PotionRegistrationEntry<TPool, TPotion> : IContentRegistrationEntry
        where TPool : PotionPoolModel
        where TPotion : PotionModel
    {
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPotion<TPool, TPotion>();
        }
    }

    public sealed class PowerRegistrationEntry<TPower> : IContentRegistrationEntry
        where TPower : PowerModel
    {
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPower<TPower>();
        }
    }
}
