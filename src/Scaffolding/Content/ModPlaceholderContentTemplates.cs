using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the no-op base implementation used by generated placeholder cards. Mods normally register
    ///         placeholders through
    ///         <see cref="ModContentRegistry.RegisterPlaceholderCard{TPool}(string, PlaceholderCardDescriptor)" />
    ///         instead of subclassing this type.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供生成式占位卡牌使用的无操作基础实现。模组通常应通过
    ///         <see cref="ModContentRegistry.RegisterPlaceholderCard{TPool}(string, PlaceholderCardDescriptor)" />
    ///         注册占位卡牌，而不是继承此类型。
    ///     </para>
    /// </summary>
    public abstract class ModPlaceholderCardTemplate(
        int baseCost,
        CardType type,
        CardRarity rarity,
        TargetType target,
        bool showInCardLibrary = false)
        : ModCardTemplate(baseCost, type, rarity, target, showInCardLibrary)
    {
        /// <summary>
        ///     <para xml:lang="en">Completes immediately without applying a card effect.</para>
        ///     <para xml:lang="zh-CN">立即完成，不产生卡牌效果。</para>
        /// </summary>
        protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the configurable base implementation used by generated placeholder relics. Prefer
    ///         <see cref="ModContentRegistry.RegisterPlaceholderRelic{TPool}(string, PlaceholderRelicDescriptor)" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供生成式占位遗物使用的可配置基础实现。请优先使用
    ///         <see cref="ModContentRegistry.RegisterPlaceholderRelic{TPool}(string, PlaceholderRelicDescriptor)" />。
    ///     </para>
    /// </summary>
    public abstract class ModPlaceholderRelicTemplate(
        RelicRarity rarity,
        bool isUsedUp = false,
        bool hasUponPickupEffect = false,
        bool spawnsPets = false,
        bool isStackable = false,
        bool addsPet = false,
        bool showCounter = false,
        int displayAmount = 0,
        bool includeEnergyHoverTip = false,
        int merchantCostOverride = -1,
        bool alwaysAllowedInRun = true,
        string flashSfx = "event:/sfx/ui/relic_activate_general",
        bool shouldFlashOnPlayer = true)
        : ModRelicTemplate
    {
        /// <inheritdoc />
        public override RelicRarity Rarity => rarity;

        /// <inheritdoc />
        public override bool IsUsedUp => isUsedUp;

        /// <inheritdoc />
        public override bool HasUponPickupEffect => hasUponPickupEffect;

        /// <inheritdoc />
        public override bool SpawnsPets => spawnsPets;

        /// <inheritdoc />
        public override bool IsStackable => isStackable;

        /// <inheritdoc />
        public override bool AddsPet => addsPet;

        /// <inheritdoc />
        public override bool ShowCounter => showCounter;

        /// <inheritdoc />
        public override int DisplayAmount => displayAmount;

        /// <inheritdoc />
        protected override bool IncludeEnergyHoverTip => includeEnergyHoverTip;

        /// <inheritdoc />
        public override int MerchantCost => merchantCostOverride >= 0 ? merchantCostOverride : base.MerchantCost;

        /// <inheritdoc />
        public override string FlashSfx => flashSfx;

        /// <inheritdoc />
        public override bool ShouldFlashOnPlayer => shouldFlashOnPlayer;

        /// <inheritdoc />
        public override bool IsAllowed(IRunState runState)
        {
            return alwaysAllowedInRun;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the configurable no-op base implementation used by generated placeholder potions. Prefer
    ///         <see cref="ModContentRegistry.RegisterPlaceholderPotion{TPool}(string, PlaceholderPotionDescriptor)" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供生成式占位药水使用的可配置无操作基础实现。请优先使用
    ///         <see cref="ModContentRegistry.RegisterPlaceholderPotion{TPool}(string, PlaceholderPotionDescriptor)" />。
    ///     </para>
    /// </summary>
    public abstract class ModPlaceholderPotionTemplate(
        PotionRarity rarity,
        PotionUsage usage,
        TargetType targetType,
        bool canBeGeneratedInCombat = true,
        bool passesCustomUsabilityCheck = true)
        : ModPotionTemplate
    {
        /// <inheritdoc />
        public override PotionRarity Rarity => rarity;

        /// <inheritdoc />
        public override PotionUsage Usage => usage;

        /// <inheritdoc />
        public override TargetType TargetType => targetType;

        /// <inheritdoc />
        public override bool CanBeGeneratedInCombat => canBeGeneratedInCombat;

        /// <inheritdoc />
        public override bool PassesCustomUsabilityCheck => passesCustomUsabilityCheck;

        /// <summary>
        ///     <para xml:lang="en">Completes immediately without applying a potion effect.</para>
        ///     <para xml:lang="zh-CN">立即完成，不产生药水效果。</para>
        /// </summary>
        protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
        {
            return Task.CompletedTask;
        }
    }
}
