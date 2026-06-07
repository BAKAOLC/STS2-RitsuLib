#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.RunData;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Save and restore helpers for secondary-resource state.
    ///     次级资源状态的保存和恢复辅助工具。
    /// </summary>
    public static class SecondaryResourcePersistence
    {
        private const string SaveKey = "secondary_resources";

        private static readonly RunSavedData<SecondaryResourceRunSaveState> SavedData =
            RunSavedDataStore.For(Const.ModId).Register<SecondaryResourceRunSaveState>(
                SaveKey,
                () => new(),
                new() { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault });

        private static bool _initialized;

        /// <summary>
        ///     Registers lifecycle persistence hooks.
        ///     注册生命周期持久化 hook。
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(OnCombatStarting);
            RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(OnCombatEnded);
        }

        /// <summary>
        ///     Creates a serializable snapshot for selected persistence policies.
        ///     为选定的持久化策略创建可序列化快照。
        /// </summary>
        public static SecondaryResourceRunSaveState CreateSnapshot(
            CombatStateLike combatState,
            bool includeCombatScoped)
        {
            ArgumentNullException.ThrowIfNull(combatState);

            var snapshot = new SecondaryResourceRunSaveState();
            if (!ModSecondaryResourceRegistry.HasAny)
                return snapshot;

            foreach (var player in combatState.Players)
                CapturePlayer(player, snapshot, includeCombatScoped);

            return snapshot;
        }

        /// <summary>
        ///     Restores a snapshot into current combat state.
        ///     将快照恢复到当前战斗状态中。
        /// </summary>
        public static void RestoreSnapshot(CombatStateLike combatState, SecondaryResourceRunSaveState snapshot)
        {
            ArgumentNullException.ThrowIfNull(combatState);
            ArgumentNullException.ThrowIfNull(snapshot);

            if (!ModSecondaryResourceRegistry.HasAny)
                return;

            foreach (var player in combatState.Players)
                if (snapshot.PlayerAmounts.TryGetValue(player.NetId, out var amounts))
                    RestorePlayer(player, amounts);
        }

        internal static void SyncRunScopedToSavedData(RunState runState, CombatStateLike combatState)
        {
            if (!ModSecondaryResourceRegistry.HasAny)
            {
                SavedData.Remove(runState);
                return;
            }

            var snapshot = CreateSnapshot(combatState, false);
            if (snapshot.IsEmpty)
                SavedData.Remove(runState);
            else
                SavedData.Set(runState, snapshot);
        }

        private static void OnCombatStarting(CombatStartingEvent evt)
        {
            if (!ModSecondaryResourceRegistry.HasAny ||
                evt.RunState is not RunState runState ||
                evt.CombatState == null ||
                !SavedData.TryGet(runState, out var snapshot))
                return;

            RestoreSnapshot(evt.CombatState, snapshot);
        }

        private static void OnCombatEnded(CombatEndedEvent evt)
        {
            if (evt is { RunState: RunState runState, CombatState: not null })
                SyncRunScopedToSavedData(runState, evt.CombatState);
        }

        private static void CapturePlayer(
            Player player,
            SecondaryResourceRunSaveState snapshot,
            bool includeCombatScoped)
        {
            if (!SecondaryResourceStateStore.TryGet(player, out var state))
                return;

            foreach (var (resourceId, amount) in state.Snapshot())
            {
                if (!ModSecondaryResourceRegistry.TryGet(resourceId, out var definition))
                    continue;

                if (definition.PersistencePolicy == SecondaryResourcePersistencePolicy.Run ||
                    (includeCombatScoped && definition.PersistencePolicy == SecondaryResourcePersistencePolicy.Combat))
                    snapshot.Set(player.NetId, resourceId, amount);
            }
        }

        private static void RestorePlayer(Player player, Dictionary<string, int> amounts)
        {
            foreach (var (resourceId, amount) in amounts)
            {
                if (!ModSecondaryResourceRegistry.TryGet(resourceId, out var definition))
                    continue;

                if (definition.PersistencePolicy is SecondaryResourcePersistencePolicy.Run
                    or SecondaryResourcePersistencePolicy.Combat)
                    SecondaryResourceStateStore.SetFromPersistence(player, resourceId, amount);
            }
        }
    }

    /// <summary>
    ///     Serializable secondary-resource state grouped by player net id.
    ///     按玩家 net id 分组的可序列化次级资源状态。
    /// </summary>
    public sealed class SecondaryResourceRunSaveState
    {
        /// <summary>
        ///     Resource amounts by player net id and resource id.
        ///     按玩家 net id 和资源 id 存储的资源数量。
        /// </summary>
        public Dictionary<ulong, Dictionary<string, int>> PlayerAmounts { get; set; } = [];

        /// <summary>
        ///     True when no resource amounts are stored.
        ///     未存储任何资源数量时为 true。
        /// </summary>
        public bool IsEmpty => PlayerAmounts.Count == 0 ||
                               PlayerAmounts.Values.All(static amounts => amounts.Count == 0);

        internal void Set(ulong playerNetId, string resourceId, int amount)
        {
            if (!PlayerAmounts.TryGetValue(playerNetId, out var amounts))
            {
                amounts = new(StringComparer.OrdinalIgnoreCase);
                PlayerAmounts[playerNetId] = amounts;
            }

            amounts[resourceId] = amount;
        }
    }
}
