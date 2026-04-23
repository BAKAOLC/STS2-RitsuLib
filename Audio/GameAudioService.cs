using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Default implementation behind <see cref="GameFmod.Playback" />.
    /// </summary>
    public sealed class GameAudioService : IGameAudio
    {
        private GameAudioService()
        {
        }

        /// <summary>
        ///     Shared singleton instance.
        /// </summary>
        public static GameAudioService Shared { get; } = new();

        /// <inheritdoc />
        public AudioPlayResult Play(AudioSource source, AudioPlaybackOptions? options = null)
        {
            options ??= new();

            if (options.CooldownMs <= 0)
                return source switch
                {
                    StudioEventSource eventSource => PlayStudioEvent(eventSource, options),
                    StudioGuidSource guidSource => PlayStudioEventFromGuid(guidSource, options),
                    SoundFileSource fileSource => PlaySoundFile(fileSource, options),
                    StreamingMusicSource musicSource => PlayStreamingMusic(musicSource, options),
                    SnapshotSource snapshotSource => PlaySnapshot(snapshotSource, options),
                    _ => AudioPlayResult.Fail(AudioPlayStatus.InvalidSource),
                };
            var cooldownKey = options.DebugName ?? source.ToString() ?? source.GetType().Name;
            if (!FmodPlaybackThrottle.TryEnter(cooldownKey, options.CooldownMs))
                return AudioPlayResult.Fail(AudioPlayStatus.SkippedCooldown);

            return source switch
            {
                StudioEventSource eventSource => PlayStudioEvent(eventSource, options),
                StudioGuidSource guidSource => PlayStudioEventFromGuid(guidSource, options),
                SoundFileSource fileSource => PlaySoundFile(fileSource, options),
                StreamingMusicSource musicSource => PlayStreamingMusic(musicSource, options),
                SnapshotSource snapshotSource => PlaySnapshot(snapshotSource, options),
                _ => AudioPlayResult.Fail(AudioPlayStatus.InvalidSource),
            };
        }

        /// <inheritdoc />
        public AudioPlayResult PlayOneShot(AudioSource source, AudioPlaybackOptions? options = null)
        {
            options ??= new();

            return source switch
            {
                StudioEventSource eventSource when options.UseVanillaRouting =>
                    PlayVanillaOneShot(eventSource, options),
                StudioEventSource eventSource => PlayStudioEvent(eventSource, options),
                StudioGuidSource guidSource => PlayStudioGuid(guidSource, options),
                SoundFileSource fileSource => PlaySoundFile(fileSource, options),
                _ => Play(source, options),
            };
        }

        /// <inheritdoc />
        public AudioLoopHandle? PlayLoop(AudioSource source, AudioPlaybackOptions? options = null)
        {
            options ??= new();
            var result = source switch
            {
                StudioEventSource eventSource => PlayStudioLoop(eventSource, options),
                StudioGuidSource guidSource => PlayStudioLoopFromGuid(guidSource, options),
                SoundFileSource fileSource => PlaySoundFile(fileSource, options),
                StreamingMusicSource musicSource => PlayStreamingMusic(musicSource, options),
                _ => AudioPlayResult.Fail(AudioPlayStatus.NotSupported),
            };

            return result.Handle as AudioLoopHandle;
        }

        /// <inheritdoc />
        public AudioMusicHandle? PlayMusic(AudioSource source, AudioPlaybackOptions? options = null)
        {
            options ??= new();
            var result = source switch
            {
                StudioEventSource eventSource when options.UseVanillaRouting => PlayVanillaMusic(eventSource, options),
                StudioEventSource eventSource => PlayStudioEvent(eventSource, options, true),
                StudioGuidSource guidSource => PlayStudioEventFromGuid(guidSource, options, true),
                StreamingMusicSource musicSource => PlayStreamingMusic(musicSource, options, true),
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

        private static AudioPlayResult PlayVanillaOneShot(StudioEventSource source, AudioPlaybackOptions options)
        {
            if (options.GetParameters().Count == 0)
                GameFmod.Studio.PlayOneShot(source.Path, options.Volume);
            else
                GameFmod.Studio.PlayOneShot(source.Path, options.GetParameters(), options.Volume);

            return AudioPlayResult.Started();
        }

        private static AudioPlayResult PlayVanillaMusic(StudioEventSource source, AudioPlaybackOptions options)
        {
            GameFmod.Studio.PlayMusic(source.Path);
            return AudioPlayResult.Started();
        }

        private static AudioPlayResult PlayStudioGuid(StudioGuidSource source, AudioPlaybackOptions options)
        {
            return !FmodStudioDirectOneShots.TryPlayUsingGuid(source.Value)
                ? AudioPlayResult.Fail(AudioPlayStatus.Failed)
                : AudioPlayResult.Started();
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
            if (!TryApplyRouting(handle, options))
            {
                handle.Dispose();
                return AudioPlayResult.Fail(AudioPlayStatus.Failed, "Channel already occupied.");
            }

            handle.TrySetVolume(options.Volume);
            foreach (var parameter in options.GetParameters())
                handle.TrySetParameter(parameter.Key, parameter.Value);
            if (options.UsesLoopParameter)
                handle.TrySetParameter("loop", 0);
            if (options.StartPaused)
                handle.TryPause();
            if (options.AutoPlay)
                handle.TryPlay();

            AudioLifecycleRegistry.Shared.Attach(handle, options);
            return AudioPlayResult.Started(handle);
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

            if (!TryApplyRouting(handle, options))
            {
                handle.Dispose();
                return AudioPlayResult.Fail(AudioPlayStatus.Failed, "Channel already occupied.");
            }

            handle.TrySetVolume(options.Volume);
            foreach (var parameter in options.GetParameters())
                handle.TrySetParameter(parameter.Key, parameter.Value);
            if (options.StartPaused)
                handle.TryPause();
            if (options.AutoPlay)
                handle.TryPlay();

            AudioLifecycleRegistry.Shared.Attach(handle, options);
            return AudioPlayResult.Started(handle);
        }

        private static AudioPlayResult PlaySoundFile(SoundFileSource source, AudioPlaybackOptions options)
        {
            var instance = FmodStudioStreamingFiles.TryCreateSoundInstance(source.AbsolutePath);
            if (instance is null)
                return AudioPlayResult.Fail(AudioPlayStatus.MissingInstance);

            var handle = new AudioFileHandle(source, ResolveScope(options), instance);
            if (!TryApplyRouting(handle, options))
            {
                handle.Dispose();
                return AudioPlayResult.Fail(AudioPlayStatus.Failed, "Channel already occupied.");
            }

            handle.TrySetVolume(options.Volume);
            handle.TrySetPitch(options.Pitch);
            if (options.StartPaused)
                handle.TryPause();
            if (options.AutoPlay)
                handle.TryPlay();

            AudioLifecycleRegistry.Shared.Attach(handle, options);
            return AudioPlayResult.Started(handle);
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

            if (!TryApplyRouting(handle, options))
            {
                handle.Dispose();
                return AudioPlayResult.Fail(AudioPlayStatus.Failed, "Channel already occupied.");
            }

            handle.TrySetVolume(options.Volume);
            handle.TrySetPitch(options.Pitch);
            if (options.StartPaused)
                handle.TryPause();
            if (options.AutoPlay)
                handle.TryPlay();

            AudioLifecycleRegistry.Shared.Attach(handle, options);
            return AudioPlayResult.Started(handle);
        }

        private static AudioPlayResult PlaySnapshot(SnapshotSource source, AudioPlaybackOptions options)
        {
            var instance = FmodStudioSnapshots.TryStart(source.Path);
            if (instance is null)
                return AudioPlayResult.Fail(AudioPlayStatus.MissingInstance);

            var handle = new AudioSnapshotHandle(source, ResolveScope(options), instance);
            if (!TryApplyRouting(handle, options))
            {
                handle.Dispose();
                return AudioPlayResult.Fail(AudioPlayStatus.Failed, "Channel already occupied.");
            }

            AudioLifecycleRegistry.Shared.Attach(handle, options);
            return AudioPlayResult.Started(handle);
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
            if (routing.ReplaceTaggedGroup)
                AudioChannelRegistry.Shared.StopTag(routing.Tag, routing.AllowFadeOutOnReplace);

            AudioChannelRegistry.Shared.AttachTag(routing.Tag, handle);

            return string.IsNullOrWhiteSpace(routing.Channel) ||
                   AudioChannelRegistry.Shared.TryClaimChannel(routing.Channel, handle, routing.ChannelMode,
                       routing.AllowFadeOutOnReplace);
        }

        private static AudioLifecycleScope ResolveScope(AudioPlaybackOptions options)
        {
            return options.ScopeToken?.Scope ?? options.Scope;
        }
    }
}
