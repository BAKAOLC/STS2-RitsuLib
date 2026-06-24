namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     FMOD Studio event path (e.g. <c>event:/sfx/block_gain</c>). Implicitly converts to <see cref="string" />.
    ///     FMOD Studio 事件路径（例如 <c>event:/sfx/block_gain</c>）。隐式转换为 <see cref="string" />。
    /// </summary>
    /// <param name="Value">
    ///     Raw Studio path string.
    ///     原始 Studio 路径字符串。
    /// </param>
    public readonly record struct FmodEventPath(string Value)
    {
        /// <summary>
        ///     True when <see cref="Value" /> is null or empty.
        ///     <see cref="Value" /> 为 null 或空时为 true。
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(Value);

        /// <summary>
        ///     Returns the wrapped path string.
        ///     返回包装的路径字符串。
        /// </summary>
        public static implicit operator string(FmodEventPath path)
        {
            return path.Value;
        }

        /// <summary>
        ///     Wraps a string as an <see cref="FmodEventPath" />.
        ///     将字符串包装为 <see cref="FmodEventPath" />。
        /// </summary>
        public static implicit operator FmodEventPath(string value)
        {
            return new(value);
        }

        /// <summary>
        ///     Returns <see cref="Value" />.
        ///     返回 <see cref="Value" />。
        /// </summary>
        public override string ToString()
        {
            return Value;
        }
    }
}
