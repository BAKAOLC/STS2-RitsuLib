namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>Per-peer last-known capability from <see cref="RitsuLibSidecarHandshakeBinary" />.</summary>
    public static class RitsuLibSidecarConnectionSession
    {
        private static readonly Lock Gate = new();

        private static readonly Dictionary<ulong, RitsuLibSidecarPeerFeatures> PeerToFeatures = [];

        /// <summary>Best-effort: records features reported for <paramref name="remoteNetId" />.</summary>
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
        /// </summary>
        public static bool TryGetPeerFeatures(ulong remoteNetId, out RitsuLibSidecarPeerFeatures features)
        {
            lock (Gate)
            {
                return PeerToFeatures.TryGetValue(remoteNetId, out features);
            }
        }

        /// <summary>Removes all cached per-peer feature state (e.g. when leaving multiplayer).</summary>
        public static void Clear()
        {
            lock (Gate)
            {
                PeerToFeatures.Clear();
            }
        }
    }
}
