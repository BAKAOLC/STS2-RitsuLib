using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Relics;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers an <see cref="ArchaicTooth" /> transcendence pair. If the player's deck contains
        ///         <typeparamref name="TStarterCard" />, obtaining the relic transforms it into <typeparamref name="TAncientCard" />
        ///         while preserving upgrades and enchantments, as with vanilla starter cards.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册 <see cref="ArchaicTooth" /> 超越配对。若玩家牌组包含 <typeparamref name="TStarterCard" />，获得该遗物时会将其
        ///         转化为 <typeparamref name="TAncientCard" />，并像原版初始牌一样保留升级和附魔。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Uses <see cref="ModelDb.GetId{T}" /> for the starter key and stores <typeparamref name="TAncientCard" /> as
        ///         a type for lazy <see cref="ModelDb" /> resolution, so it is safe during content-pack <c>Apply()</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <see cref="ModelDb.GetId{T}" /> 作为初始牌键，并将 <typeparamref name="TAncientCard" /> 保存为类型以供
        ///         <see cref="ModelDb" /> 延迟解析，因此可在内容包 <c>Apply()</c> 期间安全调用。
        ///     </para>
        /// </remarks>
        /// <param name="registeringModId">
        ///     <para xml:lang="en">Optional mod ID included in log messages when a mapping is replaced.</para>
        ///     <para xml:lang="zh-CN">映射被替换时包含在日志消息中的可选模组 ID。</para>
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
        ///     <para xml:lang="en">
        ///         Registers an <see cref="ArchaicTooth" /> transcendence mapping from CLR types. The starter ID resolves
        ///         lazily, so registration can occur before content registration assigns RitsuLib's fixed public <see cref="ModelDb" /> entry.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 CLR 类型注册 <see cref="ArchaicTooth" /> 超越映射。初始牌 ID 会延迟解析，因此可在内容注册为类型分配
        ///         RitsuLib 固定的公共 <see cref="ModelDb" /> 条目前进行注册。
        ///     </para>
        /// </summary>
        public static void RegisterArchaicToothTranscendenceMapping(Type starterCardType, Type ancientCardType,
            string? registeringModId = null)
        {
            OrobasAncientUpgradeRegistry.RegisterTranscendence(starterCardType, ancientCardType, registeringModId);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers an <see cref="ArchaicTooth" /> transcendence mapping from an explicit starter ID and Ancient-card type.</para>
        ///     <para xml:lang="zh-CN">使用显式初始牌 ID 和先古卡牌类型注册 <see cref="ArchaicTooth" /> 超越映射。</para>
        /// </summary>
        /// <param name="starterCardId">
        ///     <para xml:lang="en">Deck card model ID to match.</para>
        ///     <para xml:lang="zh-CN">要匹配的牌组卡牌模型 ID。</para>
        /// </param>
        /// <param name="ancientCardType">
        ///     <para xml:lang="en">Concrete card type, resolved through <see cref="ModelDb" /> when the blessing runs.</para>
        ///     <para xml:lang="zh-CN">具体卡牌类型；祝福运行时通过 <see cref="ModelDb" /> 解析。</para>
        /// </param>
        /// <param name="registeringModId">
        ///     <para xml:lang="en">Optional mod ID included in log messages when a mapping is replaced.</para>
        ///     <para xml:lang="zh-CN">映射被替换时包含在日志消息中的可选模组 ID。</para>
        /// </param>
        public static void RegisterArchaicToothTranscendenceMapping(ModelId starterCardId, Type ancientCardType,
            string? registeringModId = null)
        {
            OrobasAncientUpgradeRegistry.RegisterTranscendence(starterCardId, ancientCardType, registeringModId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a <see cref="TouchOfOrobas" /> refinement pair. If the player's starter relic is
        ///         <typeparamref name="TStarterRelic" />, the blessing replaces it with <typeparamref name="TUpgradedRelic" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册 <see cref="TouchOfOrobas" /> 精炼配对。若玩家的初始遗物是 <typeparamref name="TStarterRelic" />，该祝福会将其
        ///         替换为 <typeparamref name="TUpgradedRelic" />。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Uses <see cref="ModelDb.GetId{T}" /> for the starter key and stores the upgraded relic as a type for lazy
        ///         <see cref="ModelDb" /> resolution, so it is safe during content-pack <c>Apply()</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <see cref="ModelDb.GetId{T}" /> 作为初始遗物键，并将升级后的遗物保存为类型以供 <see cref="ModelDb" /> 延迟解析，
        ///         因此可在内容包 <c>Apply()</c> 期间安全调用。
        ///     </para>
        /// </remarks>
        /// <param name="registeringModId">
        ///     <para xml:lang="en">Optional mod ID included in log messages when a mapping is replaced.</para>
        ///     <para xml:lang="zh-CN">映射被替换时包含在日志消息中的可选模组 ID。</para>
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
        ///     <para xml:lang="en">
        ///         Registers a <see cref="TouchOfOrobas" /> refinement mapping from CLR types. The starter ID resolves lazily,
        ///         so registration can occur before content registration assigns RitsuLib's fixed public <see cref="ModelDb" /> entry.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 CLR 类型注册 <see cref="TouchOfOrobas" /> 精炼映射。初始遗物 ID 会延迟解析，因此可在内容注册为类型分配
        ///         RitsuLib 固定的公共 <see cref="ModelDb" /> 条目前进行注册。
        ///     </para>
        /// </summary>
        public static void RegisterTouchOfOrobasRefinementMapping(Type starterRelicType, Type upgradedRelicType,
            string? registeringModId = null)
        {
            OrobasAncientUpgradeRegistry.RegisterRefinement(starterRelicType, upgradedRelicType, registeringModId);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a <see cref="TouchOfOrobas" /> refinement mapping from an explicit starter ID and upgraded-relic type.</para>
        ///     <para xml:lang="zh-CN">使用显式初始遗物 ID 和升级后遗物类型注册 <see cref="TouchOfOrobas" /> 精炼映射。</para>
        /// </summary>
        /// <param name="starterRelicId">
        ///     <para xml:lang="en">Starter-relic instance ID to match.</para>
        ///     <para xml:lang="zh-CN">要匹配的初始遗物实例 ID。</para>
        /// </param>
        /// <param name="upgradedRelicType">
        ///     <para xml:lang="en">Concrete relic type, resolved through <see cref="ModelDb" /> when the blessing runs.</para>
        ///     <para xml:lang="zh-CN">具体遗物类型；祝福运行时通过 <see cref="ModelDb" /> 解析。</para>
        /// </param>
        /// <param name="registeringModId">
        ///     <para xml:lang="en">Optional mod ID included in log messages when a mapping is replaced.</para>
        ///     <para xml:lang="zh-CN">映射被替换时包含在日志消息中的可选模组 ID。</para>
        /// </param>
        public static void RegisterTouchOfOrobasRefinementMapping(ModelId starterRelicId, Type upgradedRelicType,
            string? registeringModId = null)
        {
            OrobasAncientUpgradeRegistry.RegisterRefinement(starterRelicId, upgradedRelicType, registeringModId);
        }
    }
}
