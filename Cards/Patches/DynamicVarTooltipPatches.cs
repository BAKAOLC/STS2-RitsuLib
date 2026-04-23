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
        /// </summary>
        /// <param name="__instance">
        ///     Card being queried for hover tips.
        /// </param>
        /// <param name="__result">
        ///     Original enumerable; replaced with a distinct concat when any extra tips exist.
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
        /// </summary>
        /// <param name="__instance">
        ///     Original dynamic var.
        /// </param>
        /// <param name="__result">
        ///     Cloned dynamic var.
        /// </param>
        public static void Postfix(DynamicVar __instance, DynamicVar __result)
            // ReSharper restore InconsistentNaming
        {
            DynamicVarTooltipRegistry.CopyTo(__instance, __result);
        }
    }
}
