namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     Controls how <see cref="ModContentRegistry" /> assigns the patched public <c>ModelDb.GetEntry(Type)</c> string
    ///     (the stable segment used in saves and localization keys for RitsuLib-registered models).
    ///     控制 <see cref="ModContentRegistry" /> 如何分配已修补的公共 <c>ModelDb.GetEntry(Type)</c> 字符串
    ///     （RitsuLib 注册模型在存档和本地化 key 中使用的稳定段）。
    /// </summary>
    public readonly record struct ModelPublicEntryOptions
    {
        internal ModelPublicEntryOptions(ModelPublicEntryKind kind, string? value)
        {
            Kind = kind;
            Value = value;
        }

        /// <summary>
        ///     Uses the default rule: <c>&lt;MOD&gt;_&lt;CATEGORY&gt;_&lt;CLR_TYPE_NAME&gt;</c> (normalized).
        ///     使用默认规则：<c>&lt;MOD&gt;_&lt;CATEGORY&gt;_&lt;CLR_TYPE_NAME&gt;</c>（规范化后）。
        /// </summary>
        public static ModelPublicEntryOptions FromTypeName => default;

        internal ModelPublicEntryKind Kind { get; }

        internal string? Value { get; }

        /// <summary>
        ///     Replaces the CLR type-name segment with a stable author-chosen stem (normalized).
        ///     Final entry: <c>&lt;MOD&gt;_&lt;CATEGORY&gt;_&lt;STEM&gt;</c>.
        ///     用作者选择的稳定 stem 替换 CLR 类型名段（规范化后）。
        ///     最终条目：<c>&lt;MOD&gt;_&lt;CATEGORY&gt;_&lt;STEM&gt;</c>。
        /// </summary>
        public static ModelPublicEntryOptions FromStem(string entryStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entryStem);
            return new(ModelPublicEntryKind.Stem, entryStem);
        }

        /// <summary>
        ///     Uses the given public entry string verbatim after normalization (must match the patched entry format).
        ///     规范化后逐字使用给定的公共条目字符串（必须匹配已修补的条目格式）。
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
