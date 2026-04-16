using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace STS2RitsuLib.RuntimeInput
{
    /// <summary>
    ///     Provides a settings-independent runtime hotkey API that parses persisted binding strings and
    ///     registers input callbacks against a shared router node.
    /// </summary>
    public static class RuntimeHotkeyService
    {
        private static readonly Lock SyncRoot = new();
        private static RuntimeHotkeyRouterNode? _router;
        private static IDisposable? _lifecycleSubscription;

        /// <summary>
        ///     Ensures the shared router will be attached when the game root becomes ready.
        /// </summary>
        public static void Initialize()
        {
            lock (SyncRoot)
            {
                _lifecycleSubscription ??= RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(evt =>
                {
                    EnsureRouterAttached(evt.Game);
                });
            }
        }

        /// <summary>
        ///     Returns read-only snapshots for all currently registered runtime hotkeys.
        /// </summary>
        public static IReadOnlyList<RuntimeHotkeyRegistrationInfo> GetRegisteredHotkeys()
        {
            lock (SyncRoot)
            {
                return _router?.GetRegistrationInfos() ?? Array.Empty<RuntimeHotkeyRegistrationInfo>();
            }
        }

        /// <summary>
        ///     Tries to return the currently registered hotkey snapshot for a stable registration id.
        /// </summary>
        /// <param name="id">Stable registration id to locate.</param>
        /// <param name="registrationInfo">Registration snapshot when a matching id exists.</param>
        /// <returns><c>true</c> when a matching registration was found.</returns>
        public static bool TryGetRegisteredHotkey(string id, out RuntimeHotkeyRegistrationInfo registrationInfo)
        {
            lock (SyncRoot)
            {
                var info = _router?.GetRegistrationInfoById(id);
                if (info != null)
                {
                    registrationInfo = info;
                    return true;
                }

                registrationInfo = null!;
                return false;
            }
        }

        /// <summary>
        ///     Attempts to normalize a persisted binding string into the runtime hotkey canonical format.
        /// </summary>
        /// <param name="bindingText">Binding text to normalize.</param>
        /// <param name="normalizedBinding">Canonical binding string when parsing succeeds.</param>
        /// <returns><c>true</c> when the binding string was parsed successfully.</returns>
        public static bool TryNormalizeBinding(string? bindingText, out string normalizedBinding)
        {
            return RuntimeHotkeyParser.TryParse(bindingText, out _, out normalizedBinding);
        }

        /// <summary>
        ///     Returns the normalized binding string, or <paramref name="fallback" /> when parsing fails.
        /// </summary>
        /// <param name="bindingText">Binding text to normalize.</param>
        /// <param name="fallback">Fallback value returned when parsing fails.</param>
        public static string NormalizeOrDefault(string? bindingText, string fallback)
        {
            return RuntimeHotkeyParser.NormalizeOrDefault(bindingText, fallback);
        }

        /// <summary>
        ///     Registers a runtime hotkey directly from a persisted binding string.
        /// </summary>
        /// <param name="bindingText">Persisted binding string to parse.</param>
        /// <param name="callback">Callback invoked when the hotkey matches.</param>
        /// <param name="options">Optional router behavior overrides.</param>
        /// <returns>A handle that supports explicit rebind and unregister operations.</returns>
        /// <exception cref="FormatException">Thrown when <paramref name="bindingText" /> is invalid.</exception>
        public static IRuntimeHotkeyHandle Register(string bindingText, Action callback,
            RuntimeHotkeyOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(callback);
            Initialize();
            if (!RuntimeHotkeyParser.TryParse(bindingText, out var binding, out var normalizedBinding))
                throw new FormatException($"Invalid runtime hotkey binding '{bindingText}'.");

            lock (SyncRoot)
            {
                EnsureRouterAttached(NGame.Instance);
                if (_router == null)
                    throw new InvalidOperationException("Runtime hotkey router is not available.");

                var handle = _router.Register(binding, callback, options);
                RitsuLibFramework.Logger.Info(
                    $"[RuntimeHotkey] Registered '{normalizedBinding}'{FormatDebugName(options)}");
                return handle;
            }
        }

        private static void EnsureRouterAttached(Node? gameNode)
        {
            if (_router != null && GodotObject.IsInstanceValid(_router))
                return;
            if (gameNode == null)
                return;

            _router = new() { Name = "RitsuRuntimeHotkeyRouter" };
            gameNode.AddChild(_router);
        }

        private static string FormatDebugName(RuntimeHotkeyOptions? options)
        {
            return string.IsNullOrWhiteSpace(options?.DebugName) ? string.Empty : $" for {options.DebugName}";
        }
    }
}
