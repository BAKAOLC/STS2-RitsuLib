namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Lightweight event-path mapping for audio resources that are not authored in an FMOD Studio bank. This is a
    ///     compatibility layer: it reuses FMOD file playback and RitsuLib lifecycle routing, but it is not a native FMOD
    ///     Studio event and does not provide DSP graph membership or dynamic bus routing.
    ///     非 FMOD Studio bank 音频资源的轻量 event-path 映射。这是兼容层：复用 FMOD 文件播放和 RitsuLib
    ///     生命周期路由，但它不是 native FMOD Studio event，也不提供 DSP 图成员关系或动态 bus 路由。
    /// </summary>
    public static class VirtualFmodEventRegistry
    {
        private static readonly Lock Gate = new();

        private static readonly Dictionary<string, VirtualFmodEventDefinition>
            Definitions = new(StringComparer.Ordinal);

        private static readonly Dictionary<string, int> VariantIndexes = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Queue<IAudioHandle>> Loops = new(StringComparer.Ordinal);
        private static IAudioHandle? _music;

        /// <summary>
        ///     Registers a one-shot event path backed by a Godot resource path.
        ///     注册由 Godot 资源路径支撑的 one-shot event path。
        /// </summary>
        public static void RegisterOneShot(string eventPath, string resourcePath,
            string busPath = FmodStudioRouting.SfxBus, float volume = 1f, float pitch = 1f)
        {
            Register(new(eventPath, resourcePath, VirtualFmodEventKind.OneShot, busPath, volume, pitch));
        }

        /// <summary>
        ///     Registers multiple one-shot event paths from an event-path-to-resource-path map.
        ///     通过 event path 到资源路径的映射批量注册 one-shot event path。
        /// </summary>
        public static void RegisterOneShots(IReadOnlyDictionary<string, string> eventResourcePaths,
            string busPath = FmodStudioRouting.SfxBus, float volume = 1f, float pitch = 1f)
        {
            ArgumentNullException.ThrowIfNull(eventResourcePaths);

            foreach (var (eventPath, resourcePath) in eventResourcePaths)
                RegisterOneShot(eventPath, resourcePath, busPath, volume, pitch);
        }

        /// <summary>
        ///     Registers a one-shot event path backed by multiple resource variants. Variants are only supported for
        ///     one-shot virtual events.
        ///     注册由多个资源变体支撑的 one-shot event path。资源变体仅支持 one-shot 虚拟 event。
        /// </summary>
        public static void RegisterOneShotVariants(string eventPath, IReadOnlyList<string> resourcePaths,
            VirtualFmodVariantSelection selection = VirtualFmodVariantSelection.Random,
            string busPath = FmodStudioRouting.SfxBus, float volume = 1f, float pitch = 1f)
        {
            var normalizedResourcePaths = ValidateResourcePaths(resourcePaths, nameof(resourcePaths));
            Register(new(eventPath, normalizedResourcePaths[0], VirtualFmodEventKind.OneShot, busPath, volume, pitch)
            {
                ResourcePaths = normalizedResourcePaths,
                VariantSelection = selection,
            });
        }

        /// <summary>
        ///     Registers multiple one-shot event paths from an event-path-to-resource-variants map.
        ///     通过 event path 到资源变体列表的映射批量注册 one-shot event path。
        /// </summary>
        public static void RegisterOneShotVariants(
            IReadOnlyDictionary<string, IReadOnlyList<string>> eventResourcePaths,
            VirtualFmodVariantSelection selection = VirtualFmodVariantSelection.Random,
            string busPath = FmodStudioRouting.SfxBus, float volume = 1f, float pitch = 1f)
        {
            ArgumentNullException.ThrowIfNull(eventResourcePaths);

            foreach (var (eventPath, resourcePaths) in eventResourcePaths)
                RegisterOneShotVariants(eventPath, resourcePaths, selection, busPath, volume, pitch);
        }

        /// <summary>
        ///     Registers a loop event path backed by a Godot resource path.
        ///     注册由 Godot 资源路径支撑的 loop event path。
        /// </summary>
        public static void RegisterLoop(string eventPath, string resourcePath,
            string busPath = FmodStudioRouting.SfxBus, float volume = 1f, float pitch = 1f, bool stream = true)
        {
            Register(new(eventPath, resourcePath, VirtualFmodEventKind.Loop, busPath, volume, pitch, stream));
        }

        /// <summary>
        ///     Registers a music event path backed by a Godot resource path.
        ///     注册由 Godot 资源路径支撑的 music event path。
        /// </summary>
        public static void RegisterMusic(string eventPath, string resourcePath,
            string busPath = FmodStudioRouting.MusicBus, float volume = 1f, float pitch = 1f)
        {
            Register(new(eventPath, resourcePath, VirtualFmodEventKind.Music, busPath, volume, pitch, true));
        }

        /// <summary>
        ///     Registers or replaces a virtual event definition.
        ///     注册或替换一个虚拟 event 定义。
        /// </summary>
        public static void Register(VirtualFmodEventDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            if (string.IsNullOrWhiteSpace(definition.EventPath))
                throw new ArgumentException("Virtual FMOD event path must be non-empty.", nameof(definition));
            if (string.IsNullOrWhiteSpace(definition.ResourcePath))
                throw new ArgumentException("Virtual FMOD event resource path must be non-empty.", nameof(definition));

            var resourcePaths = ValidateResourcePaths(definition.ResourcePaths, nameof(definition));
            if (definition.Kind != VirtualFmodEventKind.OneShot && resourcePaths.Length > 1)
                throw new ArgumentException("Only one-shot virtual FMOD events can use resource variants.",
                    nameof(definition));

            definition = definition with { ResourcePaths = resourcePaths };

            lock (Gate)
            {
                Definitions[definition.EventPath] = definition;
            }
        }

        /// <summary>
        ///     Removes a virtual event mapping.
        ///     移除一个虚拟 event 映射。
        /// </summary>
        public static bool Unregister(string eventPath)
        {
            lock (Gate)
            {
                VariantIndexes.Remove(eventPath);
                return Definitions.Remove(eventPath);
            }
        }

        /// <summary>
        ///     True when <paramref name="eventPath" /> is registered as a virtual event.
        ///     当 <paramref name="eventPath" /> 已注册为虚拟 event 时为 true。
        /// </summary>
        public static bool IsRegistered(string? eventPath)
        {
            if (string.IsNullOrWhiteSpace(eventPath))
                return false;

            lock (Gate)
            {
                return Definitions.ContainsKey(eventPath);
            }
        }

        internal static bool TryPlayOneShot(string eventPath, float volume,
            IReadOnlyDictionary<string, float>? parameters = null)
        {
            if (!TryGetDefinition(eventPath, out var definition))
                return false;

            if (definition.Kind != VirtualFmodEventKind.OneShot)
                return false;

            WarnIgnoredParameters(eventPath, parameters);
            var resourcePath = SelectResourcePath(definition);
            var result = GameFmod.Playback.PlayOneShot(
                AudioSource.ResourceFile(resourcePath),
                BuildOptions(definition, volume, null, AudioLifecycleScope.Manual));
            return result.Succeeded;
        }

        internal static bool TryPlayLoop(string eventPath)
        {
            if (!TryGetDefinition(eventPath, out var definition))
                return false;

            if (definition.Kind != VirtualFmodEventKind.Loop)
                return false;

            AudioSource source = definition.Stream
                ? AudioSource.StreamingResourceMusic(definition.ResourcePath)
                : AudioSource.ResourceFile(definition.ResourcePath);
            var result = GameFmod.Playback.Play(source,
                BuildOptions(definition, 1f, eventPath, AudioLifecycleScope.Room));
            if (!result.Succeeded || result.Handle is null)
                return false;

            lock (Gate)
            {
                if (!Loops.TryGetValue(eventPath, out var queue))
                {
                    queue = new();
                    Loops[eventPath] = queue;
                }

                queue.Enqueue(result.Handle);
            }

            return true;
        }

        internal static bool TryStopLoop(string eventPath)
        {
            IAudioHandle? handle = null;
            lock (Gate)
            {
                if (Loops.TryGetValue(eventPath, out var queue) && queue.Count > 0)
                {
                    handle = queue.Dequeue();
                    if (queue.Count == 0)
                        Loops.Remove(eventPath);
                }
            }

            if (handle is null)
                return false;

            handle.Dispose();
            return true;
        }

        internal static void StopAllLoops()
        {
            IAudioHandle[] handles;
            lock (Gate)
            {
                handles = Loops.Values.SelectMany(static q => q).ToArray();
                Loops.Clear();
            }

            foreach (var handle in handles)
                handle.Dispose();
        }

        internal static bool TryPlayMusic(string eventPath)
        {
            if (!TryGetDefinition(eventPath, out var definition))
                return false;

            if (definition.Kind != VirtualFmodEventKind.Music)
                return false;

            StopMusic();
            var result = GameFmod.Playback.PlayMusic(
                AudioSource.StreamingResourceMusic(definition.ResourcePath),
                BuildOptions(definition, 1f, "virtual-music", AudioLifecycleScope.Run));
            if (result is not { IsValid: true })
                return false;

            lock (Gate)
            {
                _music = result;
            }

            return true;
        }

        internal static void StopMusic()
        {
            IAudioHandle? music;
            lock (Gate)
            {
                music = _music;
                _music = null;
            }

            music?.Dispose();
        }

        internal static bool HasActiveMusic()
        {
            lock (Gate)
            {
                return _music is { IsValid: true };
            }
        }

        internal static bool TrySetParameter(string eventPath, string parameter, float value)
        {
            _ = parameter;
            _ = value;
            return IsRegistered(eventPath);
        }

        private static bool TryGetDefinition(string eventPath, out VirtualFmodEventDefinition definition)
        {
            lock (Gate)
            {
                return Definitions.TryGetValue(eventPath, out definition!);
            }
        }

        private static string SelectResourcePath(VirtualFmodEventDefinition definition)
        {
            if (definition.ResourcePaths.Count == 1)
                return definition.ResourcePaths[0];

            var index = definition.VariantSelection switch
            {
                VirtualFmodVariantSelection.Random => Random.Shared.Next(definition.ResourcePaths.Count),
                VirtualFmodVariantSelection.RoundRobin => NextRoundRobinIndex(definition.EventPath,
                    definition.ResourcePaths.Count),
                _ => 0,
            };
            return definition.ResourcePaths[index];
        }

        private static int NextRoundRobinIndex(string eventPath, int count)
        {
            lock (Gate)
            {
                var index = VariantIndexes.GetValueOrDefault(eventPath, 0);

                VariantIndexes[eventPath] = (index + 1) % count;
                return index;
            }
        }

        private static string[] ValidateResourcePaths(IReadOnlyList<string>? resourcePaths, string parameterName)
        {
            if (resourcePaths is null || resourcePaths.Count == 0)
                throw new ArgumentException("Virtual FMOD event resource paths must be non-empty.", parameterName);

            var result = new string[resourcePaths.Count];
            for (var i = 0; i < resourcePaths.Count; i++)
            {
                var resourcePath = resourcePaths[i];
                if (string.IsNullOrWhiteSpace(resourcePath))
                    throw new ArgumentException("Virtual FMOD event resource paths must be non-empty.",
                        parameterName);

                result[i] = resourcePath;
            }

            return result;
        }

        private static AudioPlaybackOptions BuildOptions(VirtualFmodEventDefinition definition, float callVolume,
            string? channel, AudioLifecycleScope scope)
        {
            var volume = definition.Volume * callVolume * ResolveBusVolume(definition.BusPath);
            return new()
            {
                Volume = volume,
                Pitch = definition.Pitch,
                Scope = scope,
                Routing = string.IsNullOrWhiteSpace(channel)
                    ? null
                    : new AudioRoutingOptions { Channel = channel, ChannelMode = AudioChannelMode.ReplaceExisting },
            };
        }

        private static float ResolveBusVolume(string? busPath)
        {
            if (string.IsNullOrWhiteSpace(busPath))
                return 1f;

            return FmodStudioServer.TryCheckBusPath(busPath) == true
                ? Math.Max(0f, FmodStudioBusAccess.TryGetVolume(busPath))
                : 1f;
        }

        private static void WarnIgnoredParameters(string eventPath, IReadOnlyDictionary<string, float>? parameters)
        {
            if (parameters is not { Count: > 0 })
                return;

            RitsuLibFramework.Logger.Warn(
                $"[Audio] virtual FMOD event ignores Studio parameters: {eventPath} ({parameters.Count} parameter(s)).");
        }
    }

    /// <summary>
    ///     Definition for a virtual FMOD event backed by a Godot audio resource.
    ///     由 Godot 音频资源支撑的虚拟 FMOD event 定义。
    /// </summary>
    public sealed record VirtualFmodEventDefinition(
        string EventPath,
        string ResourcePath,
        VirtualFmodEventKind Kind,
        string BusPath = FmodStudioRouting.SfxBus,
        float Volume = 1f,
        float Pitch = 1f,
        bool Stream = false)
    {
        /// <summary>
        ///     Resource variants used by one-shot virtual events. Loop and music virtual events must use one resource.
        ///     one-shot 虚拟 event 使用的资源变体。loop 和 music 虚拟 event 必须只使用一个资源。
        /// </summary>
        public IReadOnlyList<string> ResourcePaths { get; init; } = [ResourcePath];

        /// <summary>
        ///     Selection mode for one-shot resource variants.
        ///     one-shot 资源变体的选择模式。
        /// </summary>
        public VirtualFmodVariantSelection VariantSelection { get; init; } = VirtualFmodVariantSelection.Random;
    }

    /// <summary>
    ///     Variant selection mode for one-shot virtual FMOD events.
    ///     one-shot 虚拟 FMOD event 的资源变体选择模式。
    /// </summary>
    public enum VirtualFmodVariantSelection
    {
        /// <summary>
        ///     Select a random resource each time the event is played.
        ///     每次播放 event 时随机选择资源。
        /// </summary>
        Random,

        /// <summary>
        ///     Cycle through resources in registration order.
        ///     按注册顺序轮询资源。
        /// </summary>
        RoundRobin,
    }

    /// <summary>
    ///     Playback role for a virtual FMOD event.
    ///     虚拟 FMOD event 的播放角色。
    /// </summary>
    public enum VirtualFmodEventKind
    {
        /// <summary>
        ///     Short one-shot sound.
        ///     短 one-shot 音效。
        /// </summary>
        OneShot,

        /// <summary>
        ///     Looping sound.
        ///     循环音效。
        /// </summary>
        Loop,

        /// <summary>
        ///     Single active music track.
        ///     单个活动音乐轨道。
        /// </summary>
        Music,
    }
}
