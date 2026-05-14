namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     One-shots through <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" />.
    ///     通过 <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" /> 播放 one-shot。
    /// </summary>
    public interface IFmodOneShotPlayback
    {
        /// <summary>
        ///     Plays a one-shot at <paramref name="volume" /> linear scale.
        ///     以 <paramref name="volume" /> 线性缩放播放 one-shot。
        /// </summary>
        void PlayOneShot(string eventPath, float volume = 1f);

        /// <summary>
        ///     Plays a one-shot with initial parameter values and linear <paramref name="volume" />.
        ///     使用初始参数值和线性 <paramref name="volume" /> 播放 one-shot。
        /// </summary>
        void PlayOneShot(string eventPath, IReadOnlyDictionary<string, float> parameters, float volume = 1f);
    }
}
