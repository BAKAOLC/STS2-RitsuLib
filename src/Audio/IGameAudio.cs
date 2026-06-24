namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     High-level audio API for typed playback, handle-based control, lifecycle scopes, and adaptive music flows.
    ///     用于类型化播放、基于句柄的控制、生命周期作用域和自适应音乐流程的高级音频 API。
    /// </summary>
    public interface IGameAudio
    {
        /// <summary>
        ///     Plays any supported source and returns a structured result.
        ///     播放任何受支持的源并返回结构化结果。
        /// </summary>
        AudioPlayResult Play(AudioSource source, AudioPlaybackOptions? options = null);

        /// <summary>
        ///     Plays a source as a one-shot when supported.
        ///     受支持时，将源作为 one-shot 播放。
        /// </summary>
        AudioPlayResult PlayOneShot(AudioSource source, AudioPlaybackOptions? options = null);

        /// <summary>
        ///     Plays a loop-capable source and returns a controllable loop handle when successful.
        ///     播放支持 loop 的源，并在成功时返回可控制的 loop 句柄。
        /// </summary>
        AudioLoopHandle? PlayLoop(AudioSource source, AudioPlaybackOptions? options = null);

        /// <summary>
        ///     Plays a music-capable source and returns a controllable music handle when successful.
        ///     播放支持 music 的源，并在成功时返回可控制的 music 句柄。
        /// </summary>
        AudioMusicHandle? PlayMusic(AudioSource source, AudioPlaybackOptions? options = null);

        /// <summary>
        ///     Attaches an adaptive music plan that follows room/combat/victory lifecycle transitions.
        ///     附加跟随房间/战斗/胜利生命周期转换的自适应音乐计划。
        /// </summary>
        AudioAdaptiveMusicHandle FollowAdaptiveMusic(AudioAdaptiveMusicPlan plan);

        /// <summary>
        ///     Creates a caller-owned manual scope for grouped stop and cleanup.
        ///     创建调用方拥有的手动作用域，用于分组停止和清理。
        /// </summary>
        AudioScopeToken CreateManualScope(string name);

        /// <summary>
        ///     Stops everything attached to the provided scope token.
        ///     停止附加到所提供 scope token 的所有内容。
        /// </summary>
        bool StopScope(AudioScopeToken scope, bool allowFadeOut = true);

        /// <summary>
        ///     Stops the current owner of a named channel.
        ///     停止命名通道的当前所有者。
        /// </summary>
        bool StopChannel(string channel, bool allowFadeOut = true);

        /// <summary>
        ///     Stops every handle attached to a tag group.
        ///     停止附加到标签组的每个句柄。
        /// </summary>
        bool StopTag(string tag, bool allowFadeOut = true);
    }
}
