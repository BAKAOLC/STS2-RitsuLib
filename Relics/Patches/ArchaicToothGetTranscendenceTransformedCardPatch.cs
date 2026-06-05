using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Relics.Patches
{
    /// <summary>
    ///     Applies mod transcendence templates with the same upgrade / enchantment carry-over as vanilla.
    ///     以与原版相同的升级/附魔继承方式应用 mod 超越模板。
    /// </summary>
    internal sealed class ArchaicToothGetTranscendenceTransformedCardPatch : IPatchMethod
    {
        public static string PatchId => "archaic_tooth_transcendence_transform_mod";

        public static string Description =>
            "Apply RitsuLib-registered ArchaicTooth transcendence card transforms";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ArchaicTooth), nameof(ArchaicTooth.GetTranscendenceTransformedCard),
                    [typeof(CardModel)]),
            ];
        }

        public static bool Prefix(CardModel starterCard, ref CardModel __result)
        {
            if (!OrobasAncientUpgradeRegistry.TryGetTranscendenceAncient(starterCard.Id, out var template))
                return true;

            var cardModel = starterCard.Owner.RunState.CreateCard(template, starterCard.Owner);
            if (starterCard.IsUpgraded)
                CardCmd.Upgrade(cardModel);

            if (starterCard.Enchantment != null)
            {
                var enchantmentModel = (EnchantmentModel)starterCard.Enchantment.MutableClone();
                CardCmd.Enchant(enchantmentModel, cardModel, enchantmentModel.Amount);
            }

            __result = cardModel;
            return false;
        }
    }
}
