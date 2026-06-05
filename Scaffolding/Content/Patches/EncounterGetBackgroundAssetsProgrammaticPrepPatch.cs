using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Runs before <c>EncounterModel.GetBackgroundAssets</c> so <see cref="ModEncounterTemplate" /> can fill the
    ///     programmatic slot (needs <see cref="ActModel" /> + <see cref="Rng" />; vanilla
    ///     <c>CreateBackgroundAssetsForCustom</c> only receives <c>Rng</c>).
    ///     在 <c>EncounterModel.GetBackgroundAssets</c> 之前运行，使 <see cref="ModEncounterTemplate" /> 可以填充
    ///     编程式槽位（需要 <see cref="ActModel" /> + <see cref="Rng" />；原版
    ///     <c>CreateBackgroundAssetsForCustom</c> 只接收 <c>Rng</c>）。
    /// </summary>
    public class EncounterGetBackgroundAssetsProgrammaticPrepPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_encounter_programmatic_background_prep";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Prepare ModEncounterTemplate programmatic combat BackgroundAssets for CreateBackgroundAssetsForCustom";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EncounterModel), "GetBackgroundAssets", [typeof(ActModel), typeof(Rng)])];
        }

        /// <summary>
        ///     Invokes <see cref="ModEncounterTemplate.PrepareProgrammaticCombatBackground" /> when the encounter has no cached
        ///     background yet.
        ///     当遭遇还没有缓存的背景时，调用 <see cref="ModEncounterTemplate.PrepareProgrammaticCombatBackground" />。
        /// </summary>
        public static void Prefix(EncounterModel __instance, ActModel parentAct, Rng rng)
        {
            if (__instance is not ModEncounterTemplate { UsesProgrammaticCombatBackground: true } template)
                return;

            if (CachedBackgroundAssets(__instance) != null)
                return;

            template.PrepareProgrammaticCombatBackground(parentAct, rng);
        }

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_backgroundAssets")]
        private static extern ref BackgroundAssets? CachedBackgroundAssets(EncounterModel instance);
    }
}
