using System.Buffers.Binary;
using System.Security.Cryptography;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal readonly record struct RitsuLibSidecarDiagnosticRelaySession(
        ulong OriginatingSenderNetId,
        uint ChecksumId,
        ushort Tag,
        ulong Nonce,
        long IssuedUnixMilliseconds);

    internal static class RitsuLibSidecarDiagnosticRelayGate
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<RelaySessionKey, RitsuLibSidecarDiagnosticRelaySession> HostSessions = [];
        private static readonly Dictionary<ulong, List<DateTime>> HostPeerStarts = [];
        private static readonly List<DateTime> HostGlobalStarts = [];
        private static readonly Dictionary<LocalSessionKey, DateTime> LocalDumpedSessions = [];
        private static readonly List<DateTime> LocalDumpTimes = [];

        internal static bool TryBeginHostSession(
            ulong originatingSenderNetId,
            uint checksumId,
            ushort tag,
            out RitsuLibSidecarDiagnosticRelaySession session)
        {
            session = default;
            if (!IsKnownTag(tag))
                return false;

            var now = DateTime.UtcNow;
            lock (Gate)
            {
                Sweep_NoLock(now);
                var key = new RelaySessionKey(originatingSenderNetId, checksumId, tag);
                if (HostSessions.TryGetValue(key, out session))
                    return false;

                if (IsHostRateLimited_NoLock(originatingSenderNetId, now))
                    return false;

                session = new(
                    originatingSenderNetId,
                    checksumId,
                    tag,
                    CreateNonce(),
                    new DateTimeOffset(now).ToUnixTimeMilliseconds());
                HostSessions[key] = session;
                AddHostStart_NoLock(originatingSenderNetId, now);
                return true;
            }
        }

        internal static bool TryValidateRequest(ulong senderNetId)
        {
            var now = DateTime.UtcNow;
            lock (Gate)
            {
                Sweep_NoLock(now);
                return HostSessions.Values.Any(session => session.OriginatingSenderNetId == senderNetId);
            }
        }

        internal static bool TryValidateFanout(
            INetGameService? netService,
            ulong senderNetId,
            RitsuLibSidecarDiagnosticRelaySession session)
        {
            if (netService is not NetClientGameService client || senderNetId != client.HostNetId)
                return false;
            if (!IsKnownTag(session.Tag))
                return false;

            if (!TryGetIssuedUtc(session, out var issued))
                return false;

            var now = DateTime.UtcNow;
            if (issued - now > RitsuLibSidecarDiagnosticPolicy.FanoutFutureTolerance)
                return false;
            return now - issued <= RitsuLibSidecarDiagnosticPolicy.RelaySessionTtl &&
                   RitsuLibSidecarSessionManager.CanSendToPeer(senderNetId);
        }

        internal static bool TryConsumeLocalDump(RitsuLibSidecarDiagnosticRelaySession session)
        {
            var now = DateTime.UtcNow;
            lock (Gate)
            {
                Sweep_NoLock(now);
                var key = new LocalSessionKey(
                    session.OriginatingSenderNetId,
                    session.ChecksumId,
                    session.Tag,
                    session.Nonce);
                if (LocalDumpedSessions.ContainsKey(key))
                    return false;
                if (LocalDumpTimes.Count >= RitsuLibSidecarDiagnosticPolicy.MaxLocalDumpsGlobalPerWindow)
                {
                    RitsuLibSidecarRepeatedWarningLog.Warn(
                        "diagnostic-relay-local-dump-rate-limit",
                        "[Sidecar] Diagnostic relay local dump suppressed by rate limit.");
                    return false;
                }

                LocalDumpedSessions[key] = now;
                LocalDumpTimes.Add(now);
                return true;
            }
        }

        private static bool IsHostRateLimited_NoLock(ulong peerNetId, DateTime now)
        {
            if (HostGlobalStarts.Count >= RitsuLibSidecarDiagnosticPolicy.MaxRelaySessionsGlobalPerWindow)
            {
                RitsuLibSidecarRepeatedWarningLog.Warn(
                    "diagnostic-relay-host-global-rate-limit",
                    "[Sidecar] Diagnostic relay host session suppressed by global rate limit.");
                return true;
            }

            if (!HostPeerStarts.TryGetValue(peerNetId, out var starts))
                return false;
            if (starts.Count < RitsuLibSidecarDiagnosticPolicy.MaxRelaySessionsPerPeerPerWindow)
                return false;

            RitsuLibSidecarRepeatedWarningLog.Warn(
                $"diagnostic-relay-host-peer-rate-limit:{peerNetId}",
                $"[Sidecar] Diagnostic relay host session suppressed by peer rate limit peer={peerNetId}.");
            return true;
        }

        private static void AddHostStart_NoLock(ulong peerNetId, DateTime now)
        {
            if (!HostPeerStarts.TryGetValue(peerNetId, out var starts))
            {
                starts = [];
                HostPeerStarts[peerNetId] = starts;
            }

            starts.Add(now);
            HostGlobalStarts.Add(now);
        }

        private static bool IsKnownTag(ushort tag)
        {
            return tag == RitsuLibSidecarDiagnosticPolicy.DivergenceRelayTag;
        }

        private static ulong CreateNonce()
        {
            Span<byte> bytes = stackalloc byte[RitsuLibSidecarBinaryLayout.U64Size];
            RandomNumberGenerator.Fill(bytes);
            return BinaryPrimitives.ReadUInt64BigEndian(bytes);
        }

        private static void Sweep_NoLock(DateTime now)
        {
            SweepList_NoLock(HostGlobalStarts, now, RitsuLibSidecarDiagnosticPolicy.RelayRateWindow);
            SweepList_NoLock(LocalDumpTimes, now, RitsuLibSidecarDiagnosticPolicy.LocalDumpRateWindow);

            foreach (var (peer, starts) in HostPeerStarts.ToArray())
            {
                SweepList_NoLock(starts, now, RitsuLibSidecarDiagnosticPolicy.RelayRateWindow);
                if (starts.Count == 0)
                    HostPeerStarts.Remove(peer);
            }

            foreach (var (key, session) in HostSessions.ToArray())
                if (!TryGetIssuedUtc(session, out var issued) ||
                    now - issued > RitsuLibSidecarDiagnosticPolicy.RelaySessionTtl)
                    HostSessions.Remove(key);

            foreach (var (key, dumpedAt) in LocalDumpedSessions.ToArray())
                if (now - dumpedAt > RitsuLibSidecarDiagnosticPolicy.LocalDumpRateWindow)
                    LocalDumpedSessions.Remove(key);
        }

        private static bool TryGetIssuedUtc(RitsuLibSidecarDiagnosticRelaySession session, out DateTime issued)
        {
            try
            {
                issued = DateTimeOffset.FromUnixTimeMilliseconds(session.IssuedUnixMilliseconds).UtcDateTime;
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                issued = default;
                return false;
            }
        }

        private static void SweepList_NoLock(List<DateTime> values, DateTime now, TimeSpan retention)
        {
            for (var i = values.Count - 1; i >= 0; i--)
                if (now - values[i] > retention)
                    values.RemoveAt(i);
        }

        private readonly record struct RelaySessionKey(ulong OriginatingSenderNetId, uint ChecksumId, ushort Tag);

        private readonly record struct LocalSessionKey(
            ulong OriginatingSenderNetId,
            uint ChecksumId,
            ushort Tag,
            ulong Nonce);
    }
}
