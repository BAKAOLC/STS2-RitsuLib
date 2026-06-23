using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using STS2RitsuLib.Compat;

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
                var modelDbMismatch = host.ModelDbHash != local.ModelDbHash;
                var modelDbHashModeMismatch =
                    host.ModelDbHashUsesDeterministicCache != local.ModelDbHashUsesDeterministicCache;
                var deterministicModelDbHash =
                    host.ModelDbHashUsesDeterministicCache && local.ModelDbHashUsesDeterministicCache;
                AddModIssues(issues, host, local, modelDbMismatch && deterministicModelDbHash,
                    deterministicModelDbHash);
                AddVersionIssue(issues, host, local);
                if (!modelDbMismatch && modelDbHashModeMismatch)
                    AddModelDbHashModeIssue(issues, host, local);
                AddModelDbIssue(issues, host, local, deterministicModelDbHash, modelDbHashModeMismatch);
            }

            if (issues.Count == 0)
                issues.Add(new(
                    JoinFailureIssueKind.Network,
                    T("issue.network.title", "Network failure"),
                    F("issue.network.body", "The join failed with network reason: {0}.", LocalizeReason(reason)),
                    []));

            var summary = issues[0].Description;
            var suggestedSolutions = BuildSuggestedSolutions(host, local, issues, hostPayload);
            return new(
                T("panel.title", "Join Failure Details"),
                summary,
                issues,
                suggestedSolutions,
                host,
                local,
                LocalizeReason(reason),
                info.GetErrorString().Trim());
        }

        private static void AddModelDbHashModeIssue(
            ICollection<JoinFailureIssue> issues,
            JoinPeerSnapshot host,
            JoinPeerSnapshot local)
        {
            if (host.ModelDbHashUsesDeterministicCache == local.ModelDbHashUsesDeterministicCache)
                return;

            issues.Add(new(
                JoinFailureIssueKind.ModelDbHashMode,
                T("issue.modelDbHashMode.title", "ModelDb hashing mode differs"),
                T("issue.modelDbHashMode.body",
                    "One side reports stable ModelDb sorting, but the other side does not. The other side may have this setting disabled, may be missing the capability, or may be using an older RitsuLib version."),
                [
                    new(T("row.modelDbHashMode", "ModelDb hash mode"),
                        FormatModelDbHashMode(host.ModelDbHashUsesDeterministicCache),
                        FormatModelDbHashMode(local.ModelDbHashUsesDeterministicCache)),
                    new(T("row.modelDbHash", "ModelDb hash"), host.ModelDbHash.ToString(),
                        local.ModelDbHash.ToString()),
                    new(T("row.modelDbHashModeDetail", "ModelDb hash mode detail"),
                        FormatModelDbHashModeDetail(host),
                        FormatModelDbHashModeDetail(local)),
                ]));
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
            JoinPeerSnapshot local,
            bool suppressOrderIssueForModelDbMismatch,
            bool deterministicModelDbHash)
        {
            if (host.HasProcessedContentMods &&
                local.HasProcessedContentMods &&
                (host.ContentMods.Count > 0 || local.ContentMods.Count > 0))
            {
                AddContentModIssues(
                    issues,
                    host.ContentMods,
                    local.ContentMods,
                    suppressOrderIssueForModelDbMismatch,
                    deterministicModelDbHash);
                return;
            }

            AddGameplayModIssues(
                issues,
                host.GameplayMods,
                local.GameplayMods,
                suppressOrderIssueForModelDbMismatch,
                deterministicModelDbHash);
        }

        private static void AddGameplayModIssues(
            ICollection<JoinFailureIssue> issues,
            IReadOnlyList<JoinDiagnosticsModEntry> hostMods,
            IReadOnlyList<JoinDiagnosticsModEntry> localMods,
            bool suppressOrderIssueForModelDbMismatch,
            bool deterministicModelDbHash)
        {
            var hostByKey = hostMods.ToDictionary(m => m.Key, StringComparer.Ordinal);
            var localByKey = localMods.ToDictionary(m => m.Key, StringComparer.Ordinal);
            var missingOnLocal = hostMods.Where(m => !localByKey.ContainsKey(m.Key)).ToList();
            var missingOnHost = localMods.Where(m => !hostByKey.ContainsKey(m.Key)).ToList();
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

            if (hostMods.Count != localMods.Count ||
                hostMods.Select(m => m.Key).SequenceEqual(localMods.Select(m => m.Key),
                    StringComparer.Ordinal))
                return;

            if (suppressOrderIssueForModelDbMismatch)
                return;

            var firstDifferentIndex = -1;
            var differentPositions = 0;
            for (var i = 0; i < hostMods.Count; i++)
            {
                if (string.Equals(hostMods[i].Key, localMods[i].Key, StringComparison.Ordinal))
                    continue;

                if (firstDifferentIndex < 0)
                    firstDifferentIndex = i;
                differentPositions++;
            }

            issues.Add(new(
                JoinFailureIssueKind.ModOrder,
                T("issue.modOrder.title", "Gameplay mod order differs"),
                GetModOrderBody(deterministicModelDbHash),
                [
                    new(T("row.orderMismatchCount", "Different positions"), differentPositions.ToString(),
                        differentPositions.ToString()),
                    new(F("row.firstDifferentPosition", "First different position: {0}", firstDifferentIndex + 1),
                        FormatModValue(hostMods[firstDifferentIndex]),
                        FormatModValue(localMods[firstDifferentIndex])),
                ]));
        }

        private static void AddContentModIssues(
            ICollection<JoinFailureIssue> issues,
            IReadOnlyList<ContentModInventoryEntry> hostMods,
            IReadOnlyList<ContentModInventoryEntry> localMods,
            bool suppressOrderIssueForModelDbMismatch,
            bool deterministicModelDbHash)
        {
            var hostByKey = BuildContentModIdentityMap(hostMods);
            var localByKey = BuildContentModIdentityMap(localMods);
            var missingOnLocal = hostMods.Where(m => !localByKey.ContainsKey(ContentModIdentityKey(m))).ToList();
            var missingOnHost = localMods.Where(m => !hostByKey.ContainsKey(ContentModIdentityKey(m))).ToList();
            var versionRows = ExtractContentVersionRows(missingOnLocal, missingOnHost);

            if (missingOnLocal.Count > 0 || missingOnHost.Count > 0 || versionRows.Count > 0)
            {
                var rows = new List<JoinFailureDetailRow>();
                rows.AddRange(versionRows);
                rows.AddRange(missingOnLocal.Select(m => new JoinFailureDetailRow(
                    FormatContentModLabel(m),
                    FormatContentModValue(m),
                    T("value.missing", "Missing"))));
                rows.AddRange(missingOnHost.Select(m => new JoinFailureDetailRow(
                    FormatContentModLabel(m),
                    T("value.missing", "Missing"),
                    FormatContentModValue(m))));

                issues.Add(new(
                    JoinFailureIssueKind.ModSet,
                    T("issue.modSet.title", "Gameplay mod mismatch"),
                    T("issue.modSet.body", "The host and local gameplay mod lists are not the same."),
                    LimitRows(rows)));
                return;
            }

            if (hostMods.Count != localMods.Count ||
                hostMods.Select(ContentModIdentityKey)
                    .SequenceEqual(localMods.Select(ContentModIdentityKey), StringComparer.Ordinal))
                return;

            if (suppressOrderIssueForModelDbMismatch)
                return;

            var firstDifferentIndex = -1;
            var differentPositions = 0;
            for (var i = 0; i < hostMods.Count; i++)
            {
                if (string.Equals(
                        ContentModIdentityKey(hostMods[i]),
                        ContentModIdentityKey(localMods[i]),
                        StringComparison.Ordinal))
                    continue;

                if (firstDifferentIndex < 0)
                    firstDifferentIndex = i;
                differentPositions++;
            }

            issues.Add(new(
                JoinFailureIssueKind.ModOrder,
                T("issue.modOrder.title", "Gameplay mod order differs"),
                GetModOrderBody(deterministicModelDbHash),
                [
                    new(T("row.orderMismatchCount", "Different positions"), differentPositions.ToString(),
                        differentPositions.ToString()),
                    new(F("row.firstDifferentPosition", "First different position: {0}", firstDifferentIndex + 1),
                        FormatContentModValue(hostMods[firstDifferentIndex]),
                        FormatContentModValue(localMods[firstDifferentIndex])),
                ]));
        }

        private static void AddModelDbIssue(
            ICollection<JoinFailureIssue> issues,
            JoinPeerSnapshot host,
            JoinPeerSnapshot local,
            bool deterministicModelDbHash,
            bool modelDbHashModeMismatch)
        {
            if (host.ModelDbHash == local.ModelDbHash)
                return;

            issues.Add(new(
                JoinFailureIssueKind.ModelDb,
                T("issue.modelDb.title", "ModelDb hash mismatch"),
                GetModelDbMismatchBody(deterministicModelDbHash, modelDbHashModeMismatch),
                [
                    new(T("row.modelDbHash", "ModelDb hash"), host.ModelDbHash.ToString(),
                        local.ModelDbHash.ToString()),
                    new(T("row.modelDbHashMode", "ModelDb hash mode"),
                        FormatModelDbHashMode(host.ModelDbHashUsesDeterministicCache),
                        FormatModelDbHashMode(local.ModelDbHashUsesDeterministicCache)),
                    new(T("row.modelDbHashModeDetail", "ModelDb hash mode detail"),
                        FormatModelDbHashModeDetail(host),
                        FormatModelDbHashModeDetail(local)),
                ]));
        }

        private static string GetModOrderBody(bool deterministicModelDbHash)
        {
            return deterministicModelDbHash
                ? T("issue.modOrder.body.deterministic",
                    "Both sides have the same gameplay mods, but their load order is different. ModelDb hash is based on deterministic final content on both sides, so order alone should not change it.")
                : T("issue.modOrder.body.existing",
                    "Both sides have the same gameplay mods, but their load order is different. Deterministic final-content hashing is not active on at least one side, so load order can still affect diagnostics through the existing initialization path.");
        }

        private static string GetModelDbMismatchBody(bool deterministicModelDbHash, bool modelDbHashModeMismatch)
        {
            if (modelDbHashModeMismatch)
                return T("issue.modelDb.body.modeMismatch",
                    "The gameplay model database is different between host and local, and only one side reports stable ModelDb sorting. Enable the same ModelDb deterministic sorting setting and use a RitsuLib version that supports reporting this mode on both sides.");

            return deterministicModelDbHash
                ? T("issue.modelDb.body.deterministic",
                    "The gameplay model database is different between host and local even though both sides use deterministic ModelDb hashing. If mod lists match, check runtime model registration, direct ModelDb patches, or different local mod files.")
                : T("issue.modelDb.body.existing",
                    "The gameplay model database is different between host and local. Deterministic final-content hashing is not active on at least one side, so also compare mod load order and any library, patch, or runtime registration differences.");
        }

        private static string FormatModelDbHashMode(bool deterministic)
        {
            return deterministic
                ? T("value.modelDbHashMode.deterministic", "Stable sorting")
                : T("value.modelDbHashMode.notReported", "Stable sorting not reported");
        }

        private static string FormatModelDbHashModeDetail(JoinPeerSnapshot snapshot)
        {
            return string.IsNullOrWhiteSpace(snapshot.ModelDbHashModeDetail)
                ? FormatModelDbHashMode(snapshot.ModelDbHashUsesDeterministicCache)
                : snapshot.ModelDbHashModeDetail.Trim();
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

        private static List<JoinFailureDetailRow> ExtractContentVersionRows(
            List<ContentModInventoryEntry> missingOnLocal,
            List<ContentModInventoryEntry> missingOnHost)
        {
            var rows = new List<JoinFailureDetailRow>();
            for (var i = missingOnLocal.Count - 1; i >= 0; i--)
            {
                var hostMod = missingOnLocal[i];
                var localIndex = missingOnHost.FindIndex(m =>
                    !string.IsNullOrEmpty(m.Id) &&
                    string.Equals(m.Id, hostMod.Id, StringComparison.Ordinal) &&
                    string.Equals(m.Id, hostMod.Id, StringComparison.Ordinal));
                if (localIndex < 0)
                    continue;

                var localMod = missingOnHost[localIndex];
                rows.Add(new(
                    FormatContentModLabel(hostMod),
                    FormatContentModValue(hostMod),
                    FormatContentModValue(localMod)));
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
            var id = string.IsNullOrWhiteSpace(mod.Id) ? mod.Key : mod.Id;
            return "#" + (mod.Index + 1) + "  " + id + "  version=" + version;
        }

        private static string FormatContentModLabel(ContentModInventoryEntry mod)
        {
            if (!string.IsNullOrWhiteSpace(mod.Name) &&
                !string.Equals(mod.Name, mod.Id, StringComparison.Ordinal))
                return mod.Name + " (" + mod.Id + ")";

            return string.IsNullOrWhiteSpace(mod.Id) ? ContentModIdentityKey(mod) : mod.Id;
        }

        private static string FormatContentModValue(ContentModInventoryEntry mod)
        {
            var version = string.IsNullOrWhiteSpace(mod.Version)
                ? T("value.noVersion", "No version")
                : mod.Version;
            var source = string.IsNullOrWhiteSpace(mod.Source) ? "" : "  source=" + mod.Source;
            return "#" + (mod.Index + 1) + "  " + mod.Id + "  version=" + version + source;
        }

        private static IReadOnlyDictionary<string, ContentModInventoryEntry> BuildContentModIdentityMap(
            IEnumerable<ContentModInventoryEntry> mods)
        {
            return mods
                .GroupBy(ContentModIdentityKey, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        }

        private static string ContentModIdentityKey(ContentModInventoryEntry mod)
        {
            return mod.Id + "\0" + mod.Version;
        }

        private static IReadOnlyList<JoinFailureSuggestedSolution> BuildSuggestedSolutions(
            JoinPeerSnapshot? host,
            JoinPeerSnapshot local,
            IReadOnlyList<JoinFailureIssue> issues,
            JoinDiagnosticsPayload? hostPayload)
        {
            if (!IsOrderOnlyModelDbMismatch(host, local, issues, hostPayload))
                return [];

            return
            [
                new(
                    T("solution.sortMods.title", "Sort mod load order"),
                    T("solution.sortMods.description",
                        "Open content mod load order sorting. This rewrites the saved relevant mod order; restart the game before joining again."),
                    T("solution.sortMods.button", "Open mod order sort"),
                    Const.ModId,
                    "content-mod-load-order",
                    "content_mod_load_order_actions",
                    "content_mod_load_order_sort"),
                new(
                    T("solution.sortModelDb.title", "Sort ModelDb for this session"),
                    T("solution.sortModelDb.description",
                        "Open manual ModelDb deterministic sorting. This only rebuilds the current session's ModelDb network cache and does not persist after restart."),
                    T("solution.sortModelDb.button", "Open ModelDb sort"),
                    Const.ModId,
                    "core",
                    "core",
                    "modeldb_deterministic_sort_now"),
            ];
        }

        private static bool IsOrderOnlyModelDbMismatch(
            JoinPeerSnapshot? host,
            JoinPeerSnapshot local,
            IReadOnlyList<JoinFailureIssue> issues,
            JoinDiagnosticsPayload? hostPayload)
        {
            if (hostPayload == null ||
                host == null ||
                host.ModelDbHash == local.ModelDbHash ||
                host.ModelDbHashUsesDeterministicCache ||
                local.ModelDbHashUsesDeterministicCache)
                return false;

            return issues.Count == 2 &&
                   issues.Any(static issue => issue.Kind == JoinFailureIssueKind.ModOrder) &&
                   issues.Any(static issue => issue.Kind == JoinFailureIssueKind.ModelDb);
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
