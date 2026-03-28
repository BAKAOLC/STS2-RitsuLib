using Godot;
using STS2RitsuLib.Audio.Internal;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Studio global parameters, system-wide mute/pause, DSP buffer, and performance snapshot.
    /// </summary>
    public static class FmodStudioMixerGlobals
    {
        /// <summary>
        ///     Sets a global parameter by name to a numeric value.
        /// </summary>
        public static bool TrySetGlobalParameter(string name, float value)
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.SetGlobalParameterByName, name, value);
        }

        /// <summary>
        ///     Reads a global parameter; 0 when missing or not convertible.
        /// </summary>
        public static float TryGetGlobalParameter(string name)
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetGlobalParameterByName, name))
                return 0f;

            try
            {
                return v.AsSingle();
            }
            catch
            {
                // ignored
                return 0f;
            }
        }

        /// <summary>
        ///     Sets a global parameter using a labeled discrete value.
        /// </summary>
        public static bool TrySetGlobalParameterByLabel(string name, string label)
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.SetGlobalParameterByNameWithLabel, name, label);
        }

        /// <summary>
        ///     Mutes all playing events at the Studio system level.
        /// </summary>
        public static bool TryMuteAllEvents()
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.MuteAllEvents);
        }

        /// <summary>
        ///     Clears system-wide mute.
        /// </summary>
        public static bool TryUnmuteAllEvents()
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.UnmuteAllEvents);
        }

        /// <summary>
        ///     Pauses all events.
        /// </summary>
        public static bool TryPauseAllEvents()
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.PauseAllEvents);
        }

        /// <summary>
        ///     Resumes paused events.
        /// </summary>
        public static bool TryUnpauseAllEvents()
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.UnpauseAllEvents);
        }

        /// <summary>
        ///     Adjusts DSP buffer sizing (advanced; may affect latency).
        /// </summary>
        public static bool TrySetDspBufferSize(int bufferLength, int bufferCount)
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.SetSystemDspBufferSize, bufferLength, bufferCount);
        }

        /// <summary>
        ///     Addon-specific performance payload; inspect in debugger or forward to your telemetry.
        /// </summary>
        public static Variant TryGetPerformanceData()
        {
            return FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetPerformanceData)
                ? v
                : default;
        }
    }
}
