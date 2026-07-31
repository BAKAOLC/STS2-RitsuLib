namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Plays FMOD Studio one-shots through the game's
    ///         <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" /> route.
    ///     </para>
    ///     <para xml:lang="zh-CN">通过游戏的 <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" /> 路由播放 FMOD Studio 单次事件。</para>
    /// </summary>
    public interface IFmodOneShotPlayback
    {
        /// <summary>
        ///     <para xml:lang="en">Plays a path-based event once at the supplied linear volume.</para>
        ///     <para xml:lang="zh-CN">以指定的线性音量单次播放路径型事件。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The FMOD Studio event path resolved by the game's audio proxy.</para>
        ///     <para xml:lang="zh-CN">由游戏音频代理解析的 FMOD Studio 事件路径。</para>
        /// </param>
        /// <param name="volume">
        ///     <para xml:lang="en">The unclamped linear instance volume.</para>
        ///     <para xml:lang="zh-CN">未钳制的实例线性音量。</para>
        /// </param>
        void PlayOneShot(string eventPath, float volume = 1f);

        /// <summary>
        ///     <para xml:lang="en">Plays a path-based event once with initial numeric parameters and linear volume.</para>
        ///     <para xml:lang="zh-CN">使用初始数值参数和线性音量单次播放路径型事件。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The FMOD Studio event path resolved by the game's audio proxy.</para>
        ///     <para xml:lang="zh-CN">由游戏音频代理解析的 FMOD Studio 事件路径。</para>
        /// </param>
        /// <param name="parameters">
        ///     <para xml:lang="en">The initial parameter values; the native proxy skips names absent from the event description.</para>
        ///     <para xml:lang="zh-CN">初始参数值；原生代理会跳过事件描述中不存在的参数名。</para>
        /// </param>
        /// <param name="volume">
        ///     <para xml:lang="en">The unclamped linear instance volume.</para>
        ///     <para xml:lang="zh-CN">未钳制的实例线性音量。</para>
        /// </param>
        void PlayOneShot(string eventPath, IReadOnlyDictionary<string, float> parameters, float volume = 1f);
    }
}
