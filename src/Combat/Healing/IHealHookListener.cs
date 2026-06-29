namespace STS2RitsuLib.Combat.Healing
{
    /// <summary>
    ///     Optional listener for creature healing amount hooks.
    ///     生物治疗数值 hook 的可选监听器。
    /// </summary>
    public interface IHealHookListener
    {
        /// <summary>
        ///     Modifies the healing amount passed to <c>CreatureCmd.Heal</c>.
        ///     修正传给 <c>CreatureCmd.Heal</c> 的治疗数值。
        /// </summary>
        decimal ModifyHealAmount(HealContext context, decimal amount)
        {
            return amount;
        }
    }
}
