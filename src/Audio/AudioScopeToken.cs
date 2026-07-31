namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">A caller-owned identity token that groups audio handles for explicit bulk cleanup.</para>
    ///     <para xml:lang="zh-CN">由调用方持有、用于将音频句柄分组并显式批量清理的标识令牌。</para>
    /// </summary>
    public sealed class AudioScopeToken : IDisposable
    {
        private int _disposeState;

        internal AudioScopeToken(string name, AudioLifecycleScope scope)
        {
            Name = name;
            Scope = scope;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the caller-provided display name.</para>
        ///     <para xml:lang="zh-CN">获取调用方提供的显示名称。</para>
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the lifecycle scope recorded on handles attached through this token.</para>
        ///     <para xml:lang="zh-CN">获取通过此令牌附加的句柄所记录的生命周期作用域。</para>
        /// </summary>
        public AudioLifecycleScope Scope { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether disposal completed after every tracked handle released successfully.</para>
        ///     <para xml:lang="zh-CN">获取是否已在所有跟踪句柄均成功释放后完成令牌释放。</para>
        /// </summary>
        public bool IsDisposed => Volatile.Read(ref _disposeState) == 2;

        internal bool IsClosing => Volatile.Read(ref _disposeState) != 0;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to stop and release all attached handles, marking the token disposed only after
        ///         complete cleanup; failed cleanup can be retried.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试停止并释放所有附加句柄；仅在清理全部完成后将令牌标记为已释放，失败的清理可再次尝试。</para>
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposeState, 1, 0) != 0)
                return;

            if (AudioLifecycleRegistry.Shared.TryDisposeScope(this))
                Volatile.Write(ref _disposeState, 2);
            else
                Volatile.Write(ref _disposeState, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to stop and release all handles currently attached to this token without changing its
        ///         disposed state.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试停止并释放当前附加到此令牌的所有句柄，但不更改令牌的释放状态。</para>
        /// </summary>
        /// <param name="allowFadeOut">
        ///     <para xml:lang="en">Whether stopped event handles may fade out.</para>
        ///     <para xml:lang="zh-CN">停止事件句柄时是否允许淡出。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when at least one handle was found and every release completed;
        ///         otherwise <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">找到至少一个句柄且所有释放均已完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public bool StopAll(bool allowFadeOut = true)
        {
            return AudioLifecycleRegistry.Shared.StopScope(this, allowFadeOut);
        }
    }
}
