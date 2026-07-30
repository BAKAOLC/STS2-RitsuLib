using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides extension methods for converting between mod pile IDs and runtime
    ///         <see cref="PileType" /> values.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供在模组牌堆 ID 与运行时 <see cref="PileType" /> 值之间转换的扩展方法。
    ///     </para>
    /// </summary>
    public static class ModCardPileExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the deterministic <see cref="PileType" /> value for a pile ID. The ID does not need to
        ///         have a registered definition.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取牌堆 ID 对应的确定性 <see cref="PileType" /> 值；该 ID 无需已有已注册定义。
        ///     </para>
        /// </summary>
        /// <param name="pileId">
        ///     <para xml:lang="en">The nonblank pile ID.</para>
        ///     <para xml:lang="zh-CN">非空白的牌堆 ID。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The deterministic runtime value assigned to the ID.</para>
        ///     <para xml:lang="zh-CN">分配给该 ID 的确定性运行时值。</para>
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en"><paramref name="pileId" /> is empty or contains only whitespace.</para>
        ///     <para xml:lang="zh-CN"><paramref name="pileId" /> 为空或仅包含空白字符。</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="pileId" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="pileId" /> 为 <see langword="null" />。</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">The ID collides with a different ID's deterministic runtime value.</para>
        ///     <para xml:lang="zh-CN">该 ID 与另一 ID 的确定性运行时值发生冲突。</para>
        /// </exception>
        public static PileType GetModCardPileType(this string pileId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pileId);
            return ModCardPileRegistry.GetPileType(pileId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to get the ID of the registered mod pile represented by a <see cref="PileType" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试获取指定 <see cref="PileType" /> 所表示的已注册模组牌堆 ID。
        ///     </para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">The runtime pile type to look up.</para>
        ///     <para xml:lang="zh-CN">要查找的运行时牌堆类型。</para>
        /// </param>
        /// <param name="id">
        ///     <para xml:lang="en">
        ///         When this method returns <see langword="true" />, the registered pile ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         此方法返回 <see langword="true" /> 时，为已注册的牌堆 ID。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if the value belongs to a registered mod pile; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         如果该值属于已注册的模组牌堆，则为 <see langword="true" />；否则为
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryGetModCardPileId(this PileType value, out string id)
        {
            return ModCardPileRegistry.TryGetId(value, out id);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the ID of the registered mod pile represented by a <see cref="PileType" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取指定 <see cref="PileType" /> 所表示的已注册模组牌堆 ID。
        ///     </para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">The runtime pile type to look up.</para>
        ///     <para xml:lang="zh-CN">要查找的运行时牌堆类型。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The registered pile ID.</para>
        ///     <para xml:lang="zh-CN">已注册的牌堆 ID。</para>
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        ///     <para xml:lang="en"><paramref name="value" /> does not belong to a registered mod pile.</para>
        ///     <para xml:lang="zh-CN"><paramref name="value" /> 不属于任何已注册的模组牌堆。</para>
        /// </exception>
        public static string GetModCardPileId(this PileType value)
        {
            return ModCardPileRegistry.TryGetId(value, out var id)
                ? id
                : throw new KeyNotFoundException($"PileType '0x{(int)value:X8}' is not a registered mod card pile.");
        }
    }
}
