namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">Event-subscription API for other mods that prefer stable subscribe and unsubscribe methods.</para>
    ///     <para xml:lang="zh-CN">为其他模组提供稳定订阅与取消订阅方法的事件 API。</para>
    /// </summary>
    public static class RitsuLibSidecarEvents
    {
        /// <summary>
        ///     <para xml:lang="en">Subscribes session-bound events.</para>
        ///     <para xml:lang="zh-CN">订阅会话绑定事件。</para>
        /// </summary>
        public static IDisposable OnSessionBound(Action<SidecarSessionBoundEvent> handler)
        {
            RitsuLibSidecarSessionManager.SessionBound += handler;
            return new Subscription(() => RitsuLibSidecarSessionManager.SessionBound -= handler);
        }

        /// <summary>
        ///     <para xml:lang="en">Subscribes session-unbound events.</para>
        ///     <para xml:lang="zh-CN">订阅会话解绑事件。</para>
        /// </summary>
        public static IDisposable OnSessionUnbound(Action<SidecarSessionUnboundEvent> handler)
        {
            RitsuLibSidecarSessionManager.SessionUnbound += handler;
            return new Subscription(() => RitsuLibSidecarSessionManager.SessionUnbound -= handler);
        }

        /// <summary>
        ///     <para xml:lang="en">Subscribes peer reachability transition events.</para>
        ///     <para xml:lang="zh-CN">订阅对等端可达性变化事件。</para>
        /// </summary>
        public static IDisposable OnPeerReachabilityChanged(Action<SidecarPeerReachabilityChangedEvent> handler)
        {
            RitsuLibSidecarSessionManager.PeerReachabilityChanged += handler;
            return new Subscription(() => RitsuLibSidecarSessionManager.PeerReachabilityChanged -= handler);
        }

        /// <summary>
        ///     <para xml:lang="en">Subscribes handshake-completed events.</para>
        ///     <para xml:lang="zh-CN">订阅握手完成事件。</para>
        /// </summary>
        public static IDisposable OnHandshakeCompleted(Action<SidecarHandshakeCompletedEvent> handler)
        {
            RitsuLibSidecarSessionManager.HandshakeCompleted += handler;
            return new Subscription(() => RitsuLibSidecarSessionManager.HandshakeCompleted -= handler);
        }

        /// <summary>
        ///     <para xml:lang="en">Subscribes typed-message receive events.</para>
        ///     <para xml:lang="zh-CN">订阅类型化消息接收事件。</para>
        /// </summary>
        public static IDisposable OnTypedMessageReceived(Action<SidecarTypedMessageReceivedEvent> handler)
        {
            RitsuLibSidecarTypedMessageRegistry.TypedMessageReceived += handler;
            return new Subscription(() => RitsuLibSidecarTypedMessageRegistry.TypedMessageReceived -= handler);
        }

        /// <summary>
        ///     <para xml:lang="en">Subscribes config topic-change events.</para>
        ///     <para xml:lang="zh-CN">订阅配置主题变更事件。</para>
        /// </summary>
        public static IDisposable OnConfigTopicChanged(Action<SidecarConfigTopicChangedEvent> handler)
        {
            RitsuLibSidecarConfigSyncService.TopicChanged += handler;
            return new Subscription(() => RitsuLibSidecarConfigSyncService.TopicChanged -= handler);
        }

        /// <summary>
        ///     <para xml:lang="en">Subscribes required-capability validation completion events.</para>
        ///     <para xml:lang="zh-CN">订阅所需能力验证完成事件。</para>
        /// </summary>
        public static IDisposable OnRequiredCapabilityCheck(
            Action<SidecarRequiredCapabilityCheckCompletedEvent> handler)
        {
            RitsuLibSidecarRequiredCapabilities.CheckCompleted += handler;
            return new Subscription(() => RitsuLibSidecarRequiredCapabilities.CheckCompleted -= handler);
        }

        private sealed class Subscription(Action dispose) : IDisposable
        {
            private int _disposed;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;
                dispose();
            }
        }
    }
}
