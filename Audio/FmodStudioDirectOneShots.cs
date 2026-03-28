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
            return FmodStudioGateway.TryCall(FmodStudioMethodNames.PlayOneShotUsingGuid, eventGuid);
        }
    }
}
