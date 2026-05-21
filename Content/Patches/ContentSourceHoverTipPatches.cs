using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    internal static class ContentSourceHoverTipPatchHelper
    {
        internal static void Append(AbstractModel model, ref IEnumerable<IHoverTip> result)
        {
            if (!ContentSourceHoverTipFactory.TryCreate(model, out var tip))
                return;

            result = result.Concat([tip]).ToArray();
        }
    }

    internal sealed class CardModelSourceHoverTipPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_card_model_source_hover_tip";

        public static string Description => "Append content source hover tip to card hover tips";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "HoverTips", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
            // ReSharper restore InconsistentNaming
        {
            ContentSourceHoverTipPatchHelper.Append(__instance, ref __result);
        }
    }

    internal sealed class RelicModelSourceHoverTipPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_relic_model_source_hover_tip";

        public static string Description => "Append content source hover tip to relic hover tips";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RelicModel), "HoverTips", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(RelicModel __instance, ref IEnumerable<IHoverTip> __result)
            // ReSharper restore InconsistentNaming
        {
            ContentSourceHoverTipPatchHelper.Append(__instance, ref __result);
        }
    }

    internal sealed class RelicModelSourceHoverTipExcludingRelicPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_relic_model_source_hover_tip_excluding_relic";

        public static string Description => "Append content source hover tip to relic side hover tips";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RelicModel), "HoverTipsExcludingRelic", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(RelicModel __instance, ref IEnumerable<IHoverTip> __result)
            // ReSharper restore InconsistentNaming
        {
            ContentSourceHoverTipPatchHelper.Append(__instance, ref __result);
        }
    }

    internal sealed class PotionModelSourceHoverTipPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_potion_model_source_hover_tip";

        public static string Description => "Append content source hover tip to potion hover tips";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PotionModel), "HoverTips", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(PotionModel __instance, ref IEnumerable<IHoverTip> __result)
            // ReSharper restore InconsistentNaming
        {
            ContentSourceHoverTipPatchHelper.Append(__instance, ref __result);
        }
    }
}
