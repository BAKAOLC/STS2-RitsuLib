namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarPayloadCompressionSelector
    {
        internal static RitsuLibSidecarPayloadCompression ForPeer(
            ulong opcode,
            ReadOnlySpan<byte> payload,
            ulong peerNetId)
        {
            if (!CanAutoCompress(opcode, payload))
                return RitsuLibSidecarPayloadCompression.None;

            return PeerSupportsBrotli(peerNetId)
                ? RitsuLibSidecarPayloadCompression.Auto
                : RitsuLibSidecarPayloadCompression.None;
        }

        internal static RitsuLibSidecarPayloadCompression ForPeers(
            ulong opcode,
            ReadOnlySpan<byte> payload,
            IEnumerable<ulong> peerNetIds)
        {
            if (!CanAutoCompress(opcode, payload))
                return RitsuLibSidecarPayloadCompression.None;

            var any = false;
            foreach (var peerNetId in peerNetIds)
            {
                if (!RitsuLibSidecarSessionManager.CanSendToPeer(peerNetId))
                    continue;

                any = true;
                if (!PeerSupportsBrotli(peerNetId))
                    return RitsuLibSidecarPayloadCompression.None;
            }

            return any ? RitsuLibSidecarPayloadCompression.Auto : RitsuLibSidecarPayloadCompression.None;
        }

        internal static bool PeerSupportsBrotli(ulong peerNetId)
        {
            return RitsuLibSidecarSessionManager.TryGetPeerFeatures(peerNetId, out var features) &&
                   (features & RitsuLibSidecarPeerFeatures.BrotliPayloadCompression) != 0;
        }

        private static bool CanAutoCompress(ulong opcode, ReadOnlySpan<byte> payload)
        {
            return payload.Length >= RitsuLibSidecarCompression.AutoCompressionMinPayloadBytes &&
                   !IsControlOpcode(opcode);
        }

        private static bool IsControlOpcode(ulong opcode)
        {
            return opcode is
                RitsuLibSidecarControlOpcodes.Handshake or
                RitsuLibSidecarControlOpcodes.HandshakeAck or
                RitsuLibSidecarControlOpcodes.ChunkedFrame or
                RitsuLibSidecarControlOpcodes.ChunkStreamSelectiveNack or
                RitsuLibSidecarControlOpcodes.ChunkStreamReassemblyDone;
        }
    }
}
