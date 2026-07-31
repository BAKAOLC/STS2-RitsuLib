using STS2RitsuLib.Interactions.RightClick;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Coordinates one-time installation of RitsuLib Sidecar's built-in protocol handlers.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         协调 RitsuLib Sidecar 内置协议处理器的一次性安装。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarProtocol
    {
        private static readonly Lock Gate = new();

        private static int _registered;
        private static int _registering;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers the built-in control, synchronization, right-click, lifecycle, and capability handlers once
        ///         per process.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在每个进程中注册一次内置的控制、同步、右键交互、生命周期和能力处理器。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         The completed marker is written only after every registration succeeds. An installation failure clears
        ///         the in-progress marker, allowing a later call to retry. Calls that arrive while installation is in
        ///         progress return without waiting, including reentrant calls.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         仅在所有注册成功后才写入完成标记。安装失败会清除进行中标记，使后续调用可以重试。
        ///         在安装进行期间到达的调用（包括重入调用）会直接返回而不会等待。
        ///     </para>
        /// </remarks>
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
