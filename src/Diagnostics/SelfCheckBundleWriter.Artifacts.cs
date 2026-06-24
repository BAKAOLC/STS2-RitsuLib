using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Platform;
using Environment = System.Environment;
using IOFileAccess = System.IO.FileAccess;

namespace STS2RitsuLib.Diagnostics
{
    internal static partial class SelfCheckBundleWriter
    {
        private const long MaxLogFileBytes = 8_388_608L;
        private const long MaxLinuxCoreDumpBytes = 209_715_200L;
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        private static ArtifactCollectionResult CollectSupportArtifacts(string bundleDir)
        {
            var artifacts = new List<ArtifactEntry>();
            var warnings = new List<string>();

            TryCollect("active_replay_flush", () => TryFlushActiveReplay(warnings), warnings);
            TryCollect("logs", () => CopyUserLogTree(bundleDir, artifacts, warnings), warnings);
            TryCollect("crashes", () => CopyCrashDiagnostics(bundleDir, artifacts, warnings), warnings);
            TryCollect("saves", () => CopySaveTree(bundleDir, artifacts, warnings), warnings);
            TryCollect("release_info", () => CopyReleaseInfo(bundleDir, artifacts, warnings), warnings);
            TryCollect("screenshot", () => CaptureScreenshot(bundleDir, artifacts, warnings), warnings);
            TryCollect("runtime_environment", () => WriteRuntimeEnvironment(bundleDir, artifacts, warnings), warnings);
            TryCollect("mod_inventory", () => WriteModInventory(bundleDir, artifacts, warnings), warnings);
            TryCollect("loaded_assemblies", () => WriteLoadedAssemblyInventory(bundleDir, artifacts, warnings),
                warnings);

            return new(artifacts, warnings);
        }

        private static void TryFlushActiveReplay(ICollection<string> warnings)
        {
            try
            {
                if (RunManager.Instance is { IsInProgress: true, CombatReplayWriter.IsRecordingReplay: true })
                    RunManager.Instance.WriteReplay(false);
            }
            catch (Exception ex)
            {
                warnings.Add($"failed to flush active combat replay before collecting saves: {ex.Message}");
            }
        }

        private static void CopyUserLogTree(
            string bundleDir,
            ICollection<ArtifactEntry> artifacts,
            ICollection<string> warnings)
        {
            var userDir = GetUserDataDirectory();
            var sourceDir = Path.Combine(userDir, "logs");
            if (!Directory.Exists(sourceDir))
            {
                warnings.Add($"logs source directory not found: {SanitizeForReport(sourceDir)}");
                return;
            }

            foreach (var source in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories)
                         .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var relative = Path.GetRelativePath(userDir, source);
                var entryName = NormalizeEntryName(relative);
                CopyTextArtifact(source, GetBundlePath(bundleDir, entryName), entryName, "log", MaxLogFileBytes, true,
                    artifacts, warnings);
            }
        }

        private static void CopyCrashDiagnostics(
            string bundleDir,
            ICollection<ArtifactEntry> artifacts,
            ICollection<string> warnings)
        {
            var candidates = new List<string>();
            var sentryReportDir = Path.Combine(GetUserDataDirectory(), "sentry", "reports");
            if (Directory.Exists(sentryReportDir))
                candidates.AddRange(Directory.GetFiles(sentryReportDir));

            if (OS.GetName() == "Windows")
            {
                var crashDumpDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CrashDumps");
                if (Directory.Exists(crashDumpDir))
                {
                    var executableName = Path.GetFileNameWithoutExtension(OS.GetExecutablePath());
                    candidates.AddRange(Directory.GetFiles(crashDumpDir)
                        .Where(file => Path.GetFileName(file)
                            .StartsWith(executableName, StringComparison.OrdinalIgnoreCase)));
                }
            }

            var latestCrash = candidates
                .Where(File.Exists)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
            if (latestCrash != null)
            {
                var lastWrite = File.GetLastWriteTime(latestCrash);
                var entryName =
                    $"crashes/crash_{lastWrite:yyyy-MM-dd_HH-mm-ss}{Path.GetExtension(latestCrash)}";
                CopyBinaryArtifact(latestCrash, GetBundlePath(bundleDir, entryName), entryName, "crash",
                    artifacts, warnings);
            }

            TryCollectLinuxCoreDump(bundleDir, artifacts, warnings);
        }

