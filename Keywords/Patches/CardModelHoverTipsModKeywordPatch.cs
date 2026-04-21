using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Keywords.Patches
{
    /// <summary>
    ///     Strips hover tips for mod keywords whose <see cref="ModKeywordDefinition.IncludeInCardHoverTip" /> is
    ///     <c>false</c> from the vanilla <see cref="CardModel.HoverTips" /> enumeration. Mod keywords now live
    ///     inside vanilla <c>CardModel.Keywords</c> as minted <c>CardKeyword</c> values, so vanilla already
    ///     iterates them and calls <see cref="HoverTipFactory.FromKeyword" /> on each; the Registry routing
    ///     patch (<see cref="HoverTipFactoryFromKeywordPatch" />) returns a real hover tip for every mod
    ///     keyword. This postfix is only required to honor the opt-out flag.
    /// </summary>
    public sealed class CardModelHoverTipsModKeywordPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_card_model_hover_tips_mod_keyword_exclude";

        /// <inheritdoc />
        public static string Description =>
            "Remove mod keyword hover tips from CardModel.HoverTips when IncludeInCardHoverTip is false";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "get_HoverTips")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Removes any mod-keyword hover tip that vanilla produced (via
        ///     <see cref="HoverTipFactory.FromKeyword" />) but is marked non-hoverable in the registry.
        /// </summary>
        public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            HashSet<IHoverTip>? toRemove = null;
            foreach (var keyword in __instance.Keywords)
            {
                if (!ModKeywordRegistry.TryGetByCardKeyword(keyword, out var definition))
                    continue;

                if (definition.IncludeInCardHoverTip)
                    continue;

                toRemove ??= [];
                toRemove.Add(HoverTipFactory.FromKeyword(keyword));
            }

            if (toRemove is null)
                return;

            __result = __result.Where(tip => !toRemove.Contains(tip)).ToArray();
        }
        // ReSharper restore InconsistentNaming
    }
}
