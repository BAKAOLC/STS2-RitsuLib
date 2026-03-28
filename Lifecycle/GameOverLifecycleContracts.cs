using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Game over UI was created for a finished run.
    /// </summary>
    /// <param name="RunState">Run state presented on the screen.</param>
    /// <param name="SerializableRun">Serialized run payload.</param>
    /// <param name="Screen">Game over screen node.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct GameOverScreenCreatedEvent(
        RunState RunState,
        SerializableRun SerializableRun,
        NGameOverScreen Screen,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
