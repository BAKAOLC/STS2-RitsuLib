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
    public class CardDynamicVarTooltipPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "card_dynamic_var_tooltips";

        /// <inheritdoc />
        public static string Description => "Append registered dynamic variable tooltips to card hover tips";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "HoverTips", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Appends tooltip instances built from each <see cref="CardModel.DynamicVars" /> entry that has a factory.
        ///     追加由每个带工厂的 <see cref="CardModel.DynamicVars" /> 条目构建的工具提示实例。
        /// </summary>
        /// <param name="__instance">
        ///     Card being queried for hover tips.
        ///     正在查询悬停提示的卡牌。
        /// </param>
        /// <param name="__result">
        ///     Original enumerable; replaced with a distinct concat when any extra tips exist.
        ///     原始 enumerable；存在额外提示时替换为去重拼接结果。
        /// </param>
        public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
            // ReSharper restore InconsistentNaming
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
    public class DynamicVarTooltipClonePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "dynamic_var_tooltip_clone";

        /// <inheritdoc />
        public static string Description => "Preserve registered dynamic variable tooltip metadata when cloning";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(DynamicVar), nameof(DynamicVar.Clone), Type.EmptyTypes),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Copies tooltip factory attachment from the source instance to the clone.
        ///     将工具提示工厂附加项从源实例复制到克隆实例。
        /// </summary>
        /// <param name="__instance">
        ///     Original dynamic var.
        ///     原始动态变量。
        /// </param>
        /// <param name="__result">
        ///     Cloned dynamic var.
        ///     克隆后的动态变量。
        /// </param>
        public static void Postfix(DynamicVar __instance, DynamicVar __result)
            // ReSharper restore InconsistentNaming
        {
            DynamicVarTooltipRegistry.CopyTo(__instance, __result);
        }
    }
}
