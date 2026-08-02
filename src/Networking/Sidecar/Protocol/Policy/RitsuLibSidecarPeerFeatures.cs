namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines the feature flags advertised during a <see cref="RitsuLibSidecarHandshakeBinary" /> handshake.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义通过 <see cref="RitsuLibSidecarHandshakeBinary" /> 握手声明的功能标志。
    ///     </para>
    /// </summary>
    [Flags]
    public enum RitsuLibSidecarPeerFeatures : uint
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         No optional features are advertised.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         不声明任何可选功能。
        ///     </para>
        /// </summary>
        None = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Supports reassembling large payloads sent through
        ///         <see cref="RitsuLibSidecarControlOpcodes.ChunkedFrame" /> messages.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         支持重组通过 <see cref="RitsuLibSidecarControlOpcodes.ChunkedFrame" /> 消息发送的大型载荷。
        ///     </para>
        /// </summary>
        ChunkedStreams = 1 << 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Supports carrying RitsuLib-managed actions in vanilla action-enqueue messages.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         支持在原版动作入队消息中承载由 RitsuLib 管理的动作。
        ///     </para>
        /// </summary>
        ManagedNetActions = 1 << 1,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Supports Brotli-compressed sidecar envelope payloads.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         支持使用 Brotli 压缩的 sidecar 信封载荷。
        ///     </para>
        /// </summary>
        BrotliPayloadCompression = 1 << 2,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Supports source-aware model right-click actions, including actions for active combat orbs.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         支持保留来源信息的模型右键操作，包括对战斗中充能球的操作。
        ///     </para>
        /// </summary>
        ModelRightClickV2 = 1 << 3,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Indicates that this player can apply host-approved state changes made with RitsuLib developer tools.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         表示该玩家能够应用由主机批准的 RitsuLib 开发者工具状态修改。
        ///     </para>
        /// </summary>
        DeveloperActionsV1 = 1 << 4,
    }
}
