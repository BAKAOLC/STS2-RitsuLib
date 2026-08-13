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
        ///         Asynchronously tests one source text. Expansion work is cached and performed away from the calling
        ///         thread; cancellation stops this caller's wait without discarding work shared by another caller.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         异步测试一个源文本。扩展工作会被缓存并在调用线程之外执行；取消只停止当前调用方的等待，不会丢弃
        ///         其他调用方正在共享的工作。
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
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">A token that stops waiting for the result.</para>
        ///     <para xml:lang="zh-CN">用于停止等待结果的取消令牌。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A task whose result indicates whether the text matches.</para>
        ///     <para xml:lang="zh-CN">结果指示文本是否匹配的任务。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="text" /> or <paramref name="term" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="text" /> 或 <paramref name="term" /> 为 null。</para>
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     <para xml:lang="en"><paramref name="cancellationToken" /> is canceled.</para>
        ///     <para xml:lang="zh-CN"><paramref name="cancellationToken" /> 已取消。</para>
        /// </exception>
        public static ValueTask<bool> ContainsAsync(
            string text,
            string term,
            CancellationToken cancellationToken = default)
        {
            return Prepare(text).ContainsAsync(term, cancellationToken);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Searches a sequence asynchronously and returns one complete, source-ordered result list. At most
        ///         <paramref name="maximumResults" /> matches are requested; enumeration stops after that requirement is
        ///         satisfied. Use <see cref="SearchStreamAsync{T}" /> when results should appear progressively.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         异步搜索序列并一次返回按源顺序排列的完整结果列表。最多请求
        ///         <paramref name="maximumResults" /> 个匹配项，达到该需求后停止枚举。需要逐步显示结果时使用
        ///         <see cref="SearchStreamAsync{T}" />。
        ///     </para>
        /// </summary>
        /// <typeparam name="T">
        ///     <para xml:lang="en">The source item type.</para>
        ///     <para xml:lang="zh-CN">源项目类型。</para>
        /// </typeparam>
        /// <param name="source">
        ///     <para xml:lang="en">The finite sequence to search once in enumeration order.</para>
        ///     <para xml:lang="zh-CN">按枚举顺序搜索一次的有限序列。</para>
        /// </param>
        /// <param name="textSelector">
        ///     <para xml:lang="en">A thread-safe callback that returns non-null searchable text for each item.</para>
        ///     <para xml:lang="zh-CN">为每个项目返回非 null 可搜索文本的线程安全回调。</para>
        /// </param>
        /// <param name="term">
        ///     <para xml:lang="en">The substring or alternate representation to find.</para>
        ///     <para xml:lang="zh-CN">要查找的子串或可选表示。</para>
        /// </param>
        /// <param name="maximumResults">
        ///     <para xml:lang="en">The positive number of matches required before enumeration may stop.</para>
        ///     <para xml:lang="zh-CN">达到后可以停止枚举的正数匹配项数量。</para>
        /// </param>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">A token that cancels enumeration and waiting.</para>
        ///     <para xml:lang="zh-CN">用于取消枚举和等待的令牌。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A task containing up to the requested number of matching source items.</para>
        ///     <para xml:lang="zh-CN">包含不超过请求数量的匹配源项目的任务。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="source" />, <paramref name="textSelector" />, or <paramref name="term" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="source" />、<paramref name="textSelector" /> 或 <paramref name="term" /> 为 null。</para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en"><paramref name="maximumResults" /> is not positive.</para>
        ///     <para xml:lang="zh-CN"><paramref name="maximumResults" /> 不是正数。</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en"><paramref name="textSelector" /> returns null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="textSelector" /> 返回 null。</para>
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     <para xml:lang="en"><paramref name="cancellationToken" /> is canceled.</para>
        ///     <para xml:lang="zh-CN"><paramref name="cancellationToken" /> 已取消。</para>
        /// </exception>
        public static async Task<IReadOnlyList<T>> SearchAsync<T>(
            IEnumerable<T> source,
            Func<T, string> textSelector,
            string term,
            int maximumResults,
            CancellationToken cancellationToken = default)
        {
            var results = new List<T>();
            await foreach (var result in SearchStreamAsync(
                               source,
                               textSelector,
                               term,
                               maximumResults,
                               cancellationToken).ConfigureAwait(false))
                results.Add(result);
            return results.AsReadOnly();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Searches a sequence asynchronously and yields each match as it becomes available in source order.
        ///         Enumeration stops when the source ends, cancellation is requested, or
        ///         <paramref name="maximumResults" /> matches have been yielded. Text expansions are cached for the
        ///         duration of this enumeration.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         异步搜索序列，并按源顺序在每个匹配项可用时立即生成。源结束、请求取消或已生成
        ///         <paramref name="maximumResults" /> 个匹配项时停止枚举。文本扩展会在本次枚举期间缓存。
        ///     </para>
        /// </summary>
        /// <typeparam name="T">
        ///     <para xml:lang="en">The source item type.</para>
        ///     <para xml:lang="zh-CN">源项目类型。</para>
        /// </typeparam>
        /// <param name="source">
        ///     <para xml:lang="en">The finite sequence to search once in enumeration order.</para>
        ///     <para xml:lang="zh-CN">按枚举顺序搜索一次的有限序列。</para>
        /// </param>
        /// <param name="textSelector">
        ///     <para xml:lang="en">A thread-safe callback that returns non-null searchable text for each item.</para>
        ///     <para xml:lang="zh-CN">为每个项目返回非 null 可搜索文本的线程安全回调。</para>
        /// </param>
        /// <param name="term">
        ///     <para xml:lang="en">The substring or alternate representation to find.</para>
        ///     <para xml:lang="zh-CN">要查找的子串或可选表示。</para>
        /// </param>
        /// <param name="maximumResults">
        ///     <para xml:lang="en">The positive number of matches required before enumeration stops.</para>
        ///     <para xml:lang="zh-CN">达到后停止枚举的正数匹配项数量。</para>
        /// </param>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">A token that cancels enumeration and waiting.</para>
        ///     <para xml:lang="zh-CN">用于取消枚举和等待的令牌。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">An asynchronous stream of matching source items.</para>
        ///     <para xml:lang="zh-CN">匹配源项目的异步流。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="source" />, <paramref name="textSelector" />, or <paramref name="term" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="source" />、<paramref name="textSelector" /> 或 <paramref name="term" /> 为 null。</para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en"><paramref name="maximumResults" /> is not positive.</para>
        ///     <para xml:lang="zh-CN"><paramref name="maximumResults" /> 不是正数。</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en"><paramref name="textSelector" /> returns null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="textSelector" /> 返回 null。</para>
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     <para xml:lang="en"><paramref name="cancellationToken" /> is canceled.</para>
        ///     <para xml:lang="zh-CN"><paramref name="cancellationToken" /> 已取消。</para>
        /// </exception>
        public static async IAsyncEnumerable<T> SearchStreamAsync<T>(
            IEnumerable<T> source,
            Func<T, string> textSelector,
            string term,
            int maximumResults,
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(textSelector);
            ArgumentNullException.ThrowIfNull(term);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumResults);

            var preparedTexts = new Dictionary<string, RitsuSearchText>(StringComparer.Ordinal);
            var resultCount = 0;
            var processedCount = 0;
            foreach (var item in source)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var text = textSelector(item) ??
                           throw new InvalidOperationException("The search text selector returned null.");
                if (!preparedTexts.TryGetValue(text, out var preparedText))
                {
                    preparedText = Prepare(text);
                    preparedTexts.Add(text, preparedText);
                }

                if (await preparedText.ContainsAsync(term, cancellationToken).ConfigureAwait(false))
                {
                    yield return item;
                    if (++resultCount == maximumResults)
                        yield break;
                }

                if (++processedCount % 32 == 0)
                    await Task.Yield();
            }
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

        /// <summary>
        ///     <para xml:lang="en">
        ///         Asynchronously tests the prepared text, sharing its expansion cache with synchronous and concurrent
        ///         asynchronous calls. Cancellation stops this caller's wait without canceling shared cache generation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         异步测试已准备文本，并与同步及并发异步调用共享扩展缓存。取消只停止当前调用方的等待，不会取消共享
        ///         缓存的生成。
        ///     </para>
        /// </summary>
        /// <param name="term">
        ///     <para xml:lang="en">The substring or alternate representation to find.</para>
        ///     <para xml:lang="zh-CN">要查找的子串或可选表示。</para>
        /// </param>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">A token that stops waiting for the result.</para>
        ///     <para xml:lang="zh-CN">用于停止等待结果的取消令牌。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A task whose result indicates whether the text matches.</para>
        ///     <para xml:lang="zh-CN">结果指示文本是否匹配的任务。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="term" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="term" /> 为 null。</para>
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     <para xml:lang="en"><paramref name="cancellationToken" /> is canceled.</para>
        ///     <para xml:lang="zh-CN"><paramref name="cancellationToken" /> 已取消。</para>
        /// </exception>
        public ValueTask<bool> ContainsAsync(string term, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(term);
            return RitsuSearchMatcher.ContainsAsync(_text, term, _preparedText, cancellationToken);
        }
    }
}
