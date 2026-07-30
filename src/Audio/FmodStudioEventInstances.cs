using System.Diagnostics.CodeAnalysis;
using Godot;
using STS2RitsuLib.Audio.Internal;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Creates and controls FMOD Studio event or snapshot instances whose start, stop, and release
    ///         lifecycle is managed explicitly.
    ///     </para>
    ///     <para xml:lang="zh-CN">创建并控制需要显式管理启动、停止和释放生命周期的 FMOD Studio 事件或快照实例。</para>
    /// </summary>
    public static class FmodStudioEventInstances
    {
        private static readonly StringName Start = new("start");
        private static readonly StringName Stop = new("stop");
        private static readonly StringName Release = new("release");

        private static bool IsUsable([NotNullWhen(true)] GodotObject? instance)
        {
            return instance is not null && GodotObject.IsInstanceValid(instance);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to create a typed event handle for a path- or GUID-based Studio source.</para>
        ///     <para xml:lang="zh-CN">尝试为基于路径或 GUID 的 Studio 音频源创建类型化事件句柄。</para>
        /// </summary>
        /// <param name="source">
        ///     <para xml:lang="en">The source to create; unsupported source kinds produce no handle.</para>
        ///     <para xml:lang="zh-CN">要创建的音频源；不支持的音频源类型不会生成句柄。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">
        ///         Optional playback metadata. Only its manual-token scope or lifecycle scope is copied to the new
        ///         handle.
        ///     </para>
        ///     <para xml:lang="zh-CN">可选的播放元数据。新句柄只会复制其中手动令牌的作用域或生命周期作用域。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         A typed handle around the valid instance, or null when the source is unsupported or creation
        ///         fails.
        ///     </para>
        ///     <para xml:lang="zh-CN">包装有效实例的类型化句柄；音频源不受支持或创建失败时为 <see langword="null" />。</para>
        /// </returns>
        public static AudioEventHandle? TryCreateHandle(AudioSource source, AudioPlaybackOptions? options = null)
        {
            options ??= new();
            var instance = source switch
            {
                StudioEventSource path => TryCreate(path.Path),
                StudioGuidSource guid => TryCreateFromGuid(guid.Value),
                _ => null,
            };

            return instance is null
                ? null
                : new AudioEventHandle(source, options.ScopeToken?.Scope ?? options.Scope, instance);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to create a valid Studio event or snapshot instance from a path, honoring any
        ///         registered path-to-GUID mapping.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试按路径创建有效的 Studio 事件或快照实例，并采用已注册的路径到 GUID 映射。</para>
        /// </summary>
        /// <param name="eventOrSnapshotPath">
        ///     <para xml:lang="en">The nonblank FMOD Studio event or snapshot path.</para>
        ///     <para xml:lang="zh-CN">非空白的 FMOD Studio 事件或快照路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The valid instance, or null when neither mapped-GUID nor native-path creation succeeds.</para>
        ///     <para xml:lang="zh-CN">有效实例；按映射 GUID 和原生路径均无法创建时为 <see langword="null" />。</para>
        /// </returns>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         When a mapping exists and its GUID is loaded, GUID creation is attempted first. Native path
        ///         creation is used when the mapped GUID is unavailable or creation by GUID fails and the native path is present.
        ///     </para>
        ///     <para xml:lang="zh-CN">存在映射且其 GUID 已加载时，会优先尝试按 GUID 创建。映射 GUID 不可用，或按 GUID 创建失败但原生路径存在时，改用原生路径创建。</para>
        /// </remarks>
        public static GodotObject? TryCreate(string eventOrSnapshotPath)
        {
            if (string.IsNullOrWhiteSpace(eventOrSnapshotPath))
                return null;

            if (!FmodStudioGuidPathTable.TryGetStudioGuidForEventPath(eventOrSnapshotPath, out var mappedGuid))
                return TryCreateByPathOnly(eventOrSnapshotPath);

            var guidInCache = FmodStudioServer.TryCheckEventGuid(mappedGuid) == true;
            var pathInCache = ProbeStudioHasEventPath(eventOrSnapshotPath) == true;

            if (!guidInCache) return pathInCache ? TryCreateByPathOnly(eventOrSnapshotPath) : null;
            var byGuid = TryCreateFromGuid(mappedGuid);
            if (byGuid is not null)
                return byGuid;

            return pathInCache ? TryCreateByPathOnly(eventOrSnapshotPath) : null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Probes the raw <c>FmodServer.check_event_path</c> result without treating a registered GUID
        ///         mapping as proof that the native path exists.
        ///     </para>
        ///     <para xml:lang="zh-CN">直接探测 <c>FmodServer.check_event_path</c>，不会将已注册的 GUID 映射视为原生路径存在的证明。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The event path to probe.</para>
        ///     <para xml:lang="zh-CN">要探测的事件路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> or <see langword="false" /> for a completed probe; null when invocation
        ///         is unavailable or fails.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         探测完成时为 <see langword="true" /> 或 <see langword="false" />；调用不可用或失败时为 <see langword="null" />
        ///         。
        ///     </para>
        /// </returns>
        private static bool? ProbeStudioHasEventPath(string eventPath)
        {
            if (string.IsNullOrWhiteSpace(eventPath))
                return false;

            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CheckEventPath, eventPath))
                return null;

            return v.VariantType == Variant.Type.Bool ? v.AsBool() : null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to create a valid Studio event or snapshot instance from a normalized GUID, using the
        ///         same add-on entry point as its editor tools.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试根据规范化 GUID 创建有效的 Studio 事件或快照实例，使用与插件编辑器工具相同的入口点。</para>
        /// </summary>
        /// <param name="eventGuid">
        ///     <para xml:lang="en">The event or snapshot GUID string.</para>
        ///     <para xml:lang="zh-CN">事件或快照的 GUID 字符串。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The valid instance, or null when the GUID is blank, malformed, known to be absent, or cannot be
        ///         created.
        ///     </para>
        ///     <para xml:lang="zh-CN">有效实例；GUID 为空白、格式错误、已确认不存在或无法创建时为 <see langword="null" />。</para>
        /// </returns>
        public static GodotObject? TryCreateFromGuid(string eventGuid)
        {
            if (string.IsNullOrWhiteSpace(eventGuid))
                return null;

            if (!FmodStudioGuidInterop.TryNormalizeForAddon(eventGuid, out var normalized))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Audio] FMOD create_event_instance_with_guid: invalid GUID string '{eventGuid}' " +
                    $"(GDExtension expects braced format, see fmod-gdextension helpers/common.h string_to_fmod_guid).");
                return null;
            }

            if (FmodStudioServer.TryCheckEventGuid(normalized) == false)
                return null;

            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CreateEventInstanceWithGuid, normalized) ||
                v.VariantType != Variant.Type.Object)
                return null;

            var instance = v.AsGodotObject();
            return IsUsable(instance) ? instance : null;
        }

        private static GodotObject? TryCreateByPathOnly(string eventOrSnapshotPath)
        {
            if (string.IsNullOrWhiteSpace(eventOrSnapshotPath))
                return null;

            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CreateEventInstance,
                    eventOrSnapshotPath) ||
                v.VariantType != Variant.Type.Object)
                return null;

            var instance = v.AsGodotObject();
            return IsUsable(instance) ? instance : null;
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to call <c>start</c> on a valid instance that supports the method.</para>
        ///     <para xml:lang="zh-CN">尝试对支持 <c>start</c> 方法的有效实例调用该方法。</para>
        /// </summary>
        /// <param name="instance">
        ///     <para xml:lang="en">The FMOD event or snapshot instance.</para>
        ///     <para xml:lang="zh-CN">FMOD 事件或快照实例。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when the method invocation completes; otherwise
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">方法调用完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryStart(GodotObject? instance)
        {
            if (!IsUsable(instance) || !instance.HasMethod(Start))
                return false;

            try
            {
                instance.Call(Start);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] FMOD event start: {ex}");
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to stop a valid instance using FMOD's fade-out or immediate stop mode.</para>
        ///     <para xml:lang="zh-CN">尝试使用 FMOD 的淡出或立即停止模式停止有效实例。</para>
        /// </summary>
        /// <param name="instance">
        ///     <para xml:lang="en">The FMOD event or snapshot instance.</para>
        ///     <para xml:lang="zh-CN">FMOD 事件或快照实例。</para>
        /// </param>
        /// <param name="allowFadeOut">
        ///     <para xml:lang="en">
        ///         Whether to use FMOD stop mode <c>0</c> (allow fade-out); <see langword="false" /> uses
        ///         immediate mode <c>1</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">是否使用允许淡出的 FMOD 停止模式 <c>0</c>；为 <see langword="false" /> 时使用立即停止模式 <c>1</c>。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the stop invocation completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">停止调用完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryStop(GodotObject? instance, bool allowFadeOut = true)
        {
            if (!IsUsable(instance) || !instance.HasMethod(Stop))
                return false;

            try
            {
                instance.Call(Stop, allowFadeOut ? 0 : 1);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] FMOD event stop: {ex}");
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to schedule native release for a valid instance, logging a missing method or
        ///         invocation failure.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试为有效实例安排原生资源释放；缺少方法或调用失败时会记录日志。</para>
        /// </summary>
        /// <param name="instance">
        ///     <para xml:lang="en">The FMOD event or snapshot instance; null or invalid instances are ignored.</para>
        ///     <para xml:lang="zh-CN">FMOD 事件或快照实例；为 <see langword="null" /> 或无效时会被忽略。</para>
        /// </param>
        public static void TryRelease(GodotObject? instance)
        {
            TryScheduleRelease(instance);
        }

        internal static bool TryScheduleRelease(GodotObject? instance)
        {
            if (!IsUsable(instance))
                return true;

            if (!instance.HasMethod(Release))
            {
                RitsuLibFramework.Logger.Warn("[Audio] FMOD event release: instance does not expose release.");
                return false;
            }

            try
            {
                instance.Call(Release);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] FMOD event release: {ex}");
                return false;
            }
        }
    }
}
