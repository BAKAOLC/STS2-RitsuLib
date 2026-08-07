namespace STS2RitsuLib.Search
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides read-only access to RitsuLib's local text matching for mod-owned search controls.
    ///     </para>
    ///     <para xml:lang="zh-CN">为模组自己的搜索控件提供对 RitsuLib 本地文本匹配的只读访问。</para>
    /// </summary>
    public static class RitsuSearch
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Tests whether <paramref name="text" /> contains <paramref name="term" /> directly or through an enabled
        ///         search expansion provider. Provider callbacks may run synchronously on the calling thread; this method
        ///         does not download or initialize provider data.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         测试 <paramref name="text" /> 是否直接或通过已启用的搜索扩展提供器包含
        ///         <paramref name="term" />。提供器回调可能在调用线程上同步执行；本方法不会下载或初始化提供器数据。
        ///     </para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">The source text to search.</para>
        ///     <para xml:lang="zh-CN">要搜索的源文本。</para>
        /// </param>
        /// <param name="term">
        ///     <para xml:lang="en">The substring or alternate representation to find.</para>
        ///     <para xml:lang="zh-CN">要查找的子串或可选表示。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when a case-insensitive direct or expanded match exists; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         存在不区分大小写的直接匹配或扩展匹配时为 <see langword="true" />；否则为
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="text" /> or <paramref name="term" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="text" /> 或 <paramref name="term" /> 为 null。</para>
        /// </exception>
        public static bool Contains(string text, string term)
        {
            return Prepare(text).Contains(term);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Prepares immutable source text for repeated matching. The returned object is thread-safe and refreshes
        ///         its bounded expansions automatically after language, provider registration, provider data, or user
        ///         enablement changes. Preparation itself does not invoke providers or perform I/O.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为重复匹配准备不可变源文本。返回对象是线程安全的，并会在语言、提供器注册、提供器数据或用户启用
        ///         状态变化后自动刷新受限扩展。准备过程本身不会调用提供器或执行 I/O。
        ///     </para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">
        ///         The source text retained by the returned object until that object is no longer referenced.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回对象在不再被引用前会保留的源文本。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A reusable search-text object owned by the caller.</para>
        ///     <para xml:lang="zh-CN">由调用方持有的可复用搜索文本对象。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="text" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="text" /> 为 null。</para>
        /// </exception>
        public static RitsuSearchText Prepare(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            return new(text);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Holds immutable source text and a lazily refreshed, bounded expansion cache for repeated local searches.
    ///         Instances are thread-safe and do not require disposal.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         保存不可变源文本及用于重复本地搜索、按需刷新的受限扩展缓存。实例是线程安全的，且无需释放。
    ///     </para>
    /// </summary>
    public sealed class RitsuSearchText
    {
        private readonly RitsuSearchPreparedText _preparedText;
        private readonly string _text;

        internal RitsuSearchText(string text)
        {
            _text = text;
            _preparedText = new(text);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tests whether the prepared source text contains <paramref name="term" /> directly or through an enabled
        ///         search expansion provider. Provider callbacks run lazily and may execute synchronously on the calling
        ///         thread; this method does not download or initialize provider data.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         测试已准备的源文本是否直接或通过已启用的搜索扩展提供器包含 <paramref name="term" />。提供器回调
        ///         按需运行，并可能在调用线程上同步执行；本方法不会下载或初始化提供器数据。
        ///     </para>
        /// </summary>
        /// <param name="term">
        ///     <para xml:lang="en">The substring or alternate representation to find.</para>
        ///     <para xml:lang="zh-CN">要查找的子串或可选表示。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when a case-insensitive direct or expanded match exists; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         存在不区分大小写的直接匹配或扩展匹配时为 <see langword="true" />；否则为
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="term" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="term" /> 为 null。</para>
        /// </exception>
        public bool Contains(string term)
        {
            ArgumentNullException.ThrowIfNull(term);
            return RitsuSearchMatcher.Contains(_text, term, _preparedText);
        }
    }
}
