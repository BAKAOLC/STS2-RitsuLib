using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Relics.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Adds mod-provided Ancient card targets to <see cref="ArchaicTooth.TranscendenceCards" /> for Dusty Tome and other
    ///         consumers.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将模组提供的先古卡牌目标加入 <see cref="ArchaicTooth.TranscendenceCards" />，供尘封之书等逻辑使用。
    ///     </para>
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
