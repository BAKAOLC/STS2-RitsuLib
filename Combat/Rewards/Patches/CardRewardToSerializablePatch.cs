using HarmonyLib;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.Rewards.Patches
{
    /// <summary>
    ///     Replaces <see cref="CardReward.ToSerializable" /> to handle Flags, CustomCardPool,
    ///     and CardPoolFilter without throwing <see cref="NotImplementedException" />.
    ///     Extended data is stored via <see cref="RewardSerializationExt" /> sideband and later
    ///     written to <c>EncounterState</c> by the room serialization postfix.
    /// </summary>
    internal sealed class CardRewardToSerializablePatch : IPatchMethod
    {
        private static readonly Func<CardReward, CardCreationOptions> GetOptions =
            AccessTools.MethodDelegate<Func<CardReward, CardCreationOptions>>(
                AccessTools.DeclaredPropertyGetter(typeof(CardReward), "Options"));

        private static readonly Func<CardReward, int> GetOptionCount =
            AccessTools.MethodDelegate<Func<CardReward, int>>(
                AccessTools.DeclaredPropertyGetter(typeof(CardReward), "OptionCount"));

        public static string PatchId => "card_reward_to_serializable_ext";

        public static string Description =>
            "Fix CardReward.ToSerializable for Flags, CustomCardPool and CardPoolFilter";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardReward), nameof(CardReward.ToSerializable), Type.EmptyTypes)];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CardReward __instance, ref SerializableReward __result)
            // ReSharper restore InconsistentNaming
        {
            if (RewardSerializationExt.IsBaselibRewardPatchLoaded())
                return true;

            var options = GetOptions(__instance);
            var hasFlags = options.Flags != 0;
            var hasFilter = options.CardPoolFilter != null;
            var hasNoPools = options.CardPools.Count <= 0;

            if (!hasFlags && !hasFilter && !hasNoPools)
                return true;

            var result = new SerializableReward { RewardType = RewardType.Card };
            RewardExtData? ext = null;

            if (hasNoPools && options.CustomCardPool != null)
            {
                ext = BuildCustomPoolExt(options);
                result.Source = options.Source;
                result.RarityOdds = options.RarityOdds;
            }
            else if (hasFilter && options.CardPools.Count > 0)
            {
                ext = BuildFilterSnapshotExt(options, __instance);
                result.Source = options.Source;
                result.RarityOdds = options.RarityOdds;
            }
            else
            {
                result.Source = options.Source;
                result.RarityOdds = options.RarityOdds;
                result.CardPoolIds = options.CardPools.Select(p => p.Id).ToList();
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

        private static RewardExtData BuildCustomPoolExt(CardCreationOptions options)
        {
            return new()
            {
                IsCustomPool = true,
                CustomCardIds = options.CustomCardPool!.Select(c => c.Id.ToString()).ToList(),
                Source = (int)options.Source,
                RarityOdds = (int)options.RarityOdds,
            };
        }

        private static RewardExtData BuildFilterSnapshotExt(
            CardCreationOptions options, CardReward reward)
        {
            var allCards = options.CardPools
                .SelectMany(p => p.AllCards)
                .Where(options.CardPoolFilter!)
                .ToList();

            return new()
            {
                IsCustomPool = true,
                CustomCardIds = allCards.Select(c => c.Id.ToString()).ToList(),
                Source = (int)options.Source,
                RarityOdds = (int)options.RarityOdds,
            };
        }
    }
}
