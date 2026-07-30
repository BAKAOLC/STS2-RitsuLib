namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides typed playback, handle-based control, lifecycle ownership, routing, and adaptive
    ///         music.
    ///     </para>
    ///     <para xml:lang="zh-CN">提供类型化播放、基于句柄的控制、生命周期归属、路由和自适应音乐。</para>
    /// </summary>
    public interface IGameAudio
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Dispatches a supported source and reports whether backend creation and requested initialization
        ///         completed.
        ///     </para>
        ///     <para xml:lang="zh-CN">分派受支持的音频源，并报告后端创建及所请求的初始化是否完成。</para>
        /// </summary>
        /// <param name="source">
        ///     <para xml:lang="en">The typed audio source to play.</para>
        ///     <para xml:lang="zh-CN">要播放的类型化音频源。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">Optional initial values, lifecycle ownership, cooldown, and routing rules.</para>
        ///     <para xml:lang="zh-CN">可选的初始值、生命周期归属、冷却和路由规则。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The structured playback result, including a controllable handle when that route creates one.</para>
        ///     <para xml:lang="zh-CN">结构化播放结果；所选路径创建可控制句柄时，结果中会包含该句柄。</para>
        /// </returns>
        AudioPlayResult Play(AudioSource source, AudioPlaybackOptions? options = null);

        /// <summary>
        ///     <para xml:lang="en">Plays a Studio event or fully loaded sound-file source once.</para>
        ///     <para xml:lang="zh-CN">单次播放 Studio 事件或完整加载的声音文件源。</para>
        /// </summary>
        /// <param name="source">
        ///     <para xml:lang="en">A Studio path, Studio GUID, absolute sound file, or Godot-resource sound file.</para>
        ///     <para xml:lang="zh-CN">Studio 路径、Studio GUID、绝对路径声音文件或 Godot 资源声音文件。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">
        ///         Optional playback settings. A path-based Studio event uses the native manager when
        ///         <see cref="AudioPlaybackOptions.UseVanillaRouting" /> is enabled.
        ///     </para>
        ///     <para xml:lang="zh-CN">可选的播放设置。启用 <see cref="AudioPlaybackOptions.UseVanillaRouting" /> 时，路径型 Studio 事件使用原生管理器。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The playback result; unsupported source kinds return
        ///         <see cref="AudioPlayStatus.NotSupported" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">播放结果；不受支持的音频源类型返回 <see cref="AudioPlayStatus.NotSupported" />。</para>
        /// </returns>
        AudioPlayResult PlayOneShot(AudioSource source, AudioPlaybackOptions? options = null);

        /// <summary>
        ///     <para xml:lang="en">Plays a Studio event or streaming-music source as a controllable loop.</para>
        ///     <para xml:lang="zh-CN">将 Studio 事件或流式音乐源作为可控制循环播放。</para>
        /// </summary>
        /// <param name="source">
        ///     <para xml:lang="en">The loop-capable source.</para>
        ///     <para xml:lang="zh-CN">支持循环的音频源。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">Optional initial values, lifecycle ownership, cooldown, and routing rules.</para>
        ///     <para xml:lang="zh-CN">可选的初始值、生命周期归属、冷却和路由规则。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The initialized loop handle, or null when the source is unsupported or playback fails.</para>
        ///     <para xml:lang="zh-CN">已初始化的循环句柄；音频源不受支持或播放失败时为 <see langword="null" />。</para>
        /// </returns>
        AudioLoopHandle? PlayLoop(AudioSource source, AudioPlaybackOptions? options = null);

        /// <summary>
        ///     <para xml:lang="en">Plays a Studio event or streaming-music source through a controllable music handle.</para>
        ///     <para xml:lang="zh-CN">通过可控制的音乐句柄播放 Studio 事件或流式音乐源。</para>
        /// </summary>
        /// <param name="source">
        ///     <para xml:lang="en">The music-capable source.</para>
        ///     <para xml:lang="zh-CN">支持音乐播放的音频源。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">Optional initial values, lifecycle ownership, cooldown, and routing rules.</para>
        ///     <para xml:lang="zh-CN">可选的初始值、生命周期归属、冷却和路由规则。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The initialized music handle, or null when the source is unsupported or playback fails.</para>
        ///     <para xml:lang="zh-CN">已初始化的音乐句柄；音频源不受支持或播放失败时为 <see langword="null" />。</para>
        /// </returns>
        AudioMusicHandle? PlayMusic(AudioSource source, AudioPlaybackOptions? options = null);

        /// <summary>
        ///     <para xml:lang="en">Attaches an adaptive music plan to room, combat, and victory lifecycle transitions.</para>
        ///     <para xml:lang="zh-CN">将自适应音乐方案附加到房间、战斗和胜利生命周期转换。</para>
        /// </summary>
        /// <param name="plan">
        ///     <para xml:lang="en">The adaptive music plan to follow.</para>
        ///     <para xml:lang="zh-CN">要遵循的自适应音乐方案。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A caller-owned handle that controls the attached plan.</para>
        ///     <para xml:lang="zh-CN">用于控制已附加方案、由调用方持有的句柄。</para>
        /// </returns>
        AudioAdaptiveMusicHandle FollowAdaptiveMusic(AudioAdaptiveMusicPlan plan);

        /// <summary>
        ///     <para xml:lang="en">Creates a caller-owned token for grouping playback and retryable cleanup.</para>
        ///     <para xml:lang="zh-CN">创建由调用方持有的令牌，用于对播放分组并执行可重试清理。</para>
        /// </summary>
        /// <param name="name">
        ///     <para xml:lang="en">The display name recorded on the token.</para>
        ///     <para xml:lang="zh-CN">记录在令牌上的显示名称。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A new manual lifecycle token.</para>
        ///     <para xml:lang="zh-CN">新的手动生命周期令牌。</para>
        /// </returns>
        AudioScopeToken CreateManualScope(string name);

        /// <summary>
        ///     <para xml:lang="en">Attempts to stop and release every handle attached to a manual scope token.</para>
        ///     <para xml:lang="zh-CN">尝试停止并释放附加到手动作用域令牌的所有句柄。</para>
        /// </summary>
        /// <param name="scope">
        ///     <para xml:lang="en">The manual scope token to clean up.</para>
        ///     <para xml:lang="zh-CN">要清理的手动作用域令牌。</para>
        /// </param>
        /// <param name="allowFadeOut">
        ///     <para xml:lang="en">Whether stopped event handles may fade out.</para>
        ///     <para xml:lang="zh-CN">停止事件句柄时是否允许淡出。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when at least one handle was found and every release completed;
        ///         otherwise <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">找到至少一个句柄且所有释放均已完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        bool StopScope(AudioScopeToken scope, bool allowFadeOut = true);

        /// <summary>
        ///     <para xml:lang="en">Attempts to stop and release the current owner of a named channel.</para>
        ///     <para xml:lang="zh-CN">尝试停止并释放命名通道的当前占用者。</para>
        /// </summary>
        /// <param name="channel">
        ///     <para xml:lang="en">The case-sensitive channel name.</para>
        ///     <para xml:lang="zh-CN">区分大小写的通道名称。</para>
        /// </param>
        /// <param name="allowFadeOut">
        ///     <para xml:lang="en">Whether the stopped event handle may fade out.</para>
        ///     <para xml:lang="zh-CN">停止事件句柄时是否允许淡出。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when an owner was found and released; otherwise
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">找到并释放占用者时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        bool StopChannel(string channel, bool allowFadeOut = true);

        /// <summary>
        ///     <para xml:lang="en">Attempts to stop and release every handle attached to a tag group.</para>
        ///     <para xml:lang="zh-CN">尝试停止并释放附加到标签组的所有句柄。</para>
        /// </summary>
        /// <param name="tag">
        ///     <para xml:lang="en">The case-sensitive tag name.</para>
        ///     <para xml:lang="zh-CN">区分大小写的标签名称。</para>
        /// </param>
        /// <param name="allowFadeOut">
        ///     <para xml:lang="en">Whether stopped event handles may fade out.</para>
        ///     <para xml:lang="zh-CN">停止事件句柄时是否允许淡出。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when at least one handle was found and every release completed;
        ///         otherwise <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">找到至少一个句柄且所有释放均已完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        bool StopTag(string tag, bool allowFadeOut = true);
    }
}
