using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Generates and registers a placeholder card using <paramref name="stableEntryStem" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="stableEntryStem" /> 生成并注册占位卡牌。
        ///     </para>
        /// </summary>
        public void RegisterPlaceholderCard<TPool>(string stableEntryStem,
            PlaceholderCardDescriptor descriptor = default)
            where TPool : CardPoolModel
        {
            RegisterPlaceholderCard<TPool>(ModelPublicEntryOptions.FromStem(stableEntryStem), descriptor);
        }

        /// <summary>
        ///     <para xml:lang="en">Generates and registers a placeholder card with an explicit public entry.</para>
        ///     <para xml:lang="zh-CN">使用显式公共条目生成并注册占位卡牌。</para>
        /// </summary>
        public void RegisterPlaceholderCard<TPool>(ModelPublicEntryOptions publicEntry,
            PlaceholderCardDescriptor descriptor)
            where TPool : CardPoolModel
        {
            var emitted = PlaceholderModelTypeEmitter.EmitCardType(ModId, in descriptor);
            RegisterPoolModel(
                typeof(TPool),
                emitted,
                typeof(CardPoolModel),
                typeof(CardModel),
                "card",
                publicEntry);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Generates and registers a placeholder relic using <paramref name="stableEntryStem" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="stableEntryStem" /> 生成并注册占位遗物。
        ///     </para>
        /// </summary>
        public void RegisterPlaceholderRelic<TPool>(string stableEntryStem,
            PlaceholderRelicDescriptor descriptor = default)
            where TPool : RelicPoolModel
        {
            RegisterPlaceholderRelic<TPool>(ModelPublicEntryOptions.FromStem(stableEntryStem), descriptor);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Generates and registers a placeholder relic with <paramref name="publicEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="publicEntry" /> 生成并注册占位遗物。
        ///     </para>
        /// </summary>
        public void RegisterPlaceholderRelic<TPool>(ModelPublicEntryOptions publicEntry,
            PlaceholderRelicDescriptor descriptor)
            where TPool : RelicPoolModel
        {
            var emitted = PlaceholderModelTypeEmitter.EmitRelicType(ModId, in descriptor);
            RegisterPoolModel(
                typeof(TPool),
                emitted,
                typeof(RelicPoolModel),
                typeof(RelicModel),
                "relic",
                publicEntry);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Generates and registers a placeholder potion using <paramref name="stableEntryStem" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="stableEntryStem" /> 生成并注册占位药水。
        ///     </para>
        /// </summary>
        public void RegisterPlaceholderPotion<TPool>(string stableEntryStem,
            PlaceholderPotionDescriptor descriptor = default)
            where TPool : PotionPoolModel
        {
            RegisterPlaceholderPotion<TPool>(ModelPublicEntryOptions.FromStem(stableEntryStem), descriptor);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Generates and registers a placeholder potion with <paramref name="publicEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="publicEntry" /> 生成并注册占位药水。
        ///     </para>
        /// </summary>
        public void RegisterPlaceholderPotion<TPool>(ModelPublicEntryOptions publicEntry,
            PlaceholderPotionDescriptor descriptor)
            where TPool : PotionPoolModel
        {
            var emitted = PlaceholderModelTypeEmitter.EmitPotionType(ModId, in descriptor);
            RegisterPoolModel(
                typeof(TPool),
                emitted,
                typeof(PotionPoolModel),
                typeof(PotionModel),
                "potion",
                publicEntry);
        }
    }
}
