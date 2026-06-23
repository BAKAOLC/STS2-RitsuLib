namespace STS2RitsuLib.Networking.JoinDiagnostics
{
    internal sealed record JoinFailureDiagnosticReport(
        string Title,
        string Summary,
        IReadOnlyList<JoinFailureIssue> Issues,
        IReadOnlyList<JoinFailureSuggestedSolution> SuggestedSolutions,
        JoinPeerSnapshot? Host,
        JoinPeerSnapshot Local,
        string NetworkReason,
        string NetworkInfo);

    internal sealed record JoinFailureIssue(
        JoinFailureIssueKind Kind,
        string Title,
        string Description,
        IReadOnlyList<JoinFailureDetailRow> Rows);

    internal sealed record JoinFailureDetailRow(string Label, string HostValue, string LocalValue);

    internal sealed record JoinFailureSuggestedSolution(
        string Title,
        string Description,
        string ButtonText,
        JoinFailureSuggestedSolutionAction Action,
        string? ModId = null,
        string? PageId = null,
        string? SectionId = null,
        string? EntryId = null,
        ulong? WorkshopItemId = null);

    internal enum JoinFailureSuggestedSolutionAction
    {
        OpenSettings,
        SubscribeWorkshopItem,
    }

    internal enum JoinFailureIssueKind
    {
        Transport,
        HostRejected,
        GameVersion,
        ModSet,
        ModOrder,
        ModelDbHashMode,
        ModelDb,
        Network,
    }
}
