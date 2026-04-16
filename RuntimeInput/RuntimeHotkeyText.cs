using STS2RitsuLib.Settings;

namespace STS2RitsuLib.RuntimeInput
{
    /// <summary>
    ///     Deferred runtime hotkey metadata text that can be fixed or resolved dynamically at read time.
    /// </summary>
    public abstract class RuntimeHotkeyText
    {
        /// <summary>
        ///     Resolves the text for the current locale or runtime state.
        /// </summary>
        public abstract string Resolve();

        /// <summary>
        ///     Creates fixed text that never changes.
        /// </summary>
        public static RuntimeHotkeyText Literal(string text)
        {
            return new LiteralRuntimeHotkeyText(text);
        }

        /// <summary>
        ///     Creates text resolved dynamically each time metadata is read.
        /// </summary>
        public static RuntimeHotkeyText Dynamic(Func<string> resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            return new DynamicRuntimeHotkeyText(resolver);
        }

        /// <summary>
        ///     Implicitly wraps a fixed string.
        /// </summary>
        public static implicit operator RuntimeHotkeyText(string text)
        {
            return Literal(text);
        }

        /// <summary>
        ///     Implicitly wraps deferred mod-settings text.
        /// </summary>
        public static implicit operator RuntimeHotkeyText(ModSettingsText text)
        {
            ArgumentNullException.ThrowIfNull(text);
            return Dynamic(text.Resolve);
        }

        /// <summary>
        ///     Implicitly wraps a deferred string resolver.
        /// </summary>
        public static implicit operator RuntimeHotkeyText(Func<string> resolver)
        {
            return Dynamic(resolver);
        }

        private sealed class LiteralRuntimeHotkeyText(string text) : RuntimeHotkeyText
        {
            public override string Resolve()
            {
                return text;
            }
        }

        private sealed class DynamicRuntimeHotkeyText(Func<string> resolver) : RuntimeHotkeyText
        {
            public override string Resolve()
            {
                return resolver();
            }
        }
    }
}
