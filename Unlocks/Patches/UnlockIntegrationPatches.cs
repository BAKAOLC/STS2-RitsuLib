using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Unlocks.Patches
{
    public class CharacterUnlockFilterPatch : IPatchMethod
    {
        public static string PatchId => "character_unlock_filter";
        public static string Description => "Filter locked mod characters from UnlockState.Characters";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(UnlockState), "get_Characters")];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(UnlockState __instance, ref IEnumerable<CharacterModel> __result)
            // ReSharper restore InconsistentNaming
        {
            __result = ModUnlockRegistry.FilterUnlocked(__result, __instance);
        }
    }

    public class SharedAncientUnlockFilterPatch : IPatchMethod
    {
        public static string PatchId => "shared_ancient_unlock_filter";
        public static string Description => "Filter locked mod shared ancients from UnlockState.SharedAncients";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(UnlockState), "get_SharedAncients")];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(UnlockState __instance, ref IEnumerable<AncientEventModel> __result)
            // ReSharper restore InconsistentNaming
        {
            __result = ModUnlockRegistry.FilterUnlocked(__result, __instance);
        }
    }

    public class CardUnlockFilterPatch : IPatchMethod
    {
        public static string PatchId => "card_unlock_filter";
        public static string Description => "Filter locked mod cards from pool unlock results";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardPoolModel), nameof(CardPoolModel.GetUnlockedCards),
                    [typeof(UnlockState), typeof(CardMultiplayerConstraint)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(UnlockState unlockState, ref IEnumerable<CardModel> __result)
        {
            __result = ModUnlockRegistry.FilterUnlocked(__result, unlockState);
        }
    }

    public class RelicUnlockFilterPatch : IPatchMethod
    {
        public static string PatchId => "relic_unlock_filter";
        public static string Description => "Filter locked mod relics from pool unlock results";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RelicPoolModel), nameof(RelicPoolModel.GetUnlockedRelics), [typeof(UnlockState)])];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(UnlockState unlockState, ref IEnumerable<RelicModel> __result)
        {
            __result = ModUnlockRegistry.FilterUnlocked(__result, unlockState);
        }
    }

    public class PotionUnlockFilterPatch : IPatchMethod
    {
        public static string PatchId => "potion_unlock_filter";
        public static string Description => "Filter locked mod potions from pool unlock results";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PotionPoolModel), nameof(PotionPoolModel.GetUnlockedPotions), [typeof(UnlockState)])];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(UnlockState unlockState, ref IEnumerable<PotionModel> __result)
        {
            __result = ModUnlockRegistry.FilterUnlocked(__result, unlockState);
        }
    }

    public class GeneratedRoomEventUnlockFilterPatch : IPatchMethod
    {
        public static string PatchId => "generated_room_event_unlock_filter";
        public static string Description => "Remove locked mod events after act rooms are generated";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ActModel), nameof(ActModel.GenerateRooms), [typeof(Rng), typeof(UnlockState), typeof(bool)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(ActModel __instance, UnlockState unlockState)
        {
            var roomsField = typeof(ActModel).GetField("_rooms", BindingFlags.Instance | BindingFlags.NonPublic)
                             ?? throw new MissingFieldException(typeof(ActModel).FullName, "_rooms");
            var roomSet = (RoomSet)roomsField.GetValue(__instance)!;
            var originalEvents = roomSet.events.ToArray();
            var filteredEvents = originalEvents
                .Where(eventModel => ModUnlockRegistry.IsUnlocked(eventModel, unlockState))
                .ToArray();

            if (filteredEvents.Length == originalEvents.Length)
                return;

            if (filteredEvents.Length == 0)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Unlocks] Filtering generated events for act '{__instance.Id}' would leave the pool empty. " +
                    "Keeping the unfiltered pool to avoid a hard failure; ensure at least one event remains unlocked.");
                return;
            }

            roomSet.events.Clear();
            roomSet.events.AddRange(filteredEvents);
        }
    }
}
