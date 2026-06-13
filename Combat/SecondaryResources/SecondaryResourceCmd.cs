#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Command API for mutating registered secondary-resource amounts.
    ///     用于修改已注册次级资源数量的命令 API。
    /// </summary>
    public static class SecondaryResourceCmd
    {
        /// <summary>
        ///     Reads the current amount without creating state when the feature is inactive.
        ///     读取当前数量；功能未启用时不会创建状态。
        /// </summary>
        public static int Get(Player player, string resourceId)
        {
            return SecondaryResourceStateStore.GetAmount(player, resourceId);
        }

        /// <summary>
        ///     Calculates the current max amount, or null for resources without a max concept.
        ///     计算当前最大数量；没有上限概念的资源返回 null。
        /// </summary>
        public static int? GetMax(Player player, string resourceId)
        {
            return SecondaryResourceStateStore.GetMaxAmount(player, resourceId);
        }

        /// <summary>
        ///     Gains a resource amount after gain hooks.
        ///     在 gain hook 处理后获得资源数量。
        /// </summary>
        public static async Task<int> Gain(
            Player player,
            string resourceId,
            int amount,
            AbstractModel? source = null)
        {
            if (amount <= 0 || !TryResolve(player, resourceId, out var combatState, out var definition))
                return Get(player, resourceId);

            var context = new SecondaryResourceContext(combatState, player, definition, source);
            if (!SecondaryResourceHook.ShouldGain(context, amount))
                return Get(player, resourceId);

            var modified = SecondaryResourceHook.ModifyGain(context, amount);
            var effective = Math.Max(0, (int)Math.Floor(modified));
            if (effective <= 0)
                return Get(player, resourceId);

            return await SetCore(
                combatState,
                player,
                definition,
                Get(player, definition.Id) + effective,
                SecondaryResourceChangeReason.Gain,
                source);
        }

        /// <summary>
        ///     Loses a resource amount.
        ///     失去指定资源数量。
        /// </summary>
        public static async Task<int> Lose(
            Player player,
            string resourceId,
            int amount,
            AbstractModel? source = null)
        {
            if (amount <= 0 || !TryResolve(player, resourceId, out var combatState, out var definition))
                return Get(player, resourceId);

            return await SetCore(
                combatState,
                player,
                definition,
                Get(player, definition.Id) - amount,
                SecondaryResourceChangeReason.Lose,
                source);
        }

        /// <summary>
        ///     Sets the current amount.
        ///     设置当前数量。
        /// </summary>
        public static async Task<int> Set(
            Player player,
            string resourceId,
            int amount,
            AbstractModel? source = null)
        {
            if (!TryResolve(player, resourceId, out var combatState, out var definition))
                return 0;

            return await SetCore(
                combatState,
                player,
                definition,
                amount,
                SecondaryResourceChangeReason.Set,
                source);
        }

        /// <summary>
        ///     Spends a resource amount after spend hooks.
        ///     在 spend hook 处理后消耗资源数量。
        /// </summary>
        public static async Task<bool> Spend(
            Player player,
            string resourceId,
            int amount,
            CardModel? card = null,
            AbstractModel? source = null)
        {
            if (amount <= 0)
                return true;

            if (!TryResolve(player, resourceId, out var combatState, out var definition))
                return false;

            if (Get(player, definition.Id) < amount)
                return false;

            var spendContext = new SecondaryResourceSpendContext(combatState, player, definition, card, amount, source);
            if (!SecondaryResourceHook.ShouldSpend(spendContext))
                return false;

            return await SpendCore(player, definition, combatState, amount, card, source);
        }

        internal static async Task<bool> SpendResolvedCardPayment(
            Player player,
            string resourceId,
            int amount,
            CardModel card,
            AbstractModel? source = null)
        {
            ArgumentNullException.ThrowIfNull(card);

            if (amount <= 0)
                return true;

            if (!TryResolve(player, resourceId, out var combatState, out var definition))
                return false;

            return await SpendCore(player, definition, combatState, amount, card, source);
        }

        private static async Task<bool> SpendCore(
            Player player,
            SecondaryResourceDefinition definition,
            CombatStateLike combatState,
            int amount,
            CardModel? card,
            AbstractModel? source)
        {
            if (Get(player, definition.Id) < amount)
                return false;

            var spendContext = new SecondaryResourceSpendContext(combatState, player, definition, card, amount, source);
            var oldAmount = Get(player, definition.Id);
            var newAmount = await SetCore(
                combatState,
                player,
                definition,
                oldAmount - amount,
                SecondaryResourceChangeReason.Spend,
                source);

            if (oldAmount == newAmount)
                return false;

            SecondaryResourceHistory.Spent(combatState, spendContext);
            await SecondaryResourceHook.AfterSpent(spendContext);
            return true;
        }

        /// <summary>
        ///     Resets a resource to its default or max amount.
        ///     将资源重置为默认数量或最大数量。
        /// </summary>
        public static async Task<int> Reset(
            Player player,
            string resourceId,
            bool toMax = false,
            AbstractModel? source = null)
        {
            if (!TryResolve(player, resourceId, out var combatState, out var definition))
                return 0;

            var context = new SecondaryResourceContext(combatState, player, definition, source);
            if (!SecondaryResourceHook.ShouldReset(context))
                return Get(player, definition.Id);

            var target = toMax && GetMax(player, definition.Id) is { } max
                ? max
                : definition.DefaultAmount;

            var oldAmount = Get(player, definition.Id);
            var newAmount = await SetCore(
                combatState,
                player,
                definition,
                target,
                SecondaryResourceChangeReason.Reset,
                source,
                true);

            if (oldAmount != newAmount)
                SecondaryResourceHistory.Reset(combatState,
                    new(combatState, player, definition, oldAmount, newAmount, newAmount - oldAmount,
                        SecondaryResourceChangeReason.Reset, source));

            return newAmount;
        }

        /// <summary>
        ///     Applies built-in start-of-turn policies to all registered resources.
        ///     对所有已注册资源应用内建回合开始策略。
        /// </summary>
        public static async Task ApplyTurnStartPolicies(Player player, AbstractModel? source = null)
        {
            ArgumentNullException.ThrowIfNull(player);

            if (!ModSecondaryResourceRegistry.HasAny)
                return;

            foreach (var definition in ModSecondaryResourceRegistry.GetDefinitionsSnapshot())
                await ApplyTurnStartPolicy(player, definition, source);
        }

        private static async Task ApplyTurnStartPolicy(
            Player player,
            SecondaryResourceDefinition definition,
            AbstractModel? source)
        {
            switch (definition.TurnStartPolicy)
            {
                case SecondaryResourceTurnStartPolicy.None:
                    return;
                case SecondaryResourceTurnStartPolicy.ResetToMax:
                    await Reset(player, definition.Id, true, source);
                    return;
                case SecondaryResourceTurnStartPolicy.AddMaxToCurrent:
                    if (GetMax(player, definition.Id) is { } max)
                        await Gain(player, definition.Id, max, source);
                    return;
                case SecondaryResourceTurnStartPolicy.Clear:
                    await Set(player, definition.Id, definition.MinAmount, source);
                    return;
                default:
#pragma warning disable CA2208
                    throw new ArgumentOutOfRangeException(nameof(definition.TurnStartPolicy));
#pragma warning restore CA2208
            }
        }

        private static async Task<int> SetCore(
            CombatStateLike combatState,
            Player player,
            SecondaryResourceDefinition definition,
            int amount,
            SecondaryResourceChangeReason reason,
            AbstractModel? source,
            bool afterReset = false)
        {
            var state = SecondaryResourceStateStore.Get(player);
            var oldAmount = state.Get(definition.Id);
            var newAmount = state.Set(player, definition, amount, reason, source);
            if (oldAmount == newAmount)
                return newAmount;

            var context = new SecondaryResourceChangeContext(
                combatState,
                player,
                definition,
                oldAmount,
                newAmount,
                newAmount - oldAmount,
                reason,
                source);

            SecondaryResourceHistory.Changed(combatState, context);
            SecondaryResourceUiRuntime.UpdateCurrentCombatUi(player);
            await SecondaryResourceHook.AfterChanged(context);
            if (afterReset)
                await SecondaryResourceHook.AfterReset(context);

            return newAmount;
        }

        private static bool TryResolve(
            Player player,
            string resourceId,
            out CombatStateLike combatState,
            out SecondaryResourceDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(player);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);

            combatState = null!;
            definition = null!;

            if (!ModSecondaryResourceRegistry.HasAny ||
                !ModSecondaryResourceRegistry.TryGet(resourceId, out definition))
                return false;

            combatState = player.Creature?.CombatState ?? null!;
            return combatState != null;
        }
    }
}
