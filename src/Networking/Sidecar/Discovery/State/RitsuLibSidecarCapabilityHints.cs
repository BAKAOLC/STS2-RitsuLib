namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarCapabilityHints
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<ulong, RitsuLibSidecarPeerReachability> Hints = [];

        public static void SetHint(ulong peerNetId, RitsuLibSidecarPeerReachability reachability)
        {
            lock (Gate)
            {
                Hints[peerNetId] = reachability;
            }
        }

        public static RitsuLibSidecarPeerReachability? TryGetHint(ulong peerNetId)
        {
            lock (Gate)
            {
                return Hints.GetValueOrDefault(peerNetId, RitsuLibSidecarPeerReachability.Unknown) switch
                {
                    RitsuLibSidecarPeerReachability.Unknown => null,
                    var v => v,
                };
            }
        }
    }
}
