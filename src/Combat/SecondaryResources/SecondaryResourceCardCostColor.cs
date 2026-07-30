using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">Specifies the display color state of a card's secondary-resource cost.</para>
    ///     <para xml:lang="zh-CN">指定卡牌次级资源费用的显示颜色状态。</para>
    /// </summary>
    public enum SecondaryResourceCardCostColor
    {
        /// <summary>
        ///     <para xml:lang="en">Uses the default cost color.</para>
        ///     <para xml:lang="zh-CN">使用默认的费用颜色。</para>
        /// </summary>
        Unmodified,

        /// <summary>
        ///     <para xml:lang="en">Indicates that the cost is higher than its current base value.</para>
        ///     <para xml:lang="zh-CN">表示费用高于当前基础值。</para>
        /// </summary>
        Increased,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Indicates that the cost is below its current base value, or that the upgrade preview lowers the
        ///         base value.
        ///     </para>
        ///     <para xml:lang="zh-CN">表示费用低于当前基础值，或升级预览降低了基础值。</para>
        /// </summary>
        Decreased,

        /// <summary>
        ///     <para xml:lang="en">Indicates that a required cost cannot be paid.</para>
        ///     <para xml:lang="zh-CN">表示一项必需费用无法支付。</para>
        /// </summary>
        InsufficientResources,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Indicates that a required cost has a shortfall, but its payment policy still permits the card to
        ///         be played.
        ///     </para>
        ///     <para xml:lang="zh-CN">表示一项必需费用存在缺口，但其支付策略仍允许打出卡牌。</para>
        /// </summary>
        ShortfallPlayable,

        /// <summary>
        ///     <para xml:lang="en">Indicates that an optional payment is unavailable without blocking card play.</para>
        ///     <para xml:lang="zh-CN">表示一项可选支付不可用，但不会阻止打出卡牌。</para>
        /// </summary>
        OptionalUnavailable,
    }

    /// <summary>
    ///     <para xml:lang="en">Applies the game's card-cost color rules to secondary-resource costs.</para>
    ///     <para xml:lang="zh-CN">将游戏的卡牌费用颜色规则应用于次级资源费用。</para>
    /// </summary>
    public static class SecondaryResourceCardCostHelper
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the display color state for a resolved payment entry.</para>
        ///     <para xml:lang="zh-CN">获取已解析支付条目的显示颜色状态。</para>
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

            if (previewMode == CardPreviewMode.Upgrade &&
                line.UpgradePreviewBaseCost is { } upgradePreviewBaseCost &&
                line.BaseCost < upgradePreviewBaseCost)
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

            if (!line.HasRuntimeCostModifier)
                return SecondaryResourceCardCostColor.Unmodified;

            if (line.Cost > line.BaseCost)
                return SecondaryResourceCardCostColor.Increased;

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (line.Cost < line.BaseCost)
                return SecondaryResourceCardCostColor.Decreased;

            return SecondaryResourceCardCostColor.Unmodified;
        }
    }
}
