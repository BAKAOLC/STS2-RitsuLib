namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Mixer volumes using the same linear curve as vanilla settings.
    /// </summary>
    public interface IFmodMixerVolumes
    {
        /// <summary>
        ///     Master bus volume in linear 0–1 space.
        /// </summary>
        void SetMasterVolume(float linear01);

        /// <summary>
        ///     SFX bus volume in linear 0–1 space.
        /// </summary>
        void SetSfxVolume(float linear01);

        /// <summary>
        ///     Ambience bus volume in linear 0–1 space.
        /// </summary>
        void SetAmbienceVolume(float linear01);

        /// <summary>
        ///     Music / BGM bus volume in linear 0–1 space.
        /// </summary>
        void SetBgmVolume(float linear01);
    }
}
