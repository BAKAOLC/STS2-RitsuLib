namespace STS2RitsuLib.Search
{
    /// <summary>
    ///     <para xml:lang="en">Represents one bounded alternate form that can participate in local text search.</para>
    ///     <para xml:lang="zh-CN">表示一个可参与本地文本搜索、长度受限的可选形式。</para>
    /// </summary>
    public sealed class RitsuSearchExpansion
    {
        /// <summary>
        ///     <para xml:lang="en">The maximum accepted expansion length.</para>
        ///     <para xml:lang="zh-CN">允许的扩展文本最大长度。</para>
        /// </summary>
        public const int MaximumTextLength = 512;

        /// <summary>
        ///     <para xml:lang="en">Creates a searchable expansion.</para>
        ///     <para xml:lang="zh-CN">创建一个可搜索扩展。</para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">Non-empty alternate text of at most 512 characters.</para>
        ///     <para xml:lang="zh-CN">非空且不超过 512 个字符的可选文本。</para>
        /// </param>
        /// <param name="kind">
        ///     <para xml:lang="en">The expansion category used by RitsuLib to rank matches.</para>
        ///     <para xml:lang="zh-CN">RitsuLib 用于排列匹配结果的扩展类别。</para>
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en"><paramref name="text" /> is empty, whitespace, or too long.</para>
        ///     <para xml:lang="zh-CN"><paramref name="text" /> 为空、仅含空白或过长。</para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en"><paramref name="kind" /> is not defined.</para>
        ///     <para xml:lang="zh-CN"><paramref name="kind" /> 不是已定义的值。</para>
        /// </exception>
        public RitsuSearchExpansion(string text, RitsuSearchExpansionKind kind)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            if (text.Length > MaximumTextLength)
                throw new ArgumentException($"Search expansion text cannot exceed {MaximumTextLength} characters.",
                    nameof(text));
            if (!Enum.IsDefined(kind))
                throw new ArgumentOutOfRangeException(nameof(kind));

            Text = text.Trim();
            Kind = kind;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the alternate searchable text.</para>
        ///     <para xml:lang="zh-CN">获取可选搜索文本。</para>
        /// </summary>
        public string Text { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the expansion category.</para>
        ///     <para xml:lang="zh-CN">获取扩展类别。</para>
        /// </summary>
        public RitsuSearchExpansionKind Kind { get; }
    }
}
