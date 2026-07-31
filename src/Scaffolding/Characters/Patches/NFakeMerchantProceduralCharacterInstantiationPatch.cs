using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using STS2RitsuLib.Scaffolding.Godot;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Reimplements <c>NFakeMerchant.AfterRoomIsLoaded</c> when any player has a RitsuLib merchant-visual
    ///         override. Player booth visuals are then created as <see cref="NMerchantCharacter" /> nodes through
    ///         <see cref="NMerchantRoomProceduralCharacterInstantiationPatch" />, including procedural visuals and
    ///         world-animation cues.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         当任一玩家具有 RitsuLib 商人形象替换时，重新实现 <c>NFakeMerchant.AfterRoomIsLoaded</c>。随后通过
    ///         <see cref="NMerchantRoomProceduralCharacterInstantiationPatch" /> 将所有玩家的摊位形象创建为
    ///         <see cref="NMerchantCharacter" /> 节点，并支持程序化形象和世界场景动画提示。
    ///     </para>
    /// </summary>
    internal class NFakeMerchantProceduralCharacterInstantiationPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NFakeMerchant, List<Player>> PlayersRef =
            AccessTools.FieldRefAccess<NFakeMerchant, List<Player>>("_players");

        private static readonly AccessTools.FieldRef<NFakeMerchant, Control> CharacterContainerRef =
            AccessTools.FieldRefAccess<NFakeMerchant, Control>("_characterContainer");

        private static readonly AccessTools.FieldRef<NFakeMerchant, FakeMerchant> EventRef =
            AccessTools.FieldRefAccess<NFakeMerchant, FakeMerchant>("_event");

        private static readonly Func<NFakeMerchant, Task> ShowWelcomeDialogueInvoker =
            AccessTools.MethodDelegate<Func<NFakeMerchant, Task>>(
                AccessTools.Method(typeof(NFakeMerchant), "ShowWelcomeDialogue"));

        public static string PatchId => "n_fake_merchant_procedural_character_instantiation";

        public static string Description =>
            "Instantiate fake-merchant event player visuals via merchant character path when mod merchant visuals apply";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NFakeMerchant), "AfterRoomIsLoaded")];
        }

        public static bool Prefix(NFakeMerchant __instance)
        {
            if (!PlayersRef(__instance).Any(static player =>
                    NMerchantRoomProceduralCharacterInstantiationPatch.HasRitsuMerchantVisualOverride(
                        player.Character)))
                return true;

            RunAfterRoomIsLoaded(__instance);
            return false;
        }

        private static void RunAfterRoomIsLoaded(NFakeMerchant screen)
        {
            var players = PlayersRef(screen);
            var characterContainer = CharacterContainerRef(screen);
            var evt = EventRef(screen);

            var me = LocalContext.GetMe(players);
            ArgumentNullException.ThrowIfNull(me);
            players.Remove(me);
            players.Insert(0, me);

            var playerVisuals = new List<NMerchantCharacter>();
            var num = Mathf.CeilToInt(Mathf.Sqrt(players.Count));
            for (var i = 0; i < num; i++)
            {
                var num2 = -75f * i;
                for (var j = 0; j < num; j++)
                {
                    var num3 = i * num + j;
                    if (num3 >= players.Count)
                        break;

                    var player = players[num3];
                    var nMerchantCharacter =
                        NMerchantRoomProceduralCharacterInstantiationPatch.CreateMerchantCharacter(player.Character);

                    RitsuGodotTreeCompat.AddChildSafely(characterContainer, nMerchantCharacter);
                    RitsuGodotTreeCompat.MoveChildSafely(characterContainer, nMerchantCharacter, 0);
                    nMerchantCharacter.Position = new(num2, -50f * i);
                    if (i > 0)
                        nMerchantCharacter.Modulate = new(0.5f, 0.5f, 0.5f);

                    num2 -= 275f;
                    playerVisuals.Add(nMerchantCharacter);
                }
            }

            ModCreatureVisualPlayback.RegisterFakeMerchantPlayerVisuals(screen, playerVisuals, players);

            if (!evt.StartedFight)
                TaskHelper.RunSafely(ShowWelcomeDialogueInvoker(screen));
        }
    }
}
