using MegaCrit.Sts2.Core.Entities.Multiplayer;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Networking.StateDivergence
{
    internal sealed record StateDivergenceDiagnosticReport(
        string Title,
        string Summary,
        string Role,
        ulong RemotePeerId,
        StateDivergenceChecksumInfo LocalChecksum,
        StateDivergenceChecksumInfo RemoteChecksum,
        IReadOnlyList<StateDivergenceDiagnosticSection> Sections,
        IReadOnlyList<StateDivergenceDiagnosticSection> ExportSections,
        IReadOnlyList<ContentModInventoryEntry> LocalContentMods,
        IReadOnlyList<ContentModInventoryEntry> RemoteContentMods,
        bool HasRemoteContentModInventory,
        ProgressDiagnosticsSnapshot? LocalProgress,
        ProgressDiagnosticsSnapshot? RemoteProgress,
        string LocalStateDump,
        string RemoteStateDump);

    internal sealed record StateDivergenceChecksumInfo(
        uint Id,
        uint Checksum,
        string Context,
        bool? ModelDbHashUsesDeterministicCache,
        bool? SavedPropertyNetIdUsesDeterministicSort);

    internal sealed record StateDivergenceDiagnosticSection(
        string Title,
        string Description,
        bool StartsCollapsed,
        IReadOnlyList<StateDivergenceDiagnosticRow> Rows);

    internal enum StateDivergenceDiagnosticRowKind
    {
        Normal,
        ModelList,
    }

    internal sealed record StateDivergenceDiagnosticRow(
        string Path,
        string LocalValue,
        string RemoteValue,
        string Detail = "",
        StateDivergenceDiagnosticRowKind Kind = StateDivergenceDiagnosticRowKind.Normal,
        IReadOnlyList<StateDivergenceDiagnosticModelListItem>? ModelItems = null);

    internal sealed record StateDivergenceDiagnosticModelListItem(
        string Index,
        string LocalSummary,
        string RemoteSummary,
        IReadOnlyList<StateDivergenceDiagnosticFieldDifference> Differences);

    internal sealed record StateDivergenceDiagnosticFieldDifference(
        string Path,
        string LocalValue,
        string RemoteValue);

    internal readonly record struct StateDivergenceTrackedState(
        NetChecksumData Checksum,
        string Context,
        NetFullCombatState FullState);
}
