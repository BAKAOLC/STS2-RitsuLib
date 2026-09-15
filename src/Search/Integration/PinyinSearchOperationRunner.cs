using Godot;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Toast;

namespace STS2RitsuLib.Search.Pinyin
{
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
            return bytes switch
            {
                < 1024 => $"{bytes} B",
                < 1024 * 1024 => $"{bytes / 1024d:F1} KiB",
                _ => $"{bytes / (1024d * 1024d):F1} MiB",
            };
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
