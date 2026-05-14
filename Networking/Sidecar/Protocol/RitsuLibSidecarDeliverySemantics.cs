namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     How a sidecar payload should be sent and (by convention) interpreted. The first byte of
    ///     <see cref="RitsuLibSidecarEnvelope.ParsedEnvelope.HeaderExtension" />, when present, records this; see
    ///     <see cref="RitsuLibSidecarHeaderExtension.GetDeliveryOrUnspecified" />.
    ///     sidecar 载荷应如何发送以及按约定如何解释。存在时，
    ///     <see cref="RitsuLibSidecarEnvelope.ParsedEnvelope.HeaderExtension" /> 的第一个字节记录此信息；见
    ///     <see cref="RitsuLibSidecarHeaderExtension.GetDeliveryOrUnspecified" />。
    /// </summary>
    public enum RitsuLibSidecarDeliverySemantics : byte
    {
        /// <summary>
        ///     Unordered / loss-tolerant: map to unreliable transport and a best-effort channel. Handlers may run
        ///     as soon as the frame arrives; no cross-stream ordering is implied.
        ///     无序 / 容忍丢失：映射到不可靠传输和 best-effort channel。处理器可在
        ///     frame 到达后立即运行；不隐含跨流排序。
        /// </summary>
        BestEffort = 0,

        /// <summary>
        ///     Reliable, ordered with respect to other reliable sidecar traffic on the same ENet stream. This does not
        ///     by itself marshal handler code to the Godot main thread or merge with vanilla game action serialization;
        ///     it only selects transport parameters for sidecar envelopes.
        ///     相对于同一 ENet stream 上其他可靠 sidecar 流量可靠且有序。这本身不会
        ///     把处理器代码调度到 Godot 主线程，也不会与原版游戏 action 序列化合并；
        ///     它只为 sidecar envelope 选择传输参数。
        /// </summary>
        StableSync = 1,

        /// <summary>
        ///     Header extension omits a delivery tag; treated like <see cref="StableSync" /> for send helpers.
        ///     Header 扩展省略投递标签；发送辅助方法会按 <see cref="StableSync" /> 处理。
        /// </summary>
        Unspecified = 0xFF,
    }
}
