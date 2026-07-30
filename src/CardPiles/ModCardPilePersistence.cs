using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.RunData;

namespace STS2RitsuLib.CardPiles
{
    internal static class ModCardPilePersistence
    {
        private const string SaveKey = "run_persistent_card_piles";

        private static readonly PlayerRunSavedData<ModCardPilePlayerSaveState> SavedData =
            RunSavedDataStore.For(Const.ModId).RegisterPerPlayer<ModCardPilePlayerSaveState>(
                SaveKey,
                () => new(),
                new() { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault });

        public static void Initialize()
        {
            _ = SavedData;
        }

        internal static void SyncToSavedData(RunState runState)
        {
            ArgumentNullException.ThrowIfNull(runState);

            if (!HasRunPersistentDefinitions())
                return;

            foreach (var player in runState.Players)
            {
                SavedData.TryGet(runState, player.NetId, out var existing);

                var snapshot = CreateSnapshot(player, existing);
                if (snapshot.IsEmpty)
                    SavedData.Remove(runState, player.NetId);
                else
                    SavedData.Set(runState, player.NetId, snapshot);
            }
        }

        internal static void RestoreFromSavedData(RunState runState)
        {
            ArgumentNullException.ThrowIfNull(runState);

            if (!HasRunPersistentDefinitions())
                return;

            foreach (var player in runState.Players)
            {
                if (!SavedData.TryGet(runState, player.NetId, out var snapshot) || snapshot.IsEmpty)
                    continue;

                RestorePlayer(runState, player, snapshot);
            }
        }

        private static ModCardPilePlayerSaveState CreateSnapshot(
            Player player,
            ModCardPilePlayerSaveState? existing)
        {
            var snapshot = new ModCardPilePlayerSaveState();

            if (existing != null)
                foreach (var (pileId, cards) in existing.Piles)
                    if (!ModCardPileRegistry.TryGet(pileId, out _))
                        snapshot.Piles[pileId] = cards;

            foreach (var pile in ModCardPileStorage.GetRunPiles(player))
            {
                if (pile.Definition.Scope != ModCardPileScope.RunPersistent || pile.Cards.Count == 0)
                    continue;

                snapshot.Piles[pile.Definition.Id] =
                [
                    .. pile.Cards
                        .Select(card => card.ToSerializable()),
                ];
            }

            return snapshot;
        }

        private static void RestorePlayer(
            RunState runState,
            Player player,
            ModCardPilePlayerSaveState snapshot)
        {
            foreach (var (pileId, cards) in snapshot.Piles)
            {
                if (cards.Count == 0)
                    continue;

                if (!ModCardPileRegistry.TryGet(pileId, out var definition) ||
                    definition.Scope != ModCardPileScope.RunPersistent)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[CardPiles] Saved run-persistent pile '{pileId}' is not registered; "
                        + "its cards cannot be restored.");
                    continue;
                }

                var pile = ModCardPileStorage.Resolve(definition.PileType, player);
                if (pile == null)
                    continue;

                pile.Clear(true);
                foreach (var serializableCard in cards)
                    RestoreCard(runState, player, pile, serializableCard);
            }
        }

        private static void RestoreCard(
            RunState runState,
            Player player,
            CardPile pile,
            SerializableCard serializableCard)
        {
            CardModel card;
            try
            {
                card = runState.LoadCard(serializableCard, player);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardPiles] Failed to load a card for run-persistent pile '{pile.Type}': {ex}");
                return;
            }

            pile.AddInternal(card, -1, true);
        }

        private static bool HasRunPersistentDefinitions()
        {
            return ModCardPileRegistry.GetDefinitionsSnapshot()
                .Any(static definition => definition.Scope == ModCardPileScope.RunPersistent);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Represents one player's serializable run-persistent pile contents.</para>
    ///     <para xml:lang="zh-CN">表示单个玩家可序列化的局内持久牌堆内容。</para>
    /// </summary>
    public sealed class ModCardPilePlayerSaveState
    {
        /// <summary>
        ///     <para xml:lang="en">Gets or sets serialized cards grouped by registered pile ID.</para>
        ///     <para xml:lang="zh-CN">获取或设置按已注册牌堆 ID 分组的序列化卡牌。</para>
        /// </summary>
        public Dictionary<string, List<SerializableCard>> Piles { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     <para xml:lang="en">Gets whether the state contains no cards.</para>
        ///     <para xml:lang="zh-CN">获取该状态是否未包含任何卡牌。</para>
        /// </summary>
        public bool IsEmpty => Piles.Count == 0 || Piles.Values.All(static cards => cards.Count == 0);
    }
}
