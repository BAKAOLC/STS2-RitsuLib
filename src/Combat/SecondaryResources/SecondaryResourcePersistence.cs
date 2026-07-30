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
    ///     <para xml:lang="en">Saves and restores persistent secondary-resource state.</para>
    ///     <para xml:lang="zh-CN">保存和恢复需要持久化的次级资源状态。</para>
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
        ///     <para xml:lang="en">Registers the combat lifecycle handlers used for persistence.</para>
        ///     <para xml:lang="zh-CN">注册持久化所用的战斗生命周期处理器。</para>
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
        ///     <para xml:lang="en">
        ///         Creates a serializable snapshot. Run-scoped resources are always included; combat-scoped resources
        ///         are included only when <paramref name="includeCombatScoped" /> is <see langword="true" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建可序列化快照。始终包含跑局范围资源；仅当 <paramref name="includeCombatScoped" /> 为
        ///         <see langword="true" /> 时包含战斗范围资源。
        ///     </para>
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
        ///     <para xml:lang="en">Restores registered run- and combat-scoped values into a combat.</para>
        ///     <para xml:lang="zh-CN">将已注册的跑局范围与战斗范围资源值恢复到一场战斗中。</para>
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
    ///     <para xml:lang="en">Stores serializable secondary-resource state grouped by player network ID.</para>
    ///     <para xml:lang="zh-CN">存储按玩家网络 ID 分组的可序列化次级资源状态。</para>
    /// </summary>
    public sealed class SecondaryResourceRunSaveState
    {
        /// <summary>
        ///     <para xml:lang="en">Gets or sets amounts indexed first by player network ID, then by resource ID.</para>
        ///     <para xml:lang="zh-CN">获取或设置先按玩家网络 ID、再按资源 ID 索引的数量。</para>
        /// </summary>
        public Dictionary<ulong, Dictionary<string, int>> PlayerAmounts { get; set; } = [];

        /// <summary>
        ///     <para xml:lang="en">Gets whether no resource amounts are stored.</para>
        ///     <para xml:lang="zh-CN">获取是否未存储任何资源数量。</para>
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
