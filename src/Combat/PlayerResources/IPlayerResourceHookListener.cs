namespace STS2RitsuLib.Combat.PlayerResources
{
    /// <summary>
    ///     <para xml:lang="en">Defines optional gameplay hooks for built-in player resources.</para>
    ///     <para xml:lang="zh-CN">定义游戏内置玩家资源的可选钩子。</para>
    /// </summary>
    public interface IPlayerResourceHookListener
    {
        /// <summary>
        ///     <para xml:lang="en">Runs after the player gains energy through <c>PlayerCmd.GainEnergy</c>.</para>
        ///     <para xml:lang="zh-CN">在玩家通过 <c>PlayerCmd.GainEnergy</c> 获得能量后运行。</para>
        /// </summary>
        Task AfterPlayerEnergyGained(PlayerResourceGainContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     <para xml:lang="en">Runs after the player gains stars through <c>PlayerCmd.GainStars</c>.</para>
        ///     <para xml:lang="zh-CN">在玩家通过 <c>PlayerCmd.GainStars</c> 获得辉星后运行。</para>
        /// </summary>
        Task AfterPlayerStarsGained(PlayerResourceGainContext context)
        {
            return Task.CompletedTask;
        }
    }
}
