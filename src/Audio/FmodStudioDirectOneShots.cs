using Godot;
using Godot.Collections;
using STS2RitsuLib.Audio.Internal;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Fire-and-forget one-shots on <c>FmodServer</c>. These do <b>not</b> go through
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" /> — volume routing may differ from in-game SFX. Prefer
    ///     <see cref="GameFmod.Studio" /> or <see cref="Sts2SfxAlignedFmod" /> for vanilla-aligned playback.
    ///     在 <c>FmodServer</c> 上触发即弃的一次性音效。它们<b>不会</b>经过
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" />，音量路由可能不同于游戏内 SFX。若要与原版一致，优先使用
    ///     <see cref="GameFmod.Studio" /> 或 <see cref="Sts2SfxAlignedFmod" /> 播放。
    /// </summary>
    public static class FmodStudioDirectOneShots
    {
        private static readonly StringName SetVolume = new("set_volume");
        private static readonly StringName SetParameterByName = new("set_parameter_by_name");
        private static readonly StringName Start = new("start");
        private static readonly StringName GetParameters = new("get_parameters");
        private static readonly StringName GetName = new("get_name");

        /// <summary>
        ///     Plays a one-shot by event path via the Godot FMOD addon.
        ///     通过 Godot FMOD addon 按事件路径播放 one-shot。
        /// </summary>
        public static bool TryPlay(string eventPath)
        {
            return !string.IsNullOrWhiteSpace(eventPath) &&
                   FmodStudioGateway.TryCall(FmodStudioMethodNames.PlayOneShot, eventPath);
        }

        /// <summary>
        ///     Plays a one-shot with initial parameter values.
        ///     使用初始参数值播放 one-shot。
        /// </summary>
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
        ///     Plays a one-shot using a Studio event GUID string.
        ///     使用 Studio 事件 GUID 字符串播放 one-shot。
        /// </summary>
        public static bool TryPlayUsingGuid(string eventGuid)
        {
            if (FmodStudioGuidInterop.TryNormalizeForAddon(eventGuid, out var normalized))
                return FmodStudioServer.TryCheckEventGuid(normalized) != false &&
                       FmodStudioGateway.TryCall(FmodStudioMethodNames.PlayOneShotUsingGuid, normalized);
            RitsuLibFramework.Logger.Warn($"[Audio] FMOD play_one_shot_using_guid: invalid GUID '{eventGuid}'.");
            return false;
        }

        /// <summary>
        ///     Plays a one-shot with initial parameter values, using a Studio event GUID string.
        ///     使用初始参数值和 Studio 事件 GUID 字符串播放 one-shot。
        /// </summary>
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
        ///     Mirrors Godot one-shot semantics for a mapped <c>event:/…</c> path: prefers path-based creation (same as
        ///     vanilla proxy), then GUID when needed.
        ///     为映射的 <c>event:/…</c> 路径复现 Godot one-shot 语义：优先按路径创建（与
        ///     原版 proxy 相同），必要时再使用 GUID。
        /// </summary>
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
