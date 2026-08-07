namespace STS2RitsuLib.Search
{
    /// <summary>
    ///     <para xml:lang="en">Describes the locale context used while preparing alternate search text.</para>
    ///     <para xml:lang="zh-CN">描述准备可选搜索文本时使用的语言环境上下文。</para>
    /// </summary>
    public sealed class RitsuSearchExpansionContext
    {
        internal RitsuSearchExpansionContext(string languageCode)
        {
            LanguageCode = languageCode;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets RitsuLib's normalized current game language code, such as <c>zhs</c>, <c>jpn</c>, or
        ///         <c>eng</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 RitsuLib 规范化后的当前游戏语言代码，例如 <c>zhs</c>、<c>jpn</c> 或 <c>eng</c>。
        ///     </para>
        /// </summary>
        public string LanguageCode { get; }
    }
}
