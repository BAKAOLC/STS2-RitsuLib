namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Immutable parameter bag used by the high-level playback API.
    /// </summary>
    public sealed class AudioParameterSet
    {
        private AudioParameterSet(IReadOnlyDictionary<string, float> values)
        {
            Values = values;
        }

        /// <summary>
        ///     Empty parameter set.
        /// </summary>
        public static AudioParameterSet Empty { get; } = new(new Dictionary<string, float>());

        /// <summary>
        ///     Parameter values carried by this set.
        /// </summary>
        public IReadOnlyDictionary<string, float> Values { get; }

        /// <summary>
        ///     Creates a parameter set from an existing dictionary.
        /// </summary>
        public static AudioParameterSet From(IReadOnlyDictionary<string, float>? values)
        {
            if (values is null || values.Count == 0)
                return Empty;

            return new(new Dictionary<string, float>(values));
        }

        /// <summary>
        ///     Returns a new parameter set with the given name/value applied.
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
