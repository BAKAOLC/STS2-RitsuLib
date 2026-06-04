using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;

namespace STS2RitsuLib.Networking.JoinDiagnostics
{
    internal static class JoinFailureDiagnosticsService
    {
        private static readonly Lock SyncRoot = new();
        private static DateTimeOffset? _joinAttemptStartedAt;
        private static InitialGameInfoMessage? _lastInitialMessage;
        private static JoinDiagnosticsPayload? _lastHostPayload;

        public static void BeginJoinAttempt()
        {
            lock (SyncRoot)
            {
                _joinAttemptStartedAt = DateTimeOffset.UtcNow;
                _lastInitialMessage = null;
                _lastHostPayload = null;
            }
        }

        public static void ClearJoinAttempt()
        {
            lock (SyncRoot)
            {
                _joinAttemptStartedAt = null;
                _lastInitialMessage = null;
                _lastHostPayload = null;
            }
        }

        public static void ObserveInitialGameInfo(InitialGameInfoMessage message)
        {
            lock (SyncRoot)
            {
                _lastInitialMessage = message;
            }
        }

        public static void ObserveHostPayload(JoinDiagnosticsPayload? payload)
        {
            lock (SyncRoot)
            {
                _lastHostPayload = payload;
            }
        }

        public static bool TryCreateReport(NetErrorInfo info, out JoinFailureDiagnosticReport report)
        {
            InitialGameInfoMessage? initialMessage;
            JoinDiagnosticsPayload? hostPayload;
            lock (SyncRoot)
            {
                var recentJoinAttempt = _joinAttemptStartedAt.HasValue
                                        && DateTimeOffset.UtcNow - _joinAttemptStartedAt.Value <
                                        TimeSpan.FromMinutes(3);
                if (!recentJoinAttempt)
                {
                    report = null!;
                    return false;
                }

                initialMessage = _lastInitialMessage;
                hostPayload = _lastHostPayload;
                _joinAttemptStartedAt = null;
            }

            report = BuildReport(info, initialMessage, hostPayload);
            return true;
        }

        private static JoinFailureDiagnosticReport BuildReport(
            NetErrorInfo info,
            InitialGameInfoMessage? initialMessage,
            JoinDiagnosticsPayload? hostPayload)
        {
            var local = JoinDiagnosticsPayloadCodec.CreateLocalSnapshot();
            var host = initialMessage.HasValue
                ? JoinDiagnosticsPayloadCodec.CreateHostSnapshot(initialMessage.Value, hostPayload)
                : null;
            var issues = new List<JoinFailureIssue>();
            var reason = info.GetReason();

            if (host == null)
            {
                issues.Add(new(
                    JoinFailureIssueKind.Transport,
                    T("issue.transport.title", "No game-info handshake"),
                    F("issue.transport.body",
                        "The connection failed before the host sent game-info data. Network reason: {0}.",
                        LocalizeReason(reason)),
                    []));
            }
            else if (initialMessage?.connectionFailureReason is { } hostReason)
            {
                issues.Add(new(
                    JoinFailureIssueKind.HostRejected,
                    T("issue.hostRejected.title", "Host rejected the join"),
                    F("issue.hostRejected.body", "The host explicitly rejected the join request: {0}.",
                        LocalizeConnectionReason(hostReason)),
                    []));
            }
            else
            {
                AddVersionIssue(issues, host, local);
                AddModIssues(issues, host, local);
                AddModelDbIssue(issues, host, local);
            }

            if (issues.Count == 0)
                issues.Add(new(
                    JoinFailureIssueKind.Network,
                    T("issue.network.title", "Network failure"),
                    F("issue.network.body", "The join failed with network reason: {0}.", LocalizeReason(reason)),
                    []));

            var summary = issues[0].Description;
            return new(
                T("panel.title", "Join Failure Details"),
                summary,
                issues,
                host,
                local,
                LocalizeReason(reason),
                info.GetErrorString().Trim());
        }

        private static void AddVersionIssue(
            ICollection<JoinFailureIssue> issues,
            JoinPeerSnapshot host,
            JoinPeerSnapshot local)
        {
            if (string.Equals(host.GameVersion, local.GameVersion, StringComparison.Ordinal))
                return;

            issues.Add(new(
                JoinFailureIssueKind.GameVersion,
                T("issue.version.title", "Game version mismatch"),
                T("issue.version.body", "The host and local game versions are different."),
                [new(T("row.gameVersion", "Game version"), host.GameVersion, local.GameVersion)]));
        }

