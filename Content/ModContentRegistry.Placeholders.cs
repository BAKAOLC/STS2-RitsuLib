using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        /// <summary>
        ///     Registers a generated placeholder card: no mod-authored CLR type, stable entry from
        ///     <paramref name="stableEntryStem" />.
        /// </summary>
        public void RegisterPlaceholderCard<TPool>(string stableEntryStem,
            PlaceholderCardDescriptor descriptor = default)
            where TPool : CardPoolModel
        {
            RegisterPlaceholderCard<TPool>(ModelPublicEntryOptions.FromStem(stableEntryStem), descriptor);
        }

        /// <summary>
        ///     Registers a generated placeholder card with an explicit public entry option.
        /// </summary>
        public void RegisterPlaceholderCard<TPool>(ModelPublicEntryOptions publicEntry,
            PlaceholderCardDescriptor descriptor)
            where TPool : CardPoolModel
        {
            var emitted = PlaceholderModelTypeEmitter.EmitCardType(ModId, in descriptor);
            RegisterPoolModel(typeof(TPool), emitted, "card", publicEntry);
        }

        /// <summary>
        ///     Registers a generated placeholder relic from <paramref name="stableEntryStem" /> and
        ///     <paramref name="descriptor" />.
        /// </summary>
        public void RegisterPlaceholderRelic<TPool>(string stableEntryStem,
            PlaceholderRelicDescriptor descriptor = default)
            where TPool : RelicPoolModel
        {
            RegisterPlaceholderRelic<TPool>(ModelPublicEntryOptions.FromStem(stableEntryStem), descriptor);
        }

        /// <summary>
        ///     Registers a generated placeholder relic with explicit <paramref name="publicEntry" />.
        /// </summary>
        public void RegisterPlaceholderRelic<TPool>(ModelPublicEntryOptions publicEntry,
            PlaceholderRelicDescriptor descriptor)
            where TPool : RelicPoolModel
        {
            var emitted = PlaceholderModelTypeEmitter.EmitRelicType(ModId, in descriptor);
            RegisterPoolModel(typeof(TPool), emitted, "relic", publicEntry);
        }

        /// <summary>
        ///     Registers a generated placeholder potion from <paramref name="stableEntryStem" /> and
        ///     <paramref name="descriptor" />.
        /// </summary>
        public void RegisterPlaceholderPotion<TPool>(string stableEntryStem,
            PlaceholderPotionDescriptor descriptor = default)
            where TPool : PotionPoolModel
        {
            RegisterPlaceholderPotion<TPool>(ModelPublicEntryOptions.FromStem(stableEntryStem), descriptor);
        }

        /// <summary>
        ///     Registers a generated placeholder potion with explicit <paramref name="publicEntry" />.
        /// </summary>
        public void RegisterPlaceholderPotion<TPool>(ModelPublicEntryOptions publicEntry,
            PlaceholderPotionDescriptor descriptor)
            where TPool : PotionPoolModel
        {
            var emitted = PlaceholderModelTypeEmitter.EmitPotionType(ModId, in descriptor);
            RegisterPoolModel(typeof(TPool), emitted, "potion", publicEntry);
        }
    }
}
