using System.Reflection;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Ui.RichTextEffects
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers custom <see cref="RichTextEffect" /> instances for
    ///         <see cref="MegaRichTextLabel" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="MegaRichTextLabel" /> 注册自定义 <see cref="RichTextEffect" /> 实例。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         BBCode tag names are global. Repeating a registration returns the existing definition only
    ///         when the owning mod and effect instance are the same; every other collision fails.
    ///     </para>
    ///     <para xml:lang="en">
    ///         RitsuLib installs registered effects into BBCode-enabled labels when they become ready, after
    ///         text updates, and after the Godot editor restores scene data following a save.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         BBCode 标签名属于全局命名空间。仅当所属模组和特效实例都相同时，重复注册才会返回已有定义；
    ///         其他冲突都会失败。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         RitsuLib 会在启用 BBCode 的标签进入就绪状态、文本更新后，以及 Godot 编辑器在保存后恢复
    ///         场景数据时安装已注册特效。
    ///     </para>
    /// </remarks>
    public static class ModRichTextEffectRegistry
    {
        private const string QualifiedBbcodeTypeSegment = "RICHTEXT";

        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModRichTextEffectRegistration> Registrations =
            new(StringComparer.Ordinal);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a lowercase, mod-qualified BBCode tag name from <paramref name="localTagStem" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         根据 <paramref name="localTagStem" /> 构建小写且包含模组限定信息的 BBCode 标签名。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         The result follows RitsuLib's compound-ID convention; for example, mod ID
        ///         <c>My Mod</c> and local stem <c>Glitch</c> produce <c>mymod_richtext_glitch</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         结果遵循 RitsuLib 的复合 ID 约定。例如，模组 ID <c>My Mod</c> 和本地名称
        ///         <c>Glitch</c> 会生成 <c>mymod_richtext_glitch</c>。
        ///     </para>
        /// </remarks>
        public static string GetQualifiedBbcode(string modId, string localTagStem)
        {
            return ModContentRegistry.GetCompoundId(modId, QualifiedBbcodeTypeSegment, localTagStem)
                .ToLowerInvariant();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates and registers <typeparamref name="TEffect" /> under the mod-qualified tag derived
        ///         from <paramref name="localTagStem" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建 <typeparamref name="TEffect" />，并使用根据 <paramref name="localTagStem" /> 派生的
        ///         模组限定标签进行注册。
        ///     </para>
        /// </summary>
        public static ModRichTextEffectRegistration RegisterOwned<TEffect>(string modId, string localTagStem)
            where TEffect : RichTextEffect, new()
        {
            return RegisterOwned(modId, localTagStem, new TEffect());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="effect" /> under the mod-qualified tag derived from
        ///         <paramref name="localTagStem" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用根据 <paramref name="localTagStem" /> 派生且包含模组限定信息的标签注册
        ///         <paramref name="effect" />。
        ///     </para>
        /// </summary>
        public static ModRichTextEffectRegistration RegisterOwned(
            string modId,
            string localTagStem,
            RichTextEffect effect)
        {
            return Register(modId, GetQualifiedBbcode(modId, localTagStem), effect);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates and registers <typeparamref name="TEffect" /> using the global tag name exposed by
        ///         its <c>bbcode</c> field or property. Prefer <see cref="RegisterOwned{TEffect}" /> for
        ///         mod-owned effects.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建 <typeparamref name="TEffect" />，并使用其 <c>bbcode</c> 字段或属性公开的全局标签名
        ///         进行注册。模组自有特效建议使用 <see cref="RegisterOwned{TEffect}" />。
        ///     </para>
        /// </summary>
        public static ModRichTextEffectRegistration Register<TEffect>(string modId)
            where TEffect : RichTextEffect, new()
        {
            return Register(modId, new TEffect());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates and registers <typeparamref name="TEffect" /> under an explicit global
        ///         <paramref name="bbcode" /> tag. Prefer <see cref="RegisterOwned{TEffect}" /> for
        ///         mod-owned effects.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建 <typeparamref name="TEffect" />，并使用显式全局 <paramref name="bbcode" /> 标签
        ///         进行注册。模组自有特效建议使用 <see cref="RegisterOwned{TEffect}" />。
        ///     </para>
        /// </summary>
        public static ModRichTextEffectRegistration Register<TEffect>(string modId, string bbcode)
            where TEffect : RichTextEffect, new()
        {
            return Register(modId, bbcode, new TEffect());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="effect" /> using the global tag name exposed by its
        ///         <c>bbcode</c> field or property. Prefer
        ///         <see cref="RegisterOwned(string,string,RichTextEffect)" /> for mod-owned effects.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="effect" /> 的 <c>bbcode</c> 字段或属性公开的全局标签名注册该特效。
        ///         模组自有特效建议使用 <see cref="RegisterOwned(string,string,RichTextEffect)" />。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Registers <paramref name="effect" /> under an explicit global <paramref name="bbcode" />
        ///         tag. The effect must expose a writable string <c>bbcode</c> member or already use the
        ///         requested name.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用显式全局 <paramref name="bbcode" /> 标签注册 <paramref name="effect" />。
        ///         该特效必须公开可写的字符串 <c>bbcode</c> 成员，或已经使用所请求的名称。
        ///     </para>
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
                    if (StringComparer.OrdinalIgnoreCase.Equals(existing.ModId, registration.ModId) &&
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
        ///     <para xml:lang="en">Attempts to find a registered effect by global BBCode tag name.</para>
        ///     <para xml:lang="zh-CN">尝试按全局 BBCode 标签名查找已注册特效。</para>
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
        ///     <para xml:lang="en">Returns all registered effects in ordinal BBCode-name order.</para>
        ///     <para xml:lang="zh-CN">按 BBCode 标签名的序数排序返回所有已注册特效。</para>
        /// </summary>
        public static ModRichTextEffectRegistration[] GetRegistrationsSnapshot()
        {
            lock (SyncRoot)
            {
                return
                [
                    .. Registrations.Values
                        .OrderBy(registration => registration.Bbcode, StringComparer.Ordinal),
                ];
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Wraps <paramref name="text" /> in the global <paramref name="bbcode" /> tag.
        ///         This method does not require the tag to be registered.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用全局 <paramref name="bbcode" /> 标签包裹 <paramref name="text" />。
        ///         此方法不要求该标签已经注册。
        ///     </para>
        /// </summary>
        public static string Wrap(string bbcode, string text, params ModRichTextTagParameter[] parameters)
        {
            return ModRichTextTag.Wrap(bbcode, text, parameters);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Wraps <paramref name="text" /> in the mod-qualified tag derived from
        ///         <paramref name="localTagStem" />. This method does not require the tag to be registered.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用根据 <paramref name="localTagStem" /> 派生且包含模组限定信息的标签包裹
        ///         <paramref name="text" />。此方法不要求该标签已经注册。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Wraps <paramref name="text" /> in the tag described by <paramref name="registration" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="registration" /> 描述的标签包裹 <paramref name="text" />。
        ///     </para>
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

                snapshot = [.. Registrations.Values];
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

            var value = effect.Get("bbcode");
            if (value.VariantType == Variant.Type.String)
            {
                var s = value.AsString();
                if (!string.IsNullOrWhiteSpace(s))
                    return s;
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
