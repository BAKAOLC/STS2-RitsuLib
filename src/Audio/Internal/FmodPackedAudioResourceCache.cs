using System.Security.Cryptography;
using System.Text;
using Godot;
using GArray = Godot.Collections.Array;
using FileAccess = Godot.FileAccess;

namespace STS2RitsuLib.Audio.Internal
{
    internal static class FmodPackedAudioResourceCache
    {
        private const string CacheRoot = "user://ritsulib/fmod-cache/audio";
        private const uint OggCrcPolynomial = 0x04c11db7;
        private static readonly Lock Gate = new();
        private static readonly uint[] OggCrcTable = BuildOggCrcTable();

        public static bool TryMaterialize(string resourcePath, out string absolutePath)
        {
            absolutePath = string.Empty;
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                RitsuLibFramework.Logger.ErrorNoTrace("[Audio] FMOD resource playback requires a non-empty path.");
                return false;
            }

            if (!resourcePath.StartsWith("res://", StringComparison.OrdinalIgnoreCase) &&
                !resourcePath.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[Audio] FMOD resource playback requires a Godot resource path: {resourcePath}");
                return false;
            }

            if (TryReadRawPlayableBytes(resourcePath, out var rawBytes, out var rawExtension))
                return TryWriteCached(resourcePath, rawBytes, rawExtension, out absolutePath);

            AudioStream? stream;
            try
            {
                stream = ResourceLoader.Load<AudioStream>(resourcePath);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[Audio] FMOD resource playback failed to load imported audio resource: {resourcePath}; {ex.Message}");
                return false;
            }

            // ReSharper disable once InvertIf
            if (stream is null)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[Audio] FMOD resource playback resource is not an AudioStream: {resourcePath}");
                return false;
            }

