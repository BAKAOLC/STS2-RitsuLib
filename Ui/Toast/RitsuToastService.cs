using Godot;
using MegaCrit.Sts2.Core.Nodes;
using STS2RitsuLib.Data;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Toast
{
    /// <summary>
    ///     Global toast entrypoint for framework and mod callers.
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
        /// </summary>
        public static void Show(RitsuToastRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            Initialize();
            lock (SyncRoot)
            {
                EnsureHostAttached(NGame.Instance);
                _host?.Enqueue(request);
            }
        }

        /// <summary>
        ///     Enqueues a default informational toast.
        /// </summary>
        public static void ShowInfo(string body, string? title = null, Action? onClick = null)
        {
            Show(new(body, title, null, RitsuToastLevel.Info, null, onClick));
        }

        /// <summary>
        ///     Enqueues a default warning toast.
        /// </summary>
        public static void ShowWarning(string body, string? title = null, Action? onClick = null)
        {
            Show(new(body, title, null, RitsuToastLevel.Warning, null, onClick));
        }

        /// <summary>
        ///     Enqueues a default error toast.
        /// </summary>
        public static void ShowError(string body, string? title = null, Action? onClick = null)
        {
            Show(new(body, title, null, RitsuToastLevel.Error, null, onClick));
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
