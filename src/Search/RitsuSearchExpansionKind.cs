namespace STS2RitsuLib.Search
{
    /// <summary>
    ///     <para xml:lang="en">Classifies an alternate searchable representation supplied by a search expansion provider.</para>
    ///     <para xml:lang="zh-CN">对搜索扩展提供器给出的可选搜索表示形式进行分类。</para>
    /// </summary>
    public enum RitsuSearchExpansionKind
    {
        /// <summary>
        ///     <para xml:lang="en">A provider's preferred full transliteration.</para>
        ///     <para xml:lang="zh-CN">提供器首选的完整转写。</para>
        /// </summary>
        Transliteration,

        /// <summary>
        ///     <para xml:lang="en">A valid but non-preferred reading or transliteration.</para>
        ///     <para xml:lang="zh-CN">有效但非首选的读音或转写。</para>
        /// </summary>
        AlternateReading,

        /// <summary>
        ///     <para xml:lang="en">An abbreviated representation such as transliterated initials.</para>
        ///     <para xml:lang="zh-CN">转写首字母等缩略表示。</para>
        /// </summary>
        Initialism,
    }
}
