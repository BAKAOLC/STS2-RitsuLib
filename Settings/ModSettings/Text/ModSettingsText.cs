using System.Collections.Immutable;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Deferred label or body text for mod settings (literal, dynamic, or localized).
    ///     mod 设置的延迟标签或正文文本（字面量、动态或本地化）。
    /// </summary>
    public abstract class ModSettingsText
    {
        /// <summary>
        ///     Resolves to the final string for the current locale / state.
        ///     解析为当前语言环境 / 状态下的最终字符串。
        /// </summary>
        public abstract string Resolve();

        /// <summary>
        ///     Declares which binding dirties should invalidate live UI text derived from this instance.
        ///     声明哪些 binding 变脏时应使派生自此实例的实时 UI 文本失效。
        /// </summary>
        internal virtual ModSettingsUiRefreshSpec GetUiRefreshSpec()
        {
            return ModSettingsUiRefreshSpec.StaticDisplay;
        }

        /// <summary>
        ///     Fixed string that never changes.
        ///     永不变化的固定字符串。
        /// </summary>
        public static ModSettingsText Literal(string text)
        {
            return new LiteralModSettingsText(text);
        }

        /// <summary>
        ///     Recomputed on each <see cref="Resolve" /> (e.g. live statistics in descriptions).
        ///     每次 <see cref="Resolve" /> 时重新计算（例如描述中的实时统计）。
        /// </summary>
        public static ModSettingsText Dynamic(Func<string> resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            return new DynamicModSettingsText(resolver, default);
        }

        /// <summary>
        ///     Dynamic text that only needs UI refresh when one of the listed bindings was marked dirty (narrower than
        ///     <see cref="Dynamic(Func{string})" />).
        ///     动态文本；只有列出的某个 binding 被标记为脏时才需要刷新 UI（范围窄于
        ///     <see cref="Dynamic(Func{string})" />）。
        /// </summary>
        public static ModSettingsText Dynamic(Func<string> resolver,
            params IModSettingsBinding[] refreshWhenAnyOfTheseChange)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            ArgumentNullException.ThrowIfNull(refreshWhenAnyOfTheseChange);
            return new DynamicModSettingsText(
                resolver,
                refreshWhenAnyOfTheseChange.Length > 0
                    ? [..refreshWhenAnyOfTheseChange]
                    : default);
        }

        /// <summary>
        ///     Dynamic text that is only recomputed on a whole-page UI refresh (no binding dirty hints), for example
        ///     counters updated by button actions without going through a settings binding.
        ///     仅在整页 UI 刷新时重新计算的动态文本（没有 binding 脏提示），例如
        ///     由按钮动作更新且不经过设置 binding 的计数器。
        /// </summary>
        public static ModSettingsText DynamicFullRefreshOnly(Func<string> resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            return new DynamicFullPassModSettingsText(resolver);
        }

        /// <summary>
        ///     Looks up a <see cref="MegaCrit.Sts2.Core.Localization.LocString" /> by table and key with
        ///     <paramref name="fallback" />.
        ///     按表和键查找 <see cref="MegaCrit.Sts2.Core.Localization.LocString" />，并使用
        ///     <paramref name="fallback" />。
        /// </summary>
        public static ModSettingsText LocString(string table, string key, string fallback)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(table);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return new LocStringModSettingsText(table, key, fallback);
        }

        /// <summary>
        ///     Wraps an existing <see cref="MegaCrit.Sts2.Core.Localization.LocString" /> with optional fallback text.
        ///     包装现有 <see cref="MegaCrit.Sts2.Core.Localization.LocString" />，并可附带回退文本。
        /// </summary>
        public static ModSettingsText LocString(LocString locString, string? fallback = null)
        {
            ArgumentNullException.ThrowIfNull(locString);
            return new ExistingLocStringModSettingsText(locString, fallback ?? locString.LocEntryKey);
        }

        /// <summary>
        ///     Resolves via <see cref="I18N.Get" /> (mod settings UI localization tables).
        ///     解析 via <see cref="I18N.Get" /> (mod 设置 UI localization tables)。
        /// </summary>
        public static ModSettingsText I18N(I18N localization, string key, string fallback)
        {
            ArgumentNullException.ThrowIfNull(localization);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return new I18NModSettingsText(localization, key, fallback);
        }

        private sealed class LiteralModSettingsText(string text) : ModSettingsText
        {
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
            public override string Resolve()
            {
                try
                {
                    return MegaCrit.Sts2.Core.Localization.LocString.GetIfExists(table, key)?.GetFormattedText() ??
                           fallback;
                }
                catch
                {
                    // ignored
                    return fallback;
                }
            }
        }

        private sealed class ExistingLocStringModSettingsText(LocString locString, string fallback) : ModSettingsText
        {
            public override string Resolve()
            {
                try
                {
                    return locString.Exists() ? locString.GetFormattedText() : fallback;
                }
                catch
                {
                    // ignored
                    return fallback;
                }
            }
        }

        private sealed class I18NModSettingsText(I18N localization, string key, string fallback) : ModSettingsText
        {
            public override string Resolve()
            {
                try
                {
                    return localization.Get(key, fallback);
                }
                catch
                {
                    // ignored
                    return fallback;
                }
            }
        }
    }
}
