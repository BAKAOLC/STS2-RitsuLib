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
    ///     在原版 <c>CardModel.Tags</c> 基于 <see cref="CardModel.CanonicalTags" /> 实体化底层 <c>_tags</c>
    ///     集合后，将已生成的 mod <see cref="CardTag" /> 值播种到每个 <see cref="ModCardTemplate" /> 实例。
    ///     让 <see cref="ModCardTemplate.RegisteredCardTagIds" /> 与 <c>CanonicalTags</c> 保持分离，使 mod 仍可覆盖
    ///     原版规范标签而不会丢失声明式 mod 标签。
    /// </summary>
    internal sealed class CardModelTagsModSeedPatch : IPatchMethod
    {
        private static readonly ConditionalWeakTable<CardModel, object> SeededCards = new();
        private static readonly object SeededMarker = new();
        public static string PatchId => "ritsulib_card_model_tags_mod_seed";

        public static string Description =>
            "Seed ModCardTemplate.RegisteredCardTagIds into CardModel.Tags after the canonical set is built";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "Tags", MethodType.Getter)];
        }

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

                if (ModCardTagRegistry.TryResolveCardTag(id, out var value))
                    storage.Add(value);
            }

            SeededCards.Add(__instance, SeededMarker);
        }
    }
}
