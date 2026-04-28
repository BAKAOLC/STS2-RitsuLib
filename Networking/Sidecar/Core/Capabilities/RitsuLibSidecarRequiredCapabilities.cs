namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Required capability validation policy.
    /// </summary>
    public enum RitsuLibSidecarRequiredCapabilityPolicy
    {
        /// <summary>
        ///     Emit warnings but allow begin-run flow to continue.
        /// </summary>
        Warn = 0,

        /// <summary>
        ///     Block begin-run when validation fails.
        /// </summary>
        Fail = 1,
    }

    /// <summary>
    ///     Event payload produced after required capability validation.
    /// </summary>
    public readonly record struct SidecarRequiredCapabilityCheckCompletedEvent(
        bool Passed,
        RitsuLibSidecarRequiredCapabilityPolicy Policy,
        IReadOnlyList<SidecarRequiredCapabilityMiss> MissingByPeer);

    /// <summary>
    ///     Missing required capabilities for one peer.
    /// </summary>
    public readonly record struct SidecarRequiredCapabilityMiss(
        ulong PeerNetId,
        IReadOnlyList<string> MissingCapabilities);

    /// <summary>
    ///     Registry and validator for required sidecar capabilities.
    /// </summary>
    public static class RitsuLibSidecarRequiredCapabilities
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<string, Func<ulong, bool>> CapabilityChecks = [];

        /// <summary>
        ///     Validation policy used during pre-run checks.
        /// </summary>
        public static RitsuLibSidecarRequiredCapabilityPolicy Policy { get; set; } =
            RitsuLibSidecarRequiredCapabilityPolicy.Warn;

        /// <summary>
        ///     Raised after each validation run.
        /// </summary>
        public static event Action<SidecarRequiredCapabilityCheckCompletedEvent>? CheckCompleted;

        /// <summary>
        ///     Registers one required capability evaluator.
        /// </summary>
        public static void RegisterRequiredCapability(string capabilityKey, Func<ulong, bool> evaluator)
        {
            ArgumentException.ThrowIfNullOrEmpty(capabilityKey);
            ArgumentNullException.ThrowIfNull(evaluator);
            lock (Gate)
            {
                CapabilityChecks[capabilityKey] = evaluator;
            }
        }

        /// <summary>
        ///     Validates required capabilities for the specified peer set.
        /// </summary>
        public static bool ValidatePeers(IEnumerable<ulong> peerNetIds, out SidecarRequiredCapabilityMiss[] misses)
        {
            Func<ulong, bool>[] checks;
            string[] names;
            lock (Gate)
            {
                names = [..CapabilityChecks.Keys];
                checks = [..CapabilityChecks.Values];
            }

            var missList = new List<SidecarRequiredCapabilityMiss>();
            foreach (var peerId in peerNetIds.Distinct())
            {
                var missing = new List<string>();
                for (var i = 0; i < checks.Length; i++)
                    if (!checks[i](peerId))
                        missing.Add(names[i]);

                if (missing.Count > 0)
                    missList.Add(new(peerId, missing));
            }

            misses = [..missList];
            var passed = misses.Length == 0 || Policy == RitsuLibSidecarRequiredCapabilityPolicy.Warn;
            CheckCompleted?.Invoke(new(passed, Policy, misses));
            return passed;
        }
    }
}
