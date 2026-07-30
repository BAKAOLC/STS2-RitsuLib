using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Relics.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Searches the deck for a mod-registered transcendence starter when the base game finds none.
    ///     </para>
    ///     <para xml:lang="zh-CN">游戏本体未找到超越起始卡牌时，在牌组中查找模组注册的起始卡牌。</para>
    /// </summary>
    internal sealed class ArchaicToothGetTranscendenceStarterCardPatch : IPatchMethod
    {
        public static string PatchId => "archaic_tooth_transcendence_starter_mod";

        public static string Description =>
            "Allow ArchaicTooth transcendence to detect mod-registered starter cards in the deck";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ArchaicTooth), nameof(ArchaicTooth.GetTranscendenceStarterCard), [typeof(Player)])];
        }

        public static void Postfix(Player player, ref CardModel? __result)
        {
            if (__result != null)
                return;

            __result = player.Deck.Cards.FirstOrDefault(c =>
                OrobasAncientUpgradeRegistry.HasTranscendenceStarter(c.Id));
        }
    }
}
