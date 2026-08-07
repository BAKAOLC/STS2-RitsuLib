namespace STS2RitsuLib.Search
{
    /// <summary>
    ///     <para xml:lang="en">Owns one registered search expansion provider and supports cache invalidation.</para>
    ///     <para xml:lang="zh-CN">持有一个已注册的搜索扩展提供器，并支持缓存失效通知。</para>
    /// </summary>
    public sealed class RitsuSearchExpansionRegistration : IDisposable
    {
        private readonly long _token;
        private int _disposed;

        internal RitsuSearchExpansionRegistration(string providerId, long token)
        {
            ProviderId = providerId;
            _token = token;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the registered provider ID.</para>
        ///     <para xml:lang="zh-CN">获取已注册的提供器 ID。</para>
        /// </summary>
        public string ProviderId { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Invalidates cached expansions after the provider's already-local data changes. Calling this method
        ///         after disposal has no effect. The provider must complete downloads and file work before calling it.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在提供器已有的本地数据发生变化后使扩展缓存失效。释放后调用不会产生效果。提供器必须先完成下载和
        ///         文件操作，再调用此方法。
        ///     </para>
        /// </summary>
        public void Invalidate()
        {
            if (Volatile.Read(ref _disposed) == 0)
                RitsuSearchExpansionRegistry.Invalidate(ProviderId, _token);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Unregisters the provider. Repeated disposal has no effect. A callback already captured by a concurrent
        ///         search may finish after this method returns, so the owner must keep its provider safe for such calls.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注销提供器；重复释放不会产生效果。并发搜索已经取得的回调可能会在本方法返回后才结束，因此所属方
        ///         必须保证提供器仍能安全完成此类调用。
        ///     </para>
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                RitsuSearchExpansionRegistry.Unregister(ProviderId, _token);
        }
    }
}
