namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Single active music instance (same as vanilla proxy).
    /// </summary>
    public interface IFmodMusicPlayback
    {
        /// <summary>
        ///     Switches the active music event to <paramref name="eventPath" />.
        /// </summary>
        void PlayMusic(string eventPath);

        /// <summary>
        ///     Stops the current music instance.
        /// </summary>
        void StopMusic();

        /// <summary>
        ///     Updates a global music parameter using a label value.
        /// </summary>
        void UpdateMusicParameter(string parameterName, string labelValue);
    }
}
