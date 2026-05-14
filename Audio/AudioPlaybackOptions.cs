namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Unified playback options for volume, pitch, parameters, lifecycle scope, and higher-level routing rules.
    ///     音量、音高、参数、生命周期作用域和更高层路由规则的统一播放选项。
    /// </summary>
    public sealed class AudioPlaybackOptions
    {
        /// <summary>
        ///     Initial numeric parameters to apply.
        ///     要应用的初始数值参数。
        /// </summary>
        public AudioParameterSet? Parameters { get; init; }

        /// <summary>
        ///     Initial volume.
        ///     初始音量。
        /// </summary>
        public float Volume { get; init; } = 1f;

        /// <summary>
        ///     Initial pitch when the backend supports it.
        ///     后端支持时使用的初始音高。
        /// </summary>
        public float Pitch { get; init; } = 1f;

        /// <summary>
        ///     When true, playback starts immediately after handle creation.
        ///     为 true 时，句柄创建后立即开始播放。
        /// </summary>
        public bool AutoPlay { get; init; } = true;

        /// <summary>
        ///     When true, playback begins paused.
        ///     为 true 时，播放以暂停状态开始。
        /// </summary>
        public bool StartPaused { get; init; }

        /// <summary>
        ///     Preferred stop mode for higher-level stop flows.
        ///     更高层停止流程使用的首选停止模式。
        /// </summary>
        public bool AllowFadeOutOnStop { get; init; } = true;

        /// <summary>
        ///     Optional cooldown in milliseconds.
        ///     可选冷却时间（毫秒）。
        /// </summary>
        public int CooldownMs { get; init; }

        /// <summary>
        ///     Built-in lifecycle scope used when no manual scope token is supplied.
        ///     未提供手动作用域令牌时使用的内置生命周期作用域。
        /// </summary>
        public AudioLifecycleScope Scope { get; init; } = AudioLifecycleScope.Manual;

        /// <summary>
        ///     Optional manual scope token for grouping handles.
        ///     用于分组句柄的可选手动作用域令牌。
        /// </summary>
        public AudioScopeToken? ScopeToken { get; init; }

        /// <summary>
        ///     When true, prefer vanilla-routed playback where applicable.
        ///     为 true 时，在适用处优先使用原版路由播放。
        /// </summary>
        public bool UseVanillaRouting { get; init; } = true;

        /// <summary>
        ///     Whether loop playback should use the vanilla loop parameter convention.
        ///     循环播放是否应使用原版循环参数约定。
        /// </summary>
        public bool UsesLoopParameter { get; init; } = true;

        /// <summary>
        ///     Optional identifier used for diagnostics or cooldown grouping.
        ///     用于诊断或冷却分组的可选标识符。
        /// </summary>
        public string? DebugName { get; init; }

        /// <summary>
        ///     Optional higher-level channel and tag routing rules.
        ///     可选的更高层通道和标签路由规则。
        /// </summary>
        public AudioRoutingOptions? Routing { get; init; }

        /// <summary>
        ///     Returns the normalized parameter dictionary for the current options.
        ///     返回当前选项的规范化参数字典。
        /// </summary>
        public IReadOnlyDictionary<string, float> GetParameters()
        {
            return Parameters?.Values ?? FmodParameterMap.Empty();
        }
    }
}
