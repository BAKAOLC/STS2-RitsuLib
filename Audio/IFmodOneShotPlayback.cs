namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     One-shots through <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" />.
    /// </summary>
    public interface IFmodOneShotPlayback
    {
        /// <summary>
        ///     Plays a one-shot at <paramref name="volume" /> linear scale.
        /// </summary>
        void PlayOneShot(string eventPath, float volume = 1f);

        /// <summary>
        ///     Plays a one-shot with initial parameter values and linear <paramref name="volume" />.
        /// </summary>
        void PlayOneShot(string eventPath, IReadOnlyDictionary<string, float> parameters, float volume = 1f);
    }
}
