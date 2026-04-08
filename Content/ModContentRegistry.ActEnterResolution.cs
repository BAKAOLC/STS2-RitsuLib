using System.Reflection;
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

        /// <summary>
        ///     True when any act-enter force or pool registration exists (cheap check before <see cref="RunManager" /> work).
        /// </summary>
        public static bool HasAnyActEnterRegistration => Volatile.Read(ref _actEnterRegistrationCount) > 0;

        /// <summary>
        ///     When true, <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapScreen.SetMap" /> postfix should call
        ///     <see cref="MapSelectionSynchronizer.BeforeMapGenerated" /> once so multiplayer map votes match the layout after
        ///     act-enter replacement (same idea as custom-act transitions in community mods).
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
        ///     When <paramref name="eligibility" /> is true, forces <see cref="MegaCrit.Sts2.Core.Runs.RunState.Acts" />[
        ///     <paramref name="slotIndex" />] to <typeparamref name="TAct" /> on <see cref="RunManager.EnterAct" />. Higher
        ///     <paramref name="priority" /> wins; ties break by earlier registration.
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
        ///     Declares a uniform pool for <paramref name="slotIndex" /> (required before uniform candidates). Baseline is the
        ///     act already in that slot when entering; eligible <see cref="RegisterActEnterUniformPoolCandidate{TAct}" /> rows
        ///     are unioned and deduped by id, then one act is drawn uniformly.
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
        ///     Adds a uniform-pool candidate for <paramref name="slotIndex" /> when <paramref name="eligibility" /> is true.
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
        ///     Declares a weighted pool for <paramref name="slotIndex" />. Use
        ///     <see cref="RegisterActEnterWeightedPoolCandidate{TAct}" /> and optionally
        ///     <see cref="RegisterActEnterWeightedPoolBaseline" /> so the act already in the slot participates with a weight.
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
        ///     Adds a weighted-pool candidate; weight must be &gt; 0 when eligible for the row to participate.
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
        ///     Gives the act currently in <paramref name="slotIndex" /> a weight in the weighted pool (explicit; no implicit
        ///     baseline in weighted mode).
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
            var beforeId = runState.Acts[slotIndex].Id;
            var list = runState.Acts.ToList();
            var replacement = ModelDb.GetById<ActModel>(ModelDb.GetId(actType)).ToMutable();
            list[slotIndex] = replacement;
            RunStateActsSetter(runState, list);
            InitializeRoomsForReplacedActIfNeeded(runManager, runState, slotIndex, beforeId);
        }

        private static void ReplaceActAtSlot(RunManager runManager, RunState runState, int slotIndex,
            ActModel replacementMutable)
        {
            var beforeId = runState.Acts[slotIndex].Id;
            var list = runState.Acts.ToList();
            list[slotIndex] = replacementMutable;
            RunStateActsSetter(runState, list);
            InitializeRoomsForReplacedActIfNeeded(runManager, runState, slotIndex, beforeId);
        }

        private static void InitializeRoomsForReplacedActIfNeeded(RunManager runManager, RunState runState,
            int actIndex,
            ModelId actIdBeforeReplace)
        {
            var act = runState.Acts[actIndex];
            if (act.Id == actIdBeforeReplace)
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
