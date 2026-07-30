using Godot;
using MegaCrit.Sts2.Core.Nodes;
using STS2RitsuLib.Data;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Toast
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the global entry point for displaying and managing RitsuLib toast notifications.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供显示和管理 RitsuLib toast 通知的全局入口。
    ///     </para>
    /// </summary>
    public static class RitsuToastService
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Queue<PendingToast> PendingBeforeHost = [];
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
                    lock (SyncRoot)
                    {
                        EnsureHostAttached(evt.Game);
                    }
                }, false);
                RitsuShellThemeRuntime.ThemeChanged += HandleThemeChanged;
                EnsureHostAttached(NGame.Instance);
            }
        }

        internal static void ApplySettings(RitsuToastSettings settings)
        {
            lock (SyncRoot)
            {
                _settings = settings;
                if (!settings.Enabled)
                    PendingBeforeHost.Clear();
                EnsureHostAttached(NGame.Instance);
                _host?.ApplySettings(settings);
            }
        }

        internal static void RefreshSettingsFromStore()
        {
            ApplySettings(RitsuLibSettingsStore.GetToastSettings());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Enqueues a request for display. When toast notifications are enabled but the game host is not ready,
        ///         the request remains queued until the host is attached.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将请求加入显示队列。若 toast 通知已启用但游戏宿主尚未就绪，请求会保留至宿主挂载完成。
        ///     </para>
        /// </summary>
        /// <param name="request">
        ///     <para xml:lang="en">The request to display.</para>
        ///     <para xml:lang="zh-CN">要显示的请求。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="request" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="request" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static void Show(RitsuToastRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            Initialize();
            lock (SyncRoot)
            {
                EnsureHostAttached(NGame.Instance);
                EnqueueOrStore(Guid.NewGuid(), request);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Enqueues a request and returns a handle for managing the toast.</para>
        ///     <para xml:lang="zh-CN">将请求加入队列，并返回用于管理该 toast 的句柄。</para>
        /// </summary>
        /// <param name="request">
        ///     <para xml:lang="en">The request to display.</para>
        ///     <para xml:lang="zh-CN">要显示的请求。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         A handle associated with the request. If toast notifications are disabled, the returned handle is
        ///         not alive.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         与该请求关联的句柄。若 toast 通知已禁用，返回的句柄不会处于存活状态。
        ///     </para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="request" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="request" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static RitsuToastHandle ShowTracked(RitsuToastRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            Initialize();
            var handle = new RitsuToastHandle(Guid.NewGuid());
            lock (SyncRoot)
            {
                EnsureHostAttached(NGame.Instance);
                EnqueueOrStore(handle.Id, request);
            }

            return handle;
        }

        /// <summary>
        ///     <para xml:lang="en">Enqueues an informational toast with default request options.</para>
        ///     <para xml:lang="zh-CN">使用默认请求选项将信息 toast 加入队列。</para>
        /// </summary>
        /// <param name="body">
        ///     <para xml:lang="en">The required body text.</para>
        ///     <para xml:lang="zh-CN">必需的正文文本。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The optional title.</para>
        ///     <para xml:lang="zh-CN">可选标题。</para>
        /// </param>
        /// <param name="onClick">
        ///     <para xml:lang="en">The optional callback invoked when the toast is clicked.</para>
        ///     <para xml:lang="zh-CN">点击 toast 时调用的可选回调。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="body" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="body" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static void ShowInfo(string body, string? title = null, Action? onClick = null)
        {
            Show(new(body, title, null, RitsuToastLevel.Info, null, onClick));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Enqueues an informational toast with default request options and returns a handle for managing it.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用默认请求选项将信息 toast 加入队列，并返回用于管理它的句柄。
        ///     </para>
        /// </summary>
        /// <param name="body">
        ///     <para xml:lang="en">The required body text.</para>
        ///     <para xml:lang="zh-CN">必需的正文文本。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The optional title.</para>
        ///     <para xml:lang="zh-CN">可选标题。</para>
        /// </param>
        /// <param name="onClick">
        ///     <para xml:lang="en">The optional callback invoked when the toast is clicked.</para>
        ///     <para xml:lang="zh-CN">点击 toast 时调用的可选回调。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The handle associated with the request.</para>
        ///     <para xml:lang="zh-CN">与该请求关联的句柄。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="body" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="body" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static RitsuToastHandle ShowInfoTracked(string body, string? title = null, Action? onClick = null)
        {
            return ShowTracked(new(body, title, null, RitsuToastLevel.Info, null, onClick));
        }

        /// <summary>
        ///     <para xml:lang="en">Enqueues a warning toast with default request options.</para>
        ///     <para xml:lang="zh-CN">使用默认请求选项将警告 toast 加入队列。</para>
        /// </summary>
        /// <param name="body">
        ///     <para xml:lang="en">The required body text.</para>
        ///     <para xml:lang="zh-CN">必需的正文文本。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The optional title.</para>
        ///     <para xml:lang="zh-CN">可选标题。</para>
        /// </param>
        /// <param name="onClick">
        ///     <para xml:lang="en">The optional callback invoked when the toast is clicked.</para>
        ///     <para xml:lang="zh-CN">点击 toast 时调用的可选回调。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="body" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="body" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static void ShowWarning(string body, string? title = null, Action? onClick = null)
        {
            Show(new(body, title, null, RitsuToastLevel.Warning, null, onClick));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Enqueues a warning toast with default request options and returns a handle for managing it.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用默认请求选项将警告 toast 加入队列，并返回用于管理它的句柄。
        ///     </para>
        /// </summary>
        /// <param name="body">
        ///     <para xml:lang="en">The required body text.</para>
        ///     <para xml:lang="zh-CN">必需的正文文本。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The optional title.</para>
        ///     <para xml:lang="zh-CN">可选标题。</para>
        /// </param>
        /// <param name="onClick">
        ///     <para xml:lang="en">The optional callback invoked when the toast is clicked.</para>
        ///     <para xml:lang="zh-CN">点击 toast 时调用的可选回调。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The handle associated with the request.</para>
        ///     <para xml:lang="zh-CN">与该请求关联的句柄。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="body" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="body" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static RitsuToastHandle ShowWarningTracked(string body, string? title = null,
            Action? onClick = null)
        {
            return ShowTracked(new(body, title, null, RitsuToastLevel.Warning, null, onClick));
        }

        /// <summary>
        ///     <para xml:lang="en">Enqueues an error toast with default request options.</para>
        ///     <para xml:lang="zh-CN">使用默认请求选项将错误 toast 加入队列。</para>
        /// </summary>
        /// <param name="body">
        ///     <para xml:lang="en">The required body text.</para>
        ///     <para xml:lang="zh-CN">必需的正文文本。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The optional title.</para>
        ///     <para xml:lang="zh-CN">可选标题。</para>
        /// </param>
        /// <param name="onClick">
        ///     <para xml:lang="en">The optional callback invoked when the toast is clicked.</para>
        ///     <para xml:lang="zh-CN">点击 toast 时调用的可选回调。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="body" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="body" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static void ShowError(string body, string? title = null, Action? onClick = null)
        {
            Show(new(body, title, null, RitsuToastLevel.Error, null, onClick));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Enqueues an error toast with default request options and returns a handle for managing it.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用默认请求选项将错误 toast 加入队列，并返回用于管理它的句柄。
        ///     </para>
        /// </summary>
        /// <param name="body">
        ///     <para xml:lang="en">The required body text.</para>
        ///     <para xml:lang="zh-CN">必需的正文文本。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The optional title.</para>
        ///     <para xml:lang="zh-CN">可选标题。</para>
        /// </param>
        /// <param name="onClick">
        ///     <para xml:lang="en">The optional callback invoked when the toast is clicked.</para>
        ///     <para xml:lang="zh-CN">点击 toast 时调用的可选回调。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The handle associated with the request.</para>
        ///     <para xml:lang="zh-CN">与该请求关联的句柄。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="body" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="body" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static RitsuToastHandle ShowErrorTracked(string body, string? title = null, Action? onClick = null)
        {
            return ShowTracked(new(body, title, null, RitsuToastLevel.Error, null, onClick));
        }

        /// <summary>
        ///     <para xml:lang="en">Determines whether a tracked toast is queued or active and has not begun closing.</para>
        ///     <para xml:lang="zh-CN">确定可跟踪 toast 是否仍在队列中或处于活动状态，且尚未开始关闭。</para>
        /// </summary>
        /// <param name="handle">
        ///     <para xml:lang="en">The handle to query.</para>
        ///     <para xml:lang="zh-CN">要查询的句柄。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the toast is still alive; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">若该 toast 仍然存活则为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="handle" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="handle" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static bool IsAlive(RitsuToastHandle handle)
        {
            ArgumentNullException.ThrowIfNull(handle);
            lock (SyncRoot)
            {
                return FindPending(handle.Id) != null || _host?.IsAlive(handle.Id) == true;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Removes a queued toast or requests that an active toast close.</para>
        ///     <para xml:lang="zh-CN">移除队列中的 toast，或请求关闭活动中的 toast。</para>
        /// </summary>
        /// <param name="handle">
        ///     <para xml:lang="en">The handle of the toast to close.</para>
        ///     <para xml:lang="zh-CN">要关闭的 toast 句柄。</para>
        /// </param>
        /// <param name="immediate">
        ///     <para xml:lang="en">Whether an active toast should close without playing its exit animation.</para>
        ///     <para xml:lang="zh-CN">活动中的 toast 是否应跳过退出动画并立即关闭。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the service found the toast; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">若服务找到了该 toast 则为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="handle" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="handle" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static bool Close(RitsuToastHandle handle, bool immediate = false)
        {
            ArgumentNullException.ThrowIfNull(handle);
            lock (SyncRoot)
            {
                if (RemovePending(handle.Id))
                    return true;
                return _host?.Close(handle.Id, immediate) == true;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Removes all queued toasts and requests that all active toasts close.</para>
        ///     <para xml:lang="zh-CN">移除所有队列中的 toast，并请求关闭所有活动中的 toast。</para>
        /// </summary>
        /// <param name="immediate">
        ///     <para xml:lang="en">Whether active toasts should close without playing their exit animations.</para>
        ///     <para xml:lang="zh-CN">活动中的 toast 是否应跳过退出动画并立即关闭。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The number of queued, active, or already-closing toasts found by the service.</para>
        ///     <para xml:lang="zh-CN">服务找到的队列中、活动中或已在关闭的 toast 数量。</para>
        /// </returns>
        public static int CloseAll(bool immediate = false)
        {
            lock (SyncRoot)
            {
                var closed = PendingBeforeHost.Count;
                PendingBeforeHost.Clear();
                return closed + (_host?.CloseAll(immediate) ?? 0);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Replaces the request associated with a tracked toast.</para>
        ///     <para xml:lang="zh-CN">替换与可跟踪 toast 关联的请求。</para>
        /// </summary>
        /// <param name="handle">
        ///     <para xml:lang="en">The handle of the toast to update.</para>
        ///     <para xml:lang="zh-CN">要更新的 toast 句柄。</para>
        /// </param>
        /// <param name="request">
        ///     <para xml:lang="en">The replacement request.</para>
        ///     <para xml:lang="zh-CN">替换后的请求。</para>
        /// </param>
        /// <param name="resetDuration">
        ///     <para xml:lang="en">Whether to restart the timer if the toast is already active.</para>
        ///     <para xml:lang="zh-CN">若 toast 已处于活动状态，是否重新开始计时。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the toast was updated; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">若已更新该 toast 则为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="handle" /> or <paramref name="request" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="handle" /> 或 <paramref name="request" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static bool Update(RitsuToastHandle handle, RitsuToastRequest request, bool resetDuration = true)
        {
            ArgumentNullException.ThrowIfNull(handle);
            ArgumentNullException.ThrowIfNull(request);
            lock (SyncRoot)
            {
                var pending = FindPending(handle.Id);
                if (pending != null)
                {
                    pending.Request = request;
                    return true;
                }

                return _host?.Update(handle.Id, request, resetDuration) == true;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Updates the body while preserving the other request values.</para>
        ///     <para xml:lang="zh-CN">更新正文并保留请求中的其他值。</para>
        /// </summary>
        /// <param name="handle">
        ///     <para xml:lang="en">The handle of the toast to update.</para>
        ///     <para xml:lang="zh-CN">要更新的 toast 句柄。</para>
        /// </param>
        /// <param name="body">
        ///     <para xml:lang="en">The replacement body text.</para>
        ///     <para xml:lang="zh-CN">替换后的正文文本。</para>
        /// </param>
        /// <param name="resetDuration">
        ///     <para xml:lang="en">Whether to restart the timer if the toast is already active.</para>
        ///     <para xml:lang="zh-CN">若 toast 已处于活动状态，是否重新开始计时。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the toast was updated; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">若已更新该 toast 则为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="handle" /> or <paramref name="body" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="handle" /> 或 <paramref name="body" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static bool UpdateBody(RitsuToastHandle handle, string body, bool resetDuration = true)
        {
            ArgumentNullException.ThrowIfNull(handle);
            ArgumentNullException.ThrowIfNull(body);
            lock (SyncRoot)
            {
                var pending = FindPending(handle.Id);
                if (pending != null)
                {
                    pending.Request = pending.Request.WithBody(body);
                    return true;
                }

                return _host?.Update(handle.Id, request => request.WithBody(body), resetDuration) == true;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Updates the body and title while preserving the other request values.</para>
        ///     <para xml:lang="zh-CN">更新正文和标题，并保留请求中的其他值。</para>
        /// </summary>
        /// <param name="handle">
        ///     <para xml:lang="en">The handle of the toast to update.</para>
        ///     <para xml:lang="zh-CN">要更新的 toast 句柄。</para>
        /// </param>
        /// <param name="body">
        ///     <para xml:lang="en">The replacement body text.</para>
        ///     <para xml:lang="zh-CN">替换后的正文文本。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The replacement title, or <see langword="null" /> to hide it.</para>
        ///     <para xml:lang="zh-CN">替换后的标题；传入 <see langword="null" /> 可隐藏标题。</para>
        /// </param>
        /// <param name="resetDuration">
        ///     <para xml:lang="en">Whether to restart the timer if the toast is already active.</para>
        ///     <para xml:lang="zh-CN">若 toast 已处于活动状态，是否重新开始计时。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the toast was updated; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">若已更新该 toast 则为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="handle" /> or <paramref name="body" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="handle" /> 或 <paramref name="body" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static bool UpdateText(RitsuToastHandle handle, string body, string? title,
            bool resetDuration = true)
        {
            ArgumentNullException.ThrowIfNull(handle);
            ArgumentNullException.ThrowIfNull(body);
            lock (SyncRoot)
            {
                var pending = FindPending(handle.Id);
                if (pending != null)
                {
                    pending.Request = pending.Request.WithText(body, title);
                    return true;
                }

                return _host?.Update(handle.Id, request => request.WithText(body, title), resetDuration) == true;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Updates the title while preserving the other request values.</para>
        ///     <para xml:lang="zh-CN">更新标题并保留请求中的其他值。</para>
        /// </summary>
        /// <param name="handle">
        ///     <para xml:lang="en">The handle of the toast to update.</para>
        ///     <para xml:lang="zh-CN">要更新的 toast 句柄。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The replacement title, or <see langword="null" /> to hide it.</para>
        ///     <para xml:lang="zh-CN">替换后的标题；传入 <see langword="null" /> 可隐藏标题。</para>
        /// </param>
        /// <param name="resetDuration">
        ///     <para xml:lang="en">Whether to restart the timer if the toast is already active.</para>
        ///     <para xml:lang="zh-CN">若 toast 已处于活动状态，是否重新开始计时。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the toast was updated; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">若已更新该 toast 则为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="handle" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="handle" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static bool UpdateTitle(RitsuToastHandle handle, string? title, bool resetDuration = false)
        {
            ArgumentNullException.ThrowIfNull(handle);
            lock (SyncRoot)
            {
                var pending = FindPending(handle.Id);
                if (pending != null)
                {
                    pending.Request = pending.Request.WithTitle(title);
                    return true;
                }

                return _host?.Update(handle.Id, request => request.WithTitle(title), resetDuration) == true;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Restarts a tracked toast timer and optionally replaces its per-toast duration.</para>
        ///     <para xml:lang="zh-CN">重新开始可跟踪 toast 的计时，并可选择替换其单条持续时间。</para>
        /// </summary>
        /// <param name="handle">
        ///     <para xml:lang="en">The handle of the toast to reset.</para>
        ///     <para xml:lang="zh-CN">要重新计时的 toast 句柄。</para>
        /// </param>
        /// <param name="durationSeconds">
        ///     <para xml:lang="en">
        ///         The new duration in seconds, or <see langword="null" /> to reuse the request or global duration.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         新的持续时间（秒）；传入 <see langword="null" /> 可继续使用请求值或全局值。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the service found the toast; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">若服务找到了该 toast 则为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="handle" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="handle" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static bool ResetDuration(RitsuToastHandle handle, double? durationSeconds = null)
        {
            ArgumentNullException.ThrowIfNull(handle);
            lock (SyncRoot)
            {
                var pending = FindPending(handle.Id);
                if (pending != null)
                {
                    if (durationSeconds.HasValue)
                        pending.Request = pending.Request.WithDuration(durationSeconds);
                    return true;
                }

                return _host?.ResetDuration(handle.Id, durationSeconds) == true;
            }
        }

        private static void EnsureHostAttached(Node? gameNode)
        {
            if (_host != null && GodotObject.IsInstanceValid(_host))
                return;
            if (gameNode == null || !GodotObject.IsInstanceValid(gameNode))
                return;
            _host = new();
            gameNode.AddChild(_host);
            _host.ApplySettings(_settings);
            while (PendingBeforeHost.Count > 0)
            {
                var pending = PendingBeforeHost.Dequeue();
                _host.Enqueue(pending.Id, pending.Request);
            }
        }

        private static void HandleThemeChanged()
        {
            lock (SyncRoot)
            {
                _host?.RefreshTheme();
            }
        }

        private static void EnqueueOrStore(Guid id, RitsuToastRequest request)
        {
            if (!_settings.Enabled)
                return;

            if (_host != null && GodotObject.IsInstanceValid(_host))
            {
                _host.Enqueue(id, request);
                return;
            }

            PendingBeforeHost.Enqueue(new(id, request));
        }

        private static PendingToast? FindPending(Guid id)
        {
            return PendingBeforeHost.FirstOrDefault(pending => pending.Id == id);
        }

        private static bool RemovePending(Guid id)
        {
            var removed = false;
            var count = PendingBeforeHost.Count;
            for (var i = 0; i < count; i++)
            {
                var pending = PendingBeforeHost.Dequeue();
                if (pending.Id == id)
                {
                    removed = true;
                    continue;
                }

                PendingBeforeHost.Enqueue(pending);
            }

            return removed;
        }

        private sealed class PendingToast(Guid id, RitsuToastRequest request)
        {
            public Guid Id { get; } = id;
            public RitsuToastRequest Request { get; set; } = request;
        }
    }
}
