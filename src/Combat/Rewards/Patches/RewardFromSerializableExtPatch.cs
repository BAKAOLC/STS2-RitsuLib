using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.Rewards.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Extends <see cref="Reward.FromSerializable" /> to rebuild registered custom rewards and card rewards
    ///         that use supplemental serialization data, while recovering from unavailable custom card content.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         扩展 <see cref="Reward.FromSerializable" />，用于重建已注册的自定义奖励和使用补充序列化数据的
    ///         卡牌奖励，并在自定义卡牌内容不可用时进行恢复。
    ///     </para>
    /// </summary>
    internal sealed class RewardFromSerializableExtPatch : IPatchMethod
    {
        public static string PatchId => "reward_from_serializable_ext";

        public static string Description =>
            "Extend Reward.FromSerializable with sideband ext data and registered custom reward types";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Reward), nameof(Reward.FromSerializable),
                    [typeof(SerializableReward), typeof(Player)]),
            ];
        }

        [HarmonyBefore(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(SerializableReward save, Player player, ref Reward __result)
        {
            RewardSerializationExt.TryGetExtData(save, out var ext);

            if (LinkedRewardSetSerialization.TryCreate(save, player, out var linkedRewardSet) &&
                linkedRewardSet != null)
            {
                __result = linkedRewardSet;
                return false;
            }

            if (ModRewardRegistry.TryCreate(save.RewardType, save, player, ext?.CustomRewardJson, out var customReward)
                && customReward != null)
            {
                __result = customReward;
                return false;
            }

            if (save.RewardType != RewardType.Card || ext == null)
                return true;

            __result = RebuildCardReward(save, ext, player);
            return false;
        }

        private static CardReward RebuildCardReward(
            SerializableReward save, RewardExtData ext, Player player)
        {
            var flags = (CardCreationFlags)ext.Flags;

            if (ext.CandidateCardIds != null)
                return new(CreateCandidateOptions(save, ext.CandidateCardIds, flags, player), save.OptionCount, player);

            if (ext is { IsCustomPool: true, CustomCardIds: not null })
            {
                var source = (CardCreationSource)ext.Source;
                var rarityOdds = (CardRarityOddsType)ext.RarityOdds;
                var cards = ext.CustomCardIds
                    .Select(TryResolveCard)
                    .Where(c => c != null)
                    .Select(c => c!)
                    .ToList();

                if (cards.Count > 0)
                {
#if STS2_AT_LEAST_0_108_0
                    if (cards.Count > save.OptionCount)
                        return new(CreateCandidateOptions(save, ext.CustomCardIds, flags, player),
                            save.OptionCount, player);

                    var rerollOptions = new CardCreationOptions(
                        [player.Character.CardPool],
                        source,
                        rarityOdds);
#else
                    var options = new CardCreationOptions(cards, source, rarityOdds);
                    if (flags != 0) options.WithFlags(flags);
                    return new(options, save.OptionCount, player);
#endif
#if STS2_AT_LEAST_0_108_0
                    if (flags != 0) rerollOptions.WithFlags(flags);
                    return new(cards, source, player, rerollOptions);
#endif
                }

                Log.Warn("[RitsuLib] Reward.FromSerializable: CustomCardPool had no resolvable cards, " +
                         "falling back to standard card reward.");
            }

            var pools = ResolveCardPools(save.CardPoolIds, player);
            var poolOptions = new CardCreationOptions(pools, save.Source, save.RarityOdds);
            if (flags != 0)
                poolOptions.WithFlags(flags);

            return new(poolOptions, save.OptionCount, player);
        }

        private static CardCreationOptions CreateCandidateOptions(
            SerializableReward save, List<string> candidateCardIds, CardCreationFlags flags, Player player)
        {
            var cards = candidateCardIds.Select(TryResolveCard).OfType<CardModel>().ToList();
#if !STS2_AT_LEAST_0_108_0
            if (save.CardPoolIds is not { Count: > 0 })
                return new CardCreationOptions(cards, save.Source, save.RarityOdds).WithFlags(flags);
#endif
            var pools = save.CardPoolIds is { Count: > 0 }
                ? ResolveCardPools(save.CardPoolIds, player)
                : [.. cards.Select(card => card.Pool).Distinct()];
            HashSet<ModelId> candidateIds = [.. cards.Select(card => card.Id)];
            return new CardCreationOptions(pools, save.Source, save.RarityOdds,
                card => candidateIds.Contains(card.Id)).WithFlags(flags);
        }

        private static List<CardPoolModel> ResolveCardPools(IEnumerable<ModelId>? poolIds, Player player)
        {
            List<CardPoolModel> pools = [];
            foreach (var poolId in poolIds ?? [])
            {
                CardPoolModel? pool;
                try
                {
                    pool = ModelDb.GetByIdOrNull<CardPoolModel>(poolId);
                }
                catch (InvalidCastException ex)
                {
                    Log.Warn(
                        $"[RitsuLib] Reward.FromSerializable: Ignoring invalid card pool id '{poolId}': {ex.Message}");
                    continue;
                }

                if (pool == null)
                {
                    Log.Warn(
                        $"[RitsuLib] Reward.FromSerializable: Ignoring unavailable card pool id '{poolId}'.");
                    continue;
                }

                pools.Add(pool);
            }

            if (pools.Count == 0)
            {
                Log.Warn("[RitsuLib] Reward.FromSerializable: No saved card pools were available; " +
                         "using the player's card pool.");
                pools.Add(player.Character.CardPool);
            }

            return pools;
        }

        private static CardModel? TryResolveCard(string serializedId)
        {
            if (string.IsNullOrWhiteSpace(serializedId))
            {
                Log.Warn("[RitsuLib] Reward.FromSerializable: Ignoring an empty custom card id.");
                return null;
            }

            try
            {
                var card = ModelDb.GetByIdOrNull<CardModel>(ModelId.Deserialize(serializedId));
                if (card == null)
                    Log.Warn(
                        $"[RitsuLib] Reward.FromSerializable: Ignoring unavailable custom card id '{serializedId}'.");
                return card;
            }
            catch (JsonException ex)
            {
                LogInvalidCardId(serializedId, ex);
            }
            catch (ArgumentException ex)
            {
                LogInvalidCardId(serializedId, ex);
            }
            catch (InvalidCastException ex)
            {
                LogInvalidCardId(serializedId, ex);
            }

            return null;
        }

        private static void LogInvalidCardId(string serializedId, Exception ex)
        {
            Log.Warn(
                $"[RitsuLib] Reward.FromSerializable: Ignoring invalid custom card id '{serializedId}': {ex.Message}");
        }
    }
}
