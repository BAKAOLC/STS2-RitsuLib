namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Immutable parameter bag used by the high-level playback API.
    ///     高级播放 API 使用的不可变参数包。
    /// </summary>
    public sealed class AudioParameterSet
    {
        private AudioParameterSet(IReadOnlyDictionary<string, float> values)
        {
            Values = values;
        }

        /// <summary>
        ///     Empty parameter set.
        ///     空参数集。
        /// </summary>
        public static AudioParameterSet Empty { get; } = new(new Dictionary<string, float>());

        /// <summary>
        ///     Parameter values carried by this set.
        ///     此集合携带的参数值。
        /// </summary>
        public IReadOnlyDictionary<string, float> Values { get; }

        /// <summary>
        ///     Creates a parameter set from an existing dictionary.
        ///     从现有字典创建参数集。
        /// </summary>
        public static AudioParameterSet From(IReadOnlyDictionary<string, float>? values)
        {
            if (values is null || values.Count == 0)
                return Empty;

            return new(new Dictionary<string, float>(values));
        }

        /// <summary>
        ///     Returns a new parameter set with the given name/value applied.
        ///     返回一个应用了给定 name/value 的新参数集。
        /// </summary>
        public AudioParameterSet With(string name, float value)
        {
            var next = new Dictionary<string, float>(Values)
            {
                [name] = value,
            };
            return new(next);
        }
    }
}
