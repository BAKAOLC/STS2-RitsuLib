using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        private static readonly List<ActEnterForceEntry> ActEnterForces = [];
        private static readonly Dictionary<int, ActEnterPool<ActEnterResolveContext, Type>> ActEnterPools = [];
        private static int _actEnterForceTieBreakSeq;
        private static int _actEnterRegistrationCount;
        private static int _actEnterPostMapUiMapSyncBumpPending;

        private static readonly Action<RunState, IReadOnlyList<ActModel>> RunStateActsSetter =
            CreateRunStateActsSetter();

        private static readonly AccessTools.FieldRef<ActModel, List<AncientEventModel>?> ActSharedAncientSubsetRef =
            AccessTools.FieldRefAccess<ActModel, List<AncientEventModel>?>("_sharedAncientSubset");

        /// <summary>
        ///     <para xml:lang="en">Gets whether any act-entry force or pool rule has been registered.</para>
        ///     <para xml:lang="zh-CN">获取是否已注册任意章节进入强制规则或牌池规则。</para>
        /// </summary>
        public static bool HasAnyActEnterRegistration => Volatile.Read(ref _actEnterRegistrationCount) > 0;

        /// <summary>
        ///     <para xml:lang="en">Requests one multiplayer map-selection synchronization after act replacement.</para>
        ///     <para xml:lang="zh-CN">请求在替换章节后执行一次多人地图选择同步。</para>
        /// </summary>
        internal static void RequestActEnterPostMapUiMapSyncBump()
        {
            Interlocked.Exchange(ref _actEnterPostMapUiMapSyncBumpPending, 1);
        }

        internal static bool TryConsumeActEnterPostMapUiMapSyncBump()
        {
            return Interlocked.Exchange(ref _actEnterPostMapUiMapSyncBumpPending, 0) != 0;
        }

        private static Action<RunState, IReadOnlyList<ActModel>> CreateRunStateActsSetter()
        {
            var prop = typeof(RunState).GetProperty(nameof(RunState.Acts),
                BindingFlags.Public | BindingFlags.Instance);
            var set = prop?.GetSetMethod(true)
                      ?? throw new InvalidOperationException("RunState.Acts setter not found.");
            return (rs, acts) => set.Invoke(rs, [acts]);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a rule that replaces <paramref name="slotIndex" /> with
        ///         <typeparamref name="TAct" /> when eligible. Higher priority wins, with earlier registration
        ///         breaking ties.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一条在符合条件时将 <paramref name="slotIndex" /> 替换为
        ///         <typeparamref name="TAct" /> 的规则。优先级较高者胜出，同优先级时先注册者胜出。
        ///     </para>
        /// </summary>
        public void RegisterActEnterForce<TAct>(int slotIndex, int priority,
            Func<ActEnterResolveContext, bool> eligibility)
            where TAct : ActModel
        {
            ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
            ArgumentNullException.ThrowIfNull(eligibility);
            EnsureMutable($"register act enter force at slot {slotIndex}");
            EnsureModelType(typeof(TAct), typeof(ActModel), nameof(TAct));
            lock (SyncRoot)
            {
                EnsureMutable($"register act enter force at slot {slotIndex}");
                var tie = ++_actEnterForceTieBreakSeq;
                ActEnterForces.Add(new(slotIndex, static _ => typeof(TAct), priority, tie, eligibility));
                Interlocked.Increment(ref _actEnterRegistrationCount);
            }

            _logger.Info(
                $"[Content] Registered act enter force: slot {slotIndex} priority {priority} -> {typeof(TAct).Name}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a forced act-entry rule that selects only from the supplied weighted candidates.
        ///         Equal weights give equal probabilities. The existing act is included only when explicitly supplied.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一条仅从指定加权候选中选择的强制章节进入规则。
        ///         相同权重对应相同概率；只有显式提供时才会包含已有章节。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Register before content freezes. No ordinary pool declaration is required. Each call adds an
        ///         independent rule owned by this mod for the process lifetime; there is no per-run registration or removal.
        ///         The array is copied, validated and ordered by type name. Candidate types must be concrete, closed
        ///         act models; custom acts must also be registered as content before model initialization.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         必须在内容冻结前注册，无须声明普通池。每次调用为本模组添加一条独立规则，
        ///         在进程存续期间有效，不提供每局注册或移除。候选数组会被复制、验证并按类型名排序。
        ///         候选必须是具体、封闭的章节模型类型；自定义章节还须在模型初始化前注册为内容。
        ///     </para>
        ///     <para xml:lang="en">
        ///         At act entry, this rule competes with all single-act and candidate-set force rules.
        ///         Higher priority wins; equal priorities use earlier registration. Eligibility runs once when the rule
        ///         is reached, outside the registration lock, and must be deterministic and side-effect-free.
        ///         False continues to the next rule; if no rule matches, ordinary pool selection runs.
        ///         A winning rule draws once from the run's RNG, or does not draw for a single candidate.
        ///         Losing rules do not draw or evaluate candidate weights. Exceptions propagate before act replacement.
        ///         Selecting the existing act preserves its instance and still prevents lower rules and the ordinary pool.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         进入章节时，此规则与所有单章节和候选集合强制规则共同竞争。
        ///         优先级较高者胜出，同优先级时先注册者胜出。检查到此规则时，条件回调在注册锁外执行一次，
        ///         必须确定且无副作用。返回 false 时继续检查下一条规则；均不命中时进入普通池。
        ///         胜出的规则使用本局随机数源抽选一次，单候选不消耗随机数；未胜出的规则不抽选或计算候选权重。
        ///         异常在替换章节前向上传播。选中已有章节时保留其实例，且仍阻止后续规则及普通池执行。
        ///     </para>
        /// </remarks>
        /// <param name="slotIndex">
        ///     <para xml:lang="en">The non-negative, zero-based act slot; 1 is the second act.</para>
        ///     <para xml:lang="zh-CN">非负、从零开始的章节槽位；1 表示第二幕。</para>
        /// </param>
        /// <param name="priority">
        ///     <para xml:lang="en">The rule priority, compared with all force rules, including other mods' rules.</para>
        ///     <para xml:lang="zh-CN">规则优先级，与包括其他模组在内的所有强制规则比较。</para>
        /// </param>
        /// <param name="eligibility">
        ///     <para xml:lang="en">A non-null callback deciding whether this entire candidate set applies.</para>
        ///     <para xml:lang="zh-CN">决定整个候选集合是否适用的非空回调。</para>
        /// </param>
        /// <param name="candidates">
        ///     <para xml:lang="en">1 to 256 distinct, non-null act types and their finite, strictly positive weights.</para>
        ///     <para xml:lang="zh-CN">1 到 256 个互不重复的非空章节类型，以及各自有限且严格大于零的权重。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">The callback, array, or a candidate type is null.</para>
        ///     <para xml:lang="zh-CN">回调、数组或某个候选类型为空。</para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en">The slot is negative or the candidate count is outside [1, 256].</para>
        ///     <para xml:lang="zh-CN">槽位为负数或候选数量不在 [1, 256] 范围内。</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">A candidate type is invalid or duplicated, or a weight is non-finite or non-positive.</para>
        ///     <para xml:lang="zh-CN">候选类型无效或重复，或权重不是有限正数。</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">Content registration is frozen.</para>
        ///     <para xml:lang="zh-CN">内容注册已冻结。</para>
        /// </exception>
        public void RegisterActEnterForcePool(int slotIndex, int priority,
            Func<ActEnterResolveContext, bool> eligibility, params (Type ActType, double Weight)[] candidates)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
            ArgumentNullException.ThrowIfNull(eligibility);
            EnsureMutable($"register act enter force pool at slot {slotIndex}");
            var snapshot = PrepareActEnterForceCandidates(candidates);
            var weighted = snapshot
                .Select(candidate => new KeyValuePair<Type, double>(candidate.ActType, candidate.Weight))
                .ToArray();
            lock (SyncRoot)
            {
                EnsureMutable($"register act enter force pool at slot {slotIndex}");
                var tie = ++_actEnterForceTieBreakSeq;
                ActEnterForces.Add(new(slotIndex,
                    ctx => ActEnterPool<ActEnterResolveContext, Type>.SelectWeighted(weighted,
                        () => ctx.Rng.NextDouble()),
                    priority, tie, eligibility));
                Interlocked.Increment(ref _actEnterRegistrationCount);
            }

            _logger.Info(
                $"[Content] Registered act enter force pool: slot {slotIndex} priority {priority}, {weighted.Length} candidates");
        }

        internal static (Type ActType, double Weight)[] PrepareActEnterForceCandidates(
            (Type ActType, double Weight)[] candidates)
        {
            return ActEnterPool<ActEnterResolveContext, Type>.PrepareForcedCandidates(candidates,
                static type => EnsureModelType(type, typeof(ActModel), nameof(candidates)), GetActEnterCandidateKey);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Declares this mod's uniform contributions to a zero-based slot before content freezes. Repeating the
        ///         declaration is harmless; switching this mod's mode throws. Other mods may use weighted candidates in the
        ///         same slot. Uniform candidates contribute weight 1, and the existing act always has fixed weight 1.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在内容冻结前为本模组声明从零开始的槽位的均匀候选。重复声明无影响，切换本模组的模式会抛出异常。其他模组可在同一槽位使用加权候选。均匀候选与已有章节均以权重 1 参与抽选，已有章节的权重不可修改。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Registrations remain active for the process lifetime; there is no per-run registration or removal.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册在进程存续期间有效，不提供每局注册或移除。
        ///     </para>
        /// </remarks>
        public void RegisterActEnterUniformPool(int slotIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
            EnsureMutable($"register act enter uniform pool at slot {slotIndex}");
            lock (SyncRoot)
            {
                EnsureMutable($"register act enter uniform pool at slot {slotIndex}");
                GetOrCreateActEnterPool(slotIndex).Declare(ModId, ActEnterPoolModeKind.Uniform);
                Interlocked.Increment(ref _actEnterRegistrationCount);
            }

            _logger.Info($"[Content] Registered act enter uniform pool: slot {slotIndex}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a weight-1 candidate to this mod's previously declared uniform pool before content freezes.
        ///         Registering the same type twice in one mod and slot throws. Different mods' contributions for the same
        ///         type merge by maximum eligible weight. Candidates matching the existing act are ignored.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在内容冻结前向本模组已声明的均匀池添加权重为 1 的候选。同一模组在同一槽位重复注册类型会抛出异常；不同模组的同类型候选取最大有效权重。与已有章节相同的候选被忽略。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Registrations remain active for the process lifetime; there is no per-run registration or removal.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册在进程存续期间有效，不提供每局注册或移除。
        ///     </para>
        ///     <para xml:lang="en">
        ///         Callbacks execute outside the registration lock in ordinal, case-insensitive mod-ID order, then ordinal
        ///         type-name order. Each runs at most once per resolution, with eligibility before weight. They must be
        ///         deterministic and side-effect-free; exceptions propagate before act replacement.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         回调于注册锁外按模组 ID（不区分大小写）、类型名的序数顺序执行，每次解析至多执行一次，先判断条件再计算权重。回调必须确定且无副作用；异常在替换章节前向上传播。
        ///     </para>
        /// </remarks>
        public void RegisterActEnterUniformPoolCandidate<TAct>(int slotIndex,
            Func<ActEnterResolveContext, bool> eligibility)
            where TAct : ActModel
        {
            ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
            ArgumentNullException.ThrowIfNull(eligibility);
            EnsureMutable($"register act enter uniform pool candidate at slot {slotIndex}");
            EnsureModelType(typeof(TAct), typeof(ActModel), nameof(TAct));
            lock (SyncRoot)
            {
                EnsureMutable($"register act enter uniform pool candidate at slot {slotIndex}");
                RequireActEnterPool(slotIndex).AddCandidate(ModId, ActEnterPoolModeKind.Uniform,
                    typeof(TAct), eligibility, static _ => 1d);
                Interlocked.Increment(ref _actEnterRegistrationCount);
            }

            _logger.Info(
                $"[Content] Registered act enter uniform pool candidate: slot {slotIndex} -> {typeof(TAct).Name}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Declares this mod's weighted contributions to a zero-based slot before content freezes. Repeating the
        ///         declaration is harmless; switching this mod's mode throws. Other mods may contribute uniform candidates.
        ///         The existing act always participates with fixed weight 1 alongside eligible candidates.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在内容冻结前为本模组声明从零开始的槽位的加权候选。重复声明无影响，切换本模组的模式会抛出异常。其他模组可贡献均匀候选。已有章节始终以固定权重 1 与有效候选共同参与抽选。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Registrations remain active for the process lifetime; there is no per-run registration or removal.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册在进程存续期间有效，不提供每局注册或移除。
        ///     </para>
        /// </remarks>
        public void RegisterActEnterWeightedPool(int slotIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
            EnsureMutable($"register act enter weighted pool at slot {slotIndex}");
            lock (SyncRoot)
            {
                EnsureMutable($"register act enter weighted pool at slot {slotIndex}");
                GetOrCreateActEnterPool(slotIndex).Declare(ModId, ActEnterPoolModeKind.Weighted);
                Interlocked.Increment(ref _actEnterRegistrationCount);
            }

            _logger.Info($"[Content] Registered act enter weighted pool: slot {slotIndex}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a candidate to this mod's previously declared weighted pool before content freezes. Registering the
        ///         same type twice in one mod and slot throws. Different mods' contributions for the same type merge by
        ///         maximum eligible weight. Candidates matching the existing act are ignored. Non-finite and non-positive
        ///         weights are excluded.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在内容冻结前向本模组已声明的加权池添加候选。同一模组在同一槽位重复注册类型会抛出异常；不同模组的同类型候选取最大有效权重。与已有章节相同的候选被忽略。非有限值和非正权重的候选被排除。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Registrations remain active for the process lifetime; there is no per-run registration or removal.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册在进程存续期间有效，不提供每局注册或移除。
        ///     </para>
        ///     <para xml:lang="en">
        ///         Callbacks execute outside the registration lock in ordinal, case-insensitive mod-ID order, then ordinal
        ///         type-name order. Each runs at most once per resolution, with eligibility before weight. They must be
        ///         deterministic and side-effect-free; exceptions propagate before act replacement.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         回调于注册锁外按模组 ID（不区分大小写）、类型名的序数顺序执行，每次解析至多执行一次，先判断条件再计算权重。回调必须确定且无副作用；异常在替换章节前向上传播。
        ///     </para>
        /// </remarks>
        public void RegisterActEnterWeightedPoolCandidate<TAct>(int slotIndex,
            Func<ActEnterResolveContext, bool> eligibility, Func<ActEnterResolveContext, double> weight)
            where TAct : ActModel
        {
            ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
            ArgumentNullException.ThrowIfNull(eligibility);
            ArgumentNullException.ThrowIfNull(weight);
            EnsureMutable($"register act enter weighted pool candidate at slot {slotIndex}");
            EnsureModelType(typeof(TAct), typeof(ActModel), nameof(TAct));
            lock (SyncRoot)
            {
                EnsureMutable($"register act enter weighted pool candidate at slot {slotIndex}");
                RequireActEnterPool(slotIndex).AddCandidate(ModId, ActEnterPoolModeKind.Weighted,
                    typeof(TAct), eligibility, weight);
                Interlocked.Increment(ref _actEnterRegistrationCount);
            }

            _logger.Info(
                $"[Content] Registered act enter weighted pool candidate: slot {slotIndex} -> {typeof(TAct).Name}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Retained for compatibility. The existing act always participates with weight 1.
        ///         The supplied callback is validated for null, but is neither stored nor invoked.
        ///         This call does not declare a pool or change its candidates.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为兼容旧调用而保留。已有章节始终以权重 1 参与抽选。
        ///         传入的回调仅检查非空，不保存也不执行；此调用不声明池，也不改变候选。
        ///     </para>
        /// </summary>
        /// <param name="slotIndex">
        ///     <para xml:lang="en">The non-negative, zero-based act slot.</para>
        ///     <para xml:lang="zh-CN">非负、从零开始的章节槽位。</para>
        /// </param>
        /// <param name="weight">
        ///     <para xml:lang="en">An unused, non-null legacy weight provider.</para>
        ///     <para xml:lang="zh-CN">不再使用的非空旧式权重提供器。</para>
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en">The slot is negative.</para>
        ///     <para xml:lang="zh-CN">槽位为负数。</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">The weight provider is null.</para>
        ///     <para xml:lang="zh-CN">权重提供器为空。</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">Content registration is frozen.</para>
        ///     <para xml:lang="zh-CN">内容注册已冻结。</para>
        /// </exception>
        [Obsolete("The existing act has fixed weight 1. Remove this call and configure only candidate weights.")]
        public void RegisterActEnterWeightedPoolBaseline(int slotIndex,
            Func<ActEnterResolveContext, double> weight)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
            ArgumentNullException.ThrowIfNull(weight);
            EnsureMutable($"register act enter weighted pool baseline at slot {slotIndex}");
        }

        internal static void ResolveActEnterForEnterAct(RunManager runManager, RunState runState, int enteringActIndex)
        {
            if (!HasAnyActEnterRegistration)
                return;

            if ((uint)enteringActIndex >= (uint)runState.Acts.Count)
                return;

            ActEnterForceEntry[] forces;
            ActEnterPool<ActEnterResolveContext, Type>.Snapshot? pool;
            lock (SyncRoot)
            {
                forces = [.. ActEnterForces];
                pool = ActEnterPools.TryGetValue(enteringActIndex, out var registeredPool)
                    ? registeredPool.CreateSnapshot(GetActEnterCandidateKey)
                    : null;
            }

            var isMp = runManager.NetService != null && runManager.NetService.Type != NetGameType.Singleplayer;
            var ctx = new ActEnterResolveContext(runManager, runState, enteringActIndex, runState.Rng.Niche,
                runState.UnlockState, isMp);

            Array.Sort(forces, static (a, b) =>
            {
                var p = b.Priority.CompareTo(a.Priority);
                return p != 0 ? p : a.TieBreakOrder.CompareTo(b.TieBreakOrder);
            });

            foreach (var f in forces)
            {
                if (f.SlotIndex != enteringActIndex)
                    continue;

                if (!f.Eligibility(ctx))
                    continue;

                var forcedType = f.Select(ctx);
                if (forcedType != runState.Acts[enteringActIndex].GetType())
                    ReplaceActAtSlot(runManager, runState, enteringActIndex, forcedType);
                return;
            }

            if (pool == null)
                return;

            var baselineType = runState.Acts[enteringActIndex].GetType();
            var selected = pool.Select(ctx, baselineType, GetActEnterCandidateKey, () => ctx.Rng.NextDouble());
            if (selected != baselineType)
                ReplaceActAtSlot(runManager, runState, enteringActIndex, selected);
        }

        private static string GetActEnterCandidateKey(Type type)
        {
            return type.FullName ?? type.Name;
        }

        private static ActEnterPool<ActEnterResolveContext, Type> GetOrCreateActEnterPool(int slotIndex)
        {
            if (ActEnterPools.TryGetValue(slotIndex, out var pool))
                return pool;
            pool = new();
            ActEnterPools.Add(slotIndex, pool);
            return pool;
        }

        private static ActEnterPool<ActEnterResolveContext, Type> RequireActEnterPool(int slotIndex)
        {
            if (!ActEnterPools.TryGetValue(slotIndex, out var pool))
                throw new InvalidOperationException($"Slot {slotIndex}: declare an act-entry pool first.");
            return pool;
        }

        private static void ReplaceActAtSlot(RunManager runManager, RunState runState, int slotIndex, Type actType)
        {
            var beforeAct = runState.Acts[slotIndex];
            var list = runState.Acts.ToList();
            var replacement = ModelDb.GetById<ActModel>(ModelDb.GetId(actType)).ToMutable();
            CopySharedAncientSubset(beforeAct, replacement);
            list[slotIndex] = replacement;
            RunStateActsSetter(runState, list);
            InitializeRoomsForReplacedActIfNeeded(runManager, runState, slotIndex, beforeAct);
        }

        private static void CopySharedAncientSubset(ActModel source, ActModel target)
        {
            if (ReferenceEquals(source, target))
                return;

            var sourceSubset = ActSharedAncientSubsetRef(source);
            ActSharedAncientSubsetRef(target) = sourceSubset == null ? null : [.. sourceSubset];
        }

        private static void InitializeRoomsForReplacedActIfNeeded(RunManager runManager, RunState runState,
            int actIndex,
            ActModel actBeforeReplace)
        {
            var act = runState.Acts[actIndex];
            if (ReferenceEquals(act, actBeforeReplace))
                return;

            RequestActEnterPostMapUiMapSyncBump();

            act.AssertMutable();
            act.GenerateRooms(runState.Rng.UpFront, runState.UnlockState, runState.Players.Count > 1);
            if (runManager.ShouldApplyTutorialModifications())
                act.ApplyDiscoveryOrderModifications(runState.UnlockState);

            if (actIndex != runState.Acts.Count - 1 ||
                !runManager.AscensionManager.HasLevel(AscensionLevel.DoubleBoss)) return;
            var secondBoss = runState.Rng.UpFront.NextItem(
                act.AllBossEncounters.Where(e => e.Id != act.BossEncounter.Id));
            act.SetSecondBossEncounter(secondBoss);
        }


        private readonly record struct ActEnterForceEntry(
            int SlotIndex,
            Func<ActEnterResolveContext, Type> Select,
            int Priority,
            int TieBreakOrder,
            Func<ActEnterResolveContext, bool> Eligibility);
    }
}
