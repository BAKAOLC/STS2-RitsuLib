using System.Buffers.Binary;
using System.IO.Hashing;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarNativeTrailerEvidence
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<ulong, long> SupportedEpochByPeer = [];

        public static bool TryAppendLocalTrailer(ref byte[] packetBytes, ref int length)
        {
            if (length <= 0 || packetBytes.Length < length)
                return false;

            var payload = packetBytes.AsSpan(0, length);
            if (RitsuLibSidecarWire.MatchesMagic(payload))
                return false;
            if (TryReadTrailer(payload, out _))
                return false;

            var next = new byte[length + RitsuLibSidecarNativeTrailerLayout.TrailerSize];
            payload.CopyTo(next);
            WriteTrailer(next.AsSpan(length), payload);
            packetBytes = next;
            length = next.Length;
            return true;
        }

        public static void ObserveInbound(ulong senderId, byte[] packetBytes)
        {
            if (packetBytes.Length < RitsuLibSidecarNativeTrailerLayout.TrailerSize)
                return;
            if (!TryReadTrailer(packetBytes, out var flags))
                return;
            if ((flags & RitsuLibSidecarNativeTrailerLayout.FlagSidecarSupported) == 0)
                return;

            var epoch = RitsuLibSidecarSessionManager.Epoch;
            lock (Gate)
            {
                SupportedEpochByPeer[senderId] = epoch;
            }

            RitsuLibSidecarSessionManager.RefreshReachabilityFromProviders(senderId);
        }

        public static bool HasCurrentSessionEvidence(ulong peerNetId)
        {
            var epoch = RitsuLibSidecarSessionManager.Epoch;
            lock (Gate)
            {
                return SupportedEpochByPeer.TryGetValue(peerNetId, out var evidenceEpoch) && evidenceEpoch == epoch;
            }
        }

        private static void WriteTrailer(Span<byte> destination, ReadOnlySpan<byte> payload)
        {
            RitsuLibSidecarNativeTrailerLayout.HeaderSignature.CopyTo(
                destination.Slice(
                    RitsuLibSidecarNativeTrailerLayout.HeaderSignatureOffset,
                    RitsuLibSidecarNativeTrailerLayout.SignatureSize));
            BinaryPrimitives.WriteUInt16BigEndian(
                destination.Slice(
                    RitsuLibSidecarNativeTrailerLayout.VersionOffset,
                    RitsuLibSidecarNativeTrailerLayout.VersionSize),
                RitsuLibSidecarNativeTrailerLayout.Version);
            BinaryPrimitives.WriteUInt16BigEndian(
                destination.Slice(
                    RitsuLibSidecarNativeTrailerLayout.FlagsOffset,
                    RitsuLibSidecarNativeTrailerLayout.FlagsSize),
                RitsuLibSidecarNativeTrailerLayout.FlagSidecarSupported);
            BinaryPrimitives.WriteUInt32BigEndian(
                destination.Slice(
                    RitsuLibSidecarNativeTrailerLayout.PayloadLengthOffset,
                    RitsuLibSidecarNativeTrailerLayout.PayloadLengthSize),
                (uint)payload.Length);
            BinaryPrimitives.WriteUInt32BigEndian(
                destination.Slice(
                    RitsuLibSidecarNativeTrailerLayout.PayloadCrc32Offset,
                    RitsuLibSidecarNativeTrailerLayout.PayloadCrc32Size),
                Crc32.HashToUInt32(payload));
            RitsuLibSidecarNativeTrailerLayout.FooterSignature.CopyTo(
                destination.Slice(
                    RitsuLibSidecarNativeTrailerLayout.FooterSignatureOffset,
                    RitsuLibSidecarNativeTrailerLayout.FooterSignatureSize));
        }

        private static bool TryReadTrailer(ReadOnlySpan<byte> packetBytes, out ushort flags)
        {
            flags = 0;
            if (packetBytes.Length < RitsuLibSidecarNativeTrailerLayout.TrailerSize)
                return false;

            if (!TryLocateTrailerStart(packetBytes, out var trailerStart))
                return false;

            var trailer = packetBytes.Slice(trailerStart, RitsuLibSidecarNativeTrailerLayout.TrailerSize);
            if (!trailer
                    .Slice(
                        RitsuLibSidecarNativeTrailerLayout.HeaderSignatureOffset,
                        RitsuLibSidecarNativeTrailerLayout.SignatureSize)
                    .SequenceEqual(RitsuLibSidecarNativeTrailerLayout.HeaderSignature))
                return false;

            var version = BinaryPrimitives.ReadUInt16BigEndian(
                trailer.Slice(
                    RitsuLibSidecarNativeTrailerLayout.VersionOffset,
                    RitsuLibSidecarNativeTrailerLayout.VersionSize));
            if (version != RitsuLibSidecarNativeTrailerLayout.Version)
                return false;

            if (!trailer
                    .Slice(
                        RitsuLibSidecarNativeTrailerLayout.FooterSignatureOffset,
                        RitsuLibSidecarNativeTrailerLayout.FooterSignatureSize)
                    .SequenceEqual(RitsuLibSidecarNativeTrailerLayout.FooterSignature))
                return false;

            flags = BinaryPrimitives.ReadUInt16BigEndian(
                trailer.Slice(
                    RitsuLibSidecarNativeTrailerLayout.FlagsOffset,
                    RitsuLibSidecarNativeTrailerLayout.FlagsSize));

            var declaredPayloadLength = BinaryPrimitives.ReadUInt32BigEndian(
                trailer.Slice(
                    RitsuLibSidecarNativeTrailerLayout.PayloadLengthOffset,
                    RitsuLibSidecarNativeTrailerLayout.PayloadLengthSize));
            if ((uint)trailerStart != declaredPayloadLength)
                return false;

            var expectedCrc32 = BinaryPrimitives.ReadUInt32BigEndian(
                trailer.Slice(
                    RitsuLibSidecarNativeTrailerLayout.PayloadCrc32Offset,
                    RitsuLibSidecarNativeTrailerLayout.PayloadCrc32Size));
            var payload = packetBytes[..trailerStart];
            return Crc32.HashToUInt32(payload) == expectedCrc32;
        }

        private static bool TryLocateTrailerStart(ReadOnlySpan<byte> packetBytes, out int trailerStart)
        {
            trailerStart = -1;
            var footerSize = RitsuLibSidecarNativeTrailerLayout.FooterSignatureSize;
            var footerOffset = RitsuLibSidecarNativeTrailerLayout.FooterSignatureOffset;
            var minFooterStart = RitsuLibSidecarNativeTrailerLayout.TrailerSize - footerSize;
            for (var footerStart = packetBytes.Length - footerSize; footerStart >= minFooterStart; footerStart--)
            {
                if (!packetBytes.Slice(footerStart, footerSize)
                        .SequenceEqual(RitsuLibSidecarNativeTrailerLayout.FooterSignature))
                    continue;

                var candidateStart = footerStart - footerOffset;
                if (candidateStart < 0)
                    continue;
                if (candidateStart + RitsuLibSidecarNativeTrailerLayout.TrailerSize > packetBytes.Length)
                    continue;

                trailerStart = candidateStart;
                return true;
            }

            return false;
        }
    }
}
