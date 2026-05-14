using System.Runtime.CompilerServices;
using HarmonyLib;
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
    ///     在原版 <c>CardModel.get_Keywords</c> 实体化底层 <c>_keywords</c> 集合后，将铸造的 mod <see cref="CardKeyword" /> 值种入每个
    ///     <see cref="ModCardTemplate" /> 实例。保持
    ///     <see cref="ModCardTemplate.RegisteredKeywordIds" /> 作为独立于原版
    ///     <see cref="CardModel.CanonicalKeywords" /> 的通道，使下游 mod 仍可覆盖
    ///     <c>CanonicalKeywords</c> 而不会丢失其 mod 关键词声明。种入过程使用
    ///     <see cref="ConditionalWeakTable{TKey,TValue}" /> 标记按实例跟踪，使 postfix 在每张卡牌生命周期内
    ///     只执行一次解析循环（后续调用为 O(1) 早退）。
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
            return [new(typeof(CardModel), "Keywords", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Unions the minted <see cref="CardKeyword" /> values of the card's
        ///     <see cref="ModCardTemplate.RegisteredKeywordIds" /> into the vanilla keyword set the first time the
        ///     getter runs. The returned <c>IReadOnlySet&lt;CardKeyword&gt;</c> is physically the private
        ///     <c>HashSet&lt;CardKeyword&gt;</c> field, so direct casts are safe and the writes flow into the real
        ///     storage used by subsequent reads, <c>AddKeyword</c>/<c>RemoveKeyword</c>, and
        ///     <c>DeepCloneFields</c>.
        ///     第一次运行 getter 时，将卡牌的
        ///     <see cref="ModCardTemplate.RegisteredKeywordIds" /> 对应的铸造 <see cref="CardKeyword" /> 值并入原版关键词集合。返回的
        ///     <c>IReadOnlySet&lt;CardKeyword&gt;</c> 实际上就是 private
        ///     <c>HashSet&lt;CardKeyword&gt;</c> 字段，因此直接强转是安全的，写入也会流入后续读取、<c>AddKeyword</c>/<c>RemoveKeyword</c> 以及
        ///     <c>DeepCloneFields</c> 使用的真实
        ///     存储。
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

                if (ModKeywordRegistry.TryResolveCardKeyword(id, out var value))
                    storage.Add(value);
            }

            SeededCards.Add(__instance, SeededMarker);
        }
        // ReSharper restore InconsistentNaming
    }
}
