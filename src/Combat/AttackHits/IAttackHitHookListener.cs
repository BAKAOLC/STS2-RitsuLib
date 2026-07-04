namespace STS2RitsuLib.Combat.AttackHits
{
    /// <summary>
    ///     Optional listener for per-hit attack hooks.
    ///     每段攻击 hook 的可选监听器。
    /// </summary>
    public interface IAttackHitHookListener
    {
        /// <summary>
        ///     Runs before the hit's damage command. Await game commands here, then mutate
        ///     damage command inputs on <see cref="AttackHitContext" /> to affect only this hit.
        ///     在本段伤害命令执行前运行。可在此 await 游戏命令，然后修改
        ///     <see cref="AttackHitContext" /> 上的伤害命令输入，只影响本段。
        /// </summary>
        Task BeforeAttackHit(AttackHitContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Runs after the hit's damage command resolves.
        ///     在本段伤害命令结算后运行。
        /// </summary>
        Task AfterAttackHit(AttackHitContext context)
        {
            return Task.CompletedTask;
        }
    }
}
