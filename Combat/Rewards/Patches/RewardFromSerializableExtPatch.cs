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
    ///     Extends <see cref="Reward.FromSerializable" /> to reconstruct
    ///     <see cref="CardCreationOptions" /> with Flags and CustomCardPool from sideband data.
    /// </summary>
    internal sealed class RewardFromSerializableExtPatch : IPatchMethod
    {
        public static string PatchId => "reward_from_serializable_ext";

        public static string Description =>
            "Extend Reward.FromSerializable with sideband ext data and RewardType.None fallback";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Reward), nameof(Reward.FromSerializable),
                    [typeof(SerializableReward), typeof(Player)]),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(SerializableReward save, Player player, ref Reward __result)
            // ReSharper restore InconsistentNaming
        {
            if (RewardSerializationExt.IsBaselibRewardPatchLoaded())
                return true;

            if (save.RewardType != RewardType.Card
                || !RewardSerializationExt.TryGetExtData(save, out var ext)
                || ext == null) return true;
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
                    .Select(id => ModelDb.GetByIdOrNull<CardModel>(ModelId.Deserialize(id)))
                    .Where(c => c != null)
                    .Select(c => c!)
                    .ToList();

                if (cards.Count > 0)
                {
                    var options = new CardCreationOptions(cards, source, rarityOdds);
                    if (flags != 0) options.WithFlags(flags);
                    return new CardReward(options, save.OptionCount, player);
                }

                Log.Warn("[RitsuLib] Reward.FromSerializable: CustomCardPool had no resolvable cards, " +
                         "falling back to standard card reward.");
            }

            var pools = save.CardPoolIds
                .Select(ModelDb.GetById<CardPoolModel>)
                .ToList();
            var poolOptions = new CardCreationOptions(pools, save.Source, save.RarityOdds);
            if (flags != 0)
                poolOptions.WithFlags(flags);

            return new CardReward(poolOptions, save.OptionCount, player);
        }
    }
}
