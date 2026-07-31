using System.Collections.Immutable;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Represents deferred label or body text for mod settings. Text can be literal, dynamic, or localized.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         表示模组设置使用的延迟标签或正文文本。文本可以是字面文本、动态文本或本地化文本。
    ///     </para>
    /// </summary>
    public abstract class ModSettingsText
    {
        internal virtual string? FallbackText => null;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves the final string for the current locale and state.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析当前语言和状态对应的最终字符串。
        ///     </para>
        /// </summary>
        public abstract string Resolve();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Declares which dirty bindings invalidate live UI text derived from this instance.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         声明哪些脏绑定会使由此实例生成的实时界面文本失效。
        ///     </para>
        /// </summary>
        internal virtual ModSettingsUiRefreshSpec GetUiRefreshSpec()
        {
            return ModSettingsUiRefreshSpec.StaticDisplay;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates text backed by a fixed string.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建由固定字符串提供的文本。
        ///     </para>
        /// </summary>
        public static ModSettingsText Literal(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            return new LiteralModSettingsText(text);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates text recomputed on every <see cref="Resolve" /> call. In the settings UI, any dirty binding
        ///         invalidates the displayed text.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建每次调用 <see cref="Resolve" /> 时重新计算的文本。在设置界面中，任意绑定变脏都会使其显示文本失效。
        ///     </para>
        /// </summary>
        public static ModSettingsText Dynamic(Func<string> resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            return new DynamicModSettingsText(resolver, default);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates dynamic text whose UI display is invalidated only when one of the listed bindings becomes
        ///         dirty. This is narrower than <see cref="Dynamic(Func{string})" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建仅在所列绑定之一变脏时使界面显示失效的动态文本。其刷新范围小于
        ///         <see cref="Dynamic(Func{string})" />。
        ///     </para>
        /// </summary>
        public static ModSettingsText Dynamic(Func<string> resolver,
            params IModSettingsBinding[] refreshWhenAnyOfTheseChange)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            ArgumentNullException.ThrowIfNull(refreshWhenAnyOfTheseChange);
            if (refreshWhenAnyOfTheseChange.Any(static binding => binding == null))
                throw new ArgumentException("Refresh bindings cannot contain null.",
                    nameof(refreshWhenAnyOfTheseChange));

            return new DynamicModSettingsText(
                resolver,
                refreshWhenAnyOfTheseChange.Length > 0
                    ? [.. refreshWhenAnyOfTheseChange]
                    : default);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates dynamic text whose UI display is recomputed only during a whole-page refresh. Use it for
        ///         values changed outside settings bindings, such as counters updated by button actions.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建仅在整页刷新时重新计算界面显示的动态文本。适用于不经由设置绑定变更的值，例如由按钮操作更新的计数器。
        ///     </para>
        /// </summary>
        public static ModSettingsText DynamicFullRefreshOnly(Func<string> resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            return new DynamicFullPassModSettingsText(resolver);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates text that looks up a <see cref="MegaCrit.Sts2.Core.Localization.LocString" /> by table and
        ///         key, returning <paramref name="fallback" /> when lookup or formatting fails.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建按表名和键查找 <see cref="MegaCrit.Sts2.Core.Localization.LocString" /> 的文本；查找或格式化失败时
        ///         返回 <paramref name="fallback" />。
        ///     </para>
        /// </summary>
        public static ModSettingsText LocString(string table, string key, string fallback)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(table);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(fallback);
            return new LocStringModSettingsText(table, key, fallback);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Wraps an existing <see cref="MegaCrit.Sts2.Core.Localization.LocString" /> and uses its localization
        ///         key as the fallback when <paramref name="fallback" /> is omitted.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         包装现有的 <see cref="MegaCrit.Sts2.Core.Localization.LocString" />；未提供
        ///         <paramref name="fallback" /> 时，以其本地化键作为回退文本。
        ///     </para>
        /// </summary>
        public static ModSettingsText LocString(LocString locString, string? fallback = null)
        {
            ArgumentNullException.ThrowIfNull(locString);
            return new ExistingLocStringModSettingsText(locString, fallback ?? locString.LocEntryKey);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates text resolved through <see cref="I18N.Get" /> using the supplied localization table.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建通过所给本地化表的 <see cref="I18N.Get" /> 解析的文本。
        ///     </para>
        /// </summary>
        public static ModSettingsText I18N(I18N localization, string key, string fallback)
        {
            ArgumentNullException.ThrowIfNull(localization);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(fallback);
            return new I18NModSettingsText(localization, key, fallback);
        }

        internal static ModSettingsText DeferredI18N(Func<I18N> localizationFactory, string key, string fallback)
        {
            ArgumentNullException.ThrowIfNull(localizationFactory);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(fallback);
            return new DeferredI18NModSettingsText(localizationFactory, key, fallback);
        }

        private sealed class LiteralModSettingsText(string text) : ModSettingsText
        {
            internal override string FallbackText => text;

            public override string Resolve()
            {
                return text;
            }
        }

        private sealed class DynamicModSettingsText(
            Func<string> resolver,
            ImmutableArray<IModSettingsBinding> refreshWhen)
            : ModSettingsText
        {
            public override string Resolve()
            {
                return resolver();
            }

            internal override ModSettingsUiRefreshSpec GetUiRefreshSpec()
            {
                return refreshWhen.IsDefaultOrEmpty
                    ? ModSettingsUiRefreshSpec.AnyBindingDirty
                    : new(ModSettingsRefreshRegistrationKind.SpecificBindings, refreshWhen);
            }
        }

        private sealed class DynamicFullPassModSettingsText(Func<string> resolver) : ModSettingsText
        {
            public override string Resolve()
            {
                return resolver();
            }

            internal override ModSettingsUiRefreshSpec GetUiRefreshSpec()
            {
                return ModSettingsUiRefreshSpec.StaticDisplay;
            }
        }

        private sealed class LocStringModSettingsText(string table, string key, string fallback) : ModSettingsText
        {
            internal override string FallbackText => fallback;

            public override string Resolve()
            {
                try
                {
                    return MegaCrit.Sts2.Core.Localization.LocString.GetIfExists(table, key)?.GetFormattedText() ??
                           fallback;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Settings] Failed to resolve localization '{table}:{key}'; using fallback: {ex}");
                    return fallback;
                }
            }
        }

        private sealed class ExistingLocStringModSettingsText(LocString locString, string fallback) : ModSettingsText
        {
            internal override string FallbackText => fallback;

            public override string Resolve()
            {
                try
                {
                    return locString.Exists() ? locString.GetFormattedText() : fallback;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Settings] Failed to resolve a game localization value; using fallback: {ex}");
                    return fallback;
                }
            }
        }

        private sealed class I18NModSettingsText(I18N localization, string key, string fallback) : ModSettingsText
        {
            internal override string FallbackText => fallback;

            public override string Resolve()
            {
                try
                {
                    return localization.Get(key, fallback);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Settings] Failed to resolve I18N key '{key}'; using fallback: {ex}");
                    return fallback;
                }
            }
        }

        private sealed class DeferredI18NModSettingsText(Func<I18N> localizationFactory, string key, string fallback)
            : ModSettingsText
        {
            internal override string FallbackText => fallback;

            public override string Resolve()
            {
                try
                {
                    return localizationFactory().Get(key, fallback);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Settings] Failed to resolve deferred I18N key '{key}'; using fallback: {ex}");
                    return fallback;
                }
            }
        }
    }
}
