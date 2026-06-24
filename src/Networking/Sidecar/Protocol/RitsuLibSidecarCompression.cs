using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarCompression
    {
        internal static byte[] GzipCompress(ReadOnlySpan<byte> data)
        {
            using var output = new MemoryStream();
            using (var gz = new GZipStream(output, CompressionLevel.SmallestSize, true))
            {
                gz.Write(data);
            }

            return output.ToArray();
        }

        internal static bool TryGunzip(ReadOnlySpan<byte> compressed, [NotNullWhen(true)] out byte[]? decompressed)
        {
            decompressed = null;
            try
            {
                using var input = new MemoryStream(compressed.ToArray(), false);
                using var gz = new GZipStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                var buffer = new byte[8 * RitsuLibSidecarBinaryLayout.KiB];
                while (true)
                {
                    var read = gz.Read(buffer);
                    if (read <= 0)
                        break;
                    output.Write(buffer, 0, read);
                    if (output.Length > RitsuLibSidecarWire.MaxPayloadBytes)
                        return false;
                }

                decompressed = output.ToArray();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
