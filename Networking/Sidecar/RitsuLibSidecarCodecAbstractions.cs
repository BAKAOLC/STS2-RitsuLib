using System.Buffers;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>Binary encode/decode for one opcode; implement one concrete type T.</summary>
    public interface IRitsuLibSidecarMessageCodec<T>
        where T : notnull
    {
        /// <summary>User or control <c>ulong</c> opcode; must match <see cref="RitsuLibSidecarBus" /> registration.</summary>
        ulong Opcode { get; }

        /// <summary>Decodes the sidecar logical payload (no outer magic; that is stripped by the bus).</summary>
        /// <param name="input">Bytes after the fixed envelope header and optional extension.</param>
        /// <param name="message">Set when the return value is <c>true</c>.</param>
        bool TryDecode(ReadOnlySpan<byte> input, out T? message);

        /// <summary>Appends the wire form of <paramref name="message" /> to <paramref name="writer" />.</summary>
        /// <param name="writer">Destination buffer writer.</param>
        /// <param name="message">Value to encode.</param>
        void Encode(IBufferWriter<byte> writer, T message);
    }

    /// <summary>
    ///     Apply a decoded value after <see cref="IRitsuLibSidecarMessageCodec{T}.TryDecode" />; thread matches the
    ///     sidecar receive path unless you register with
    ///     <see cref="RitsuLibSidecarMessageBinding.RegisterForGodotMainLoop{T}" />.
    /// </summary>
    public interface IRitsuLibSidecarSyncProcessor<in T>
        where T : notnull
    {
        /// <param name="message">Value from <see cref="IRitsuLibSidecarMessageCodec{T}.TryDecode" />.</param>
        /// <param name="context">Per-packet transport and envelope information.</param>
        void Apply(T message, in RitsuLibSidecarDispatchContext context);
    }
}
