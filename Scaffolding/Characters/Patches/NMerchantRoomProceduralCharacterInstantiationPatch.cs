using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
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
    /// </summary>
    public class NMerchantRoomProceduralCharacterInstantiationPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NMerchantRoom, List<Player>> PlayersRef =
            AccessTools.FieldRefAccess<NMerchantRoom, List<Player>>("_players");

        private static readonly AccessTools.FieldRef<NMerchantRoom, Control> CharacterContainerRef =
            AccessTools.FieldRefAccess<NMerchantRoom, Control>("_characterContainer");

        private static readonly AccessTools.FieldRef<NMerchantRoom, List<NMerchantCharacter>> PlayerVisualsRef =
            AccessTools.FieldRefAccess<NMerchantRoom, List<NMerchantCharacter>>("_playerVisuals");

        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "n_merchant_room_procedural_character_instantiation";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Instantiate merchant characters via ModWorldSceneVisualNodeFactory when procedural merchant visuals are set";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMerchantRoom), "AfterRoomIsLoaded")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Replaces vanilla layout; returns <see langword="false" /> so the original <c>AfterRoomIsLoaded</c> is skipped.
        /// </summary>
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
                    characterContainer.AddChildSafely(nMerchantCharacter);
                    characterContainer.MoveChild(nMerchantCharacter, 0);
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
