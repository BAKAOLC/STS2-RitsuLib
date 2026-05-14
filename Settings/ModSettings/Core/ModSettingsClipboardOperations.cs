using System.Collections.Concurrent;
using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Safe clipboard text reads: try/catch, per-process-frame cache, and <see cref="InvalidateCache" /> after writes.
    ///     Reduces Windows "Unable to open clipboard" spam when many menus query paste state in one frame.
    ///     安全读取剪贴板文本：try/catch、每进程帧缓存，以及写入后调用 <see cref="InvalidateCache" />。
    ///     减少多个菜单在同一帧查询粘贴状态时 Windows "Unable to open clipboard" 的刷屏。
    /// </summary>
    public static class ModSettingsClipboardAccess
    {
        private static ulong _cacheFrame = ulong.MaxValue;
        private static string _cacheText = string.Empty;

        /// <summary>
        ///     Clears the in-memory clipboard cache so the next read hits the OS again (call after writing the clipboard).
        ///     清除内存剪贴板缓存，使下一次读取再次访问 OS（写入剪贴板后调用）。
        /// </summary>
        public static void InvalidateCache()
        {
            _cacheFrame = ulong.MaxValue;
        }

        /// <summary>
        ///     Returns false if the clipboard is empty, unavailable, or an error occurred.
        ///     如果剪贴板为空、不可用或发生错误，则返回 false。
        /// </summary>
        public static bool TryGetText(out string text)
        {
            text = string.Empty;
            var frame = Engine.GetProcessFrames();
            if (_cacheFrame == frame)
            {
                if (string.IsNullOrWhiteSpace(_cacheText))
                    return false;
                text = _cacheText;
                return true;
            }

            _cacheFrame = frame;
            try
            {
                _cacheText = DisplayServer.ClipboardGet() ?? string.Empty;
            }
            catch
            {
                _cacheText = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(_cacheText))
                return false;
            text = _cacheText;
            return true;
        }
    }

    /// <summary>
    ///     Raised before a binding value is copied; set <see cref="SuppressDefaultClipboardWrite" /> to true to handle
    ///     clipboard writes yourself.
    ///     在复制绑定值之前触发；将 <see cref="SuppressDefaultClipboardWrite" /> 设为 true 可自行处理
    ///     剪贴板写入。
    /// </summary>
    public sealed class ModSettingsCopyActionEventArgs(
        IModSettingsBinding binding,
        Type valueType,
        object? value,
        ModSettingsClipboardScope scope)
        : EventArgs
    {
        /// <summary>
        ///     Binding whose value is being copied.
        ///     其值正在被复制的绑定。
        /// </summary>
        public IModSettingsBinding Binding { get; } = binding;

        /// <summary>
        ///     CLR type of the value being copied.
        ///     正在复制的值的 CLR 类型。
        /// </summary>
        public Type ValueType { get; } = valueType;

        /// <summary>
        ///     Current value snapshot passed to serializers.
        ///     当前 value snapshot passed to serializers。
        /// </summary>
        public object? Value { get; } = value;

        /// <summary>
        ///     Self-only vs. subtree copy semantics.
        ///     仅自身与子树复制语义。
        /// </summary>
        public ModSettingsClipboardScope Scope { get; } = scope;

        /// <summary>
        ///     When set by a handler, the default JSON envelope write is skipped.
        ///     由处理器设置时，会跳过默认 JSON 信封写入。
        /// </summary>
        public bool SuppressDefaultClipboardWrite { get; set; }
    }

    /// <summary>
    ///     Public snapshot of a clipboard envelope for paste validation (hides internal serialization types).
    ///     剪贴板信封的公开快照，用于粘贴校验（隐藏内部序列化类型）。
    /// </summary>
    /// <param name="Kind">
    ///     Envelope discriminator (e.g. binding vs. chrome).
    ///     信封判别符（例如 binding 或 chrome）。
    /// </param>
    /// <param name="TypeName">
    ///     Serialized CLR or logical type name inside the payload.
    ///     载荷内部的序列化 CLR 类型名或逻辑类型名。
    /// </param>
    /// <param name="TargetSignature">
    ///     Binding identity when copied from a specific setting.
    ///     从特定设置复制时的绑定标识。
    /// </param>
    /// <param name="SchemaSignature">
    ///     Adapter/schema version token for compatibility checks.
    ///     Adapter/schema version token 用于 compatibility checks.
    /// </param>
    /// <param name="Scope">
    ///     Self vs. subtree copy semantics.
    ///     自身与子树复制语义。
    /// </param>
    /// <param name="Payload">
    ///     JSON or opaque payload string.
    ///     JSON 或 opaque payload string.
    /// </param>
    public sealed record ModSettingsClipboardEnvelopeView(
        string Kind,
        string TypeName,
        string TargetSignature,
        string SchemaSignature,
        ModSettingsClipboardScope Scope,
        string Payload);

    /// <summary>
    ///     Why a binding paste did not apply (for UI feedback).
    ///     绑定粘贴未应用的原因（用于 UI 反馈）。
    /// </summary>
    public enum ModSettingsPasteFailureReason
    {
        /// <summary>
        ///     Paste succeeded or no failure was classified.
        ///     Paste succeeded 或 no failure was classified.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Clipboard text was missing or unusable.
        ///     剪贴板文本缺失或不可用。
        /// </summary>
        ClipboardEmpty = 1,

        /// <summary>
        ///     A registered paste rule returned <see cref="ModSettingsPasteVerdict.Deny" />.
        ///     某个已注册粘贴规则返回了 <see cref="ModSettingsPasteVerdict.Deny" />。
        /// </summary>
        PasteRuleDenied = 2,

        /// <summary>
        ///     Envelope or payload did not match the target binding or adapter.
        ///     信封或载荷与目标绑定或适配器不匹配。
        /// </summary>
        TypeOrShapeMismatch = 3,
    }

    /// <summary>
    ///     Whether a paste attempt should be vetoed; <see cref="Deny" /> prevents writing to the target binding.
    ///     是否应否决粘贴尝试；<see cref="Deny" /> 会阻止写入目标绑定。
    /// </summary>
    public enum ModSettingsPasteVerdict
    {
        /// <summary>
        ///     Continue with default rules (type name and schema signature match target; optional source-binding match).
        ///     继续使用默认规则（类型名和 schema 签名匹配目标；可选匹配源绑定）。
        /// </summary>
        UseDefault = 0,

        /// <summary>
        ///     Reject paste into this target.
        ///     拒绝粘贴到此目标。
        /// </summary>
        Deny = 1,
    }

    /// <summary>
    ///     Context for paste validation; <see cref="Envelope" /> is null when the clipboard is not a valid JSON envelope.
    ///     粘贴校验的上下文；当剪贴板不是有效 JSON envelope 时，<see cref="Envelope" /> 为 null。
    /// </summary>
    public sealed class ModSettingsPasteValidationContext
    {
        /// <summary>
        ///     Binding that would receive the paste.
        ///     将接收粘贴的绑定。
        /// </summary>
        public required IModSettingsBinding TargetBinding { get; init; }

        /// <summary>
        ///     Expected value type of <see cref="TargetBinding" />.
        ///     <see cref="TargetBinding" /> 的期望值类型。
        /// </summary>
        public required Type TargetValueType { get; init; }

        /// <summary>
        ///     Raw clipboard string (may or may not be a valid envelope).
        ///     原始剪贴板字符串（可能是有效信封，也可能不是）。
        /// </summary>
        public required string ClipboardText { get; init; }

        /// <summary>
        ///     Parsed envelope metadata when <see cref="ClipboardText" /> is a known RitsuLib settings envelope.
        ///     当 <see cref="ClipboardText" /> 是已知 RitsuLib 设置 envelope 时解析出的 envelope 元数据。
        /// </summary>
        public ModSettingsClipboardEnvelopeView? Envelope { get; init; }
    }

    /// <summary>
    ///     Tries to parse the clipboard into <typeparamref name="TValue" /> before default deserialization; if true, skips
    ///     <c>ModSettingsClipboardData.TryReadValue</c>.
    ///     在默认反序列化前尝试将剪贴板解析为 <typeparamref name="TValue" />；如果为 true，则跳过
    ///     <c>ModSettingsClipboardData.TryReadValue</c>。
    /// </summary>
    public delegate bool ModSettingsTryPasteApplier<TValue>(
        IModSettingsValueBinding<TValue> binding,
        IStructuredModSettingsValueAdapter<TValue> adapter,
        string clipboardText,
        out TValue value);

    /// <summary>
    ///     Central entry for binding copy/paste: default behavior, registrable paste rules, and optional strict source-binding
    ///     match.
    ///     绑定复制 / 粘贴的中心入口：默认行为、可注册粘贴规则，以及可选的严格源绑定
    ///     匹配。
    /// </summary>
    public static class ModSettingsClipboardOperations
    {
        private static readonly List<Func<ModSettingsPasteValidationContext, ModSettingsPasteVerdict>> PasteRules = [];
        private static readonly Lock PasteRulesLock = new();
        private static readonly ConcurrentDictionary<Type, List<Delegate>> PasteAppliers = new();

        /// <summary>
        ///     When true, envelope <c>TargetSignature</c> must match the current binding (legacy strict paste).
        ///     为 true 时，信封 <c>TargetSignature</c> 必须匹配当前绑定（旧版严格粘贴）。
        /// </summary>
        public static bool RequireMatchingSourceBindingForPaste { get; set; }

        /// <summary>
        ///     Raised before the default copy-to-clipboard for a binding; handlers may set
        ///     <see cref="ModSettingsCopyActionEventArgs.SuppressDefaultClipboardWrite" />.
        ///     在绑定执行默认复制到剪贴板之前触发；处理程序可设置
        ///     <see cref="ModSettingsCopyActionEventArgs.SuppressDefaultClipboardWrite" />。
        /// </summary>
        public static event Action<ModSettingsCopyActionEventArgs>? BindingValueCopyRequested;

        /// <summary>
        ///     Registers a paste rule; if any rule returns <see cref="ModSettingsPasteVerdict.Deny" />, paste is blocked.
        ///     注册粘贴规则；如果任一规则返回 <see cref="ModSettingsPasteVerdict.Deny" />，则阻止粘贴。
        /// </summary>
        public static void RegisterPasteRule(Func<ModSettingsPasteValidationContext, ModSettingsPasteVerdict> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            lock (PasteRulesLock)
            {
                PasteRules.Add(rule);
            }
        }

        /// <summary>
        ///     Registers a custom paste parser for <typeparamref name="TValue" />; runs before built-in JSON/envelope handling.
        ///     为 <typeparamref name="TValue" /> 注册自定义粘贴解析器；在内置 JSON / envelope 处理前运行。
        /// </summary>
        public static void RegisterPasteApplier<TValue>(ModSettingsTryPasteApplier<TValue> applier)
        {
            ArgumentNullException.ThrowIfNull(applier);
            PasteAppliers.GetOrAdd(typeof(TValue), _ => []).Add(applier);
        }

        /// <summary>
        ///     Runs copy hooks then writes the default clipboard envelope unless suppressed.
        ///     运行复制 hook，然后写入默认剪贴板 envelope，除非被抑制。
        /// </summary>
        public static void InvokeCopy<TValue>(IModSettingsValueBinding<TValue> binding,
            ModSettingsClipboardScope scope,
            IStructuredModSettingsValueAdapter<TValue> adapter,
            TValue value)
        {
            var args = new ModSettingsCopyActionEventArgs(binding, typeof(TValue), value, scope);
            var h = BindingValueCopyRequested;
            if (h != null)
                foreach (var @delegate in h.GetInvocationList())
                {
                    var d = (Action<ModSettingsCopyActionEventArgs>)@delegate;
                    d(args);
                }

            if (!args.SuppressDefaultClipboardWrite)
                ModSettingsClipboardData.CopyValue(binding, scope, adapter, value);
        }

        /// <summary>
        ///     Returns whether the current clipboard can be deserialized into <typeparamref name="TValue" /> for
        ///     <paramref name="binding" /> after paste rules.
        ///     返回在应用粘贴规则后，当前剪贴板是否可为 <paramref name="binding" /> 反序列化为
        ///     <typeparamref name="TValue" />。
        /// </summary>
        public static bool CanPasteBindingValue<TValue>(IModSettingsValueBinding<TValue> binding,
            IStructuredModSettingsValueAdapter<TValue> adapter)
        {
            if (!ModSettingsClipboardAccess.TryGetText(out var clipboard))
                return false;

            var view = TryCreateEnvelopeView(clipboard);
            if (!RunPasteRules(binding, typeof(TValue), clipboard, view))
                return false;

            return TryInvokePasteApplier(binding, adapter, clipboard, out _) ||
                   ModSettingsClipboardData.TryReadValue(binding, adapter, out _, RequireMatchingSourceBindingForPaste);
        }

        /// <summary>
        ///     Attempts to paste into <paramref name="binding" />; returns false when clipboard or validation fails.
        ///     尝试粘贴到 <paramref name="binding" />；当剪贴板或校验失败时返回 false。
        /// </summary>
        public static bool TryPasteBindingValue<TValue>(IModSettingsValueBinding<TValue> binding,
            IStructuredModSettingsValueAdapter<TValue> adapter, out TValue value)
        {
            return TryPasteBindingValue(binding, adapter, out value, out _);
        }

        /// <summary>
        ///     Attempts to paste into <paramref name="binding" /> and reports a <paramref name="failureReason" /> when false.
        ///     尝试粘贴到 <paramref name="binding" />，并在返回 false 时报告 <paramref name="failureReason" />。
        /// </summary>
        public static bool TryPasteBindingValue<TValue>(IModSettingsValueBinding<TValue> binding,
            IStructuredModSettingsValueAdapter<TValue> adapter, out TValue value,
            out ModSettingsPasteFailureReason failureReason)
        {
            failureReason = ModSettingsPasteFailureReason.None;
            value = default!;

            if (!ModSettingsClipboardAccess.TryGetText(out var clipboard))
            {
                failureReason = ModSettingsPasteFailureReason.ClipboardEmpty;
                return false;
            }

            var view = TryCreateEnvelopeView(clipboard);
            if (!RunPasteRules(binding, typeof(TValue), clipboard, view))
            {
                failureReason = ModSettingsPasteFailureReason.PasteRuleDenied;
                return false;
            }

            if (TryInvokePasteApplier(binding, adapter, clipboard, out value))
                return true;

            if (ModSettingsClipboardData.TryReadValue(binding, adapter, out value,
                    RequireMatchingSourceBindingForPaste))
                return true;

            failureReason = ModSettingsPasteFailureReason.TypeOrShapeMismatch;
            return false;
        }

        private static bool TryInvokePasteApplier<TValue>(IModSettingsValueBinding<TValue> binding,
            IStructuredModSettingsValueAdapter<TValue> adapter, string clipboardText, out TValue value)
        {
            if (!PasteAppliers.TryGetValue(typeof(TValue), out var list) || list.Count == 0)
            {
                value = default!;
                return false;
            }

            foreach (var d in list)
                if (((ModSettingsTryPasteApplier<TValue>)d)(binding, adapter, clipboardText, out value))
                    return true;

            value = default!;
            return false;
        }

        internal static ModSettingsClipboardEnvelopeView? TryCreateEnvelopeView(string clipboardText)
        {
            if (!ModSettingsClipboardData.TryDeserializeEnvelope(clipboardText, out var env) || env == null)
                return null;

            return new(
                env.Kind,
                env.TypeName,
                env.TargetSignature,
                env.SchemaSignature,
                env.Scope,
                env.Payload);
        }

        private static bool RunPasteRules(IModSettingsBinding binding, Type targetValueType, string clipboardText,
            ModSettingsClipboardEnvelopeView? view)
        {
            var ctx = new ModSettingsPasteValidationContext
            {
                TargetBinding = binding,
                TargetValueType = targetValueType,
                ClipboardText = clipboardText,
                Envelope = view,
            };

            List<Func<ModSettingsPasteValidationContext, ModSettingsPasteVerdict>> snapshot;
            lock (PasteRulesLock)
            {
                snapshot = [..PasteRules];
            }

            return snapshot.All(rule => rule(ctx) != ModSettingsPasteVerdict.Deny);
        }
    }
}
