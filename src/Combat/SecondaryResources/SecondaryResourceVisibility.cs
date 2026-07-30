using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">Provides data for secondary-resource combat UI visibility.</para>
    ///     <para xml:lang="zh-CN">提供判断次级资源战斗界面可见性时使用的数据。</para>
    /// </summary>
    public readonly record struct SecondaryResourceCombatVisibilityContext(
        Player Player,
        SecondaryResourceDefinition Definition,
        int Amount,
        int? MaxAmount);

    /// <summary>
    ///     <para xml:lang="en">Provides data for secondary-resource card UI visibility.</para>
    ///     <para xml:lang="zh-CN">提供判断次级资源卡牌界面可见性时使用的数据。</para>
    /// </summary>
    public readonly record struct SecondaryResourceCardVisibilityContext(
        CardModel Card,
        SecondaryResourceDefinition Definition,
        SecondaryResourcePaymentLine? PaymentLine);

    /// <summary>
    ///     <para xml:lang="en">Determines whether a secondary resource should be visible in the combat UI.</para>
    ///     <para xml:lang="zh-CN">判断次级资源是否应显示在战斗界面中。</para>
    /// </summary>
    public delegate bool SecondaryResourceCombatUiVisibilityPredicate(
        SecondaryResourceCombatVisibilityContext context);

    /// <summary>
    ///     <para xml:lang="en">Resolves secondary-resource visibility in combat and card UI.</para>
    ///     <para xml:lang="zh-CN">解析次级资源在战斗界面和卡牌界面中的可见性。</para>
    /// </summary>
    public static class SecondaryResourceVisibility
    {
        private static readonly Lock PredicateFailureSync = new();
        private static readonly HashSet<SecondaryResourceCombatUiVisibilityPredicate> LoggedPredicateFailures = [];

        private static readonly AttachedState<PlayerCombatState, HashSet<string>> CombatUiSeenMaterialResources =
            new(() => new(StringComparer.OrdinalIgnoreCase));

        /// <summary>
        ///     <para xml:lang="en">Returns resource definitions visible for the current combat UI update.</para>
        ///     <para xml:lang="zh-CN">返回当前战斗界面更新中可见的资源定义。</para>
        /// </summary>
        public static IReadOnlyList<SecondaryResourceDefinition> GetCombatUiDefinitions(Player? player)
        {
            return GetCombatUiDefinitions(player, false);
        }

        internal static IReadOnlyList<SecondaryResourceDefinition> GetCombatUiDefinitions(
            Player? player,
            bool retainMaterialVisibility)
        {
            if (!ModSecondaryResourceRegistry.HasAny)
                return [];

            var definitions = ModSecondaryResourceRegistry.GetDefinitionsSnapshot();
            if (player == null)
                return [];

            return
            [
                .. definitions
                    .Where(definition => IsVisibleInCombatUi(definition, player, retainMaterialVisibility)),
            ];
        }

        internal static bool IsVisibleInCombatUi(SecondaryResourceDefinition definition, Player player)
        {
            return IsVisibleInCombatUi(definition, player, false);
        }

        private static bool IsVisibleInCombatUi(
            SecondaryResourceDefinition definition,
            Player player,
            bool retainMaterialVisibility)
        {
            ArgumentNullException.ThrowIfNull(definition);
            ArgumentNullException.ThrowIfNull(player);

            var context = new SecondaryResourceCombatVisibilityContext(
                player,
                definition,
                SecondaryResourceCmd.Get(player, definition.Id),
                SecondaryResourceCmd.GetMax(player, definition.Id));

            foreach (var predicate in ModSecondaryResourceRegistry.GetCombatUiVisibilityPredicates(definition.Id))
                try
                {
                    if (predicate(context))
                        return true;
                }
                catch (Exception ex)
                {
                    LogPredicateFailureOnce(definition.Id, predicate, ex);
                }

            // ReSharper disable once InvertIf
            if (context.Amount > definition.DefaultAmount)
            {
                if (retainMaterialVisibility &&
                    player.PlayerCombatState != null)
                    CombatUiSeenMaterialResources.GetOrCreate(player.PlayerCombatState).Add(definition.Id);

                return true;
            }

            return retainMaterialVisibility &&
                   player.PlayerCombatState != null &&
                   CombatUiSeenMaterialResources.GetOrCreate(player.PlayerCombatState).Contains(definition.Id);
        }

        private static void LogPredicateFailureOnce(
            string resourceId,
            SecondaryResourceCombatUiVisibilityPredicate predicate,
            Exception exception)
        {
            lock (PredicateFailureSync)
            {
                if (!LoggedPredicateFailures.Add(predicate))
                    return;
            }

            RitsuLibFramework.Logger.Warn(
                $"[SecondaryResource] Combat UI visibility predicate for '{resourceId}' failed: {exception}");
        }

        /// <summary>
        ///     <para xml:lang="en">Returns definitions with payment lines visible on <paramref name="card" />.</para>
        ///     <para xml:lang="zh-CN">返回在 <paramref name="card" /> 上有可见支付条目的资源定义。</para>
        /// </summary>
        public static IReadOnlyList<SecondaryResourceDefinition> GetCardUiDefinitions(
            CardModel card,
            SecondaryResourcePaymentPlan plan)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentNullException.ThrowIfNull(plan);

            if (!ModSecondaryResourceRegistry.HasAny)
                return [];

            var linesByResource = plan.Lines
                .GroupBy(static line => line.ResourceId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.First(),
                    StringComparer.OrdinalIgnoreCase);

            return
            [
                .. ModSecondaryResourceRegistry.GetDefinitionsSnapshot()
                    .Where(definition =>
                        definition.IsVisibleOnCard(
                            card,
                            linesByResource.GetValueOrDefault(definition.Id))),
            ];
        }
    }
}
