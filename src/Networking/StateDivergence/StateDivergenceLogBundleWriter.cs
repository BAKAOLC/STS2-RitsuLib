using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Godot;

namespace STS2RitsuLib.Networking.StateDivergence
{
    internal static class StateDivergenceLogBundleWriter
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

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
            out string? errorMessage)
        {
            zipPath = null;
            errorMessage = null;
            string? bundleDir = null;
            try
            {
                var logsDir = ResolveLogsDirectory();
                Directory.CreateDirectory(logsDir);

                var runId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var baseName =
                    $"ritsulib_state_divergence_{runId}_checksum_{report.LocalChecksum.Id}_{report.LocalChecksum.Checksum:x8}";
                bundleDir = Path.Combine(logsDir, baseName);
                Directory.CreateDirectory(bundleDir);

                File.WriteAllText(
                    Path.Combine(bundleDir, "state-divergence-report.txt"),
                    StateDivergenceDiagnosticsPanel.BuildExportReport(report),
                    Utf8NoBom);
                WriteJson(Path.Combine(bundleDir, "metadata.json"),
                    BuildMetadata(report, localLogs, remoteLogs, trigger));
                WriteJson(Path.Combine(bundleDir, "local-debug-log.records.json"), localLogs?.Records ?? []);
                WriteJson(Path.Combine(bundleDir, "remote-debug-log.records.json"), remoteLogs?.Records ?? []);

                zipPath = Path.Combine(logsDir, baseName + ".zip");
                if (File.Exists(zipPath))
                    File.Delete(zipPath);
                ZipFile.CreateFromDirectory(bundleDir, zipPath, CompressionLevel.Optimal, false);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(bundleDir) && Directory.Exists(bundleDir))
                    try
                    {
                        Directory.Delete(bundleDir, true);
                    }
                    catch
                    {
                        // ignored
                    }
            }
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

        private static string ResolveLogsDirectory()
        {
            var userDataDir = OS.GetUserDataDir();
            return Path.Combine(string.IsNullOrWhiteSpace(userDataDir) ? AppContext.BaseDirectory : userDataDir,
                "logs");
        }
    }
}
