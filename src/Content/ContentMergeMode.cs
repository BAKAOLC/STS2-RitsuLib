namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Specifies how vanilla model sequences are combined with resolved mod models.
    ///     </para>
    ///     <para xml:lang="zh-CN">指定如何将原版模型序列与已解析的模组模型合并。</para>
    /// </summary>
    internal enum ContentMergeMode
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Places vanilla models first, appends mod models with previously unseen IDs, and materializes
        ///         the result as an array.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         原版模型在前，再追加 ID 尚未出现的模组模型，并将结果实例化为数组。
        ///     </para>
        /// </summary>
        AppendDistinctById = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Preserves the source sequence when there are no mod models; otherwise merges distinct IDs
        ///         into a materialized sequence.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         没有模组模型时保留来源序列；否则按不同 ID 合并为实例化序列。
        ///     </para>
        /// </summary>
        MergeDistinctById = 1,
    }
}
