namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Configures the public <c>ModelDb</c> entry assigned to a RitsuLib-registered model.
    ///     </para>
    ///     <para xml:lang="zh-CN">配置分配给 RitsuLib 已注册模型的公共 <c>ModelDb</c> 条目。</para>
    /// </summary>
    public readonly record struct ModelPublicEntryOptions
    {
        internal ModelPublicEntryOptions(ModelPublicEntryKind kind, string? value)
        {
            Kind = kind;
            Value = value;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the default entry rule: <c>&lt;MOD&gt;_&lt;CATEGORY&gt;_&lt;CLR_TYPE_NAME&gt;</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取默认条目规则：<c>&lt;MOD&gt;_&lt;CATEGORY&gt;_&lt;CLR_TYPE_NAME&gt;</c>。
        ///     </para>
        /// </summary>
        public static ModelPublicEntryOptions FromTypeName => default;

        internal ModelPublicEntryKind Kind { get; }

        internal string? Value { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an entry rule using an author-selected stem:
        ///         <c>&lt;MOD&gt;_&lt;CATEGORY&gt;_&lt;STEM&gt;</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建使用作者指定名称的条目规则：<c>&lt;MOD&gt;_&lt;CATEGORY&gt;_&lt;STEM&gt;</c>。
        ///     </para>
        /// </summary>
        public static ModelPublicEntryOptions FromStem(string entryStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entryStem);
            return new(ModelPublicEntryKind.Stem, entryStem);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an entry rule using the supplied complete public entry after normalization.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建使用规范化后完整公共条目的规则。</para>
        /// </summary>
        public static ModelPublicEntryOptions FromFullPublicEntry(string fullPublicEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullPublicEntry);
            return new(ModelPublicEntryKind.FullEntry, fullPublicEntry);
        }
    }

    internal enum ModelPublicEntryKind
    {
        FromTypeName = 0,
        Stem = 1,
        FullEntry = 2,
    }
}
