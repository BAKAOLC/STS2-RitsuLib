using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.Costs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Cards.Patches
{
    /// <summary>
    ///     After vanilla energy and star checks pass, evaluate custom play costs. If unaffordable, sets
    ///     <see cref="UnplayableReason.BlockedByCardLogic" /> (vanilla enum cannot be extended).
    /// </summary>
    public sealed class CustomCardPlayCostHasEnoughResourcesPatch : IPatchMethod
    {
        [ThreadStatic] private static List<ICardCustomPlayCost>? _scratch;

        public static string PatchId => "custom_card_play_cost_has_resources";
        public static string Description => "Merge custom card play costs into PlayerCombatState.HasEnoughResourcesFor";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PlayerCombatState), nameof(PlayerCombatState.HasEnoughResourcesFor),
                    [typeof(CardModel), typeof(UnplayableReason).MakeByRefType()]),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(CardModel card, ref bool __result, ref UnplayableReason reason)
        {
            if (!__result)
                return;

            _scratch ??= [];
            CustomCardPlayCostContributors.Collect(card, _scratch);
            if (_scratch.Count == 0)
                return;

            var player = card.Owner;
            if (_scratch.All(cost => cost.IsAffordable(card, player))) return;
            __result = false;
            reason |= UnplayableReason.BlockedByCardLogic;
        }
        // ReSharper restore InconsistentNaming
    }

    /// <summary>
    ///     After vanilla <see cref="CardModel.SpendResources" /> spends energy and stars, spend custom costs in order.
    /// </summary>
    public sealed class CustomCardPlayCostSpendResourcesPatch : IPatchMethod
    {
        [ThreadStatic] private static List<ICardCustomPlayCost>? _scratch;

        public static string PatchId => "custom_card_play_cost_spend_resources";
        public static string Description => "Chain custom card play costs after CardModel.SpendResources";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), nameof(CardModel.SpendResources)),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(CardModel __instance, ref Task<(int, int)> __result)
        {
            __result = ChainAfterVanillaSpend(__result, __instance);
        }

        private static async Task<(int, int)> ChainAfterVanillaSpend(Task<(int, int)> vanilla, CardModel card)
        {
            var tuple = await vanilla;

            _scratch ??= [];
            CustomCardPlayCostContributors.Collect(card, _scratch);
            if (_scratch.Count == 0)
                return tuple;

            var player = card.Owner;
            var spendRecords = new List<CustomCardPlayCostSpendRecord>();
            foreach (var cost in _scratch)
            {
                await cost.SpendAsync(card, player).ConfigureAwait(false);
                if (cost is not ICardCustomPlayCostSpendRecordable recordable) continue;
                var line = recordable.TryBuildSpendRecord(card, player);
                if (line is { KindId: not null and not "" })
                    spendRecords.Add(line.Value);
            }

            if (spendRecords.Count > 0 && card.CombatState is { } combatState)
                CustomCardPlayCostLedger.AppendAndRaise(combatState, card, player, spendRecords);

            return tuple;
        }
        // ReSharper restore InconsistentNaming
    }
}
