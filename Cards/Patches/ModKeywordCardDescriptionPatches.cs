using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Cards.Patches
{
    /// <summary>
    ///     Postfixes vanilla card description builders so mod keywords with
    ///     <see cref="ModKeywordDefinition.CardDescriptionPlacement" /> inject BBCode like vanilla keywords.
    /// </summary>
    public sealed class ModKeywordCardDescriptionPatches : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "card_mod_keyword_description";

        /// <inheritdoc />
        public static string Description => "Inject mod keyword BBCode into CardModel description rendering";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), nameof(CardModel.GetDescriptionForPile),
                    [typeof(PileType), typeof(Creature)]),
                new(typeof(CardModel), nameof(CardModel.GetDescriptionForUpgradePreview), Type.EmptyTypes),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Appends or prepends keyword fragments after vanilla description composition.
        /// </summary>
        public static void Postfix(CardModel __instance, ref string __result)
        {
            ModKeywordCardDescriptionInjector.AppendFragments(__instance, ref __result);
        }
        // ReSharper restore InconsistentNaming
    }
}
