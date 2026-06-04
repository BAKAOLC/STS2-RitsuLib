using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Models.Identity.Patches
{
    internal sealed class ModModelIdentityRunStateCreatePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_model_identity_run_state_create";
        public static bool IsCritical => false;
        public static string Description => "Reset runtime model identities when a run state is created";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RunState), nameof(RunState.CreateForNewRun),
                [
                    typeof(IReadOnlyList<Player>), typeof(IReadOnlyList<ActModel>),
                    typeof(IReadOnlyList<ModifierModel>), typeof(GameMode), typeof(int), typeof(string),
                ], true),
                new(typeof(RunState), nameof(RunState.FromSerializable), [typeof(SerializableRun)], true),
                new(typeof(RunState), nameof(RunState.CreateForTest),
                [
                    typeof(IReadOnlyList<Player>), typeof(IReadOnlyList<ActModel>),
                    typeof(IReadOnlyList<ModifierModel>), typeof(GameMode), typeof(int), typeof(string),
                ], true),
            ];
        }

        public static void Prefix()
        {
            ModModelIdentityRegistry.Clear();
        }
    }

    internal sealed class ModModelIdentityPlayerRunStatePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_model_identity_player_run_state";
        public static bool IsCritical => false;
        public static string Description => "Register player inventory identities after RunState assignment";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Player), "set_RunState", [typeof(IRunState)], true)];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(Player __instance, IRunState value)
        {
            if (value is not NullRunState)
                ModModelIdentityRegistry.RegisterPlayerInventory(__instance);
        }
    }

    internal sealed class ModModelIdentityRunStateAddCardPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_model_identity_run_state_add_card";
        public static bool IsCritical => false;
        public static string Description => "Register card identities when cards enter RunState";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RunState), nameof(RunState.AddCard), [typeof(CardModel), typeof(Player)], true),
                new(typeof(RunState), "AddCard", [typeof(CardModel)], true),
            ];
        }

        public static void Postfix(CardModel card)
        {
            ModModelIdentityRegistry.RegisterCardTree(card);
        }
    }

    internal sealed class ModModelIdentityCombatStateAddCardPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_model_identity_combat_state_add_card";
        public static bool IsCritical => false;
        public static string Description => "Register card identities when cards enter CombatState";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CombatState), nameof(CombatState.AddCard), [typeof(CardModel), typeof(Player)], true),
                new(typeof(CombatState), "AddCard", [typeof(CardModel)], true),
            ];
        }

        public static void Postfix(CardModel card)
        {
            ModModelIdentityRegistry.RegisterCardTree(card);
        }
    }

    internal sealed class ModModelIdentityPlayerAddRelicPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_model_identity_player_add_relic";
        public static bool IsCritical => false;
        public static string Description => "Register relic identities through player relic ownership";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Player), nameof(Player.AddRelicInternal),
                    [typeof(RelicModel), typeof(int), typeof(bool)], true),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(Player __instance, RelicModel relic)
        {
            if (__instance.RunState is not NullRunState)
                ModModelIdentityRegistry.EnsureRegistered(relic);
        }
    }

    internal sealed class ModModelIdentityPlayerRemoveRelicPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_model_identity_player_remove_relic";
        public static bool IsCritical => false;
        public static string Description => "Unregister relic identities when relic ownership is removed";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Player), nameof(Player.RemoveRelicInternal), [typeof(RelicModel), typeof(bool)], true)];
        }

        public static void Prefix(RelicModel relic)
        {
            ModModelIdentityRegistry.Unregister(relic);
        }
    }

    internal sealed class ModModelIdentityPlayerAddPotionPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_model_identity_player_add_potion";
        public static bool IsCritical => false;
        public static string Description => "Register potion identities through player potion ownership";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Player), nameof(Player.AddPotionInternal),
                    [typeof(PotionModel), typeof(int), typeof(bool)], true),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(Player __instance, PotionModel potion, PotionProcureResult __result)
            // ReSharper restore InconsistentNaming
        {
            if (__result.success && __instance.RunState is not NullRunState)
                ModModelIdentityRegistry.EnsureRegistered(potion);
        }
    }

    internal sealed class ModModelIdentityPlayerRemovePotionPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_model_identity_player_remove_potion";
        public static bool IsCritical => false;
        public static string Description => "Unregister potion identities when potion ownership is removed";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Player), "RemovePotionInternal", [typeof(PotionModel)], true)];
        }

        public static void Prefix(PotionModel potion)
        {
            ModModelIdentityRegistry.Unregister(potion);
        }
    }

    internal sealed class ModModelIdentityPowerApplyPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_model_identity_power_apply";
        public static bool IsCritical => false;
        public static string Description => "Register power identities through power ownership";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PowerModel), nameof(PowerModel.ApplyInternal),
                    [typeof(Creature), typeof(decimal), typeof(bool)], true),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(PowerModel __instance, decimal amount)
        {
            if (amount != 0)
                ModModelIdentityRegistry.EnsureRegistered(__instance);
        }
    }

    internal sealed class ModModelIdentityPowerRemovePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_model_identity_power_remove";
        public static bool IsCritical => false;
        public static string Description => "Unregister power identities when power ownership is removed";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PowerModel), nameof(PowerModel.RemoveInternal), [], true)];
        }

        // ReSharper disable once InconsistentNaming
        public static void Prefix(PowerModel __instance)
        {
            ModModelIdentityRegistry.Unregister(__instance);
        }
    }

    internal sealed class ModModelIdentityEnchantmentPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_model_identity_enchantment";
        public static bool IsCritical => false;
        public static string Description => "Register enchantment identities when enchantments are applied";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(EnchantmentModel), nameof(EnchantmentModel.ApplyInternal),
                    [typeof(CardModel), typeof(decimal)], true),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(EnchantmentModel __instance)
        {
            ModModelIdentityRegistry.EnsureRegistered(__instance);
        }
    }

    internal sealed class ModModelIdentityAfflictionPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_model_identity_affliction";
        public static bool IsCritical => false;
        public static string Description => "Register affliction identities when afflictions are attached to cards";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AfflictionModel), "set_Card", [typeof(CardModel)], true)];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(AfflictionModel __instance)
        {
            ModModelIdentityRegistry.EnsureRegistered(__instance);
        }
    }

    internal sealed class ModModelIdentityCombatStateAddCreaturePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_model_identity_combat_state_add_creature";
        public static bool IsCritical => false;
        public static string Description => "Register monster identities when creatures enter combat";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CombatState), nameof(CombatState.AddCreature), [typeof(Creature)], true)];
        }

        public static void Postfix(Creature creature)
        {
            ModModelIdentityRegistry.EnsureRegistered(creature.Monster);
        }
    }

    internal sealed class ModModelIdentityCombatStateRemoveCreaturePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_model_identity_combat_state_remove_creature";
        public static bool IsCritical => false;
        public static string Description => "Unregister monster identities when creatures leave combat";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CombatState), nameof(CombatState.RemoveCreature), [typeof(Creature), typeof(bool)], true),
            ];
        }

        public static void Prefix(Creature creature)
        {
            ModModelIdentityRegistry.Unregister(creature.Monster);
        }
    }

    internal sealed class ModModelIdentityPlayerSyncPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_model_identity_player_sync";
        public static bool IsCritical => false;
        public static string Description => "Transfer runtime model identities across serialized player replacement";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Player), nameof(Player.SyncWithSerializedPlayer), [typeof(SerializablePlayer)], true)];
        }

        // ReSharper disable InconsistentNaming
        public static void Prefix(Player __instance,
                out ModModelIdentityRegistry.PlayerInventoryIdentitySnapshot __state)
            // ReSharper restore InconsistentNaming
        {
            __state = ModModelIdentityRegistry.CapturePlayerInventory(__instance);
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(Player __instance,
                ModModelIdentityRegistry.PlayerInventoryIdentitySnapshot __state)
            // ReSharper restore InconsistentNaming
        {
            ModModelIdentityRegistry.RestorePlayerInventory(__instance, __state);
        }
    }
}
