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
    ///     Reimplements <c>NFakeMerchant.AfterRoomIsLoaded</c> so player booth visuals use the same
    ///     <see cref="NMerchantCharacter" /> path as <see cref="NMerchantRoomProceduralCharacterInstantiationPatch" />
    ///     (including procedural merchant shells and world cue playback).
    /// </summary>
    public class NFakeMerchantProceduralCharacterInstantiationPatch : IPatchMethod
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

        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "n_fake_merchant_procedural_character_instantiation";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Instantiate fake-merchant event player visuals via merchant character path when mod merchant visuals apply";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NFakeMerchant), "AfterRoomIsLoaded")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Replaces vanilla player combat-style visuals layout; returns <see langword="false" /> so the
        ///     original <c>AfterRoomIsLoaded</c> is skipped.
        /// </summary>
        public static bool Prefix(NFakeMerchant __instance)
        {
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
                        ModWorldSceneVisualNodeFactory.TryInstantiateMerchantCharacter(player.Character)
                        ?? RitsuGodotNodeFactories.CreateFromScenePath<NMerchantCharacter>(
                            player.Character.MerchantAnimPath, PackedScene.GenEditState.Disabled);

                    characterContainer.AddChildSafely(nMerchantCharacter);
                    characterContainer.MoveChildSafely(nMerchantCharacter, 0);
                    nMerchantCharacter.Position = new(num2, -50f * i);
                    if (i > 0)
                        nMerchantCharacter.Modulate = new(0.5f, 0.5f, 0.5f);

                    num2 -= 275f;
                    playerVisuals.Add(nMerchantCharacter);
                }
            }

            ModCreatureVisualPlayback.RegisterFakeMerchantPlayerVisuals(screen, playerVisuals, players);
            ApplyMerchantWorldVisuals(players, playerVisuals);

            if (!evt.StartedFight)
                TaskHelper.RunSafely(ShowWelcomeDialogueInvoker(screen));
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
