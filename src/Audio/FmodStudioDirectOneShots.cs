using Godot;
using Godot.Collections;
using STS2RitsuLib.Audio.Internal;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Provides fire-and-forget one-shot playback directly through <c>FmodServer</c>, bypassing <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" /> and its game-side routing.</para>
    ///     <para xml:lang="zh-CN">直接通过 <c>FmodServer</c> 提供触发即弃的一次性播放，不经过 <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" /> 及其游戏侧路由。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">Use <see cref="GameFmod.Studio" /> or <see cref="Sts2SfxAlignedFmod" /> when playback should follow the game's SFX routing and guards.</para>
    ///     <para xml:lang="zh-CN">需要遵循游戏音效路由和播放保护时，请使用 <see cref="GameFmod.Studio" /> 或 <see cref="Sts2SfxAlignedFmod" />。</para>
    /// </remarks>
    public static class FmodStudioDirectOneShots
    {
        private static readonly StringName SetVolume = new("set_volume");
        private static readonly StringName SetParameterByName = new("set_parameter_by_name");
        private static readonly StringName Start = new("start");
        private static readonly StringName GetParameters = new("get_parameters");
        private static readonly StringName GetName = new("get_name");

        /// <summary>
        ///     <para xml:lang="en">Attempts to invoke the Godot FMOD addon's path-based one-shot method.</para>
        ///     <para xml:lang="zh-CN">尝试调用 Godot FMOD 插件按路径播放一次性事件的方法。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The nonblank FMOD Studio event path.</para>
        ///     <para xml:lang="zh-CN">非空白的 FMOD Studio 事件路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the add-on method is available and its invocation completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">插件方法可用且调用完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryPlay(string eventPath)
        {
            return !string.IsNullOrWhiteSpace(eventPath) &&
                   FmodStudioGateway.TryCall(FmodStudioMethodNames.PlayOneShot, eventPath);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts path-based one-shot playback with a copied dictionary of initial parameter values.</para>
        ///     <para xml:lang="zh-CN">尝试按路径播放一次性事件，并传入复制后的初始参数值字典。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The nonblank FMOD Studio event path.</para>
        ///     <para xml:lang="zh-CN">非空白的 FMOD Studio 事件路径。</para>
        /// </param>
        /// <param name="parameters">
        ///     <para xml:lang="en">The named parameter values copied into a Godot dictionary.</para>
        ///     <para xml:lang="zh-CN">要复制到 Godot 字典中的命名参数值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the add-on method is available and its invocation completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">插件方法可用且调用完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="parameters" /> is null and <paramref name="eventPath" /> is nonblank.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="eventPath" /> 非空白且 <paramref name="parameters" /> 为 <see langword="null" /> 时抛出。</para>
        /// </exception>
        public static bool TryPlay(string eventPath, IReadOnlyDictionary<string, float> parameters)
        {
            if (string.IsNullOrWhiteSpace(eventPath))
                return false;

            ArgumentNullException.ThrowIfNull(parameters);

            var gd = new Dictionary();
            foreach (var kv in parameters)
                gd[kv.Key] = kv.Value;

            return FmodStudioGateway.TryCall(FmodStudioMethodNames.PlayOneShotWithParams, eventPath, gd);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts one-shot playback using a normalized FMOD Studio event GUID.</para>
        ///     <para xml:lang="zh-CN">尝试使用规范化后的 FMOD Studio 事件 GUID 播放一次性事件。</para>
        /// </summary>
        /// <param name="eventGuid">
        ///     <para xml:lang="en">The event GUID in a format accepted by the FMOD add-on interop layer.</para>
        ///     <para xml:lang="zh-CN">FMOD 插件互操作层可接受格式的事件 GUID。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the GUID is valid, is not known to be absent, and the add-on invocation completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">GUID 有效、未被确认不存在且插件调用完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryPlayUsingGuid(string eventGuid)
        {
            if (FmodStudioGuidInterop.TryNormalizeForAddon(eventGuid, out var normalized))
                return FmodStudioServer.TryCheckEventGuid(normalized) != false &&
                       FmodStudioGateway.TryCall(FmodStudioMethodNames.PlayOneShotUsingGuid, normalized);
            RitsuLibFramework.Logger.Warn($"[Audio] FMOD play_one_shot_using_guid: invalid GUID '{eventGuid}'.");
            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts GUID-based one-shot playback with a copied dictionary of initial parameter values.</para>
        ///     <para xml:lang="zh-CN">尝试按 GUID 播放一次性事件，并传入复制后的初始参数值字典。</para>
        /// </summary>
        /// <param name="eventGuid">
        ///     <para xml:lang="en">The FMOD Studio event GUID to normalize and resolve.</para>
        ///     <para xml:lang="zh-CN">要规范化并解析的 FMOD Studio 事件 GUID。</para>
        /// </param>
        /// <param name="parameters">
        ///     <para xml:lang="en">The named parameter values copied into a Godot dictionary.</para>
        ///     <para xml:lang="zh-CN">要复制到 Godot 字典中的命名参数值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the GUID is valid, is not known to be absent, and the add-on invocation completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">GUID 有效、未被确认不存在且插件调用完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="parameters" /> is null after the GUID passes validation and the event is not known to be absent.</para>
        ///     <para xml:lang="zh-CN">当 GUID 通过验证、事件未被确认不存在，且 <paramref name="parameters" /> 为 <see langword="null" /> 时抛出。</para>
        /// </exception>
        public static bool TryPlayUsingGuid(string eventGuid, IReadOnlyDictionary<string, float> parameters)
        {
            if (!FmodStudioGuidInterop.TryNormalizeForAddon(eventGuid, out var normalized))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Audio] FMOD play_one_shot_using_guid_with_params: invalid GUID '{eventGuid}'.");
                return false;
            }

            if (FmodStudioServer.TryCheckEventGuid(normalized) == false)
                return false;

            ArgumentNullException.ThrowIfNull(parameters);

            var gd = new Dictionary();
            foreach (var kv in parameters)
                gd[kv.Key] = kv.Value;

            return FmodStudioGateway.TryCall(FmodStudioMethodNames.PlayOneShotUsingGuidWithParams, normalized, gd);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts vanilla-aligned one-shot playback for a registered <c>event:/…</c> mapping by filtering parameters, applying volume, starting the instance, and scheduling its release.</para>
        ///     <para xml:lang="zh-CN">尝试为已注册的 <c>event:/…</c> 映射执行与原版一致的一次性播放：筛选参数、应用音量、启动实例并安排释放。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The mapped event path. Its registered GUID is preferred when available; native path creation is the fallback.</para>
        ///     <para xml:lang="zh-CN">已映射的事件路径。存在可用注册 GUID 时优先按 GUID 创建，否则回退到原生路径创建。</para>
        /// </param>
        /// <param name="linearVolume">
        ///     <para xml:lang="en">The unclamped linear volume applied before playback starts.</para>
        ///     <para xml:lang="zh-CN">在播放开始前应用的未钳制线性音量。</para>
        /// </param>
        /// <param name="parameters">
        ///     <para xml:lang="en">The requested parameter values. Names absent from the mapped event description are logged and skipped, matching the game proxy.</para>
        ///     <para xml:lang="zh-CN">请求的参数值。映射事件描述中不存在的名称会被记录并跳过，与游戏代理一致。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when parameter inspection, instance creation, setup, and start all complete; otherwise <see langword="false" />. A created instance is released on either result.</para>
        ///     <para xml:lang="zh-CN">参数检查、实例创建、设置和启动全部完成时为 <see langword="true" />；否则为 <see langword="false" />。只要创建了实例，无论结果如何都会释放。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="parameters" /> is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="parameters" /> 为 <see langword="null" /> 时抛出。</para>
        /// </exception>
        public static bool TryFireOneShotForMappedEventPath(string eventPath, float linearVolume,
            IReadOnlyDictionary<string, float> parameters)
        {
            ArgumentNullException.ThrowIfNull(parameters);

            if (!TryGetMappedEventParameterNames(eventPath, out var validParameters))
                return false;

            var instance = FmodStudioEventInstances.TryCreate(eventPath);
            if (instance is null)
                return false;

            try
            {
                if (!instance.HasMethod(SetVolume) || !instance.HasMethod(Start))
                    return false;

                foreach (var kv in parameters)
                {
                    if (validParameters is null || validParameters.Contains(kv.Key))
                    {
                        if (!instance.HasMethod(SetParameterByName))
                            return false;

                        instance.Call(SetParameterByName, kv.Key, kv.Value);
                    }
                    else
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[Audio] FMOD parameter '{kv.Key}' was not found on mapped event '{eventPath}'.");
                    }
                }

                instance.Call(SetVolume, linearVolume);
                instance.Call(Start);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] FMOD mapped path one-shot: {ex}");
                return false;
            }
            finally
            {
                FmodStudioEventInstances.TryRelease(instance);
            }
        }

        private static bool TryGetMappedEventParameterNames(string eventPath, out HashSet<string>? names)
        {
            names = null;
            if (!FmodStudioGuidPathTable.TryGetStudioGuidForEventPath(eventPath, out var eventGuid))
                return true;

            names = new(StringComparer.Ordinal);
            var description = FmodStudioServer.TryGetEventDescriptionFromGuid(eventGuid);
            if (description is null)
                return true;

            if (!description.HasMethod(GetParameters))
                return false;

            try
            {
                var value = description.Call(GetParameters);
                if (value.VariantType != Variant.Type.Array)
                    return false;

                foreach (var item in value.AsGodotArray())
                {
                    var parameter = item.AsGodotObject();
                    if (parameter is null || !GodotObject.IsInstanceValid(parameter) || !parameter.HasMethod(GetName))
                        continue;

                    names.Add(parameter.Call(GetName).AsString());
                }

                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] FMOD mapped event parameter inspection: {ex}");
                return false;
            }
        }
    }
}
