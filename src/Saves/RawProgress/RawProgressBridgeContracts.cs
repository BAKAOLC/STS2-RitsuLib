namespace STS2RitsuLib.Saves.RawProgress
{
    /// <summary>
    ///     <para xml:lang="en">Describes independently negotiable capabilities of the raw-progress bridge.</para>
    ///     <para xml:lang="zh-CN">描述原始进度桥接器中可独立协商的能力。</para>
    /// </summary>
    [Flags]
    public enum RawProgressBridgeFeature
    {
        /// <summary>
        ///     <para xml:lang="en">The provider accepts complete progress documents using schema 21.</para>
        ///     <para xml:lang="zh-CN">提供方接受使用 schema 21 的完整进度文档。</para>
        /// </summary>
        RawSchema21Document = 1 << 0,

        /// <summary>
        ///     <para xml:lang="en">A validated raw document is committed without a lossy typed reserialization.</para>
        ///     <para xml:lang="zh-CN">提交已验证的原始文档时不会经过有损的强类型重新序列化。</para>
        /// </summary>
        UnknownJsonPassThrough = 1 << 1,

        /// <summary>
        ///     <para xml:lang="en">Commits use the save store owned by the running game.</para>
        ///     <para xml:lang="zh-CN">提交使用当前游戏实例持有的存档存储。</para>
        /// </summary>
        LiveGameStoreCommit = 1 << 2,

        /// <summary>
        ///     <para xml:lang="en">Local replacement uses the game's backup, temporary-file, flush, and rename path.</para>
        ///     <para xml:lang="zh-CN">本地替换使用游戏的备份、临时文件、刷新和重命名流程。</para>
        /// </summary>
        DurableLocalReplacement = 1 << 3,

        /// <summary>
        ///     <para xml:lang="en">Cloud-backed commits participate in the game's save-batch scope.</para>
        ///     <para xml:lang="zh-CN">使用云存档的提交会加入游戏的存档批处理作用域。</para>
        /// </summary>
        CloudSaveBatch = 1 << 4,

        /// <summary>
        ///     <para xml:lang="en">The destination generation is compared again inside the exclusive window.</para>
        ///     <para xml:lang="zh-CN">目标代次会在独占窗口内再次进行比较。</para>
        /// </summary>
        ConditionalGenerationCheck = 1 << 5,

        /// <summary>
        ///     <para xml:lang="en">Ordinary in-process progress saves share the commit's exclusive window.</para>
        ///     <para xml:lang="zh-CN">进程内的普通进度保存与提交共享同一独占窗口。</para>
        /// </summary>
        ExclusiveSaveWindow = 1 << 6,

        /// <summary>
        ///     <para xml:lang="en">Recovery metadata is stored locally outside cloud-managed profile data.</para>
        ///     <para xml:lang="zh-CN">恢复元数据仅保存在本地，并位于云端管理的档案数据之外。</para>
        /// </summary>
        LocalOnlyRecoveryJournal = 1 << 7,

        /// <summary>
        ///     <para xml:lang="en">Partial and indeterminate states are returned as structured outcomes.</para>
        ///     <para xml:lang="zh-CN">部分成功和不确定状态会以结构化结果返回。</para>
        /// </summary>
        StructuredRecoveryOutcome = 1 << 8,

        /// <summary>
        ///     <para xml:lang="en">The capability is exposed through a documented public contract.</para>
        ///     <para xml:lang="zh-CN">该能力通过有文档说明的公共契约公开。</para>
        /// </summary>
        StablePublicContract = 1 << 9,

        /// <summary>
        ///     <para xml:lang="en">Cloud writes are checked by reading the cloud backend directly.</para>
        ///     <para xml:lang="zh-CN">云端写入会通过直接读取云端后端进行检查。</para>
        /// </summary>
        CloudReadBackVerification = 1 << 10,

        /// <summary>
        ///     <para xml:lang="en">The live progress state is replaced with the validated committed projection.</para>
        ///     <para xml:lang="zh-CN">内存中的进度状态会替换为已经验证的提交投影。</para>
        /// </summary>
        LiveProgressStateSynchronization = 1 << 11,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Later ordinary saves retain unknown JSON properties when they can be matched safely and abort the save if
        ///         an installed preservation state cannot be applied without loss.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         后续普通保存会在能够安全匹配时保留未知 JSON 属性；若已安装的保留状态无法无损应用，则中止该次保存。
        ///     </para>
        /// </summary>
        SubsequentSaveUnknownJsonPreservation = 1 << 12,

        /// <summary>
        ///     <para xml:lang="en">The provider can capture the active raw document and its local and cloud generation.</para>
        ///     <para xml:lang="zh-CN">提供方可以捕获活动原始文档及其本地与云端代次。</para>
        /// </summary>
        ActiveProgressSnapshot = 1 << 13,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Retained recovery journals can be enumerated and their original document can be restored conditionally.
        ///     </para>
        ///     <para xml:lang="zh-CN">可以枚举保留的恢复日志，并有条件地恢复其中的原始文档。</para>
        /// </summary>
        RecoveryJournalManagement = 1 << 14,
    }

    /// <summary>
    ///     <para xml:lang="en">Describes a raw-progress bridge provider and the contract it currently supports.</para>
    ///     <para xml:lang="zh-CN">描述原始进度桥接提供方及其当前支持的契约。</para>
    /// </summary>
    public sealed record RawProgressBridgeDescriptor
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable provider identifier.</para>
        ///     <para xml:lang="zh-CN">获取稳定的提供方标识符。</para>
        /// </summary>
        public required string ProviderId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the provider package version.</para>
        ///     <para xml:lang="zh-CN">获取提供方包版本。</para>
        /// </summary>
        public required Version ProviderVersion { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the exact request and result protocol version.</para>
        ///     <para xml:lang="zh-CN">获取请求和结果使用的精确协议版本。</para>
        /// </summary>
        public required int ProtocolVersion { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the progress schemas accepted by this game-compatibility build.</para>
        ///     <para xml:lang="zh-CN">获取此游戏兼容构建接受的进度 schema。</para>
        /// </summary>
        public required IReadOnlySet<int> SupportedSchemas { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the advertised capabilities.</para>
        ///     <para xml:lang="zh-CN">获取声明的能力集合。</para>
        /// </summary>
        public required RawProgressBridgeFeature Features { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the largest accepted UTF-8 document size in bytes.</para>
        ///     <para xml:lang="zh-CN">获取允许提交的 UTF-8 文档最大字节数。</para>
        /// </summary>
        public required long MaxDocumentUtf8Bytes { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the maximum number of recovery journals retained at once. New commits fail closed when this limit is
        ///         reached until an existing recovery is resolved.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取同时保留的恢复日志数量上限。达到该上限后，新的提交会保守失败，直至已有恢复项得到处理。
        ///     </para>
        /// </summary>
        public required int MaxRetainedRecoveryJournals { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Identifies one observed version of the active progress destination.</para>
    ///     <para xml:lang="zh-CN">标识活动进度目标的一次已观测版本。</para>
    /// </summary>
    public sealed record ProgressGeneration
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the active profile ID.</para>
        ///     <para xml:lang="zh-CN">获取活动档案 ID。</para>
        /// </summary>
        public required int ProfileId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the observed destination is the modded save namespace.</para>
        ///     <para xml:lang="zh-CN">获取观测目标是否属于模组存档命名空间。</para>
        /// </summary>
        public required bool IsModded { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the progress document's stable unique ID.</para>
        ///     <para xml:lang="zh-CN">获取进度文档的稳定唯一 ID。</para>
        /// </summary>
        public required string ProgressUniqueId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the lowercase SHA-256 hash of the local UTF-8 content.</para>
        ///     <para xml:lang="zh-CN">获取本地 UTF-8 内容的小写 SHA-256 哈希。</para>
        /// </summary>
        public required string LocalSha256 { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the local UTF-8 content length.</para>
        ///     <para xml:lang="zh-CN">获取本地 UTF-8 内容长度。</para>
        /// </summary>
        public required long LocalLength { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the observed local last-modified time in UTC ticks.</para>
        ///     <para xml:lang="zh-CN">获取观测到的本地最后修改时间（UTC ticks）。</para>
        /// </summary>
        public required long LocalLastModifiedUtcTicks { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the running game owns a cloud save backend.</para>
        ///     <para xml:lang="zh-CN">获取当前游戏是否持有云存档后端。</para>
        /// </summary>
        public required bool CloudAvailable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the user allows the cloud copy to be synchronized back to this machine. The game still
        ///         writes local saves to an available cloud backend when this is false.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取用户是否允许将云端副本同步回本机。即使此值为 false，只要云端后端可用，游戏仍会将本地存档写入云端。
        ///     </para>
        /// </summary>
        public required bool CloudSyncEnabled { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the progress file was persisted by the cloud backend.</para>
        ///     <para xml:lang="zh-CN">获取进度文件是否已由云端后端持久化。</para>
        /// </summary>
        public required bool CloudPersisted { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the cloud content hash when a persisted remote file was read.</para>
        ///     <para xml:lang="zh-CN">获取已持久化远端文件成功读回时的内容哈希。</para>
        /// </summary>
        public string? CloudSha256 { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the cloud UTF-8 content length when available.</para>
        ///     <para xml:lang="zh-CN">获取可用时的云端 UTF-8 内容长度。</para>
        /// </summary>
        public long? CloudLength { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the observed cloud last-modified time in UTC ticks when available.</para>
        ///     <para xml:lang="zh-CN">获取可用时观测到的云端最后修改时间（UTC ticks）。</para>
        /// </summary>
        public long? CloudLastModifiedUtcTicks { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Contains the active raw progress document and the generation captured with it.</para>
    ///     <para xml:lang="zh-CN">包含活动原始进度文档及与其同时捕获的代次。</para>
    /// </summary>
    public sealed record RawProgressSnapshot
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the progress schema found in <see cref="RawJson" />.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="RawJson" /> 中的进度 schema。</para>
        /// </summary>
        public required int SchemaVersion { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the complete raw progress JSON.</para>
        ///     <para xml:lang="zh-CN">获取完整的原始进度 JSON。</para>
        /// </summary>
        public required string RawJson { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the destination generation captured in the same exclusive window.</para>
        ///     <para xml:lang="zh-CN">获取在同一独占窗口内捕获的目标代次。</para>
        /// </summary>
        public required ProgressGeneration Generation { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Reports the outcome of capturing the active progress document.</para>
    ///     <para xml:lang="zh-CN">报告捕获活动进度文档的结果。</para>
    /// </summary>
    public enum RawProgressReadOutcome
    {
        /// <summary>
        ///     <para xml:lang="en">The snapshot was captured and validated.</para>
        ///     <para xml:lang="zh-CN">快照已捕获并通过验证。</para>
        /// </summary>
        Succeeded,

        /// <summary>
        ///     <para xml:lang="en">The game has not initialized an active profile.</para>
        ///     <para xml:lang="zh-CN">游戏尚未初始化活动档案。</para>
        /// </summary>
        ActiveProfileUnavailable,

        /// <summary>
        ///     <para xml:lang="en">The active local progress file does not exist or could not be read.</para>
        ///     <para xml:lang="zh-CN">活动本地进度文件不存在或无法读取。</para>
        /// </summary>
        LocalReadUnavailable,

        /// <summary>
        ///     <para xml:lang="en">The active document is malformed, oversized, or lacks a stable identity.</para>
        ///     <para xml:lang="zh-CN">活动文档格式错误、尺寸过大或缺少稳定身份。</para>
        /// </summary>
        ValidationFailed,

        /// <summary>
        ///     <para xml:lang="en">The active document uses a schema this provider build does not accept.</para>
        ///     <para xml:lang="zh-CN">活动文档使用了此提供方构建不接受的 schema。</para>
        /// </summary>
        SchemaUnsupported,

        /// <summary>
        ///     <para xml:lang="en">A persisted cloud copy exists but could not be read completely.</para>
        ///     <para xml:lang="zh-CN">已存在持久化云端副本，但无法完整读回。</para>
        /// </summary>
        CloudReadUnavailable,
    }

    /// <summary>
    ///     <para xml:lang="en">Returns either a validated snapshot or a fail-closed read outcome.</para>
    ///     <para xml:lang="zh-CN">返回已验证快照，或一个保守失败的读取结果。</para>
    /// </summary>
    public sealed record RawProgressReadResult
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the read outcome.</para>
        ///     <para xml:lang="zh-CN">获取读取结果。</para>
        /// </summary>
        public required RawProgressReadOutcome Outcome { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the snapshot only when <see cref="Outcome" /> is <see cref="RawProgressReadOutcome.Succeeded" />.</para>
        ///     <para xml:lang="zh-CN">仅当 <see cref="Outcome" /> 为 <see cref="RawProgressReadOutcome.Succeeded" /> 时获取快照。</para>
        /// </summary>
        public RawProgressSnapshot? Snapshot { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Requests one conditional replacement of the active raw progress document.</para>
    ///     <para xml:lang="zh-CN">请求对活动原始进度文档执行一次条件替换。</para>
    /// </summary>
    public sealed record RawProgressCommitRequest
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the exact protocol version expected by the caller.</para>
        ///     <para xml:lang="zh-CN">获取调用方要求的精确协议版本。</para>
        /// </summary>
        public required int ProtocolVersion { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the schema declared by the proposed document.</para>
        ///     <para xml:lang="zh-CN">获取拟提交文档声明的 schema。</para>
        /// </summary>
        public required int SchemaVersion { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the caller-generated transaction ID used for duplicate suppression and recovery.</para>
        ///     <para xml:lang="zh-CN">获取调用方生成的事务 ID，用于抑制重复提交和恢复。</para>
        /// </summary>
        public required Guid TransactionId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the destination generation that must still be current before mutation.</para>
        ///     <para xml:lang="zh-CN">获取在修改前必须仍然有效的目标代次。</para>
        /// </summary>
        public required ProgressGeneration ExpectedGeneration { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the complete proposed progress JSON.</para>
        ///     <para xml:lang="zh-CN">获取完整的拟提交进度 JSON。</para>
        /// </summary>
        public required string ProposedRawJson { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the caller-computed lowercase SHA-256 hash of the proposed UTF-8 content.</para>
        ///     <para xml:lang="zh-CN">获取调用方计算的拟提交 UTF-8 内容小写 SHA-256 哈希。</para>
        /// </summary>
        public required string ProposedSha256 { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the caller-computed proposed UTF-8 content length.</para>
        ///     <para xml:lang="zh-CN">获取调用方计算的拟提交 UTF-8 内容长度。</para>
        /// </summary>
        public required long ProposedUtf8Length { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Reports evidence obtained from the cloud backend after a commit attempt.</para>
    ///     <para xml:lang="zh-CN">报告提交尝试后从云端后端获得的证据。</para>
    /// </summary>
    public enum CloudReadBackStatus
    {
        /// <summary>
        ///     <para xml:lang="en">The running game has no cloud backend, so cloud verification was not required.</para>
        ///     <para xml:lang="zh-CN">当前游戏没有云端后端，因此不需要云端验证。</para>
        /// </summary>
        NotRequired,

        /// <summary>
        ///     <para xml:lang="en">The cloud document was read and matched the proposed hash.</para>
        ///     <para xml:lang="zh-CN">云端文档已读回，且与拟提交哈希一致。</para>
        /// </summary>
        Succeeded,

        /// <summary>
        ///     <para xml:lang="en">The cloud document could not be read or was not persisted.</para>
        ///     <para xml:lang="zh-CN">云端文档无法读回或未被持久化。</para>
        /// </summary>
        Unavailable,

        /// <summary>
        ///     <para xml:lang="en">The cloud document was read but did not match the proposed hash.</para>
        ///     <para xml:lang="zh-CN">云端文档已读回，但与拟提交哈希不一致。</para>
        /// </summary>
        Mismatch,

        /// <summary>
        ///     <para xml:lang="en">The cloud backend reported an operation failure.</para>
        ///     <para xml:lang="zh-CN">云端后端报告了操作失败。</para>
        /// </summary>
        FailureObserved,
    }

    /// <summary>
    ///     <para xml:lang="en">Reports the final state of a conditional raw-progress commit.</para>
    ///     <para xml:lang="zh-CN">报告条件原始进度提交的最终状态。</para>
    /// </summary>
    public enum RawProgressCommitOutcome
    {
        /// <summary>
        ///     <para xml:lang="en">Local, cloud when present, live memory, and preservation evidence all agree.</para>
        ///     <para xml:lang="zh-CN">本地、存在时的云端、内存状态和保留机制证据全部一致。</para>
        /// </summary>
        CommittedVerified,

        /// <summary>
        ///     <para xml:lang="en">The active destination no longer matches the expected generation.</para>
        ///     <para xml:lang="zh-CN">活动目标不再匹配预期代次。</para>
        /// </summary>
        GenerationConflict,

        /// <summary>
        ///     <para xml:lang="en">The active profile or modded save namespace changed.</para>
        ///     <para xml:lang="zh-CN">活动档案或模组存档命名空间已改变。</para>
        /// </summary>
        ActiveProfileChanged,

        /// <summary>
        ///     <para xml:lang="en">The requested schema is not supported by this provider build.</para>
        ///     <para xml:lang="zh-CN">请求的 schema 不受此提供方构建支持。</para>
        /// </summary>
        SchemaUnsupported,

        /// <summary>
        ///     <para xml:lang="en">The request, fingerprint, identity, or proposed document failed validation before mutation.</para>
        ///     <para xml:lang="zh-CN">请求、指纹、身份或拟提交文档在修改前未通过验证。</para>
        /// </summary>
        ValidationFailed,

        /// <summary>
        ///     <para xml:lang="en">The local destination may have changed but could not be verified.</para>
        ///     <para xml:lang="zh-CN">本地目标可能已经改变，但无法验证。</para>
        /// </summary>
        LocalReplacementUnverified,

        /// <summary>
        ///     <para xml:lang="en">The local document is verified, but the cloud document could not be verified.</para>
        ///     <para xml:lang="zh-CN">本地文档已验证，但云端文档无法验证。</para>
        /// </summary>
        CloudReadBackUnverifiedLocalPreserved,

        /// <summary>
        ///     <para xml:lang="en">The local document is verified, but cloud read-back contains different content.</para>
        ///     <para xml:lang="zh-CN">本地文档已验证，但云端读回内容不同。</para>
        /// </summary>
        CloudReadBackMismatchLocalPreserved,

        /// <summary>
        ///     <para xml:lang="en">The committed local document could not be proven equal to the live known projection.</para>
        ///     <para xml:lang="zh-CN">无法证明已提交的本地文档与内存中的已知投影一致。</para>
        /// </summary>
        LiveProgressStateUnverified,

        /// <summary>
        ///     <para xml:lang="en">Unknown-property preservation could not be installed for later ordinary saves.</para>
        ///     <para xml:lang="zh-CN">无法为后续普通保存安装未知属性保留机制。</para>
        /// </summary>
        UnknownJsonContinuationUnverified,

        /// <summary>
        ///     <para xml:lang="en">Recovery metadata could not be prepared or finalized safely.</para>
        ///     <para xml:lang="zh-CN">恢复元数据无法被安全准备或结束。</para>
        /// </summary>
        RecoveryRequired,

        /// <summary>
        ///     <para xml:lang="en">The requested recovery journal no longer exists.</para>
        ///     <para xml:lang="zh-CN">请求的恢复日志已不存在。</para>
        /// </summary>
        RecoveryJournalNotFound,

        /// <summary>
        ///     <para xml:lang="en">The requested recovery journal is malformed or failed integrity validation.</para>
        ///     <para xml:lang="zh-CN">请求的恢复日志格式错误或未通过完整性验证。</para>
        /// </summary>
        RecoveryJournalInvalid,

        /// <summary>
        ///     <para xml:lang="en">Cancellation was observed before any destination mutation.</para>
        ///     <para xml:lang="zh-CN">在修改任何目标之前收到取消请求。</para>
        /// </summary>
        CancelledBeforeCommit,

        /// <summary>
        ///     <para xml:lang="en">The caller requested a different protocol version.</para>
        ///     <para xml:lang="zh-CN">调用方请求了不同的协议版本。</para>
        /// </summary>
        ProviderIncompatible,
    }

    /// <summary>
    ///     <para xml:lang="en">Contains verification and recovery evidence for a commit attempt.</para>
    ///     <para xml:lang="zh-CN">包含一次提交尝试的验证与恢复证据。</para>
    /// </summary>
    public sealed record RawProgressCommitResult
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the final structured outcome.</para>
        ///     <para xml:lang="zh-CN">获取最终结构化结果。</para>
        /// </summary>
        public required RawProgressCommitOutcome Outcome { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the verified local read-back hash, when available.</para>
        ///     <para xml:lang="zh-CN">获取可用时已经验证的本地读回哈希。</para>
        /// </summary>
        public string? LocalReadBackSha256 { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the cloud read-back status.</para>
        ///     <para xml:lang="zh-CN">获取云端读回状态。</para>
        /// </summary>
        public required CloudReadBackStatus CloudStatus { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the cloud read-back hash, when one was obtained.</para>
        ///     <para xml:lang="zh-CN">获取成功取得时的云端读回哈希。</para>
        /// </summary>
        public string? CloudReadBackSha256 { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the hash of the live known projection, when synchronization was attempted.</para>
        ///     <para xml:lang="zh-CN">获取尝试同步后内存中已知投影的哈希。</para>
        /// </summary>
        public string? LiveKnownProjectionSha256 { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether later ordinary saves received an unknown-property preservation state.</para>
        ///     <para xml:lang="zh-CN">获取后续普通保存是否已获得未知属性保留状态。</para>
        /// </summary>
        public required bool UnknownJsonContinuationInstalled { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the raw hash against which preservation was installed.</para>
        ///     <para xml:lang="zh-CN">获取安装保留机制时对应的原始哈希。</para>
        /// </summary>
        public string? PreservedRawSha256 { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether any destination may have changed during the attempt.</para>
        ///     <para xml:lang="zh-CN">获取尝试期间是否可能已经改变任何目标。</para>
        /// </summary>
        public required bool DestinationMayHaveChanged { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the original local content is available from a verified backup or retained journal.</para>
        ///     <para xml:lang="zh-CN">获取原始本地内容是否可从已验证备份或保留日志中恢复。</para>
        /// </summary>
        public required bool VerifiedBackupAvailable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the local-only recovery journal was retained for inspection or recovery.</para>
        ///     <para xml:lang="zh-CN">获取仅本地恢复日志是否为检查或恢复而被保留。</para>
        /// </summary>
        public required bool RecoveryJournalRetained { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Identifies the last durable stage recorded for a retained recovery journal.</para>
    ///     <para xml:lang="zh-CN">标识保留恢复日志中最后持久记录的阶段。</para>
    /// </summary>
    public enum RawProgressRecoveryStage
    {
        /// <summary>
        ///     <para xml:lang="en">Recovery data was prepared before the destination write began.</para>
        ///     <para xml:lang="zh-CN">恢复数据已在目标写入开始前准备完成。</para>
        /// </summary>
        Prepared,

        /// <summary>
        ///     <para xml:lang="en">The destination may have changed, but its local content could not be verified.</para>
        ///     <para xml:lang="zh-CN">目标可能已经改变，但无法验证其本地内容。</para>
        /// </summary>
        LocalUnverified,

        /// <summary>
        ///     <para xml:lang="en">The new local content was verified, but remaining verification was not finalized.</para>
        ///     <para xml:lang="zh-CN">新的本地内容已经验证，但其余验证尚未完成。</para>
        /// </summary>
        LocalVerified,

        /// <summary>
        ///     <para xml:lang="en">At least one post-write verification step remained incomplete.</para>
        ///     <para xml:lang="zh-CN">至少有一项写入后验证未完成。</para>
        /// </summary>
        VerificationIncomplete,
    }

    /// <summary>
    ///     <para xml:lang="en">Describes one validated local recovery journal without exposing its saved document.</para>
    ///     <para xml:lang="zh-CN">描述一个已验证的本地恢复日志，但不公开其中保存的文档。</para>
    /// </summary>
    public sealed record RawProgressRecoveryRecord
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the transaction that created the journal.</para>
        ///     <para xml:lang="zh-CN">获取创建该日志的事务。</para>
        /// </summary>
        public required Guid TransactionId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the profile ID recorded before the original commit.</para>
        ///     <para xml:lang="zh-CN">获取原始提交前记录的档案 ID。</para>
        /// </summary>
        public required int ProfileId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the journal belongs to the modded save namespace.</para>
        ///     <para xml:lang="zh-CN">获取该日志是否属于模组存档命名空间。</para>
        /// </summary>
        public required bool IsModded { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the stable unique ID of the progress document.</para>
        ///     <para xml:lang="zh-CN">获取进度文档的稳定唯一 ID。</para>
        /// </summary>
        public required string ProgressUniqueId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the journal's last durable stage.</para>
        ///     <para xml:lang="zh-CN">获取日志最后持久记录的阶段。</para>
        /// </summary>
        public required RawProgressRecoveryStage Stage { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the lowercase SHA-256 hash of the recoverable original document.</para>
        ///     <para xml:lang="zh-CN">获取可恢复原始文档的小写 SHA-256 哈希。</para>
        /// </summary>
        public required string OriginalSha256 { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the lowercase SHA-256 hash proposed by the original commit.</para>
        ///     <para xml:lang="zh-CN">获取原始提交所提议内容的小写 SHA-256 哈希。</para>
        /// </summary>
        public required string ProposedSha256 { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Reports whether retained recovery journals were enumerated completely.</para>
    ///     <para xml:lang="zh-CN">报告是否完整枚举了保留的恢复日志。</para>
    /// </summary>
    public enum RawProgressRecoveryReadOutcome
    {
        /// <summary>
        ///     <para xml:lang="en">Every retained journal was read and validated.</para>
        ///     <para xml:lang="zh-CN">所有保留日志均已读取并验证。</para>
        /// </summary>
        Succeeded,

        /// <summary>
        ///     <para xml:lang="en">Valid journals are returned, but one or more malformed entries were ignored.</para>
        ///     <para xml:lang="zh-CN">返回有效日志，但忽略了一个或多个格式错误的条目。</para>
        /// </summary>
        InvalidEntriesIgnored,

        /// <summary>
        ///     <para xml:lang="en">The local recovery directory could not be inspected.</para>
        ///     <para xml:lang="zh-CN">无法检查本地恢复目录。</para>
        /// </summary>
        StorageUnavailable,
    }

    /// <summary>
    ///     <para xml:lang="en">Contains a snapshot of validated retained recovery journals.</para>
    ///     <para xml:lang="zh-CN">包含已验证保留恢复日志的快照。</para>
    /// </summary>
    public sealed record RawProgressRecoveryReadResult
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the enumeration outcome.</para>
        ///     <para xml:lang="zh-CN">获取枚举结果。</para>
        /// </summary>
        public required RawProgressRecoveryReadOutcome Outcome { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets validated journals ordered by transaction ID.</para>
        ///     <para xml:lang="zh-CN">获取按事务 ID 排序的已验证日志。</para>
        /// </summary>
        public required IReadOnlyList<RawProgressRecoveryRecord> Records { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the number of journal files ignored because validation failed.</para>
        ///     <para xml:lang="zh-CN">获取因验证失败而被忽略的日志文件数量。</para>
        /// </summary>
        public required int InvalidEntryCount { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Requests conditional restoration of the original document retained by one journal.</para>
    ///     <para xml:lang="zh-CN">请求有条件地恢复一个日志中保留的原始文档。</para>
    /// </summary>
    public sealed record RawProgressRecoveryRequest
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the transaction ID of the retained journal.</para>
        ///     <para xml:lang="zh-CN">获取保留日志的事务 ID。</para>
        /// </summary>
        public required Guid TransactionId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the current destination generation that must still match before restoration. Capture a fresh
        ///         snapshot after receiving a recovery-required result; do not reuse the generation from the original commit.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取恢复前必须仍然匹配的当前目标代次。收到需要恢复的结果后应重新捕获快照，不要复用原始提交中的代次。
        ///     </para>
        /// </summary>
        public required ProgressGeneration ExpectedGeneration { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides fail-closed capture, conditional commit, and bounded recovery operations for the active progress
    ///         document.
    ///     </para>
    ///     <para xml:lang="zh-CN">为活动进度文档提供保守失败的捕获、条件提交与有界恢复操作。</para>
    /// </summary>
    public interface IRawProgressCommitBridge
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Enumerates validated local recovery journals without exposing or changing their saved documents. Invalid
        ///         entries are counted and ignored rather than returned as trusted recovery data.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         枚举已验证的本地恢复日志，但不公开或改变其中保存的文档。无效条目会被计数并忽略，不会作为可信恢复数据返回。
        ///     </para>
        /// </summary>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">Cancellation observed before enumeration completes.</para>
        ///     <para xml:lang="zh-CN">在枚举完成前接受的取消信号。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The recovery-journal snapshot and its validation outcome.</para>
        ///     <para xml:lang="zh-CN">恢复日志快照及其验证结果。</para>
        /// </returns>
        /// <exception cref="OperationCanceledException">
        ///     <para xml:lang="en">The operation was cancelled before enumeration completed.</para>
        ///     <para xml:lang="zh-CN">操作在枚举完成前被取消。</para>
        /// </exception>
        ValueTask<RawProgressRecoveryReadResult> GetPendingRecoveriesAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Restores a journal's validated original document only when the active profile and freshly captured
        ///         destination generation still match. The same local, cloud, live-state, and preservation verification used
        ///         by a normal raw commit applies. The journal is removed only after a fully verified restoration.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         仅当活动档案与新近捕获的目标代次仍然匹配时，恢复日志中已验证的原始文档。恢复会执行与普通原始提交相同的本地、云端、内存状态和保留机制验证，且仅在恢复得到完整验证后删除日志。
        ///     </para>
        /// </summary>
        /// <param name="request">
        ///     <para xml:lang="en">The journal identity and current destination generation.</para>
        ///     <para xml:lang="zh-CN">日志身份与当前目标代次。</para>
        /// </param>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">Cancellation observed only before destination mutation begins.</para>
        ///     <para xml:lang="zh-CN">仅在目标修改开始前接受的取消信号。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         Structured verification evidence. <see cref="RawProgressCommitOutcome.CommittedVerified" /> means the
        ///         original document was restored and the journal was removed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         结构化验证证据。<see cref="RawProgressCommitOutcome.CommittedVerified" /> 表示原始文档已恢复且日志已删除。
        ///     </para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="request" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="request" /> 为 <see langword="null" />。</para>
        /// </exception>
        ValueTask<RawProgressCommitResult> RestoreRecoveryAsync(
            RawProgressRecoveryRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     <para xml:lang="en">Returns immutable provider capability metadata without reading or changing a profile.</para>
        ///     <para xml:lang="zh-CN">返回不可变的提供方能力元数据，不读取或改变任何档案。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The provider descriptor.</para>
        ///     <para xml:lang="zh-CN">提供方描述符。</para>
        /// </returns>
        RawProgressBridgeDescriptor Describe();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Captures the complete active document and its generation inside the same in-process exclusive window.
        ///         A persisted cloud copy must be readable or the operation fails closed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在同一进程内独占窗口中捕获完整活动文档及其代次。若存在已持久化云端副本但无法读回，操作会保守失败。
        ///     </para>
        /// </summary>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">Cancellation observed before the snapshot is captured.</para>
        ///     <para xml:lang="zh-CN">在捕获快照前接受的取消信号。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The snapshot result.</para>
        ///     <para xml:lang="zh-CN">快照结果。</para>
        /// </returns>
        /// <exception cref="OperationCanceledException">
        ///     <para xml:lang="en">The operation was cancelled before capture completed.</para>
        ///     <para xml:lang="zh-CN">操作在捕获完成前被取消。</para>
        /// </exception>
        ValueTask<RawProgressReadResult> CaptureAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Conditionally commits a complete active progress document. Cancellation is honored only before recovery
        ///         metadata or a destination can be changed; after that point the provider finishes verification and recovery
        ///         bookkeeping before returning.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         有条件地提交完整活动进度文档。取消信号仅会在恢复元数据或目标可能改变之前生效；越过该点后，提供方会先完成验证和恢复记录再返回。
        ///     </para>
        /// </summary>
        /// <param name="request">
        ///     <para xml:lang="en">The validated-generation commit request.</para>
        ///     <para xml:lang="zh-CN">带已验证代次的提交请求。</para>
        /// </param>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">Cancellation observed only before mutation begins.</para>
        ///     <para xml:lang="zh-CN">仅在修改开始前接受的取消信号。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">Structured verification and recovery evidence.</para>
        ///     <para xml:lang="zh-CN">结构化验证与恢复证据。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="request" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="request" /> 为 <see langword="null" />。</para>
        /// </exception>
        ValueTask<RawProgressCommitResult> CommitAsync(
            RawProgressCommitRequest request,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    ///     <para xml:lang="en">Provides the process-wide RitsuLib raw-progress bridge service.</para>
    ///     <para xml:lang="zh-CN">提供进程级的 RitsuLib 原始进度桥接服务。</para>
    /// </summary>
    public static class RawProgressBridge
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the shared provider instance.</para>
        ///     <para xml:lang="zh-CN">获取共享的提供方实例。</para>
        /// </summary>
        public static IRawProgressCommitBridge Instance { get; } = RawProgressCommitBridge.Instance;
    }
}
