namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Caller-owned token that groups handles for later stop and cleanup.
    ///     调用方拥有的 token，用于将句柄分组以便稍后停止和清理。
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
        ///     Human-readable scope name.
        ///     人类可读的作用域名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Scope category associated with the token.
        ///     与该 token 关联的作用域类别。
        /// </summary>
        public AudioLifecycleScope Scope { get; }

        /// <summary>
        ///     True after this token has been disposed.
        ///     此 token 已释放后为 true。
        /// </summary>
        public bool IsDisposed => Volatile.Read(ref _disposeState) == 2;

        internal bool IsClosing => Volatile.Read(ref _disposeState) != 0;

        /// <summary>
        ///     Stops and releases any handles still attached to this token.
        ///     停止并释放仍附加到此 token 的任何句柄。
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
        ///     Stops all handles currently attached to this token.
        ///     停止当前附加到此 token 的所有句柄。
        /// </summary>
        public bool StopAll(bool allowFadeOut = true)
        {
            return AudioLifecycleRegistry.Shared.StopScope(this, allowFadeOut);
        }
    }
}
