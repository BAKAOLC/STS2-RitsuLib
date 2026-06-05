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
    public sealed class CardModelKeywordsModSeedPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<CardModel, HashSet<CardKeyword>?> KeywordsRef =
            AccessTools.FieldRefAccess<CardModel, HashSet<CardKeyword>?>("_keywords");

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
#if STS2_AT_LEAST_0_107_0
            return [new(typeof(CardModel), "LocalKeywords", MethodType.Getter)];
#else
            return [new(typeof(CardModel), "Keywords", MethodType.Getter)];
#endif
        }

        /// <summary>
        ///     Captures whether this getter call is the one that will materialize vanilla's local keyword set.
        ///     记录本次 getter 调用是否会实体化原版的本地关键词集合。
        /// </summary>
        public static void Prefix(CardModel __instance, out bool __state)
        {
            __state = KeywordsRef(__instance) == null;
        }

        /// <summary>
        ///     Unions the minted <see cref="CardKeyword" /> values of the card's
        ///     <see cref="ModCardTemplate.RegisteredKeywordIds" /> into the vanilla local keyword set the first time
        ///     the local keyword getter runs. This keeps the legacy string channel bound to the same materialization
        ///     path where vanilla unions <see cref="CardModel.CanonicalKeywords" /> into the card's base keywords.
        ///     第一次运行 getter 时，将卡牌的
        ///     <see cref="ModCardTemplate.RegisteredKeywordIds" /> 对应的铸造 <see cref="CardKeyword" /> 值并入原版本地关键词集合。
        ///     这让旧字符串通道绑定在原版把 <see cref="CardModel.CanonicalKeywords" /> 合入卡牌基础关键词的实体化路径上。
        /// </summary>
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
