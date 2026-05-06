using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Relics.Patches
{
    /// <summary>
    ///     Extends <see cref="ArchaicTooth.TranscendenceCards" /> so Dusty Tome and similar logic see mod ancient targets.
    /// </summary>
    internal sealed class ArchaicToothTranscendenceCardsPatch : IPatchMethod
    {
        public static string PatchId => "archaic_tooth_transcendence_cards_mod";

        public static string Description =>
            "Append mod-registered ArchaicTooth transcendence targets to TranscendenceCards";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ArchaicTooth), "TranscendenceCards", MethodType.Getter),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(ref List<CardModel> __result)
        {
            foreach (var card in OrobasAncientUpgradeRegistry.GetRegisteredTranscendenceAncientTemplates())
            {
                if (__result.Exists(c => c.Id == card.Id))
                    continue;
                __result.Add(card);
            }
        }
    }
}
