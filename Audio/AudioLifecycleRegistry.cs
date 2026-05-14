using System.Collections.Concurrent;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Central registry that stops scoped handles when framework lifecycle events fire.
    ///     中央注册表，在框架生命周期事件触发时停止带作用域的句柄。
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
        ///     Shared singleton registry.
        ///     共享的单例注册表。
        /// </summary>
        public static AudioLifecycleRegistry Shared { get; } = new();

        /// <summary>
        ///     Disposes framework lifecycle subscriptions owned by this registry.
        ///     释放此注册表拥有的框架生命周期订阅。
        /// </summary>
        public void Dispose()
        {
            _combatEndedSubscription.Dispose();
            _roomExitedSubscription.Dispose();
            _runEndedSubscription.Dispose();
        }

        /// <summary>
        ///     Attaches a handle to either a manual token or a built-in scope.
        ///     将句柄附加到手动 token 或内置作用域。
        /// </summary>
        public void Attach(IAudioHandle handle, AudioPlaybackOptions? options)
        {
            var token = options?.ScopeToken;
            if (token is not null)
            {
                var tokenSet = _tokenHandles.GetOrAdd(token, _ => new());
                tokenSet.TryAdd(handle, 0);
                return;
            }

            var scopeSet = _scopeHandles.GetOrAdd(handle.Scope, _ => new());
            scopeSet.TryAdd(handle, 0);
        }

        /// <summary>
        ///     Removes a handle from all tracked scopes and tokens.
        ///     从所有已跟踪作用域和 token 中移除句柄。
        /// </summary>
        public void Detach(IAudioHandle handle)
        {
            foreach (var kv in _scopeHandles)
                kv.Value.TryRemove(handle, out _);

            foreach (var kv in _tokenHandles)
                kv.Value.TryRemove(handle, out _);
        }

        /// <summary>
        ///     Stops and releases every handle attached to a built-in scope.
        ///     停止并释放附加到内置作用域的每个句柄。
        /// </summary>
        public bool StopScope(AudioLifecycleScope scope, bool allowFadeOut = true)
        {
            if (!_scopeHandles.TryGetValue(scope, out var handles))
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
        ///     Stops and releases every handle attached to a manual token.
        ///     停止并释放附加到手动 token 的每个句柄。
        /// </summary>
        public bool StopScope(AudioScopeToken token, bool allowFadeOut = true)
        {
            if (!_tokenHandles.TryGetValue(token, out var handles))
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
    }
}
