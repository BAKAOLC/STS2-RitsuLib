using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.SecondaryResources.Patches
{
    internal sealed class NCombatUiActivateSecondaryResourcesPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_secondary_resource_combat_ui_activate";
        public static string Description => "Refresh secondary-resource combat UI alongside NCombatUi.Activate";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCombatUi), nameof(NCombatUi.Activate), [typeof(CombatState)])];
        }

        public static void Postfix(NCombatUi __instance, CombatState state)
        {
            if (!ModSecondaryResourceRegistry.HasAny)
                return;

            SecondaryResourceUiRuntime.UpdateCombatUi(__instance, LocalContext.GetMe(state));
        }
    }

    internal sealed class NCombatUiAnimOutSecondaryResourcesPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_secondary_resource_combat_ui_anim_out";
        public static string Description => "Hide secondary-resource combat UI alongside NCombatUi.AnimOut";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCombatUi), nameof(NCombatUi.AnimOut))];
        }

        public static void Postfix(NCombatUi __instance)
        {
            if (ModSecondaryResourceRegistry.HasAny)
                SecondaryResourceUiRuntime.HideCombatUi(__instance);
        }
    }

    internal sealed class NCombatUiDeactivateSecondaryResourcesPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_secondary_resource_combat_ui_deactivate";
        public static string Description => "Hide secondary-resource combat UI alongside NCombatUi.Deactivate";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCombatUi), nameof(NCombatUi.Deactivate))];
        }

        public static void Postfix(NCombatUi __instance)
        {
            if (ModSecondaryResourceRegistry.HasAny)
                SecondaryResourceUiRuntime.HideCombatUi(__instance);
        }
    }

    internal sealed class NMultiplayerPlayerStateCombatSetUpSecondaryResourcesPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_secondary_resource_multiplayer_player_state_combat_set_up";

        public static string Description =>
            "Refresh secondary-resource multiplayer player-state UI alongside NMultiplayerPlayerState.OnCombatSetUp";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMultiplayerPlayerState), "OnCombatSetUp", [typeof(CombatState)], true)];
        }

        public static void Postfix(NMultiplayerPlayerState __instance)
        {
            if (!ModSecondaryResourceRegistry.HasAny)
                return;

            if (LocalContext.IsMe(__instance.Player))
            {
                SecondaryResourceUiRuntime.SetMultiplayerPlayerStateCombatActive(__instance, false);
                return;
            }

            SecondaryResourceUiRuntime.SetMultiplayerPlayerStateCombatActive(__instance, true);
            SecondaryResourceUiRuntime.UpdateMultiplayerPlayerStateUi(__instance);
        }
    }

    internal sealed class NMultiplayerPlayerStateCombatEndedSecondaryResourcesPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_secondary_resource_multiplayer_player_state_combat_ended";

        public static string Description =>
            "Hide secondary-resource multiplayer player-state UI alongside NMultiplayerPlayerState.OnCombatEnded";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMultiplayerPlayerState), "OnCombatEnded", [typeof(CombatRoom)], true)];
        }

        public static void Postfix(NMultiplayerPlayerState __instance)
        {
            if (ModSecondaryResourceRegistry.HasAny)
                SecondaryResourceUiRuntime.SetMultiplayerPlayerStateCombatActive(__instance, false);
        }
    }

    internal sealed class NCardUpdateVisualsSecondaryResourceCardUiPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_secondary_resource_card_ui_update";
        public static string Description => "Refresh secondary-resource card UI alongside NCard.UpdateVisuals";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCard), nameof(NCard.UpdateVisuals), [typeof(PileType), typeof(CardPreviewMode)])];
        }

        public static void Postfix(NCard __instance)
        {
            if (!ModSecondaryResourceRegistry.HasAny ||
                __instance.Model == null)
                return;

            SecondaryResourceUiRuntime.UpdateCardUi(__instance, __instance.Model);
        }
    }
}
