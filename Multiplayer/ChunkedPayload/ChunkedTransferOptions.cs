namespace STS2RitsuLib.Multiplayer.ChunkedPayload
{
    /// <summary>
    ///     Tunables for chunk sizing, timeouts, pacing, and resource limits.
    /// </summary>
    public sealed class ChunkedTransferOptions
    {
        /// <summary>
        ///     When set, successful payloads are dispatched through this delegate (e.g. Godot main thread).
        ///     If null, <see cref="ChunkedPayloadReceivedEventArgs" /> is raised synchronously on the net thread.
        /// </summary>
        public Action<Action>? CompletionDispatcher;

        /// <summary>
        ///     Invoked on the same thread as the net message bus unless <see cref="CompletionDispatcher" /> is set.
        /// </summary>
        public EventHandler<ChunkedPayloadFailedEventArgs>? TransferFailed;

        /// <summary>
        ///     Maximum size of each fragment’s raw payload (excluding outer net message framing).
        ///     Smaller values reduce loss impact and ease congestion on poor links; larger values reduce overhead.
        /// </summary>
        public int MaxFragmentPayloadBytes { get; init; } = 6144;

        /// <summary>
        ///     Upper bound on a single logical payload after reassembly.
        /// </summary>
        public int MaxTotalPayloadBytes { get; init; } = 16 * 1024 * 1024;

        /// <summary>
        ///     Wall-clock timeout for an incomplete transfer after the first fragment is seen.
        /// </summary>
        public TimeSpan TransferTimeout { get; init; } = TimeSpan.FromSeconds(45);

        /// <summary>
        ///     Optional delay between sending fragments to avoid bursting the reliable channel on bad networks.
        /// </summary>
        public TimeSpan InterFragmentDelay { get; init; } = TimeSpan.Zero;

        /// <summary>
        ///     Maximum number of incomplete incoming transfers (distinct sender + transfer id) tracked at once.
        /// </summary>
        public int MaxConcurrentIncomingTransfers { get; init; } = 32;

        /// <summary>
        ///     Approximate cap on bytes reserved for incomplete reassembly buffers (excluding overhead).
        /// </summary>
        public long MaxReassemblyBytesInFlight { get; init; } = 64 * 1024 * 1024;
    }
}