        private static void CopySaveTree(
            string bundleDir,
            ICollection<ArtifactEntry> artifacts,
            ICollection<string> warnings)
        {
            var accountBasePath = ProjectSettings.GlobalizePath(UserDataPathProvider.GetAccountScopedBasePath(""));
            if (!Directory.Exists(accountBasePath))
            {
                warnings.Add($"account save directory not found: {SanitizeForReport(accountBasePath)}");
                return;
            }

            foreach (var source in Directory.EnumerateFiles(accountBasePath, "*", SearchOption.AllDirectories)
                         .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var relative = Path.GetRelativePath(accountBasePath, source);
                var entryName = NormalizeEntryName(Path.Combine("saves", relative));
                if (Path.GetExtension(source).Equals(".json", StringComparison.OrdinalIgnoreCase))
                    CopyTextArtifact(source, GetBundlePath(bundleDir, entryName), entryName, "save", 0, true,
                        artifacts, warnings);
                else
                    CopyBinaryArtifact(source, GetBundlePath(bundleDir, entryName), entryName, "save",
                        artifacts, warnings);
            }
        }

        private static void CopyReleaseInfo(
            string bundleDir,
            ICollection<ArtifactEntry> artifacts,
            ICollection<string> warnings)
        {
            foreach (var source in EnumerateReleaseInfoPaths())
            {
                if (!File.Exists(source))
                    continue;

                CopyTextArtifact(source, GetBundlePath(bundleDir, "release_info.json"), "release_info.json",
                    "release-info", 0, true, artifacts, warnings);
                return;
            }

            warnings.Add("release_info.json not found beside the game executable.");
        }

        private static void CaptureScreenshot(
            string bundleDir,
            ICollection<ArtifactEntry> artifacts,
            ICollection<string> warnings)
        {
            if (Engine.GetMainLoop() is not SceneTree { Root: { } root })
            {
                warnings.Add("screenshot skipped: no active SceneTree root.");
                return;
            }

            var image = root.GetViewport().GetTexture().GetImage();
            if (image == null || image.IsEmpty())
            {
                warnings.Add("screenshot skipped: viewport image was empty.");
                return;
            }

            var target = GetBundlePath(bundleDir, "screenshot.png");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllBytes(target, image.SavePngToBuffer());
            artifacts.Add(CreateArtifact("screenshot.png", target, "screenshot", "viewport capture"));
        }

        private static void WriteRuntimeEnvironment(
            string bundleDir,
            ICollection<ArtifactEntry> artifacts,
            ICollection<string> warnings)
        {
            var target = GetBundlePath(bundleDir, "diagnostics/runtime_environment.txt");
            var lines = new List<string>
            {
                "=== Runtime Environment ===",
                $"Generated: {DateTime.Now:O}",
                $"RitsuLib Version: {Const.Version}",
                $"RitsuLib Informational Version: {RitsuLibBuildInfo.InformationalVersion}",
                $"RitsuLib Is Dev Build: {RitsuLibBuildInfo.IsDevBuild}",
                $"Compat Branch: {RitsuLibFramework.GetCompatBranchLabel()}",
                $"Host Version: {Sts2HostVersion.ReleaseLabel ?? Sts2HostVersion.Numeric?.ToString() ?? "unknown"}",
                $"Godot OS Name: {SafeGodotString(OS.GetName)}",
                $"Godot Executable Path: {SanitizeForReport(SafeGodotString(OS.GetExecutablePath))}",
                $"Godot User Data Dir: {SanitizeForReport(SafeGodotString(OS.GetUserDataDir))}",
                $"Godot Data Dir: {SanitizeForReport(SafeGodotString(OS.GetDataDir))}",
                $"user://: {SanitizeForReport(GetUserDataDirectory())}",
                $"Account Scoped Base Path: {SanitizeForReport(ProjectSettings.GlobalizePath(UserDataPathProvider.GetAccountScopedBasePath("")))}",
                $".NET Framework: {RuntimeInformation.FrameworkDescription}",
                $"OS Description: {RuntimeInformation.OSDescription}",
                $"OS Architecture: {RuntimeInformation.OSArchitecture}",
                $"Process Architecture: {RuntimeInformation.ProcessArchitecture}",
                $"Is 64-bit Process: {Environment.Is64BitProcess}",
                $"Process ID: {Environment.ProcessId}",
                $"Processor Count: {Environment.ProcessorCount}",
                $"Current Directory: {SanitizeForReport(Environment.CurrentDirectory)}",
                $"Proton Launch: {SteamCompatibilityRuntime.IsProtonLaunch}",
                $"STEAM_COMPAT_DATA_PATH present: {HasEnvironmentValue("STEAM_COMPAT_DATA_PATH")}",
                $"STEAM_COMPAT_CLIENT_INSTALL_PATH present: {HasEnvironmentValue("STEAM_COMPAT_CLIENT_INSTALL_PATH")}",
                $"WINEPREFIX present: {HasEnvironmentValue("WINEPREFIX")}",
                $"DOTNET_ROOT present: {HasEnvironmentValue("DOTNET_ROOT")}",
                RuntimeFrameworkVersionSummary.TryBuildBaseLibDisplayLine(out var baseLibLine)
                    ? baseLibLine
                    : "BaseLib: not detected",
                "",
                "Build Metadata:",
            };

            lines.AddRange(RitsuLibBuildInfo.Metadata.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"- {pair.Key}: {SanitizeForReport(pair.Value)}"));

