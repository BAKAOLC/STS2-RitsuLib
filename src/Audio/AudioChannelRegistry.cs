using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Tracks case-sensitive named channels and tag groups for playback replacement and bulk stop operations.</para>
    ///     <para xml:lang="zh-CN">跟踪区分大小写的命名通道和标签组，用于替换播放与批量停止操作。</para>
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
        ///     <para xml:lang="en">Gets the shared channel and tag registry.</para>
        ///     <para xml:lang="zh-CN">获取共享的通道与标签注册表。</para>
        /// </summary>
        public static AudioChannelRegistry Shared { get; } = new();

        /// <summary>
        ///     <para xml:lang="en">Attempts to assign a handle to a named channel according to the requested collision policy.</para>
        ///     <para xml:lang="zh-CN">按照指定的冲突策略尝试将句柄分配到命名通道。</para>
        /// </summary>
        /// <param name="channel">
        ///     <para xml:lang="en">The case-sensitive channel name.</para>
        ///     <para xml:lang="zh-CN">区分大小写的通道名称。</para>
        /// </param>
        /// <param name="handle">
        ///     <para xml:lang="en">The handle that should own the channel.</para>
        ///     <para xml:lang="zh-CN">应占用该通道的句柄。</para>
        /// </param>
        /// <param name="mode">
        ///     <para xml:lang="en">How to handle an existing owner.</para>
        ///     <para xml:lang="zh-CN">已有占用者的处理方式。</para>
        /// </param>
        /// <param name="allowFadeOut">
        ///     <para xml:lang="en">Whether a replaced owner may fade out when stopped.</para>
        ///     <para xml:lang="zh-CN">停止被替换的占用者时是否允许淡出。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the handle already owns or successfully claims the channel; <see langword="false" /> when an existing owner is kept.</para>
        ///     <para xml:lang="zh-CN">句柄已占用或成功占用该通道时为 <see langword="true" />；保留已有占用者时为 <see langword="false" />。</para>
        /// </returns>
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
                    if (!current.TryRelease())
                        return false;

                    if (!_channels.TryUpdate(channel, handle, current))
                        continue;

                    return true;
                }

                if (_channels.TryAdd(channel, handle))
                    return true;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Removes a handle from every named channel it currently owns without stopping it.</para>
        ///     <para xml:lang="zh-CN">从该句柄当前占用的所有命名通道中移除它，但不停止播放。</para>
        /// </summary>
        /// <param name="handle">
        ///     <para xml:lang="en">The channel owner to remove.</para>
        ///     <para xml:lang="zh-CN">要移除的通道占用者。</para>
        /// </param>
        public void ReleaseChannel(IAudioHandle handle)
        {
            foreach (var pair in _channels)
                if (ReferenceEquals(pair.Value, handle))
                    _channels.TryRemove(pair.Key, out _);
        }

        /// <summary>
        ///     <para xml:lang="en">Adds a handle by reference identity to a tag group for later bulk stopping.</para>
        ///     <para xml:lang="zh-CN">按引用标识将句柄添加到标签组，以便之后批量停止。</para>
        /// </summary>
        /// <param name="tag">
        ///     <para xml:lang="en">The case-sensitive tag name.</para>
        ///     <para xml:lang="zh-CN">区分大小写的标签名称。</para>
        /// </param>
        /// <param name="handle">
        ///     <para xml:lang="en">The handle to add.</para>
        ///     <para xml:lang="zh-CN">要添加的句柄。</para>
        /// </param>
        public void AttachTag(string tag, IAudioHandle handle)
        {
            var set = _tags.GetOrAdd(tag, _ => new(ReferenceEqualityComparer.Instance));
            set.TryAdd(handle, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">Removes a handle from all tracked channels and tag groups without stopping it.</para>
        ///     <para xml:lang="zh-CN">从所有跟踪的通道和标签组中移除句柄，但不停止播放。</para>
        /// </summary>
        /// <param name="handle">
        ///     <para xml:lang="en">The handle to detach.</para>
        ///     <para xml:lang="zh-CN">要分离的句柄。</para>
        /// </param>
        public void Detach(IAudioHandle handle)
        {
            ReleaseChannel(handle);
            foreach (var pair in _tags)
                pair.Value.TryRemove(handle, out _);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to stop and release every handle in a tag group, retaining entries whose release fails.</para>
        ///     <para xml:lang="zh-CN">尝试停止并释放标签组中的所有句柄，并保留释放失败的条目。</para>
        /// </summary>
        /// <param name="tag">
        ///     <para xml:lang="en">The case-sensitive tag name.</para>
        ///     <para xml:lang="zh-CN">区分大小写的标签名称。</para>
        /// </param>
        /// <param name="allowFadeOut">
        ///     <para xml:lang="en">Whether stopped handles may fade out.</para>
        ///     <para xml:lang="zh-CN">停止句柄时是否允许淡出。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when at least one handle was found and every release completed; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">找到至少一个句柄且所有释放均已完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public bool StopTag(string tag, bool allowFadeOut = true)
        {
            if (!_tags.TryGetValue(tag, out var handles))
                return false;

            var any = false;
            var allReleased = true;
            foreach (var handle in handles.Keys.ToArray())
            {
                any = true;
                handle.TryStop(allowFadeOut);
                if (!handle.TryRelease())
                {
                    allReleased = false;
                    continue;
                }

                handles.TryRemove(handle, out _);
            }

            return any && allReleased;
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to stop and release the handle assigned to a named channel, removing it only when release succeeds.</para>
        ///     <para xml:lang="zh-CN">尝试停止并释放分配到命名通道的句柄，并且仅在释放成功时移除它。</para>
        /// </summary>
        /// <param name="channel">
        ///     <para xml:lang="en">The case-sensitive channel name.</para>
        ///     <para xml:lang="zh-CN">区分大小写的通道名称。</para>
        /// </param>
        /// <param name="allowFadeOut">
        ///     <para xml:lang="en">Whether the stopped handle may fade out.</para>
        ///     <para xml:lang="zh-CN">停止句柄时是否允许淡出。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when an assigned handle was found and released; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">找到并成功释放已分配句柄时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public bool StopChannel(string channel, bool allowFadeOut = true)
        {
            if (!_channels.TryGetValue(channel, out var handle))
                return false;

            handle.TryStop(allowFadeOut);
            if (!handle.TryRelease())
                return false;

            _channels.TryRemove(new(channel, handle));
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
