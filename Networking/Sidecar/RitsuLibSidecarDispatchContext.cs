using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Arguments for a received sidecar envelope after magic detection, length checks, optional decompression, and
    ///     opcode dispatch. Payload and header extension memory may reference the transient receive buffer until the
    ///     callback returns; use <see cref="WithOwnedEnvelopeMemory" /> before deferring work.
    /// </summary>
    public readonly struct RitsuLibSidecarDispatchContext
    {
        /// <summary>Creates a dispatch context for an opcode handler.</summary>
        /// <param name="senderNetId">Vanilla sender peer id from the receive callback.</param>
        /// <param name="transferMode">Reliable or unreliable as reported by the transport.</param>
        /// <param name="channel">ENet channel the packet arrived on.</param>
        /// <param name="isHostIngest">True when the host service received the packet.</param>
        /// <param name="envelope">Parsed sidecar envelope for this packet.</param>
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

        /// <summary>Sender id from the vanilla transport callback.</summary>
        public ulong SenderNetId { get; }

        /// <summary>Reliable or unreliable delivery mode.</summary>
        public NetTransferMode TransferMode { get; }

        /// <summary>ENet channel index.</summary>
        public int Channel { get; }

        /// <summary>
        ///     True when this packet was ingested on <see cref="MegaCrit.Sts2.Core.Multiplayer.NetHostGameService" />.
        /// </summary>
        public bool IsHostIngest { get; }

        /// <summary>Full parsed envelope.</summary>
        public RitsuLibSidecarEnvelope.ParsedEnvelope Envelope { get; }

        /// <summary>Convenience: <see cref="RitsuLibSidecarEnvelope.ParsedEnvelope.Opcode" />.</summary>
        public ulong Opcode => Envelope.Opcode;

        /// <summary>Convenience: logical payload memory.</summary>
        public ReadOnlyMemory<byte> Payload => Envelope.Payload;

        /// <summary>
        ///     Copies header extension and logical payload into new arrays so the context stays valid after the
        ///     multiplayer receive callback returns or when work is deferred to the Godot main loop.
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
