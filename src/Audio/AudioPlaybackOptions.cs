namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Configures initial backend values, lifecycle ownership, throttling, and higher-level routing
    ///         for a playback request.
    ///     </para>
    ///     <para xml:lang="zh-CN">配置播放请求的初始后端值、生命周期归属、节流和高级路由。</para>
    /// </summary>
    public sealed class AudioPlaybackOptions
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes the numeric FMOD parameters applied when the selected playback path
        ///         supports them.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或初始化所选播放路径支持时应用的数值型 FMOD 参数。</para>
        /// </summary>
        public AudioParameterSet? Parameters { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the unclamped linear volume passed to the backend.</para>
        ///     <para xml:lang="zh-CN">获取或初始化传递给后端的未钳制线性音量。</para>
        /// </summary>
        public float Volume { get; init; } = 1f;

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the unclamped pitch multiplier applied when supported.</para>
        ///     <para xml:lang="zh-CN">获取或初始化后端支持时应用的未钳制音高倍率。</para>
        /// </summary>
        public float Pitch { get; init; } = 1f;

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes whether a newly created controllable handle starts immediately.</para>
        ///     <para xml:lang="zh-CN">获取或初始化新创建的可控制句柄是否立即开始播放。</para>
        /// </summary>
        public bool AutoPlay { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes whether a newly created controllable handle requests a paused initial
        ///         state.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或初始化新创建的可控制句柄是否请求以暂停状态开始。</para>
        /// </summary>
        public bool StartPaused { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes whether disposal and higher-level stop flows may use FMOD fade-out.</para>
        ///     <para xml:lang="zh-CN">获取或初始化释放及高级停止流程是否可使用 FMOD 淡出。</para>
        /// </summary>
        public bool AllowFadeOutOnStop { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes the playback cooldown in milliseconds; zero or a negative value disables
        ///         throttling.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或初始化播放冷却时间（毫秒）；零或负值会禁用节流。</para>
        /// </summary>
        public int CooldownMs { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the built-in lifecycle scope used when <see cref="ScopeToken" /> is absent.</para>
        ///     <para xml:lang="zh-CN">获取或初始化未设置 <see cref="ScopeToken" /> 时使用的内置生命周期作用域。</para>
        /// </summary>
        public AudioLifecycleScope Scope { get; init; } = AudioLifecycleScope.Manual;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes the active manual token that overrides <see cref="Scope" /> for handle
        ///         grouping and cleanup; closing or disposed tokens reject playback.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或初始化用于覆盖 <see cref="Scope" /> 并对句柄进行分组清理的活动手动令牌；正在释放或已释放的令牌会使播放请求失败。</para>
        /// </summary>
        public AudioScopeToken? ScopeToken { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes whether Studio event-path one-shots use the game's native audio-manager
        ///         route.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或初始化 Studio 事件路径单次播放是否使用游戏原生音频管理器路径。</para>
        /// </summary>
        public bool UseVanillaRouting { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes whether Studio loop playback applies the game's <c>loop = 0</c> parameter
        ///         convention.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或初始化 Studio 循环播放是否应用游戏的 <c>loop = 0</c> 参数约定。</para>
        /// </summary>
        public bool UsesLoopParameter { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes the optional cooldown-group key; the source representation is used when
        ///         absent.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或初始化可选的冷却分组键；未设置时使用音频源的字符串表示。</para>
        /// </summary>
        public string? DebugName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the optional named-channel and tag-group routing rules.</para>
        ///     <para xml:lang="zh-CN">获取或初始化可选的命名通道与标签组路由规则。</para>
        /// </summary>
        public AudioRoutingOptions? Routing { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the configured read-only parameter dictionary, or the shared empty dictionary when none is
        ///         configured.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取已配置的只读参数字典；未配置时返回共享空字典。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The parameter dictionary used by playback dispatch.</para>
        ///     <para xml:lang="zh-CN">播放分派使用的参数字典。</para>
        /// </returns>
        public IReadOnlyDictionary<string, float> GetParameters()
        {
            return Parameters?.Values ?? FmodParameterMap.Empty();
        }
    }
}
