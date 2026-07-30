namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">Required capability validation policy.</para>
    ///     <para xml:lang="zh-CN">所需能力验证策略。</para>
    /// </summary>
    public enum RitsuLibSidecarRequiredCapabilityPolicy
    {
        /// <summary>
        ///     <para xml:lang="en">Emit warnings but allow starting the run to continue.</para>
        ///     <para xml:lang="zh-CN">发出警告，但允许继续开始一局游戏。</para>
        /// </summary>
        Warn = 0,

        /// <summary>
        ///     <para xml:lang="en">Block starting the run when validation fails.</para>
        ///     <para xml:lang="zh-CN">验证失败时阻止开始一局游戏。</para>
        /// </summary>
        Fail = 1,
    }

    /// <summary>
    ///     <para xml:lang="en">Event payload produced after required capability validation.</para>
    ///     <para xml:lang="zh-CN">所需能力验证后生成的事件载荷。</para>
    /// </summary>
    public readonly record struct SidecarRequiredCapabilityCheckCompletedEvent(
        bool Passed,
        RitsuLibSidecarRequiredCapabilityPolicy Policy,
        IReadOnlyList<SidecarRequiredCapabilityMiss> MissingByPeer);

    /// <summary>
    ///     <para xml:lang="en">Missing required capabilities for one peer.</para>
    ///     <para xml:lang="zh-CN">某个对等端缺失的所需能力。</para>
    /// </summary>
    public readonly record struct SidecarRequiredCapabilityMiss(
        ulong PeerNetId,
        IReadOnlyList<string> MissingCapabilities);

    /// <summary>
    ///     <para xml:lang="en">Registry and validator for required sidecar capabilities.</para>
    ///     <para xml:lang="zh-CN">所需 Sidecar 能力的注册表与验证器。</para>
    /// </summary>
    public static class RitsuLibSidecarRequiredCapabilities
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<string, Func<ulong, bool>> CapabilityChecks = [];

        /// <summary>
        ///     <para xml:lang="en">Validation policy used before starting a run.</para>
        ///     <para xml:lang="zh-CN">开始一局游戏前检查所使用的验证策略。</para>
        /// </summary>
        public static RitsuLibSidecarRequiredCapabilityPolicy Policy { get; set; } =
            RitsuLibSidecarRequiredCapabilityPolicy.Warn;

        /// <summary>
        ///     <para xml:lang="en">Raised after each validation run.</para>
        ///     <para xml:lang="zh-CN">每次验证运行后引发。</para>
        /// </summary>
        public static event Action<SidecarRequiredCapabilityCheckCompletedEvent>? CheckCompleted;

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an evaluator for one required capability.</para>
        ///     <para xml:lang="zh-CN">注册或替换一项所需能力的判定器。</para>
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
        ///     <para xml:lang="en">Validates required capabilities for the specified peer set.</para>
        ///     <para xml:lang="zh-CN">验证指定对等端集合的所需能力。</para>
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
