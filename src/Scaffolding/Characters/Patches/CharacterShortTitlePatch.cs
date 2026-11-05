#if STS2_AT_LEAST_0_113_0
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    internal sealed class CharacterShortTitlePatch : IPatchMethod
    {
        public static string PatchId => "mod_character_short_title_fallback";
        public static string Description => "Use registered mod character titles when no short title is available";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.ShortTitle), MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref LocString __result)
        {
            if (!ModContentRegistry.IsRegisteredCharacter(__instance.GetType()) ||
                LocManager.Instance is not { } locManager ||
                locManager.GetTable(CharacterModel.locTable).HasEntry(__instance.Id.Entry + ".shortTitle"))
                return true;

            __result = __instance.Title;
            return false;
        }
    }
}
#endif
