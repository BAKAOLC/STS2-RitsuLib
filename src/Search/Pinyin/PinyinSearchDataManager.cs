using System.Net.Http.Headers;
using System.Security.Cryptography;
using Godot;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Toast;
using STS2RitsuLib.Utils.Persistence;
using HttpClient = System.Net.Http.HttpClient;

namespace STS2RitsuLib.Search.Pinyin
{
    internal enum PinyinSearchDataState
    {
        NotInstalled,
        Loading,
        Downloading,
        Generating,
        Ready,
        Failed,
    }

    internal sealed record PinyinSearchDataStatus(
        PinyinSearchDataState State,
        string UnicodeVersion,
        long BytesReceived = 0,
        long TotalBytes = 0,
        long CacheBytes = 0,
        bool SourceArchiveCached = false,
        string? Error = null)
    {
        internal bool IsBusy => State is PinyinSearchDataState.Loading or PinyinSearchDataState.Downloading or
            PinyinSearchDataState.Generating;

        internal bool IsReady => State == PinyinSearchDataState.Ready;
    }

    internal static class PinyinSearchDataManager
    {
        private const int BufferSize = 64 * 1024;
        private const long CachePresenceCheckIntervalMilliseconds = 1000;
        private const long MaximumDownloadBytes = 16L * 1024 * 1024;
        private const string CompiledFileName = "pinyin-data.v1.bin";
        private const string DownloadTemporaryFileName = "pinyin-unihan.download.tmp";
        private const string GeneratedTemporaryFileName = "pinyin-data.generate.tmp";
        private const string LockFileName = "pinyin-data.lock";
        private const string SourceArchiveFileName = "pinyin-unihan.zip";
        private static readonly HttpClient Client = CreateClient();
        private static readonly SemaphoreSlim OperationGate = new(1, 1);
        private static readonly Lock StateLock = new();
        private static PinyinSearchData? _data;
        private static long _nextCachePresenceCheck;

        private static PinyinSearchDataStatus _status = new(
            PinyinSearchDataState.NotInstalled,
            PinyinSearchDataSource.Current.UnicodeVersion);

        internal static event Action? DataChanged;

        internal static PinyinSearchData? Data
        {
            get
            {
                EnsureCachedDataStillInstalled(false);
                return Volatile.Read(ref _data);
            }
        }

        internal static PinyinSearchDataStatus GetStatus()
        {
            EnsureCachedDataStillInstalled(true);
            lock (StateLock)
            {
                return _status;
            }
        }

        internal static async Task TryLoadCachedAsync(CancellationToken cancellationToken = default)
        {
            await OperationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await using var processLock =
                    await RitsuLibDataPaths.AcquireSharedCacheLockAsync(LockFileName, cancellationToken)
                        .ConfigureAwait(false);
                SetStatus(PinyinSearchDataState.Loading);
                var compiledPath = GetCachePath(CompiledFileName);
                if (!File.Exists(compiledPath))
                {
                    Volatile.Write(ref _data, null);
                    SetNotInstalledStatus();
                    return;
                }

                var loaded = await Task.Run(
                        () => PinyinSearchData.Load(compiledPath, PinyinSearchDataSource.Current),
                        cancellationToken)
                    .ConfigureAwait(false);
                Volatile.Write(ref _data, loaded);
                SetReadyStatus();
                DataChanged?.Invoke();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                Volatile.Write(ref _data, null);
                SetFailure(ex);
                RitsuLibFramework.Logger.Warn($"[Search/Pinyin] Could not load cached data: {ex.Message}");
            }
            finally
            {
                OperationGate.Release();
            }
        }

