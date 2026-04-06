using System.Buffers.Binary;
using System.IO.Hashing;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace STS2RitsuLib.Multiplayer.ChunkedPayload
{
    /// <summary>
    ///     Single reliable fragment for <see cref="ChunkedPayloadChannel" />. Registered with the game net stack via
    ///     mod type discovery; all players must load RitsuLib (same assembly) for compatible IDs.
    /// </summary>
    public sealed class RitsuLibChunkedNetFragmentMessage : INetMessage
    {
        /// <summary>
        ///     Current wire schema; bump when changing <see cref="Serialize" /> layout.
        /// </summary>
        public const byte SupportedSchemaVersion = 1;

        /// <summary>
        ///     Total fragments for this transfer.
        /// </summary>
        public uint ChunkCount;

        /// <summary>
        ///     Zero-based index.
        /// </summary>
        public uint ChunkIndex;

        /// <summary>
        ///     Must match sender <see cref="ChunkedTransferOptions.MaxFragmentPayloadBytes" />.
        /// </summary>
        public ushort DeclaredMaxFragmentBytes;

        /// <summary>
        ///     CRC32 (IEEE) of the full payload before splitting.
        /// </summary>
        public uint PayloadCrc32;

        /// <summary>
        ///     Wire schema of this instance.
        /// </summary>
        public byte SchemaVersion = SupportedSchemaVersion;

        /// <summary>
        ///     Multiplex key; must match the receiving <see cref="ChunkedPayloadChannel.StreamId" />.
        /// </summary>
        public ushort StreamId;

        /// <summary>
        ///     Final reassembled size in bytes.
        /// </summary>
        public uint TotalPayloadLength;

        /// <summary>
        ///     Random id scoped per send operation.
        /// </summary>
        public ulong TransferId;

        /// <summary>
        ///     Raw bytes for this chunk.
        /// </summary>
        public byte[] Fragment { get; set; } = [];

        /// <inheritdoc />
        public bool ShouldBroadcast => false;

        /// <inheritdoc />
        public NetTransferMode Mode => NetTransferMode.Reliable;

        /// <inheritdoc />
        public LogLevel LogLevel => LogLevel.VeryDebug;

        /// <inheritdoc />
        public void Serialize(PacketWriter writer)
        {
            writer.WriteByte(SchemaVersion);
            writer.WriteUShort(StreamId);
            writer.WriteUShort(DeclaredMaxFragmentBytes);
            writer.WriteULong(TransferId);
            writer.WriteUInt(TotalPayloadLength);
            writer.WriteUInt(ChunkIndex);
            writer.WriteUInt(ChunkCount);
            writer.WriteUInt(PayloadCrc32);
            writer.WriteInt(Fragment.Length);
            if (Fragment.Length > 0)
                writer.WriteBytes(Fragment, Fragment.Length);
        }

        /// <inheritdoc />
        public void Deserialize(PacketReader reader)
        {
            SchemaVersion = reader.ReadByte();
            StreamId = reader.ReadUShort();
            DeclaredMaxFragmentBytes = reader.ReadUShort();
            TransferId = reader.ReadULong();
            TotalPayloadLength = reader.ReadUInt();
            ChunkIndex = reader.ReadUInt();
            ChunkCount = reader.ReadUInt();
            PayloadCrc32 = reader.ReadUInt();
            var len = reader.ReadInt();
            switch (len)
            {
                case < 0:
                    throw new InvalidOperationException("Negative fragment length.");
                case > ushort.MaxValue:
                    throw new InvalidOperationException($"Fragment length {len} exceeds {ushort.MaxValue}.");
            }

            if (len > DeclaredMaxFragmentBytes)
                throw new InvalidOperationException(
                    $"Fragment length {len} exceeds declared maximum {DeclaredMaxFragmentBytes}.");
            Fragment = len == 0 ? [] : new byte[len];
            if (len > 0)
                reader.ReadBytes(Fragment, len);
        }

        /// <summary>
        ///     Computes CRC32 (IEEE polynomial) for a full payload.
        /// </summary>
        public static uint ComputePayloadCrc32(ReadOnlySpan<byte> payload)
        {
            Span<byte> dest = stackalloc byte[4];
            Crc32.Hash(payload, dest);
            return BinaryPrimitives.ReadUInt32LittleEndian(dest);
        }
    }
}
