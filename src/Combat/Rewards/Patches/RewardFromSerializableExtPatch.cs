using System.Text.Json;
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
    ///     Extends <see cref="Reward.FromSerializable" /> to reconstruct registered custom reward types
    ///     and card reward serialization-fix sideband data.
    ///     扩展 <see cref="Reward.FromSerializable" />，用于重建已注册的自定义 reward 类型
    ///     以及卡牌 reward 序列化修正的 sideband 数据。
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

        public static bool Prefix(SerializableReward save, Player player, ref Reward __result)
        {
            RewardSerializationExt.TryGetExtData(save, out var ext);

            if (ModRewardRegistry.TryCreate(save.RewardType, save, player, ext?.CustomRewardJson, out var customReward)
                && customReward != null)
            {
                __result = customReward;
                return false;
            }

            if (RewardSerializationExt.IsBaselibRewardPatchLoaded())
                return true;

            if (save.RewardType != RewardType.Card || ext == null)
                return true;

            __result = RebuildCardReward(save, ext, player);
            return false;
        }

        private static CardReward RebuildCardReward(
            SerializableReward save, RewardExtData ext, Player player)
        {
            var flags = (CardCreationFlags)ext.Flags;

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

            List<CardPoolModel> pools = [];
            foreach (var poolId in save.CardPoolIds ?? [])
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

            var poolOptions = new CardCreationOptions(pools, save.Source, save.RarityOdds);
            if (flags != 0)
                poolOptions.WithFlags(flags);

            return new(poolOptions, save.OptionCount, player);
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
