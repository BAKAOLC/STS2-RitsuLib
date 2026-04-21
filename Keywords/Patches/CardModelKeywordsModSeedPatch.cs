using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Keywords.Patches
{
    /// <summary>
    ///     Seeds minted mod <see cref="CardKeyword" /> values onto every <see cref="ModCardTemplate" /> instance
    ///     after vanilla <c>CardModel.get_Keywords</c> materializes the underlying <c>_keywords</c> set. Keeps
    ///     <see cref="ModCardTemplate.RegisteredKeywordIds" /> as an independent channel from vanilla
    ///     <see cref="CardModel.CanonicalKeywords" /> so downstream mods can still override
    ///     <c>CanonicalKeywords</c> without dropping their mod keyword declarations. Seeding is tracked per
    ///     instance with a <see cref="ConditionalWeakTable{TKey,TValue}" /> marker so the postfix executes the
    ///     resolution loop exactly once per card lifetime (subsequent calls are an O(1) early-out).
    /// </summary>
    public sealed class CardModelKeywordsModSeedPatch : IPatchMethod
    {
        private static readonly ConditionalWeakTable<CardModel, object> SeededCards = new();
        private static readonly object SeededMarker = new();

        /// <inheritdoc />
        public static string PatchId => "ritsulib_card_model_keywords_mod_seed";

        /// <inheritdoc />
        public static string Description =>
            "Seed ModCardTemplate.RegisteredKeywordIds into CardModel.Keywords after the canonical set is built";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "get_Keywords")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Unions the minted <see cref="CardKeyword" /> values of the card's
        ///     <see cref="ModCardTemplate.RegisteredKeywordIds" /> into the vanilla keyword set the first time the
        ///     getter runs. The returned <c>IReadOnlySet&lt;CardKeyword&gt;</c> is physically the private
        ///     <c>HashSet&lt;CardKeyword&gt;</c> field, so direct casts are safe and the writes flow into the real
        ///     storage used by subsequent reads, <c>AddKeyword</c>/<c>RemoveKeyword</c>, and
        ///     <c>DeepCloneFields</c>.
        /// </summary>
        public static void Postfix(CardModel __instance, IReadOnlySet<CardKeyword> __result)
        {
            if (__instance is not ModCardTemplate template)
                return;

            if (SeededCards.TryGetValue(__instance, out _))
                return;

            if (__result is not HashSet<CardKeyword> storage)
                return;

            foreach (var id in template.EnumerateRegisteredKeywordIds())
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (ModKeywordRegistry.TryGetCardKeyword(id, out var value))
                    storage.Add(value);
            }

            SeededCards.Add(__instance, SeededMarker);
        }
        // ReSharper restore InconsistentNaming
    }
}
