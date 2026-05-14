using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Arguments for a received sidecar envelope after magic detection, length checks, optional decompression, and
    ///     opcode dispatch. Payload and header extension memory may reference the transient receive buffer until the
    ///     callback returns; use <see cref="WithOwnedEnvelopeMemory" /> before deferring work.
    ///     经过 magic 检测、长度检查、可选解压和
    ///     opcode 分发后的已接收 sidecar envelope 参数。载荷和 header 扩展内存可能在
    ///     回调返回前引用临时接收缓冲区；延后工作前请使用 <see cref="WithOwnedEnvelopeMemory" />。
    /// </summary>
    public readonly struct RitsuLibSidecarDispatchContext
    {
        /// <summary>
        ///     Creates a dispatch context for an opcode handler.
        ///     为 opcode 处理器创建分发上下文。
        /// </summary>
        /// <param name="senderNetId">
        ///     Vanilla sender peer id from the receive callback.
        ///     来自接收回调的原版发送方 peer id。
        /// </param>
        /// <param name="transferMode">
        ///     Reliable or unreliable as reported by the transport.
        ///     传输层报告的可靠或不可靠模式。
        /// </param>
        /// <param name="channel">
        ///     ENet channel the packet arrived on.
        ///     数据包到达的 ENet channel。
        /// </param>
        /// <param name="isHostIngest">
        ///     True when the host service received the packet.
        ///     主机服务收到数据包时为 true。
        /// </param>
        /// <param name="envelope">
        ///     Parsed sidecar envelope for this packet.
        ///     此数据包解析出的 sidecar envelope。
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
        ///     Sender id from the vanilla transport callback.
        ///     来自原版传输回调的发送方 id。
        /// </summary>
        public ulong SenderNetId { get; }

        /// <summary>
        ///     Reliable or unreliable delivery mode.
        ///     可靠或不可靠投递模式。
        /// </summary>
        public NetTransferMode TransferMode { get; }

        /// <summary>
        ///     ENet channel index.
        ///     ENet channel 索引。
        /// </summary>
        public int Channel { get; }

        /// <summary>
        ///     True when this packet was ingested on <see cref="MegaCrit.Sts2.Core.Multiplayer.NetHostGameService" />.
        ///     当此数据包在 <see cref="MegaCrit.Sts2.Core.Multiplayer.NetHostGameService" /> 上被接收时为 True。
        /// </summary>
        public bool IsHostIngest { get; }

        /// <summary>
        ///     Full parsed envelope.
        ///     完整解析后的 envelope。
        /// </summary>
        public RitsuLibSidecarEnvelope.ParsedEnvelope Envelope { get; }

        /// <summary>
        ///     Convenience: <see cref="RitsuLibSidecarEnvelope.ParsedEnvelope.Opcode" />.
        ///     便捷属性：<see cref="RitsuLibSidecarEnvelope.ParsedEnvelope.Opcode" />。
        /// </summary>
        public ulong Opcode => Envelope.Opcode;

        /// <summary>
        ///     Convenience: logical payload memory.
        ///     Convenience: logical payload memory.
        ///     便捷属性：逻辑载荷内存。
        ///     便捷属性：逻辑载荷内存。
        /// </summary>
        public ReadOnlyMemory<byte> Payload => Envelope.Payload;

        /// <summary>
        ///     Copies header extension and logical payload into new arrays so the context stays valid after the
        ///     multiplayer receive callback returns or when work is deferred to the Godot main loop.
        ///     将 header 扩展和逻辑载荷复制到新数组中，使上下文在
        ///     多人接收回调返回后，或工作延后到 Godot 主循环时仍保持有效。
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
