using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal static class RitsuDebugCardPreviewCloner
    {
        [ThreadStatic] private static int _cloneDepth;

        internal static bool IsCloning => _cloneDepth > 0;

        internal static CardModel Clone(CardModel card)
        {
            try
            {
                _cloneDepth++;
                return card.IsMutable ? (CardModel)card.ClonePreservingMutability() : card.ToMutable();
            }
            finally
            {
                _cloneDepth--;
            }
        }
    }

    internal sealed class RitsuDebugCardPreviewClonePatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<CardModel, Action?> EnchantmentChanged =
            AccessTools.FieldRefAccess<CardModel, Action?>(nameof(CardModel.EnchantmentChanged));

        private static readonly AccessTools.FieldRef<CardModel, Action?> AfflictionChanged =
            AccessTools.FieldRefAccess<CardModel, Action?>(nameof(CardModel.AfflictionChanged));

        public static string PatchId => "debug_card_preview_clone";
        public static string Description => "Keep debug preview clones from notifying the source card's subscribers";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "DeepCloneFields", Type.EmptyTypes)];
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(CardModel __instance)
        {
            if (!RitsuDebugCardPreviewCloner.IsCloning)
                return;
            // DeepCloneFields reattaches enchantments before AfterCloned clears copied event handlers.
            EnchantmentChanged(__instance) = null;
            AfflictionChanged(__instance) = null;
        }
    }
}
