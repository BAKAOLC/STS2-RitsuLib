using System.Buffers.Binary;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarDiagnosticPayload
    {
        internal const int FanoutPayloadSize = RitsuLibSidecarDiagnosticRelayLayout.FanoutPayloadSize;

        internal static byte[] BuildFanoutPayload(RitsuLibSidecarDiagnosticRelaySession session)
        {
            var buf = new byte[FanoutPayloadSize];
            BinaryPrimitives.WriteUInt64BigEndian(
                buf.AsSpan(
                    RitsuLibSidecarDiagnosticRelayLayout.OriginatingSenderNetIdOffset,
                    RitsuLibSidecarDiagnosticRelayLayout.OriginatingSenderNetIdSize),
                session.OriginatingSenderNetId);
            BinaryPrimitives.WriteUInt16BigEndian(
                buf.AsSpan(
                    RitsuLibSidecarDiagnosticRelayLayout.TagOffset,
                    RitsuLibSidecarDiagnosticRelayLayout.TagSize),
                session.Tag);
            BinaryPrimitives.WriteUInt32BigEndian(
                buf.AsSpan(
                    RitsuLibSidecarDiagnosticRelayLayout.ChecksumIdOffset,
                    RitsuLibSidecarDiagnosticRelayLayout.ChecksumIdSize),
                session.ChecksumId);
            BinaryPrimitives.WriteUInt64BigEndian(
                buf.AsSpan(
                    RitsuLibSidecarDiagnosticRelayLayout.NonceOffset,
                    RitsuLibSidecarDiagnosticRelayLayout.NonceSize),
                session.Nonce);
            BinaryPrimitives.WriteInt64BigEndian(
                buf.AsSpan(
                    RitsuLibSidecarDiagnosticRelayLayout.IssuedUnixMillisecondsOffset,
                    RitsuLibSidecarDiagnosticRelayLayout.IssuedUnixMillisecondsSize),
                session.IssuedUnixMilliseconds);
            return buf;
        }

        internal static bool TryParseFanout(ReadOnlySpan<byte> payload,
            out RitsuLibSidecarDiagnosticRelaySession session)
        {
            session = default;
            if (payload.Length != FanoutPayloadSize)
                return false;

            var originatingSenderNetId = BinaryPrimitives.ReadUInt64BigEndian(
                payload.Slice(
                    RitsuLibSidecarDiagnosticRelayLayout.OriginatingSenderNetIdOffset,
                    RitsuLibSidecarDiagnosticRelayLayout.OriginatingSenderNetIdSize));
            var tag = BinaryPrimitives.ReadUInt16BigEndian(
                payload.Slice(
                    RitsuLibSidecarDiagnosticRelayLayout.TagOffset,
                    RitsuLibSidecarDiagnosticRelayLayout.TagSize));
            var checksumId = BinaryPrimitives.ReadUInt32BigEndian(
                payload.Slice(
                    RitsuLibSidecarDiagnosticRelayLayout.ChecksumIdOffset,
                    RitsuLibSidecarDiagnosticRelayLayout.ChecksumIdSize));
            var nonce = BinaryPrimitives.ReadUInt64BigEndian(
                payload.Slice(
                    RitsuLibSidecarDiagnosticRelayLayout.NonceOffset,
                    RitsuLibSidecarDiagnosticRelayLayout.NonceSize));
            var issuedUnixMilliseconds = BinaryPrimitives.ReadInt64BigEndian(
                payload.Slice(
                    RitsuLibSidecarDiagnosticRelayLayout.IssuedUnixMillisecondsOffset,
                    RitsuLibSidecarDiagnosticRelayLayout.IssuedUnixMillisecondsSize));
            session = new(originatingSenderNetId, checksumId, tag, nonce, issuedUnixMilliseconds);
            return true;
        }
    }
}
