using System.Diagnostics.CodeAnalysis;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal sealed class RitsuLibSidecarChunkReassembly
    {
        internal static readonly TimeSpan IncompleteStreamRetentionDefault = TimeSpan.FromMinutes(2);

        private readonly Lock _gate = new();

        private readonly Dictionary<(ulong Sender, ulong StreamId), StreamState> _streams = [];

        internal TimeSpan IncompleteStreamRetention { get; set; } = IncompleteStreamRetentionDefault;

        internal bool TryIngest(
            ulong sender,
            ulong userOpcode,
            ulong streamId,
            uint index,
            uint count,
            uint totalPayloadSize,
            ReadOnlySpan<byte> segment,
            [NotNullWhen(true)] out byte[]? completed)
        {
            completed = null;
            var key = (sender, streamId);
            RitsuLibSidecarChunkReceiveProgress? notify = null;
            var result = false;
            lock (_gate)
            {
                SweepStale_NoLock(DateTime.UtcNow);

                if (!_streams.TryGetValue(key, out var st))
                {
                    if (count == 0 || totalPayloadSize == 0 ||
                        index >= count ||
                        segment.Length > totalPayloadSize)
                    {
                        RitsuLibFramework.Logger.Warn("[Sidecar] Chunk rejected (invalid opening frame).");
                        return false;
                    }

                    st = new()
                    {
                        UserOpcode = userOpcode,
                        ExpectedCount = (int)count,
                        TotalLogicalSize = (int)totalPayloadSize,
                        Parts = new byte[(int)count][],
                        ReceivedIndices = 0,
                        AccumulatedLogicalBytes = 0,
                        CreatedUtc = DateTime.UtcNow,
                    };
                    _streams[key] = st;
                }
                else if (st.UserOpcode != userOpcode ||
                         st.ExpectedCount != (int)count ||
                         st.TotalLogicalSize != (int)totalPayloadSize)
                {
                    RitsuLibFramework.Logger.Warn("[Sidecar] Chunk metadata mismatch; dropping partial stream.");
                    _streams.Remove(key);
                    return false;
                }

                if (index >= (uint)st.Parts!.Length)
                {
                    RitsuLibFramework.Logger.Warn("[Sidecar] Chunk index overflow; dropping stream.");
                    _streams.Remove(key);
                    return false;
                }

                if (st.Parts[index] != null)
                {
                    result = TryComplete_NoLock(st, key, ref completed);
                    if (result)
                        notify = new RitsuLibSidecarChunkReceiveProgress(
                            sender,
                            streamId,
                            st.UserOpcode,
                            st.ReceivedIndices,
                            st.ExpectedCount,
                            st.AccumulatedLogicalBytes,
                            st.TotalLogicalSize,
                            true);
                }
                else
                {
                    var owned = GC.AllocateUninitializedArray<byte>(segment.Length);
                    segment.CopyTo(owned.AsSpan());
                    st.Parts[index] = owned;
                    st.ReceivedIndices++;
                    st.AccumulatedLogicalBytes += owned.Length;

                    result = TryComplete_NoLock(st, key, ref completed);
                    notify = new RitsuLibSidecarChunkReceiveProgress(
                        sender,
                        streamId,
                        st.UserOpcode,
                        st.ReceivedIndices,
                        st.ExpectedCount,
                        st.AccumulatedLogicalBytes,
                        st.TotalLogicalSize,
                        result);
                }
            }

            if (notify.HasValue)
                RitsuLibSidecarChunkTransferNotifications.RaiseReceive(notify.Value);

            return result;
        }

        internal bool TryListMissingIndices(
            ulong sender,
            ulong streamId,
            [NotNullWhen(true)] out uint[]? missing)
        {
            missing = null;
            var key = (sender, streamId);
            lock (_gate)
            {
                if (!_streams.TryGetValue(key, out var st) || st.Parts == null)
                    return false;

                var n = st.Parts.Count(t => t == null);

                if (n == 0)
                    return false;

                var a = new uint[n];
                for (int i = 0, j = 0; i < st.Parts.Length; i++)
                    if (st.Parts[i] == null)
                        a[j++] = (uint)i;

                missing = a;
                return true;
            }
        }

        private bool TryComplete_NoLock(StreamState st, (ulong, ulong) key,
            ref byte[]? completed)
        {
            if (st.Parts == null ||
                st.ReceivedIndices != st.ExpectedCount ||
                st.AccumulatedLogicalBytes != st.TotalLogicalSize)
                return false;

            if (st.Parts.Any(t => t == null)) return false;

            var merged = GC.AllocateUninitializedArray<byte>(st.TotalLogicalSize);
            var o = 0;
            foreach (var part in st.Parts)
            {
                part.AsSpan().CopyTo(merged.AsSpan(o));
                o += part.Length;
            }

            if (o != merged.Length)
            {
                RitsuLibFramework.Logger.Warn("[Sidecar] Chunk merged length mismatch.");
                _streams.Remove(key);
                return false;
            }

            completed = merged;
            _streams.Remove(key);
            return true;
        }

        private void SweepStale_NoLock(DateTime utcNow)
        {
            if (_streams.Count == 0 || IncompleteStreamRetention <= TimeSpan.Zero)
                return;

            List<(ulong Sender, ulong StreamId)>? dead = null;
            foreach (var kv in _streams.Where(kv => utcNow - kv.Value.CreatedUtc > IncompleteStreamRetention))
            {
                dead ??= [];
                dead.Add(kv.Key);
            }

            if (dead == null)
                return;

            foreach (var k in dead)
            {
                _streams.Remove(k);
                RitsuLibFramework.Logger.Warn($"[Sidecar] Chunk stream stale evicted ({k}).");
            }
        }

        private sealed class StreamState
        {
            public ulong UserOpcode { get; init; }

            public int ExpectedCount { get; init; }

            public int TotalLogicalSize { get; init; }

            public byte[][]? Parts { get; init; }

            public int ReceivedIndices { get; set; }

            public int AccumulatedLogicalBytes { get; set; }

            public DateTime CreatedUtc { get; init; }
        }
    }
}
