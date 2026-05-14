namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Mixer volumes using the same linear curve as vanilla settings.
    ///     使用与原版设置相同线性曲线的 mixer 音量。
    /// </summary>
    public interface IFmodMixerVolumes
    {
        /// <summary>
        ///     Master bus volume in linear 0–1 space.
        ///     线性 0-1 空间中的 master bus 音量。
        /// </summary>
        void SetMasterVolume(float linear01);

        /// <summary>
        ///     SFX bus volume in linear 0–1 space.
        ///     线性 0-1 空间中的 SFX bus 音量。
        /// </summary>
        void SetSfxVolume(float linear01);

        /// <summary>
        ///     Ambience bus volume in linear 0–1 space.
        ///     线性 0-1 空间中的环境音 bus 音量。
        /// </summary>
        void SetAmbienceVolume(float linear01);

        /// <summary>
        ///     Music / BGM bus volume in linear 0–1 space.
        ///     线性 0-1 空间中的音乐/BGM bus 音量。
        /// </summary>
        void SetBgmVolume(float linear01);
    }
}
