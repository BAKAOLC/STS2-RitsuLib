using System.Buffers.Binary;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarDiagnosticPayload
    {
        internal const int FanoutPayloadSize = RitsuLibSidecarDiagnosticRelayLayout.FanoutPayloadSize;

        internal static byte[] BuildFanoutPayload(ulong originatingSenderNetId, ushort tag)
        {
            var buf = new byte[FanoutPayloadSize];
            BinaryPrimitives.WriteUInt64BigEndian(
                buf.AsSpan(
                    RitsuLibSidecarDiagnosticRelayLayout.OriginatingSenderNetIdOffset,
                    RitsuLibSidecarDiagnosticRelayLayout.OriginatingSenderNetIdSize),
                originatingSenderNetId);
            BinaryPrimitives.WriteUInt16BigEndian(
                buf.AsSpan(
                    RitsuLibSidecarDiagnosticRelayLayout.TagOffset,
                    RitsuLibSidecarDiagnosticRelayLayout.TagSize),
                tag);
            return buf;
        }

        internal static bool TryParseFanout(ReadOnlySpan<byte> payload, out ulong originatingSenderNetId,
            out ushort tag)
        {
            originatingSenderNetId = 0;
            tag = 0;
            if (payload.Length < FanoutPayloadSize)
                return false;

            originatingSenderNetId = BinaryPrimitives.ReadUInt64BigEndian(
                payload.Slice(
                    RitsuLibSidecarDiagnosticRelayLayout.OriginatingSenderNetIdOffset,
                    RitsuLibSidecarDiagnosticRelayLayout.OriginatingSenderNetIdSize));
            tag = BinaryPrimitives.ReadUInt16BigEndian(
                payload.Slice(
                    RitsuLibSidecarDiagnosticRelayLayout.TagOffset,
                    RitsuLibSidecarDiagnosticRelayLayout.TagSize));
            return true;
        }
    }
}
