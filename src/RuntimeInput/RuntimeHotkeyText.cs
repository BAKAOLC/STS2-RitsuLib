using STS2RitsuLib.Settings;

namespace STS2RitsuLib.RuntimeInput
{
    /// <summary>
    ///     <para xml:lang="en">Represents fixed or dynamically resolved runtime hotkey metadata text.</para>
    ///     <para xml:lang="zh-CN">表示固定或动态解析的运行时热键元数据文本。</para>
    /// </summary>
    public abstract class RuntimeHotkeyText
    {
        /// <summary>
        ///     <para xml:lang="en">Resolves the text for the current locale and runtime state.</para>
        ///     <para xml:lang="zh-CN">根据当前区域设置与运行时状态解析文本。</para>
        /// </summary>
        public abstract string Resolve();

        /// <summary>
        ///     <para xml:lang="en">Creates fixed text.</para>
        ///     <para xml:lang="zh-CN">创建固定文本。</para>
        /// </summary>
        public static RuntimeHotkeyText Literal(string text)
        {
            return new LiteralRuntimeHotkeyText(text);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates text that is resolved each time the metadata is read.</para>
        ///     <para xml:lang="zh-CN">创建每次读取元数据时动态解析的文本。</para>
        /// </summary>
        public static RuntimeHotkeyText Dynamic(Func<string> resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            return new DynamicRuntimeHotkeyText(resolver);
        }

        /// <summary>
        ///     <para xml:lang="en">Wraps a fixed string.</para>
        ///     <para xml:lang="zh-CN">包装固定字符串。</para>
        /// </summary>
        public static implicit operator RuntimeHotkeyText(string text)
        {
            return Literal(text);
        }

        /// <summary>
        ///     <para xml:lang="en">Wraps deferred mod settings text.</para>
        ///     <para xml:lang="zh-CN">包装延迟解析的模组设置文本。</para>
        /// </summary>
        public static implicit operator RuntimeHotkeyText(ModSettingsText text)
        {
            ArgumentNullException.ThrowIfNull(text);
            return Dynamic(text.Resolve);
        }

        /// <summary>
        ///     <para xml:lang="en">Wraps a deferred string resolver.</para>
        ///     <para xml:lang="zh-CN">包装延迟执行的字符串解析器。</para>
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
