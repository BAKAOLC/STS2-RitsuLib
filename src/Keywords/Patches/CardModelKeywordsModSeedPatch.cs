using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Keywords.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Seeds minted mod <see cref="CardKeyword" /> values into each <see cref="ModCardTemplate" /> when the game
    ///         first materializes its local keyword set. Keeping
    ///         <see cref="ModCardTemplate.RegisteredKeywordIds" /> separate from
    ///         <see cref="CardModel.CanonicalKeywords" /> lets derived mods override
    ///         <c>CanonicalKeywords</c> without discarding keywords declared through the template.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         游戏首次创建本地关键词集合时，将动态生成的模组 <see cref="CardKeyword" /> 值加入每个
    ///         <see cref="ModCardTemplate" />。由于
    ///         <see cref="ModCardTemplate.RegisteredKeywordIds" /> 与
    ///         <see cref="CardModel.CanonicalKeywords" /> 是相互独立的数据来源，派生模组即使重写
    ///         <c>CanonicalKeywords</c>，也不会丢失通过模板声明的关键词。
    ///     </para>
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
