using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace STS2RitsuLib.RuntimeInput
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a settings-independent runtime hotkey API that parses persisted bindings and routes callbacks through
    ///         a shared input node.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供独立于设置系统的运行时热键 API，用于解析持久化绑定，并通过共享输入节点路由回调。
    ///     </para>
    /// </summary>
    public static class RuntimeHotkeyService
    {
        private static readonly Lock SyncRoot = new();
        private static RuntimeHotkeyRouterNode? _router;
        private static IDisposable? _lifecycleSubscription;

        /// <summary>
        ///     <para xml:lang="en">Schedules the shared router to be attached when the game root becomes ready.</para>
        ///     <para xml:lang="zh-CN">安排在游戏根节点就绪时附加共享路由器。</para>
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
        ///     <para xml:lang="en">Gets compatibility snapshots for all registered runtime hotkeys.</para>
        ///     <para xml:lang="zh-CN">获取所有已注册运行时热键的兼容快照。</para>
        /// </summary>
        public static IReadOnlyList<RuntimeHotkeyRegistrationInfo> GetRegisteredHotkeys()
        {
            lock (SyncRoot)
            {
                return _router?.GetRegistrationInfos() ?? [];
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Gets detailed snapshots for all registered runtime hotkeys and their bindings.</para>
        ///     <para xml:lang="zh-CN">获取所有已注册运行时热键及其绑定的详细快照。</para>
        /// </summary>
        public static IReadOnlyList<RuntimeHotkeyRegistrationDetails> GetRegisteredHotkeyDetails()
        {
            lock (SyncRoot)
            {
                return _router?.GetRegistrationDetails() ?? [];
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to find a registered hotkey by its stable identifier.</para>
        ///     <para xml:lang="zh-CN">尝试按稳定标识符查找已注册热键。</para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">The stable registration identifier to find.</para>
        ///     <para xml:lang="zh-CN">要查找的稳定注册标识符。</para>
        /// </param>
        /// <param name="registrationInfo">
        ///     <para xml:lang="en">Receives the matching registration snapshot.</para>
        ///     <para xml:lang="zh-CN">接收匹配的注册快照。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if a matching registration was found; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若找到匹配的注册，则返回 <see langword="true" />；否则返回
        ///         <see langword="false" />。
        ///     </para>
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
        ///     <para xml:lang="en">Tries to parse a persisted binding into its canonical form.</para>
        ///     <para xml:lang="zh-CN">尝试将持久化绑定解析为规范形式。</para>
        /// </summary>
        /// <param name="bindingText">
        ///     <para xml:lang="en">The binding text to normalize.</para>
        ///     <para xml:lang="zh-CN">要规范化的绑定文本。</para>
        /// </param>
        /// <param name="normalizedBinding">
        ///     <para xml:lang="en">Receives the canonical binding when parsing succeeds.</para>
        ///     <para xml:lang="zh-CN">解析成功时接收规范化绑定。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if the binding was parsed; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若成功解析绑定，则返回 <see langword="true" />；否则返回
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryNormalizeBinding(string? bindingText, out string normalizedBinding)
        {
            return RuntimeHotkeyParser.TryParse(bindingText, out _, out normalizedBinding);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the canonical runtime binding for a Godot input action.</para>
        ///     <para xml:lang="zh-CN">为 Godot 输入动作创建规范化运行时绑定。</para>
        /// </summary>
        /// <param name="actionName">
        ///     <para xml:lang="en">The input action name, such as <c>accept</c> or <c>view_map</c>.</para>
        ///     <para xml:lang="zh-CN">输入动作名称，例如 <c>accept</c> 或 <c>view_map</c>。</para>
        /// </param>
        public static string ActionBinding(string actionName)
        {
            return RuntimeHotkeyParser.ActionBinding(actionName);
        }

        /// <summary>
        ///     <para xml:lang="en">Normalizes a binding, returning <paramref name="fallback" /> if parsing fails.</para>
        ///     <para xml:lang="zh-CN">规范化绑定；解析失败时返回 <paramref name="fallback" />。</para>
        /// </summary>
        /// <param name="bindingText">
        ///     <para xml:lang="en">The binding text to normalize.</para>
        ///     <para xml:lang="zh-CN">要规范化的绑定文本。</para>
        /// </param>
        /// <param name="fallback">
        ///     <para xml:lang="en">The value returned when parsing fails.</para>
        ///     <para xml:lang="zh-CN">解析失败时返回的值。</para>
        /// </param>
        public static string NormalizeOrDefault(string? bindingText, string fallback)
        {
            return RuntimeHotkeyParser.NormalizeOrDefault(bindingText, fallback);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers one runtime hotkey from a persisted binding.</para>
        ///     <para xml:lang="zh-CN">从一个持久化绑定注册运行时热键。</para>
        /// </summary>
        /// <param name="bindingText">
        ///     <para xml:lang="en">The persisted binding to parse.</para>
        ///     <para xml:lang="zh-CN">要解析的持久化绑定。</para>
        /// </param>
        /// <param name="callback">
        ///     <para xml:lang="en">The callback invoked when the hotkey matches.</para>
        ///     <para xml:lang="zh-CN">热键匹配时调用的回调。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">Optional routing and presentation settings.</para>
        ///     <para xml:lang="zh-CN">可选的路由与显示设置。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A handle for rebinding or unregistering the hotkey.</para>
        ///     <para xml:lang="zh-CN">用于重新绑定或注销热键的句柄。</para>
        /// </returns>
        /// <exception cref="FormatException">
        ///     <para xml:lang="en">Thrown when <paramref name="bindingText" /> is invalid.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="bindingText" /> 无效时引发。</para>
        /// </exception>
        public static IRuntimeHotkeyHandle Register(string bindingText, Action callback,
            RuntimeHotkeyOptions? options = null)
        {
            return Register([bindingText], callback, options);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers one runtime hotkey with multiple persisted bindings.</para>
        ///     <para xml:lang="zh-CN">使用多个持久化绑定注册一个运行时热键。</para>
        /// </summary>
        /// <param name="bindingTexts">
        ///     <para xml:lang="en">The persisted bindings to parse.</para>
        ///     <para xml:lang="zh-CN">要解析的持久化绑定。</para>
        /// </param>
        /// <param name="callback">
        ///     <para xml:lang="en">The callback invoked when any binding matches.</para>
        ///     <para xml:lang="zh-CN">任一绑定匹配时调用的回调。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">Optional routing and presentation settings.</para>
        ///     <para xml:lang="zh-CN">可选的路由与显示设置。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A handle for rebinding or unregistering the hotkey.</para>
        ///     <para xml:lang="zh-CN">用于重新绑定或注销热键的句柄。</para>
        /// </returns>
        /// <exception cref="FormatException">
        ///     <para xml:lang="en">Thrown when any binding is invalid or no binding is provided.</para>
        ///     <para xml:lang="zh-CN">当任一绑定无效或未提供任何绑定时引发。</para>
        /// </exception>
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
