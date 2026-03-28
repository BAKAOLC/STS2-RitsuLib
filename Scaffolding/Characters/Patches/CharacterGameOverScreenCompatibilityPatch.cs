using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Replaces <c>MoveCreaturesToDifferentLayerAndDisableUi</c> so game-over layout works for mod characters that
    ///     use non-Spine or differently structured combat visuals.
    /// </summary>
    public class CharacterGameOverScreenCompatibilityPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NGameOverScreen, RunState> RunStateRef =
            AccessTools.FieldRefAccess<NGameOverScreen, RunState>("_runState");

        private static readonly AccessTools.FieldRef<NGameOverScreen, Control> CreatureContainerRef =
            AccessTools.FieldRefAccess<NGameOverScreen, Control>("_creatureContainer");

        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "character_game_over_screen_compatibility";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Handle non-Spine mod character visuals when GameOverScreen recreates player visuals";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NGameOverScreen), "MoveCreaturesToDifferentLayerAndDisableUi")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Runs a safe reimplementation of creature relocation; returns <c>false</c> to skip the original method on
        ///     success, or <c>true</c> to fall back if an unexpected error occurs.
        /// </summary>
        public static bool Prefix(NGameOverScreen __instance)
            // ReSharper restore InconsistentNaming
        {
            try
            {
                MoveCreaturesToDifferentLayerAndDisableUiSafe(__instance);
                return false;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error(
                    $"[Visuals] GameOverScreen compatibility patch failed, falling back to base implementation: {ex}");
                return true;
            }
        }

        private static void MoveCreaturesToDifferentLayerAndDisableUiSafe(NGameOverScreen screen)
        {
            var runState = RunStateRef(screen);
            var creatureContainer = CreatureContainerRef(screen);

            List<NCreatureVisuals> visuals = [];
            List<NCreature> creatureNodes;

            if (NCombatRoom.Instance != null)
            {
                if (NCombatRoom.Instance.Mode == CombatRoomMode.ActiveCombat)
                    if (runState.CurrentRoom is CombatRoom currentCombatRoom)
                        NCombatUiAnimOutCompat.AnimOutForGameOver(NCombatRoom.Instance.Ui, currentCombatRoom);

                creatureNodes = NCombatRoom.Instance.CreatureNodes.ToList();
                visuals = creatureNodes.Select(creature => creature.Visuals).ToList();
            }
            else if (NMerchantRoom.Instance != null)
            {
                creatureNodes = [];
                foreach (var playerVisual in NMerchantRoom.Instance.PlayerVisuals)
                {
                    playerVisual.PlayAnimation("die");
                    playerVisual.Reparent(creatureContainer);
                }
            }
            else if (NRestSiteRoom.Instance != null)
            {
                creatureNodes = [];

                foreach (var player in runState.Players)
                {
                    var creatureVisuals = player.Creature.CreateVisuals();
                    if (creatureVisuals == null)
                        continue;

                    visuals.Add(creatureVisuals);
                    creatureContainer.AddChildSafely(creatureVisuals);
                    TryPlayDeathAnimation(creatureVisuals, player);

                    var characterForPlayer = NRestSiteRoom.Instance.GetCharacterForPlayer(player);
                    if (characterForPlayer == null)
                        continue;

                    creatureVisuals.GlobalPosition = characterForPlayer.GlobalPosition;
                    creatureVisuals.Scale = characterForPlayer.Scale;
                    characterForPlayer.Visible = false;

                    var offset = new Vector2(100f, 100f);
                    creatureVisuals.Position += offset *
                                                new Vector2(Math.Sign(creatureVisuals.Scale.X),
                                                    Math.Sign(creatureVisuals.Scale.Y));
                }
            }
            else
            {
                creatureNodes = [];

                foreach (var player in runState.Players)
                {
                    var creatureVisuals = player.Creature.CreateVisuals();
                    if (creatureVisuals == null)
                        continue;

                    visuals.Add(creatureVisuals);
                    creatureContainer.AddChildSafely(creatureVisuals);
                    TryPlayDeathAnimation(creatureVisuals, player);
                }

                if (visuals.Count > 0)
                {
                    var spacing = visuals.Count == 1
                        ? 0f
                        : Math.Min(250f, (screen.Size.X - 200f) / (visuals.Count - 1));

                    var startOffset = (visuals.Count - 1) * (0f - spacing) * 0.5f;
                    foreach (var creatureVisual in visuals)
                    {
                        creatureVisual.Position = creatureContainer.Size * 0.5f + new Vector2(startOffset, 200f);
                        startOffset += spacing;
                    }
                }
            }

            creatureNodes.Sort((left, right) => left.GetIndex().CompareTo(right.GetIndex()));
            foreach (var creatureNode in creatureNodes)
            {
                creatureNode.AnimHideIntent();
                creatureNode.AnimDisableUi();
            }

            foreach (var creatureVisual in visuals.Where(creatureVisual =>
                         creatureVisual.GetParent() != creatureContainer))
                creatureVisual.Reparent(creatureContainer);
        }

        private static void TryPlayDeathAnimation(NCreatureVisuals creatureVisuals, Player player)
        {
            if (!GodotObject.IsInstanceValid(creatureVisuals))
                return;

            try
            {
                creatureVisuals.SpineBody?.GetAnimationState().SetAnimation("die", false);
            }
            catch (Exception ex)
            {
                var characterId = player.Character?.Id.ToString() ?? "<unknown>";
                RitsuLibFramework.Logger.Warn(
                    $"[Visuals] Failed to play game-over death animation for character {characterId}: {ex.Message}");
            }
        }
    }
}
