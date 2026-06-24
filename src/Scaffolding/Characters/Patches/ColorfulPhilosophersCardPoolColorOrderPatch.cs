using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Appends opt-in mod character card pools to Colorful Philosophers reward color candidates.
    /// </summary>
    internal class ColorfulPhilosophersCardPoolColorOrderPatch : IPatchMethod
    {
        public static string PatchId => "colorful_philosophers_card_pool_color_order";

        public static string Description =>
            "Append opt-in mod character card pools to Colorful Philosophers color order";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ColorfulPhilosophers), "CardPoolColorOrder", MethodType.Getter)];
        }

        /// <summary>
        ///     Extends the original candidate order while leaving option generation and reward handling to vanilla code.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<CardPoolModel> __result)
        {
            var modPools = ModelDb.AllCharacterCardPools
                .Where(static pool => pool is IModColorfulPhilosophersCardPool);

            __result = __result
                .Concat(modPools)
                .DistinctBy(static pool => pool.Id)
                .ToArray();
        }
    }
}
