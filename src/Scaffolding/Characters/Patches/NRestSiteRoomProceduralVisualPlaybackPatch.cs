using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     After the rest-site room finishes layout, drives procedural rest visuals (per-act loop cues) for characters
    ///     that use <see cref="IModCharacterAssetOverrides.WorldProceduralVisuals" /><c>.RestSite</c>.
    ///     在休息点房间完成布局后，为使用 <see cref="IModCharacterAssetOverrides.WorldProceduralVisuals" /><c>.RestSite</c> 的角色驱动程序化休息点视觉（按章节
    ///     loop cue）。
    /// </summary>
    internal class NRestSiteRoomProceduralVisualPlaybackPatch : IPatchMethod
    {
        private static readonly ConditionalWeakTable<NRestSiteCharacter, StateMachineSlot> StateMachinesByRoot = new();

        public static string PatchId => "n_rest_site_room_procedural_visual_playback";

        public static string Description =>
            "Apply procedural rest-site frame / texture cues after NRestSiteRoom._Ready";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRestSiteRoom), nameof(NRestSiteRoom._Ready))];
        }

        public static void Postfix(NRestSiteRoom __instance)
        {
            foreach (var c in __instance.Characters)
            {
                var pl = c.Player;
                if (pl?.Character is not { } character)
                    continue;

                var cue = RestSiteActLoopCue(pl.RunState.CurrentActIndex);
                if (TryRouteToStateMachine(c, character, cue))
                    continue;

                if (character is not IModCharacterAssetOverrides
                    {
                        WorldProceduralVisuals.RestSite.CueSet: { } cueSet,
                    })
                    continue;

                ModCreatureVisualPlayback.TryPlayOnVisualRoot(c, character, cue, true, cueSet);
            }
        }

        private static bool TryRouteToStateMachine(NRestSiteCharacter restSiteCharacter, CharacterModel character,
            string anim)
        {
            if (character is not IModCharacterRestSiteAnimationStateMachineFactory factory)
                return false;

            var slot = StateMachinesByRoot.GetValue(restSiteCharacter, _ => new());
            slot.EnsureBuilt(factory, restSiteCharacter, character);

            if (slot.StateMachine == null)
                return false;

            slot.StateMachine.SetTrigger(ModCreatureVisualPlayback.MapWorldAnimationToStateMachineTrigger(anim));
            return true;
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

        private sealed class StateMachineSlot
        {
            private bool _built;
            public ModAnimStateMachine? StateMachine { get; private set; }

            public void EnsureBuilt(IModCharacterRestSiteAnimationStateMachineFactory factory, Node root,
                CharacterModel character)
            {
                if (_built)
                    return;

                _built = true;
                StateMachine = factory.TryCreateRestSiteAnimationStateMachine(root, character);
            }
        }
    }
}
