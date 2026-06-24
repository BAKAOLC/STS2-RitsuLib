namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarBinaryLayout
    {
        public const int ByteSize = sizeof(byte);
        public const int U16Size = sizeof(ushort);
        public const int U32Size = sizeof(uint);
        public const int U64Size = sizeof(ulong);

        public const int KiB = 1024;
        public const int MiB = KiB * KiB;
    }
}
