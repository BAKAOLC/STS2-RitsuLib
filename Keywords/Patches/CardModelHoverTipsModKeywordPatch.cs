using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Keywords.Patches
{
    /// <summary>
    ///     Appends the card's current mod-keyword hover tips to the vanilla <see cref="CardModel.HoverTips" />
    ///     enumeration so mod keywords always reflect the **effective** runtime set for every card, whether the
    ///     card is a vanilla <see cref="CardModel" />, a third-party mod card, or a
    ///     <see cref="Scaffolding.Content.ModCardTemplate" /> subclass. The set is read from
    ///     <see cref="CardModKeywordStore" /> which itself returns canonical seeds for untouched cards and the
    ///     persisted runtime set for modified ones.
    /// </summary>
    public sealed class CardModelHoverTipsModKeywordPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_card_model_hover_tips_mod_keyword_append";

        /// <inheritdoc />
        public static string Description =>
            "Append mod keyword hover tips to CardModel.HoverTips for all cards via CardModKeywordStore";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "get_HoverTips")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Appends unique mod-keyword hover tips after vanilla keyword / enchantment / affliction tips.
        /// </summary>
        public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            var ids = CardModKeywordStore.GetIds(__instance);
            if (ids.Count == 0)
                return;

            var extra = ids.ToHoverTips().ToArray();
            if (extra.Length == 0)
                return;

            __result = [.. __result, .. extra];
        }
        // ReSharper restore InconsistentNaming
    }
}
