namespace STS2RitsuLib.Combat.AttackHits
{
    /// <summary>
    ///     <para xml:lang="en">Defines optional hooks for individual attack hits.</para>
    ///     <para xml:lang="zh-CN">定义单次攻击命中的可选钩子。</para>
    /// </summary>
    public interface IAttackHitHookListener
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Runs before the hit's damage command. Game commands may be awaited here, and mutable inputs on
        ///         <see cref="AttackHitContext" /> affect only this hit.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在此次命中的伤害命令执行前运行。可在此等待游戏命令；修改 <see cref="AttackHitContext" /> 中的输入
        ///         只影响此次命中。
        ///     </para>
        /// </summary>
        Task BeforeAttackHit(AttackHitContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     <para xml:lang="en">Runs after the hit's damage command resolves.</para>
        ///     <para xml:lang="zh-CN">在此次命中的伤害命令结算后运行。</para>
        /// </summary>
        Task AfterAttackHit(AttackHitContext context)
        {
            return Task.CompletedTask;
        }
    }
}
