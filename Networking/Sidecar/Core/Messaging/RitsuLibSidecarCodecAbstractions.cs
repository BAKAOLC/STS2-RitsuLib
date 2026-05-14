using System.Buffers;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Binary encode/decode for one opcode; implement one concrete type T.
    ///     单个 opcode 的二进制编码/解码；实现一个具体类型 T。
    /// </summary>
    public interface IRitsuLibSidecarMessageCodec<T>
        where T : notnull
    {
        /// <summary>
        ///     User or control <c>ulong</c> opcode; must match <see cref="RitsuLibSidecarBus" /> registration.
        ///     用户或控制 <c>ulong</c> opcode；必须与 <see cref="RitsuLibSidecarBus" /> 注册匹配。
        /// </summary>
        ulong Opcode { get; }

        /// <summary>
        ///     Decodes the sidecar logical payload (no outer magic; that is stripped by the bus).
        ///     解码 sidecar 逻辑载荷（不含外层 magic；bus 已将其剥离）。
        /// </summary>
        /// <param name="input">
        ///     Bytes after the fixed envelope header and optional extension.
        ///     固定 envelope header 和可选扩展之后的字节。
        /// </param>
        /// <param name="message">
        ///     Set when the return value is <c>true</c>.
        ///     返回值为 <c>true</c> 时设置。
        /// </param>
        bool TryDecode(ReadOnlySpan<byte> input, out T? message);

        /// <summary>
        ///     Appends the wire form of <paramref name="message" /> to <paramref name="writer" />.
        ///     将 <paramref name="message" /> 的线格式追加到 <paramref name="writer" />。
        /// </summary>
        /// <param name="writer">
        ///     Destination buffer writer.
        ///     目标缓冲区 writer。
        /// </param>
        /// <param name="message">
        ///     Value to encode.
        ///     要编码的值。
        /// </param>
        void Encode(IBufferWriter<byte> writer, T message);
    }

    /// <summary>
    ///     Apply a decoded value after <see cref="IRitsuLibSidecarMessageCodec{T}.TryDecode" />; thread matches the
    ///     sidecar receive path unless you register with
    ///     <see cref="RitsuLibSidecarMessageBinding.RegisterForGodotMainLoop{T}" />.
    ///     在 <see cref="IRitsuLibSidecarMessageCodec{T}.TryDecode" /> 之后应用解码值；线程与
    ///     sidecar 接收路径一致，除非通过
    ///     <see cref="RitsuLibSidecarMessageBinding.RegisterForGodotMainLoop{T}" /> 注册。
    /// </summary>
    public interface IRitsuLibSidecarSyncProcessor<in T>
        where T : notnull
    {
        /// <param name="message">
        ///     Value from <see cref="IRitsuLibSidecarMessageCodec{T}.TryDecode" />.
        ///     来自 <see cref="IRitsuLibSidecarMessageCodec{T}.TryDecode" /> 的值。
        /// </param>
        /// <param name="context">
        ///     Per-packet transport and envelope information.
        ///     每个数据包的传输和 envelope 信息。
        /// </param>
        void Apply(T message, in RitsuLibSidecarDispatchContext context);
    }
}
