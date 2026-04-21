namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Caller-owned token that groups handles for later stop and cleanup.
    /// </summary>
    public sealed class AudioScopeToken : IDisposable
    {
        internal AudioScopeToken(string name, AudioLifecycleScope scope)
        {
            Name = name;
            Scope = scope;
        }

        /// <summary>
        ///     Human-readable scope name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Scope category associated with the token.
        /// </summary>
        public AudioLifecycleScope Scope { get; }

        /// <summary>
        ///     True after this token has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        ///     Stops and releases any handles still attached to this token.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            AudioLifecycleRegistry.Shared.StopScope(this);
        }

        /// <summary>
        ///     Stops all handles currently attached to this token.
        /// </summary>
        public bool StopAll(bool allowFadeOut = true)
        {
            return AudioLifecycleRegistry.Shared.StopScope(this, allowFadeOut);
        }
    }
}
