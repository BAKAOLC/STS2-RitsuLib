using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Maps <see cref="RitsuLibSidecarDeliverySemantics" /> to a transport mode and channel.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将 <see cref="RitsuLibSidecarDeliverySemantics" /> 映射到传输模式和通道。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarNetworkMapping
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Maps <see cref="RitsuLibSidecarDeliverySemantics.BestEffort" /> to unreliable transport and the
        ///         best-effort channel; all other values use reliable transport and the sidecar synchronization channel.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="RitsuLibSidecarDeliverySemantics.BestEffort" /> 映射到不可靠传输和尽力而为通道；
        ///         其他值均使用可靠传输和 sidecar 同步通道。
        ///     </para>
        /// </summary>
        /// <param name="semantics">
        ///     <para xml:lang="en">The requested delivery semantics.</para>
        ///     <para xml:lang="zh-CN">请求的投递语义。</para>
        /// </param>
        /// <param name="mode">
        ///     <para xml:lang="en">The resulting transport mode.</para>
        ///     <para xml:lang="zh-CN">生成的传输模式。</para>
        /// </param>
        /// <param name="channel">
        ///     <para xml:lang="en">The resulting channel index.</para>
        ///     <para xml:lang="zh-CN">生成的通道索引。</para>
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
