namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     High-level audio API for typed playback, handle-based control, lifecycle scopes, and adaptive music flows.
    /// </summary>
    public interface IGameAudio
    {
        /// <summary>
        ///     Plays any supported source and returns a structured result.
        /// </summary>
        AudioPlayResult Play(AudioSource source, AudioPlaybackOptions? options = null);

        /// <summary>
        ///     Plays a source as a one-shot when supported.
        /// </summary>
        AudioPlayResult PlayOneShot(AudioSource source, AudioPlaybackOptions? options = null);

        /// <summary>
        ///     Plays a loop-capable source and returns a controllable loop handle when successful.
        /// </summary>
        AudioLoopHandle? PlayLoop(AudioSource source, AudioPlaybackOptions? options = null);

        /// <summary>
        ///     Plays a music-capable source and returns a controllable music handle when successful.
        /// </summary>
        AudioMusicHandle? PlayMusic(AudioSource source, AudioPlaybackOptions? options = null);

        /// <summary>
        ///     Attaches an adaptive music plan that follows room/combat/victory lifecycle transitions.
        /// </summary>
        AudioAdaptiveMusicHandle FollowAdaptiveMusic(AudioAdaptiveMusicPlan plan);

        /// <summary>
        ///     Creates a caller-owned manual scope for grouped stop and cleanup.
        /// </summary>
        AudioScopeToken CreateManualScope(string name);

        /// <summary>
        ///     Stops everything attached to the provided scope token.
        /// </summary>
        bool StopScope(AudioScopeToken scope, bool allowFadeOut = true);

        /// <summary>
        ///     Stops the current owner of a named channel.
        /// </summary>
        bool StopChannel(string channel, bool allowFadeOut = true);

        /// <summary>
        ///     Stops every handle attached to a tag group.
        /// </summary>
        bool StopTag(string tag, bool allowFadeOut = true);
    }
}
