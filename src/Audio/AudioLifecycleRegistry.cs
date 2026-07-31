using System.Collections.Concurrent;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Tracks audio handles by built-in lifecycle scope or manual token and cleans them up at matching
    ///         lifecycle boundaries.
    ///     </para>
    ///     <para xml:lang="zh-CN">按内置生命周期作用域或手动令牌跟踪音频句柄，并在对应的生命周期边界清理它们。</para>
    /// </summary>
    public sealed class AudioLifecycleRegistry : IDisposable
    {
        private readonly IDisposable _combatEndedSubscription;
        private readonly IDisposable _roomExitedSubscription;
        private readonly IDisposable _runEndedSubscription;

        private readonly ConcurrentDictionary<AudioLifecycleScope, ConcurrentDictionary<IAudioHandle, byte>>
            _scopeHandles = new();

        private readonly ConcurrentDictionary<AudioScopeToken, ConcurrentDictionary<IAudioHandle, byte>> _tokenHandles =
            new();

        private AudioLifecycleRegistry()
        {
            _combatEndedSubscription =
                RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(_ => StopScope(AudioLifecycleScope.Combat));
            _roomExitedSubscription =
                RitsuLibFramework.SubscribeLifecycle<RoomExitedEvent>(_ => StopScope(AudioLifecycleScope.Room));
            _runEndedSubscription =
                RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ => StopScope(AudioLifecycleScope.Run));
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the shared lifecycle registry.</para>
        ///     <para xml:lang="zh-CN">获取共享的生命周期注册表。</para>
        /// </summary>
        public static AudioLifecycleRegistry Shared { get; } = new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Disposes this registry's lifecycle subscriptions without stopping or detaching currently
        ///         tracked handles.
        ///     </para>
        ///     <para xml:lang="zh-CN">释放此注册表的生命周期订阅，但不会停止或分离当前跟踪的句柄。</para>
        /// </summary>
        public void Dispose()
        {
            _combatEndedSubscription.Dispose();
            _roomExitedSubscription.Dispose();
            _runEndedSubscription.Dispose();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tracks a handle under an active manual token when present, or under the handle's built-in scope otherwise. A
        ///         handle that races with token disposal is disposed immediately; if release fails, its built-in scope retains
        ///         it for a later cleanup attempt.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         设置了活动的手动令牌时按该令牌跟踪句柄，否则按句柄的内置作用域跟踪。与令牌释放发生竞争的句柄会立即尝试释放；
        ///         如果释放失败，则由其内置作用域保留，以便之后重试清理。
        ///     </para>
        /// </summary>
        /// <param name="handle">
        ///     <para xml:lang="en">The handle to track.</para>
        ///     <para xml:lang="zh-CN">要跟踪的句柄。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">The playback options that may provide an active manual scope token.</para>
        ///     <para xml:lang="zh-CN">可提供活动手动作用域令牌的播放选项。</para>
        /// </param>
        public void Attach(IAudioHandle handle, AudioPlaybackOptions? options)
        {
            TryAttach(handle, options);
        }

        internal bool TryAttach(IAudioHandle handle, AudioPlaybackOptions? options)
        {
            var token = options?.ScopeToken;
            if (token is null)
            {
                TrackByScope(handle);
                return true;
            }

            if (token.IsClosing)
            {
                DisposeOrRetainByScope(handle);
                return false;
            }

            var tokenSet = _tokenHandles.GetOrAdd(token, _ => new());
            tokenSet.TryAdd(handle, 0);
            // Keep the close-race cleanup as the exceptional branch.
            // ReSharper disable once InvertIf
            if (token.IsClosing && tokenSet.TryRemove(handle, out _))
            {
                DisposeOrRetainByScope(handle);
                return false;
            }

            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Removes a handle from every tracked scope and token without stopping it.</para>
        ///     <para xml:lang="zh-CN">从所有跟踪的作用域和令牌中移除句柄，但不停止播放。</para>
        /// </summary>
        /// <param name="handle">
        ///     <para xml:lang="en">The handle to detach.</para>
        ///     <para xml:lang="zh-CN">要分离的句柄。</para>
        /// </param>
        public void Detach(IAudioHandle handle)
        {
            foreach (var kv in _scopeHandles)
                kv.Value.TryRemove(handle, out _);

            foreach (var kv in _tokenHandles)
                kv.Value.TryRemove(handle, out _);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to stop and release every handle tracked under a built-in scope, retaining entries
        ///         whose release fails.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试停止并释放内置作用域下跟踪的所有句柄，并保留释放失败的条目。</para>
        /// </summary>
        /// <param name="scope">
        ///     <para xml:lang="en">The built-in lifecycle scope to clean up.</para>
        ///     <para xml:lang="zh-CN">要清理的内置生命周期作用域。</para>
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
        public bool StopScope(AudioLifecycleScope scope, bool allowFadeOut = true)
        {
            if (!_scopeHandles.TryGetValue(scope, out var handles))
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
        ///     <para xml:lang="en">
        ///         Attempts to stop and release every handle tracked under a manual token, retaining entries whose
        ///         release fails.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试停止并释放手动令牌下跟踪的所有句柄，并保留释放失败的条目。</para>
        /// </summary>
        /// <param name="token">
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
        public bool StopScope(AudioScopeToken token, bool allowFadeOut = true)
        {
            if (!_tokenHandles.TryGetValue(token, out var handles))
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

        internal bool TryDisposeScope(AudioScopeToken token, bool allowFadeOut = true)
        {
            if (!_tokenHandles.TryGetValue(token, out var handles))
                return true;

            var allReleased = true;
            foreach (var handle in handles.Keys.ToArray())
            {
                handle.TryStop(allowFadeOut);
                if (!handle.TryRelease())
                {
                    allReleased = false;
                    continue;
                }

                handles.TryRemove(handle, out _);
            }

            if (!allReleased || !handles.IsEmpty)
                return false;

            _tokenHandles.TryRemove(new(token, handles));
            return true;
        }

        private void DisposeOrRetainByScope(IAudioHandle handle)
        {
            handle.Dispose();
            if (!handle.IsReleased)
                TrackByScope(handle);
        }

        private void TrackByScope(IAudioHandle handle)
        {
            var scopeSet = _scopeHandles.GetOrAdd(handle.Scope, _ => new());
            scopeSet.TryAdd(handle, 0);
        }
    }
}
