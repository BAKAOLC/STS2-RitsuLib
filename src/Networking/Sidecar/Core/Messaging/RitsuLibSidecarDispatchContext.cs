using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Context for a received Sidecar envelope after magic detection, length checks, optional decompression, and
    ///         opcode dispatch. Payload and header extension memory may reference the transient receive buffer until the
    ///         callback returns; use <see cref="WithOwnedEnvelopeMemory" /> before deferring work.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         Sidecar 信封经过魔数检测、长度检查、可选解压和操作码分发后形成的接收上下文。
    ///         载荷和标头扩展可能引用仅在回调期间有效的临时接收缓冲区；延后处理前请使用
    ///         <see cref="WithOwnedEnvelopeMemory" />。
    ///     </para>
    /// </summary>
    public readonly struct RitsuLibSidecarDispatchContext
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a dispatch context for an opcode handler.</para>
        ///     <para xml:lang="zh-CN">为操作码处理器创建分发上下文。</para>
        /// </summary>
        /// <param name="senderNetId">
        ///     <para xml:lang="en">Vanilla sender peer ID from the receive callback.</para>
        ///     <para xml:lang="zh-CN">来自接收回调的游戏原版发送方对等端 ID。</para>
        /// </param>
        /// <param name="transferMode">
        ///     <para xml:lang="en">Reliable or unreliable as reported by the transport.</para>
        ///     <para xml:lang="zh-CN">传输层报告的可靠或不可靠模式。</para>
        /// </param>
        /// <param name="channel">
        ///     <para xml:lang="en">ENet channel the packet arrived on.</para>
        ///     <para xml:lang="zh-CN">数据包到达时使用的 ENet 通道。</para>
        /// </param>
        /// <param name="isHostIngest">
        ///     <para xml:lang="en">Whether the host service received the packet.</para>
        ///     <para xml:lang="zh-CN">数据包是否由主机服务接收。</para>
        /// </param>
        /// <param name="envelope">
        ///     <para xml:lang="en">Parsed Sidecar envelope for this packet.</para>
        ///     <para xml:lang="zh-CN">从此数据包解析出的 Sidecar 信封。</para>
        /// </param>
        public RitsuLibSidecarDispatchContext(
            ulong senderNetId,
            NetTransferMode transferMode,
            int channel,
            bool isHostIngest,
            RitsuLibSidecarEnvelope.ParsedEnvelope envelope)
        {
            SenderNetId = senderNetId;
            TransferMode = transferMode;
            Channel = channel;
            IsHostIngest = isHostIngest;
            Envelope = envelope;
        }

        /// <summary>
        ///     <para xml:lang="en">Sender ID from the vanilla transport callback.</para>
        ///     <para xml:lang="zh-CN">来自游戏原版传输回调的发送方 ID。</para>
        /// </summary>
        public ulong SenderNetId { get; }

        /// <summary>
        ///     <para xml:lang="en">Reliable or unreliable delivery mode.</para>
        ///     <para xml:lang="zh-CN">可靠或不可靠投递模式。</para>
        /// </summary>
        public NetTransferMode TransferMode { get; }

        /// <summary>
        ///     <para xml:lang="en">ENet channel index.</para>
        ///     <para xml:lang="zh-CN">ENet 通道索引。</para>
        /// </summary>
        public int Channel { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when this packet was received by
        ///         <see cref="MegaCrit.Sts2.Core.Multiplayer.NetHostGameService" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         此数据包由 <see cref="MegaCrit.Sts2.Core.Multiplayer.NetHostGameService" /> 接收时为
        ///         <see langword="true" />。
        ///     </para>
        /// </summary>
        public bool IsHostIngest { get; }

        /// <summary>
        ///     <para xml:lang="en">Full parsed envelope.</para>
        ///     <para xml:lang="zh-CN">完整解析后的信封。</para>
        /// </summary>
        public RitsuLibSidecarEnvelope.ParsedEnvelope Envelope { get; }

        /// <summary>
        ///     <para xml:lang="en">Shortcut for <see cref="RitsuLibSidecarEnvelope.ParsedEnvelope.Opcode" />.</para>
        ///     <para xml:lang="zh-CN"><see cref="RitsuLibSidecarEnvelope.ParsedEnvelope.Opcode" /> 的快捷属性。</para>
        /// </summary>
        public ulong Opcode => Envelope.Opcode;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Logical payload memory from <see cref="Envelope" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         来自 <see cref="Envelope" /> 的逻辑载荷内存。
        ///     </para>
        /// </summary>
        public ReadOnlyMemory<byte> Payload => Envelope.Payload;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Copies the header extension and logical payload into new arrays so the context stays valid after the
        ///         multiplayer receive callback returns or when work is deferred to the Godot main loop.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将标头扩展和逻辑载荷复制到新数组中，使上下文在多人接收回调返回后，
        ///         或处理延后到 Godot 主循环时仍保持有效。
        ///     </para>
        /// </summary>
        public RitsuLibSidecarDispatchContext WithOwnedEnvelopeMemory()
        {
            var ext = Envelope.HeaderExtension.Length == 0
                ? ReadOnlyMemory<byte>.Empty
                : Envelope.HeaderExtension.ToArray();
            var pay = Envelope.Payload.Length == 0
                ? ReadOnlyMemory<byte>.Empty
                : Envelope.Payload.ToArray();
            var owned = new RitsuLibSidecarEnvelope.ParsedEnvelope(
                Envelope.WireFormatVersion,
                Envelope.Flags,
                Envelope.Opcode,
                ext,
                pay);
            return new(SenderNetId, TransferMode, Channel, IsHostIngest, owned);
        }
    }
}
