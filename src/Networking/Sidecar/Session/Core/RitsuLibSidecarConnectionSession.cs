namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Stores the last feature flags reported by each peer through a sidecar handshake.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         存储各对等端通过 sidecar 握手报告的最新功能标志。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarConnectionSession
    {
        private static readonly Lock Gate = new();

        private static readonly Dictionary<ulong, RitsuLibSidecarPeerFeatures> PeerToFeatures = [];

        /// <summary>
        ///     <para xml:lang="en">
        ///         Records the feature flags reported by <paramref name="remoteNetId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         记录 <paramref name="remoteNetId" /> 报告的功能标志。
        ///     </para>
        /// </summary>
        public static void SetPeerFeatures(ulong remoteNetId, RitsuLibSidecarPeerFeatures features)
        {
            lock (Gate)
            {
                PeerToFeatures[remoteNetId] = features;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to get the last feature flags recorded for <paramref name="remoteNetId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试获取为 <paramref name="remoteNetId" /> 记录的最新功能标志。
        ///     </para>
        /// </summary>
        public static bool TryGetPeerFeatures(ulong remoteNetId, out RitsuLibSidecarPeerFeatures features)
        {
            lock (Gate)
            {
                return PeerToFeatures.TryGetValue(remoteNetId, out features);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Removes all cached peer feature flags.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         移除所有缓存的对等端功能标志。
        ///     </para>
        /// </summary>
        public static void Clear()
        {
            lock (Gate)
            {
                PeerToFeatures.Clear();
            }
        }
    }
}
