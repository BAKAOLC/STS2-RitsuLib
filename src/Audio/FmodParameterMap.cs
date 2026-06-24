namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Builds parameter maps for <see cref="IFmodOneShotPlayback" /> multi-parameter overloads.
    ///     为 <see cref="IFmodOneShotPlayback" /> 多参数重载构建参数映射。
    /// </summary>
    public static class FmodParameterMap
    {
        /// <summary>
        ///     Builds an <see cref="AudioParameterSet" /> for the high-level playback API.
        ///     为高级播放 API 构建 <see cref="AudioParameterSet" />。
        /// </summary>
        public static AudioParameterSet Set(params (string Name, float Value)[] pairs)
        {
            return AudioParameterSet.From(Of(pairs));
        }

        /// <summary>
        ///     Empty parameter map for overloads that require a dictionary instance.
        ///     需要字典实例的重载使用的空参数映射。
        /// </summary>
        public static Dictionary<string, float> Empty()
        {
            return [];
        }

        /// <summary>
        ///     Single named parameter suitable for one-shot playback helpers.
        ///     适用于 one-shot 播放 helper 的单个命名参数。
        /// </summary>
        public static Dictionary<string, float> Single(string name, float value)
        {
            return new() { [name] = value };
        }

        /// <summary>
        ///     Builds a map from name/value tuples; duplicates last writer wins.
        ///     从 name/value 元组构建映射；重复项由最后写入者获胜。
        /// </summary>
        public static Dictionary<string, float> Of(params (string Name, float Value)[] pairs)
        {
            if (pairs.Length == 0)
                return [];

            var d = new Dictionary<string, float>(pairs.Length);
            foreach (var (name, value) in pairs)
                d[name] = value;

            return d;
        }
    }
}
