using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     Filters locked mod characters out of <c>UnlockState.Characters</c>.
    /// </summary>
    public class CharacterUnlockFilterPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_unlock_filter";

        /// <inheritdoc />
        public static string Description => "Filter locked mod characters from UnlockState.Characters";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(UnlockState), "get_Characters")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Replaces the character enumeration with unlock-filtered results.
        /// </summary>
        public static void Postfix(UnlockState __instance, ref IEnumerable<CharacterModel> __result)
            // ReSharper restore InconsistentNaming
        {
            __result = ModUnlockRegistry.FilterUnlocked(__result, __instance);
        }
    }

    /// <summary>
    ///     Filters locked mod shared ancients out of <c>UnlockState.SharedAncients</c>.
    /// </summary>
    public class SharedAncientUnlockFilterPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "shared_ancient_unlock_filter";

        /// <inheritdoc />
        public static string Description => "Filter locked mod shared ancients from UnlockState.SharedAncients";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(UnlockState), "get_SharedAncients")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Replaces the shared ancient enumeration with unlock-filtered results.
        /// </summary>
        public static void Postfix(UnlockState __instance, ref IEnumerable<AncientEventModel> __result)
            // ReSharper restore InconsistentNaming
        {
            __result = ModUnlockRegistry.FilterUnlocked(__result, __instance);
        }
    }

    /// <summary>
    ///     Filters locked mod cards from <c>CardPoolModel.GetUnlockedCards</c> results.
    /// </summary>
    public class CardUnlockFilterPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "card_unlock_filter";

        /// <inheritdoc />
        public static string Description => "Filter locked mod cards from pool unlock results";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardPoolModel), nameof(CardPoolModel.GetUnlockedCards),
                    [typeof(UnlockState), typeof(CardMultiplayerConstraint)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Replaces the unlocked card list with unlock-filtered results.
        /// </summary>
        public static void Postfix(UnlockState unlockState, ref IEnumerable<CardModel> __result)
        {
            __result = ModUnlockRegistry.FilterUnlocked(__result, unlockState);
        }
    }

    /// <summary>
    ///     Filters locked mod relics from <c>RelicPoolModel.GetUnlockedRelics</c> results.
    /// </summary>
    public class RelicUnlockFilterPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "relic_unlock_filter";

        /// <inheritdoc />
        public static string Description => "Filter locked mod relics from pool unlock results";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RelicPoolModel), nameof(RelicPoolModel.GetUnlockedRelics), [typeof(UnlockState)])];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Replaces the unlocked relic list with unlock-filtered results.
        /// </summary>
        public static void Postfix(UnlockState unlockState, ref IEnumerable<RelicModel> __result)
        {
            __result = ModUnlockRegistry.FilterUnlocked(__result, unlockState);
        }
    }

    /// <summary>
    ///     Filters locked mod potions from <c>PotionPoolModel.GetUnlockedPotions</c> results.
    /// </summary>
    public class PotionUnlockFilterPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "potion_unlock_filter";

        /// <inheritdoc />
        public static string Description => "Filter locked mod potions from pool unlock results";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PotionPoolModel), nameof(PotionPoolModel.GetUnlockedPotions), [typeof(UnlockState)])];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Replaces the unlocked potion list with unlock-filtered results.
        /// </summary>
        public static void Postfix(UnlockState unlockState, ref IEnumerable<PotionModel> __result)
        {
            __result = ModUnlockRegistry.FilterUnlocked(__result, unlockState);
        }
    }

    /// <summary>
    ///     Removes locked mod room events from generated act room sets when safe to do so.
    /// </summary>
    public class GeneratedRoomEventUnlockFilterPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "generated_room_event_unlock_filter";

        /// <inheritdoc />
        public static string Description => "Remove locked mod events after act rooms are generated";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ActModel), nameof(ActModel.GenerateRooms), [typeof(Rng), typeof(UnlockState), typeof(bool)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Filters generated events in the act's private room list by unlock state.
        /// </summary>
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
