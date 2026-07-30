using System.Buffers;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">Binary codec for one message type and opcode.</para>
    ///     <para xml:lang="zh-CN">用于一种消息类型和操作码的二进制编解码器。</para>
    /// </summary>
    public interface IRitsuLibSidecarMessageCodec<T>
        where T : notnull
    {
        /// <summary>
        ///     <para xml:lang="en">User or control <c>ulong</c> opcode; must match the <see cref="RitsuLibSidecarBus" /> registration.</para>
        ///     <para xml:lang="zh-CN">用户或控制 <c>ulong</c> 操作码；必须与 <see cref="RitsuLibSidecarBus" /> 中的注册匹配。</para>
        /// </summary>
        ulong Opcode { get; }

        /// <summary>
        ///     <para xml:lang="en">Decodes the Sidecar logical payload after the envelope has been removed.</para>
        ///     <para xml:lang="zh-CN">解码已移除信封后的 Sidecar 逻辑载荷。</para>
        /// </summary>
        /// <param name="input">
        ///     <para xml:lang="en">Bytes after the fixed envelope header and optional extension.</para>
        ///     <para xml:lang="zh-CN">固定信封标头和可选扩展之后的字节。</para>
        /// </param>
        /// <param name="message">
        ///     <para xml:lang="en">Set when the return value is <see langword="true" />.</para>
        ///     <para xml:lang="zh-CN">返回值为 <see langword="true" /> 时设置。</para>
        /// </param>
        bool TryDecode(ReadOnlySpan<byte> input, out T? message);

        /// <summary>
        ///     <para xml:lang="en">Appends the binary representation of <paramref name="message" /> to <paramref name="writer" />.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="message" /> 的二进制表示追加到 <paramref name="writer" />。</para>
        /// </summary>
        /// <param name="writer">
        ///     <para xml:lang="en">Destination buffer writer.</para>
        ///     <para xml:lang="zh-CN">目标缓冲区写入器。</para>
        /// </param>
        /// <param name="message">
        ///     <para xml:lang="en">Value to encode.</para>
        ///     <para xml:lang="zh-CN">要编码的值。</para>
        /// </param>
        void Encode(IBufferWriter<byte> writer, T message);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Processes a decoded value after <see cref="IRitsuLibSidecarMessageCodec{T}.TryDecode" />. It runs on
    ///         the Sidecar receive thread unless registered through
    ///         <see cref="RitsuLibSidecarMessageBinding.RegisterForGodotMainLoop{T}" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         处理 <see cref="IRitsuLibSidecarMessageCodec{T}.TryDecode" /> 解码出的值。除非通过
    ///         <see cref="RitsuLibSidecarMessageBinding.RegisterForGodotMainLoop{T}" /> 注册，否则会在
    ///         Sidecar 接收线程上运行。
    ///     </para>
    /// </summary>
    public interface IRitsuLibSidecarSyncProcessor<in T>
        where T : notnull
    {
        /// <param name="message">
        ///     <para xml:lang="en">Value from <see cref="IRitsuLibSidecarMessageCodec{T}.TryDecode" />.</para>
        ///     <para xml:lang="zh-CN">来自 <see cref="IRitsuLibSidecarMessageCodec{T}.TryDecode" /> 的值。</para>
        /// </param>
        /// <param name="context">
        ///     <para xml:lang="en">Per-packet transport and envelope information.</para>
        ///     <para xml:lang="zh-CN">每个数据包的传输与信封信息。</para>
        /// </param>
        void Apply(T message, in RitsuLibSidecarDispatchContext context);
    }
}
