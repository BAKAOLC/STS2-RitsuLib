namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         How patched <see cref="MegaCrit.Sts2.Core.Models.ModelDb" /> getter postfixes merge vanilla
    ///         output with mod models.
    ///     </para>
    ///     <para xml:lang="zh-CN">patched <see cref="MegaCrit.Sts2.Core.Models.ModelDb" /> getter postfix 如何合并原版输出与 mod 模型。</para>
    /// </summary>
    internal enum ContentMergeMode
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Vanilla first, then mod; first <see cref="MegaCrit.Sts2.Core.Models.AbstractModel.Id" /> wins;
        ///         materialize as array.
        ///     </para>
        ///     <para xml:lang="zh-CN">原版在前、mod 在后；首个 <see cref="MegaCrit.Sts2.Core.Models.AbstractModel.Id" /> 胜出；物化为数组。</para>
        /// </summary>
        AppendDistinctById = 0,

        /// <summary>
        ///     <para xml:lang="en">Skip materialization when mod is empty; otherwise union by id into a list.</para>
        ///     <para xml:lang="zh-CN">mod 为空时不物化 vanilla；否则按 id 合并为列表。</para>
        /// </summary>
        MergeDistinctById = 1,
    }
}
