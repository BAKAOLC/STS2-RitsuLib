using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
    public abstract class ModSettingsText
    {
        public abstract string Resolve();

        public static ModSettingsText Literal(string text)
        {
            return new LiteralModSettingsText(text);
        }

        public static ModSettingsText Dynamic(Func<string> resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            return new DynamicModSettingsText(resolver);
        }

        public static ModSettingsText LocString(string table, string key, string fallback)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(table);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return new LocStringModSettingsText(table, key, fallback);
        }

        public static ModSettingsText LocString(LocString locString, string? fallback = null)
        {
            ArgumentNullException.ThrowIfNull(locString);
            return new ExistingLocStringModSettingsText(locString, fallback ?? locString.LocEntryKey);
        }

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

        private sealed class DynamicModSettingsText(Func<string> resolver) : ModSettingsText
        {
            public override string Resolve()
            {
                return resolver();
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
                    return fallback;
                }
            }
        }
    }
}
