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
    public class AncientDialoguePopulateLocKeysPatch : IPatchMethod
    {
        private static readonly AttachedState<AncientDialogueSet, HashSet<string>> ProcessedAncients = new(() => []);

        /// <inheritdoc />
        public static string PatchId => "ancient_dialogue_localization_mod_character_append";

        /// <inheritdoc />
        public static string Description =>
            "Append localization-defined ancient dialogues for registered mod characters before PopulateLocKeys";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(AncientDialogueSet), nameof(AncientDialogueSet.PopulateLocKeys), [typeof(string)]),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Ensures mod-character lines are merged once per ancient before vanilla localization key population.
        ///     确保在原版本地化 key 填充之前，每个远古事件的 mod 角色台词只合并一次。
        /// </summary>
        public static void Prefix(AncientDialogueSet __instance, string ancientEntry)
            // ReSharper restore InconsistentNaming
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
