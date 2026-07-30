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
        private static readonly Dictionary<int, ActEnterPoolModeKind> ActEnterPoolModes = [];
        private static readonly List<ActEnterUniformPoolCandidateEntry> ActEnterUniformPoolCandidates = [];
        private static readonly List<ActEnterWeightedPoolCandidateEntry> ActEnterWeightedPoolCandidates = [];
        private static readonly Dictionary<int, Func<ActEnterResolveContext, double>> ActEnterWeightedBaselines = [];
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
                var tie = ++_actEnterForceTieBreakSeq;
                ActEnterForces.Add(new(slotIndex, typeof(TAct), priority, tie, eligibility));
                Interlocked.Increment(ref _actEnterRegistrationCount);
            }

            _logger.Info(
                $"[Content] Registered act enter force: slot {slotIndex} priority {priority} -> {typeof(TAct).Name}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Declares a uniform act-entry pool for <paramref name="slotIndex" />. Register this before
        ///         adding candidates.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="slotIndex" /> 声明均匀章节进入池。必须先声明再添加候选章节。
        ///     </para>
        /// </summary>
        public void RegisterActEnterUniformPool(int slotIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
            EnsureMutable($"register act enter uniform pool at slot {slotIndex}");
            lock (SyncRoot)
            {
                if (ActEnterPoolModes.TryGetValue(slotIndex, out var existing) &&
                    existing != ActEnterPoolModeKind.Uniform)
                    throw new InvalidOperationException(
                        $"Act slot {slotIndex} already uses {existing}; cannot register uniform pool.");

                ActEnterPoolModes[slotIndex] = ActEnterPoolModeKind.Uniform;
                Interlocked.Increment(ref _actEnterRegistrationCount);
            }

            _logger.Info($"[Content] Registered act enter uniform pool: slot {slotIndex}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds an eligible candidate to the uniform pool for <paramref name="slotIndex" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="slotIndex" /> 的均匀池添加符合条件的候选章节。
        ///     </para>
        /// </summary>
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
                RequirePoolMode(slotIndex, ActEnterPoolModeKind.Uniform, "uniform pool candidate");
                ActEnterUniformPoolCandidates.Add(new(slotIndex, typeof(TAct), eligibility));
                Interlocked.Increment(ref _actEnterRegistrationCount);
            }

            _logger.Info(
                $"[Content] Registered act enter uniform pool candidate: slot {slotIndex} -> {typeof(TAct).Name}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Declares a weighted act-entry pool for <paramref name="slotIndex" />. Register this before
        ///         adding candidates or a baseline.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="slotIndex" /> 声明加权章节进入池。必须先声明再添加候选章节或基线。
        ///     </para>
        /// </summary>
        public void RegisterActEnterWeightedPool(int slotIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
            EnsureMutable($"register act enter weighted pool at slot {slotIndex}");
            lock (SyncRoot)
            {
                if (ActEnterPoolModes.TryGetValue(slotIndex, out var existing) &&
                    existing != ActEnterPoolModeKind.Weighted)
                    throw new InvalidOperationException(
                        $"Act slot {slotIndex} already uses {existing}; cannot register weighted pool.");

                ActEnterPoolModes[slotIndex] = ActEnterPoolModeKind.Weighted;
                Interlocked.Increment(ref _actEnterRegistrationCount);
            }

            _logger.Info($"[Content] Registered act enter weighted pool: slot {slotIndex}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds an eligible candidate and its weight provider to a weighted act-entry pool.
        ///     </para>
        ///     <para xml:lang="zh-CN">向加权章节进入池添加符合条件的候选章节及其权重提供器。</para>
        /// </summary>
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
                RequirePoolMode(slotIndex, ActEnterPoolModeKind.Weighted, "weighted pool candidate");
                ActEnterWeightedPoolCandidates.Add(new(slotIndex, typeof(TAct), eligibility, weight));
                Interlocked.Increment(ref _actEnterRegistrationCount);
            }

            _logger.Info(
                $"[Content] Registered act enter weighted pool candidate: slot {slotIndex} -> {typeof(TAct).Name}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers the weight of the act already occupying <paramref name="slotIndex" />. Weighted
        ///         pools have no implicit baseline.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册 <paramref name="slotIndex" /> 中已有章节的权重。加权池没有隐式基线。
        ///     </para>
        /// </summary>
        public void RegisterActEnterWeightedPoolBaseline(int slotIndex,
            Func<ActEnterResolveContext, double> weight)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
            ArgumentNullException.ThrowIfNull(weight);
            EnsureMutable($"register act enter weighted pool baseline at slot {slotIndex}");
            lock (SyncRoot)
            {
                RequirePoolMode(slotIndex, ActEnterPoolModeKind.Weighted, "weighted pool baseline");
                ActEnterWeightedBaselines[slotIndex] = weight;
                Interlocked.Increment(ref _actEnterRegistrationCount);
            }

            _logger.Info($"[Content] Registered act enter weighted pool baseline: slot {slotIndex}");
        }

        internal static void ResolveActEnterForEnterAct(RunManager runManager, RunState runState, int enteringActIndex)
        {
            if (!HasAnyActEnterRegistration)
                return;

            if ((uint)enteringActIndex >= (uint)runState.Acts.Count)
                return;

            ActEnterForceEntry[] forces;
            Dictionary<int, ActEnterPoolModeKind> poolModes;
            ActEnterUniformPoolCandidateEntry[] uniformCands;
            ActEnterWeightedPoolCandidateEntry[] weightedCands;
            Dictionary<int, Func<ActEnterResolveContext, double>> weightedBaselines;
            lock (SyncRoot)
            {
                forces = [.. ActEnterForces];
                poolModes = new(ActEnterPoolModes);
                uniformCands = [.. ActEnterUniformPoolCandidates];
                weightedCands = [.. ActEnterWeightedPoolCandidates];
                weightedBaselines = new(ActEnterWeightedBaselines);
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

                ReplaceActAtSlot(runManager, runState, enteringActIndex, f.ActType);
                return;
            }

            if (!poolModes.TryGetValue(enteringActIndex, out var mode))
                return;

            switch (mode)
            {
                case ActEnterPoolModeKind.Uniform:
                    ApplyUniformPool(runManager, runState, enteringActIndex, ctx, uniformCands);
                    break;
                case ActEnterPoolModeKind.Weighted:
                    ApplyWeightedPool(runManager, runState, enteringActIndex, ctx, weightedCands, weightedBaselines);
                    break;
                default:
                    throw new InvalidOperationException($"Unhandled {nameof(ActEnterPoolModeKind)}: {mode}.");
            }
        }

        private static void RequirePoolMode(int slotIndex, ActEnterPoolModeKind required, string what)
        {
            if (!ActEnterPoolModes.TryGetValue(slotIndex, out var mode) || mode != required)
                throw new InvalidOperationException(
                    $"Slot {slotIndex}: register {required} pool before adding {what}.");
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

        private static void ReplaceActAtSlot(RunManager runManager, RunState runState, int slotIndex,
            ActModel replacementMutable)
        {
            var beforeAct = runState.Acts[slotIndex];
            var list = runState.Acts.ToList();
            CopySharedAncientSubset(beforeAct, replacementMutable);
            list[slotIndex] = replacementMutable;
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

        private static void ApplyUniformPool(RunManager runManager, RunState runState, int slotIndex,
            ActEnterResolveContext ctx,
            ActEnterUniformPoolCandidateEntry[] uniformCands)
        {
            var baseline = runState.Acts[slotIndex];
            var set = new List<ActModel> { baseline };
            foreach (var c in uniformCands)
            {
                if (c.SlotIndex != slotIndex)
                    continue;

                if (!c.Eligibility(ctx))
                    continue;

                var added = ModelDb.GetById<ActModel>(ModelDb.GetId(c.CandidateActType)).ToMutable();
                if (set.Exists(a => a.Id == added.Id))
                    continue;

                set.Add(added);
            }

            if (set.Count <= 1)
                return;

            ReplaceActAtSlot(runManager, runState, slotIndex, set[ctx.Rng.NextInt(0, set.Count)]);
        }

        private static void ApplyWeightedPool(RunManager runManager, RunState runState, int slotIndex,
            ActEnterResolveContext ctx,
            ActEnterWeightedPoolCandidateEntry[] weightedCands,
            Dictionary<int, Func<ActEnterResolveContext, double>> weightedBaselines)
        {
            var weighted = new List<(ActModel Act, double W)>();
            if (weightedBaselines.TryGetValue(slotIndex, out var baselineWeightFn))
            {
                var w = baselineWeightFn(ctx);
                if (w > 0d)
                    weighted.Add((runState.Acts[slotIndex], w));
            }

            foreach (var c in weightedCands)
            {
                if (c.SlotIndex != slotIndex)
                    continue;

                if (!c.Eligibility(ctx))
                    continue;

                var ww = c.Weight(ctx);
                if (ww <= 0d)
                    continue;

                var act = ModelDb.GetById<ActModel>(ModelDb.GetId(c.CandidateActType)).ToMutable();
                if (weighted.Exists(t => t.Act.Id == act.Id))
                    continue;

                weighted.Add((act, ww));
            }

            var total = weighted.Sum(t => t.W);
            if (total <= 0d || weighted.Count == 0)
                return;

            var roll = ctx.Rng.NextDouble() * total;
            foreach (var (act, w) in weighted)
            {
                roll -= w;
                if (!(roll <= 0d)) continue;
                ReplaceActAtSlot(runManager, runState, slotIndex, act);
                return;
            }

            ReplaceActAtSlot(runManager, runState, slotIndex, weighted[^1].Act);
        }

        private readonly record struct ActEnterForceEntry(
            int SlotIndex,
            Type ActType,
            int Priority,
            int TieBreakOrder,
            Func<ActEnterResolveContext, bool> Eligibility);

        private readonly record struct ActEnterUniformPoolCandidateEntry(
            int SlotIndex,
            Type CandidateActType,
            Func<ActEnterResolveContext, bool> Eligibility);

        private readonly record struct ActEnterWeightedPoolCandidateEntry(
            int SlotIndex,
            Type CandidateActType,
            Func<ActEnterResolveContext, bool> Eligibility,
            Func<ActEnterResolveContext, double> Weight);
    }
}
