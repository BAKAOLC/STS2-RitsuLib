using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace STS2RitsuLib.RuntimeInput
{
    /// <summary>
    ///     Provides a settings-independent runtime hotkey API that parses persisted binding strings and
    ///     registers input callbacks against a shared router node.
    ///     提供与设置无关的运行时热键 API，用于解析持久化绑定字符串，并
    ///     针对共享路由器节点注册输入回调。
    /// </summary>
    public static class RuntimeHotkeyService
    {
        private static readonly Lock SyncRoot = new();
        private static RuntimeHotkeyRouterNode? _router;
        private static IDisposable? _lifecycleSubscription;

        /// <summary>
        ///     Ensures the shared router will be attached when the game root becomes ready.
        ///     确保游戏根节点 ready 后会附加共享路由器。
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
        ///     返回所有当前已注册运行时热键的只读快照。
        /// </summary>
        public static IReadOnlyList<RuntimeHotkeyRegistrationInfo> GetRegisteredHotkeys()
        {
            lock (SyncRoot)
            {
                return _router?.GetRegistrationInfos() ?? [];
            }
        }

        /// <summary>
        ///     Returns detailed read-only snapshots for all currently registered runtime hotkeys, including every binding.
        ///     返回所有当前已注册运行时热键的详细只读快照，包括每个绑定。
        /// </summary>
        public static IReadOnlyList<RuntimeHotkeyRegistrationDetails> GetRegisteredHotkeyDetails()
        {
            lock (SyncRoot)
            {
                return _router?.GetRegistrationDetails() ?? [];
            }
        }

        /// <summary>
        ///     Tries to return the currently registered hotkey snapshot for a stable registration id.
        ///     尝试返回稳定注册 id 对应的当前已注册热键快照。
        /// </summary>
        /// <param name="id">
        ///     Stable registration id to locate.
        ///     要定位的稳定注册 id。
        /// </param>
        /// <param name="registrationInfo">
        ///     Registration snapshot when a matching id exists.
        ///     存在匹配 id 时的注册快照。
        /// </param>
        /// <returns>
        ///     <c>true</c> when a matching registration was found.
        ///     找到匹配注册时为 <c>true</c>。
        /// </returns>
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
        ///     尝试将持久化绑定字符串规范化为运行时热键规范格式。
        /// </summary>
        /// <param name="bindingText">
        ///     Binding text to normalize.
        ///     要规范化的绑定文本。
        /// </param>
        /// <param name="normalizedBinding">
        ///     Canonical binding string when parsing succeeds.
        ///     解析成功时的规范绑定字符串。
        /// </param>
        /// <returns>
        ///     <c>true</c> when the binding string was parsed successfully.
        ///     绑定字符串成功解析时为 <c>true</c>。
        /// </returns>
        public static bool TryNormalizeBinding(string? bindingText, out string normalizedBinding)
        {
            return RuntimeHotkeyParser.TryParse(bindingText, out _, out normalizedBinding);
        }

        /// <summary>
        ///     Returns the normalized binding string, or <paramref name="fallback" /> when parsing fails.
        ///     返回规范化绑定字符串；解析失败时返回 <paramref name="fallback" />。
        /// </summary>
        /// <param name="bindingText">
        ///     Binding text to normalize.
        ///     要规范化的绑定文本。
        /// </param>
        /// <param name="fallback">
        ///     Fallback value returned when parsing fails.
        ///     解析失败时返回的回退值。
        /// </param>
        public static string NormalizeOrDefault(string? bindingText, string fallback)
        {
            return RuntimeHotkeyParser.NormalizeOrDefault(bindingText, fallback);
        }

        /// <summary>
        ///     Registers a runtime hotkey directly from a persisted binding string.
        ///     直接从持久化绑定字符串注册运行时热键。
        /// </summary>
        /// <param name="bindingText">
        ///     Persisted binding string to parse.
        ///     要解析的持久化绑定字符串。
        /// </param>
        /// <param name="callback">
        ///     Callback invoked when the hotkey matches.
        ///     热键匹配时调用的回调。
        /// </param>
        /// <param name="options">
        ///     Optional router behavior overrides.
        ///     可选路由器行为覆盖。
        /// </param>
        /// <returns>
        ///     A handle that supports explicit rebind and unregister operations.
        ///     支持显式重新绑定和注销操作的句柄。
        /// </returns>
        /// <exception cref="FormatException">Thrown when <paramref name="bindingText" /> is invalid.</exception>
        public static IRuntimeHotkeyHandle Register(string bindingText, Action callback,
            RuntimeHotkeyOptions? options = null)
        {
            return Register([bindingText], callback, options);
        }

        /// <summary>
        ///     Registers one logical runtime hotkey against multiple persisted binding strings.
        ///     针对多个持久化绑定字符串注册一个逻辑运行时热键。
        /// </summary>
        /// <param name="bindingTexts">
        ///     Persisted binding strings to parse.
        ///     要解析的持久化绑定字符串。
        /// </param>
        /// <param name="callback">
        ///     Callback invoked when any registered binding matches.
        ///     任意已注册绑定匹配时调用的回调。
        /// </param>
        /// <param name="options">
        ///     Optional router behavior overrides.
        ///     可选路由器行为覆盖。
        /// </param>
        /// <returns>
        ///     A handle that supports explicit rebind and unregister operations.
        ///     支持显式重新绑定和注销操作的句柄。
        /// </returns>
        /// <exception cref="FormatException">Thrown when any binding is invalid.</exception>
        public static IRuntimeHotkeyHandle Register(IEnumerable<string> bindingTexts, Action callback,
            RuntimeHotkeyOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(bindingTexts);
            ArgumentNullException.ThrowIfNull(callback);
            Initialize();

            var bindings = ParseBindings(bindingTexts);
            if (bindings.Count == 0)
                throw new FormatException("Runtime hotkey registration requires at least one valid binding.");

            lock (SyncRoot)
            {
                EnsureRouterAttached(NGame.Instance);
                if (_router == null)
                    throw new InvalidOperationException("Runtime hotkey router is not available.");

                var handle = _router.Register(bindings, callback, options);
                RitsuLibFramework.Logger.Info(
                    $"[RuntimeHotkey] Registered '{string.Join("', '", bindings.Select(static b => b.CanonicalString))}'{FormatDebugName(options)}");
                return handle;
            }
        }

        private static List<RuntimeHotkeyBinding> ParseBindings(IEnumerable<string> bindingTexts)
        {
            var bindings = new List<RuntimeHotkeyBinding>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var bindingText in bindingTexts)
            {
                if (!RuntimeHotkeyParser.TryParse(bindingText, out var binding, out var normalizedBinding))
                    throw new FormatException($"Invalid runtime hotkey binding '{bindingText}'.");
                if (seen.Add(normalizedBinding))
                    bindings.Add(binding);
            }

            return bindings;
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