            return TryExtractImportedStream(resourcePath, stream, out absolutePath);
        }

        private static bool TryReadRawPlayableBytes(string resourcePath, out byte[] bytes, out string extension)
        {
            bytes = [];
            extension = string.Empty;

            if (!FileAccess.FileExists(resourcePath))
                return false;

            try
            {
                bytes = FileAccess.GetFileAsBytes(resourcePath);
            }
            catch
            {
                return false;
            }

            return bytes.Length != 0 && TryGetPlayableExtension(bytes, resourcePath, out extension);
        }

        private static bool TryGetPlayableExtension(byte[] bytes, string resourcePath, out string extension)
        {
            extension = string.Empty;
            switch (bytes.Length)
            {
                case >= 12 when
                    bytes[0] == (byte)'R' &&
                    bytes[1] == (byte)'I' &&
                    bytes[2] == (byte)'F' &&
                    bytes[3] == (byte)'F' &&
                    bytes[8] == (byte)'W' &&
                    bytes[9] == (byte)'A' &&
                    bytes[10] == (byte)'V' &&
                    bytes[11] == (byte)'E':
                    extension = ".wav";
                    return true;
                case >= 4 when
                    bytes[0] == (byte)'O' &&
                    bytes[1] == (byte)'g' &&
                    bytes[2] == (byte)'g' &&
                    bytes[3] == (byte)'S':
                    extension = ".ogg";
                    return true;
                case >= 3 when bytes[0] == (byte)'I' && bytes[1] == (byte)'D' && bytes[2] == (byte)'3':
                case >= 2 when bytes[0] == 0xff && (bytes[1] & 0xe0) == 0xe0:
                    extension = ".mp3";
                    return true;
            }

            var sourceExtension = Path.GetExtension(resourcePath).ToLowerInvariant();
            if (sourceExtension is not (".wav" or ".ogg" or ".mp3")) return false;
            extension = sourceExtension;
            return true;
        }

        private static bool TryExtractImportedStream(string resourcePath, AudioStream stream, out string absolutePath)
        {
            absolutePath = string.Empty;
            try
            {
                switch (stream)
                {
                    case AudioStreamMP3 mp3:
                        return TryWriteCached(resourcePath, mp3.GetData(), ".mp3", out absolutePath);
                    case AudioStreamWav wav:
                        return TryWriteCached(resourcePath, BuildWavFile(wav), ".wav", out absolutePath);
                    case AudioStreamOggVorbis ogg:
                        return TryWriteCached(resourcePath, BuildOggFile(ogg), ".ogg", out absolutePath);
                    default:
                        RitsuLibFramework.Logger.Warn(
                            $"[Audio] FMOD resource playback does not know how to extract {stream.GetClass()}: {resourcePath}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[Audio] FMOD resource playback extraction failed: {resourcePath}; {stream.GetClass()}; {ex.Message}");
                return false;
            }
        }

        private static bool TryWriteCached(string resourcePath, byte[] bytes, string extension, out string absolutePath)
        {
            absolutePath = string.Empty;
            if (bytes.Length == 0)
                return false;

            var digest = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            var cacheDir = ProjectSettings.GlobalizePath(CacheRoot);
            var fileName =
                $"{SanitizeFileName(Path.GetFileNameWithoutExtension(resourcePath))}-{digest[..16]}{extension}";
            var path = Path.Combine(cacheDir, fileName);

            lock (Gate)
            {
                Directory.CreateDirectory(cacheDir);
                if (!File.Exists(path) || new FileInfo(path).Length != bytes.Length)
                    File.WriteAllBytes(path, bytes);
            }

            absolutePath = path;
            return true;
        }

        private static byte[] BuildWavFile(AudioStreamWav wav)
        {
            var data = wav.GetData();
            var channels = wav.IsStereo() ? 2 : 1;
            var sampleRate = wav.GetMixRate();
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            short bitsPerSample = wav.GetFormat() switch
            {
                AudioStreamWav.FormatEnum.Format8Bits => 8,
                AudioStreamWav.FormatEnum.Format16Bits => 16,
                _ => throw new NotSupportedException(
                    $"AudioStreamWav format {wav.GetFormat()} cannot be exported to a simple PCM wav file."),
            };

            using var output = new MemoryStream(44 + data.Length);
            using var writer = new BinaryWriter(output);
            var byteRate = sampleRate * channels * bitsPerSample / 8;
            var blockAlign = (short)(channels * bitsPerSample / 8);

            writer.Write("RIFF"u8);
            writer.Write(36 + data.Length);
            writer.Write("WAVE"u8);
            writer.Write("fmt "u8);
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);
            writer.Write("data"u8);
            writer.Write(data.Length);
            writer.Write(data);
            return output.ToArray();
        }

        private static byte[] BuildOggFile(AudioStreamOggVorbis ogg)
        {
            var sequence = ogg.GetPacketSequence() ??
                           throw new NotSupportedException("AudioStreamOggVorbis has no packet sequence.");
            var packetPages = sequence.GetPacketData();
            var granulePositions = sequence.GetPacketGranulePositions();
            if (packetPages.Count == 0)
                throw new NotSupportedException("AudioStreamOggVorbis packet sequence is empty.");

            using var output = new MemoryStream();
            var serial = BitConverter.ToUInt32(SHA256.HashData(BitConverter.GetBytes(sequence.GetLength())), 0);
            for (var pageIndex = 0; pageIndex < packetPages.Count; pageIndex++)
            {
                var page = packetPages[pageIndex];
                var granule = pageIndex < granulePositions.Length ? granulePositions[pageIndex] : 0;
                var headerType = (byte)0;
                if (pageIndex == 0)
                    headerType |= 0x02;
                if (pageIndex == packetPages.Count - 1)
                    headerType |= 0x04;

                WriteOggPage(output, page, granule, serial, pageIndex, headerType);
            }

            return output.ToArray();
        }

        private static void WriteOggPage(Stream output, GArray packets, long granulePosition, uint serial,
            int sequenceNumber, byte headerType)
        {
            var segmentTable = new List<byte>();
            var payload = new List<byte>();
            foreach (var packetVariant in packets)
            {
                var packet = packetVariant.AsByteArray();
                var offset = 0;
                while (packet.Length - offset >= 255)
                {
                    segmentTable.Add(255);
                    payload.AddRange(packet.AsSpan(offset, 255).ToArray());
                    offset += 255;
                }

                segmentTable.Add((byte)(packet.Length - offset));
                if (packet.Length > offset)
                    payload.AddRange(packet.AsSpan(offset).ToArray());
            }

            if (segmentTable.Count > 255)
                throw new NotSupportedException("Ogg page has more than 255 lacing segments.");

            using var page = new MemoryStream();
            using (var writer = new BinaryWriter(page, Encoding.UTF8, true))
            {
                writer.Write("OggS"u8);
                writer.Write((byte)0);
                writer.Write(headerType);
                writer.Write(granulePosition);
                writer.Write(serial);
                writer.Write(sequenceNumber);
                writer.Write(0);
                writer.Write((byte)segmentTable.Count);
                writer.Write(segmentTable.ToArray());
                writer.Write(payload.ToArray());
            }

            var bytes = page.ToArray();
            var crc = ComputeOggCrc(bytes);
            bytes[22] = (byte)(crc & 0xff);
            bytes[23] = (byte)((crc >> 8) & 0xff);
            bytes[24] = (byte)((crc >> 16) & 0xff);
            bytes[25] = (byte)((crc >> 24) & 0xff);
            output.Write(bytes);
        }

        private static uint ComputeOggCrc(byte[] bytes)
        {
            return bytes.Aggregate<byte, uint>(0,
                (current, b) => (current << 8) ^ OggCrcTable[((current >> 24) & 0xff) ^ b]);
        }

        private static uint[] BuildOggCrcTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < table.Length; i++)
            {
                var r = i << 24;
                for (var j = 0; j < 8; j++)
                    r = (r & 0x80000000) != 0 ? (r << 1) ^ OggCrcPolynomial : r << 1;

                table[i] = r;
            }

            return table;
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "audio";

            var invalid = Path.GetInvalidFileNameChars();
            var chars = value.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
            return new(chars);
        }
    }
}
