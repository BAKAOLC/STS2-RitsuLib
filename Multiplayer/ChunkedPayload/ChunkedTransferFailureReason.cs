namespace STS2RitsuLib.Multiplayer.ChunkedPayload
{
    /// <summary>
    ///     Why an incoming transfer was abandoned or a send was rejected.
    /// </summary>
    public enum ChunkedTransferFailureReason
    {
        /// <summary>
        ///     Caller cancelled the send via <see cref="System.Threading.CancellationToken" />.
        /// </summary>
        Cancelled,

        /// <summary>
        ///     Payload exceeds <see cref="ChunkedTransferOptions.MaxTotalPayloadBytes" />.
        /// </summary>
        PayloadTooLarge,

        /// <summary>
        ///     Computed chunk count or wire header is inconsistent with options.
        /// </summary>
        InvalidLayout,

        /// <summary>
        ///     Reassembled bytes did not match the declared CRC32.
        /// </summary>
        CrcMismatch,

        /// <summary>
        ///     No progress until <see cref="ChunkedTransferOptions.TransferTimeout" /> elapsed.
        /// </summary>
        TimedOut,

        /// <summary>
        ///     Too many incomplete transfers from distinct peers / ids (memory protection).
        /// </summary>
        TooManyConcurrentTransfers,

        /// <summary>
        ///     Sum of buffered incomplete transfers would exceed memory ceiling.
        /// </summary>
        ReassemblyMemoryCap,

        /// <summary>
        ///     Schema or stream mismatch (unsupported protocol version).
        /// </summary>
        UnsupportedOrCorrupt,

        /// <summary>
        ///     Not connected; cannot send.
        /// </summary>
        NotConnected,

        /// <summary>
        ///     Client peers cannot address a specific <c>targetPeerId</c>; use host-only sends.
        /// </summary>
        InvalidSendTarget,
    }
}
