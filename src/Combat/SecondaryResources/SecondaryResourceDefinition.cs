using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Immutable definition for a registered secondary combat resource.
    ///     已注册次级战斗资源的不可变定义。
    /// </summary>
    public sealed record SecondaryResourceDefinition
    {
        /// <summary>
        ///     Default vanilla loc table used for secondary-resource hover tips.
        ///     次级资源悬浮提示默认使用的原版本地化表。
        /// </summary>
        public const string DefaultLocTable = "static_hover_tips";

        /// <summary>
        ///     Creates a secondary resource definition.
        ///     创建一个次级资源定义。
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
        ///     Full RitsuLib compound id. Filled by <see cref="ModSecondaryResourceRegistry" />.
        ///     完整的 RitsuLib compound id，由 <see cref="ModSecondaryResourceRegistry" /> 填充。
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        ///     Owning mod id. Filled by <see cref="ModSecondaryResourceRegistry" />.
        ///     所属 mod id，由 <see cref="ModSecondaryResourceRegistry" /> 填充。
        /// </summary>
        public string ModId { get; init; } = string.Empty;

        /// <summary>
        ///     Mod-local resource id stem before compound-id expansion.
        ///     compound id 展开前的 mod 本地资源 id stem。
        /// </summary>
        public string LocalId { get; init; } = string.Empty;

        /// <summary>
        ///     Initial amount used when state is first created.
        ///     状态首次创建时使用的初始数量。
        /// </summary>
        public int DefaultAmount { get; init; }

        /// <summary>
        ///     Base max amount before secondary-resource max hooks. Null means the resource has no max concept.
        ///     次级资源 max hook 前的基础上限；null 表示该资源没有上限概念。
        /// </summary>
        public int? BaseMaxAmount { get; init; }

        /// <summary>
        ///     Hard lower clamp for current amount.
        ///     当前数量的硬下限。
        /// </summary>
        public int MinAmount { get; init; }

        /// <summary>
        ///     Hard upper clamp for current amount.
        ///     当前数量的硬上限。
        /// </summary>
        public int HardMaxAmount { get; init; }

        /// <summary>
        ///     Built-in turn-start behavior.
        ///     内建的回合开始行为。
        /// </summary>
        public SecondaryResourceTurnStartPolicy TurnStartPolicy { get; init; }

        /// <summary>
        ///     Run-save persistence scope.
        ///     跑局存档持久化范围。
        /// </summary>
        public SecondaryResourcePersistencePolicy PersistencePolicy { get; init; }

        /// <summary>
        ///     Default policy for required card payments that are short on this resource.
        ///     此资源作为卡牌必需支付且数量不足时的默认策略。
        /// </summary>
        public SecondaryResourceInsufficientPayment DefaultInsufficientPayment { get; init; } =
            SecondaryResourceInsufficientPayment.BlockPlay;

        /// <summary>
        ///     Optional localization table for title and description.
        ///     用于标题和描述的可选本地化表。
        /// </summary>
        public string? LocTable { get; init; }

        /// <summary>
        ///     Optional localization key for the display title.
        ///     显示标题的可选本地化 key。
        /// </summary>
        public string? TitleKey { get; init; }

        /// <summary>
        ///     Optional localization key for the hover/description text.
        ///     悬浮提示/描述文本的可选本地化 key。
        /// </summary>
        public string? DescriptionKey { get; init; }

        /// <summary>
        ///     Effective localization table for this resource.
        ///     此资源实际使用的本地化表。
        /// </summary>
        public string EffectiveLocTable => string.IsNullOrWhiteSpace(LocTable) ? DefaultLocTable : LocTable;

        /// <summary>
        ///     Effective localization key for the display title.
        ///     显示标题实际使用的本地化 key。
        /// </summary>
        public string EffectiveTitleKey =>
            string.IsNullOrWhiteSpace(TitleKey) ? $"{Id}.title" : TitleKey;

        /// <summary>
        ///     Effective localization key for the hover/description text.
        ///     悬浮提示/描述文本实际使用的本地化 key。
        /// </summary>
        public string EffectiveDescriptionKey =>
            string.IsNullOrWhiteSpace(DescriptionKey) ? $"{Id}.description" : DescriptionKey;

        /// <summary>
        ///     Optional small icon path for text/card displays.
        ///     用于文本/卡牌显示的可选小图标路径。
        /// </summary>
        public string? SmallIconPath { get; init; }

        /// <summary>
        ///     Optional large icon path for combat UI displays.
        ///     用于战斗 UI 显示的可选大图标路径。
        /// </summary>
        public string? LargeIconPath { get; init; }

        /// <summary>
        ///     Returns whether this resource is visible in combat UI for <paramref name="player" />.
        ///     返回该资源对 <paramref name="player" /> 的战斗 UI 是否可见。
        /// </summary>
        public bool IsVisibleInCombatUi(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);

            return SecondaryResourceVisibility.IsVisibleInCombatUi(this, player);
        }

        /// <summary>
        ///     Returns whether this resource is visible in card UI for <paramref name="card" />.
        ///     返回该资源在 <paramref name="card" /> 的卡牌 UI 上是否可见。
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

            var normalizedLocalId = localId.Trim();
            var id = ModSecondaryResourceRegistry.GetResourceId(modId, normalizedLocalId);
            Validate(id);
            return this with
            {
                Id = id,
                ModId = modId,
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
