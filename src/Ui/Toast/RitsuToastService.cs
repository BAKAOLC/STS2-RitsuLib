using Godot;
using MegaCrit.Sts2.Core.Nodes;
using STS2RitsuLib.Data;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Toast
{
    /// <summary>
    ///     Global toast entrypoint for framework and mod callers.
    ///     供框架和 mod 调用方使用的全局 toast 入口。
    /// </summary>
    public static class RitsuToastService
    {
        private static readonly Lock SyncRoot = new();
        private static RitsuToastHost? _host;
        private static IDisposable? _lifecycleSubscription;
        private static bool _initialized;
        private static RitsuToastSettings _settings = RitsuToastSettings.Default;

        internal static void Initialize()
        {
            lock (SyncRoot)
            {
                if (_initialized)
                    return;
                _initialized = true;
                _settings = RitsuLibSettingsStore.GetToastSettings();
                _lifecycleSubscription ??= RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(evt =>
                {
                    EnsureHostAttached(evt.Game);
                });
                RitsuShellThemeRuntime.ThemeChanged += HandleThemeChanged;
            }
        }

        internal static void ApplySettings(RitsuToastSettings settings)
        {
            lock (SyncRoot)
            {
                _settings = settings;
                EnsureHostAttached(NGame.Instance);
                _host?.ApplySettings(settings);
            }
        }

        internal static void RefreshSettingsFromStore()
        {
            ApplySettings(RitsuLibSettingsStore.GetToastSettings());
        }

        /// <summary>
        ///     Enqueues a toast request for display.
        ///     将 toast 请求加入显示队列。
        /// </summary>
        public static void Show(RitsuToastRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            Initialize();
            lock (SyncRoot)
            {
                EnsureHostAttached(NGame.Instance);
                _host?.Enqueue(Guid.NewGuid(), request);
            }
        }

        /// <summary>
        ///     Enqueues a toast request and returns a handle that can update or close it later.
        ///     将 toast 请求加入显示队列，并返回可在之后更新或关闭它的句柄。
        /// </summary>
        public static RitsuToastHandle ShowTracked(RitsuToastRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            Initialize();
            var handle = new RitsuToastHandle(Guid.NewGuid());
            lock (SyncRoot)
            {
                EnsureHostAttached(NGame.Instance);
                _host?.Enqueue(handle.Id, request);
            }

            return handle;
        }

        /// <summary>
        ///     Enqueues a default informational toast.
        ///     将默认信息 toast 加入队列。
        /// </summary>
        public static void ShowInfo(string body, string? title = null, Action? onClick = null)
        {
            Show(new(body, title, null, RitsuToastLevel.Info, null, onClick));
        }

        /// <summary>
        ///     Enqueues a default informational toast and returns a handle.
        ///     将默认信息 toast 加入队列并返回句柄。
        /// </summary>
        public static RitsuToastHandle ShowInfoTracked(string body, string? title = null, Action? onClick = null)
        {
            return ShowTracked(new(body, title, null, RitsuToastLevel.Info, null, onClick));
        }

        /// <summary>
        ///     Enqueues a default warning toast.
        ///     将默认警告 toast 加入队列。
        /// </summary>
        public static void ShowWarning(string body, string? title = null, Action? onClick = null)
        {
            Show(new(body, title, null, RitsuToastLevel.Warning, null, onClick));
        }

        /// <summary>
        ///     Enqueues a default warning toast and returns a handle.
        ///     将默认警告 toast 加入队列并返回句柄。
        /// </summary>
        public static RitsuToastHandle ShowWarningTracked(string body, string? title = null,
            Action? onClick = null)
        {
            return ShowTracked(new(body, title, null, RitsuToastLevel.Warning, null, onClick));
        }

        /// <summary>
        ///     Enqueues a default error toast.
        ///     将默认错误 toast 加入队列。
        /// </summary>
        public static void ShowError(string body, string? title = null, Action? onClick = null)
        {
            Show(new(body, title, null, RitsuToastLevel.Error, null, onClick));
        }

        /// <summary>
        ///     Enqueues a default error toast and returns a handle.
        ///     将默认错误 toast 加入队列并返回句柄。
        /// </summary>
        public static RitsuToastHandle ShowErrorTracked(string body, string? title = null, Action? onClick = null)
        {
            return ShowTracked(new(body, title, null, RitsuToastLevel.Error, null, onClick));
        }

        /// <summary>
        ///     Returns whether a tracked toast is still pending or visible.
        ///     返回可跟踪 toast 是否仍在等待显示或已经可见。
        /// </summary>
        public static bool IsAlive(RitsuToastHandle handle)
        {
            ArgumentNullException.ThrowIfNull(handle);
            lock (SyncRoot)
            {
                return _host?.IsAlive(handle.Id) == true;
            }
        }

        /// <summary>
        ///     Closes a tracked toast if it is still pending or visible.
        ///     如果可跟踪 toast 仍在等待显示或已经可见，则关闭它。
        /// </summary>
        public static bool Close(RitsuToastHandle handle, bool immediate = false)
        {
            ArgumentNullException.ThrowIfNull(handle);
            lock (SyncRoot)
            {
                return _host?.Close(handle.Id, immediate) == true;
            }
        }

        /// <summary>
        ///     Closes all pending and visible toasts.
        ///     关闭所有等待显示和已经可见的 toast。
        /// </summary>
        public static int CloseAll(bool immediate = false)
        {
            lock (SyncRoot)
            {
                return _host?.CloseAll(immediate) ?? 0;
            }
        }

        /// <summary>
        ///     Replaces a tracked toast request while preserving the same handle.
        ///     在保留同一句柄的同时替换可跟踪 toast 请求。
        /// </summary>
        public static bool Update(RitsuToastHandle handle, RitsuToastRequest request, bool resetDuration = true)
        {
            ArgumentNullException.ThrowIfNull(handle);
            ArgumentNullException.ThrowIfNull(request);
            lock (SyncRoot)
            {
                return _host?.Update(handle.Id, request, resetDuration) == true;
            }
        }

        /// <summary>
        ///     Updates only the body text for a tracked toast.
        ///     仅更新可跟踪 toast 的正文文本。
        /// </summary>
        public static bool UpdateBody(RitsuToastHandle handle, string body, bool resetDuration = true)
        {
            ArgumentNullException.ThrowIfNull(handle);
            lock (SyncRoot)
            {
                return _host?.Update(handle.Id, request => request.WithBody(body), resetDuration) == true;
            }
        }

        /// <summary>
        ///     Updates the body and title text for a tracked toast.
        ///     更新可跟踪 toast 的正文和标题文本。
        /// </summary>
        public static bool UpdateText(RitsuToastHandle handle, string body, string? title,
            bool resetDuration = true)
        {
            ArgumentNullException.ThrowIfNull(handle);
            lock (SyncRoot)
            {
                return _host?.Update(handle.Id, request => request.WithText(body, title), resetDuration) == true;
            }
        }

        /// <summary>
        ///     Updates the title for a tracked toast while preserving the body text.
        ///     更新可跟踪 toast 的标题并保留正文文本。
        /// </summary>
        public static bool UpdateTitle(RitsuToastHandle handle, string? title, bool resetDuration = false)
        {
            ArgumentNullException.ThrowIfNull(handle);
            lock (SyncRoot)
            {
                return _host?.Update(handle.Id, request => request.WithTitle(title), resetDuration) == true;
            }
        }

        /// <summary>
        ///     Restarts a tracked toast's remaining display time, optionally overriding its duration.
        ///     重新开始可跟踪 toast 的剩余显示时间，并可选覆盖其持续时间。
        /// </summary>
        public static bool ResetDuration(RitsuToastHandle handle, double? durationSeconds = null)
        {
            ArgumentNullException.ThrowIfNull(handle);
            lock (SyncRoot)
            {
                return _host?.ResetDuration(handle.Id, durationSeconds) == true;
            }
        }

        private static void EnsureHostAttached(Node? gameNode)
        {
            if (_host != null && GodotObject.IsInstanceValid(_host))
                return;
            if (gameNode == null)
                return;
            _host = new();
            gameNode.AddChild(_host);
            _host.ApplySettings(_settings);
        }

        private static void HandleThemeChanged()
        {
            lock (SyncRoot)
            {
                _host?.RefreshTheme();
            }
        }
    }
}
