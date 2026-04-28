using System.Buffers.Binary;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarDiagnosticPayload
    {
        internal const int FanoutPayloadSize = 8 + 2;

        internal static byte[] BuildFanoutPayload(ulong originatingSenderNetId, ushort tag)
        {
            var buf = new byte[FanoutPayloadSize];
            BinaryPrimitives.WriteUInt64BigEndian(buf.AsSpan(0, 8), originatingSenderNetId);
            BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(8, 2), tag);
            return buf;
        }

        internal static bool TryParseFanout(ReadOnlySpan<byte> payload, out ulong originatingSenderNetId,
            out ushort tag)
        {
            originatingSenderNetId = 0;
            tag = 0;
            if (payload.Length < FanoutPayloadSize)
                return false;

            originatingSenderNetId = BinaryPrimitives.ReadUInt64BigEndian(payload);
            tag = BinaryPrimitives.ReadUInt16BigEndian(payload.Slice(8, 2));
            return true;
        }
    }
}
