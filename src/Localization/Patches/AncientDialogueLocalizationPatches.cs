using MegaCrit.Sts2.Core.Entities.Ancients;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Localization.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Adds localization-defined ancient-event dialogue for registered modded characters before
    ///         <c>AncientDialogueSet.PopulateLocKeys</c> runs.
    ///     </para>
    ///     <para xml:lang="zh-CN">在 <c>AncientDialogueSet.PopulateLocKeys</c> 运行前，为已注册的模组角色添加由本地化定义的先古之民事件对话。</para>
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
