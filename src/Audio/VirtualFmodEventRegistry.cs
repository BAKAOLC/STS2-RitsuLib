using System.Collections.ObjectModel;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Maps FMOD-style event paths to Godot audio resources for compatibility with native game call sites. Virtual
    ///         events use RitsuLib file playback and lifecycle tracking, but are not Studio events and do not join the FMOD
    ///         DSP graph or follow later bus-volume changes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将 FMOD 风格的事件路径映射到 Godot 音频资源，以兼容游戏的原生调用位置。虚拟事件使用 RitsuLib
    ///         的文件播放和生命周期跟踪，但不属于 Studio 事件，也不会加入 FMOD DSP 图或跟随之后的总线音量变化。
    ///     </para>
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
        ///     <para xml:lang="en">Registers or replaces a one-shot event backed by one Godot audio resource.</para>
        ///     <para xml:lang="zh-CN">注册或替换由单个 Godot 音频资源支撑的单次事件。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The case-sensitive virtual event path.</para>
        ///     <para xml:lang="zh-CN">区分大小写的虚拟事件路径。</para>
        /// </param>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The packed, imported, or raw Godot audio-resource path.</para>
        ///     <para xml:lang="zh-CN">打包、导入或原始 Godot 音频资源路径。</para>
        /// </param>
        /// <param name="busPath">
        ///     <para xml:lang="en">The bus whose current volume is sampled when playback starts.</para>
        ///     <para xml:lang="zh-CN">开始播放时采样当前音量的总线路径。</para>
        /// </param>
        /// <param name="volume">
        ///     <para xml:lang="en">The finite, non-negative event-volume multiplier.</para>
        ///     <para xml:lang="zh-CN">有限且非负的事件音量倍率。</para>
        /// </param>
        /// <param name="pitch">
        ///     <para xml:lang="en">The finite, positive pitch multiplier.</para>
        ///     <para xml:lang="zh-CN">有限且为正的音高倍率。</para>
        /// </param>
        public static void RegisterOneShot(string eventPath, string resourcePath,
            string busPath = FmodStudioRouting.SfxBus, float volume = 1f, float pitch = 1f)
        {
            Register(new(eventPath, resourcePath, VirtualFmodEventKind.OneShot, busPath, volume, pitch));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces one-shot events from an event-path-to-resource-path map.</para>
        ///     <para xml:lang="zh-CN">根据事件路径到资源路径的映射注册或替换多个单次事件。</para>
        /// </summary>
        /// <param name="eventResourcePaths">
        ///     <para xml:lang="en">The case-sensitive event paths and their Godot audio resources.</para>
        ///     <para xml:lang="zh-CN">区分大小写的事件路径及其 Godot 音频资源。</para>
        /// </param>
        /// <param name="busPath">
        ///     <para xml:lang="en">The bus whose current volume is sampled for each playback.</para>
        ///     <para xml:lang="zh-CN">每次播放时采样当前音量的总线路径。</para>
        /// </param>
        /// <param name="volume">
        ///     <para xml:lang="en">The finite, non-negative event-volume multiplier.</para>
        ///     <para xml:lang="zh-CN">有限且非负的事件音量倍率。</para>
        /// </param>
        /// <param name="pitch">
        ///     <para xml:lang="en">The finite, positive pitch multiplier.</para>
        ///     <para xml:lang="zh-CN">有限且为正的音高倍率。</para>
        /// </param>
        public static void RegisterOneShots(IReadOnlyDictionary<string, string> eventResourcePaths,
            string busPath = FmodStudioRouting.SfxBus, float volume = 1f, float pitch = 1f)
        {
            ArgumentNullException.ThrowIfNull(eventResourcePaths);

            foreach (var (eventPath, resourcePath) in eventResourcePaths)
                RegisterOneShot(eventPath, resourcePath, busPath, volume, pitch);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a one-shot event backed by multiple resource variants.</para>
        ///     <para xml:lang="zh-CN">注册或替换由多个资源变体支撑的单次事件。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The case-sensitive virtual event path.</para>
        ///     <para xml:lang="zh-CN">区分大小写的虚拟事件路径。</para>
        /// </param>
        /// <param name="resourcePaths">
        ///     <para xml:lang="en">The non-empty variant list, copied in enumeration order.</para>
        ///     <para xml:lang="zh-CN">非空的变体列表；注册时按枚举顺序复制。</para>
        /// </param>
        /// <param name="selection">
        ///     <para xml:lang="en">How a resource is selected for each playback.</para>
        ///     <para xml:lang="zh-CN">每次播放时选择资源的方式。</para>
        /// </param>
        /// <param name="busPath">
        ///     <para xml:lang="en">The bus whose current volume is sampled when playback starts.</para>
        ///     <para xml:lang="zh-CN">开始播放时采样当前音量的总线路径。</para>
        /// </param>
        /// <param name="volume">
        ///     <para xml:lang="en">The finite, non-negative event-volume multiplier.</para>
        ///     <para xml:lang="zh-CN">有限且非负的事件音量倍率。</para>
        /// </param>
        /// <param name="pitch">
        ///     <para xml:lang="en">The finite, positive pitch multiplier.</para>
        ///     <para xml:lang="zh-CN">有限且为正的音高倍率。</para>
        /// </param>
        public static void RegisterOneShotVariants(string eventPath, IReadOnlyList<string> resourcePaths,
            VirtualFmodVariantSelection selection = VirtualFmodVariantSelection.Random,
            string busPath = FmodStudioRouting.SfxBus, float volume = 1f, float pitch = 1f)
        {
            if (!Enum.IsDefined(selection))
                throw new ArgumentOutOfRangeException(nameof(selection), selection, null);

            var normalizedResourcePaths = ValidateResourcePaths(resourcePaths, nameof(resourcePaths));
            Register(new(eventPath, normalizedResourcePaths[0], VirtualFmodEventKind.OneShot, busPath, volume, pitch)
            {
                ResourcePaths = normalizedResourcePaths,
                VariantSelection = selection,
            });
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces variant-backed one-shot events from a map.</para>
        ///     <para xml:lang="zh-CN">根据映射注册或替换多个由变体支撑的单次事件。</para>
        /// </summary>
        /// <param name="eventResourcePaths">
        ///     <para xml:lang="en">The case-sensitive event paths and their non-empty resource-variant lists.</para>
        ///     <para xml:lang="zh-CN">区分大小写的事件路径及其非空资源变体列表。</para>
        /// </param>
        /// <param name="selection">
        ///     <para xml:lang="en">How a resource is selected for each playback.</para>
        ///     <para xml:lang="zh-CN">每次播放时选择资源的方式。</para>
        /// </param>
        /// <param name="busPath">
        ///     <para xml:lang="en">The bus whose current volume is sampled for each playback.</para>
        ///     <para xml:lang="zh-CN">每次播放时采样当前音量的总线路径。</para>
        /// </param>
        /// <param name="volume">
        ///     <para xml:lang="en">The finite, non-negative event-volume multiplier.</para>
        ///     <para xml:lang="zh-CN">有限且非负的事件音量倍率。</para>
        /// </param>
        /// <param name="pitch">
        ///     <para xml:lang="en">The finite, positive pitch multiplier.</para>
        ///     <para xml:lang="zh-CN">有限且为正的音高倍率。</para>
        /// </param>
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
        ///     <para xml:lang="en">Registers or replaces a streaming loop event backed by a Godot audio resource.</para>
        ///     <para xml:lang="zh-CN">注册或替换由 Godot 音频资源支撑的流式循环事件。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The case-sensitive virtual event path.</para>
        ///     <para xml:lang="zh-CN">区分大小写的虚拟事件路径。</para>
        /// </param>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The packed, imported, or raw Godot audio-resource path.</para>
        ///     <para xml:lang="zh-CN">打包、导入或原始 Godot 音频资源路径。</para>
        /// </param>
        /// <param name="busPath">
        ///     <para xml:lang="en">The bus whose current volume is sampled when playback starts.</para>
        ///     <para xml:lang="zh-CN">开始播放时采样当前音量的总线路径。</para>
        /// </param>
        /// <param name="volume">
        ///     <para xml:lang="en">The finite, non-negative event-volume multiplier.</para>
        ///     <para xml:lang="zh-CN">有限且非负的事件音量倍率。</para>
        /// </param>
        /// <param name="pitch">
        ///     <para xml:lang="en">The finite, positive pitch multiplier.</para>
        ///     <para xml:lang="zh-CN">有限且为正的音高倍率。</para>
        /// </param>
        /// <param name="stream">
        ///     <para xml:lang="en">Must be <see langword="true" /> because the bundled backend supports looping only for streaming music files.</para>
        ///     <para xml:lang="zh-CN">必须为 <see langword="true" />，因为随游戏提供的后端仅支持流式音乐文件循环播放。</para>
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">Thrown when <paramref name="stream" /> is <see langword="false" /> or another definition value is invalid.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="stream" /> 为 <see langword="false" /> 或其他定义值无效时抛出。</para>
        /// </exception>
        public static void RegisterLoop(string eventPath, string resourcePath,
            string busPath = FmodStudioRouting.SfxBus, float volume = 1f, float pitch = 1f, bool stream = true)
        {
            Register(new(eventPath, resourcePath, VirtualFmodEventKind.Loop, busPath, volume, pitch, stream));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a streaming music event backed by a Godot audio resource.</para>
        ///     <para xml:lang="zh-CN">注册或替换由 Godot 音频资源支撑的流式音乐事件。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The case-sensitive virtual event path.</para>
        ///     <para xml:lang="zh-CN">区分大小写的虚拟事件路径。</para>
        /// </param>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The packed, imported, or raw Godot audio-resource path.</para>
        ///     <para xml:lang="zh-CN">打包、导入或原始 Godot 音频资源路径。</para>
        /// </param>
        /// <param name="busPath">
        ///     <para xml:lang="en">The bus whose current volume is sampled when playback starts.</para>
        ///     <para xml:lang="zh-CN">开始播放时采样当前音量的总线路径。</para>
        /// </param>
        /// <param name="volume">
        ///     <para xml:lang="en">The finite, non-negative event-volume multiplier.</para>
        ///     <para xml:lang="zh-CN">有限且非负的事件音量倍率。</para>
        /// </param>
        /// <param name="pitch">
        ///     <para xml:lang="en">The finite, positive pitch multiplier.</para>
        ///     <para xml:lang="zh-CN">有限且为正的音高倍率。</para>
        /// </param>
        public static void RegisterMusic(string eventPath, string resourcePath,
            string busPath = FmodStudioRouting.MusicBus, float volume = 1f, float pitch = 1f)
        {
            Register(new(eventPath, resourcePath, VirtualFmodEventKind.Music, busPath, volume, pitch, true));
        }

        /// <summary>
        ///     <para xml:lang="en">Validates, snapshots, and registers or replaces a virtual event definition.</para>
        ///     <para xml:lang="zh-CN">验证并快照虚拟事件定义，然后注册或替换该定义。</para>
        /// </summary>
        /// <param name="definition">
        ///     <para xml:lang="en">The definition to register. Replacing an event resets its round-robin position.</para>
        ///     <para xml:lang="zh-CN">要注册的定义。替换事件时会重置其轮询位置。</para>
        /// </param>
        public static void Register(VirtualFmodEventDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            if (string.IsNullOrWhiteSpace(definition.EventPath))
                throw new ArgumentException("Virtual FMOD event path must be non-empty.", nameof(definition));
            if (string.IsNullOrWhiteSpace(definition.ResourcePath))
                throw new ArgumentException("Virtual FMOD event resource path must be non-empty.", nameof(definition));
            if (!Enum.IsDefined(definition.Kind))
                throw new ArgumentOutOfRangeException(nameof(definition), definition.Kind,
                    "Virtual FMOD event kind is not defined.");
            if (!Enum.IsDefined(definition.VariantSelection))
                throw new ArgumentOutOfRangeException(nameof(definition), definition.VariantSelection,
                    "Virtual FMOD variant selection mode is not defined.");
            if (!float.IsFinite(definition.Volume) || definition.Volume < 0f)
                throw new ArgumentOutOfRangeException(nameof(definition), definition.Volume,
                    "Virtual FMOD event volume must be finite and non-negative.");
            if (!float.IsFinite(definition.Pitch) || definition.Pitch <= 0f)
                throw new ArgumentOutOfRangeException(nameof(definition), definition.Pitch,
                    "Virtual FMOD event pitch must be finite and positive.");
            if (definition.Kind == VirtualFmodEventKind.Loop && !definition.Stream)
                throw new ArgumentException(
                    "Virtual FMOD loops require streaming playback because the bundled backend does not loop fully loaded sounds.",
                    nameof(definition));

            var resourcePaths = ValidateResourcePaths(definition.ResourcePaths, nameof(definition));
            if (definition.Kind != VirtualFmodEventKind.OneShot && resourcePaths.Count > 1)
                throw new ArgumentException("Only one-shot virtual FMOD events can use resource variants.",
                    nameof(definition));

            definition = definition with
            {
                ResourcePath = resourcePaths[0],
                ResourcePaths = resourcePaths,
            };

            lock (Gate)
            {
                Definitions[definition.EventPath] = definition;
                VariantIndexes.Remove(definition.EventPath);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Removes a virtual event definition and its round-robin position without stopping active playback.</para>
        ///     <para xml:lang="zh-CN">移除虚拟事件定义及其轮询位置，但不停止正在进行的播放。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The case-sensitive virtual event path.</para>
        ///     <para xml:lang="zh-CN">区分大小写的虚拟事件路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when a definition was removed; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">成功移除定义时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool Unregister(string eventPath)
        {
            lock (Gate)
            {
                VariantIndexes.Remove(eventPath);
                return Definitions.Remove(eventPath);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Gets whether an exact event path is registered as a virtual event.</para>
        ///     <para xml:lang="zh-CN">获取准确事件路径是否已注册为虚拟事件。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The case-sensitive event path to test.</para>
        ///     <para xml:lang="zh-CN">要测试的区分大小写事件路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when a nonblank exact path is registered; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">非空白的准确路径已注册时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
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
            return FmodStudioStreamingFiles.TryPlayResourceSound(
                resourcePath,
                ResolveVolume(definition, volume),
                definition.Pitch);
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
                BuildOptions(definition, 1f, null, AudioLifecycleScope.Room));
            if (!result.Succeeded || result.Handle is not { IsValid: true })
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
            lock (Gate)
            {
                if (!Loops.TryGetValue(eventPath, out var queue) || queue.Count == 0)
                    return false;

                var handle = queue.Peek();
                handle.Dispose();
                if (!handle.IsReleased)
                    return false;

                queue.Dequeue();
                if (queue.Count == 0)
                    Loops.Remove(eventPath);
                return true;
            }
        }

        internal static void StopAllLoops()
        {
            lock (Gate)
            {
                foreach (var path in Loops.Keys.ToArray())
                {
                    var queue = Loops[path];
                    var retained = new Queue<IAudioHandle>();
                    foreach (var handle in queue)
                    {
                        handle.Dispose();
                        if (!handle.IsReleased)
                            retained.Enqueue(handle);
                    }

                    if (retained.Count == 0)
                        Loops.Remove(path);
                    else
                        Loops[path] = retained;
                }
            }
        }

        internal static bool TryPlayMusic(string eventPath)
        {
            if (!TryGetDefinition(eventPath, out var definition))
                return false;

            if (definition.Kind != VirtualFmodEventKind.Music)
                return false;

            if (!StopMusic())
                return false;

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

        internal static bool StopMusic()
        {
            lock (Gate)
            {
                if (_music is null)
                    return true;

                _music.Dispose();
                if (!_music.IsReleased)
                    return false;

                _music = null;
                return true;
            }
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

        private static IReadOnlyList<string> ValidateResourcePaths(IReadOnlyList<string>? resourcePaths,
            string parameterName)
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

            return new ReadOnlyCollection<string>(result);
        }

        private static AudioPlaybackOptions BuildOptions(VirtualFmodEventDefinition definition, float callVolume,
            string? channel, AudioLifecycleScope scope)
        {
            return new()
            {
                Volume = ResolveVolume(definition, callVolume),
                Pitch = definition.Pitch,
                Scope = scope,
                Routing = string.IsNullOrWhiteSpace(channel)
                    ? null
                    : new AudioRoutingOptions { Channel = channel, ChannelMode = AudioChannelMode.ReplaceExisting },
            };
        }

        private static float ResolveVolume(VirtualFmodEventDefinition definition, float callVolume)
        {
            return definition.Volume * callVolume * ResolveBusVolume(definition.BusPath);
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
    ///     <para xml:lang="en">Defines an FMOD-style virtual event backed by one or more Godot audio resources.</para>
    ///     <para xml:lang="zh-CN">定义由一个或多个 Godot 音频资源支撑的 FMOD 风格虚拟事件。</para>
    /// </summary>
    /// <param name="EventPath">
    ///     <para xml:lang="en">The case-sensitive virtual event path.</para>
    ///     <para xml:lang="zh-CN">区分大小写的虚拟事件路径。</para>
    /// </param>
    /// <param name="ResourcePath">
    ///     <para xml:lang="en">The primary packed, imported, or raw Godot audio-resource path.</para>
    ///     <para xml:lang="zh-CN">主要的打包、导入或原始 Godot 音频资源路径。</para>
    /// </param>
    /// <param name="Kind">
    ///     <para xml:lang="en">The playback role supported by the event.</para>
    ///     <para xml:lang="zh-CN">事件支持的播放用途。</para>
    /// </param>
    /// <param name="BusPath">
    ///     <para xml:lang="en">The bus whose current volume is sampled when playback starts.</para>
    ///     <para xml:lang="zh-CN">开始播放时采样当前音量的总线路径。</para>
    /// </param>
    /// <param name="Volume">
    ///     <para xml:lang="en">The finite, non-negative event-volume multiplier.</para>
    ///     <para xml:lang="zh-CN">有限且非负的事件音量倍率。</para>
    /// </param>
    /// <param name="Pitch">
    ///     <para xml:lang="en">The finite, positive pitch multiplier.</para>
    ///     <para xml:lang="zh-CN">有限且为正的音高倍率。</para>
    /// </param>
    /// <param name="Stream">
    ///     <para xml:lang="en">Whether file playback uses the streaming, looping backend mode. Registered loops require this to be <see langword="true" />.</para>
    ///     <para xml:lang="zh-CN">文件播放是否使用流式循环后端模式。注册循环事件时必须为 <see langword="true" />。</para>
    /// </param>
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
        ///     <para xml:lang="en">Gets or initializes the resource variants used by a one-shot event; loops and music require exactly one.</para>
        ///     <para xml:lang="zh-CN">获取或初始化单次事件使用的资源变体；循环和音乐事件必须恰好使用一个资源。</para>
        /// </summary>
        public IReadOnlyList<string> ResourcePaths { get; init; } = [ResourcePath];

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes how one-shot resource variants are selected.</para>
        ///     <para xml:lang="zh-CN">获取或初始化单次事件资源变体的选择方式。</para>
        /// </summary>
        public VirtualFmodVariantSelection VariantSelection { get; init; } = VirtualFmodVariantSelection.Random;
    }

    /// <summary>
    ///     <para xml:lang="en">Specifies how a one-shot virtual event selects a resource variant.</para>
    ///     <para xml:lang="zh-CN">指定单次虚拟事件选择资源变体的方式。</para>
    /// </summary>
    public enum VirtualFmodVariantSelection
    {
        /// <summary>
        ///     <para xml:lang="en">Selects a resource independently through <see cref="Random.Shared" /> for each playback.</para>
        ///     <para xml:lang="zh-CN">每次播放时通过 <see cref="Random.Shared" /> 独立随机选择资源。</para>
        /// </summary>
        Random,

        /// <summary>
        ///     <para xml:lang="en">Cycles through resources in registration order, restarting after registration or replacement.</para>
        ///     <para xml:lang="zh-CN">按注册顺序轮询资源；注册或替换后从头开始。</para>
        /// </summary>
        RoundRobin,
    }

    /// <summary>
    ///     <para xml:lang="en">Specifies the playback role of a virtual FMOD event.</para>
    ///     <para xml:lang="zh-CN">指定虚拟 FMOD 事件的播放用途。</para>
    /// </summary>
    public enum VirtualFmodEventKind
    {
        /// <summary>
        ///     <para xml:lang="en">A fully loaded sound played once.</para>
        ///     <para xml:lang="zh-CN">完整加载并单次播放的音效。</para>
        /// </summary>
        OneShot,

        /// <summary>
        ///     <para xml:lang="en">A streaming, looping sound tracked in the room lifecycle scope.</para>
        ///     <para xml:lang="zh-CN">在房间生命周期作用域中跟踪的流式循环音效。</para>
        /// </summary>
        Loop,

        /// <summary>
        ///     <para xml:lang="en">A streaming music track that replaces the current virtual music channel.</para>
        ///     <para xml:lang="zh-CN">替换当前虚拟音乐通道的流式音乐轨道。</para>
        /// </summary>
        Music,
    }
}
