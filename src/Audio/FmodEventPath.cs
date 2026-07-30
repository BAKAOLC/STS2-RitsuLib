namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">A lightweight value wrapper for an FMOD Studio event or snapshot path such as <c>event:/sfx/block_gain</c>.</para>
    ///     <para xml:lang="zh-CN">FMOD Studio 事件或快照路径的轻量值包装，例如 <c>event:/sfx/block_gain</c>。</para>
    /// </summary>
    /// <param name="Value">
    ///     <para xml:lang="en">The raw FMOD Studio path; a default value may contain null at runtime.</para>
    ///     <para xml:lang="zh-CN">原始 FMOD Studio 路径；默认值在运行时可能包含 <see langword="null" />。</para>
    /// </param>
    public readonly record struct FmodEventPath(string Value)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets whether <see cref="Value" /> is null or empty; whitespace alone is not considered empty.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="Value" /> 是否为 <see langword="null" /> 或空；仅含空白不视为空。</para>
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(Value);

        /// <summary>
        ///     <para xml:lang="en">Converts a wrapped path to its raw value, using an empty string for a default null value.</para>
        ///     <para xml:lang="zh-CN">将包装路径转换为其原始值；默认值中的 <see langword="null" /> 会转换为空字符串。</para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">The wrapped path.</para>
        ///     <para xml:lang="zh-CN">已包装的路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The raw path, or an empty string when the stored value is null.</para>
        ///     <para xml:lang="zh-CN">原始路径；存储值为 <see langword="null" /> 时返回空字符串。</para>
        /// </returns>
        public static implicit operator string(FmodEventPath path)
        {
            return path.Value ?? string.Empty;
        }

        /// <summary>
        ///     <para xml:lang="en">Wraps a raw string without validating or normalizing it.</para>
        ///     <para xml:lang="zh-CN">包装原始字符串，不进行验证或规范化。</para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">The raw value to wrap.</para>
        ///     <para xml:lang="zh-CN">要包装的原始值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The wrapped FMOD Studio path.</para>
        ///     <para xml:lang="zh-CN">包装后的 FMOD Studio 路径。</para>
        /// </returns>
        public static implicit operator FmodEventPath(string value)
        {
            return new(value);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns <see cref="Value" />, using an empty string for a default null value.</para>
        ///     <para xml:lang="zh-CN">返回 <see cref="Value" />；默认值中的 <see langword="null" /> 会转换为空字符串。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The raw path, or an empty string when the stored value is null.</para>
        ///     <para xml:lang="zh-CN">原始路径；存储值为 <see langword="null" /> 时返回空字符串。</para>
        /// </returns>
        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }
}
