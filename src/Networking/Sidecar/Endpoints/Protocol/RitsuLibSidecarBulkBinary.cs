using System.Buffers.Binary;
using System.IO.Hashing;
using System.Text;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarBulkBinary
    {
        internal const byte Version = 1;
        internal const int CommonHeaderSize = 10;
        internal const int OfferHeaderSize = 28;
        internal const int AcceptFrameSize = 18;
        internal const int DataHeaderSize = 22;
        internal const int AcknowledgeFrameSize = 18;
        internal const int CompleteFrameSize = 50;
        internal const int CompletedFrameSize = CommonHeaderSize;
        internal const int AbortFrameSize = 11;
        internal const int Sha256Size = 32;

        internal static byte[] WriteOffer(
            ulong transferId,
            long totalLength,
            int requestedChunkBytes,
            int requestedWindowBytes,
            RitsuLibSidecarBulkStreamMetadata metadata)
        {
            ValidateTransferId(transferId);
            ArgumentNullException.ThrowIfNull(metadata);
            ValidateLength(totalLength);
            ValidateChunkAndWindow(requestedChunkBytes, requestedWindowBytes);
            var name = EncodeOptional(metadata.Name);
            var contentType = EncodeOptional(metadata.ContentType);
            if (name.Length > RitsuLibSidecarEndpointPolicy.MaxBulkNameUtf8Bytes ||
                contentType.Length > RitsuLibSidecarEndpointPolicy.MaxBulkContentTypeUtf8Bytes)
                throw new ArgumentOutOfRangeException(nameof(metadata));

            var output = new byte[OfferHeaderSize + name.Length + contentType.Length];
            WriteCommon(output, RitsuLibSidecarBulkFrameType.Offer, transferId);
            BinaryPrimitives.WriteInt64BigEndian(output.AsSpan(10, 8), totalLength);
            BinaryPrimitives.WriteInt32BigEndian(output.AsSpan(18, 4), requestedChunkBytes);
            BinaryPrimitives.WriteInt32BigEndian(output.AsSpan(22, 4), requestedWindowBytes);
            output[26] = (byte)name.Length;
            output[27] = (byte)contentType.Length;
            name.CopyTo(output.AsSpan(OfferHeaderSize));
            contentType.CopyTo(output.AsSpan(OfferHeaderSize + name.Length));
            return output;
        }

        internal static byte[] WriteAccept(ulong transferId, int chunkBytes, int windowBytes)
        {
            ValidateTransferId(transferId);
            ValidateChunkAndWindow(chunkBytes, windowBytes);
            var output = new byte[AcceptFrameSize];
            WriteCommon(output, RitsuLibSidecarBulkFrameType.Accept, transferId);
            BinaryPrimitives.WriteInt32BigEndian(output.AsSpan(10, 4), chunkBytes);
            BinaryPrimitives.WriteInt32BigEndian(output.AsSpan(14, 4), windowBytes);
            return output;
        }

        internal static byte[] WriteData(ulong transferId, long offset, ReadOnlySpan<byte> payload)
        {
            ValidateTransferId(transferId);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (payload.Length is < 1 or > RitsuLibSidecarEndpointPolicy.MaxBulkChunkBytes)
                throw new ArgumentOutOfRangeException(nameof(payload));

            var output = new byte[DataHeaderSize + payload.Length];
            WriteCommon(output, RitsuLibSidecarBulkFrameType.Data, transferId);
            BinaryPrimitives.WriteInt64BigEndian(output.AsSpan(10, 8), offset);
            BinaryPrimitives.WriteUInt32BigEndian(output.AsSpan(18, 4), Crc32.HashToUInt32(payload));
            payload.CopyTo(output.AsSpan(DataHeaderSize));
            return output;
        }

        internal static byte[] WriteAcknowledge(ulong transferId, long nextOffset)
        {
            ValidateTransferId(transferId);
            ArgumentOutOfRangeException.ThrowIfNegative(nextOffset);
            var output = new byte[AcknowledgeFrameSize];
            WriteCommon(output, RitsuLibSidecarBulkFrameType.Acknowledge, transferId);
            BinaryPrimitives.WriteInt64BigEndian(output.AsSpan(10, 8), nextOffset);
            return output;
        }

        internal static byte[] WriteComplete(ulong transferId, long totalLength, ReadOnlySpan<byte> sha256)
        {
            ValidateTransferId(transferId);
            ValidateLength(totalLength);
            if (sha256.Length != Sha256Size)
                throw new ArgumentException($"SHA-256 digest must contain {Sha256Size} bytes.", nameof(sha256));
            var output = new byte[CompleteFrameSize];
            WriteCommon(output, RitsuLibSidecarBulkFrameType.Complete, transferId);
            BinaryPrimitives.WriteInt64BigEndian(output.AsSpan(10, 8), totalLength);
            sha256.CopyTo(output.AsSpan(18, Sha256Size));
            return output;
        }

        internal static byte[] WriteCompleted(ulong transferId)
        {
            ValidateTransferId(transferId);
            var output = new byte[CompletedFrameSize];
            WriteCommon(output, RitsuLibSidecarBulkFrameType.Completed, transferId);
            return output;
        }

        internal static byte[] WriteAbort(ulong transferId, RitsuLibSidecarBulkAbortReason reason)
        {
            ValidateTransferId(transferId);
            if (!Enum.IsDefined(reason))
                throw new ArgumentOutOfRangeException(nameof(reason), reason, "Invalid bulk abort reason.");
            var output = new byte[AbortFrameSize];
            WriteCommon(output, RitsuLibSidecarBulkFrameType.Abort, transferId);
            output[10] = (byte)reason;
            return output;
        }

        internal static bool TryReadFrame(ReadOnlyMemory<byte> source, out RitsuLibSidecarBulkFrame frame)
        {
            frame = default;
            if (source.Length < CommonHeaderSize || source.Span[0] != Version)
                return false;
            var type = (RitsuLibSidecarBulkFrameType)source.Span[1];
            var transferId = BinaryPrimitives.ReadUInt64BigEndian(source.Span.Slice(2, 8));
            if (transferId == 0 || !Enum.IsDefined(type))
                return false;

            return type switch
            {
                RitsuLibSidecarBulkFrameType.Offer => TryReadOffer(source, transferId, out frame),
                RitsuLibSidecarBulkFrameType.Accept => TryReadAccept(source, transferId, out frame),
                RitsuLibSidecarBulkFrameType.Data => TryReadData(source, transferId, out frame),
                RitsuLibSidecarBulkFrameType.Acknowledge => TryReadAcknowledge(source, transferId, out frame),
                RitsuLibSidecarBulkFrameType.Complete => TryReadComplete(source, transferId, out frame),
                RitsuLibSidecarBulkFrameType.Completed when source.Length == CompletedFrameSize =>
                    SetSimpleFrame(type, transferId, out frame),
                RitsuLibSidecarBulkFrameType.Abort => TryReadAbort(source, transferId, out frame),
                _ => false,
            };
        }

        private static bool TryReadOffer(
            ReadOnlyMemory<byte> source,
            ulong transferId,
            out RitsuLibSidecarBulkFrame frame)
        {
            frame = default;
            if (source.Length < OfferHeaderSize)
                return false;
            var span = source.Span;
            var totalLength = BinaryPrimitives.ReadInt64BigEndian(span.Slice(10, 8));
            var chunkBytes = BinaryPrimitives.ReadInt32BigEndian(span.Slice(18, 4));
            var windowBytes = BinaryPrimitives.ReadInt32BigEndian(span.Slice(22, 4));
            var nameLength = span[26];
            var contentTypeLength = span[27];
            if (!IsValidLength(totalLength) ||
                !IsValidChunkAndWindow(chunkBytes, windowBytes) ||
                nameLength > RitsuLibSidecarEndpointPolicy.MaxBulkNameUtf8Bytes ||
                contentTypeLength > RitsuLibSidecarEndpointPolicy.MaxBulkContentTypeUtf8Bytes ||
                source.Length != OfferHeaderSize + nameLength + contentTypeLength)
                return false;
            if (!TryDecodeOptional(span.Slice(OfferHeaderSize, nameLength), out var name) ||
                !TryDecodeOptional(span.Slice(OfferHeaderSize + nameLength, contentTypeLength), out var contentType))
                return false;

            RitsuLibSidecarBulkStreamMetadata metadata;
            try
            {
                metadata = new(name, contentType);
            }
            catch (ArgumentException)
            {
                return false;
            }

            frame = new(
                RitsuLibSidecarBulkFrameType.Offer,
                transferId,
                totalLength,
                chunkBytes,
                windowBytes,
                0,
                default,
                metadata,
                ReadOnlyMemory<byte>.Empty,
                null);
            return true;
        }

        private static bool TryReadAccept(
            ReadOnlyMemory<byte> source,
            ulong transferId,
            out RitsuLibSidecarBulkFrame frame)
        {
            frame = default;
            if (source.Length != AcceptFrameSize)
                return false;
            var chunkBytes = BinaryPrimitives.ReadInt32BigEndian(source.Span.Slice(10, 4));
            var windowBytes = BinaryPrimitives.ReadInt32BigEndian(source.Span.Slice(14, 4));
            if (!IsValidChunkAndWindow(chunkBytes, windowBytes))
                return false;
            frame = new(
                RitsuLibSidecarBulkFrameType.Accept,
                transferId,
                0,
                chunkBytes,
                windowBytes,
                0,
                default,
                null,
                ReadOnlyMemory<byte>.Empty,
                null);
            return true;
        }

        private static bool TryReadData(
            ReadOnlyMemory<byte> source,
            ulong transferId,
            out RitsuLibSidecarBulkFrame frame)
        {
            frame = default;
            var payloadLength = source.Length - DataHeaderSize;
            if (payloadLength is < 1 or > RitsuLibSidecarEndpointPolicy.MaxBulkChunkBytes)
                return false;
            var offset = BinaryPrimitives.ReadInt64BigEndian(source.Span.Slice(10, 8));
            var expectedCrc = BinaryPrimitives.ReadUInt32BigEndian(source.Span.Slice(18, 4));
            var payload = source[DataHeaderSize..];
            if (offset < 0 || Crc32.HashToUInt32(payload.Span) != expectedCrc)
                return false;
            frame = new(
                RitsuLibSidecarBulkFrameType.Data,
                transferId,
                0,
                0,
                0,
                offset,
                default,
                null,
                payload,
                null);
            return true;
        }

        private static bool TryReadAcknowledge(
            ReadOnlyMemory<byte> source,
            ulong transferId,
            out RitsuLibSidecarBulkFrame frame)
        {
            frame = default;
            if (source.Length != AcknowledgeFrameSize)
                return false;
            var nextOffset = BinaryPrimitives.ReadInt64BigEndian(source.Span.Slice(10, 8));
            if (nextOffset < 0)
                return false;
            frame = new(
                RitsuLibSidecarBulkFrameType.Acknowledge,
                transferId,
                0,
                0,
                0,
                nextOffset,
                default,
                null,
                ReadOnlyMemory<byte>.Empty,
                null);
            return true;
        }

        private static bool TryReadComplete(
            ReadOnlyMemory<byte> source,
            ulong transferId,
            out RitsuLibSidecarBulkFrame frame)
        {
            frame = default;
            if (source.Length != CompleteFrameSize)
                return false;
            var totalLength = BinaryPrimitives.ReadInt64BigEndian(source.Span.Slice(10, 8));
            if (!IsValidLength(totalLength))
                return false;
            frame = new(
                RitsuLibSidecarBulkFrameType.Complete,
                transferId,
                totalLength,
                0,
                0,
                0,
                default,
                null,
                ReadOnlyMemory<byte>.Empty,
                source.Slice(18, Sha256Size).ToArray());
            return true;
        }

        private static bool TryReadAbort(
            ReadOnlyMemory<byte> source,
            ulong transferId,
            out RitsuLibSidecarBulkFrame frame)
        {
            frame = default;
            if (source.Length != AbortFrameSize)
                return false;
            var reason = (RitsuLibSidecarBulkAbortReason)source.Span[10];
            if (!Enum.IsDefined(reason))
                return false;
            frame = new(
                RitsuLibSidecarBulkFrameType.Abort,
                transferId,
                0,
                0,
                0,
                0,
                reason,
                null,
                ReadOnlyMemory<byte>.Empty,
                null);
            return true;
        }

        private static bool SetSimpleFrame(
            RitsuLibSidecarBulkFrameType type,
            ulong transferId,
            out RitsuLibSidecarBulkFrame frame)
        {
            frame = new(
                type,
                transferId,
                0,
                0,
                0,
                0,
                default,
                null,
                ReadOnlyMemory<byte>.Empty,
                null);
            return true;
        }

        private static void WriteCommon(Span<byte> destination, RitsuLibSidecarBulkFrameType type, ulong transferId)
        {
            destination[0] = Version;
            destination[1] = (byte)type;
            BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(2, 8), transferId);
        }

        private static byte[] EncodeOptional(string? value)
        {
            return value == null ? [] : Encoding.UTF8.GetBytes(value);
        }

        private static bool TryDecodeOptional(ReadOnlySpan<byte> value, out string? decoded)
        {
            decoded = null;
            if (value.IsEmpty)
                return true;
            try
            {
                decoded = new UTF8Encoding(false, true).GetString(value);
                return true;
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
        }

        private static void ValidateTransferId(ulong transferId)
        {
            ArgumentOutOfRangeException.ThrowIfZero(transferId);
        }

        private static void ValidateLength(long length)
        {
            if (!IsValidLength(length))
                throw new ArgumentOutOfRangeException(nameof(length));
        }

        private static bool IsValidLength(long length)
        {
            return length is >= 0 and <= RitsuLibSidecarEndpointPolicy.MaxBulkStreamBytes;
        }

        private static void ValidateChunkAndWindow(int chunkBytes, int windowBytes)
        {
            if (!IsValidChunkAndWindow(chunkBytes, windowBytes))
                throw new ArgumentOutOfRangeException(nameof(chunkBytes), "Invalid bulk chunk or window size.");
        }

        private static bool IsValidChunkAndWindow(int chunkBytes, int windowBytes)
        {
            return chunkBytes is >= RitsuLibSidecarEndpointPolicy.MinBulkChunkBytes and
                       <= RitsuLibSidecarEndpointPolicy.MaxBulkChunkBytes &&
                   windowBytes is >= RitsuLibSidecarEndpointPolicy.MinBulkWindowBytes and
                       <= RitsuLibSidecarEndpointPolicy.MaxBulkWindowBytes &&
                   windowBytes >= chunkBytes;
        }
    }
}
