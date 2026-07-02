namespace STS2RitsuLib.Combat.PlayerResources
{
    /// <summary>
    ///     Optional listener for built-in player resource gameplay hooks.
    ///     内建玩家资源 gameplay hook 的可选监听器。
    /// </summary>
    public interface IPlayerResourceHookListener
    {
        /// <summary>
        ///     Runs after the player gains energy through <c>PlayerCmd.GainEnergy</c>.
        ///     在玩家通过 <c>PlayerCmd.GainEnergy</c> 获得能量后运行。
        /// </summary>
        Task AfterPlayerEnergyGained(PlayerResourceGainContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Runs after the player gains stars through <c>PlayerCmd.GainStars</c>.
        ///     在玩家通过 <c>PlayerCmd.GainStars</c> 获得辉星后运行。
        /// </summary>
        Task AfterPlayerStarsGained(PlayerResourceGainContext context)
        {
            return Task.CompletedTask;
        }
    }
}
