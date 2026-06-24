using Godot;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarChunkOutboundRegistry
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<ulong, OutboundChunkStream> Active = [];
        private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(2);

        internal static void Register(OutboundChunkStream state)
        {
            lock (Gate)
            {
                Active[state.StreamId] = state;
            }

            ScheduleTtlDrop(state.StreamId, DefaultTtl);
        }

        internal static void TryRemove(ulong streamId)
        {
            lock (Gate)
            {
                Active.Remove(streamId);
            }
        }

        private static void ScheduleTtlDrop(ulong streamId, TimeSpan ttl)
        {
            if (Engine.GetMainLoop() is not SceneTree tree)
                return;

            var t = tree.CreateTimer((float)ttl.TotalSeconds);
            t.Timeout += OnTtl;
            return;

            void OnTtl()
            {
                t.Timeout -= OnTtl;
                lock (Gate)
                {
                    Active.Remove(streamId);
                }
            }
        }

        internal static void HandleSelectiveNack(
            RunManager? rm,
            ulong nackSenderNetId,
            ulong streamId,
            ulong userOpcode,
            uint count,
            RitsuLibSidecarChunkGapBinary.MissingRange[] missingRanges)
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            OutboundChunkStream? st;
            lock (Gate)
            {
                if (!Active.TryGetValue(streamId, out st))
                    return;
            }

            if (st.UserOpcode != userOpcode || st.Count != (int)count)
                return;

            switch (st.Kind)
            {
                case RitsuLibSidecarChunkSendKind.Client:
                    if (rm?.NetService is not NetClientGameService c || nackSenderNetId != c.HostNetId)
                        return;
                    break;
                case RitsuLibSidecarChunkSendKind.HostToPeer:
                    if (rm?.NetService is not NetHostGameService)
                        return;
                    if (st.UnicastClientNetId != nackSenderNetId)
                        return;
                    break;
                case RitsuLibSidecarChunkSendKind.HostBroadcast:
                    if (rm?.NetService is not NetHostGameService)
                        return;
                    break;
                default:
                    return;
            }

            foreach (var range in missingRanges)
            {
                if (range.Length == 0 || range.StartIndex >= count)
                    continue;

                var endExclusive = Math.Min(count, (ulong)range.StartIndex + range.Length);
                for (ulong i = range.StartIndex; i < endExclusive; i++)
                {
                    var frame = st.Frames[(int)i];
                    if (st.Kind == RitsuLibSidecarChunkSendKind.Client)
                        RitsuLibSidecarHighLevelSend.TrySendAsClient(
                            rm,
                            RitsuLibSidecarControlOpcodes.ChunkedFrame,
                            frame,
                            st.Semantics);
                    else
                        RitsuLibSidecarHighLevelSend.TrySendAsHostToPeer(
                            rm,
                            nackSenderNetId,
                            RitsuLibSidecarControlOpcodes.ChunkedFrame,
                            frame,
                            st.Semantics);
                }
            }
        }

        internal sealed class OutboundChunkStream
        {
            public required ulong StreamId { get; init; }
            public required ulong UserOpcode { get; init; }
            public required int Count { get; init; }
            public required byte[][] Frames { get; init; }
            public required RitsuLibSidecarChunkSendKind Kind { get; init; }
            public required RitsuLibSidecarDeliverySemantics Semantics { get; init; }
            public ulong? UnicastClientNetId { get; init; }
        }
    }
}
