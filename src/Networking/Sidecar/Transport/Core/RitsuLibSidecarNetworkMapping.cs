using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Maps <see cref="RitsuLibSidecarDeliverySemantics" /> to ENet/Steam <see cref="NetTransferMode" /> and channel.
    ///     将 <see cref="RitsuLibSidecarDeliverySemantics" /> 映射到 ENet/Steam <see cref="NetTransferMode" /> 和 channel。
    /// </summary>
    public static class RitsuLibSidecarNetworkMapping
    {
        /// <summary>
        ///     <see cref="RitsuLibSidecarDeliverySemantics.BestEffort" /> → unreliable + best-effort channel; all other
        ///     values (including <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" />) → reliable + sidecar
        ///     reliable channel.
        ///     <see cref="RitsuLibSidecarDeliverySemantics.BestEffort" /> → unreliable + best-effort channel；所有其他
        ///     值（包括 <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" />）→ reliable + sidecar
        ///     reliable channel。
        /// </summary>
        /// <param name="semantics">
        ///     Delivery intent from the envelope extension or caller.
        ///     来自 envelope 扩展或调用方的投递意图。
        /// </param>
        /// <param name="mode">
        ///     Resulting <see cref="NetTransferMode" /> for the vanilla send API.
        ///     用于原版发送 API 的结果 <see cref="NetTransferMode" />。
        /// </param>
        /// <param name="channel">
        ///     ENet channel index for the vanilla send API.
        ///     用于原版发送 API 的 ENet channel 索引。
        /// </param>
        public static void GetNetworkParameters(
            RitsuLibSidecarDeliverySemantics semantics,
            out NetTransferMode mode,
            out int channel)
        {
            if (semantics is RitsuLibSidecarDeliverySemantics.BestEffort)
            {
                mode = NetTransferMode.Unreliable;
                channel = RitsuLibSidecarWire.RecommendedUnreliableChannel;
            }
            else
            {
                mode = NetTransferMode.Reliable;
                channel = RitsuLibSidecarWire.RecommendedReliableChannel;
            }
        }
    }
}
