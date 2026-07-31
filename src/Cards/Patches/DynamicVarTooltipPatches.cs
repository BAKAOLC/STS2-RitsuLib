using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Cards.Patches
{
    /// <summary>
    ///     <para xml:lang="en">Appends registered dynamic-variable tooltips to <see cref="CardModel.HoverTips" />.</para>
    ///     <para xml:lang="zh-CN">将已注册的动态变量工具提示追加到 <see cref="CardModel.HoverTips" />。</para>
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

            __result = [.. __result.Concat(extraTips).Distinct()];
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Preserves tooltip registration when <see cref="DynamicVar.Clone()" /> clones a variable.</para>
    ///     <para xml:lang="zh-CN">在 <see cref="DynamicVar.Clone()" /> 克隆变量时保留工具提示注册信息。</para>
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
