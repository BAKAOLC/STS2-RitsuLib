namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Per-peer last-known capability from <see cref="RitsuLibSidecarHandshakeBinary" />.
    ///     来自 <see cref="RitsuLibSidecarHandshakeBinary" /> 的每 peer 最后已知能力。
    /// </summary>
    public static class RitsuLibSidecarConnectionSession
    {
        private static readonly Lock Gate = new();

        private static readonly Dictionary<ulong, RitsuLibSidecarPeerFeatures> PeerToFeatures = [];

        /// <summary>
        ///     Best-effort: records features reported for <paramref name="remoteNetId" />.
        ///     Best-effort：记录 <paramref name="remoteNetId" /> 报告的 feature。
        /// </summary>
        public static void SetPeerFeatures(ulong remoteNetId, RitsuLibSidecarPeerFeatures features)
        {
            lock (Gate)
            {
                PeerToFeatures[remoteNetId] = features;
            }
        }

        /// <summary>
        ///     Returns the last recorded <see cref="RitsuLibSidecarPeerFeatures" /> for <paramref name="remoteNetId" />, if
        ///     any.
        ///     返回最后记录的 <see cref="RitsuLibSidecarPeerFeatures" />，对于 <paramref name="remoteNetId" />，如果
        ///     存在。
        /// </summary>
        public static bool TryGetPeerFeatures(ulong remoteNetId, out RitsuLibSidecarPeerFeatures features)
        {
            lock (Gate)
            {
                return PeerToFeatures.TryGetValue(remoteNetId, out features);
            }
        }

        /// <summary>
        ///     Removes all cached per-peer feature state (e.g. when leaving multiplayer).
        ///     移除所有缓存的每 peer feature 状态（例如离开多人游戏时）。
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
