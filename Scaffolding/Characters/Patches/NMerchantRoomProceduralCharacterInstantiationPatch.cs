using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using STS2RitsuLib.Scaffolding.Godot;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Reimplements <c>NMerchantRoom.AfterRoomIsLoaded</c> so characters with
    ///     <see cref="IModCharacterAssetOverrides.WorldProceduralVisuals" /><c>.Merchant</c> can use in-memory shells
    ///     (mirrors vanilla layout; otherwise loads <c>MerchantAnimPath</c> through
    ///     <see cref="RitsuGodotNodeFactories" /> for baselib-style scenes).
    ///     重新实现 <c>NMerchantRoom.AfterRoomIsLoaded</c>，让带有 <see cref="IModCharacterAssetOverrides.WorldProceduralVisuals" />
    ///     <c>.Merchant</c> 的角色可以使用内存外壳（镜像原版布局；否则通过 <see cref="RitsuGodotNodeFactories" /> 加载 <c>MerchantAnimPath</c> 以支持
    ///     baselib 风格场景）。
    /// </summary>
    internal class NMerchantRoomProceduralCharacterInstantiationPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NMerchantRoom, List<Player>> PlayersRef =
            AccessTools.FieldRefAccess<NMerchantRoom, List<Player>>("_players");

        private static readonly AccessTools.FieldRef<NMerchantRoom, Control> CharacterContainerRef =
            AccessTools.FieldRefAccess<NMerchantRoom, Control>("_characterContainer");

        private static readonly AccessTools.FieldRef<NMerchantRoom, List<NMerchantCharacter>> PlayerVisualsRef =
            AccessTools.FieldRefAccess<NMerchantRoom, List<NMerchantCharacter>>("_playerVisuals");

        public static string PatchId => "n_merchant_room_procedural_character_instantiation";

        public static string Description =>
            "Instantiate merchant characters via ModWorldSceneVisualNodeFactory when procedural merchant visuals are set";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMerchantRoom), "AfterRoomIsLoaded")];
        }

        public static bool Prefix(NMerchantRoom __instance)
        {
            RunAfterRoomIsLoaded(__instance);
            return false;
        }

        private static void RunAfterRoomIsLoaded(NMerchantRoom room)
        {
            var players = PlayersRef(room);
            var characterContainer = CharacterContainerRef(room);
            var playerVisuals = PlayerVisualsRef(room);

            var me = LocalContext.GetMe(players);
            ArgumentNullException.ThrowIfNull(me);
            players.Remove(me);
            players.Insert(0, me);
            var num = Mathf.CeilToInt(Mathf.Sqrt(players.Count));
            for (var i = 0; i < num; i++)
            {
                var num2 = -140f * i;
                for (var j = 0; j < num; j++)
                {
                    var num3 = i * num + j;
                    if (num3 >= players.Count)
                        break;

                    var player = players[num3];
                    var nMerchantCharacter =
                        ModWorldSceneVisualNodeFactory.TryInstantiateMerchantCharacter(player.Character)
                        ?? RitsuGodotNodeFactories.CreateFromScenePath<NMerchantCharacter>(
                            player.Character.MerchantAnimPath, PackedScene.GenEditState.Disabled);
                    RitsuGodotTreeCompat.AddChildSafely(characterContainer, nMerchantCharacter);
                    RitsuGodotTreeCompat.MoveChildSafely(characterContainer, nMerchantCharacter, 0);
                    nMerchantCharacter.Position = new(num2, -50f * i);
                    if (i > 0)
                        nMerchantCharacter.Modulate = new(0.5f, 0.5f, 0.5f);

                    num2 -= 275f;
                    playerVisuals.Add(nMerchantCharacter);
                }
            }

            ApplyMerchantWorldVisuals(players, playerVisuals);
        }

        private static void ApplyMerchantWorldVisuals(IReadOnlyList<Player> players,
            IReadOnlyList<NMerchantCharacter> visuals)
        {
            var n = Math.Min(visuals.Count, players.Count);
            for (var i = 0; i < n; i++)
            {
                var character = players[i].Character;
                if (character is not IModCharacterAssetOverrides
                    {
                        WorldProceduralVisuals.Merchant.CueSet: { } cueSet,
                    })
                    continue;

                ModCreatureVisualPlayback.TryPlayOnVisualRoot(visuals[i], character, "relaxed_loop", true, cueSet);
            }
        }
    }
}
