namespace STS2RitsuLib.Networking.JoinDiagnostics
{
    internal sealed record JoinFailureDiagnosticReport(
        string Title,
        string Summary,
        IReadOnlyList<JoinFailureIssue> Issues,
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
