using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     After the rest-site room finishes layout, drives procedural rest visuals (per-act loop cues) for characters
    ///     that use <see cref="IModCharacterAssetOverrides.WorldProceduralVisuals" /><c>.RestSite</c>.
    /// </summary>
    public class NRestSiteRoomProceduralVisualPlaybackPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "n_rest_site_room_procedural_visual_playback";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Apply procedural rest-site frame / texture cues after NRestSiteRoom._Ready";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRestSiteRoom), nameof(NRestSiteRoom._Ready))];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Runs after vanilla <c>_Ready</c> so all rest-site character nodes exist.
        /// </summary>
        public static void Postfix(NRestSiteRoom __instance)
        {
            foreach (var c in __instance.Characters)
            {
                var pl = c.Player;
                if (pl?.Character is not IModCharacterAssetOverrides
                    {
                        WorldProceduralVisuals.RestSite.CueSet: { } cueSet,
                    })
                    continue;

                var cue = RestSiteActLoopCue(pl.RunState.CurrentActIndex);
                ModCreatureVisualPlayback.TryPlayOnVisualRoot(c, pl.Character, cue, true, cueSet);
            }
        }

        private static string RestSiteActLoopCue(int actIndex)
        {
            return actIndex switch
            {
                0 => "overgrowth_loop",
                1 => "hive_loop",
                2 => "glory_loop",
                _ => "overgrowth_loop",
            };
        }
    }
}
