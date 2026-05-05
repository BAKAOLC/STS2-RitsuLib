using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.ActSequence
{
    /// <summary>
    ///     Per-mod registry for act-sequence mutation rules (insert/append acts).
    /// </summary>
    public sealed class ModActSequenceRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModActSequenceRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static bool _frozen;
        private static int _registrationCount;
        private static int _tieBreakSeq;

        private static readonly Action<RunState, IReadOnlyList<ActModel>> RunStateActsSetter =
            CreateRunStateActsSetter();

        private readonly Logger _logger;

        private readonly string _modId;
        private readonly List<RegisteredRule> _rules = [];
        private string? _freezeReason;

        private ModActSequenceRegistry(string modId)
        {
            _modId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
        }

        /// <summary>
        ///     True when at least one rule has been registered across all mods.
        /// </summary>
        public static bool HasAnyRegistration => Volatile.Read(ref _registrationCount) > 0;

        /// <summary>
        ///     Returns the singleton registry for <paramref name="modId" /> (created on first use).
        /// </summary>
        public static ModActSequenceRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var existing))
                    return existing;

                var created = new ModActSequenceRegistry(modId);
                Registries[modId] = created;
                return created;
            }
        }

        /// <summary>
        ///     Registers an insert-at rule.
        /// </summary>
        public void RegisterInsertAt<TAct>(
            string ruleId,
            ActSequenceTrigger trigger,
            int index,
            int priority,
            Func<ActSequenceResolveContext, bool> eligibility
        )
            where TAct : ActModel
        {
            RegisterRule(ActSequenceRule.InsertAt(_modId, ruleId, trigger, index, typeof(TAct), priority, eligibility));
        }

        /// <summary>
        ///     Registers an append rule.
        /// </summary>
        public void RegisterAppend<TAct>(
            string ruleId,
            ActSequenceTrigger trigger,
            int priority,
            Func<ActSequenceResolveContext, bool> eligibility
        )
            where TAct : ActModel
        {
            RegisterRule(ActSequenceRule.Append(_modId, ruleId, trigger, typeof(TAct), priority, eligibility));
        }

        /// <summary>
        ///     Registers <paramref name="rule" />. Throws if registration is frozen.
        /// </summary>
        public void RegisterRule(ActSequenceRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnsureMutable($"register act-sequence rule '{rule.RuleId}'");

            if (!typeof(ActModel).IsAssignableFrom(rule.ActType) || rule.ActType.IsAbstract)
                throw new ArgumentException($"Act type '{rule.ActType.Name}' must be a concrete {nameof(ActModel)}.",
                    nameof(rule));

            lock (SyncRoot)
            {
                var tie = ++_tieBreakSeq;
                _rules.Add(new(rule, tie));
                Interlocked.Increment(ref _registrationCount);
            }

            _logger.Info(
                $"[ActSequence] Registered rule '{rule.RuleId}' ({rule.Trigger}, {rule.Operation}) -> {rule.ActType.Name}");
        }

        internal static void FreezeRegistrations(string reason)
        {
            lock (SyncRoot)
            {
                if (_frozen)
                    return;

                _frozen = true;
                foreach (var registry in Registries.Values)
                    registry._freezeReason = reason;
            }

            foreach (var registry in Registries.Values)
                registry._logger.Info($"[ActSequence] Registration is now frozen ({reason}).");
        }

        internal static bool TryApplyRules(
            RunManager runManager,
            RunState runState,
            ActSequenceTrigger trigger,
            int currentActIndex,
            bool isMultiplayer
        )
        {
            if (!HasAnyRegistration)
                return false;

            RegisteredRule[] rules;
            lock (SyncRoot)
            {
                if (Registries.Count == 0)
                    return false;

                rules = Registries.Values.SelectMany(r => r._rules).ToArray();
            }

            if (rules.Length == 0)
                return false;

            Array.Sort(rules, static (a, b) =>
            {
                var p = b.Rule.Priority.CompareTo(a.Rule.Priority);
                return p != 0 ? p : a.TieBreakOrder.CompareTo(b.TieBreakOrder);
            });

            var ctx = new ActSequenceResolveContext(runManager, runState, trigger, currentActIndex, isMultiplayer);
            var acts = runState.Acts.ToList();
            var mutated = false;
            var addedActs = new List<ActModel>();

            foreach (var entry in rules)
            {
                var rule = entry.Rule;
                if (rule.Trigger != trigger)
                    continue;

                if (!rule.Eligibility(ctx))
                    continue;

                // Idempotency: skip if the act already exists in the sequence by id.
                var targetId = ModelDb.GetId(rule.ActType);
                if (acts.Any(a => a.Id == targetId))
                    continue;

                var mutable = ModelDb.GetById<ActModel>(targetId).ToMutable();
                switch (rule.Operation)
                {
                    case ActSequenceOperationKind.Append:
                        acts.Add(mutable);
                        addedActs.Add(mutable);
                        mutated = true;
                        break;
                    case ActSequenceOperationKind.InsertAt:
                        var idx = rule.InsertIndex;
                        if (idx < 0)
                            idx = 0;
                        if (idx > acts.Count)
                            idx = acts.Count;
                        acts.Insert(idx, mutable);
                        addedActs.Add(mutable);
                        mutated = true;
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Unhandled {nameof(ActSequenceOperationKind)}: {rule.Operation}");
                }
            }

            if (!mutated)
                return false;

            // Write back the list and initialize rooms for newly added acts (GenerateRooms is idempotent on mutable clones
            // in the sense that it initializes containers; callers should ensure this runs before map generation).
            SetRunStateActs(runState, acts);

            foreach (var act in addedActs) act.GenerateRooms(runState.Rng.UpFront, runState.UnlockState, isMultiplayer);

            return true;
        }

        private static Action<RunState, IReadOnlyList<ActModel>> CreateRunStateActsSetter()
        {
            var prop = typeof(RunState).GetProperty(nameof(RunState.Acts));
            var set = prop?.GetSetMethod(true) ??
                      throw new InvalidOperationException("RunState.Acts setter not found.");
            return (rs, list) => set.Invoke(rs, [list]);
        }

        private static void SetRunStateActs(RunState runState, IReadOnlyList<ActModel> acts)
        {
            RunStateActsSetter(runState, acts);
        }

        private void EnsureMutable(string operation)
        {
            if (!_frozen)
                return;

            var reason = _freezeReason ?? "unknown";
            throw new InvalidOperationException(
                $"Cannot {operation}: act-sequence registration is frozen ({reason}).");
        }

        private readonly record struct RegisteredRule(ActSequenceRule Rule, int TieBreakOrder);
    }
}
