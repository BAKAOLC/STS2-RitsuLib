using Godot;
using STS2RitsuLib.Audio.Internal;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Provides guarded access to FMOD Studio global parameters, system-wide event state, DSP buffer settings, and performance data.</para>
    ///     <para xml:lang="zh-CN">提供对 FMOD Studio 全局参数、系统级事件状态、DSP 缓冲区设置和性能数据的受保护访问。</para>
    /// </summary>
    public static class FmodStudioMixerGlobals
    {
        private static readonly StringName DspSettingsClass = new("FmodDspSettings");
        private static readonly StringName SetDspBufferSize = new("set_dsp_buffer_size");
        private static readonly StringName SetDspBufferCount = new("set_dsp_buffer_count");

        /// <summary>
        ///     <para xml:lang="en">Attempts to set a named global parameter to a numeric value.</para>
        ///     <para xml:lang="zh-CN">尝试将命名全局参数设置为数值。</para>
        /// </summary>
        /// <param name="name">
        ///     <para xml:lang="en">The FMOD Studio global parameter name.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 全局参数名称。</para>
        /// </param>
        /// <param name="value">
        ///     <para xml:lang="en">The numeric value passed to FMOD.</para>
        ///     <para xml:lang="zh-CN">传递给 FMOD 的数值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the add-on method is available and invocation completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">插件方法可用且调用完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TrySetGlobalParameter(string name, float value)
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.SetGlobalParameterByName, name, value);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to read a named global parameter as a numeric value.</para>
        ///     <para xml:lang="zh-CN">尝试以数值形式读取命名全局参数。</para>
        /// </summary>
        /// <param name="name">
        ///     <para xml:lang="en">The FMOD Studio global parameter name.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 全局参数名称。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The reported floating-point or integer value converted to <see cref="float" />, or <c>0</c> when the call fails or returns another Variant type.</para>
        ///     <para xml:lang="zh-CN">报告的浮点值，或转换为 <see cref="float" /> 的整数值；调用失败或返回其他 Variant 类型时为 <c>0</c>。</para>
        /// </returns>
        public static float TryGetGlobalParameter(string name)
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetGlobalParameterByName, name))
                return 0f;

            return v.VariantType switch
            {
                Variant.Type.Float => v.AsSingle(),
                Variant.Type.Int => v.AsInt64(),
                _ => 0f,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to set a named global parameter by its labeled discrete value.</para>
        ///     <para xml:lang="zh-CN">尝试使用离散值标签设置命名全局参数。</para>
        /// </summary>
        /// <param name="name">
        ///     <para xml:lang="en">The FMOD Studio global parameter name.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 全局参数名称。</para>
        /// </param>
        /// <param name="label">
        ///     <para xml:lang="en">The labeled parameter value.</para>
        ///     <para xml:lang="zh-CN">参数值标签。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the add-on method is available and invocation completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">插件方法可用且调用完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TrySetGlobalParameterByLabel(string name, string label)
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.SetGlobalParameterByNameWithLabel, name, label);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to mute all events through the Studio system's master bus.</para>
        ///     <para xml:lang="zh-CN">尝试通过 Studio 系统的主总线将所有事件静音。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the add-on invocation completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">插件调用完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryMuteAllEvents()
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.MuteAllEvents);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to clear system-wide event muting.</para>
        ///     <para xml:lang="zh-CN">尝试取消系统级事件静音。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the add-on invocation completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">插件调用完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryUnmuteAllEvents()
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.UnmuteAllEvents);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to pause every event instance tracked by the FMOD add-on.</para>
        ///     <para xml:lang="zh-CN">尝试暂停 FMOD 插件跟踪的所有事件实例。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the add-on invocation completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">插件调用完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryPauseAllEvents()
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.PauseAllEvents);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to resume every event instance tracked by the FMOD add-on.</para>
        ///     <para xml:lang="zh-CN">尝试恢复 FMOD 插件跟踪的所有事件实例。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the add-on invocation completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">插件调用完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryUnpauseAllEvents()
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.UnpauseAllEvents);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to apply positive DSP buffer dimensions through a dynamically created <c>FmodDspSettings</c> resource.</para>
        ///     <para xml:lang="zh-CN">尝试通过动态创建的 <c>FmodDspSettings</c> 资源应用正数 DSP 缓冲区参数。</para>
        /// </summary>
        /// <param name="bufferLength">
        ///     <para xml:lang="en">The positive DSP buffer length in samples.</para>
        ///     <para xml:lang="zh-CN">正数 DSP 缓冲区长度（采样数）。</para>
        /// </param>
        /// <param name="bufferCount">
        ///     <para xml:lang="en">The positive number of DSP buffers.</para>
        ///     <para xml:lang="zh-CN">正数 DSP 缓冲区数量。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the settings resource and add-on method are available and invocation completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">设置资源和插件方法可用且调用完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TrySetDspBufferSize(int bufferLength, int bufferCount)
        {
            if (bufferLength <= 0 || bufferCount <= 0)
                return false;

            try
            {
                if (!ClassDB.CanInstantiate(DspSettingsClass))
                    return false;

                var value = ClassDB.Instantiate(DspSettingsClass);
                if (value.VariantType != Variant.Type.Object)
                    return false;

                var settings = value.AsGodotObject();
                if (settings is null || !GodotObject.IsInstanceValid(settings) ||
                    !settings.HasMethod(SetDspBufferSize) || !settings.HasMethod(SetDspBufferCount))
                    return false;

                settings.Call(SetDspBufferSize, bufferLength);
                settings.Call(SetDspBufferCount, bufferCount);
                return FmodStudioGateway.TryCall(FmodStudioMethodNames.SetSystemDspBufferSize, settings);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] FMOD DSP buffer settings: {ex}");
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to retrieve the add-on-specific FMOD performance-data resource.</para>
        ///     <para xml:lang="zh-CN">尝试获取 FMOD 插件特有的性能数据资源。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The add-on payload, or a default nil Variant when unavailable.</para>
        ///     <para xml:lang="zh-CN">插件返回的数据；不可用时为默认的 nil Variant。</para>
        /// </returns>
        public static Variant TryGetPerformanceData()
        {
            return FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetPerformanceData)
                ? v
                : default;
        }
    }
}