            TryWriteTextFile(target, string.Join(Environment.NewLine, lines) + Environment.NewLine, warnings);
            artifacts.Add(CreateArtifact("diagnostics/runtime_environment.txt", target, "diagnostic", null));
        }

        private static void WriteModInventory(
            string bundleDir,
            ICollection<ArtifactEntry> artifacts,
            ICollection<string> warnings)
        {
            var target = GetBundlePath(bundleDir, "diagnostics/mod_inventory.tsv");
            var sb = new StringBuilder();
            sb.AppendLine(
                "scope\tid\tname\tversion\tstate\tsource\taffectsGameplay\tassemblyName\tassemblyVersion\terrorCount\terrors");

            foreach (var mod in Sts2ModManagerCompat.BuildLoadedModInventoryEntries()
                         .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                AppendModInventoryLine(sb, "loaded", mod);

            foreach (var mod in Sts2ModManagerCompat.BuildModInventoryEntries()
                         .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                AppendModInventoryLine(sb, "registered", mod);

            TryWriteTextFile(target, sb.ToString(), warnings);
            artifacts.Add(CreateArtifact("diagnostics/mod_inventory.tsv", target, "diagnostic", null));
        }

        private static void WriteLoadedAssemblyInventory(
            string bundleDir,
            ICollection<ArtifactEntry> artifacts,
            ICollection<string> warnings)
        {
            var target = GetBundlePath(bundleDir, "diagnostics/loaded_assemblies.tsv");
            var sb = new StringBuilder();
            sb.AppendLine("name\tversion\tinformationalVersion\tlocation");

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()
                         .OrderBy(assembly => assembly.GetName().Name, StringComparer.OrdinalIgnoreCase))
                try
                {
                    var name = assembly.GetName();
                    var informationalVersion =
                        assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                    var location = assembly.IsDynamic ? "<dynamic>" : assembly.Location;
                    sb.AppendLine(string.Join('\t',
                        EscapeTsv(name.Name),
                        EscapeTsv(name.Version?.ToString()),
                        EscapeTsv(informationalVersion),
                        EscapeTsv(SanitizeForReport(location))));
                }
                catch (Exception ex)
                {
                    warnings.Add($"failed to inspect loaded assembly: {ex.Message}");
                }

            TryWriteTextFile(target, sb.ToString(), warnings);
            artifacts.Add(CreateArtifact("diagnostics/loaded_assemblies.tsv", target, "diagnostic", null));
        }

        private static void TryCollectLinuxCoreDump(
            string bundleDir,
            ICollection<ArtifactEntry> artifacts,
            ICollection<string> warnings)
        {
            if (OS.GetName() != "Linux")
                return;

            var executableName = Path.GetFileName(OS.GetExecutablePath());
            TryRunProcessText(
                "coredumpctl",
                $"info -1 --no-pager {executableName}",
                TimeSpan.FromSeconds(10),
                out var infoText,
                warnings);
            if (!string.IsNullOrWhiteSpace(infoText))
            {
                const string entryName = "crashes/coredump_info.txt";
                var target = GetBundlePath(bundleDir, entryName);
                TryWriteTextFile(target, SanitizeForReport(infoText), warnings);
                artifacts.Add(CreateArtifact(entryName, target, "crash", "coredumpctl info"));
            }

            var tempCorePath = Path.Combine(Path.GetTempPath(), $"sts2_coredump_{Guid.NewGuid():N}.core");
            try
            {
                TryRunProcessText(
                    "coredumpctl",
                    $"dump -1 --no-pager -o \"{tempCorePath}\" {executableName}",
                    TimeSpan.FromSeconds(10),
                    out _,
                    warnings);
                if (!File.Exists(tempCorePath))
                    return;

                var length = new FileInfo(tempCorePath).Length;
                if (length > MaxLinuxCoreDumpBytes)
                {
                    warnings.Add(
                        $"Linux core dump skipped because it is {FormatByteCount(length)} (limit {FormatByteCount(MaxLinuxCoreDumpBytes)}).");
                    return;
                }

                CopyBinaryArtifact(tempCorePath, GetBundlePath(bundleDir, "crashes/coredump.core"),
                    "crashes/coredump.core", "crash", artifacts, warnings);
            }
            finally
            {
                TryDelete(tempCorePath);
            }
        }

        private static void TryRunProcessText(
            string fileName,
            string arguments,
            TimeSpan timeout,
            out string? output,
            ICollection<string> warnings)
        {
            output = null;
            try
            {
                using var process = new Process();
                process.StartInfo = new()
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                process.Start();
                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit((int)timeout.TotalMilliseconds))
                {
                    process.Kill();
                    process.WaitForExit(5000);
                    warnings.Add($"{fileName} timed out after {timeout.TotalSeconds:0}s.");
                    return;
                }

                output = stdoutTask.GetAwaiter().GetResult();
                var stderr = stderrTask.GetAwaiter().GetResult();
                if (process.ExitCode != 0)
                    warnings.Add($"{fileName} exited with code {process.ExitCode}: {SanitizeForReport(stderr)}");
            }
            catch (Exception ex)
            {
                warnings.Add($"failed to run {fileName}: {ex.Message}");
            }
        }

        private static void CopyTextArtifact(
            string source,
            string target,
            string entryName,
            string kind,
            long maxBytes,
            bool sanitize,
            ICollection<ArtifactEntry> artifacts,
            ICollection<string> warnings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                using var stream = new FileStream(source, FileMode.Open, IOFileAccess.Read, FileShare.ReadWrite);
                var text = ReadTailText(stream, maxBytes);
                if (sanitize)
                    text = SanitizeForReport(text);
                File.WriteAllText(target, text, Utf8NoBom);
                artifacts.Add(CreateArtifact(entryName, source, kind,
                    maxBytes > 0 && stream.Length > maxBytes ? $"tail {FormatByteCount(maxBytes)}" : null));
            }
            catch (Exception ex)
            {
                warnings.Add($"failed to copy text artifact {SanitizeForReport(source)}: {ex.Message}");
            }
        }

        private static void CopyBinaryArtifact(
            string source,
            string target,
            string entryName,
            string kind,
            ICollection<ArtifactEntry> artifacts,
            ICollection<string> warnings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                using var input = new FileStream(source, FileMode.Open, IOFileAccess.Read, FileShare.ReadWrite);
                using var output = new FileStream(target, FileMode.Create, IOFileAccess.Write, FileShare.None);
                input.CopyTo(output);
                artifacts.Add(CreateArtifact(entryName, source, kind, null));
            }
            catch (Exception ex)
            {
                warnings.Add($"failed to copy binary artifact {SanitizeForReport(source)}: {ex.Message}");
            }
        }

        private static string ReadTailText(Stream stream, long maxBytes)
        {
            var truncated = false;
            if (maxBytes > 0 && stream.Length > maxBytes)
            {
                stream.Seek(stream.Length - maxBytes, SeekOrigin.Begin);
                while (stream.Position < stream.Length)
                {
                    var b = stream.ReadByte();
                    if (b == -1)
                        break;

                    if ((b & 0xC0) == 0x80) continue;
                    stream.Seek(-1, SeekOrigin.Current);
                    break;
                }

                truncated = true;
            }

            using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true);
            var text = reader.ReadToEnd();
            if (!truncated)
                return text;

            var newline = text.IndexOf('\n', StringComparison.Ordinal);
            if (newline >= 0)
                text = text[(newline + 1)..];
            return $"[...truncated, showing last ~{maxBytes / 1_048_576} MB...]\n{text}";
        }

        private static IEnumerable<string> EnumerateReleaseInfoPaths()
        {
            var executablePath = OS.GetExecutablePath();
            var executableDir = Path.GetDirectoryName(executablePath);
            if (string.IsNullOrWhiteSpace(executableDir))
                yield break;

            if (OS.GetName() == "macOS")
                yield return Path.GetFullPath(Path.Combine(executableDir, "..", "Resources", "release_info.json"));

            yield return Path.Combine(executableDir, "release_info.json");
        }

        private static void AppendModInventoryLine(StringBuilder sb, string scope, Sts2ModInventoryEntry mod)
        {
            var errors = string.Join(" | ", mod.Errors.Select(error => error.ToString()));
            sb.AppendLine(string.Join('\t',
                EscapeTsv(scope),
                EscapeTsv(mod.Id),
                EscapeTsv(mod.Name),
                EscapeTsv(mod.Version),
                EscapeTsv(mod.State),
                EscapeTsv(mod.Source),
                EscapeTsv(mod.AffectsGameplay.ToString()),
                EscapeTsv(mod.AssemblyName),
                EscapeTsv(mod.AssemblyVersion),
                EscapeTsv(mod.Errors.Count.ToString()),
                EscapeTsv(SanitizeForReport(errors))));
        }

        private static void TryCollect(string label, Action action, ICollection<string> warnings)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                warnings.Add($"{label} collection failed: {ex.Message}");
            }
        }

        private static void TryWriteTextFile(string target, string text, ICollection<string> warnings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllText(target, text, Utf8NoBom);
            }
            catch (Exception ex)
            {
                warnings.Add($"failed to write {NormalizeEntryName(target)}: {ex.Message}");
            }
        }

        private static string GetUserDataDirectory()
        {
            return ProjectSettings.GlobalizePath("user://");
        }

        private static string GetBundlePath(string bundleDir, string entryName)
        {
            return Path.Combine(bundleDir, entryName.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string NormalizeEntryName(string path)
        {
            return path.Replace('\\', '/').TrimStart('/');
        }

        private static string EscapeTsv(string? value)
        {
            return (value ?? "")
                .Replace('\t', ' ')
                .Replace('\r', ' ')
                .Replace('\n', ' ');
        }

        private static string SafeGodotString(Func<string> read)
        {
            try
            {
                return read();
            }
            catch (Exception ex)
            {
                return $"<error: {ex.Message}>";
            }
        }

        private static bool HasEnvironmentValue(string name)
        {
            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name));
        }

        private static string SanitizeForReport(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return text ?? "";

            try
            {
                return LogSanitizer.Sanitize(text);
            }
            catch
            {
                return text;
            }
        }

        private static ArtifactEntry CreateArtifact(string entryName, string sourcePath, string kind, string? note)
        {
            return new(
                NormalizeEntryName(entryName),
                SanitizeForReport(sourcePath),
                TryGetFileSize(sourcePath),
                kind,
                note ?? "");
        }

        private static long? TryGetFileSize(string path)
        {
            try
            {
                return File.Exists(path) ? new FileInfo(path).Length : null;
            }
            catch
            {
                return null;
            }
        }

        private static string FormatByteCount(long? bytes)
        {
            if (bytes == null)
                return "unknown";

            var value = bytes.Value;
            return value switch
            {
                >= 1_073_741_824 => $"{value / 1_073_741_824d:0.##} GiB",
                >= 1_048_576 => $"{value / 1_048_576d:0.##} MiB",
                >= 1024 => $"{value / 1024d:0.##} KiB",
                _ => $"{value} B",
            };
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best-effort temporary file cleanup.
            }
        }

        private readonly record struct ArtifactCollectionResult(
            IReadOnlyList<ArtifactEntry> Artifacts,
            IReadOnlyList<string> Warnings);

        private readonly record struct ArtifactEntry(
            string EntryName,
            string SourcePath,
            long? SizeBytes,
            string Kind,
            string Note);
    }
}
