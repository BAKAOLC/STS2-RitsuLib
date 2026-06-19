using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Relics.Patches
{
    /// <summary>
    ///     Lets mods provide per-character Dusty Tome ancient-card candidates before vanilla unlocked-card selection.
    ///     允许 mod 在原版已解锁卡牌选择前，为每个角色提供 Dusty Tome ancient 卡牌候选。
    /// </summary>
    internal sealed class DustyTomeSetupForPlayerPatch : IPatchMethod
    {
        public static string PatchId => "dusty_tome_mod_ancient_cards";

        public static string Description => "Prefer RitsuLib-registered Dusty Tome ancient card candidates";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(DustyTome), nameof(DustyTome.SetupForPlayer), [typeof(Player)]),
            ];
        }

        public static bool Prefix(DustyTome __instance, Player player)
        {
            if (DustyTomeCardRegistry.TryGetPreferredCards(player.Character.Id, out var ritsuLibCandidates))
                if (SelectCardId(player, ritsuLibCandidates) is { } cardId)
                {
                    __instance.AncientCard = cardId;
                    return false;
                }

            var transcendenceCardIds = ArchaicTooth.TranscendenceCards
                .Select(static card => card.Id)
                .ToHashSet();
            var vanillaCandidates = player.Character.CardPool
                .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                .Where(card => card.Rarity == CardRarity.Ancient && !transcendenceCardIds.Contains(card.Id))
                .ToArray();

            if (vanillaCandidates.Length > 0)
                if (SelectCardId(player, vanillaCandidates) is { } cardId)
                {
                    __instance.AncientCard = cardId;
                    return false;
                }

            var lockedCandidates = player.Character.CardPool.AllCards
                .Where(card => card.Rarity == CardRarity.Ancient && !transcendenceCardIds.Contains(card.Id))
                .ToArray();
            if (lockedCandidates.Length == 0)
                return true;

            RitsuLibFramework.Logger.Warn(
                $"[DustyTomeCards] {player.Character.Id} has no unlocked non-transcendence Ancient cards; " +
                "falling back to locked character-pool Ancient cards to avoid Dusty Tome setup failure.");
            if (SelectCardId(player, lockedCandidates) is not { } lockedCardId)
                return true;

            __instance.AncientCard = lockedCardId;
            return false;
        }

        private static ModelId? SelectCardId(Player player, IReadOnlyList<CardModel> candidates)
        {
            return player.PlayerRng.Rewards.NextItem(candidates)?.Id;
        }
    }
}
