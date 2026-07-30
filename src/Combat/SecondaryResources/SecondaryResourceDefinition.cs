using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">Defines a registered secondary combat resource.</para>
    ///     <para xml:lang="zh-CN">定义已注册的次级战斗资源。</para>
    /// </summary>
    public sealed record SecondaryResourceDefinition
    {
        /// <summary>
        ///     <para xml:lang="en">The base-game localization table used by default for resource hover tips.</para>
        ///     <para xml:lang="zh-CN">资源悬浮提示默认使用的原版本地化表。</para>
        /// </summary>
        public const string DefaultLocTable = "static_hover_tips";

        /// <summary>
        ///     <para xml:lang="en">Initializes a secondary-resource definition.</para>
        ///     <para xml:lang="zh-CN">初始化次级资源定义。</para>
        /// </summary>
        public SecondaryResourceDefinition(
            int defaultAmount = 0,
            int? baseMaxAmount = null,
            int minAmount = 0,
            int hardMaxAmount = 999_999_999,
            SecondaryResourceTurnStartPolicy turnStartPolicy = SecondaryResourceTurnStartPolicy.None,
            SecondaryResourcePersistencePolicy persistencePolicy = SecondaryResourcePersistencePolicy.None,
            string? locTable = null,
            string? titleKey = null,
            string? descriptionKey = null,
            string? smallIconPath = null,
            string? largeIconPath = null)
        {
            DefaultAmount = defaultAmount;
            BaseMaxAmount = baseMaxAmount;
            MinAmount = minAmount;
            HardMaxAmount = hardMaxAmount;
            TurnStartPolicy = turnStartPolicy;
            PersistencePolicy = persistencePolicy;
            LocTable = locTable;
            TitleKey = titleKey;
            DescriptionKey = descriptionKey;
            SmallIconPath = smallIconPath;
            LargeIconPath = largeIconPath;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the full resource ID assigned during registration.</para>
        ///     <para xml:lang="zh-CN">获取注册时分配的完整资源 ID。</para>
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">Gets the owning mod ID assigned during registration.</para>
        ///     <para xml:lang="zh-CN">获取注册时分配的所属模组 ID。</para>
        /// </summary>
        public string ModId { get; init; } = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">Gets the mod-local resource ID assigned during registration.</para>
        ///     <para xml:lang="zh-CN">获取注册时分配的模组内资源 ID。</para>
        /// </summary>
        public string LocalId { get; init; } = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">Gets the amount used before an explicit value is stored.</para>
        ///     <para xml:lang="zh-CN">获取显式存储数值前使用的默认数量。</para>
        /// </summary>
        public int DefaultAmount { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the maximum before hook modifiers, or <see langword="null" /> when the resource has no maximum.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取钩子修正前的最大数量；资源没有最大数量时为 <see langword="null" />。
        ///     </para>
        /// </summary>
        public int? BaseMaxAmount { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the hard lower bound for the current amount.</para>
        ///     <para xml:lang="zh-CN">获取当前数量的硬下限。</para>
        /// </summary>
        public int MinAmount { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the hard upper bound for the current amount.</para>
        ///     <para xml:lang="zh-CN">获取当前数量的硬上限。</para>
        /// </summary>
        public int HardMaxAmount { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the built-in turn-start behavior.</para>
        ///     <para xml:lang="zh-CN">获取内置的回合开始行为。</para>
        /// </summary>
        public SecondaryResourceTurnStartPolicy TurnStartPolicy { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the persistence scope in run saves.</para>
        ///     <para xml:lang="zh-CN">获取在跑局存档中的持久化范围。</para>
        /// </summary>
        public SecondaryResourcePersistencePolicy PersistencePolicy { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the default policy for an underfunded required card payment.</para>
        ///     <para xml:lang="zh-CN">获取卡牌必需支付资源不足时的默认策略。</para>
        /// </summary>
        public SecondaryResourceInsufficientPayment DefaultInsufficientPayment { get; init; } =
            SecondaryResourceInsufficientPayment.BlockPlay;

        /// <summary>
        ///     <para xml:lang="en">Gets the optional localization table for the title and description.</para>
        ///     <para xml:lang="zh-CN">获取标题和说明使用的可选本地化表。</para>
        /// </summary>
        public string? LocTable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional localization key for the display title.</para>
        ///     <para xml:lang="zh-CN">获取显示标题的可选本地化键。</para>
        /// </summary>
        public string? TitleKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional localization key for the hover-tip description.</para>
        ///     <para xml:lang="zh-CN">获取悬浮提示说明的可选本地化键。</para>
        /// </summary>
        public string? DescriptionKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the effective localization table with surrounding whitespace removed.</para>
        ///     <para xml:lang="zh-CN">获取移除首尾空白后实际使用的本地化表。</para>
        /// </summary>
        public string EffectiveLocTable => string.IsNullOrWhiteSpace(LocTable) ? DefaultLocTable : LocTable.Trim();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the effective display-title localization key with surrounding whitespace removed.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取移除首尾空白后实际使用的显示标题本地化键。</para>
        /// </summary>
        public string EffectiveTitleKey =>
            string.IsNullOrWhiteSpace(TitleKey) ? $"{Id}.title" : TitleKey.Trim();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the effective hover-tip description localization key with surrounding whitespace removed.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取移除首尾空白后实际使用的悬浮提示说明本地化键。</para>
        /// </summary>
        public string EffectiveDescriptionKey =>
            string.IsNullOrWhiteSpace(DescriptionKey) ? $"{Id}.description" : DescriptionKey.Trim();

        /// <summary>
        ///     <para xml:lang="en">Gets the optional small icon path used in text and card UI.</para>
        ///     <para xml:lang="zh-CN">获取文本和卡牌界面使用的可选小图标路径。</para>
        /// </summary>
        public string? SmallIconPath { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional large icon path used in combat UI.</para>
        ///     <para xml:lang="zh-CN">获取战斗界面使用的可选大图标路径。</para>
        /// </summary>
        public string? LargeIconPath { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Determines whether this resource is visible in combat UI for <paramref name="player" />.</para>
        ///     <para xml:lang="zh-CN">判断该资源是否在 <paramref name="player" /> 的战斗界面中可见。</para>
        /// </summary>
        public bool IsVisibleInCombatUi(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);

            return SecondaryResourceVisibility.IsVisibleInCombatUi(this, player);
        }

        /// <summary>
        ///     <para xml:lang="en">Determines whether this resource has a payment line to show on <paramref name="card" />.</para>
        ///     <para xml:lang="zh-CN">判断该资源是否有需要在 <paramref name="card" /> 上显示的支付条目。</para>
        /// </summary>
        public bool IsVisibleOnCard(CardModel card, SecondaryResourcePaymentLine? paymentLine = null)
        {
            ArgumentNullException.ThrowIfNull(card);
            return paymentLine != null;
        }

        internal bool IsVisibleInCombatUiWithoutPlayer()
        {
            return false;
        }

        internal SecondaryResourceDefinition Bind(string modId, string localId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localId);

            var normalizedModId = modId.Trim();
            var normalizedLocalId = localId.Trim();
            var id = ModSecondaryResourceRegistry.GetResourceId(normalizedModId, normalizedLocalId);
            Validate(id);
            return this with
            {
                Id = id,
                ModId = normalizedModId,
                LocalId = normalizedLocalId,
            };
        }

        private void Validate(string id)
        {
            if (HardMaxAmount < MinAmount)
                throw new InvalidOperationException(
                    $"Secondary resource '{id}' has HardMaxAmount below MinAmount.");

            if (BaseMaxAmount is < 0)
                throw new InvalidOperationException(
                    $"Secondary resource '{id}' cannot have a negative base max amount.");
        }
    }
}
