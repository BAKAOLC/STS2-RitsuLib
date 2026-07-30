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
    ///     <para xml:lang="en">Provides commands that mutate registered secondary-resource amounts.</para>
    ///     <para xml:lang="zh-CN">提供修改已注册次级资源数量的命令。</para>
    /// </summary>
    public static class SecondaryResourceCmd
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the current amount without creating state when no resources are registered.</para>
        ///     <para xml:lang="zh-CN">获取当前数量；未注册资源时不会创建状态。</para>
        /// </summary>
        public static int Get(Player player, string resourceId)
        {
            return SecondaryResourceStateStore.GetAmount(player, resourceId);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the current maximum, or <see langword="null" /> for an uncapped resource.</para>
        ///     <para xml:lang="zh-CN">获取当前最大数量；资源没有上限时返回 <see langword="null" />。</para>
        /// </summary>
        public static int? GetMax(Player player, string resourceId)
        {
            return SecondaryResourceStateStore.GetMaxAmount(player, resourceId);
        }

        /// <summary>
        ///     <para xml:lang="en">Adds a positive amount after applying gain checks and modifiers.</para>
        ///     <para xml:lang="zh-CN">通过增加检查并应用修正后，增加一个正数数量。</para>
        /// </summary>
        public static async Task<int> Gain(
            Player player,
            string resourceId,
            int amount,
            AbstractModel? source = null)
        {
            if (amount <= 0 || !TryResolve(player, resourceId, out var combatState, out var definition))
                return Get(player, resourceId);

            return await GainCore(
                combatState,
                player,
                definition,
                amount,
                SecondaryResourceChangeReason.Gain,
                source);
        }

        private static async Task<int> GainCore(
            CombatStateLike combatState,
            Player player,
            SecondaryResourceDefinition definition,
            int amount,
            SecondaryResourceChangeReason reason,
            AbstractModel? source)
        {
            var context = new SecondaryResourceContext(combatState, player, definition, source);
            if (!SecondaryResourceHook.ShouldGain(context, amount))
                return Get(player, definition.Id);

            var modified = SecondaryResourceHook.ModifyGain(context, amount);
            var effective = SecondaryResourceAmountMath.FloorAndClamp(modified, 0, int.MaxValue);
            if (effective <= 0)
                return Get(player, definition.Id);

            return await SetCore(
                combatState,
                player,
                definition,
                SecondaryResourceAmountMath.AddSaturating(Get(player, definition.Id), effective),
                reason,
                source);
        }

        /// <summary>
        ///     <para xml:lang="en">Subtracts a positive amount from a resource.</para>
        ///     <para xml:lang="zh-CN">从资源中减去一个正数数量。</para>
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
                SecondaryResourceAmountMath.SubtractSaturating(Get(player, definition.Id), amount),
                SecondaryResourceChangeReason.Lose,
                source);
        }

        /// <summary>
        ///     <para xml:lang="en">Sets the current amount after applying the resource bounds.</para>
        ///     <para xml:lang="zh-CN">应用资源上下限后设置当前数量。</para>
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
        ///     <para xml:lang="en">Pays a positive amount after applying spend checks.</para>
        ///     <para xml:lang="zh-CN">通过支付检查后支付一个正数数量。</para>
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
        ///     <para xml:lang="en">Resets a resource to its default amount or current maximum.</para>
        ///     <para xml:lang="zh-CN">将资源重置为默认数量或当前最大数量。</para>
        /// </summary>
        public static async Task<int> Reset(
            Player player,
            string resourceId,
            bool toMax = false,
            AbstractModel? source = null)
        {
            if (!TryResolve(player, resourceId, out var combatState, out var definition))
                return 0;

            return await ResetCore(
                combatState,
                player,
                definition,
                toMax,
                SecondaryResourceChangeReason.Reset,
                source);
        }

        private static async Task<int> ResetCore(
            CombatStateLike combatState,
            Player player,
            SecondaryResourceDefinition definition,
            bool toMax,
            SecondaryResourceChangeReason reason,
            AbstractModel? source)
        {
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
                reason,
                source,
                true);

            if (oldAmount != newAmount)
                SecondaryResourceHistory.Reset(combatState,
                    new(combatState, player, definition, oldAmount, newAmount,
                        SecondaryResourceAmountMath.SubtractSaturating(newAmount, oldAmount),
                        reason, source));

            return newAmount;
        }

        /// <summary>
        ///     <para xml:lang="en">Applies each registered resource's built-in turn-start policy.</para>
        ///     <para xml:lang="zh-CN">应用各已注册资源的内置回合开始策略。</para>
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
                    if (TryResolve(player, definition.Id, out var resetCombatState, out _))
                        await ResetCore(
                            resetCombatState,
                            player,
                            definition,
                            true,
                            SecondaryResourceChangeReason.TurnStart,
                            source);
                    return;
                case SecondaryResourceTurnStartPolicy.AddMaxToCurrent:
                    if (GetMax(player, definition.Id) is { } max && max > 0 &&
                        TryResolve(player, definition.Id, out var gainCombatState, out _))
                        await GainCore(
                            gainCombatState,
                            player,
                            definition,
                            max,
                            SecondaryResourceChangeReason.TurnStart,
                            source);
                    return;
                case SecondaryResourceTurnStartPolicy.Clear:
                    if (TryResolve(player, definition.Id, out var clearCombatState, out _))
                        await SetCore(
                            clearCombatState,
                            player,
                            definition,
                            definition.MinAmount,
                            SecondaryResourceChangeReason.TurnStart,
                            source);
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
                SecondaryResourceAmountMath.SubtractSaturating(newAmount, oldAmount),
                reason,
                source);

            SecondaryResourceHistory.Changed(combatState, context);
            SecondaryResourceUiRuntime.UpdateCurrentCombatUi(player);
            SecondaryResourceUiRuntime.NotifyCurrentCombatUiChanged(context);
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
