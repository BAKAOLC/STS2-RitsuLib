using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Tracks named channels and tagged playback groups for higher-level replacement and bulk stop semantics.
    /// </summary>
    public sealed class AudioChannelRegistry
    {
        private readonly ConcurrentDictionary<string, IAudioHandle> _channels = new(StringComparer.Ordinal);

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<IAudioHandle, byte>> _tags =
            new(StringComparer.Ordinal);

        private AudioChannelRegistry()
        {
        }

        /// <summary>
        ///     Shared singleton registry.
        /// </summary>
        public static AudioChannelRegistry Shared { get; } = new();

        /// <summary>
        ///     Claims a named channel for a handle, optionally replacing the currently attached playback.
        /// </summary>
        public bool TryClaimChannel(string channel, IAudioHandle handle, AudioChannelMode mode, bool allowFadeOut)
        {
            while (true)
            {
                if (_channels.TryGetValue(channel, out var current))
                {
                    if (ReferenceEquals(current, handle))
                        return true;

                    if (mode == AudioChannelMode.KeepExisting)
                        return false;

                    current.TryStop(allowFadeOut);
                    current.TryRelease();
                    if (!_channels.TryUpdate(channel, handle, current))
                        continue;

                    return true;
                }

                if (_channels.TryAdd(channel, handle))
                    return true;
            }
        }

        /// <summary>
        ///     Removes a handle from any named channel it currently owns.
        /// </summary>
        public void ReleaseChannel(IAudioHandle handle)
        {
            foreach (var pair in _channels)
                if (ReferenceEquals(pair.Value, handle))
                    _channels.TryRemove(pair.Key, out _);
        }

        /// <summary>
        ///     Attaches a handle to a tag group for later bulk stop operations.
        /// </summary>
        public void AttachTag(string tag, IAudioHandle handle)
        {
            var set = _tags.GetOrAdd(tag, _ => new(ReferenceEqualityComparer.Instance));
            set.TryAdd(handle, 0);
        }

        /// <summary>
        ///     Removes a handle from all tracked channels and tag groups.
        /// </summary>
        public void Detach(IAudioHandle handle)
        {
            ReleaseChannel(handle);
            foreach (var pair in _tags)
                pair.Value.TryRemove(handle, out _);
        }

        /// <summary>
        ///     Stops and releases every handle attached to a tag group.
        /// </summary>
        public bool StopTag(string tag, bool allowFadeOut = true)
        {
            if (!_tags.TryGetValue(tag, out var handles))
                return false;

            var any = false;
            foreach (var handle in handles.Keys.ToArray())
            {
                any = true;
                handle.TryStop(allowFadeOut);
                handle.TryRelease();
                handles.TryRemove(handle, out _);
            }

            return any;
        }

        /// <summary>
        ///     Stops and releases the handle currently attached to a named channel.
        /// </summary>
        public bool StopChannel(string channel, bool allowFadeOut = true)
        {
            if (!_channels.TryRemove(channel, out var handle))
                return false;

            handle.TryStop(allowFadeOut);
            handle.TryRelease();
            return true;
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<IAudioHandle>
        {
            public static ReferenceEqualityComparer Instance { get; } = new();

            public bool Equals(IAudioHandle? x, IAudioHandle? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(IAudioHandle obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
