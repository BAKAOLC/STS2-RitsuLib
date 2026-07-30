using Godot;
using STS2RitsuLib.Audio.Internal;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Provides guarded access to FMOD Studio bus objects alongside the path-based operations in <see cref="FmodStudioRouting" />.</para>
    ///     <para xml:lang="zh-CN">提供对 FMOD Studio 总线对象的受保护访问，与 <see cref="FmodStudioRouting" /> 的路径型操作相配合。</para>
    /// </summary>
    public static class FmodStudioBusAccess
    {
        private static readonly StringName GetVolume = new("get_volume");
        private static readonly StringName SetVolume = new("set_volume");
        private static readonly StringName SetMute = new("set_mute");
        private static readonly StringName SetPaused = new("set_paused");
        private static readonly StringName BusGetPath = new("get_path");
        private static readonly StringName BusGetStudioGuid = new("get_guid");
        private static readonly StringName BusGetNumericId = new("get_id");

        /// <summary>
        ///     <para xml:lang="en">Tries to resolve a valid FMOD Studio bus object by path.</para>
        ///     <para xml:lang="zh-CN">尝试按路径解析有效的 FMOD Studio 总线对象。</para>
        /// </summary>
        /// <param name="busPath">
        ///     <para xml:lang="en">The FMOD Studio bus path.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 总线路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The valid bus object, or null when lookup fails or returns an invalid object.</para>
        ///     <para xml:lang="zh-CN">有效的总线对象；查找失败或返回无效对象时为 <see langword="null" />。</para>
        /// </returns>
        public static GodotObject? TryGetBus(string busPath)
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetBus, busPath) ||
                v.VariantType != Variant.Type.Object)
                return null;

            var bus = v.AsGodotObject();
            return bus is not null && GodotObject.IsInstanceValid(bus) ? bus : null;
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to read a bus's linear volume, returning zero when lookup or invocation fails.</para>
        ///     <para xml:lang="zh-CN">尝试读取总线的线性音量；查找或调用失败时返回零。</para>
        /// </summary>
        /// <param name="busPath">
        ///     <para xml:lang="en">The FMOD Studio bus path.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 总线路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The reported linear volume, or <c>0</c> when unavailable.</para>
        ///     <para xml:lang="zh-CN">报告的线性音量；不可用时为 <c>0</c>。</para>
        /// </returns>
        public static float TryGetVolume(string busPath)
        {
            var bus = TryGetBus(busPath);
            if (bus is null || !bus.HasMethod(GetVolume))
                return 0f;

            try
            {
                return bus.Call(GetVolume).AsSingle();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] bus get_volume: {ex}");
                return 0f;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to set an unclamped linear volume on a resolved bus.</para>
        ///     <para xml:lang="zh-CN">尝试在解析出的总线上设置未钳制线性音量。</para>
        /// </summary>
        /// <param name="busPath">
        ///     <para xml:lang="en">The FMOD Studio bus path.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 总线路径。</para>
        /// </param>
        /// <param name="linearVolume">
        ///     <para xml:lang="en">The linear volume passed to FMOD.</para>
        ///     <para xml:lang="zh-CN">传递给 FMOD 的线性音量。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the bus supports and completes the operation; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">总线支持并完成该操作时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TrySetVolume(string busPath, float linearVolume)
        {
            var bus = TryGetBus(busPath);
            if (bus is null || !bus.HasMethod(SetVolume))
                return false;

            try
            {
                bus.Call(SetVolume, linearVolume);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] bus set_volume: {ex}");
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to mute or unmute a resolved bus.</para>
        ///     <para xml:lang="zh-CN">尝试将解析出的总线静音或取消静音。</para>
        /// </summary>
        /// <param name="busPath">
        ///     <para xml:lang="en">The FMOD Studio bus path.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 总线路径。</para>
        /// </param>
        /// <param name="muted">
        ///     <para xml:lang="en">Whether the bus should be muted.</para>
        ///     <para xml:lang="zh-CN">总线是否应静音。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the bus supports and completes the operation; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">总线支持并完成该操作时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TrySetMute(string busPath, bool muted)
        {
            var bus = TryGetBus(busPath);
            if (bus is null || !bus.HasMethod(SetMute))
                return false;

            try
            {
                bus.Call(SetMute, muted);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] bus set_mute: {ex}");
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to pause or resume a resolved bus.</para>
        ///     <para xml:lang="zh-CN">尝试暂停或恢复解析出的总线。</para>
        /// </summary>
        /// <param name="busPath">
        ///     <para xml:lang="en">The FMOD Studio bus path.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 总线路径。</para>
        /// </param>
        /// <param name="paused">
        ///     <para xml:lang="en">Whether the bus should be paused.</para>
        ///     <para xml:lang="zh-CN">总线是否应暂停。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the bus supports and completes the operation; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">总线支持并完成该操作时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TrySetPaused(string busPath, bool paused)
        {
            var bus = TryGetBus(busPath);
            if (bus is null || !bus.HasMethod(SetPaused))
                return false;

            try
            {
                bus.Call(SetPaused, paused);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] bus set_paused: {ex}");
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to read the stable FMOD Studio GUID of a bus resolved by path.</para>
        ///     <para xml:lang="zh-CN">尝试读取按路径解析出的总线所对应的稳定 FMOD Studio GUID。</para>
        /// </summary>
        /// <param name="busPath">
        ///     <para xml:lang="en">The FMOD Studio bus path.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 总线路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The GUID string, or null when unavailable.</para>
        ///     <para xml:lang="zh-CN">GUID 字符串；不可用时为 <see langword="null" />。</para>
        /// </returns>
        public static string? TryGetStudioGuid(string busPath)
        {
            var bus = TryGetBus(busPath);
            if (bus is null || !bus.HasMethod(BusGetStudioGuid))
                return null;

            try
            {
                return bus.Call(BusGetStudioGuid).AsString();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] bus get_guid: {ex}");
                return null;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to read an integral 64-bit bus identifier when the FMOD add-on provides <c>get_id</c>.</para>
        ///     <para xml:lang="zh-CN">当 FMOD 插件提供 <c>get_id</c> 时，尝试读取整数型 64 位总线标识符。</para>
        /// </summary>
        /// <param name="busPath">
        ///     <para xml:lang="en">The FMOD Studio bus path.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 总线路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The integer identifier, or null for missing, unsupported, non-integral, non-finite, or out-of-range values.</para>
        ///     <para xml:lang="zh-CN">整数标识符；缺失、不支持、非整数、非有限或超出范围时为 <see langword="null" />。</para>
        /// </returns>
        public static long? TryGetNumericId(string busPath)
        {
            var bus = TryGetBus(busPath);
            if (bus is null)
                return null;

            if (!bus.HasMethod(BusGetNumericId))
                return null;

            try
            {
                var v = bus.Call(BusGetNumericId);
                return v.VariantType switch
                {
                    Variant.Type.Int => v.AsInt64(),
                    Variant.Type.Float => ConvertFloatingNumericId(v.AsDouble()),
                    _ => null,
                };
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] bus get_id: {ex}");
                return null;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Finds the first valid bus whose Studio GUID matches a value case-insensitively.</para>
        ///     <para xml:lang="zh-CN">查找 Studio GUID 与指定值不区分大小写匹配的第一个有效总线。</para>
        /// </summary>
        /// <param name="studioBusGuid">
        ///     <para xml:lang="en">The non-empty FMOD Studio bus GUID to match.</para>
        ///     <para xml:lang="zh-CN">要匹配的非空 FMOD Studio 总线 GUID。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The matching bus path, or null when none can be resolved.</para>
        ///     <para xml:lang="zh-CN">匹配的总线路径；无法解析匹配项时为 <see langword="null" />。</para>
        /// </returns>
        public static string? TryFindBusPathByStudioGuid(string studioBusGuid)
        {
            if (string.IsNullOrWhiteSpace(studioBusGuid))
                return null;

            foreach (var item in FmodStudioServer.TryGetAllBuses())
            {
                if (item.VariantType != Variant.Type.Object)
                    continue;

                var bus = item.AsGodotObject();
                if (bus is null || !GodotObject.IsInstanceValid(bus))
                    continue;
                if (!bus.HasMethod(BusGetStudioGuid) || !bus.HasMethod(BusGetPath))
                    continue;

                try
                {
                    if (!string.Equals(bus.Call(BusGetStudioGuid).AsString(), studioBusGuid,
                            StringComparison.OrdinalIgnoreCase))
                        continue;

                    return bus.Call(BusGetPath).AsString();
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] bus enumerate match: {ex}");
                }
            }

            return null;
        }

        private static long? ConvertFloatingNumericId(double value)
        {
            return !double.IsFinite(value) || value != Math.Truncate(value) ||
                   value < long.MinValue || value >= 9223372036854775808d
                ? null
                : (long)value;
        }
    }
}
