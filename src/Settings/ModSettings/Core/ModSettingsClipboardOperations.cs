using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides non-throwing clipboard text reads cached for one Godot process frame. This avoids repeatedly
    ///         opening the operating-system clipboard when several menus query paste availability in the same frame.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供不会抛出异常且按 Godot 进程帧缓存的剪贴板文本读取，避免多个菜单在同一帧检查粘贴可用性时反复访问系统剪贴板。
    ///     </para>
    /// </summary>
    public static class ModSettingsClipboardAccess
    {
        private static ulong _cacheFrame = ulong.MaxValue;
        private static string _cacheText = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Invalidates the in-memory cache so the next read accesses the operating-system clipboard.
        ///     </para>
        ///     <para xml:lang="zh-CN">使内存缓存失效，以便下一次读取重新访问系统剪贴板。</para>
        /// </summary>
        public static void InvalidateCache()
        {
            _cacheFrame = ulong.MaxValue;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to read non-whitespace clipboard text, returning false when the clipboard is empty,
        ///         unavailable, or cannot be read.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试读取非空白剪贴板文本；剪贴板为空、不可用或无法读取时返回 false。</para>
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
    ///     <para xml:lang="en">
    ///         Describes a binding copy request before the default clipboard envelope is written. A handler can set
    ///         <see cref="SuppressDefaultClipboardWrite" /> after performing its own clipboard write.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述默认剪贴板信封写入前的绑定复制请求；处理器自行写入剪贴板后可设置
    ///         <see cref="SuppressDefaultClipboardWrite" />。
    ///     </para>
    /// </summary>
    public sealed class ModSettingsCopyActionEventArgs(
        IModSettingsBinding binding,
        Type valueType,
        object? value,
        ModSettingsClipboardScope scope)
        : EventArgs
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the binding whose value is being copied.</para>
        ///     <para xml:lang="zh-CN">获取正在复制其值的绑定。</para>
        /// </summary>
        public IModSettingsBinding Binding { get; } =
            binding ?? throw new ArgumentNullException(nameof(binding));

        /// <summary>
        ///     <para xml:lang="en">Gets the CLR type of the copied value.</para>
        ///     <para xml:lang="zh-CN">获取所复制值的 CLR 类型。</para>
        /// </summary>
        public Type ValueType { get; } =
            valueType ?? throw new ArgumentNullException(nameof(valueType));

        /// <summary>
        ///     <para xml:lang="en">Gets the current value snapshot supplied to copy handlers and serializers.</para>
        ///     <para xml:lang="zh-CN">获取传给复制处理器及序列化器的当前值快照。</para>
        /// </summary>
        public object? Value { get; } = value;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the request copies only the value itself or its supported subtree.</para>
        ///     <para xml:lang="zh-CN">获取此次请求复制的范围是仅值自身还是其支持的子树。</para>
        /// </summary>
        public ModSettingsClipboardScope Scope { get; } = scope;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether the default JSON envelope write is skipped. If a handler throws, changes made
        ///         by that handler to this property are rolled back.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置是否跳过默认 JSON 信封写入；处理器抛出异常时，该处理器对此属性所作的更改会被撤销。
        ///     </para>
        /// </summary>
        public bool SuppressDefaultClipboardWrite { get; set; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Exposes an immutable clipboard-envelope snapshot to paste validation rules without exposing internal
    ///         serialization types.
    ///     </para>
    ///     <para xml:lang="zh-CN">向粘贴校验规则公开不可变的剪贴板信封快照，同时隐藏内部序列化类型。</para>
    /// </summary>
    /// <param name="Kind">
    ///     <para xml:lang="en">The envelope discriminator, such as a binding value or UI snapshot.</para>
    ///     <para xml:lang="zh-CN">信封判别符，例如绑定值或界面快照。</para>
    /// </param>
    /// <param name="TypeName">
    ///     <para xml:lang="en">The serialized CLR or logical type name for the payload.</para>
    ///     <para xml:lang="zh-CN">载荷的序列化 CLR 类型名或逻辑类型名。</para>
    /// </param>
    /// <param name="TargetSignature">
    ///     <para xml:lang="en">The source binding signature recorded for a copied setting.</para>
    ///     <para xml:lang="zh-CN">为所复制设置记录的源绑定签名。</para>
    /// </param>
    /// <param name="SchemaSignature">
    ///     <para xml:lang="en">The adapter schema signature used for compatibility checks.</para>
    ///     <para xml:lang="zh-CN">用于兼容性检查的适配器架构签名。</para>
    /// </param>
    /// <param name="Scope">
    ///     <para xml:lang="en">Whether the envelope represents only the target itself or its supported subtree.</para>
    ///     <para xml:lang="zh-CN">信封表示仅目标自身还是其支持的子树。</para>
    /// </param>
    /// <param name="Payload">
    ///     <para xml:lang="en">The JSON or adapter-defined opaque payload.</para>
    ///     <para xml:lang="zh-CN">JSON 或由适配器定义的不透明载荷。</para>
    /// </param>
    public sealed record ModSettingsClipboardEnvelopeView(
        string Kind,
        string TypeName,
        string TargetSignature,
        string SchemaSignature,
        ModSettingsClipboardScope Scope,
        string Payload);

    /// <summary>
    ///     <para xml:lang="en">Identifies why a binding paste was not applied, for settings UI feedback.</para>
    ///     <para xml:lang="zh-CN">标识绑定粘贴未应用的原因，供设置界面反馈使用。</para>
    /// </summary>
    public enum ModSettingsPasteFailureReason
    {
        /// <summary>
        ///     <para xml:lang="en">The paste succeeded, or no failure has been classified.</para>
        ///     <para xml:lang="zh-CN">粘贴成功，或尚未归类失败原因。</para>
        /// </summary>
        None = 0,

        /// <summary>
        ///     <para xml:lang="en">Clipboard text was empty, unavailable, or unreadable.</para>
        ///     <para xml:lang="zh-CN">剪贴板文本为空、不可用或无法读取。</para>
        /// </summary>
        ClipboardEmpty = 1,

        /// <summary>
        ///     <para xml:lang="en">
        ///         A registered paste rule returned <see cref="ModSettingsPasteVerdict.Deny" /> or threw an exception.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         已注册的粘贴规则返回 <see cref="ModSettingsPasteVerdict.Deny" /> 或抛出异常。
        ///     </para>
        /// </summary>
        PasteRuleDenied = 2,

        /// <summary>
        ///     <para xml:lang="en">No custom parser or default adapter accepted the clipboard payload.</para>
        ///     <para xml:lang="zh-CN">没有自定义解析器或默认适配器接受剪贴板载荷。</para>
        /// </summary>
        TypeOrShapeMismatch = 3,
    }

    /// <summary>
    ///     <para xml:lang="en">Specifies whether a paste rule allows default validation to continue or vetoes the paste.</para>
    ///     <para xml:lang="zh-CN">指定粘贴规则是允许继续默认校验，还是拒绝此次粘贴。</para>
    /// </summary>
    public enum ModSettingsPasteVerdict
    {
        /// <summary>
        ///     <para xml:lang="en">Allows remaining rules, custom parsers, and default adapter validation to continue.</para>
        ///     <para xml:lang="zh-CN">允许继续执行其余规则、自定义解析器及默认适配器校验。</para>
        /// </summary>
        UseDefault = 0,

        /// <summary>
        ///     <para xml:lang="en">Rejects the paste before any value is written to the target binding.</para>
        ///     <para xml:lang="zh-CN">在向目标绑定写入任何值之前拒绝粘贴。</para>
        /// </summary>
        Deny = 1,
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Supplies the target and clipboard data to a paste validation rule. <see cref="Envelope" /> is null when
    ///         the text is not a recognized RitsuLib settings envelope.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         向粘贴校验规则提供目标及剪贴板数据；文本不是可识别的 RitsuLib 设置信封时，
    ///         <see cref="Envelope" /> 为 null。
    ///     </para>
    /// </summary>
    public sealed class ModSettingsPasteValidationContext
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the binding that would receive the pasted value.</para>
        ///     <para xml:lang="zh-CN">获取将接收粘贴值的绑定。</para>
        /// </summary>
        public required IModSettingsBinding TargetBinding { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the value type expected by <see cref="TargetBinding" />.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="TargetBinding" /> 期望的值类型。</para>
        /// </summary>
        public required Type TargetValueType { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the raw clipboard text, whether or not it is a recognized envelope.</para>
        ///     <para xml:lang="zh-CN">获取原始剪贴板文本，无论其是否为可识别的信封。</para>
        /// </summary>
        public required string ClipboardText { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the parsed metadata when <see cref="ClipboardText" /> is a recognized RitsuLib settings
        ///         envelope.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当 <see cref="ClipboardText" /> 是可识别的 RitsuLib 设置信封时，获取解析出的元数据。
        ///     </para>
        /// </summary>
        public ModSettingsClipboardEnvelopeView? Envelope { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Attempts to parse clipboard text into <typeparamref name="TValue" /> before the built-in envelope and
    ///         adapter path. Returning true accepts <c>value</c> and skips default deserialization.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在内置信封及适配器路径之前尝试将剪贴板文本解析为 <typeparamref name="TValue" />；返回 true
    ///         时接受 <c>value</c> 并跳过默认反序列化。
    ///     </para>
    /// </summary>
    public delegate bool ModSettingsTryPasteApplier<TValue>(
        IModSettingsValueBinding<TValue> binding,
        IStructuredModSettingsValueAdapter<TValue> adapter,
        string clipboardText,
        out TValue value);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Coordinates binding copy and paste through default envelopes, copy handlers, paste rules, custom parsers,
    ///         and optional strict source-binding matching.
    ///     </para>
    ///     <para xml:lang="zh-CN">通过默认信封、复制处理器、粘贴规则、自定义解析器及可选的严格源绑定匹配来协调绑定复制与粘贴。</para>
    /// </summary>
    public static class ModSettingsClipboardOperations
    {
        private static readonly List<Func<ModSettingsPasteValidationContext, ModSettingsPasteVerdict>> PasteRules = [];
        private static readonly Lock PasteRulesLock = new();
        private static readonly Dictionary<Type, List<Delegate>> PasteAppliers = [];
        private static readonly Lock PasteAppliersLock = new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether default envelope deserialization requires the source
        ///         <c>TargetSignature</c> to match the target binding. Disabled by default.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置默认信封反序列化是否要求源 <c>TargetSignature</c> 与目标绑定匹配；默认禁用。
        ///     </para>
        /// </summary>
        public static bool RequireMatchingSourceBindingForPaste { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Occurs before a binding's default clipboard envelope is written. Handlers run in subscription order,
        ///         and one handler's exception is logged without preventing later handlers.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在写入绑定的默认剪贴板信封之前发生；处理器按订阅顺序执行，单个处理器抛出的异常会被记录且不会阻止后续处理器。
        ///     </para>
        /// </summary>
        public static event Action<ModSettingsCopyActionEventArgs>? BindingValueCopyRequested;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends a paste validation rule. Rules run in registration order; a deny result or exception rejects
        ///         the paste before parsing.
        ///     </para>
        ///     <para xml:lang="zh-CN">追加粘贴校验规则；规则按注册顺序执行，拒绝结果或异常都会在解析前终止粘贴。</para>
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
        ///     <para xml:lang="en">
        ///         Appends a custom parser for <typeparamref name="TValue" />. Parsers run in registration order before
        ///         built-in envelope handling; a parser exception is logged and the next parser is tried.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TValue" /> 追加自定义解析器；解析器在内置信封处理前按注册顺序执行，
        ///         单个解析器抛出异常时会记录异常并继续尝试下一个解析器。
        ///     </para>
        /// </summary>
        public static void RegisterPasteApplier<TValue>(ModSettingsTryPasteApplier<TValue> applier)
        {
            ArgumentNullException.ThrowIfNull(applier);
            lock (PasteAppliersLock)
            {
                if (!PasteAppliers.TryGetValue(typeof(TValue), out var appliers))
                {
                    appliers = [];
                    PasteAppliers.Add(typeof(TValue), appliers);
                }

                appliers.Add(applier);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Runs binding copy handlers and then writes the default clipboard envelope unless a successful handler
        ///         suppresses it.
        ///     </para>
        ///     <para xml:lang="zh-CN">执行绑定复制处理器，随后写入默认剪贴板信封，除非成功完成的处理器将其抑制。</para>
        /// </summary>
        public static void InvokeCopy<TValue>(IModSettingsValueBinding<TValue> binding,
            ModSettingsClipboardScope scope,
            IStructuredModSettingsValueAdapter<TValue> adapter,
            TValue value)
        {
            ArgumentNullException.ThrowIfNull(binding);
            ArgumentNullException.ThrowIfNull(adapter);

            var args = new ModSettingsCopyActionEventArgs(binding, typeof(TValue), value, scope);
            if (BindingValueCopyRequested is { } handlers)
                foreach (var @delegate in handlers.GetInvocationList())
                {
                    var suppressDefaultBeforeHandler = args.SuppressDefaultClipboardWrite;
                    try
                    {
                        ((Action<ModSettingsCopyActionEventArgs>)@delegate)(args);
                    }
                    catch (Exception ex)
                    {
                        args.SuppressDefaultClipboardWrite = suppressDefaultBeforeHandler;
                        var bindingType = binding.GetType();
                        RitsuLibFramework.Logger.Warn(
                            $"[Settings] A binding copy handler failed for '{bindingType.FullName ?? bindingType.Name}': {ex}");
                    }
                }

            if (!args.SuppressDefaultClipboardWrite)
                ModSettingsClipboardData.CopyValue(binding, scope, adapter, value);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Determines whether the current clipboard is accepted for <paramref name="binding" /> by all paste
        ///         rules and by either a custom parser or the default adapter path.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         确定当前剪贴板是否通过所有粘贴规则，并被自定义解析器或默认适配器路径接受用于
        ///         <paramref name="binding" />。
        ///     </para>
        /// </summary>
        public static bool CanPasteBindingValue<TValue>(IModSettingsValueBinding<TValue> binding,
            IStructuredModSettingsValueAdapter<TValue> adapter)
        {
            ArgumentNullException.ThrowIfNull(binding);
            ArgumentNullException.ThrowIfNull(adapter);

            if (!ModSettingsClipboardAccess.TryGetText(out var clipboard))
                return false;

            var view = TryCreateEnvelopeView(clipboard);
            if (!RunPasteRules(binding, typeof(TValue), clipboard, view))
                return false;

            return TryInvokePasteApplier(binding, adapter, clipboard, out _) ||
                   ModSettingsClipboardData.TryReadValue(binding, adapter, out _, RequireMatchingSourceBindingForPaste);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to parse a value for <paramref name="binding" /> from the current clipboard. This method
        ///         does not write the returned value to the binding.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试从当前剪贴板解析适用于 <paramref name="binding" /> 的值；此方法不会将返回值写入绑定。
        ///     </para>
        /// </summary>
        public static bool TryPasteBindingValue<TValue>(IModSettingsValueBinding<TValue> binding,
            IStructuredModSettingsValueAdapter<TValue> adapter, out TValue value)
        {
            return TryPasteBindingValue(binding, adapter, out value, out _);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to parse a value for <paramref name="binding" /> from the current clipboard and classifies a
        ///         false result in <paramref name="failureReason" />. This method does not write to the binding.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试从当前剪贴板解析适用于 <paramref name="binding" /> 的值，并通过
        ///         <paramref name="failureReason" /> 对失败结果进行分类；此方法不会写入绑定。
        ///     </para>
        /// </summary>
        public static bool TryPasteBindingValue<TValue>(IModSettingsValueBinding<TValue> binding,
            IStructuredModSettingsValueAdapter<TValue> adapter, out TValue value,
            out ModSettingsPasteFailureReason failureReason)
        {
            ArgumentNullException.ThrowIfNull(binding);
            ArgumentNullException.ThrowIfNull(adapter);

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
            Delegate[] snapshot;
            lock (PasteAppliersLock)
            {
                if (!PasteAppliers.TryGetValue(typeof(TValue), out var appliers) || appliers.Count == 0)
                {
                    value = default!;
                    return false;
                }

                snapshot = [.. appliers];
            }

            foreach (var applier in snapshot)
                try
                {
                    if (((ModSettingsTryPasteApplier<TValue>)applier)(binding, adapter, clipboardText, out value))
                        return true;
                }
                catch (Exception ex)
                {
                    var bindingType = binding.GetType();
                    RitsuLibFramework.Logger.Warn(
                        $"[Settings] A custom paste parser failed for '{bindingType.FullName ?? bindingType.Name}': {ex}");
                }

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
                snapshot = [.. PasteRules];
            }

            foreach (var rule in snapshot)
                try
                {
                    if (rule(ctx) == ModSettingsPasteVerdict.Deny)
                        return false;
                }
                catch (Exception ex)
                {
                    var bindingType = binding.GetType();
                    RitsuLibFramework.Logger.Warn(
                        $"[Settings] A paste validation rule failed for '{bindingType.FullName ?? bindingType.Name}': {ex}");
                    return false;
                }

            return true;
        }
    }
}
