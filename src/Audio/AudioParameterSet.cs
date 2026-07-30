using System.Collections.ObjectModel;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">An immutable snapshot of named numeric parameters for high-level playback requests.</para>
    ///     <para xml:lang="zh-CN">用于高级播放请求的命名数值参数不可变快照。</para>
    /// </summary>
    public sealed class AudioParameterSet
    {
        private AudioParameterSet(IReadOnlyDictionary<string, float> values)
        {
            Values = values;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the shared empty parameter set.</para>
        ///     <para xml:lang="zh-CN">获取共享的空参数集。</para>
        /// </summary>
        public static AudioParameterSet Empty { get; } =
            new(new ReadOnlyDictionary<string, float>(new Dictionary<string, float>()));

        /// <summary>
        ///     <para xml:lang="en">Gets the read-only parameter snapshot.</para>
        ///     <para xml:lang="zh-CN">获取只读参数快照。</para>
        /// </summary>
        public IReadOnlyDictionary<string, float> Values { get; }

        /// <summary>
        ///     <para xml:lang="en">Copies a parameter dictionary into an immutable set.</para>
        ///     <para xml:lang="zh-CN">将参数字典复制为不可变参数集。</para>
        /// </summary>
        /// <param name="values">
        ///     <para xml:lang="en">The values to copy; null or empty returns <see cref="Empty" />.</para>
        ///     <para xml:lang="zh-CN">要复制的值；为 <see langword="null" /> 或空时返回 <see cref="Empty" />。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The immutable parameter snapshot.</para>
        ///     <para xml:lang="zh-CN">不可变参数快照。</para>
        /// </returns>
        public static AudioParameterSet From(IReadOnlyDictionary<string, float>? values)
        {
            if (values is null || values.Count == 0)
                return Empty;

            return new(new ReadOnlyDictionary<string, float>(new Dictionary<string, float>(values)));
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a copy with one named parameter added or replaced.</para>
        ///     <para xml:lang="zh-CN">返回添加或替换一个命名参数后的副本。</para>
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
        ///     <para xml:lang="en">A new immutable parameter set.</para>
        ///     <para xml:lang="zh-CN">新的不可变参数集。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="name" /> is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="name" /> 为 <see langword="null" /> 时抛出。</para>
        /// </exception>
        public AudioParameterSet With(string name, float value)
        {
            var next = new Dictionary<string, float>(Values)
            {
                [name] = value,
            };
            return new(new ReadOnlyDictionary<string, float>(next));
        }
    }
}
