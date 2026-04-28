namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Installs default sidecar control handlers (handshake, chunked reassembly) once. Idempotent. Called from
    ///     receive and from <see cref="RitsuLibSidecarHighLevelSend" />.
    /// </summary>
    public static class RitsuLibSidecarProtocol
    {
        private static int _registered;

        /// <summary>
        ///     Registers built-in handlers for control opcodes and chunked reassembly once per process. Safe to call
        ///     from send/receive paths; subsequent calls are no-ops.
        /// </summary>
        public static void EnsureDefaultHandlers()
        {
            if (Interlocked.CompareExchange(ref _registered, 1, 0) != 0)
                return;

            RitsuLibSidecarBuiltInHandlers.Register();
        }
    }
}
