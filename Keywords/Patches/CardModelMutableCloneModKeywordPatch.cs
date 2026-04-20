using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Keywords.Patches
{
    /// <summary>
    ///     Propagates the instance mod-keyword set from the original <see cref="CardModel" /> to its
    ///     <see cref="AbstractModel.MutableClone" /> result so clones (dupes, preview copies, etc.) keep whatever
    ///     runtime keyword state the source had — mirroring vanilla <c>CardModel.DeepCloneFields</c>’s handling of
    ///     <c>_keywords</c>.
    /// </summary>
    public sealed class CardModelMutableCloneModKeywordPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_card_model_mutable_clone_mod_keyword_propagation";

        /// <inheritdoc />
        public static string Description =>
            "Propagate CardModKeywordStore set across AbstractModel.MutableClone so clones preserve mod keywords";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AbstractModel), nameof(AbstractModel.MutableClone), Type.EmptyTypes)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Copies the source card's live mod-keyword set onto the cloned card (if any entry exists).
        /// </summary>
        public static void Postfix(AbstractModel __instance, AbstractModel __result)
        {
            if (__instance is not CardModel source || __result is not CardModel clone)
                return;

            var live = CardModKeywordStore.TryGetLive(source);
            if (live == null)
                return;

            CardModKeywordStore.Replace(clone, [.. live]);
        }
        // ReSharper restore InconsistentNaming
    }
}
