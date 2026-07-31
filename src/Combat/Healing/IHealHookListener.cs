namespace STS2RitsuLib.Combat.Healing
{
    /// <summary>
    ///     <para xml:lang="en">Defines optional hooks for modifying creature healing.</para>
    ///     <para xml:lang="zh-CN">定义修正生物治疗量的可选钩子。</para>
    /// </summary>
    public interface IHealHookListener
    {
        /// <summary>
        ///     <para xml:lang="en">Returns an amount to add during the additive modifier pass.</para>
        ///     <para xml:lang="zh-CN">返回在加法修正阶段要增加的数值。</para>
        /// </summary>
        decimal ModifyHealAdditive(HealContext context, decimal amount)
        {
            return 0m;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a multiplier for the multiplicative modifier pass.</para>
        ///     <para xml:lang="zh-CN">返回乘法修正阶段使用的倍率。</para>
        /// </summary>
        decimal ModifyHealMultiplicative(HealContext context, decimal amount)
        {
            return 1m;
        }

        /// <summary>
        ///     <para xml:lang="en">Modifies the healing amount after the additive and multiplicative passes.</para>
        ///     <para xml:lang="zh-CN">在加法和乘法修正阶段结束后修正治疗量。</para>
        /// </summary>
        decimal ModifyHealAmount(HealContext context, decimal amount)
        {
            return amount;
        }
    }
}
