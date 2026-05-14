namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Event facade for sub-mod consumers that prefer stable subscribe/unsubscribe helpers.
    ///     面向偏好稳定 subscribe/unsubscribe 辅助方法的 sub-mod 消费者的事件门面。
    /// </summary>
    public static class RitsuLibSidecarEvents
    {
        /// <summary>
        ///     Subscribes session-bound events.
        ///     订阅会话绑定事件。
        /// </summary>
        public static IDisposable OnSessionBound(Action<SidecarSessionBoundEvent> handler)
        {
            RitsuLibSidecarSessionManager.SessionBound += handler;
            return new Subscription(() => RitsuLibSidecarSessionManager.SessionBound -= handler);
        }

        /// <summary>
        ///     Subscribes session-unbound events.
        ///     订阅非会话绑定事件。
        /// </summary>
        public static IDisposable OnSessionUnbound(Action<SidecarSessionUnboundEvent> handler)
        {
            RitsuLibSidecarSessionManager.SessionUnbound += handler;
            return new Subscription(() => RitsuLibSidecarSessionManager.SessionUnbound -= handler);
        }

        /// <summary>
        ///     Subscribes peer reachability transition events.
        ///     订阅对等端可达性转换事件。
        /// </summary>
        public static IDisposable OnPeerReachabilityChanged(Action<SidecarPeerReachabilityChangedEvent> handler)
        {
            RitsuLibSidecarSessionManager.PeerReachabilityChanged += handler;
            return new Subscription(() => RitsuLibSidecarSessionManager.PeerReachabilityChanged -= handler);
        }

        /// <summary>
        ///     Subscribes handshake-completed events.
        ///     订阅握手完成事件。
        /// </summary>
        public static IDisposable OnHandshakeCompleted(Action<SidecarHandshakeCompletedEvent> handler)
        {
            RitsuLibSidecarSessionManager.HandshakeCompleted += handler;
            return new Subscription(() => RitsuLibSidecarSessionManager.HandshakeCompleted -= handler);
        }

        /// <summary>
        ///     Subscribes typed-message receive events.
        ///     订阅类型化消息接收事件。
        /// </summary>
        public static IDisposable OnTypedMessageReceived(Action<SidecarTypedMessageReceivedEvent> handler)
        {
            RitsuLibSidecarTypedMessageRegistry.TypedMessageReceived += handler;
            return new Subscription(() => RitsuLibSidecarTypedMessageRegistry.TypedMessageReceived -= handler);
        }

        /// <summary>
        ///     Subscribes config topic-change events.
        ///     订阅配置 topic 变更事件。
        /// </summary>
        public static IDisposable OnConfigTopicChanged(Action<SidecarConfigTopicChangedEvent> handler)
        {
            RitsuLibSidecarConfigSyncService.TopicChanged += handler;
            return new Subscription(() => RitsuLibSidecarConfigSyncService.TopicChanged -= handler);
        }

        /// <summary>
        ///     Subscribes required-capability validation completion events.
        ///     订阅所需能力验证完成事件。
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
