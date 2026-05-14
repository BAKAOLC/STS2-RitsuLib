using Godot;
using STS2RitsuLib.Audio.Internal;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Studio global parameters, system-wide mute/pause, DSP buffer, and performance snapshot.
    ///     Studio 全局参数、系统级静音/暂停、DSP buffer 和性能快照。
    /// </summary>
    public static class FmodStudioMixerGlobals
    {
        /// <summary>
        ///     Sets a global parameter by name to a numeric value.
        ///     按名称将全局参数设置为数值。
        /// </summary>
        public static bool TrySetGlobalParameter(string name, float value)
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.SetGlobalParameterByName, name, value);
        }

        /// <summary>
        ///     Reads a global parameter; 0 when missing or not convertible.
        ///     读取全局参数；缺失或无法转换时为 0。
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
        ///     使用带标签的离散值设置全局参数。
        /// </summary>
        public static bool TrySetGlobalParameterByLabel(string name, string label)
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.SetGlobalParameterByNameWithLabel, name, label);
        }

        /// <summary>
        ///     Mutes all playing events at the Studio system level.
        ///     在 Studio 系统级静音所有正在播放的事件。
        /// </summary>
        public static bool TryMuteAllEvents()
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.MuteAllEvents);
        }

        /// <summary>
        ///     Clears system-wide mute.
        ///     清除系统级静音。
        /// </summary>
        public static bool TryUnmuteAllEvents()
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.UnmuteAllEvents);
        }

        /// <summary>
        ///     Pauses all events.
        ///     暂停所有事件。
        /// </summary>
        public static bool TryPauseAllEvents()
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.PauseAllEvents);
        }

        /// <summary>
        ///     Resumes paused events.
        ///     恢复已暂停的事件。
        /// </summary>
        public static bool TryUnpauseAllEvents()
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.UnpauseAllEvents);
        }

        /// <summary>
        ///     Adjusts DSP buffer sizing (advanced; may affect latency).
        ///     调整 DSP buffer 大小（高级；可能影响延迟）。
        /// </summary>
        public static bool TrySetDspBufferSize(int bufferLength, int bufferCount)
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.SetSystemDspBufferSize, bufferLength, bufferCount);
        }

        /// <summary>
        ///     Addon-specific performance payload; inspect in debugger or forward to your telemetry.
        ///     Addon 专用性能载荷；可在调试器中检查，或转发到你的 telemetry。
        /// </summary>
        public static Variant TryGetPerformanceData()
        {
            return FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetPerformanceData)
                ? v
                : default;
        }
    }
}
