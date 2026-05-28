namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Installs default sidecar control handlers (handshake, chunked reassembly) once. Idempotent. Called from
    ///     receive and from <see cref="RitsuLibSidecarHighLevelSend" />.
    ///     安装默认 sidecar 控制处理器（握手、分块重组），只执行一次。幂等。由
    ///     接收路径和 <see cref="RitsuLibSidecarHighLevelSend" /> 调用。
    /// </summary>
    public static class RitsuLibSidecarProtocol
    {
        private static int _registered;

        /// <summary>
        ///     Registers built-in handlers for control opcodes and chunked reassembly once per process. Safe to call
        ///     from send/receive paths; subsequent calls are no-ops.
        ///     按进程注册一次控制 opcode 和分块重组的内置处理器。可安全地
        ///     从发送/接收路径调用；后续调用为空操作。
        /// </summary>
        public static void EnsureDefaultHandlers()
        {
            if (Interlocked.CompareExchange(ref _registered, 1, 0) != 0)
                return;

            RitsuLibSidecarSessionManager.EnsureProvidersBootstrapped();
            RitsuLibSidecarBuiltInHandlers.Register();
            RitsuLibSidecarSyncMessages.RegisterBuiltInHandler();
            RitsuLibSidecarSyncActions.RegisterBuiltInHandlers();
            RitsuLibSidecarNetworkingLifecycle.EnsureHooksInstalled();
            RitsuLibSidecarRequiredCapabilities.RegisterRequiredCapability(
                "ritsulib:sidecar_core_supported",
                RitsuLibSidecarSessionManager.CanSendToPeer);
        }
    }
}
