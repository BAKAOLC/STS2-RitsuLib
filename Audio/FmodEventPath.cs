namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     FMOD Studio event path (e.g. <c>event:/sfx/block_gain</c>). Implicitly converts to <see cref="string" />.
    /// </summary>
    /// <param name="Value">Raw Studio path string.</param>
    public readonly record struct FmodEventPath(string Value)
    {
        /// <summary>
        ///     True when <see cref="Value" /> is null or empty.
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(Value);

        /// <summary>
        ///     Returns the wrapped path string.
        /// </summary>
        public static implicit operator string(FmodEventPath path)
        {
            return path.Value;
        }

        /// <summary>
        ///     Wraps a string as an <see cref="FmodEventPath" />.
        /// </summary>
        public static implicit operator FmodEventPath(string value)
        {
            return new(value);
        }

        /// <summary>
        ///     Returns <see cref="Value" />.
        /// </summary>
        public override string ToString()
        {
            return Value;
        }
    }
}
