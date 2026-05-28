using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib.Lifecycle.Patches;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Cards.Transforms.Patches
{
    internal sealed class CardCmdTransformPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_card_cmd_transform";
        public static string Description => "Notify registered listeners after CardCmd.Transform";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardCmd), nameof(CardCmd.Transform),
                    [typeof(IEnumerable<CardTransformation>), typeof(Rng), typeof(CardPreviewStyle)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Prefix(
            ref IEnumerable<CardTransformation> transformations,
            out TransformSnapshot[] __state)
        {
            var snapshot = transformations.ToArray();
            transformations = snapshot;
            __state = CaptureSortedOriginals(snapshot);
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(
                TransformSnapshot[] __state,
                ref Task<IEnumerable<CardPileAddResult>> __result)
            // ReSharper restore InconsistentNaming
        {
            __result = LifecyclePatchTaskBridge.After(__result, result => Notify(__state, result));
        }

        private static void Notify(TransformSnapshot[] originals, IEnumerable<CardPileAddResult> results)
        {
            var resultsArray = results.ToArray();
            var count = Math.Min(originals.Length, resultsArray.Length);

            for (var i = 0; i < count; i++)
            {
                var result = resultsArray[i];
                if (!result.success || result.cardAdded == null)
                    continue;

                var original = originals[i];
                if (original.Pile == null)
                    continue;

                ModCardTransformRegistry.NotifyTransformed(
                    original.Original,
                    result.cardAdded,
                    original.Pile,
                    original.PileIndex);
            }
        }

        private static TransformSnapshot[] CaptureSortedOriginals(IReadOnlyList<CardTransformation> transformations)
        {
            var snapshots = new TransformSnapshot[transformations.Count];
            for (var i = 0; i < transformations.Count; i++)
            {
                var original = transformations[i].Original;
                var pile = original.Pile;
                var index = pile == null ? -1 : IndexOf(pile.Cards, original);
                snapshots[i] = new(original, pile, index, i);
            }

            Array.Sort(snapshots, Compare);
            return snapshots;
        }

        private static int Compare(TransformSnapshot x, TransformSnapshot y)
        {
            if (x.Pile == null || y.Pile == null)
                return x.InputIndex.CompareTo(y.InputIndex);

            var pile = x.Pile.Type.CompareTo(y.Pile.Type);
            return pile != 0 ? pile : x.PileIndex.CompareTo(y.PileIndex);
        }

        private static int IndexOf(IReadOnlyList<CardModel> cards, CardModel card)
        {
            for (var i = 0; i < cards.Count; i++)
                if (ReferenceEquals(cards[i], card))
                    return i;

            return -1;
        }

        internal readonly record struct TransformSnapshot(
            CardModel Original,
            CardPile? Pile,
            int PileIndex,
            int InputIndex);
    }
}
