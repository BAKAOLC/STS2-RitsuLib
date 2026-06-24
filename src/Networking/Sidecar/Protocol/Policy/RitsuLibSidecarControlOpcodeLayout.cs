namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarControlOpcodeLayout
    {
        public const ulong FrameworkPing = 0x01;

        public const ulong ControlRangeStart = 0x10;
        public const ulong HandshakeOffset = 0;
        public const ulong HandshakeAckOffset = 1;
        public const ulong ChunkedFrameOffset = 2;
        public const ulong ChunkStreamSelectiveNackOffset = 3;
        public const ulong ChunkStreamReassemblyDoneOffset = 4;
        public const ulong DiagnosticRelayDumpRequestOffset = 5;
        public const ulong DiagnosticRelayDumpFanoutOffset = 6;
    }
}
