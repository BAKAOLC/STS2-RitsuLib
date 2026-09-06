namespace STS2RitsuLib.Saves.RawProgress
{
    /// <summary>
    ///     <para xml:lang="en">Identifies a save namespace that can contain a progress document.</para>
    ///     <para xml:lang="zh-CN">标识可包含进度文档的存档命名空间。</para>
    /// </summary>
    public enum RawProgressEnvironment
    {
        /// <summary>
        ///     <para xml:lang="en">The unmodded game save namespace.</para>
        ///     <para xml:lang="zh-CN">未加载模组时使用的游戏存档命名空间。</para>
        /// </summary>
        Vanilla,

        /// <summary>
        ///     <para xml:lang="en">The modded game save namespace.</para>
        ///     <para xml:lang="zh-CN">加载模组时使用的游戏存档命名空间。</para>
        /// </summary>
        Modded,
    }

    /// <summary>
    ///     <para xml:lang="en">Selects one profile and save namespace without changing the active game profile.</para>
    ///     <para xml:lang="zh-CN">选择一个档案及存档命名空间，且不改变游戏当前的活动档案。</para>
    /// </summary>
    public sealed record RawProgressDestination
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the profile ID. Valid game profiles are numbered from 1 through 3.</para>
        ///     <para xml:lang="zh-CN">获取档案 ID。有效的游戏档案编号为 1 至 3。</para>
        /// </summary>
        public required int ProfileId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the selected save namespace.</para>
        ///     <para xml:lang="zh-CN">获取所选存档命名空间。</para>
        /// </summary>
        public required RawProgressEnvironment Environment { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Describes independently negotiable capabilities of the targeted raw-progress bridge.</para>
    ///     <para xml:lang="zh-CN">描述目标化原始进度桥接器中可独立协商的能力。</para>
    /// </summary>
    [Flags]
    public enum TargetedRawProgressBridgeFeature
    {
        /// <summary>
        ///     <para xml:lang="en">Callers can capture a progress document selected by profile and save namespace.</para>
        ///     <para xml:lang="zh-CN">调用方可以按档案及存档命名空间捕获进度文档。</para>
        /// </summary>
        ExplicitDestinationCapture = 1 << 0,

        /// <summary>
        ///     <para xml:lang="en">A selected destination can be committed while it is not the active progress document.</para>
        ///     <para xml:lang="zh-CN">所选目标并非活动进度文档时仍可提交。</para>
        /// </summary>
        InactiveDestinationCommit = 1 << 1,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Both the selected source and destination generations are rechecked in the commit's exclusive save window.
        ///     </para>
        ///     <para xml:lang="zh-CN">所选来源与目标的代次都会在提交的独占保存窗口内重新检查。</para>
        /// </summary>
        SourceAndDestinationGenerationCheck = 1 << 2,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Committing an inactive destination does not replace or attach preservation state to the active in-memory
        ///         progress document.
        ///     </para>
        ///     <para xml:lang="zh-CN">提交非活动目标时，不会替换活动内存进度文档，也不会向其附加未知属性保留状态。</para>
        /// </summary>
        ActiveProgressStateIsolation = 1 << 3,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Existing owner-scoped recovery operations locate the destination recorded by each journal, including an
        ///         inactive destination.
        ///     </para>
        ///     <para xml:lang="zh-CN">现有的所有者范围恢复操作会定位每份日志所记录的目标，包括非活动目标。</para>
        /// </summary>
        TargetedRecoveryJournalManagement = 1 << 4,

        /// <summary>
        ///     <para xml:lang="en">The targeted capability is exposed through a documented public contract.</para>
        ///     <para xml:lang="zh-CN">目标化能力通过有文档说明的公共契约公开。</para>
        /// </summary>
        StablePublicContract = 1 << 5,
    }

    /// <summary>
    ///     <para xml:lang="en">Describes the targeted raw-progress provider and its current limits.</para>
    ///     <para xml:lang="zh-CN">描述目标化原始进度提供方及其当前限制。</para>
    /// </summary>
    public sealed record TargetedRawProgressBridgeDescriptor
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
        ///     <para xml:lang="en">Gets the exact targeted request and result protocol version.</para>
        ///     <para xml:lang="zh-CN">获取目标化请求与结果使用的精确协议版本。</para>
        /// </summary>
        public required int ProtocolVersion { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the exact base commit protocol version required by destination commit requests.</para>
        ///     <para xml:lang="zh-CN">获取目标提交请求所需的精确基础提交协议版本。</para>
        /// </summary>
        public required int BaseProtocolVersion { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the progress schemas currently available for targeted operations.</para>
        ///     <para xml:lang="zh-CN">获取当前可用于目标化操作的进度 schema。</para>
        /// </summary>
        public required IReadOnlySet<int> SupportedSchemas { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the save namespaces accepted by the provider.</para>
        ///     <para xml:lang="zh-CN">获取提供方接受的存档命名空间。</para>
        /// </summary>
        public required IReadOnlySet<RawProgressEnvironment> SupportedEnvironments { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the advertised targeted capabilities.</para>
        ///     <para xml:lang="zh-CN">获取声明的目标化能力集合。</para>
        /// </summary>
        public required TargetedRawProgressBridgeFeature Features { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the base raw-progress capabilities used by targeted commits.</para>
        ///     <para xml:lang="zh-CN">获取目标化提交所使用的基础原始进度能力。</para>
        /// </summary>
        public required RawProgressBridgeFeature BaseFeatures { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the largest accepted UTF-8 document size in bytes.</para>
        ///     <para xml:lang="zh-CN">获取允许提交的 UTF-8 文档最大字节数。</para>
        /// </summary>
        public required long MaxDocumentUtf8Bytes { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the maximum number of recovery journals retained across all owners.</para>
        ///     <para xml:lang="zh-CN">获取所有所有者合计可保留的恢复日志数量上限。</para>
        /// </summary>
        public required int MaxRetainedRecoveryJournals { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the largest accepted UTF-8 owner identifier size in bytes.</para>
        ///     <para xml:lang="zh-CN">获取允许使用的 UTF-8 所有者标识最大字节数。</para>
        /// </summary>
        public required int MaxRecoveryOwnerIdUtf8Bytes { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Requests one conditional destination commit guarded by independently captured source and destination
    ///         generations.
    ///     </para>
    ///     <para xml:lang="zh-CN">请求一次由独立捕获的来源与目标代次共同保护的条件目标提交。</para>
    /// </summary>
    public sealed record TargetedRawProgressCommitRequest
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the exact targeted protocol version expected by the caller.</para>
        ///     <para xml:lang="zh-CN">获取调用方要求的精确目标化协议版本。</para>
        /// </summary>
        public required int ProtocolVersion { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the source whose captured generation protects the prepared proposal.</para>
        ///     <para xml:lang="zh-CN">获取以其捕获代次保护已准备提案的来源。</para>
        /// </summary>
        public required RawProgressDestination Source { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the source generation that must still be current before mutation.</para>
        ///     <para xml:lang="zh-CN">获取在修改前必须仍然有效的来源代次。</para>
        /// </summary>
        public required ProgressGeneration ExpectedSourceGeneration { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the destination to replace.</para>
        ///     <para xml:lang="zh-CN">获取要替换的目标。</para>
        /// </summary>
        public required RawProgressDestination Destination { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the base commit request containing the expected destination generation, owner, transaction, and
        ///         complete proposed document. Its protocol version must match the base descriptor.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取包含预期目标代次、所有者、事务及完整拟提交文档的基础提交请求。其协议版本必须与基础描述符匹配。
        ///     </para>
        /// </summary>
        public required RawProgressCommitRequest DestinationCommit { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Reports whether the targeted pre-mutation guards passed.</para>
    ///     <para xml:lang="zh-CN">报告目标化操作的修改前保护检查是否通过。</para>
    /// </summary>
    public enum TargetedRawProgressCommitGuardOutcome
    {
        /// <summary>
        ///     <para xml:lang="en">Every guard passed and <see cref="TargetedRawProgressCommitResult.CommitResult" /> is present.</para>
        ///     <para xml:lang="zh-CN">所有保护检查均已通过，且 <see cref="TargetedRawProgressCommitResult.CommitResult" /> 存在。</para>
        /// </summary>
        Passed,

        /// <summary>
        ///     <para xml:lang="en">The caller requested an incompatible targeted or base protocol.</para>
        ///     <para xml:lang="zh-CN">调用方请求了不兼容的目标化协议或基础协议。</para>
        /// </summary>
        ProviderIncompatible,

        /// <summary>
        ///     <para xml:lang="en">The proposed or selected document uses an unsupported progress schema.</para>
        ///     <para xml:lang="zh-CN">拟提交文档或所选文档使用了不受支持的进度 schema。</para>
        /// </summary>
        SchemaUnsupported,

        /// <summary>
        ///     <para xml:lang="en">The request, destination binding, generation, or proposed document was invalid.</para>
        ///     <para xml:lang="zh-CN">请求、目标绑定、代次或拟提交文档无效。</para>
        /// </summary>
        ValidationFailed,

        /// <summary>
        ///     <para xml:lang="en">Cancellation was observed before recovery metadata or the destination could change.</para>
        ///     <para xml:lang="zh-CN">在恢复元数据或目标可能改变之前收到取消请求。</para>
        /// </summary>
        CancelledBeforeCommit,

        /// <summary>
        ///     <para xml:lang="en">The selected source could not be captured and validated.</para>
        ///     <para xml:lang="zh-CN">无法捕获并验证所选来源。</para>
        /// </summary>
        SourceUnavailable,

        /// <summary>
        ///     <para xml:lang="en">The selected source no longer matches its expected generation.</para>
        ///     <para xml:lang="zh-CN">所选来源不再匹配其预期代次。</para>
        /// </summary>
        SourceGenerationConflict,

        /// <summary>
        ///     <para xml:lang="en">The selected destination could not be captured and validated.</para>
        ///     <para xml:lang="zh-CN">无法捕获并验证所选目标。</para>
        /// </summary>
        DestinationUnavailable,

        /// <summary>
        ///     <para xml:lang="en">The selected destination no longer matches its expected generation.</para>
        ///     <para xml:lang="zh-CN">所选目标不再匹配其预期代次。</para>
        /// </summary>
        DestinationGenerationConflict,
    }

    /// <summary>
    ///     <para xml:lang="en">Contains source and destination guard evidence plus any attempted commit result.</para>
    ///     <para xml:lang="zh-CN">包含来源与目标保护检查证据，以及存在时的提交尝试结果。</para>
    /// </summary>
    public sealed record TargetedRawProgressCommitResult
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the pre-mutation guard outcome. When this is not
        ///         <see cref="TargetedRawProgressCommitGuardOutcome.Passed" />, no recovery metadata or destination was changed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取修改前保护检查结果。当其不是 <see cref="TargetedRawProgressCommitGuardOutcome.Passed" /> 时，恢复元数据与目标均未改变。
        ///     </para>
        /// </summary>
        public required TargetedRawProgressCommitGuardOutcome GuardOutcome { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the source or destination read failure when a selected document was unavailable; otherwise
        ///         <see langword="null" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取所选来源或目标不可用时的读取失败结果；其他情况下为 <see langword="null" />。</para>
        /// </summary>
        public RawProgressReadOutcome? ReadFailure { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets detailed mutation and verification evidence only after every guard passed. A retained recovery journal
        ///         and all partial outcomes use the same bounded recovery contract as an active-document commit.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         仅在所有保护检查均通过后获取详细的修改与验证证据。保留的恢复日志及所有部分成功结果与活动文档提交使用相同的有界恢复契约。
        ///     </para>
        /// </summary>
        public RawProgressCommitResult? CommitResult { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Extends the active-document bridge with fail-closed operations for explicitly selected progress destinations.
    ///     </para>
    ///     <para xml:lang="zh-CN">在活动文档桥接器基础上，为显式选择的进度目标提供保守失败操作。</para>
    /// </summary>
    public interface ITargetedRawProgressCommitBridge : IRawProgressCommitBridge
    {
        /// <summary>
        ///     <para xml:lang="en">Returns immutable targeted provider metadata without reading or changing a profile.</para>
        ///     <para xml:lang="zh-CN">返回不可变的目标化提供方元数据，且不读取或改变任何档案。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The targeted provider descriptor.</para>
        ///     <para xml:lang="zh-CN">目标化提供方描述符。</para>
        /// </returns>
        TargetedRawProgressBridgeDescriptor DescribeTargeted();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Captures the complete selected document and its generation inside the shared exclusive save window without
        ///         changing the active profile. A persisted cloud copy must be readable or the operation fails closed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在共享独占保存窗口内捕获所选完整文档及其代次，且不改变活动档案。若存在已持久化云端副本但无法读回，操作会保守失败。
        ///     </para>
        /// </summary>
        /// <param name="destination">
        ///     <para xml:lang="en">The profile and save namespace to capture.</para>
        ///     <para xml:lang="zh-CN">要捕获的档案及存档命名空间。</para>
        /// </param>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">Cancellation observed before the snapshot is captured.</para>
        ///     <para xml:lang="zh-CN">在捕获快照前接受的取消信号。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The selected snapshot result.</para>
        ///     <para xml:lang="zh-CN">所选目标的快照结果。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="destination" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="destination" /> 为 <see langword="null" />。</para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en">The profile ID or save namespace is outside the supported range.</para>
        ///     <para xml:lang="zh-CN">档案 ID 或存档命名空间超出支持范围。</para>
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     <para xml:lang="en">The operation was cancelled before capture completed.</para>
        ///     <para xml:lang="zh-CN">操作在捕获完成前被取消。</para>
        /// </exception>
        ValueTask<RawProgressReadResult> CaptureAsync(
            RawProgressDestination destination,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Rechecks the selected source and destination inside one exclusive save window, then conditionally commits
        ///         the complete destination document. Cancellation is honored only before recovery metadata or the destination
        ///         can change. An inactive destination is committed without replacing the active in-memory progress state.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在同一独占保存窗口内重新检查所选来源与目标，然后有条件地提交完整目标文档。取消信号仅会在恢复元数据或目标可能改变之前生效。提交非活动目标时不会替换活动内存进度状态。
        ///     </para>
        /// </summary>
        /// <param name="request">
        ///     <para xml:lang="en">The source guard, selected destination, and destination commit request.</para>
        ///     <para xml:lang="zh-CN">来源保护条件、所选目标及目标提交请求。</para>
        /// </param>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">Cancellation observed only before mutation begins.</para>
        ///     <para xml:lang="zh-CN">仅在修改开始前接受的取消信号。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">Structured guard, verification, and recovery evidence.</para>
        ///     <para xml:lang="zh-CN">结构化的保护检查、验证与恢复证据。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="request" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="request" /> 为 <see langword="null" />。</para>
        /// </exception>
        ValueTask<TargetedRawProgressCommitResult> CommitAsync(
            TargetedRawProgressCommitRequest request,
            CancellationToken cancellationToken = default);
    }
}
