namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Required capability validation policy.
    ///     所需能力验证策略。
    /// </summary>
    public enum RitsuLibSidecarRequiredCapabilityPolicy
    {
        /// <summary>
        ///     Emit warnings but allow begin-run flow to continue.
        ///     发出警告，但允许 begin-run 流程继续。
        /// </summary>
        Warn = 0,

        /// <summary>
        ///     Block begin-run when validation fails.
        ///     验证失败时阻止 begin-run。
        /// </summary>
        Fail = 1,
    }

    /// <summary>
    ///     Event payload produced after required capability validation.
    ///     所需能力验证后生成的事件载荷。
    /// </summary>
    public readonly record struct SidecarRequiredCapabilityCheckCompletedEvent(
        bool Passed,
        RitsuLibSidecarRequiredCapabilityPolicy Policy,
        IReadOnlyList<SidecarRequiredCapabilityMiss> MissingByPeer);

    /// <summary>
    ///     Missing required capabilities for one peer.
    ///     某个对等端缺失的所需能力。
    /// </summary>
    public readonly record struct SidecarRequiredCapabilityMiss(
        ulong PeerNetId,
        IReadOnlyList<string> MissingCapabilities);

    /// <summary>
    ///     Registry and validator for required sidecar capabilities.
    ///     所需 sidecar 能力的注册表和验证器。
    /// </summary>
    public static class RitsuLibSidecarRequiredCapabilities
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<string, Func<ulong, bool>> CapabilityChecks = [];

        /// <summary>
        ///     Validation policy used during pre-run checks.
        ///     预跑局检查期间使用的验证策略。
        /// </summary>
        public static RitsuLibSidecarRequiredCapabilityPolicy Policy { get; set; } =
            RitsuLibSidecarRequiredCapabilityPolicy.Warn;

        /// <summary>
        ///     Raised after each validation run.
        ///     每次验证运行后引发。
        /// </summary>
        public static event Action<SidecarRequiredCapabilityCheckCompletedEvent>? CheckCompleted;

        /// <summary>
        ///     Registers one required capability evaluator.
        ///     注册一个所需能力求值器。
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
        ///     验证指定对等端集合的所需能力。
        /// </summary>
        public static bool ValidatePeers(IEnumerable<ulong> peerNetIds, out SidecarRequiredCapabilityMiss[] misses)
        {
            KeyValuePair<string, Func<ulong, bool>>[] checks;
            lock (Gate)
            {
                checks = [..CapabilityChecks];
            }

            var missList = new List<SidecarRequiredCapabilityMiss>();
            foreach (var peerId in peerNetIds.Distinct())
            {
                var missing = new List<string>();
                for (var i = 0; i < checks.Length; i++)
                    if (!checks[i].Value(peerId))
                        missing.Add(checks[i].Key);

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
