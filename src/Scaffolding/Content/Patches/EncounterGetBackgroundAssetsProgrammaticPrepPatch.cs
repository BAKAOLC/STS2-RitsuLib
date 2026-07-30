using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Prepares a <see cref="ModEncounterTemplate" /> programmatic background before
    ///         <c>EncounterModel.GetBackgroundAssets</c> delegates to the custom-background path.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <c>EncounterModel.GetBackgroundAssets</c> 转入自定义背景路径前，准备
    ///         <see cref="ModEncounterTemplate" /> 的程序化背景。
    ///     </para>
    /// </summary>
    internal class EncounterGetBackgroundAssetsProgrammaticPrepPatch : IPatchMethod
    {
        public static string PatchId => "content_encounter_programmatic_background_prep";

        public static string Description =>
            "Prepare ModEncounterTemplate programmatic combat BackgroundAssets for CreateBackgroundAssetsForCustom";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EncounterModel), "GetBackgroundAssets", [typeof(ActModel), typeof(Rng)])];
        }

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
