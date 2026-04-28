namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     How a sidecar payload should be sent and (by convention) interpreted. The first byte of
    ///     <see cref="RitsuLibSidecarEnvelope.ParsedEnvelope.HeaderExtension" />, when present, records this; see
    ///     <see cref="RitsuLibSidecarHeaderExtension.GetDeliveryOrUnspecified" />.
    /// </summary>
    public enum RitsuLibSidecarDeliverySemantics : byte
    {
        /// <summary>
        ///     Unordered / loss-tolerant: map to unreliable transport and a best-effort channel. Handlers may run
        ///     as soon as the frame arrives; no cross-stream ordering is implied.
        /// </summary>
        BestEffort = 0,

        /// <summary>
        ///     Reliable, ordered with respect to other reliable sidecar traffic on the same ENet stream; use for
        ///     state that must not diverge (same spirit as vanilla game action sync, but for sidecar payloads only).
        /// </summary>
        StableSync = 1,

        /// <summary>Header extension omits a delivery tag; treated like <see cref="StableSync" /> for send helpers.</summary>
        Unspecified = 0xFF,
    }
}
