using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Relics.Patches
{
    /// <summary>
    ///     When vanilla finds no transcendence starter, include deck cards that match mod-registered starter ids.
    ///     当原版找不到超越初始牌时，包含与 mod 注册的初始牌 id 匹配的牌组卡牌。
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

        // ReSharper disable once InconsistentNaming
        public static void Postfix(Player player, ref CardModel? __result)
        {
            if (__result != null)
                return;

            __result = player.Deck.Cards.FirstOrDefault(c =>
                OrobasAncientUpgradeRegistry.HasTranscendenceStarter(c.Id));
        }
    }
}
