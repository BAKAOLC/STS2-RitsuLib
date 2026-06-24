using STS2RitsuLib.Settings;

namespace STS2RitsuLib.RuntimeInput
{
    /// <summary>
    ///     Deferred runtime hotkey metadata text that can be fixed or resolved dynamically at read time.
    ///     延迟的运行时热键元数据文本，可在读取时固定或动态解析。
    /// </summary>
    public abstract class RuntimeHotkeyText
    {
        /// <summary>
        ///     Resolves the text for the current locale or runtime state.
        ///     为当前区域设置或运行时状态解析文本。
        /// </summary>
        public abstract string Resolve();

        /// <summary>
        ///     Creates fixed text that never changes.
        ///     创建永不变化的固定文本。
        /// </summary>
        public static RuntimeHotkeyText Literal(string text)
        {
            return new LiteralRuntimeHotkeyText(text);
        }

        /// <summary>
        ///     Creates text resolved dynamically each time metadata is read.
        ///     创建每次读取元数据时动态解析的文本。
        /// </summary>
        public static RuntimeHotkeyText Dynamic(Func<string> resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            return new DynamicRuntimeHotkeyText(resolver);
        }

        /// <summary>
        ///     Implicitly wraps a fixed string.
        ///     隐式包装固定字符串。
        /// </summary>
        public static implicit operator RuntimeHotkeyText(string text)
        {
            return Literal(text);
        }

        /// <summary>
        ///     Implicitly wraps deferred mod-settings text.
        ///     隐式包装延迟的 mod 设置文本。
        /// </summary>
        public static implicit operator RuntimeHotkeyText(ModSettingsText text)
        {
            ArgumentNullException.ThrowIfNull(text);
            return Dynamic(text.Resolve);
        }

        /// <summary>
        ///     Implicitly wraps a deferred string resolver.
        ///     隐式包装延迟的字符串解析器。
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
