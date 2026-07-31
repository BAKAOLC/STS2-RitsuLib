namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Applies the game's squared settings-volume curve to its native FMOD buses.</para>
    ///     <para xml:lang="zh-CN">将游戏设置使用的平方音量曲线应用到原生 FMOD 总线。</para>
    /// </summary>
    public interface IFmodMixerVolumes
    {
        /// <summary>
        ///     <para xml:lang="en">Sets the master-bus volume from a settings-scale input.</para>
        ///     <para xml:lang="zh-CN">根据设置刻度输入设置主总线音量。</para>
        /// </summary>
        /// <param name="linear01">
        ///     <para xml:lang="en">The unclamped input, normally from 0 to 1; the game squares it before writing the bus volume.</para>
        ///     <para xml:lang="zh-CN">通常为 0 到 1 的未钳制输入；游戏会先对其平方，再写入总线音量。</para>
        /// </param>
        void SetMasterVolume(float linear01);

        /// <summary>
        ///     <para xml:lang="en">Sets the sound-effects-bus volume from a settings-scale input.</para>
        ///     <para xml:lang="zh-CN">根据设置刻度输入设置音效总线音量。</para>
        /// </summary>
        /// <param name="linear01">
        ///     <para xml:lang="en">The unclamped input, normally from 0 to 1; the game squares it before writing the bus volume.</para>
        ///     <para xml:lang="zh-CN">通常为 0 到 1 的未钳制输入；游戏会先对其平方，再写入总线音量。</para>
        /// </param>
        void SetSfxVolume(float linear01);

        /// <summary>
        ///     <para xml:lang="en">Sets the ambience-bus volume from a settings-scale input.</para>
        ///     <para xml:lang="zh-CN">根据设置刻度输入设置环境音总线音量。</para>
        /// </summary>
        /// <param name="linear01">
        ///     <para xml:lang="en">The unclamped input, normally from 0 to 1; the game squares it before writing the bus volume.</para>
        ///     <para xml:lang="zh-CN">通常为 0 到 1 的未钳制输入；游戏会先对其平方，再写入总线音量。</para>
        /// </param>
        void SetAmbienceVolume(float linear01);

        /// <summary>
        ///     <para xml:lang="en">Sets the music-bus volume from a settings-scale input.</para>
        ///     <para xml:lang="zh-CN">根据设置刻度输入设置音乐总线音量。</para>
        /// </summary>
        /// <param name="linear01">
        ///     <para xml:lang="en">The unclamped input, normally from 0 to 1; the game squares it before writing the bus volume.</para>
        ///     <para xml:lang="zh-CN">通常为 0 到 1 的未钳制输入；游戏会先对其平方，再写入总线音量。</para>
        /// </param>
        void SetBgmVolume(float linear01);
    }
}
