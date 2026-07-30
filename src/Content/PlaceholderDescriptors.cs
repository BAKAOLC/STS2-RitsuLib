using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">Configures the model properties of a generated placeholder card.</para>
    ///     <para xml:lang="zh-CN">配置生成的占位卡牌模型属性。</para>
    /// </summary>
    public readonly record struct PlaceholderCardDescriptor(
        int BaseCost = 1,
        CardType Type = CardType.Skill,
        CardRarity Rarity = CardRarity.Token,
        TargetType Target = TargetType.None,
        bool ShowInCardLibrary = false);

    /// <summary>
    ///     <para xml:lang="en">Configures the model properties of a generated placeholder relic.</para>
    ///     <para xml:lang="zh-CN">配置生成的占位遗物模型属性。</para>
    /// </summary>
    public readonly record struct PlaceholderRelicDescriptor(
        RelicRarity Rarity = RelicRarity.Common,
        bool IsUsedUp = false,
        bool HasUponPickupEffect = false,
        bool SpawnsPets = false,
        bool IsStackable = false,
        bool AddsPet = false,
        bool ShowCounter = false,
        int DisplayAmount = 0,
        bool IncludeEnergyHoverTip = false,
        int MerchantCostOverride = -1,
        bool AlwaysAllowedInRun = true,
        string FlashSfx = "event:/sfx/ui/relic_activate_general",
        bool ShouldFlashOnPlayer = true);

    /// <summary>
    ///     <para xml:lang="en">Configures the model properties of a generated placeholder potion.</para>
    ///     <para xml:lang="zh-CN">配置生成的占位药水模型属性。</para>
    /// </summary>
    public readonly record struct PlaceholderPotionDescriptor(
        PotionRarity Rarity = PotionRarity.Common,
        PotionUsage Usage = PotionUsage.AnyTime,
        TargetType Target = TargetType.None,
        bool CanBeGeneratedInCombat = true,
        bool PassesCustomUsabilityCheck = true);
}
