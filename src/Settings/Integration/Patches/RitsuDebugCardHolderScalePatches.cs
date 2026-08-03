using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Settings.Patches
{
    internal sealed class RitsuDebugCardHolderSmallScalePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_debug_card_holder_small_scale";
        public static string Description => "Use compact card previews in the developer-tools catalog";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardHolder), "get_SmallScale")];
        }

        public static void Postfix(NCardHolder __instance, ref Vector2 __result)
        {
            if (RitsuDebugCardCatalog.IsCatalogHolder(__instance))
                __result = RitsuDebugCardCatalog.HolderScale;
        }
    }

    internal sealed class RitsuDebugCardHolderHoverScalePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_debug_card_holder_hover_scale";
        public static string Description => "Keep developer-tools card hover previews within their grid cells";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardHolder), "get_HoverScale")];
        }

        public static void Postfix(NCardHolder __instance, ref Vector2 __result)
        {
            if (RitsuDebugCardCatalog.IsCatalogHolder(__instance))
                __result = RitsuDebugCardCatalog.HolderHoverScale;
        }
    }
}
