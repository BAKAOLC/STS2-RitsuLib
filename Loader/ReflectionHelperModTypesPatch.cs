using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;

namespace STS2RitsuLib.Loader
{
    [HarmonyPatch(typeof(ReflectionHelper), nameof(ReflectionHelper.ModTypes), MethodType.Getter)]
    internal static class ReflectionHelperModTypesPatch
    {
        // ReSharper disable once InconsistentNaming
        private static void Postfix(ref Type[] __result)
        {
            var variantTypes = Bootstrap.GetVariantModTypes();
            if (variantTypes.Length == 0)
                return;

            __result = __result.Concat(variantTypes).Distinct().ToArray();
        }
    }
}