        internal static async Task DownloadAndInstallAsync(
            bool force,
            CancellationToken cancellationToken = default)
        {
            await OperationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await using var processLock =
                    await RitsuLibDataPaths.AcquireSharedCacheLockAsync(LockFileName, cancellationToken)
                        .ConfigureAwait(false);
                var source = PinyinSearchDataSource.Current;
                RitsuLibDataPaths.EnsureSharedCacheDirectory();
                RitsuLibDataPaths.EnsureTemporaryDirectory();
                var compiledPath = GetCachePath(CompiledFileName);
                if (!force && File.Exists(compiledPath))
                {
                    try
                    {
                        var loaded = await Task.Run(() => PinyinSearchData.Load(compiledPath, source),
                                cancellationToken)
                            .ConfigureAwait(false);
                        Volatile.Write(ref _data, loaded);
                        SetReadyStatus();
                        DataChanged?.Invoke();
                        return;
                    }
                    catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[Search/Pinyin] Cached data is invalid and will be regenerated: {ex.Message}");
                    }
                }

                var sourceArchivePath = GetCachePath(SourceArchiveFileName);
                var temporaryDownloadPath = GetTemporaryPath(DownloadTemporaryFileName);
                var temporaryGeneratedPath = GetTemporaryPath(GeneratedTemporaryFileName);
                DeleteIfExists(temporaryDownloadPath);
                DeleteIfExists(temporaryGeneratedPath);

