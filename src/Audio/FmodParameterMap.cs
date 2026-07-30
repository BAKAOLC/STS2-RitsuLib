namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Provides mutable dictionary and immutable-set factories for named FMOD parameter values.</para>
    ///     <para xml:lang="zh-CN">提供命名 FMOD 参数值的可变字典和不可变参数集工厂。</para>
    /// </summary>
    public static class FmodParameterMap
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an immutable parameter set from name/value tuples, with later duplicate names replacing
        ///         earlier values.
        ///     </para>
        ///     <para xml:lang="zh-CN">根据名称/值元组创建不可变参数集；后出现的同名项会替换先前值。</para>
        /// </summary>
        /// <param name="pairs">
        ///     <para xml:lang="en">The parameter tuples to copy.</para>
        ///     <para xml:lang="zh-CN">要复制的参数元组。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The immutable parameter set, or <see cref="AudioParameterSet.Empty" /> for no tuples.</para>
        ///     <para xml:lang="zh-CN">不可变参数集；没有元组时返回 <see cref="AudioParameterSet.Empty" />。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="pairs" /> or one of its names is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="pairs" /> 或其中任一名称为 <see langword="null" /> 时抛出。</para>
        /// </exception>
        public static AudioParameterSet Set(params (string Name, float Value)[] pairs)
        {
            return AudioParameterSet.From(Of(pairs));
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a new empty mutable parameter dictionary.</para>
        ///     <para xml:lang="zh-CN">创建新的空可变参数字典。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">A new empty dictionary.</para>
        ///     <para xml:lang="zh-CN">新的空字典。</para>
        /// </returns>
        public static Dictionary<string, float> Empty()
        {
            return [];
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a new mutable dictionary containing one named parameter.</para>
        ///     <para xml:lang="zh-CN">创建包含一个命名参数的新可变字典。</para>
        /// </summary>
        /// <param name="name">
        ///     <para xml:lang="en">The case-sensitive parameter name.</para>
        ///     <para xml:lang="zh-CN">区分大小写的参数名称。</para>
        /// </param>
        /// <param name="value">
        ///     <para xml:lang="en">The parameter value.</para>
        ///     <para xml:lang="zh-CN">参数值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A new one-entry dictionary.</para>
        ///     <para xml:lang="zh-CN">新的单条目字典。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="name" /> is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="name" /> 为 <see langword="null" /> 时抛出。</para>
        /// </exception>
        public static Dictionary<string, float> Single(string name, float value)
        {
            return new() { [name] = value };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a new mutable dictionary from name/value tuples, with later duplicate names replacing
        ///         earlier values.
        ///     </para>
        ///     <para xml:lang="zh-CN">根据名称/值元组创建新的可变字典；后出现的同名项会替换先前值。</para>
        /// </summary>
        /// <param name="pairs">
        ///     <para xml:lang="en">The parameter tuples to copy.</para>
        ///     <para xml:lang="zh-CN">要复制的参数元组。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A new case-sensitive parameter dictionary.</para>
        ///     <para xml:lang="zh-CN">新的区分大小写参数字典。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="pairs" /> or one of its names is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="pairs" /> 或其中任一名称为 <see langword="null" /> 时抛出。</para>
        /// </exception>
        public static Dictionary<string, float> Of(params (string Name, float Value)[] pairs)
        {
            ArgumentNullException.ThrowIfNull(pairs);
            if (pairs.Length == 0)
                return [];

            var d = new Dictionary<string, float>(pairs.Length);
            foreach (var (name, value) in pairs)
                d[name] = value;

            return d;
        }
    }
}
