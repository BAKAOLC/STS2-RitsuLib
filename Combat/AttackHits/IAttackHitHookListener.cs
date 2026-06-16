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
        ///     <see cref="AttackHitContext.Damage" /> to change this hit's base damage.
        ///     在本段伤害命令执行前运行。可在此 await 游戏命令，然后修改
        ///     <see cref="AttackHitContext.Damage" /> 改变本段基础伤害。
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
