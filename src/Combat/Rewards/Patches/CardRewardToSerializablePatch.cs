using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.Rewards.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Serializes <see cref="CardReward" /> configurations that the base game cannot preserve, including
    ///         creation flags, filtered pools, and rewards with explicitly supplied cards.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         序列化原版无法保留的 <see cref="CardReward" /> 配置，包括卡牌创建标志、经过筛选的卡池，
    ///         以及显式指定卡牌的奖励。
    ///     </para>
    /// </summary>
    internal sealed class CardRewardToSerializablePatch : IPatchMethod
    {
        private static readonly Func<CardReward, CardCreationOptions> GetOptions =
            AccessTools.MethodDelegate<Func<CardReward, CardCreationOptions>>(
                AccessTools.DeclaredPropertyGetter(typeof(CardReward), "Options"));

        private static readonly Func<CardReward, int> GetOptionCount =
            AccessTools.MethodDelegate<Func<CardReward, int>>(
                AccessTools.DeclaredPropertyGetter(typeof(CardReward), "OptionCount"));

        private static readonly Func<CardReward, CardCreationOptions> GetRerollOptions =
            AccessTools.MethodDelegate<Func<CardReward, CardCreationOptions>>(
                AccessTools.DeclaredPropertyGetter(typeof(CardReward), "RerollOptions"));

        private static readonly AccessTools.FieldRef<CardReward, bool> GetCardsWereManuallySet =
            AccessTools.FieldRefAccess<CardReward, bool>("_cardsWereManuallySet");

        public static string PatchId => "card_reward_to_serializable_ext";

        public static string Description =>
            "Fix CardReward.ToSerializable for Flags, CustomCardPool and CardPoolFilter";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardReward), nameof(CardReward.ToSerializable), Type.EmptyTypes)];
        }

        [HarmonyBefore(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(CardReward __instance, ref SerializableReward __result)
        {
            var options = GetOptions(__instance);
            var hasFlags = options.Flags != 0;
            var hasFilter = options.CardPoolFilter != null;
            var hasNoPools = options.CardPools.Count <= 0;
            var hasFixedCards = GetCardsWereManuallySet(__instance);

            if (!hasFlags && !hasFilter && !hasNoPools && !hasFixedCards)
                return true;

            var result = new SerializableReward { RewardType = RewardType.Card };
            RewardExtData? ext = null;

            if (hasFixedCards)
            {
                ext = BuildSpecificCardsExt(options, __instance.Cards, GetRerollOptions(__instance));
                result.Source = options.Source;
                result.RarityOdds = options.RarityOdds;
            }
#if !STS2_AT_LEAST_0_108_0
            else if (hasNoPools && options.CustomCardPool != null)
            {
                ext = BuildCustomPoolExt(options);
                result.Source = options.Source;
                result.RarityOdds = options.RarityOdds;
            }
#endif
            else if (hasFilter && options.CardPools.Count > 0)
            {
                ext = BuildFilterSnapshotExt(options);
                result.Source = options.Source;
                result.RarityOdds = options.RarityOdds;
                result.CardPoolIds = [.. options.CardPools.Select(p => p.Id)];
            }
            else
            {
                result.Source = options.Source;
                result.RarityOdds = options.RarityOdds;
                result.CardPoolIds = [.. options.CardPools.Select(p => p.Id)];
            }

            result.OptionCount = GetOptionCount(__instance);

            if (hasFlags)
            {
                ext ??= new();
                ext.Flags = (int)options.Flags;
            }

            if (ext != null)
                RewardSerializationExt.SetExtData(result, ext);

            __result = result;
            return false;
        }

        private static RewardExtData BuildSpecificCardsExt(
            CardCreationOptions options, IEnumerable<CardModel> cards, CardCreationOptions rerollOptions)
        {
            var cardList = cards.ToList();
            return new()
            {
                IsCustomPool = true,
                CustomCardIds = [.. cardList.Select(c => c.Id.ToString())],
                FixedCards =
                [
                    .. cardList.Select(c =>
                        JsonSerializer.Serialize(c.ToSerializable(), JsonSerializationUtility.Options)),
                ],
                RerollOptions = BuildRerollOptionsExt(rerollOptions),
                Source = (int)options.Source,
                RarityOdds = (int)options.RarityOdds,
            };
        }
#if !STS2_AT_LEAST_0_108_0
        private static RewardExtData BuildCustomPoolExt(CardCreationOptions options)
        {
            return new()
            {
                IsCustomPool = true,
                CandidateCardIds = options.CustomCardPool!.Select(c => c.Id.ToString()).ToList(),
                Source = (int)options.Source,
                RarityOdds = (int)options.RarityOdds,
            };
        }
#endif

        private static CardRewardRerollExtData BuildRerollOptionsExt(CardCreationOptions options)
        {
            var result = new CardRewardRerollExtData
            {
                CardPoolIds = [.. options.CardPools.Select(pool => pool.Id.ToString())],
                Source = (int)options.Source,
                RarityOdds = (int)options.RarityOdds,
                Flags = (int)options.Flags,
#if STS2_AT_LEAST_0_109_0
                Rng = options.RngOverride == null
                    ? null
                    : JsonSerializer.Serialize(options.RngOverride.ToSerializable(), JsonSerializationUtility.Options),
#else
                LegacyRngSeed = options.RngOverride?.Seed,
                LegacyRngCounter = options.RngOverride?.Counter ?? 0,
#endif
            };
#if !STS2_AT_LEAST_0_108_0
            if (options.CustomCardPool != null)
                result.CandidateCardIds = [.. options.CustomCardPool.Select(card => card.Id.ToString())];
            else
#endif
            if (options.CardPoolFilter != null)
                result.CandidateCardIds = BuildFilterSnapshotExt(options).CandidateCardIds;
            return result;
        }

        private static RewardExtData BuildFilterSnapshotExt(CardCreationOptions options)
        {
            var allCards = options.CardPools
                .SelectMany(p => p.AllCards)
                .Where(options.CardPoolFilter!)
                .ToList();

            return new()
            {
                IsCustomPool = true,
                CandidateCardIds = [.. allCards.Select(c => c.Id.ToString())],
                Source = (int)options.Source,
                RarityOdds = (int)options.RarityOdds,
            };
        }
    }
}
