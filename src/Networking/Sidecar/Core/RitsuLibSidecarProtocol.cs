using STS2RitsuLib.Interactions.RightClick;

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
        private static readonly Lock Gate = new();

        private static int _registered;
        private static int _registering;

        /// <summary>
        ///     Registers built-in handlers for control opcodes and chunked reassembly once per process. Safe to call
        ///     from send/receive paths; subsequent calls are no-ops.
        ///     按进程注册一次控制 opcode 和分块重组的内置处理器。可安全地
        ///     从发送/接收路径调用；后续调用为空操作。
        /// </summary>
        public static void EnsureDefaultHandlers()
        {
            if (Volatile.Read(ref _registered) != 0 || Volatile.Read(ref _registering) != 0)
                return;

            lock (Gate)
            {
                if (_registered != 0 || _registering != 0)
                    return;

                Volatile.Write(ref _registering, 1);
                try
                {
                    RitsuLibSidecarSessionManager.EnsureProvidersBootstrapped();
                    RitsuLibSidecarBuiltInHandlers.Register();
                    RitsuLibSidecarSyncMessages.RegisterBuiltInHandler();
                    ModRightClickRegistry.RegisterBuiltInSyncDescriptors();
                    RitsuLibSidecarNetworkingLifecycle.EnsureHooksInstalled();
                    RitsuLibSidecarRequiredCapabilities.RegisterRequiredCapability(
                        "ritsulib:sidecar_core_supported",
                        RitsuLibSidecarSessionManager.CanSendToPeer);
                    RitsuLibSidecarRequiredCapabilities.RegisterRequiredCapability(
                        "ritsulib:managed_net_actions",
                        peerNetId => RitsuLibSidecarSessionManager.TryGetPeerFeatures(peerNetId, out var features) &&
                                     (features & RitsuLibSidecarPeerFeatures.ManagedNetActions) != 0);
                    Volatile.Write(ref _registered, 1);
                }
                finally
                {
                    Volatile.Write(ref _registering, 0);
                }
            }
        }
    }
}
