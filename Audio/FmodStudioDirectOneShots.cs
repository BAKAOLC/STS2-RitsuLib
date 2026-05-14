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
        private static readonly StringName Release = new("release");

        /// <summary>
        ///     Plays a one-shot by event path via the Godot FMOD addon.
        ///     通过 Godot FMOD addon 按事件路径播放 one-shot。
        /// </summary>
        public static bool TryPlay(string eventPath)
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.PlayOneShot, eventPath);
        }

        /// <summary>
        ///     Plays a one-shot with initial parameter values.
        ///     使用初始参数值播放 one-shot。
        /// </summary>
        public static bool TryPlay(string eventPath, IReadOnlyDictionary<string, float> parameters)
        {
            var server = FmodStudioGateway.TryGetServer();
            if (server is null)
                return false;

            var gd = new Dictionary();
            foreach (var kv in parameters)
                gd[kv.Key] = kv.Value;

            try
            {
                server.Call(FmodStudioMethodNames.PlayOneShotWithParams, eventPath, gd);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] FMOD play_one_shot_with_params: {ex.Message}");
                return false;
            }
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

            var server = FmodStudioGateway.TryGetServer();
            if (server is null)
                return false;

            var gd = new Dictionary();
            foreach (var kv in parameters)
                gd[kv.Key] = kv.Value;

            try
            {
                server.Call(FmodStudioMethodNames.PlayOneShotUsingGuidWithParams, normalized, gd);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] FMOD play_one_shot_using_guid_with_params: {ex.Message}");
                return false;
            }
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
            var instance = FmodStudioEventInstances.TryCreate(eventPath);
            if (instance is null)
                return false;

            try
            {
                instance.Call(SetVolume, linearVolume);
                foreach (var kv in parameters)
                    instance.Call(SetParameterByName, kv.Key, kv.Value);

                instance.Call(Start);
                instance.Call(Release);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] FMOD mapped path one-shot: {ex.Message}");
                return false;
            }
        }
    }
}
