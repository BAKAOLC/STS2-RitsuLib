namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Unified playback options for volume, pitch, parameters, lifecycle scope, and higher-level routing rules.
    /// </summary>
    public sealed class AudioPlaybackOptions
    {
        /// <summary>
        ///     Initial numeric parameters to apply.
        /// </summary>
        public AudioParameterSet? Parameters { get; init; }

        /// <summary>
        ///     Initial volume.
        /// </summary>
        public float Volume { get; init; } = 1f;

        /// <summary>
        ///     Initial pitch when the backend supports it.
        /// </summary>
        public float Pitch { get; init; } = 1f;

        /// <summary>
        ///     When true, playback starts immediately after handle creation.
        /// </summary>
        public bool AutoPlay { get; init; } = true;

        /// <summary>
        ///     When true, playback begins paused.
        /// </summary>
        public bool StartPaused { get; init; }

        /// <summary>
        ///     Preferred stop mode for higher-level stop flows.
        /// </summary>
        public bool AllowFadeOutOnStop { get; init; } = true;

        /// <summary>
        ///     Optional cooldown in milliseconds.
        /// </summary>
        public int CooldownMs { get; init; }

        /// <summary>
        ///     Built-in lifecycle scope used when no manual scope token is supplied.
        /// </summary>
        public AudioLifecycleScope Scope { get; init; } = AudioLifecycleScope.Manual;

        /// <summary>
        ///     Optional manual scope token for grouping handles.
        /// </summary>
        public AudioScopeToken? ScopeToken { get; init; }

        /// <summary>
        ///     When true, prefer vanilla-routed playback where applicable.
        /// </summary>
        public bool UseVanillaRouting { get; init; } = true;

        /// <summary>
        ///     Whether loop playback should use the vanilla loop parameter convention.
        /// </summary>
        public bool UsesLoopParameter { get; init; } = true;

        /// <summary>
        ///     Optional identifier used for diagnostics or cooldown grouping.
        /// </summary>
        public string? DebugName { get; init; }

        /// <summary>
        ///     Optional higher-level channel and tag routing rules.
        /// </summary>
        public AudioRoutingOptions? Routing { get; init; }

        /// <summary>
        ///     Returns the normalized parameter dictionary for the current options.
        /// </summary>
        public IReadOnlyDictionary<string, float> GetParameters()
        {
            return Parameters?.Values ?? FmodParameterMap.Empty();
        }
    }
}
