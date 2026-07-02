using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Lifecycle.Patches;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.PlayerResources.Patches
{
    internal sealed class PlayerCmdGainEnergyHookPatch : IPatchMethod
    {
        public static string PatchId => "player_resource_hook_gain_energy";
        public static string Description => "Dispatch player energy gained hooks";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PlayerCmd), nameof(PlayerCmd.GainEnergy), [typeof(decimal), typeof(Player)])];
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(Player player, out int __state)
        {
            __state = player.PlayerCombatState?.Energy ?? 0;
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Player player, int __state, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(
                __result,
                () => PlayerResourceHook.AfterEnergyGainedIfChanged(player, __state));
        }
    }

    internal sealed class PlayerCmdGainStarsHookPatch : IPatchMethod
    {
        public static string PatchId => "player_resource_hook_gain_stars";
        public static string Description => "Dispatch player stars gained hooks";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PlayerCmd), nameof(PlayerCmd.GainStars), [typeof(decimal), typeof(Player)])];
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(Player player, out int __state)
        {
            __state = player.PlayerCombatState?.Stars ?? 0;
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Player player, int __state, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(
                __result,
                () => PlayerResourceHook.AfterStarsGainedIfChanged(player, __state));
        }
    }
}
