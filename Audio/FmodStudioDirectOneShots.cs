using Godot;
using Godot.Collections;
using STS2RitsuLib.Audio.Internal;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Fire-and-forget one-shots on <c>FmodServer</c>. These do <b>not</b> go through
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" /> — volume routing may differ from in-game SFX. Prefer
    ///     <see cref="GameFmod.Studio" /> or <see cref="Sts2SfxAlignedFmod" /> for vanilla-aligned playback.
    /// </summary>
    public static class FmodStudioDirectOneShots
    {
        private static readonly StringName SetVolume = new("set_volume");
        private static readonly StringName SetParameterByName = new("set_parameter_by_name");
        private static readonly StringName Start = new("start");
        private static readonly StringName Release = new("release");

        /// <summary>
        ///     Plays a one-shot by event path via the Godot FMOD addon.
        /// </summary>
        public static bool TryPlay(string eventPath)
        {
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.PlayOneShot, eventPath);
        }

        /// <summary>
        ///     Plays a one-shot with initial parameter values.
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
