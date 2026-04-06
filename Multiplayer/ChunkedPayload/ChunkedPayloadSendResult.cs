namespace STS2RitsuLib.Multiplayer.ChunkedPayload
{
    /// <summary>
    ///     Result of a synchronous multi-fragment send.
    /// </summary>
    public readonly struct ChunkedPayloadSendResult
    {
        /// <summary>
        ///     Creates a result value.
        /// </summary>
        public ChunkedPayloadSendResult(bool ok, int fragmentsSent, ChunkedTransferFailureReason? failure,
            string? detail = null)
        {
            Ok = ok;
            FragmentsSent = fragmentsSent;
            Failure = failure;
            Detail = detail;
        }

        /// <summary>
        ///     True if every fragment was accepted by the transport.
        /// </summary>
        public bool Ok { get; }

        /// <summary>
        ///     Fragments successfully queued (partial count on cancellation).
        /// </summary>
        public int FragmentsSent { get; }

        /// <summary>
        ///     Set when <see cref="Ok" /> is false.
        /// </summary>
        public ChunkedTransferFailureReason? Failure { get; }

        /// <summary>
        ///     Optional diagnostic when <see cref="Ok" /> is false.
        /// </summary>
        public string? Detail { get; }

        /// <summary>
        ///     Successful send.
        /// </summary>
        public static ChunkedPayloadSendResult Success(int fragments)
        {
            return new(true, fragments, null);
        }

        /// <summary>
        ///     Failed send.
        /// </summary>
        public static ChunkedPayloadSendResult Fail(ChunkedTransferFailureReason reason, string? detail = null,
            int sent = 0)
        {
            return new(false, sent, reason, detail);
        }
    }
}
