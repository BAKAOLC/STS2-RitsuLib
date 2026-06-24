using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Keywords.Patches
{
    /// <summary>
    ///     Seeds minted mod <see cref="CardKeyword" /> values onto every <see cref="ModCardTemplate" /> instance
    ///     after vanilla <c>CardModel.get_Keywords</c> materializes the underlying local keyword set. Keeps
    ///     <see cref="ModCardTemplate.RegisteredKeywordIds" /> as an independent channel from vanilla
    ///     <see cref="CardModel.CanonicalKeywords" /> so downstream mods can still override
    ///     <c>CanonicalKeywords</c> without dropping their mod keyword declarations.
    ///     在原版 <c>CardModel.get_Keywords</c> 实体化底层本地关键词集合后，将铸造的 mod <see cref="CardKeyword" /> 值种入每个
    ///     <see cref="ModCardTemplate" /> 实例。保持
    ///     <see cref="ModCardTemplate.RegisteredKeywordIds" /> 作为独立于原版
    ///     <see cref="CardModel.CanonicalKeywords" /> 的通道，使下游 mod 仍可覆盖
    ///     <c>CanonicalKeywords</c> 而不会丢失其 mod 关键词声明。
    /// </summary>
    internal sealed class CardModelKeywordsModSeedPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<CardModel, HashSet<CardKeyword>?> KeywordsRef =
            AccessTools.FieldRefAccess<CardModel, HashSet<CardKeyword>?>("_keywords");

        public static string PatchId => "ritsulib_card_model_keywords_mod_seed";

        public static string Description =>
            "Seed ModCardTemplate.RegisteredKeywordIds into CardModel.Keywords after the canonical set is built";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
#if STS2_AT_LEAST_0_107_0
            return [new(typeof(CardModel), "LocalKeywords", MethodType.Getter)];
#else
            return [new(typeof(CardModel), "Keywords", MethodType.Getter)];
#endif
        }

        public static void Prefix(CardModel __instance, out bool __state)
        {
            __state = KeywordsRef(__instance) == null;
        }

        public static void Postfix(CardModel __instance, IReadOnlySet<CardKeyword> __result, bool __state)
        {
            if (!__state)
                return;

            if (__instance is not ModCardTemplate template)
                return;

            if (__result is not HashSet<CardKeyword> storage)
                return;

            foreach (var id in template.EnumerateRegisteredKeywordIds())
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (ModKeywordRegistry.TryResolveCardKeyword(id, out var value))
                    storage.Add(value);
            }
        }
    }
}
