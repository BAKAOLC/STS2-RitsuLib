using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace STS2RitsuLib.Networking.StateDivergence
{
    internal static class StateDivergenceLogBundleWriter
    {
        private const int BundlesToKeep = 5;
        private const string BundlePrefix = "ritsulib_state_divergence_";
        private const string LocalLogsEntryName = "local-debug-log.records.json";
        private const string MetadataEntryName = "metadata.json";
        private const string RemoteLogsEntryName = "remote-debug-log.records.json";
        private const string ReportEntryName = "state-divergence-report.txt";
        private const string StagingDirectoryName = ".ritsulib-state-divergence-staging";
        private static readonly Lock BundleWriteLock = new();
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        private static readonly string[] RequiredBundleEntries =
        [
            ReportEntryName,
            MetadataEntryName,
            LocalLogsEntryName,
        ];

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
        };

        public static bool TryWrite(
            StateDivergenceDiagnosticReport report,
            StateDivergenceRecentLogSnapshot? localLogs,
            StateDivergenceRecentLogSnapshot? remoteLogs,
            string trigger,
            out string? zipPath,
            out string? zipFileName,
            out string? errorMessage)
        {
            zipPath = null;
            zipFileName = null;
            errorMessage = null;
            lock (BundleWriteLock)
            {
                string? stagingDir = null;
                try
                {
                    var logsDir = ResolveLogsDirectory();
                    Directory.CreateDirectory(logsDir);

                    var runId = DateTime.Now.ToString("yyyyMMdd_HHmmss_fffffff");
                    var baseName =
                        $"{BundlePrefix}{runId}_checksum_{report.LocalChecksum.Id}_{report.LocalChecksum.Checksum:x8}_{Guid.NewGuid():N}";
                    stagingDir = Path.Combine(ResolveStagingDirectory(), Guid.NewGuid().ToString("N"));
                    var payloadDir = Path.Combine(stagingDir, "payload");
                    Directory.CreateDirectory(payloadDir);

                    File.WriteAllText(
                        Path.Combine(payloadDir, ReportEntryName),
                        StateDivergenceDiagnosticsPanel.BuildExportReport(report),
                        Utf8NoBom);
                    WriteJson(Path.Combine(payloadDir, MetadataEntryName),
                        BuildMetadata(report, localLogs, remoteLogs, trigger));
                    WriteJson(Path.Combine(payloadDir, LocalLogsEntryName), localLogs?.Records ?? []);
                    if (remoteLogs != null)
                        WriteJson(Path.Combine(payloadDir, RemoteLogsEntryName), remoteLogs.Records);

                    var stagedZipPath = Path.Combine(stagingDir, baseName + ".zip");
                    ZipFile.CreateFromDirectory(payloadDir, stagedZipPath, CompressionLevel.Optimal, false);
                    ValidateBundle(stagedZipPath, remoteLogs != null);

                    var publishedZipPath = Path.Combine(logsDir, Path.GetFileName(stagedZipPath));
                    File.Move(stagedZipPath, publishedZipPath);
                    zipPath = publishedZipPath;
                    zipFileName = Path.GetFileName(publishedZipPath);
                    PruneOldBundles(logsDir, publishedZipPath);
                    return true;
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    return false;
                }
                finally
                {
                    if (!string.IsNullOrWhiteSpace(stagingDir) && Directory.Exists(stagingDir))
                        try
                        {
                            Directory.Delete(stagingDir, true);
                        }
                        catch
                        {
                            // ignored
                        }
                }
            }
        }

        internal static bool IsPublishedBundlePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                var fullPath = Path.GetFullPath(path);
                var directory = Path.GetDirectoryName(fullPath);
                var fileName = Path.GetFileName(fullPath);
                var comparison = OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;
                return string.Equals(directory, Path.GetFullPath(ResolveLogsDirectory()), comparison) &&
                       fileName.StartsWith(BundlePrefix, StringComparison.Ordinal) &&
                       fileName.EndsWith(".zip", StringComparison.Ordinal);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                return false;
            }
        }

        internal static byte[] BuildSanitizedSubmissionBundle(string sourcePath)
        {
            using var sourceArchive = ZipFile.OpenRead(sourcePath);
            ValidateBundleEntries(sourceArchive, null);

            using var output = new MemoryStream();
            using (var submissionArchive = new ZipArchive(output, ZipArchiveMode.Create, true))
            {
                foreach (var sourceEntry in sourceArchive.Entries)
                {
                    var submissionEntry = submissionArchive.CreateEntry(
                        sourceEntry.FullName,
                        CompressionLevel.Optimal);
                    using var source = sourceEntry.Open();
                    using var destination = submissionEntry.Open();
                    if (sourceEntry.FullName.EndsWith(".json", StringComparison.Ordinal))
                        WriteSanitizedJson(source, destination);
                    else
                        WriteSanitizedText(source, destination);
                }
            }

            return output.ToArray();
        }

        private static object BuildMetadata(
            StateDivergenceDiagnosticReport report,
            StateDivergenceRecentLogSnapshot? localLogs,
            StateDivergenceRecentLogSnapshot? remoteLogs,
            string trigger)
        {
            return new
            {
                generatedAtUtc = DateTimeOffset.UtcNow,
                trigger,
                ritsuLibVersion = Const.Version,
                report.Role,
                report.RemotePeerId,
                localChecksum = report.LocalChecksum,
                remoteChecksum = report.RemoteChecksum,
                localLogs = Summarize(localLogs),
                remoteLogs = Summarize(remoteLogs),
            };
        }

        private static object Summarize(StateDivergenceRecentLogSnapshot? logs)
        {
            if (logs == null)
                return new
                {
                    available = false,
                };

            return new
            {
                available = true,
                logs.CapturedAtUtc,
                logs.TotalRecordCount,
                logs.IncludedRecordCount,
                logs.DroppedOldRecordCount,
            };
        }

        private static void WriteJson(string path, object value)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions), Utf8NoBom);
        }

        private static void ValidateBundle(string path, bool expectRemoteLogs)
        {
            using var archive = ZipFile.OpenRead(path);
            ValidateBundleEntries(archive, expectRemoteLogs);

            foreach (var entry in archive.Entries)
            {
                using var entryStream = entry.Open();
                entryStream.CopyTo(Stream.Null);
            }
        }

        private static void ValidateBundleEntries(ZipArchive archive, bool? expectRemoteLogs)
        {
            var entries = archive.Entries.ToDictionary(entry => entry.FullName, StringComparer.Ordinal);
            foreach (var requiredEntry in RequiredBundleEntries)
                if (!entries.ContainsKey(requiredEntry))
                    throw new InvalidDataException(
                        $"The state divergence bundle is missing the required entry '{requiredEntry}'.");

            var hasRemoteLogs = entries.ContainsKey(RemoteLogsEntryName);
            if (expectRemoteLogs.HasValue && hasRemoteLogs != expectRemoteLogs.Value)
                throw new InvalidDataException(expectRemoteLogs.Value
                    ? "The state divergence bundle is missing the remote log entry."
                    : "The state divergence bundle contains an unexpected remote log entry.");

            var expectedEntryCount = RequiredBundleEntries.Length + (hasRemoteLogs ? 1 : 0);
            if (entries.Count != expectedEntryCount)
                throw new InvalidDataException(
                    $"The state divergence bundle contains {entries.Count} entries; expected {expectedEntryCount}.");
        }

        private static void WriteSanitizedJson(Stream source, Stream destination)
        {
            using var document = JsonDocument.Parse(source);
            using var writer = new Utf8JsonWriter(destination, new()
            {
                Indented = true,
            });
            WriteSanitizedJsonElement(document.RootElement, writer);
        }

        private static void WriteSanitizedJsonElement(JsonElement element, Utf8JsonWriter writer)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (var property in element.EnumerateObject())
                    {
                        writer.WritePropertyName(property.Name);
                        WriteSanitizedJsonElement(property.Value, writer);
                    }

                    writer.WriteEndObject();
                    break;
                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in element.EnumerateArray())
                        WriteSanitizedJsonElement(item, writer);
                    writer.WriteEndArray();
                    break;
                case JsonValueKind.String:
                    writer.WriteStringValue(LogSanitizer.Sanitize(element.GetString() ?? ""));
                    break;
                default:
                    element.WriteTo(writer);
                    break;
            }
        }

        private static void WriteSanitizedText(Stream source, Stream destination)
        {
            using var reader = new StreamReader(source, Encoding.UTF8, true, 1024, true);
            using var writer = new StreamWriter(destination, Utf8NoBom, 1024, true);
            writer.Write(LogSanitizer.Sanitize(reader.ReadToEnd()));
        }

        private static void PruneOldBundles(string logsDir, string currentZipPath)
        {
            try
            {
                var currentFullPath = Path.GetFullPath(currentZipPath);
                var bundles = Directory.EnumerateFiles(logsDir, BundlePrefix + "*.zip", SearchOption.TopDirectoryOnly)
                    .Select(path => new FileInfo(path))
                    .Where(file => file.Exists)
                    .OrderByDescending(file => file.LastWriteTimeUtc)
                    .ThenByDescending(file => file.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                foreach (var file in bundles.Skip(BundlesToKeep))
                {
                    if (string.Equals(Path.GetFullPath(file.FullName), currentFullPath,
                            StringComparison.OrdinalIgnoreCase))
                        continue;

                    file.Delete();
                }
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[State divergence diagnostics] failed to prune old diagnostic bundles: {ex.Message}");
            }
        }

        private static string ResolveLogsDirectory()
        {
            return Path.Combine(ResolveUserDataDirectory(), "logs");
        }

        private static string ResolveStagingDirectory()
        {
            return Path.Combine(ResolveUserDataDirectory(), StagingDirectoryName);
        }

        private static string ResolveUserDataDirectory()
        {
            var userDataDir = OS.GetUserDataDir();
            return string.IsNullOrWhiteSpace(userDataDir) ? AppContext.BaseDirectory : userDataDir;
        }
    }
}
