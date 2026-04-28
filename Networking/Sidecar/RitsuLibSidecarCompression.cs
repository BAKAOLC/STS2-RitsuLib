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
                var buffer = new byte[compressed.Length];
                compressed.CopyTo(buffer);
                using var input = new MemoryStream(buffer, false);
                using var gz = new GZipStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                gz.CopyTo(output);
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
