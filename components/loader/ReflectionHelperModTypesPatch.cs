using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;

namespace STS2RitsuLib.Loader
{
    [HarmonyPatch(typeof(ReflectionHelper), nameof(ReflectionHelper.ModTypes), MethodType.Getter)]
    internal static class ReflectionHelperModTypesPatch
    {
        private static readonly Lock Gate = new();
        private static Type[]? _source;
        private static Type[]? _variants;
        private static Type[]? _merged;

        private static void Postfix(ref Type[] __result)
        {
            var variantTypes = Bootstrap.GetVariantModTypes();
            if (variantTypes.Length == 0)
                return;

            lock (Gate)
            {
                if (ReferenceEquals(__result, _source) && ReferenceEquals(variantTypes, _variants))
                {
                    __result = _merged!;
                    return;
                }

                var seen = new HashSet<Type>(__result);
                var additions = new List<Type>();
                foreach (var type in variantTypes)
                    if (seen.Add(type))
                        additions.Add(type);
                _source = __result;
                _variants = variantTypes;
                _merged = additions.Count == 0 ? __result : [.. __result, .. additions];
                __result = _merged;
            }
        }
    }
}
