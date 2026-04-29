using System.Collections.Concurrent;
using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Safe clipboard text reads: try/catch, per-process-frame cache, and <see cref="InvalidateCache" /> after writes.
    ///     Reduces Windows "Unable to open clipboard" spam when many menus query paste state in one frame.
    /// </summary>
    public static class ModSettingsClipboardAccess
    {
        private static ulong _cacheFrame = ulong.MaxValue;
        private static string _cacheText = string.Empty;

        /// <summary>
        ///     Clears the in-memory clipboard cache so the next read hits the OS again (call after writing the clipboard).
        /// </summary>
        public static void InvalidateCache()
        {
            _cacheFrame = ulong.MaxValue;
        }

        /// <summary>
        ///     Returns false if the clipboard is empty, unavailable, or an error occurred.
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
        /// </summary>
        public IModSettingsBinding Binding { get; } = binding;

        /// <summary>
        ///     CLR type of the value being copied.
        /// </summary>
        public Type ValueType { get; } = valueType;

        /// <summary>
        ///     Current value snapshot passed to serializers.
        /// </summary>
        public object? Value { get; } = value;

        /// <summary>
        ///     Self-only vs. subtree copy semantics.
        /// </summary>
        public ModSettingsClipboardScope Scope { get; } = scope;

        /// <summary>
        ///     When set by a handler, the default JSON envelope write is skipped.
        /// </summary>
        public bool SuppressDefaultClipboardWrite { get; set; }
    }

    /// <summary>
    ///     Public snapshot of a clipboard envelope for paste validation (hides internal serialization types).
    /// </summary>
    /// <param name="Kind">Envelope discriminator (e.g. binding vs. chrome).</param>
    /// <param name="TypeName">Serialized CLR or logical type name inside the payload.</param>
    /// <param name="TargetSignature">Binding identity when copied from a specific setting.</param>
    /// <param name="SchemaSignature">Adapter/schema version token for compatibility checks.</param>
    /// <param name="Scope">Self vs. subtree copy semantics.</param>
    /// <param name="Payload">JSON or opaque payload string.</param>
    public sealed record ModSettingsClipboardEnvelopeView(
        string Kind,
        string TypeName,
        string TargetSignature,
        string SchemaSignature,
        ModSettingsClipboardScope Scope,
        string Payload);

    /// <summary>
    ///     Why a binding paste did not apply (for UI feedback).
    /// </summary>
    public enum ModSettingsPasteFailureReason
    {
        /// <summary>
        ///     Paste succeeded or no failure was classified.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Clipboard text was missing or unusable.
        /// </summary>
        ClipboardEmpty = 1,

        /// <summary>
        ///     A registered paste rule returned <see cref="ModSettingsPasteVerdict.Deny" />.
        /// </summary>
        PasteRuleDenied = 2,

        /// <summary>
        ///     Envelope or payload did not match the target binding or adapter.
        /// </summary>
        TypeOrShapeMismatch = 3,
    }

    /// <summary>
    ///     Whether a paste attempt should be vetoed; <see cref="Deny" /> prevents writing to the target binding.
    /// </summary>
    public enum ModSettingsPasteVerdict
    {
        /// <summary>
        ///     Continue with default rules (type name and schema signature match target; optional source-binding match).
        /// </summary>
        UseDefault = 0,

        /// <summary>
        ///     Reject paste into this target.
        /// </summary>
        Deny = 1,
    }

    /// <summary>
    ///     Context for paste validation; <see cref="Envelope" /> is null when the clipboard is not a valid JSON envelope.
    /// </summary>
    public sealed class ModSettingsPasteValidationContext
    {
        /// <summary>
        ///     Binding that would receive the paste.
        /// </summary>
        public required IModSettingsBinding TargetBinding { get; init; }

        /// <summary>
        ///     Expected value type of <see cref="TargetBinding" />.
        /// </summary>
        public required Type TargetValueType { get; init; }

        /// <summary>
        ///     Raw clipboard string (may or may not be a valid envelope).
        /// </summary>
        public required string ClipboardText { get; init; }

        /// <summary>
        ///     Parsed envelope metadata when <see cref="ClipboardText" /> is a known RitsuLib settings envelope.
        /// </summary>
        public ModSettingsClipboardEnvelopeView? Envelope { get; init; }
    }

    /// <summary>
    ///     Tries to parse the clipboard into <typeparamref name="TValue" /> before default deserialization; if true, skips
    ///     <c>ModSettingsClipboardData.TryReadValue</c>.
    /// </summary>
    public delegate bool ModSettingsTryPasteApplier<TValue>(
        IModSettingsValueBinding<TValue> binding,
        IStructuredModSettingsValueAdapter<TValue> adapter,
        string clipboardText,
        out TValue value);

    /// <summary>
    ///     Central entry for binding copy/paste: default behavior, registrable paste rules, and optional strict source-binding
    ///     match.
    /// </summary>
    public static class ModSettingsClipboardOperations
    {
        private static readonly List<Func<ModSettingsPasteValidationContext, ModSettingsPasteVerdict>> PasteRules = [];
        private static readonly Lock PasteRulesLock = new();
        private static readonly ConcurrentDictionary<Type, List<Delegate>> PasteAppliers = new();

        /// <summary>
        ///     When true, envelope <c>TargetSignature</c> must match the current binding (legacy strict paste).
        /// </summary>
        public static bool RequireMatchingSourceBindingForPaste { get; set; }

        /// <summary>
        ///     Raised before the default copy-to-clipboard for a binding; handlers may set
        ///     <see cref="ModSettingsCopyActionEventArgs.SuppressDefaultClipboardWrite" />.
        /// </summary>
        public static event Action<ModSettingsCopyActionEventArgs>? BindingValueCopyRequested;

        /// <summary>
        ///     Registers a paste rule; if any rule returns <see cref="ModSettingsPasteVerdict.Deny" />, paste is blocked.
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
        /// </summary>
        public static void RegisterPasteApplier<TValue>(ModSettingsTryPasteApplier<TValue> applier)
        {
            ArgumentNullException.ThrowIfNull(applier);
            PasteAppliers.GetOrAdd(typeof(TValue), _ => []).Add(applier);
        }

        /// <summary>
        ///     Runs copy hooks then writes the default clipboard envelope unless suppressed.
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
        /// </summary>
        public static bool TryPasteBindingValue<TValue>(IModSettingsValueBinding<TValue> binding,
            IStructuredModSettingsValueAdapter<TValue> adapter, out TValue value)
        {
            return TryPasteBindingValue(binding, adapter, out value, out _);
        }

        /// <summary>
        ///     Attempts to paste into <paramref name="binding" /> and reports a <paramref name="failureReason" /> when false.
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