        private static void AddModIssues(
            ICollection<JoinFailureIssue> issues,
            JoinPeerSnapshot host,
            JoinPeerSnapshot local)
        {
            var hostByKey = host.GameplayMods.ToDictionary(m => m.Key, StringComparer.Ordinal);
            var localByKey = local.GameplayMods.ToDictionary(m => m.Key, StringComparer.Ordinal);
            var missingOnLocal = host.GameplayMods.Where(m => !localByKey.ContainsKey(m.Key)).ToList();
            var missingOnHost = local.GameplayMods.Where(m => !hostByKey.ContainsKey(m.Key)).ToList();
            var versionRows = ExtractVersionRows(missingOnLocal, missingOnHost);

            if (missingOnLocal.Count > 0 || missingOnHost.Count > 0 || versionRows.Count > 0)
            {
                var rows = new List<JoinFailureDetailRow>();
                rows.AddRange(versionRows);
                rows.AddRange(missingOnLocal.Select(m => new JoinFailureDetailRow(
                    FormatModLabel(m),
                    FormatModValue(m),
                    T("value.missing", "Missing"))));
                rows.AddRange(missingOnHost.Select(m => new JoinFailureDetailRow(
                    FormatModLabel(m),
                    T("value.missing", "Missing"),
                    FormatModValue(m))));

                issues.Add(new(
                    JoinFailureIssueKind.ModSet,
                    T("issue.modSet.title", "Gameplay mod mismatch"),
                    T("issue.modSet.body", "The host and local gameplay mod lists are not the same."),
                    LimitRows(rows)));
                return;
            }

            if (host.GameplayMods.Count != local.GameplayMods.Count ||
                host.GameplayMods.Select(m => m.Key).SequenceEqual(local.GameplayMods.Select(m => m.Key),
                    StringComparer.Ordinal))
                return;

            var firstDifferentIndex = -1;
            var differentPositions = 0;
            for (var i = 0; i < host.GameplayMods.Count; i++)
            {
                if (string.Equals(host.GameplayMods[i].Key, local.GameplayMods[i].Key, StringComparison.Ordinal))
                    continue;

                if (firstDifferentIndex < 0)
                    firstDifferentIndex = i;
                differentPositions++;
            }

            issues.Add(new(
                JoinFailureIssueKind.ModOrder,
                T("issue.modOrder.title", "Gameplay mod order differs"),
                T("issue.modOrder.body",
                    "Both sides have the same gameplay mods, but their load order is different. This can change ModelDb registration order."),
                [
                    new(T("row.orderMismatchCount", "Different positions"), differentPositions.ToString(),
                        differentPositions.ToString()),
                    new(F("row.firstDifferentPosition", "First different position: {0}", firstDifferentIndex + 1),
                        FormatModValue(host.GameplayMods[firstDifferentIndex]),
                        FormatModValue(local.GameplayMods[firstDifferentIndex])),
                ]));
        }

        private static void AddModelDbIssue(
            ICollection<JoinFailureIssue> issues,
            JoinPeerSnapshot host,
            JoinPeerSnapshot local)
        {
            if (host.ModelDbHash == local.ModelDbHash)
                return;

            issues.Add(new(
                JoinFailureIssueKind.ModelDb,
                T("issue.modelDb.title", "ModelDb hash mismatch"),
                T("issue.modelDb.body",
                    "The gameplay model database is different between host and local. If mod lists match, check mod load order or runtime model registration."),
                [
                    new(T("row.modelDbHash", "ModelDb hash"), host.ModelDbHash.ToString(),
                        local.ModelDbHash.ToString()),
                ]));
        }

        private static List<JoinFailureDetailRow> ExtractVersionRows(
            List<JoinDiagnosticsModEntry> missingOnLocal,
            List<JoinDiagnosticsModEntry> missingOnHost)
        {
            var rows = new List<JoinFailureDetailRow>();
            for (var i = missingOnLocal.Count - 1; i >= 0; i--)
            {
                var hostMod = missingOnLocal[i];
                var localIndex = missingOnHost.FindIndex(m => !string.IsNullOrEmpty(m.Id) &&
                                                              string.Equals(m.Id, hostMod.Id,
                                                                  StringComparison.Ordinal));
                if (localIndex < 0)
                    continue;

                var localMod = missingOnHost[localIndex];
                rows.Add(new(
                    FormatModLabel(hostMod),
                    FormatModValue(hostMod),
                    FormatModValue(localMod)));
                missingOnLocal.RemoveAt(i);
                missingOnHost.RemoveAt(localIndex);
            }

            return rows;
        }

        private static IReadOnlyList<JoinFailureDetailRow> LimitRows(IReadOnlyList<JoinFailureDetailRow> rows)
        {
            const int maxRows = 24;
            if (rows.Count <= maxRows)
                return rows;

            var limited = rows.Take(maxRows).ToList();
            limited.Add(new(
                F("row.more", "{0} more", rows.Count - maxRows),
                T("value.omitted", "Omitted"),
                T("value.omitted", "Omitted")));
            return limited;
        }

        private static string FormatModLabel(JoinDiagnosticsModEntry mod)
        {
            if (!string.IsNullOrWhiteSpace(mod.Name) &&
                !string.Equals(mod.Name, mod.Id, StringComparison.Ordinal))
                return mod.Name + " (" + mod.Id + ")";

            return string.IsNullOrWhiteSpace(mod.Id) ? mod.Key : mod.Id;
        }

        private static string FormatModValue(JoinDiagnosticsModEntry mod)
        {
            var version = string.IsNullOrWhiteSpace(mod.Version)
                ? T("value.noVersion", "No version")
                : mod.Version;
            return "#" + (mod.Index + 1) + "  " + mod.Key + "  " + version;
        }

        private static string LocalizeReason(NetError reason)
        {
            return T("reason." + reason, reason.ToString());
        }

        private static string LocalizeConnectionReason(ConnectionFailureReason reason)
        {
            return T("connection." + reason, reason.ToString());
        }

        private static string T(string key, string fallback)
        {
            return JoinFailureDiagnosticsLocalization.Get(key, fallback);
        }

        private static string F(string key, string fallback, params object?[] args)
        {
            return JoinFailureDiagnosticsLocalization.Format(key, fallback, args);
        }
    }
}
