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
    ///     对原版卡牌描述构建器追加 postfix，使带 <see cref="ModKeywordDefinition.CardDescriptionPlacement" /> 的 mod 关键词像原版关键词一样注入 BBCode。
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
        ///     在原版描述组合完成后追加或前置关键词片段。
        /// </summary>
        public static void Postfix(CardModel __instance, ref string __result)
        {
            ModKeywordCardDescriptionInjector.AppendFragments(__instance, ref __result);
        }
        // ReSharper restore InconsistentNaming
    }
}
