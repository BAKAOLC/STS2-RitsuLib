using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    /// <summary>
    ///     Appends registered card candidates to the Trash Heap event's vanilla card pool.
    ///     将已注册的候选卡牌追加到垃圾堆事件的原版卡牌池。
    /// </summary>
    internal sealed class TrashHeapCardsRegistryPatch : IPatchMethod
    {
        public static string PatchId => "trash_heap_registered_cards";

        public static string Description => "Append RitsuLib-registered cards to the Trash Heap event";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(TrashHeap), "Cards", Type.EmptyTypes, MethodType.Getter)];
        }

        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref CardModel[] __result)
        {
            __result = TrashHeapContentRegistry.AppendCards(__result);
        }
    }

    /// <summary>
    ///     Appends registered relic candidates to the Trash Heap event's vanilla relic pool.
    ///     将已注册的候选遗物追加到垃圾堆事件的原版遗物池。
    /// </summary>
    internal sealed class TrashHeapRelicsRegistryPatch : IPatchMethod
    {
        public static string PatchId => "trash_heap_registered_relics";

        public static string Description => "Append RitsuLib-registered relics to the Trash Heap event";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(TrashHeap), "Relics", Type.EmptyTypes, MethodType.Getter)];
        }

        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref RelicModel[] __result)
        {
            __result = TrashHeapContentRegistry.AppendRelics(__result);
        }
    }
}
