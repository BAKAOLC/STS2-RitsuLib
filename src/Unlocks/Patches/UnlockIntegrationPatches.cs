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
    ///     <para xml:lang="en">Removes locked mod characters from <c>UnlockState.Characters</c>.</para>
    ///     <para xml:lang="zh-CN">从 <c>UnlockState.Characters</c> 中移除已锁定的模组角色。</para>
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
    ///     <para xml:lang="en">Removes locked mod shared Ancients from <c>UnlockState.SharedAncients</c>.</para>
    ///     <para xml:lang="zh-CN">从 <c>UnlockState.SharedAncients</c> 中移除已锁定的模组共享先古之民事件。</para>
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
    ///     <para xml:lang="en">Removes locked mod cards from <c>CardPoolModel.GetUnlockedCards</c> results.</para>
    ///     <para xml:lang="zh-CN">从 <c>CardPoolModel.GetUnlockedCards</c> 的结果中移除已锁定的模组卡牌。</para>
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
    ///     <para xml:lang="en">Removes locked mod relics from <c>RelicPoolModel.GetUnlockedRelics</c> results.</para>
    ///     <para xml:lang="zh-CN">从 <c>RelicPoolModel.GetUnlockedRelics</c> 的结果中移除已锁定的模组遗物。</para>
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
    ///     <para xml:lang="en">Removes locked mod potions from <c>PotionPoolModel.GetUnlockedPotions</c> results.</para>
    ///     <para xml:lang="zh-CN">从 <c>PotionPoolModel.GetUnlockedPotions</c> 的结果中移除已锁定的模组药水。</para>
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
    ///     <para xml:lang="en">
    ///         Removes locked mod events from a generated act's room set when at least one event remains.
    ///     </para>
    ///     <para xml:lang="zh-CN">生成章节房间集合后，在至少仍保留一个事件的前提下移除已锁定的模组事件。</para>
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
