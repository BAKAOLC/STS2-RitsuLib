using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.CardTags.Patches
{
    /// <summary>
    ///     Seeds minted mod <see cref="CardTag" /> values onto every <see cref="ModCardTemplate" /> instance after
    ///     vanilla <c>CardModel.Tags</c> materializes the backing <c>_tags</c> set from
    ///     <see cref="CardModel.CanonicalTags" />. Keeps <see cref="ModCardTemplate.RegisteredCardTagIds" /> separate
    ///     from <c>CanonicalTags</c> so mods can still override canonical vanilla tags without dropping declarative mod
    ///     tags.
    /// </summary>
    public sealed class CardModelTagsModSeedPatch : IPatchMethod
    {
        private static readonly ConditionalWeakTable<CardModel, object> SeededCards = new();
        private static readonly object SeededMarker = new();

        /// <inheritdoc />
        public static string PatchId => "ritsulib_card_model_tags_mod_seed";

        /// <inheritdoc />
        public static string Description =>
            "Seed ModCardTemplate.RegisteredCardTagIds into CardModel.Tags after the canonical set is built";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "Tags", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Unions minted <see cref="CardTag" /> values into the physical <see cref="HashSet{T}" /> storage the first
        ///     time the getter runs.
        /// </summary>
        public static void Postfix(CardModel __instance, IEnumerable<CardTag> __result)
        {
            if (__instance is not ModCardTemplate template)
                return;

            if (SeededCards.TryGetValue(__instance, out _))
                return;

            if (__result is not HashSet<CardTag> storage)
            {
                SeededCards.Add(__instance, SeededMarker);
                return;
            }

            foreach (var id in template.EnumerateRegisteredCardTagIds())
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (ModCardTagRegistry.TryGetCardTag(id, out var value))
                    storage.Add(value);
            }

            SeededCards.Add(__instance, SeededMarker);
        }
        // ReSharper restore InconsistentNaming
    }
}
