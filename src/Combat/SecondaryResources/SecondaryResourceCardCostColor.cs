using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Color state for a secondary-resource card cost.
    ///     次级资源卡牌费用的颜色状态。
    /// </summary>
    public enum SecondaryResourceCardCostColor
    {
        /// <summary>
        ///     Use the default cost color.
        ///     使用默认费用颜色。
        /// </summary>
        Unmodified,

        /// <summary>
        ///     Cost is higher than the current base cost.
        ///     费用高于当前基础费用。
        /// </summary>
        Increased,

        /// <summary>
        ///     Cost is lower than the current base cost, or upgrade preview lowered the base cost.
        ///     费用低于当前基础费用，或升级预览降低了基础费用。
        /// </summary>
        Decreased,

        /// <summary>
        ///     A required cost cannot be paid.
        ///     必需费用无法支付。
        /// </summary>
        InsufficientResources,

        /// <summary>
        ///     A required cost is short on resource, but its policy still allows the card to be played.
        ///     必需费用资源不足，但其策略仍允许卡牌打出。
        /// </summary>
        ShortfallPlayable,

        /// <summary>
        ///     An optional spend is unavailable but does not block card play.
        ///     可选支付不可用，但不阻止卡牌打出。
        /// </summary>
        OptionalUnavailable,
    }

    /// <summary>
    ///     Mirrors the game's card cost color rules for secondary-resource card UI.
    ///     为次级资源卡牌 UI 对齐游戏原版费用颜色规则。
    /// </summary>
    public static class SecondaryResourceCardCostHelper
    {
        /// <summary>
        ///     Gets the color state for a resolved secondary-resource payment line.
        ///     获取已解析次级资源支付行的颜色状态。
        /// </summary>
        public static SecondaryResourceCardCostColor GetCostColor(
            SecondaryResourcePaymentLine line,
            PileType pileType,
            CardPreviewMode previewMode,
            bool pretendCardCanBePlayed = false,
            bool includeOptionalUnavailable = true)
        {
            ArgumentNullException.ThrowIfNull(line);

            if (line.CostsX)
                return SecondaryResourceCardCostColor.Unmodified;

            if (previewMode == CardPreviewMode.Upgrade && line.BaseCost < line.CanonicalCost)
                return SecondaryResourceCardCostColor.Decreased;

            if (pileType != PileType.Hand)
                return SecondaryResourceCardCostColor.Unmodified;

            if (line is { CanPlay: false, BlocksPlay: true })
                return pretendCardCanBePlayed
                    ? SecondaryResourceCardCostColor.Unmodified
                    : SecondaryResourceCardCostColor.InsufficientResources;

            if (line.IsShortfallPlayable)
                return SecondaryResourceCardCostColor.ShortfallPlayable;

            if (includeOptionalUnavailable && line is { IsOptional: true, Activated: false })
                return SecondaryResourceCardCostColor.OptionalUnavailable;

            if (line.Cost > line.BaseCost)
                return SecondaryResourceCardCostColor.Increased;

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (line.Cost < line.BaseCost)
                return SecondaryResourceCardCostColor.Decreased;

            return SecondaryResourceCardCostColor.Unmodified;
        }
    }
}