                var generationSourcePath = await AcquireSourceArchiveAsync(
                        sourceArchivePath,
                        temporaryDownloadPath,
                        cancellationToken)
                    .ConfigureAwait(false);
                try
                {
                    SetStatus(PinyinSearchDataState.Generating);
                    var readings = await Task.Run(
                            () => PinyinSearchDataCompiler.Compile(generationSourcePath),
                            cancellationToken)
                        .ConfigureAwait(false);
                    await Task.Run(
                            () => PinyinSearchData.Write(temporaryGeneratedPath, source, readings),
                            cancellationToken)
                        .ConfigureAwait(false);
                    var loaded = await Task.Run(
                            () => PinyinSearchData.Load(temporaryGeneratedPath, source),
                            cancellationToken)
                        .ConfigureAwait(false);
                    PublishTemporaryFile(temporaryGeneratedPath, compiledPath);
                    Volatile.Write(ref _data, loaded);
                    if (!RitsuSearchSettingsStore.GetKeepPinyinSourceArchive())
                        DeleteIfExists(sourceArchivePath);
                    SetReadyStatus();
                    DataChanged?.Invoke();
                    RitsuLibFramework.Logger.Info(
                        $"[Search/Pinyin] Unicode {source.UnicodeVersion} data is ready ({loaded.Count} code points).");
                }
                finally
                {
                    DeleteIfExists(temporaryDownloadPath);
                    DeleteIfExists(temporaryGeneratedPath);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (Data == null)
                    SetNotInstalledStatus();
                else
                    SetReadyStatus();
                throw;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                SetFailure(ex);
                RitsuLibFramework.Logger.Warn($"[Search/Pinyin] Data installation failed: {ex.Message}");
                throw;
            }
            finally
            {
                OperationGate.Release();
            }
        }

        internal static async Task RebuildFromCachedSourceAsync(CancellationToken cancellationToken = default)
        {
            await OperationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await using var processLock =
                    await RitsuLibDataPaths.AcquireSharedCacheLockAsync(LockFileName, cancellationToken)
                        .ConfigureAwait(false);
                var source = PinyinSearchDataSource.Current;
                var sourcePath = GetCachePath(SourceArchiveFileName);
                await ValidateSourceArchiveAsync(sourcePath, cancellationToken).ConfigureAwait(false);
                SetStatus(PinyinSearchDataState.Generating);
                RitsuLibDataPaths.EnsureTemporaryDirectory();
                var temporaryGeneratedPath = GetTemporaryPath(GeneratedTemporaryFileName);
                var compiledPath = GetCachePath(CompiledFileName);
                DeleteIfExists(temporaryGeneratedPath);
                try
                {
                    var readings = await Task.Run(() => PinyinSearchDataCompiler.Compile(sourcePath), cancellationToken)
                        .ConfigureAwait(false);
                    await Task.Run(
                            () => PinyinSearchData.Write(temporaryGeneratedPath, source, readings),
                            cancellationToken)
                        .ConfigureAwait(false);
                    var loaded = PinyinSearchData.Load(temporaryGeneratedPath, source);
                    PublishTemporaryFile(temporaryGeneratedPath, compiledPath);
                    Volatile.Write(ref _data, loaded);
                    SetReadyStatus();
                    DataChanged?.Invoke();
                }
                finally
                {
                    DeleteIfExists(temporaryGeneratedPath);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                SetFailure(ex);
                throw;
            }
            finally
            {
                OperationGate.Release();
            }
        }

        internal static async Task DeleteCachedDataAsync(CancellationToken cancellationToken = default)
        {
            await OperationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await using var processLock =
                    await RitsuLibDataPaths.AcquireSharedCacheLockAsync(LockFileName, cancellationToken)
                        .ConfigureAwait(false);
                Volatile.Write(ref _data, null);
                foreach (var fileName in new[] { CompiledFileName, SourceArchiveFileName })
                    DeleteIfExists(GetCachePath(fileName));
                foreach (var fileName in new[] { DownloadTemporaryFileName, GeneratedTemporaryFileName })
                    DeleteIfExists(GetTemporaryPath(fileName));
                SetStatus(PinyinSearchDataState.NotInstalled);
                DataChanged?.Invoke();
            }
            finally
            {
                OperationGate.Release();
            }
        }

        private static async Task<string> AcquireSourceArchiveAsync(
            string cachedPath,
            string temporaryPath,
            CancellationToken cancellationToken)
        {
            if (File.Exists(cachedPath))
            {
                try
                {
                    await ValidateSourceArchiveAsync(cachedPath, cancellationToken).ConfigureAwait(false);
                    return cachedPath;
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Search/Pinyin] Cached Unicode source is invalid and will be replaced: {ex.Message}");
                }
            }

            var source = PinyinSearchDataSource.Current;
            SetStatus(PinyinSearchDataState.Downloading, 0, source.ExpectedLength);
            using var response = await Client.GetAsync(source.SourceUri, HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is { } contentLength &&
                contentLength is <= 0 or > MaximumDownloadBytes)
                throw new InvalidDataException("The Unicode source download has an invalid content length.");

            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var output = new FileStream(temporaryPath, FileMode.CreateNew, System.IO.FileAccess.Write,
                FileShare.None,
                BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[BufferSize];
            long total = 0;
            while (true)
            {
                var count = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (count == 0)
                    break;
                total += count;
                if (total > MaximumDownloadBytes)
                    throw new InvalidDataException("The Unicode source download exceeded the size limit.");
                hash.AppendData(buffer, 0, count);
                await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
                SetStatus(PinyinSearchDataState.Downloading, total, source.ExpectedLength);
            }

            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            output.Flush(true);
            var digest = Convert.ToHexString(hash.GetHashAndReset());
            if (total != source.ExpectedLength ||
                !string.Equals(digest, source.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The Unicode source download failed its pinned integrity check.");

            if (RitsuSearchSettingsStore.GetKeepPinyinSourceArchive())
            {
                PublishTemporaryFile(temporaryPath, cachedPath);
                return cachedPath;
            }

            return temporaryPath;
        }

        private static async Task ValidateSourceArchiveAsync(string path, CancellationToken cancellationToken)
        {
            var source = PinyinSearchDataSource.Current;
            var file = new FileInfo(path);
            if (!file.Exists || file.Length != source.ExpectedLength)
                throw new InvalidDataException("The cached Unicode source has an invalid size.");
            await using var stream = new FileStream(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var digest = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(Convert.ToHexString(digest), source.ExpectedSha256,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The cached Unicode source failed its pinned integrity check.");
        }

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("STS2-RitsuLib", Const.Version));
            return client;
        }

        private static string GetCacheDirectory()
        {
            return RitsuLibDataPaths.SharedCacheDirectory;
        }

        private static string GetCachePath(string fileName)
        {
            return Path.Combine(GetCacheDirectory(), fileName);
        }

        private static string GetTemporaryPath(string fileName)
        {
            return Path.Combine(RitsuLibDataPaths.TemporaryDirectory, fileName);
        }

        private static void SetReadyStatus()
        {
            var compiledPath = GetCachePath(CompiledFileName);
            var sourcePath = GetCachePath(SourceArchiveFileName);
            var cacheBytes = GetFileLength(compiledPath) + GetFileLength(sourcePath);
            SetStatus(
                PinyinSearchDataState.Ready,
                cacheBytes: cacheBytes,
                sourceArchiveCached: File.Exists(sourcePath));
        }

        private static void SetNotInstalledStatus()
        {
            var sourcePath = GetCachePath(SourceArchiveFileName);
            SetStatus(
                PinyinSearchDataState.NotInstalled,
                cacheBytes: GetFileLength(sourcePath),
                sourceArchiveCached: File.Exists(sourcePath));
        }

        private static void SetFailure(Exception exception)
        {
            var sourcePath = GetCachePath(SourceArchiveFileName);
            SetStatus(
                PinyinSearchDataState.Failed,
                cacheBytes: GetFileLength(GetCachePath(CompiledFileName)) + GetFileLength(sourcePath),
                sourceArchiveCached: File.Exists(sourcePath),
                error: exception.Message);
        }

        private static void SetStatus(
            PinyinSearchDataState state,
            long bytesReceived = 0,
            long totalBytes = 0,
            long cacheBytes = 0,
            bool sourceArchiveCached = false,
            string? error = null)
        {
            lock (StateLock)
            {
                _status = new(
                    state,
                    PinyinSearchDataSource.Current.UnicodeVersion,
                    bytesReceived,
                    totalBytes,
                    cacheBytes,
                    sourceArchiveCached,
                    error);
            }
        }

        private static long GetFileLength(string path)
        {
            return File.Exists(path) ? new FileInfo(path).Length : 0;
        }

        private static void EnsureCachedDataStillInstalled(bool force)
        {
            var data = Volatile.Read(ref _data);
            if (data == null)
                return;

            var now = System.Environment.TickCount64;
            var nextCheck = Volatile.Read(ref _nextCachePresenceCheck);
            if (!force && now < nextCheck)
                return;
            if (!force &&
                Interlocked.CompareExchange(
                    ref _nextCachePresenceCheck,
                    now + CachePresenceCheckIntervalMilliseconds,
                    nextCheck) != nextCheck)
                return;
            if (force)
                Volatile.Write(ref _nextCachePresenceCheck, now + CachePresenceCheckIntervalMilliseconds);

            if (File.Exists(GetCachePath(CompiledFileName)) ||
                !ReferenceEquals(Interlocked.CompareExchange(ref _data, null, data), data))
                return;

            SetNotInstalledStatus();
            DataChanged?.Invoke();
            RitsuLibFramework.Logger.Warn(
                "[Search/Pinyin] Generated cache disappeared; the provider returned to the uninitialized state.");
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static void PublishTemporaryFile(string temporaryPath, string destinationPath)
        {
            try
            {
                File.Copy(temporaryPath, destinationPath, true);
            }
            catch
            {
                DeleteIfExists(destinationPath);
                throw;
            }

            DeleteIfExists(temporaryPath);
        }
    }

    internal static class PinyinSearchOperationRunner
    {
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(150);

        internal static async Task RunAsync(
            Func<Task> operation,
            Action? onSuccess = null,
            Action? onStatusUpdated = null)
        {
            ArgumentNullException.ThrowIfNull(operation);
            var progressToast = new ProgressToast();
            progressToast.Start();

            try
            {
                var task = operation() ??
                           throw new InvalidOperationException("The pinyin data operation returned no task.");
                while (!task.IsCompleted)
                {
                    progressToast.Report(PinyinSearchDataManager.GetStatus());
                    onStatusUpdated?.Invoke();
                    await Task.WhenAny(task, Task.Delay(RefreshInterval));
                }

                await task;
                onSuccess?.Invoke();
                onStatusUpdated?.Invoke();
                progressToast.CompleteSuccess();
            }
            catch (Exception ex)
            {
                progressToast.CompleteFailure(ex);
                throw;
            }
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024d:F1} KiB";
            return $"{bytes / (1024d * 1024d):F1} MiB";
        }

        private static void PostToMainLoop(Action action)
        {
            if (Engine.GetMainLoop() is SceneTree)
            {
                Callable.From(action).CallDeferred();
                return;
            }

            action();
        }

        private sealed class ProgressToast
        {
            private readonly Lock _syncRoot = new();
            private bool _completed;
            private RitsuToastHandle? _handle;
            private PinyinSearchDataStatus? _latestStatus;
            private bool _updateQueued;

            internal void Start()
            {
                PostToMainLoop(() =>
                {
                    lock (_syncRoot)
                    {
                        if (_completed)
                            return;

                        _handle = RitsuToastService.ShowTracked(BuildProgressRequest(
                            PinyinSearchDataManager.GetStatus()));
                        if (_latestStatus is { } latestStatus)
                            UpdateNow(latestStatus);
                    }
                });
            }

            internal void Report(PinyinSearchDataStatus status)
            {
                lock (_syncRoot)
                {
                    if (_completed)
                        return;

                    _latestStatus = status;
                    if (_updateQueued)
                        return;
                    _updateQueued = true;
                }

                PostToMainLoop(() =>
                {
                    lock (_syncRoot)
                    {
                        _updateQueued = false;
                        if (_completed || _latestStatus is not { } latestStatus)
                            return;

                        if (_handle == null)
                            _handle = RitsuToastService.ShowTracked(BuildProgressRequest(latestStatus));
                        else
                            UpdateNow(latestStatus);
                    }
                });
            }

            internal void CompleteSuccess()
            {
                Complete(new RitsuToastRequest(
                        L("ritsulib.searchExtensions.operation.success", "Pinyin search is ready."),
                        L("ritsulib.searchExtensions.pinyin.toast.title", "Mandarin pinyin"))
                    .WithProgress(1f)
                    .Persistent(false));
            }

            internal void CompleteFailure(Exception exception)
            {
                var body = string.Format(
                    L("ritsulib.searchExtensions.operation.failed", "Pinyin search could not be prepared: {0}"),
                    exception.Message);
                Complete(RitsuToastRequest.Warning(
                        body,
                        L("ritsulib.searchExtensions.pinyin.toast.title", "Mandarin pinyin"))
                    .WithProgress(null)
                    .Persistent(false));
            }

            private void Complete(RitsuToastRequest request)
            {
                PostToMainLoop(() =>
                {
                    lock (_syncRoot)
                    {
                        _completed = true;
                        if (_handle?.Update(request) == true)
                            return;

                        RitsuToastService.Show(request);
                    }
                });
            }

            private void UpdateNow(PinyinSearchDataStatus status)
            {
                _handle?.Update(BuildProgressRequest(status), false);
            }

            private static RitsuToastRequest BuildProgressRequest(PinyinSearchDataStatus status)
            {
                var body = status.State switch
                {
                    PinyinSearchDataState.Downloading => string.Format(
                        L("ritsulib.searchExtensions.pinyin.toast.downloading",
                            "Downloading: {1} / {2}"),
                        status.UnicodeVersion,
                        FormatBytes(status.BytesReceived),
                        FormatBytes(status.TotalBytes)),
                    PinyinSearchDataState.Generating =>
                        L("ritsulib.searchExtensions.pinyin.toast.generating",
                            "Finishing setup..."),
                    PinyinSearchDataState.Loading =>
                        L("ritsulib.searchExtensions.pinyin.toast.loading", "Loading pinyin search..."),
                    _ => L("ritsulib.searchExtensions.pinyin.toast.preparing", "Preparing pinyin search..."),
                };

                var progress = status.State switch
                {
                    PinyinSearchDataState.Downloading when status.TotalBytes > 0 =>
                        0.05f + 0.8f * Mathf.Clamp((float)status.BytesReceived / status.TotalBytes, 0f, 1f),
                    PinyinSearchDataState.Generating => 0.9f,
                    PinyinSearchDataState.Ready => 1f,
                    _ => 0f,
                };
                return new RitsuToastRequest(
                        body,
                        L("ritsulib.searchExtensions.pinyin.toast.title", "Mandarin pinyin"))
                    .Persistent()
                    .WithDismissOnClick(false)
                    .WithProgress(progress);
            }
        }
    }
}
