namespace STS2RitsuLib.Multiplayer.ChunkedPayload
{
    /// <summary>
    ///     Fired when a full payload is available. The buffer is owned for the duration of the event; copy if you need it
    ///     beyond the handler.
    /// </summary>
    public sealed class ChunkedPayloadReceivedEventArgs : EventArgs
    {
        /// <summary>
        ///     Creates event args for a completed transfer.
        /// </summary>
        public ChunkedPayloadReceivedEventArgs(ulong senderNetId, ushort streamId, ulong transferId, byte[] payload)
        {
            SenderNetId = senderNetId;
            StreamId = streamId;
            TransferId = transferId;
            Payload = payload;
        }

        /// <summary>
        ///     Peer that originated the transfer.
        /// </summary>
        public ulong SenderNetId { get; }

        /// <summary>
        ///     Logical channel.
        /// </summary>
        public ushort StreamId { get; }

        /// <summary>
        ///     Transfer id from wire.
        /// </summary>
        public ulong TransferId { get; }

        /// <summary>
        ///     Reassembled, CRC-verified buffer.
        /// </summary>
        public byte[] Payload { get; }
    }

    /// <summary>
    ///     Fired when a transfer is abandoned after partial progress or when send validation fails.
    /// </summary>
    public sealed class ChunkedPayloadFailedEventArgs : EventArgs
    {
        /// <summary>
        ///     Creates failure args.
        /// </summary>
        public ChunkedPayloadFailedEventArgs(
            ulong? senderNetId,
            ushort streamId,
            ulong? transferId,
            ChunkedTransferFailureReason reason,
            string? detail = null)
        {
            SenderNetId = senderNetId;
            StreamId = streamId;
            TransferId = transferId;
            Reason = reason;
            Detail = detail;
        }

        /// <summary>
        ///     Peer, if known.
        /// </summary>
        public ulong? SenderNetId { get; }

        /// <summary>
        ///     Logical channel.
        /// </summary>
        public ushort StreamId { get; }

        /// <summary>
        ///     Transfer id, if known.
        /// </summary>
        public ulong? TransferId { get; }

        /// <summary>
        ///     Failure classification.
        /// </summary>
        public ChunkedTransferFailureReason Reason { get; }

        /// <summary>
        ///     Optional diagnostic text.
        /// </summary>
        public string? Detail { get; }
    }
}
