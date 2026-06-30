using System.Reflection;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Ui.RichTextEffects
{
    /// <summary>
    ///     Global registry for custom <see cref="RichTextEffect" /> instances used by <see cref="MegaRichTextLabel" />.
    ///     用于 <see cref="MegaRichTextLabel" /> 的自定义 <see cref="RichTextEffect" /> 全局注册表。
    /// </summary>
    public static class ModRichTextEffectRegistry
    {
        private const string QualifiedBbcodeTypeSegment = "RICHTEXT";

        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModRichTextEffectRegistration> Registrations =
            new(StringComparer.Ordinal);

        /// <summary>
        ///     Builds a mod-scoped BBCode tag name from a local stem.
        ///     从本地词干构建 mod 作用域 BBCode tag 名。
        /// </summary>
        /// <remarks>
        ///     The name follows RitsuLib's compound-id convention and is lowercased for BBCode use:
        ///     <c>{normalizedModId}_RICHTEXT_{normalizedLocalTagStem}</c>.
        ///     名称遵循 RitsuLib 复合 id 约定，并为 BBCode 使用转为小写：
        ///     <c>{normalizedModId}_RICHTEXT_{normalizedLocalTagStem}</c>。
        /// </remarks>
        public static string GetQualifiedBbcode(string modId, string localTagStem)
        {
            return ModContentRegistry.GetCompoundId(modId, QualifiedBbcodeTypeSegment, localTagStem)
                .ToLowerInvariant();
        }

        /// <summary>
        ///     Registers a rich-text effect with a mod-scoped BBCode tag built from <paramref name="localTagStem" />.
        ///     使用从 <paramref name="localTagStem" /> 构建的 mod 作用域 BBCode tag 注册富文本特效。
        /// </summary>
        public static ModRichTextEffectRegistration RegisterOwned<TEffect>(string modId, string localTagStem)
            where TEffect : RichTextEffect, new()
        {
            return RegisterOwned(modId, localTagStem, new TEffect());
        }

        /// <summary>
        ///     Registers a rich-text effect instance with a mod-scoped BBCode tag built from
        ///     <paramref name="localTagStem" />.
        ///     使用从 <paramref name="localTagStem" /> 构建的 mod 作用域 BBCode tag 注册富文本特效实例。
        /// </summary>
        public static ModRichTextEffectRegistration RegisterOwned(
            string modId,
            string localTagStem,
            RichTextEffect effect)
        {
            return Register(modId, GetQualifiedBbcode(modId, localTagStem), effect);
        }

        /// <summary>
        ///     Registers a rich-text effect whose raw global BBCode name is read from its <c>bbcode</c> field or
        ///     property. Prefer <see cref="RegisterOwned{TEffect}" /> for mod-owned effects.
        ///     注册富文本特效；原始全局 BBCode 名会从其 <c>bbcode</c> 字段或属性读取。mod 自有特效优先使用
        ///     <see cref="RegisterOwned{TEffect}" />。
        /// </summary>
        public static ModRichTextEffectRegistration Register<TEffect>(string modId)
            where TEffect : RichTextEffect, new()
        {
            return Register(modId, new TEffect());
        }

        /// <summary>
        ///     Registers a rich-text effect with an explicit raw global BBCode name. Prefer
        ///     <see cref="RegisterOwned{TEffect}" /> for mod-owned effects.
        ///     使用显式原始全局 BBCode 名注册富文本特效。mod 自有特效优先使用
        ///     <see cref="RegisterOwned{TEffect}" />。
        /// </summary>
        public static ModRichTextEffectRegistration Register<TEffect>(string modId, string bbcode)
            where TEffect : RichTextEffect, new()
        {
            return Register(modId, bbcode, new TEffect());
        }

        /// <summary>
        ///     Registers a rich-text effect instance whose raw global BBCode name is read from its <c>bbcode</c> field
        ///     or property. Prefer <see cref="RegisterOwned(string,string,RichTextEffect)" /> for mod-owned effects.
        ///     注册富文本特效实例；原始全局 BBCode 名会从其 <c>bbcode</c> 字段或属性读取。mod 自有特效优先使用
        ///     <see cref="RegisterOwned(string,string,RichTextEffect)" />。
        /// </summary>
        public static ModRichTextEffectRegistration Register(string modId, RichTextEffect effect)
        {
            ArgumentNullException.ThrowIfNull(effect);
            var bbcode = ResolveBbcode(effect) ??
                         throw new ArgumentException(
                             $"Rich text effect '{effect.GetType().FullName}' does not expose a non-empty bbcode field or property.",
                             nameof(effect));

            return Register(modId, bbcode, effect);
        }

        /// <summary>
        ///     Registers a rich-text effect instance with an explicit raw global BBCode name. Prefer
        ///     <see cref="RegisterOwned(string,string,RichTextEffect)" /> for mod-owned effects.
        ///     使用显式原始全局 BBCode 名注册富文本特效实例。mod 自有特效优先使用
        ///     <see cref="RegisterOwned(string,string,RichTextEffect)" />。
        /// </summary>
        public static ModRichTextEffectRegistration Register(string modId, string bbcode, RichTextEffect effect)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(bbcode);
            ArgumentNullException.ThrowIfNull(effect);

            var normalizedBbcode = ModRichTextTag.NormalizeName(bbcode, "BBCode tag");
            EnsureEffectBbcode(effect, normalizedBbcode);
            var registration = new ModRichTextEffectRegistration(modId.Trim(), normalizedBbcode, effect);

            lock (SyncRoot)
            {
                if (Registrations.TryGetValue(normalizedBbcode, out var existing))
                {
                    if (existing.ModId == registration.ModId &&
                        ReferenceEquals(existing.Effect, registration.Effect))
                        return existing;

                    throw new InvalidOperationException(
                        $"Rich text effect '[{normalizedBbcode}]' is already registered by mod '{existing.ModId}'.");
                }

                Registrations[normalizedBbcode] = registration;
            }

            RitsuLibFramework.CreateLogger(registration.ModId)
                .Info($"[RichTextEffects] Registered [{registration.Bbcode}] ({effect.GetType().FullName}).");
            return registration;
        }

        /// <summary>
        ///     Attempts to resolve a registered rich-text effect.
        ///     尝试解析已注册的富文本特效。
        /// </summary>
        public static bool TryGet(string bbcode, out ModRichTextEffectRegistration registration)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(bbcode);
            var normalizedBbcode = ModRichTextTag.NormalizeName(bbcode, "BBCode tag");
            lock (SyncRoot)
            {
                return Registrations.TryGetValue(normalizedBbcode, out registration!);
            }
        }

        /// <summary>
        ///     Returns all registered rich-text effects in BBCode-name order.
        ///     按 BBCode 名顺序返回所有已注册的富文本特效。
        /// </summary>
        public static ModRichTextEffectRegistration[] GetRegistrationsSnapshot()
        {
            lock (SyncRoot)
            {
                return Registrations.Values
                    .OrderBy(registration => registration.Bbcode, StringComparer.Ordinal)
                    .ToArray();
            }
        }

        /// <summary>
        ///     Wraps <paramref name="text" /> with a registered rich-text tag.
        ///     用已注册的富文本 tag 包装 <paramref name="text" />。
        /// </summary>
        public static string Wrap(string bbcode, string text, params ModRichTextTagParameter[] parameters)
        {
            return ModRichTextTag.Wrap(bbcode, text, parameters);
        }

        /// <summary>
        ///     Wraps <paramref name="text" /> with a mod-scoped rich-text tag built from <paramref name="localTagStem" />.
        ///     用从 <paramref name="localTagStem" /> 构建的 mod 作用域富文本 tag 包装 <paramref name="text" />。
        /// </summary>
        public static string WrapOwned(
            string modId,
            string localTagStem,
            string text,
            params ModRichTextTagParameter[] parameters)
        {
            return ModRichTextTag.Wrap(GetQualifiedBbcode(modId, localTagStem), text, parameters);
        }

        /// <summary>
        ///     Wraps <paramref name="text" /> with a registered rich-text tag.
        ///     用已注册的富文本 tag 包装 <paramref name="text" />。
        /// </summary>
        public static string Wrap(
            ModRichTextEffectRegistration registration,
            string text,
            params ModRichTextTagParameter[] parameters)
        {
            ArgumentNullException.ThrowIfNull(registration);
            return ModRichTextTag.Wrap(registration.Bbcode, text, parameters);
        }

        internal static bool InstallInto(MegaRichTextLabel label)
        {
            ArgumentNullException.ThrowIfNull(label);

            if (!label.BbcodeEnabled)
                return false;

            ModRichTextEffectRegistration[] snapshot;
            lock (SyncRoot)
            {
                if (Registrations.Count == 0)
                    return false;

                snapshot = Registrations.Values.ToArray();
            }

            var effects = label.CustomEffects;
            var changed = false;
            foreach (var registration in snapshot)
            {
                if (effects.Contains(registration.Effect))
                    continue;

                effects.Add(registration.Effect);
                changed = true;
            }

            if (!changed)
                return false;

            label.CustomEffects = effects;
            label.ParseBbcode(label.Text);
            return true;
        }

        private static string? ResolveBbcode(RichTextEffect effect)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var type = effect.GetType();

            if (type.GetField("bbcode", flags)?.GetValue(effect) is string fieldValue &&
                !string.IsNullOrWhiteSpace(fieldValue))
                return fieldValue;

            if (type.GetProperty("bbcode", flags)?.GetValue(effect) is string propertyValue &&
                !string.IsNullOrWhiteSpace(propertyValue))
                return propertyValue;

            try
            {
                var value = effect.Get("bbcode");
                if (value.VariantType == Variant.Type.String)
                {
                    var s = value.AsString();
                    if (!string.IsNullOrWhiteSpace(s))
                        return s;
                }
            }
            catch
            {
                // Some custom C# effects expose bbcode only through normal CLR members.
            }

            return null;
        }

        private static void EnsureEffectBbcode(RichTextEffect effect, string bbcode)
        {
            if (TrySetBbcode(effect, bbcode))
                return;

            var existing = ResolveBbcode(effect);
            if (string.Equals(existing, bbcode, StringComparison.Ordinal))
                return;

            throw new InvalidOperationException(
                $"Rich text effect '{effect.GetType().FullName}' cannot be registered as '[{bbcode}]' because its " +
                "bbcode field or property is not writable. Expose a writable string bbcode member, or construct the " +
                "effect with the same bbcode before registering it.");
        }

        private static bool TrySetBbcode(RichTextEffect effect, string bbcode)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var type = effect.GetType();

            var field = type.GetField("bbcode", flags);
            if (field is { IsInitOnly: false } &&
                field.FieldType == typeof(string))
            {
                field.SetValue(effect, bbcode);
                return true;
            }

            var property = type.GetProperty("bbcode", flags);
            // ReSharper disable once InvertIf
            if (property is { CanWrite: true } &&
                property.PropertyType == typeof(string))
            {
                property.SetValue(effect, bbcode);
                return true;
            }

            return false;
        }
    }
}
