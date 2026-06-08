using MegaCrit.Sts2.Core.Entities.Ancients;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Localization.Patches
{
    /// <summary>
    ///     Harmony patch that injects mod-character ancient dialogues from localization before
    ///     <c>AncientDialogueSet.PopulateLocKeys</c> runs.
    ///     Harmony patch：在 <c>AncientDialogueSet.PopulateLocKeys</c> 运行前，从本地化中注入 mod 角色的 ancient dialogue。
    /// </summary>
    internal class AncientDialoguePopulateLocKeysPatch : IPatchMethod
    {
        private static readonly AttachedState<AncientDialogueSet, HashSet<string>> ProcessedAncients = new(() => []);
        public static string PatchId => "ancient_dialogue_localization_mod_character_append";

        public static string Description =>
            "Append localization-defined ancient dialogues for registered mod characters before PopulateLocKeys";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(AncientDialogueSet), nameof(AncientDialogueSet.PopulateLocKeys), [typeof(string)]),
            ];
        }

        public static void Prefix(AncientDialogueSet __instance, string ancientEntry)
        {
            var processedAncients = ProcessedAncients.GetOrCreate(__instance);
            if (!processedAncients.Add(ancientEntry))
                return;

            AncientDialogueLocalization.AppendCharacterDialogues(
                __instance,
                ancientEntry,
                ModContentRegistry.GetModCharacters());
        }
    }
}
