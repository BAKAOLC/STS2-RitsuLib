using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Implements <see cref="GameFmod.Playback" /> with typed handles, playback options, routing, and
    ///         lifecycle ownership.
    ///     </para>
    ///     <para xml:lang="zh-CN">使用类型化句柄、播放选项、路由和生命周期归属实现 <see cref="GameFmod.Playback" />。</para>
    /// </summary>
    public sealed class GameAudioService : IGameAudio
    {
        private GameAudioService()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the shared playback service.</para>
        ///     <para xml:lang="zh-CN">获取共享播放服务。</para>
        /// </summary>
        public static GameAudioService Shared { get; } = new();

        /// <inheritdoc />
        public AudioPlayResult Play(AudioSource source, AudioPlaybackOptions? options = null)
        {
            options ??= new();
            if (options.ScopeToken is { IsClosing: true })
                return AudioPlayResult.Fail(AudioPlayStatus.Failed, "The audio scope token is disposed.");
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!TryEnterCooldown(source, options))
                return AudioPlayResult.Fail(AudioPlayStatus.SkippedCooldown);

            return PlayCore(source, options);
        }

        /// <inheritdoc />
        public AudioPlayResult PlayOneShot(AudioSource source, AudioPlaybackOptions? options = null)
        {
            options ??= new();
            if (options.ScopeToken is { IsClosing: true })
                return AudioPlayResult.Fail(AudioPlayStatus.Failed, "The audio scope token is disposed.");
            if (!TryEnterCooldown(source, options))
                return AudioPlayResult.Fail(AudioPlayStatus.SkippedCooldown);

            return source switch
            {
                StudioEventSource eventSource when options.UseVanillaRouting =>
                    PlayVanillaOneShot(eventSource, options),
                StudioEventSource eventSource => PlayStudioEvent(eventSource, options),
                StudioGuidSource guidSource => PlayStudioGuid(guidSource, options),
                SoundFileSource fileSource => PlaySoundFile(fileSource, options),
                ResourceSoundFileSource resourceFileSource => PlayResourceSoundFile(resourceFileSource, options),
                _ => AudioPlayResult.Fail(AudioPlayStatus.NotSupported),
            };
        }

        /// <inheritdoc />
        public AudioLoopHandle? PlayLoop(AudioSource source, AudioPlaybackOptions? options = null)
        {
            options ??= new();
            if (options.ScopeToken is { IsClosing: true })
                return null;
            if (!TryEnterCooldown(source, options))
                return null;

            var result = source switch
            {
                StudioEventSource eventSource => PlayStudioLoop(eventSource, options),
                StudioGuidSource guidSource => PlayStudioLoopFromGuid(guidSource, options),
                StreamingMusicSource musicSource => PlayStreamingMusic(musicSource, options),
                StreamingResourceMusicSource resourceMusicSource => PlayStreamingResourceMusic(resourceMusicSource,
                    options),
                _ => AudioPlayResult.Fail(AudioPlayStatus.NotSupported),
            };

            return result.Handle as AudioLoopHandle;
        }

        /// <inheritdoc />
        public AudioMusicHandle? PlayMusic(AudioSource source, AudioPlaybackOptions? options = null)
        {
            options ??= new();
            if (options.ScopeToken is { IsClosing: true })
                return null;
            if (!TryEnterCooldown(source, options))
                return null;

            var result = source switch
            {
                StudioEventSource eventSource => PlayStudioEvent(eventSource, options, true),
                StudioGuidSource guidSource => PlayStudioEventFromGuid(guidSource, options, true),
                StreamingMusicSource musicSource => PlayStreamingMusic(musicSource, options, true),
                StreamingResourceMusicSource resourceMusicSource => PlayStreamingResourceMusic(resourceMusicSource,
                    options, true),
                _ => AudioPlayResult.Fail(AudioPlayStatus.NotSupported),
            };

            return result.Handle as AudioMusicHandle;
        }

        /// <inheritdoc />
        public AudioAdaptiveMusicHandle FollowAdaptiveMusic(AudioAdaptiveMusicPlan plan)
        {
            return AudioAdaptiveMusicDirector.Shared.Attach(plan);
        }

        /// <inheritdoc />
        public AudioScopeToken CreateManualScope(string name)
        {
            return new(name, AudioLifecycleScope.Manual);
        }

        /// <inheritdoc />
        public bool StopScope(AudioScopeToken scope, bool allowFadeOut = true)
        {
            return AudioLifecycleRegistry.Shared.StopScope(scope, allowFadeOut);
        }

        /// <inheritdoc />
        public bool StopChannel(string channel, bool allowFadeOut = true)
        {
            return AudioChannelRegistry.Shared.StopChannel(channel, allowFadeOut);
        }

        /// <inheritdoc />
        public bool StopTag(string tag, bool allowFadeOut = true)
        {
            return AudioChannelRegistry.Shared.StopTag(tag, allowFadeOut);
        }

        private static AudioPlayResult PlayCore(AudioSource source, AudioPlaybackOptions options)
        {
            return source switch
            {
                StudioEventSource eventSource => PlayStudioEvent(eventSource, options),
                StudioGuidSource guidSource => PlayStudioEventFromGuid(guidSource, options),
                SoundFileSource fileSource => PlaySoundFile(fileSource, options),
                ResourceSoundFileSource resourceFileSource => PlayResourceSoundFile(resourceFileSource, options),
                StreamingMusicSource musicSource => PlayStreamingMusic(musicSource, options),
                StreamingResourceMusicSource resourceMusicSource => PlayStreamingResourceMusic(resourceMusicSource,
                    options),
                SnapshotSource snapshotSource => PlaySnapshot(snapshotSource, options),
                _ => AudioPlayResult.Fail(AudioPlayStatus.InvalidSource),
            };
        }

        private static AudioPlayResult PlayVanillaOneShot(StudioEventSource source, AudioPlaybackOptions options)
        {
            var started = options.GetParameters().Count == 0
                ? GameFmodAudioService.Shared.TryPlayOneShot(source.Path, options.Volume)
                : GameFmodAudioService.Shared.TryPlayOneShot(source.Path, options.GetParameters(), options.Volume);
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!started)
                return AudioPlayResult.Fail(AudioPlayStatus.MissingManager);

            return AudioPlayResult.Started();
        }

        private static AudioPlayResult PlayStudioGuid(StudioGuidSource source, AudioPlaybackOptions options)
        {
            return PlayStudioEventFromGuid(source, options);
        }

        private static AudioPlayResult PlayStudioLoop(StudioEventSource source, AudioPlaybackOptions options)
        {
            var instance = FmodStudioEventInstances.TryCreate(source.Path);
            return instance is null
                ? AudioPlayResult.Fail(AudioPlayStatus.MissingInstance)
                : AttachStudioLoop(source, instance, options);
        }

        private static AudioPlayResult PlayStudioLoopFromGuid(StudioGuidSource source, AudioPlaybackOptions options)
        {
            var instance = FmodStudioEventInstances.TryCreateFromGuid(source.Value);
            return instance is null
                ? AudioPlayResult.Fail(AudioPlayStatus.MissingInstance)
                : AttachStudioLoop(source, instance, options);
        }

        private static AudioPlayResult AttachStudioLoop(AudioSource source, GodotObject instance,
            AudioPlaybackOptions options)
        {
            var handle = new AudioLoopHandle(source, ResolveScope(options), instance);
            return AttachAndConfigure(handle, options, options.UsesLoopParameter);
        }

        private static AudioPlayResult PlayStudioEvent(StudioEventSource source, AudioPlaybackOptions options,
            bool asMusic = false)
        {
            var instance = FmodStudioEventInstances.TryCreate(source.Path);
            return instance is null
                ? AudioPlayResult.Fail(AudioPlayStatus.MissingInstance)
                : AttachStudioPlayback(source, instance, options, asMusic);
        }

        private static AudioPlayResult PlayStudioEventFromGuid(StudioGuidSource source, AudioPlaybackOptions options,
            bool asMusic = false)
        {
            var instance = FmodStudioEventInstances.TryCreateFromGuid(source.Value);
            return instance is null
                ? AudioPlayResult.Fail(AudioPlayStatus.MissingInstance)
                : AttachStudioPlayback(source, instance, options, asMusic);
        }

        private static AudioPlayResult AttachStudioPlayback(AudioSource source, GodotObject instance,
            AudioPlaybackOptions options, bool asMusic)
        {
            AudioHandleBase handle = asMusic
                ? new AudioMusicHandle(source, ResolveScope(options), instance)
                : new AudioEventHandle(source, ResolveScope(options), instance);

            return AttachAndConfigure(handle, options);
        }

        private static AudioPlayResult PlaySoundFile(SoundFileSource source, AudioPlaybackOptions options)
        {
            var instance = FmodStudioStreamingFiles.TryCreateSoundInstance(source.AbsolutePath);
            if (instance is null)
                return AudioPlayResult.Fail(AudioPlayStatus.MissingInstance);

            var handle = new AudioFileHandle(source, ResolveScope(options), instance);
            return AttachAndConfigure(handle, options);
        }

        private static AudioPlayResult PlayResourceSoundFile(ResourceSoundFileSource source,
            AudioPlaybackOptions options)
        {
            var instance = FmodStudioStreamingFiles.TryCreateResourceSoundInstance(source.ResourcePath);
            if (instance is null)
                return AudioPlayResult.Fail(AudioPlayStatus.MissingInstance);

            var handle = new AudioFileHandle(source, ResolveScope(options), instance);
            return AttachAndConfigure(handle, options);
        }

        private static AudioPlayResult PlayStreamingMusic(StreamingMusicSource source, AudioPlaybackOptions options,
            bool asMusic = false)
        {
            var instance = FmodStudioStreamingFiles.TryCreateStreamingMusicInstance(source.AbsolutePath);
            if (instance is null)
                return AudioPlayResult.Fail(AudioPlayStatus.MissingInstance);

            AudioHandleBase handle = asMusic
                ? new AudioMusicHandle(source, ResolveScope(options), instance)
                : new AudioLoopHandle(source, ResolveScope(options), instance);

            return AttachAndConfigure(handle, options);
        }

        private static AudioPlayResult PlayStreamingResourceMusic(StreamingResourceMusicSource source,
            AudioPlaybackOptions options, bool asMusic = false)
        {
            var instance = FmodStudioStreamingFiles.TryCreateResourceStreamingMusicInstance(source.ResourcePath);
            if (instance is null)
                return AudioPlayResult.Fail(AudioPlayStatus.MissingInstance);

            AudioHandleBase handle = asMusic
                ? new AudioMusicHandle(source, ResolveScope(options), instance)
                : new AudioLoopHandle(source, ResolveScope(options), instance);

            return AttachAndConfigure(handle, options);
        }

        private static AudioPlayResult PlaySnapshot(SnapshotSource source, AudioPlaybackOptions options)
        {
            var instance = FmodStudioEventInstances.TryCreate(source.Path);
            if (instance is null)
                return AudioPlayResult.Fail(AudioPlayStatus.MissingInstance);

            var handle = new AudioSnapshotHandle(source, ResolveScope(options), instance);
            return AttachAndConfigure(handle, options);
        }

        private static AudioPlayResult AttachAndConfigure(
            AudioHandleBase handle,
            AudioPlaybackOptions options,
            bool applyLoopParameter = false)
        {
            handle.AllowFadeOutOnStop = options.AllowFadeOutOnStop;
            if (!AudioLifecycleRegistry.Shared.TryAttach(handle, options))
                return AudioPlayResult.Fail(AudioPlayStatus.Failed, "The audio scope token is disposed.");

            if (!TryApplyRouting(handle, options))
                return FailHandle(handle, "Audio routing could not be applied.");
            if (!handle.TrySetVolume(options.Volume))
                return FailHandle(handle, "The initial volume could not be applied.");
            if (!handle.TrySetPitch(options.Pitch))
                return FailHandle(handle, "The initial pitch could not be applied.");

            foreach (var parameter in options.GetParameters())
                if (!handle.TrySetParameter(parameter.Key, parameter.Value))
                    return FailHandle(handle, $"The initial parameter '{parameter.Key}' could not be applied.");

            if (applyLoopParameter && !handle.TrySetParameter("loop", 0))
                return FailHandle(handle, "The initial loop parameter could not be applied.");
            if (options.AutoPlay && !handle.TryPlay())
                return FailHandle(handle, "Playback could not be started.");
            if (options.StartPaused && !handle.TryPause())
                return FailHandle(handle, "The initial paused state could not be applied.");

            return AudioPlayResult.Started(handle);
        }

        private static AudioPlayResult FailHandle(AudioHandleBase handle, string message)
        {
            handle.Dispose();
            return AudioPlayResult.Fail(AudioPlayStatus.Failed, message);
        }

        private static bool TryApplyRouting(IAudioHandle handle, AudioPlaybackOptions options)
        {
            var routing = options.Routing;
            if (routing is null)
                return true;

            if (string.IsNullOrWhiteSpace(routing.Tag))
                return string.IsNullOrWhiteSpace(routing.Channel) ||
                       AudioChannelRegistry.Shared.TryClaimChannel(routing.Channel, handle, routing.ChannelMode,
                           routing.AllowFadeOutOnReplace);
            if (routing.ReplaceTaggedGroup &&
                !AudioChannelRegistry.Shared.TryClearTag(routing.Tag, routing.AllowFadeOutOnReplace))
                return false;

            AudioChannelRegistry.Shared.AttachTag(routing.Tag, handle);

            return string.IsNullOrWhiteSpace(routing.Channel) ||
                   AudioChannelRegistry.Shared.TryClaimChannel(routing.Channel, handle, routing.ChannelMode,
                       routing.AllowFadeOutOnReplace);
        }

        private static AudioLifecycleScope ResolveScope(AudioPlaybackOptions options)
        {
            return options.ScopeToken?.Scope ?? options.Scope;
        }

        private static bool TryEnterCooldown(AudioSource source, AudioPlaybackOptions options)
        {
            if (options.CooldownMs <= 0 || source is null)
                return true;

            var cooldownKey = options.DebugName ?? source.ToString() ?? source.GetType().Name;
            return FmodPlaybackThrottle.TryEnter(cooldownKey, options.CooldownMs);
        }
    }
}
