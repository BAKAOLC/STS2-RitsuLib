using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.ActSequence
{
    /// <summary>
    ///     When an act-sequence rule is evaluated.
    /// </summary>
    public enum ActSequenceTrigger
    {
        /// <summary>
        ///     New run setup, before the first <see cref="RunManager.GenerateRooms" /> call.
        /// </summary>
        OnRunSetupBeforeGenerateRooms = 0,

        /// <summary>
        ///     When the run attempts to advance to the next act (<see cref="RunManager.EnterNextAct" />).
        /// </summary>
        OnEnterNextAct = 1,
    }

    /// <summary>
    ///     How a rule mutates <see cref="RunState.Acts" />.
    /// </summary>
    public enum ActSequenceOperationKind
    {
        /// <summary>
        ///     Inserts an act at a given index.
        /// </summary>
        InsertAt = 0,

        /// <summary>
        ///     Appends an act at the end of the act list.
        /// </summary>
        Append = 1,
    }

    /// <summary>
    ///     Runtime context for act-sequence evaluation.
    /// </summary>
    public readonly record struct ActSequenceResolveContext(
        RunManager RunManager,
        RunState RunState,
        ActSequenceTrigger Trigger,
        int CurrentActIndex,
        bool IsMultiplayer
    );

    /// <summary>
    ///     A single act-sequence mutation rule registered by a mod.
    /// </summary>
    public sealed record ActSequenceRule(
        string OwnerModId,
        string RuleId,
        ActSequenceTrigger Trigger,
        ActSequenceOperationKind Operation,
        Type ActType,
        int InsertIndex,
        int Priority,
        Func<ActSequenceResolveContext, bool> Eligibility
    )
    {
        /// <summary>
        ///     Creates an insert-at rule.
        /// </summary>
        public static ActSequenceRule InsertAt(
            string ownerModId,
            string ruleId,
            ActSequenceTrigger trigger,
            int index,
            Type actType,
            int priority,
            Func<ActSequenceResolveContext, bool> eligibility
        )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ownerModId);
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
            ArgumentNullException.ThrowIfNull(actType);
            ArgumentNullException.ThrowIfNull(eligibility);

            return new(
                ownerModId,
                ruleId,
                trigger,
                ActSequenceOperationKind.InsertAt,
                actType,
                index,
                priority,
                eligibility
            );
        }

        /// <summary>
        ///     Creates an append rule.
        /// </summary>
        public static ActSequenceRule Append(
            string ownerModId,
            string ruleId,
            ActSequenceTrigger trigger,
            Type actType,
            int priority,
            Func<ActSequenceResolveContext, bool> eligibility
        )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ownerModId);
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
            ArgumentNullException.ThrowIfNull(actType);
            ArgumentNullException.ThrowIfNull(eligibility);

            return new(
                ownerModId,
                ruleId,
                trigger,
                ActSequenceOperationKind.Append,
                actType,
                -1,
                priority,
                eligibility
            );
        }
    }
}
