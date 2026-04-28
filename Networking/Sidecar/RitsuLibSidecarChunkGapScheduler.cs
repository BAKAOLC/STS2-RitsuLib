using Godot;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarChunkGapScheduler
    {
        private const float DebounceSeconds = 0.075f;

        private static readonly Lock Gate = new();

        private static readonly Dictionary<(ulong Peer, ulong StreamId), Entry> Pending = [];

        internal static void ScheduleGapReport(
            RitsuLibSidecarChunkReassembly reassembly,
            RunManager? rm,
            ulong chunkSourceNetId,
            ulong streamId,
            ulong userOpcode,
            uint partCount)
        {
            if (rm == null || Engine.GetMainLoop() is not SceneTree tree)
                return;

            var key = (chunkSourceNetId, streamId);
            lock (Gate)
            {
                if (Pending.TryGetValue(key, out var existing) && existing.Timer != null)
                    return;

                var e = new Entry();
                Pending[key] = e;
                e.Timer = tree.CreateTimer(DebounceSeconds);
                e.Timer.Timeout += () => Flush(reassembly, rm, key, e, userOpcode, partCount);
            }
        }

        internal static void Cancel(ulong chunkSourceNetId, ulong streamId)
        {
            var key = (chunkSourceNetId, streamId);
            lock (Gate)
            {
                if (Pending.TryGetValue(key, out var e))
                    e.Aborted = true;

                Pending.Remove(key);
            }
        }

        private static void Flush(
            RitsuLibSidecarChunkReassembly reassembly,
            RunManager? rm,
            (ulong Peer, ulong StreamId) key,
            Entry e,
            ulong userOpcode,
            uint partCount)
        {
            try
            {
                if (e.Aborted)
                    return;

                RitsuLibSidecarProtocol.EnsureDefaultHandlers();
                var (dest, streamId) = key;
                if (!reassembly.TryListMissingIndices(dest, streamId, out var miss) || miss is null)
                    return;

                Array.Sort(miss);
                var ranges = BuildRanges(miss);
                var o = 0;
                while (o < ranges.Count)
                {
                    var take = Math.Min(
                        RitsuLibSidecarChunkGapBinary.MaxMissingRangesPerMessage,
                        ranges.Count - o);
                    var slice = ranges.GetRange(o, take).ToArray();
                    var need = RitsuLibSidecarChunkGapBinary.SelectiveNackHeaderSize +
                               slice.Length * RitsuLibSidecarChunkGapBinary.SelectiveNackRangeSize;
                    var buf = GC.AllocateUninitializedArray<byte>(need);
                    RitsuLibSidecarChunkGapBinary.WriteSelectiveNack(
                        buf.AsSpan(),
                        streamId,
                        userOpcode,
                        partCount,
                        slice);
                    RitsuLibSidecarControlPeerSend.SendToNetPeer(
                        rm,
                        dest,
                        RitsuLibSidecarControlOpcodes.ChunkStreamSelectiveNack,
                        buf);
                    o += take;
                }
            }
            finally
            {
                lock (Gate)
                {
                    Pending.Remove(key);
                }
            }
        }

        private static List<RitsuLibSidecarChunkGapBinary.MissingRange> BuildRanges(uint[] sorted)
        {
            var outRanges = new List<RitsuLibSidecarChunkGapBinary.MissingRange>();
            if (sorted.Length == 0)
                return outRanges;

            var start = sorted[0];
            var prev = sorted[0];
            for (var i = 1; i < sorted.Length; i++)
            {
                var cur = sorted[i];
                if (cur == prev + 1)
                {
                    prev = cur;
                    continue;
                }

                outRanges.Add(new(start, prev - start + 1));
                start = cur;
                prev = cur;
            }

            outRanges.Add(new(start, prev - start + 1));
            return outRanges;
        }

        private sealed class Entry
        {
            public bool Aborted;
            public SceneTreeTimer? Timer;
        }
    }
}
