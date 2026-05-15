using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Relics;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     Registers an <see cref="ArchaicTooth" /> transcendence pair: when the player’s deck contains
        ///     <typeparamref name="TStarterCard" />, obtaining the relic transforms it into <typeparamref name="TAncientCard" />
        ///     (preserving upgrade state and enchantments, same as vanilla starters).
        ///     注册 <see cref="ArchaicTooth" /> 超越配对：当玩家牌组包含
        ///     <typeparamref name="TStarterCard" /> 时，获得该遗物会将其转化为 <typeparamref name="TAncientCard" />
        ///     （保留升级状态和附魔，与原版初始牌相同）。
        /// </summary>
        /// <remarks>
        ///     Uses <see cref="ModelDb.GetId{T}" /> for the starter key and stores <typeparamref name="TAncientCard" /> as a
        ///     type for lazy <see cref="ModelDb" /> resolution so this is safe during content-pack <c>Apply()</c>.
        ///     使用 <see cref="ModelDb.GetId{T}" /> 作为初始牌 key，并将 <typeparamref name="TAncientCard" /> 存为
        ///     类型以便延迟 <see cref="ModelDb" /> 解析，因此可安全用于内容包 <c>Apply()</c> 期间。
        /// </remarks>
        /// <param name="registeringModId">
        ///     Optional mod id for log messages when mappings are replaced.
        ///     映射被替换时用于日志消息的可选 Mod id。
        /// </param>
        public static void RegisterArchaicToothTranscendenceMapping<TStarterCard, TAncientCard>(
            string? registeringModId = null)
            where TStarterCard : CardModel
            where TAncientCard : CardModel
        {
            RegisterArchaicToothTranscendenceMapping(
                typeof(TStarterCard),
                typeof(TAncientCard),
                registeringModId);
        }

        /// <summary>
        ///     Registers an <see cref="ArchaicTooth" /> transcendence mapping using CLR types for both sides.
        ///     The starter id is resolved lazily so explicit registration can run before content registration assigns
        ///     RitsuLib's fixed ModelDb public entry.
        ///     使用两端 CLR 类型注册 <see cref="ArchaicTooth" /> 超越映射。初始牌 id 会延迟解析，因此显式注册可以早于内容注册为
        ///     类型分配 RitsuLib 固定 ModelDb public entry。
        /// </summary>
        public static void RegisterArchaicToothTranscendenceMapping(Type starterCardType, Type ancientCardType,
            string? registeringModId = null)
        {
            OrobasAncientUpgradeRegistry.RegisterTranscendence(starterCardType, ancientCardType, registeringModId);
        }

        /// <summary>
        ///     Registers an <see cref="ArchaicTooth" /> transcendence mapping using an explicit starter id and ancient card
        ///     type.
        ///     使用显式初始牌 id 和古代卡牌类型注册 <see cref="ArchaicTooth" /> 超越映射。
        /// </summary>
        /// <param name="starterCardId">
        ///     Deck card model id to match.
        ///     要匹配的牌组卡牌模型 id。
        /// </param>
        /// <param name="ancientCardType">
        ///     Concrete card type; resolved via <see cref="ModelDb" /> when the blessing runs.
        ///     具体卡牌类型；祝福运行时通过 <see cref="ModelDb" /> 解析。
        /// </param>
        /// <param name="registeringModId">
        ///     Optional mod id for log messages when mappings are replaced.
        ///     映射被替换时用于日志消息的可选 Mod id。
        /// </param>
        public static void RegisterArchaicToothTranscendenceMapping(ModelId starterCardId, Type ancientCardType,
            string? registeringModId = null)
        {
            OrobasAncientUpgradeRegistry.RegisterTranscendence(starterCardId, ancientCardType, registeringModId);
        }

        /// <summary>
        ///     Registers a <see cref="TouchOfOrobas" /> refinement pair: when the player’s starter relic is
        ///     <typeparamref name="TStarterRelic" />, the blessing replaces it with <typeparamref name="TUpgradedRelic" />.
        ///     注册 <see cref="TouchOfOrobas" /> 精炼配对：当玩家的初始遗物是
        ///     <typeparamref name="TStarterRelic" /> 时，该祝福会将其替换为 <typeparamref name="TUpgradedRelic" />。
        /// </summary>
        /// <remarks>
        ///     Uses <see cref="ModelDb.GetId{T}" /> for the starter key and stores the upgraded relic as a type for lazy
        ///     <see cref="ModelDb" /> resolution so this is safe during content-pack <c>Apply()</c>.
        ///     使用 <see cref="ModelDb.GetId{T}" /> 作为初始遗物 key，并将升级后的遗物存为类型以便延迟
        ///     <see cref="ModelDb" /> 解析，因此可安全用于内容包 <c>Apply()</c> 期间。
        /// </remarks>
        /// <param name="registeringModId">
        ///     Optional mod id for log messages when mappings are replaced.
        ///     映射被替换时用于日志消息的可选 Mod id。
        /// </param>
        public static void RegisterTouchOfOrobasRefinementMapping<TStarterRelic, TUpgradedRelic>(
            string? registeringModId = null)
            where TStarterRelic : RelicModel
            where TUpgradedRelic : RelicModel
        {
            RegisterTouchOfOrobasRefinementMapping(
                typeof(TStarterRelic),
                typeof(TUpgradedRelic),
                registeringModId);
        }

        /// <summary>
        ///     Registers a <see cref="TouchOfOrobas" /> refinement mapping using CLR types for both sides.
        ///     The starter id is resolved lazily so explicit registration can run before content registration assigns
        ///     RitsuLib's fixed ModelDb public entry.
        ///     使用两端 CLR 类型注册 <see cref="TouchOfOrobas" /> 精炼映射。初始遗物 id 会延迟解析，因此显式注册可以早于内容注册为
        ///     类型分配 RitsuLib 固定 ModelDb public entry。
        /// </summary>
        public static void RegisterTouchOfOrobasRefinementMapping(Type starterRelicType, Type upgradedRelicType,
            string? registeringModId = null)
        {
            OrobasAncientUpgradeRegistry.RegisterRefinement(starterRelicType, upgradedRelicType, registeringModId);
        }

        /// <summary>
        ///     Registers a <see cref="TouchOfOrobas" /> refinement mapping using explicit starter id and upgraded relic type.
        ///     使用显式初始遗物 id 和升级后遗物类型注册 <see cref="TouchOfOrobas" /> 精炼映射。
        /// </summary>
        /// <param name="starterRelicId">
        ///     Starter relic instance id to match.
        ///     要匹配的初始遗物实例 id。
        /// </param>
        /// <param name="upgradedRelicType">
        ///     Concrete relic type; resolved via <see cref="ModelDb" /> when the blessing runs.
        ///     具体遗物类型；祝福运行时通过 <see cref="ModelDb" /> 解析。
        /// </param>
        /// <param name="registeringModId">
        ///     Optional mod id for log messages when mappings are replaced.
        ///     映射被替换时用于日志消息的可选 Mod id。
        /// </param>
        public static void RegisterTouchOfOrobasRefinementMapping(ModelId starterRelicId, Type upgradedRelicType,
            string? registeringModId = null)
        {
            OrobasAncientUpgradeRegistry.RegisterRefinement(starterRelicId, upgradedRelicType, registeringModId);
        }
    }
}
