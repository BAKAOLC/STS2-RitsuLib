using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Combat UI visibility context for a secondary resource.
    ///     次级资源的战斗 UI 可见性上下文。
    /// </summary>
    public readonly record struct SecondaryResourceCombatVisibilityContext(
        Player Player,
        SecondaryResourceDefinition Definition,
        int Amount,
        int? MaxAmount);

    /// <summary>
    ///     Card UI visibility context for a secondary resource.
    ///     次级资源的卡牌 UI 可见性上下文。
    /// </summary>
    public readonly record struct SecondaryResourceCardVisibilityContext(
        CardModel Card,
        SecondaryResourceDefinition Definition,
        SecondaryResourcePaymentLine? PaymentLine);

    /// <summary>
    ///     Additional combat UI visibility predicate for a secondary resource.
    ///     次级资源的额外战斗 UI 可见性谓词。
    /// </summary>
    public delegate bool SecondaryResourceCombatUiVisibilityPredicate(
        SecondaryResourceCombatVisibilityContext context);

    /// <summary>
    ///     Visibility helpers for secondary-resource UI routing.
    ///     次级资源 UI 路由的可见性辅助工具。
    /// </summary>
    public static class SecondaryResourceVisibility
    {
        /// <summary>
        ///     Returns definitions visible for a combat UI update.
        ///     返回一次战斗 UI 更新中可见的资源定义。
        /// </summary>
        public static IReadOnlyList<SecondaryResourceDefinition> GetCombatUiDefinitions(Player? player)
        {
            if (!ModSecondaryResourceRegistry.HasAny)
                return [];

            var definitions = ModSecondaryResourceRegistry.GetDefinitionsSnapshot();
            if (player == null)
                return [];

            return definitions
                .Where(definition => definition.IsVisibleInCombatUi(player))
                .ToArray();
        }

        internal static bool IsVisibleInCombatUi(SecondaryResourceDefinition definition, Player player)
        {
            ArgumentNullException.ThrowIfNull(definition);
            ArgumentNullException.ThrowIfNull(player);

            var context = new SecondaryResourceCombatVisibilityContext(
                player,
                definition,
                SecondaryResourceCmd.Get(player, definition.Id),
                SecondaryResourceCmd.GetMax(player, definition.Id));

            if (ModSecondaryResourceRegistry.GetCombatUiVisibilityPredicates(definition.Id)
                .Any(predicate => predicate(context)))
                return true;

            return context.Amount > definition.DefaultAmount;
        }

        /// <summary>
        ///     Returns definitions visible for a card UI update.
        ///     返回一次卡牌 UI 更新中可见的资源定义。
        /// </summary>
        public static IReadOnlyList<SecondaryResourceDefinition> GetCardUiDefinitions(
            CardModel card,
            SecondaryResourcePaymentPlan plan)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentNullException.ThrowIfNull(plan);

            if (!ModSecondaryResourceRegistry.HasAny)
                return [];

            var linesByResource = plan.Lines.ToDictionary(
                static line => line.ResourceId,
                static line => line,
                StringComparer.OrdinalIgnoreCase);

            return ModSecondaryResourceRegistry.GetDefinitionsSnapshot()
                .Where(definition =>
                    definition.IsVisibleOnCard(
                        card,
                        linesByResource.GetValueOrDefault(definition.Id)))
                .ToArray();
        }
    }
}
