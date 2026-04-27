using System.Collections.Immutable;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Deferred label or body text for mod settings (literal, dynamic, or localized).
    /// </summary>
    public abstract class ModSettingsText
    {
        /// <summary>
        ///     Resolves to the final string for the current locale / state.
        /// </summary>
        public abstract string Resolve();

        /// <summary>
        ///     Declares which binding dirties should invalidate live UI text derived from this instance.
        /// </summary>
        internal virtual ModSettingsUiRefreshSpec GetUiRefreshSpec()
        {
            return ModSettingsUiRefreshSpec.StaticDisplay;
        }

        /// <summary>
        ///     Fixed string that never changes.
        /// </summary>
        public static ModSettingsText Literal(string text)
        {
            return new LiteralModSettingsText(text);
        }

        /// <summary>
        ///     Recomputed on each <see cref="Resolve" /> (e.g. live statistics in descriptions).
        /// </summary>
        public static ModSettingsText Dynamic(Func<string> resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            return new DynamicModSettingsText(resolver, default);
        }

        /// <summary>
        ///     Dynamic text that only needs UI refresh when one of the listed bindings was marked dirty (narrower than
        ///     <see cref="Dynamic(Func{string})" />).
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
        /// </summary>
        public static ModSettingsText DynamicFullRefreshOnly(Func<string> resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            return new DynamicFullPassModSettingsText(resolver);
        }

        /// <summary>
        ///     Looks up a <see cref="MegaCrit.Sts2.Core.Localization.LocString" /> by table and key with
        ///     <paramref name="fallback" />.
        /// </summary>
        public static ModSettingsText LocString(string table, string key, string fallback)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(table);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return new LocStringModSettingsText(table, key, fallback);
        }

        /// <summary>
        ///     Wraps an existing <see cref="MegaCrit.Sts2.Core.Localization.LocString" /> with optional fallback text.
        /// </summary>
        public static ModSettingsText LocString(LocString locString, string? fallback = null)
        {
            ArgumentNullException.ThrowIfNull(locString);
            return new ExistingLocStringModSettingsText(locString, fallback ?? locString.LocEntryKey);
        }

        /// <summary>
        ///     Resolves via <see cref="I18N.Get" /> (mod settings UI localization tables).
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
