namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Specifies how a sidecar payload is transported and, by convention, interpreted. When present, the
    ///         first byte of the envelope header extension stores this value.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         指定 sidecar 载荷的传输方式及其约定语义。信封标头扩展存在投递标签时，其首字节存储此值。
    ///     </para>
    /// </summary>
    public enum RitsuLibSidecarDeliverySemantics : byte
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Uses unreliable transport on the best-effort channel. Frames may be lost or reordered, and handlers
        ///         may run as soon as a frame arrives.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用尽力而为通道上的不可靠传输。帧可能丢失或乱序，且处理器可以在帧到达后立即运行。
        ///     </para>
        /// </summary>
        BestEffort = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Uses reliable transport on the sidecar synchronization channel. This does not marshal handlers to
        ///         the Godot main thread or merge them into vanilla game-action serialization.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 sidecar 同步通道上的可靠传输。这不会将处理器调度到 Godot 主线程，也不会将其并入原版游戏
        ///         动作的序列化流程。
        ///     </para>
        /// </summary>
        StableSync = 1,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Omits the delivery tag when an envelope is built directly. High-level send methods treat this value
        ///         as <see cref="StableSync" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         直接构建信封时省略投递标签；高层发送方法会将此值按 <see cref="StableSync" /> 处理。
        ///     </para>
        /// </summary>
        Unspecified = 0xFF,
    }
}
