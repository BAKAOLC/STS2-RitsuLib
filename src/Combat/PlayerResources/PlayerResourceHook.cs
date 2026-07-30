#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Models.Capabilities;

namespace STS2RitsuLib.Combat.PlayerResources
{
    /// <summary>
    ///     <para xml:lang="en">Dispatches built-in player-resource hooks to models, capabilities, and global listeners.</para>
    ///     <para xml:lang="zh-CN">将游戏内置玩家资源钩子分发给模型、模型能力和全局监听器。</para>
    /// </summary>
    public static class PlayerResourceHook
    {
        private static readonly ModelHookListenerRegistry<IPlayerResourceHookListener> GlobalListeners = new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a process-wide listener. Effects owned by a model should normally implement
        ///         <see cref="IPlayerResourceHookListener" /> directly.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册进程级监听器。由模型持有的效果通常应直接实现 <see cref="IPlayerResourceHookListener" />。
        ///     </para>
        /// </summary>
        public static void RegisterGlobalListener(IPlayerResourceHookListener listener)
        {
            GlobalListeners.Register(listener);
        }

        /// <summary>
        ///     <para xml:lang="en">Runs after-energy-gained hooks.</para>
        ///     <para xml:lang="zh-CN">运行获得能量后的钩子。</para>
        /// </summary>
        public static async Task AfterEnergyGained(PlayerResourceGainContext context)
        {
            foreach (var entry in IterateListeners(context.CombatState))
                await Invoke(entry, context, static (listener, ctx) => listener.AfterPlayerEnergyGained(ctx));
        }

        /// <summary>
        ///     <para xml:lang="en">Runs after-stars-gained hooks.</para>
        ///     <para xml:lang="zh-CN">运行获得辉星后的钩子。</para>
        /// </summary>
        public static async Task AfterStarsGained(PlayerResourceGainContext context)
        {
            foreach (var entry in IterateListeners(context.CombatState))
                await Invoke(entry, context, static (listener, ctx) => listener.AfterPlayerStarsGained(ctx));
        }

        internal static async Task AfterEnergyGainedIfChanged(Player player, int oldAmount)
        {
            if (!TryCreateGainContext(player, PlayerResourceKind.Energy, oldAmount, out var context))
                return;

            await AfterEnergyGained(context);
        }

        internal static async Task AfterStarsGainedIfChanged(Player player, int oldAmount)
        {
            if (!TryCreateGainContext(player, PlayerResourceKind.Stars, oldAmount, out var context))
                return;

            await AfterStarsGained(context);
        }

        private static bool TryCreateGainContext(
            Player player,
            PlayerResourceKind resource,
            int oldAmount,
            out PlayerResourceGainContext context)
        {
            context = default;

            if (player.PlayerCombatState == null || player.Creature?.CombatState is not { } combatState)
                return false;

            var newAmount = resource switch
            {
                PlayerResourceKind.Energy => player.PlayerCombatState.Energy,
                PlayerResourceKind.Stars => player.PlayerCombatState.Stars,
                _ => oldAmount,
            };

            var amount = newAmount - oldAmount;
            if (amount <= 0)
                return false;

            context = new(combatState, player, resource, amount, oldAmount, newAmount);
            return true;
        }

        private static IEnumerable<ModelHookListener<IPlayerResourceHookListener>> IterateListeners(
            CombatStateLike combatState)
        {
            return ModelHookListenerDispatcher.FromCombat(combatState, GlobalListeners);
        }

        private static async Task Invoke(
            ModelHookListener<IPlayerResourceHookListener> entry,
            PlayerResourceGainContext context,
            Func<IPlayerResourceHookListener, PlayerResourceGainContext, Task> callback)
        {
            await callback(entry.Listener, context);
            entry.Model?.InvokeExecutionFinished();
        }
    }
}
