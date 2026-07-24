using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Networking
{
    internal sealed record ProgressDiagnosticsSnapshot(
        int SchemaVersion,
        int NumberOfRuns,
        IReadOnlyList<ProgressEpochEntry> Epochs)
    {
        public static ProgressDiagnosticsSnapshot CreateLocal()
        {
            var progress = SaveManager.Instance.Progress.ToSerializable();
            return new(
                progress.SchemaVersion,
                progress.NumberOfRuns,
                [
                    .. progress.Epochs
                        .OrderBy(epoch => epoch.Id, StringComparer.Ordinal)
                        .Select(epoch => new ProgressEpochEntry(epoch.Id, epoch.State.ToString(), epoch.ObtainDate)),
                ]);
        }
    }

    internal sealed record ProgressEpochEntry(string Id, string State, long ObtainDate);
}
