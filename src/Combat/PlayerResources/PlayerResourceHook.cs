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
    ///     Dispatches built-in player resource hooks to model, capability, and registered global listeners.
    ///     将内建玩家资源 hook 分发给模型、capability 和已注册的全局监听器。
    /// </summary>
    public static class PlayerResourceHook
    {
        private static readonly ModelHookListenerRegistry<IPlayerResourceHookListener> GlobalListeners = new();

        /// <summary>
        ///     Registers a process-wide listener. Model-owned effects should usually implement
        ///     <see cref="IPlayerResourceHookListener" /> directly.
        ///     注册一个进程级监听器。模型所属效果通常应直接实现 <see cref="IPlayerResourceHookListener" />。
        /// </summary>
        public static void RegisterGlobalListener(IPlayerResourceHookListener listener)
        {
            GlobalListeners.Register(listener);
        }

        /// <summary>
        ///     Runs after-energy-gained hooks.
        ///     运行能量获得后 hook。
        /// </summary>
        public static async Task AfterEnergyGained(PlayerResourceGainContext context)
        {
            foreach (var entry in IterateListeners(context.CombatState))
                await entry.Listener.AfterPlayerEnergyGained(context);
        }

        /// <summary>
        ///     Runs after-stars-gained hooks.
        ///     运行辉星获得后 hook。
        /// </summary>
        public static async Task AfterStarsGained(PlayerResourceGainContext context)
        {
            foreach (var entry in IterateListeners(context.CombatState))
                await entry.Listener.AfterPlayerStarsGained(context);
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
    }
}
