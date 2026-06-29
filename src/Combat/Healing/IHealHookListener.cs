namespace STS2RitsuLib.Combat.Healing
{
    /// <summary>
    ///     Optional listener for creature healing hooks.
    ///     生物治疗 hook 的可选监听器。
    /// </summary>
    public interface IHealHookListener
    {
        /// <summary>
        ///     Returns an amount to add during the additive healing modifier pass.
        ///     在 additive 治疗修正阶段返回要增加的数值。
        /// </summary>
        decimal ModifyHealAdditive(HealContext context, decimal amount)
        {
            return 0m;
        }

        /// <summary>
        ///     Returns a multiplier for the multiplicative healing modifier pass.
        ///     在 multiplicative 治疗修正阶段返回倍率。
        /// </summary>
        decimal ModifyHealMultiplicative(HealContext context, decimal amount)
        {
            return 1m;
        }

        /// <summary>
        ///     Modifies the healing amount after additive and multiplicative passes.
        ///     在 additive 和 multiplicative 阶段之后修正治疗数值。
        /// </summary>
        decimal ModifyHealAmount(HealContext context, decimal amount)
        {
            return amount;
        }
    }
}
