using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Interactions.RightClick;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the entry point that prepares RitsuLib's built-in multiplayer features.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供用于准备 RitsuLib 内置多人功能的入口。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarProtocol
    {
        private static readonly Lock Gate = new();

        private static int _registered;
        private static int _registering;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Prepares RitsuLib's built-in multiplayer support, including synchronized mod interactions and
        ///         optional developer-tool changes. Repeated calls are safe.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         准备 RitsuLib 的内置多人支持，包括同步的模组交互和可选的开发者工具修改；可安全重复调用。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         This method is safe to call repeatedly or reentrantly. A concurrent call may return before another
        ///         call finishes; if preparation fails, a later call may retry.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         此方法可安全重复调用或重入调用。并发调用可能会在另一调用完成前返回；准备失败后，后续调用可再次尝试。
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
                    RitsuDebugActionProtocol.EnsureHandlersRegistered();
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
