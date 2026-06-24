using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Cards.Patches
{
    /// <summary>
    ///     Harmony postfix on <see cref="CardModel.HoverTips" /> to append registered dynamic-var tooltips.
    ///     <see cref="CardModel.HoverTips" /> 的 Harmony postfix，用于追加已注册的动态变量工具提示。
    /// </summary>
    internal class CardDynamicVarTooltipPatch : IPatchMethod
    {
        public static string PatchId => "card_dynamic_var_tooltips";
        public static string Description => "Append registered dynamic variable tooltips to card hover tips";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "HoverTips", MethodType.Getter),
            ];
        }

        public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            var extraTips = __instance.DynamicVars.Values
                .Select(DynamicVarTooltipRegistry.Create)
                .OfType<IHoverTip>()
                .ToArray();

            if (extraTips.Length == 0)
                return;

            __result = __result.Concat(extraTips).Distinct().ToArray();
        }
    }

    /// <summary>
    ///     Harmony postfix on <see cref="DynamicVar.Clone()" /> so tooltip registration survives cloning.
    ///     <see cref="DynamicVar.Clone()" /> 的 Harmony postfix，使工具提示注册在克隆后仍保留。
    /// </summary>
    internal class DynamicVarTooltipClonePatch : IPatchMethod
    {
        public static string PatchId => "dynamic_var_tooltip_clone";
        public static string Description => "Preserve registered dynamic variable tooltip metadata when cloning";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(DynamicVar), nameof(DynamicVar.Clone), Type.EmptyTypes),
            ];
        }

        public static void Postfix(DynamicVar __instance, DynamicVar __result)
        {
            DynamicVarTooltipRegistry.CopyTo(__instance, __result);
        }
    }
}
