using System.Diagnostics;
using Godot;

namespace STS2RitsuLib.Utils.Persistence
{
    internal static class RitsuLibDataPaths
    {
        private static readonly Lock StorageDirectoryGate = new();
        private static readonly string SharedCacheRootPath = ResolveSharedCacheRootPath();

        private static readonly string SessionDirectoryName =
            $"session-{System.Environment.ProcessId}-{Guid.NewGuid():N}";

        private static bool _staleSessionsCleaned;

        static RitsuLibDataPaths()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        internal static string ModDataDirectory
        {
            get
            {
                var dataDirectory = ProfileManager.GetAccountBasePath();
                return dataDirectory.StartsWith("user://", StringComparison.OrdinalIgnoreCase)
                    ? ProjectSettings.GlobalizePath(dataDirectory)
                    : Path.GetFullPath(dataDirectory);
            }
        }

        internal static string TemporaryDirectory { get; } =
            Path.Combine(SharedCacheRootPath, SessionDirectoryName);

        internal static readonly string SharedCacheDirectory = SharedCacheRootPath;

        internal static string EnsureSharedCacheDirectory()
        {
            lock (StorageDirectoryGate)
            {
                EnsurePrivateDirectory(SharedCacheRootPath);
                if (!_staleSessionsCleaned)
                {
                    CleanupStaleSessions(SharedCacheRootPath);
                    _staleSessionsCleaned = true;
                }

                return SharedCacheRootPath;
            }
        }

        internal static string EnsureTemporaryDirectory()
        {
            EnsureSharedCacheDirectory();
            lock (StorageDirectoryGate)
            {
                EnsurePrivateDirectory(TemporaryDirectory);
                return TemporaryDirectory;
            }
        }

        internal static async Task<FileStream> AcquireSharedCacheLockAsync(
            string fileName,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            var path = Path.Combine(EnsureSharedCacheDirectory(), fileName);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return new(path, FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, FileShare.None, 1,
                        FileOptions.Asynchronous);
                }
                catch (IOException)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        internal static string CreateTemporaryDirectory(string prefix)
        {
            var path = Path.Combine(EnsureTemporaryDirectory(), $"{prefix}-{Guid.NewGuid():N}");
            EnsurePrivateDirectory(path);
            return path;
        }

        private static string ResolveSharedCacheRootPath()
        {
            var cacheDirectory = OS.GetCacheDir();
            if (string.IsNullOrWhiteSpace(cacheDirectory))
                throw new InvalidOperationException("The operating-system cache directory is unavailable.");
            return Path.Combine(Path.GetFullPath(cacheDirectory), Const.ModId);
        }

        private static void CleanupStaleSessions(string root)
        {
            try
            {
                foreach (var directory in Directory.EnumerateDirectories(
                             root,
                             "session-*",
                             SearchOption.TopDirectoryOnly))
                {
                    var name = Path.GetFileName(directory);
                    var components = name.Split('-', 3);
                    if (components.Length != 3 ||
                        !int.TryParse(components[1], out var processId) ||
                        IsProcessRunning(processId))
                        continue;

                    TryDeleteDirectory(directory);
                }
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[Storage] Failed to clean stale temporary data: {ex.Message}");
            }
        }

        private static void EnsurePrivateDirectory(string path)
        {
            if (OperatingSystem.IsWindows())
            {
                Directory.CreateDirectory(path);
                return;
            }

            const UnixFileMode mode =
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
            Directory.CreateDirectory(path, mode);
            File.SetUnixFileMode(path, mode);
        }

        private static bool IsProcessRunning(int processId)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                return true;
            }
        }

        private static void OnProcessExit(object? sender, EventArgs args)
        {
            TryDeleteDirectory(TemporaryDirectory);
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch
            {
                // Best-effort cleanup for disposable session data.
            }
        }
    }
}
