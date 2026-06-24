using System.Runtime.CompilerServices;
using HarmonyLib;
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
    ///     从 <c>UnlockState.Characters</c> 中过滤掉已锁定的 mod 角色。
    /// </summary>
    internal class CharacterUnlockFilterPatch : IPatchMethod
    {
        public static string PatchId => "character_unlock_filter";
        public static string Description => "Filter locked mod characters from UnlockState.Characters";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(UnlockState), "Characters", MethodType.Getter)];
        }

        public static void Postfix(UnlockState __instance, ref IEnumerable<CharacterModel> __result)
        {
            __result = ModUnlockRegistry.FilterUnlocked(__result, __instance);
        }
    }

    /// <summary>
    ///     Filters locked mod shared ancients out of <c>UnlockState.SharedAncients</c>.
    ///     从 <c>UnlockState.SharedAncients</c> 中过滤掉已锁定的 mod 共享远古。
    /// </summary>
    internal class SharedAncientUnlockFilterPatch : IPatchMethod
    {
        public static string PatchId => "shared_ancient_unlock_filter";
        public static string Description => "Filter locked mod shared ancients from UnlockState.SharedAncients";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(UnlockState), "SharedAncients", MethodType.Getter)];
        }

        public static void Postfix(UnlockState __instance, ref IEnumerable<AncientEventModel> __result)
        {
            __result = ModUnlockRegistry.FilterUnlocked(__result, __instance);
        }
    }

    /// <summary>
    ///     Filters locked mod cards from <c>CardPoolModel.GetUnlockedCards</c> results.
    ///     从 <c>CardPoolModel.GetUnlockedCards</c> 结果中过滤掉已锁定的 mod 卡牌。
    /// </summary>
    internal class CardUnlockFilterPatch : IPatchMethod
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

        public static void Postfix(UnlockState unlockState, ref IEnumerable<CardModel> __result)
        {
            __result = ModUnlockRegistry.FilterUnlocked(__result, unlockState);
        }
    }

    /// <summary>
    ///     Filters locked mod relics from <c>RelicPoolModel.GetUnlockedRelics</c> results.
    ///     从 <c>RelicPoolModel.GetUnlockedRelics</c> 结果中过滤掉已锁定的 mod 遗物。
    /// </summary>
    internal class RelicUnlockFilterPatch : IPatchMethod
    {
        public static string PatchId => "relic_unlock_filter";
        public static string Description => "Filter locked mod relics from pool unlock results";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RelicPoolModel), nameof(RelicPoolModel.GetUnlockedRelics), [typeof(UnlockState)])];
        }

        public static void Postfix(UnlockState unlockState, ref IEnumerable<RelicModel> __result)
        {
            __result = ModUnlockRegistry.FilterUnlocked(__result, unlockState);
        }
    }

    /// <summary>
    ///     Filters locked mod potions from <c>PotionPoolModel.GetUnlockedPotions</c> results.
    ///     从 <c>PotionPoolModel.GetUnlockedPotions</c> 结果中过滤掉已锁定的 mod 药水。
    /// </summary>
    internal class PotionUnlockFilterPatch : IPatchMethod
    {
        public static string PatchId => "potion_unlock_filter";
        public static string Description => "Filter locked mod potions from pool unlock results";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PotionPoolModel), nameof(PotionPoolModel.GetUnlockedPotions), [typeof(UnlockState)])];
        }

        public static void Postfix(UnlockState unlockState, ref IEnumerable<PotionModel> __result)
        {
            __result = ModUnlockRegistry.FilterUnlocked(__result, unlockState);
        }
    }

    /// <summary>
    ///     Removes locked mod room events from generated act room sets when safe to do so.
    ///     在安全时从生成的章节房间集合中移除仍锁定的 mod 房间事件。
    /// </summary>
    internal class GeneratedRoomEventUnlockFilterPatch : IPatchMethod
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

        public static void Postfix(ActModel __instance, UnlockState unlockState)
        {
            var roomSet = Rooms(__instance);
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

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_rooms")]
        private static extern ref RoomSet Rooms(ActModel instance);
    }
}
