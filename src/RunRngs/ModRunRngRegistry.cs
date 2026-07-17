#if STS2_AT_LEAST_0_109_0
using MegaCrit.Sts2.Core.Saves;
#endif
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.RunData;
using StsRng = MegaCrit.Sts2.Core.Random.Rng;

namespace STS2RitsuLib.RunRngs
{
    /// <summary>
    ///     Provides independent per-run RNG streams for mods.
    ///     为 Mod 提供独立的按跑局随机数流。
    /// </summary>
    public static class ModRunRngRegistry
    {
        private const string SaveKey = "mod_run_rng";
        private const string StreamSeedPrefix = "ritsulib.mod_run_rng";

        private static readonly RunSavedData<ModRunRngState> SavedData =
            RunSavedDataStore.For(Const.ModId).Register<ModRunRngState>(
                SaveKey,
                () => new(),
                new() { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault });

        private static readonly ConditionalWeakTable<RunState, RuntimeState> Runtimes = new();

        /// <summary>
        ///     Gets an independent RNG stream for the given run, mod id, and stream id.
        ///     按跑局、mod ID 和流 ID 获取一条独立 RNG 流。
        /// </summary>
        public static StsRng Get(RunState runState, string modId, string streamId)
        {
            ArgumentNullException.ThrowIfNull(runState);
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

            var runtime = Runtimes.GetValue(runState, _ => new());
            return runtime.GetOrCreate(runState, modId, streamId);
        }

        /// <summary>
        ///     Gets an independent RNG stream scoped to a player within the run.
        ///     获取作用域为跑局中某个玩家的独立 RNG 流。
        /// </summary>
        public static StsRng Get(Player player, string modId, string streamId)
        {
            ArgumentNullException.ThrowIfNull(player);
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

            if (player.RunState is not RunState runState)
                throw new InvalidOperationException("Player does not belong to a concrete RunState.");

            var runtime = Runtimes.GetValue(runState, _ => new());
            return runtime.GetOrCreate(player, modId, streamId);
        }

        internal static void SyncToSavedData(RunState runState)
        {
            if (!Runtimes.TryGetValue(runState, out var runtime) || runtime.IsEmpty)
                return;

            var state = SavedData.Get(runState);
            runtime.CopyStateTo(state);
            SavedData.Set(runState, state);
        }

        private sealed class RuntimeState
        {
            private readonly Dictionary<PlayerStreamKey, StsRng> _playerRngs = [];
            private readonly Dictionary<StreamKey, StsRng> _runRngs = [];
            private readonly Lock _syncRoot = new();

            public bool IsEmpty
            {
                get
                {
                    lock (_syncRoot)
                    {
                        return _runRngs.Count == 0 && _playerRngs.Count == 0;
                    }
                }
            }

            public StsRng GetOrCreate(RunState runState, string modId, string streamId)
            {
                var key = new StreamKey(modId, streamId);
                lock (_syncRoot)
                {
                    if (_runRngs.TryGetValue(key, out var rng))
                        return rng;

                    rng = CreateRunRng(runState, key);
                    _runRngs[key] = rng;
                    return rng;
                }
            }

            public StsRng GetOrCreate(Player player, string modId, string streamId)
            {
                var key = new PlayerStreamKey(player.NetId, modId, streamId);
                lock (_syncRoot)
                {
                    if (_playerRngs.TryGetValue(key, out var rng))
                        return rng;

                    rng = CreatePlayerRng(player, key);
                    _playerRngs[key] = rng;
                    return rng;
                }
            }

            public void CopyStateTo(ModRunRngState state)
            {
                lock (_syncRoot)
                {
                    foreach (var (key, rng) in _runRngs)
                    {
                        state.SetRunCounter(key.ModId, key.StreamId, rng.GetCounter());
#if STS2_AT_LEAST_0_109_0
                        state.SetRunRngState(key.ModId, key.StreamId, ModRunRngSnapshot.From(rng));
#endif
                    }

                    foreach (var (key, rng) in _playerRngs)
                    {
                        state.SetPlayerCounter(key.NetId, key.ModId, key.StreamId, rng.GetCounter());
#if STS2_AT_LEAST_0_109_0
                        state.SetPlayerRngState(key.NetId, key.ModId, key.StreamId, ModRunRngSnapshot.From(rng));
#endif
                    }
                }
            }

            private static StsRng CreateRunRng(RunState runState, StreamKey key)
            {
                var streamSeedName = $"{StreamSeedPrefix}/run/{key.ModId}/{key.StreamId}";
                var hasState = SavedData.TryGet(runState, out var state);
#if STS2_AT_LEAST_0_109_0
                if (hasState && state.TryGetRunRngState(key.ModId, key.StreamId, out var rngState))
                    return new(rngState.ToSerializable());
#endif
                var counter = hasState ? state.GetRunCounter(key.ModId, key.StreamId) : 0;

                var rng = new StsRng(runState.Rng.Seed, streamSeedName);
                return rng.FastForwarded(counter);
            }

            private static StsRng CreatePlayerRng(Player player, PlayerStreamKey key)
            {
                var streamSeedName = $"{StreamSeedPrefix}/player/{key.ModId}/{key.StreamId}";
                ModRunRngState state = null!;
                var hasState = player.RunState is RunState runState && SavedData.TryGet(runState, out state);
#if STS2_AT_LEAST_0_109_0
                if (hasState && state.TryGetPlayerRngState(key.NetId, key.ModId, key.StreamId, out var rngState))
                    return new(rngState.ToSerializable());
#endif
                var counter = hasState ? state.GetPlayerCounter(key.NetId, key.ModId, key.StreamId) : 0;

                var rng = new StsRng(player.PlayerRng.Seed, streamSeedName);
                return rng.FastForwarded(counter);
            }
        }

        private readonly record struct StreamKey(string ModId, string StreamId);

        private readonly record struct PlayerStreamKey(ulong NetId, string ModId, string StreamId);
    }

    /// <summary>
    ///     Saved counter state for mod RNG streams.
    ///     Mod RNG 流的已保存 counter 状态。
    /// </summary>
    public sealed class ModRunRngState
    {
        /// <summary>
        ///     Counter values grouped by mod id and stream id.
        ///     按 mod ID 和流 ID 分组的 counter 值。
        /// </summary>
        public Dictionary<string, Dictionary<string, int>> Counters { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Player-scoped counter values grouped by net id, mod id, and stream id.
        ///     按 net id、mod ID 和流 ID 分组的玩家作用域 counter 值。
        /// </summary>
        public Dictionary<ulong, Dictionary<string, Dictionary<string, int>>> PlayerCounters { get; set; } = [];

        /// <summary>
        ///     Full RNG states grouped by mod id and stream id.
        ///     按 mod ID 和流 ID 分组的完整 RNG 状态。
        /// </summary>
        public Dictionary<string, Dictionary<string, ModRunRngSnapshot>> RngStates { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Player-scoped full RNG states grouped by net id, mod id, and stream id.
        ///     按 net id、mod ID 和流 ID 分组的玩家作用域完整 RNG 状态。
        /// </summary>
        public Dictionary<ulong, Dictionary<string, Dictionary<string, ModRunRngSnapshot>>> PlayerRngStates
        {
            get;
            set;
        } =
            [];

        internal int GetRunCounter(string modId, string streamId)
        {
            return Counters.TryGetValue(modId, out var streams) &&
                   streams.TryGetValue(streamId, out var counter) &&
                   counter > 0
                ? counter
                : 0;
        }

        internal int GetPlayerCounter(ulong netId, string modId, string streamId)
        {
            return PlayerCounters.TryGetValue(netId, out var mods) &&
                   mods.TryGetValue(modId, out var streams) &&
                   streams.TryGetValue(streamId, out var counter) &&
                   counter > 0
                ? counter
                : 0;
        }

        internal bool TryGetRunRngState(string modId, string streamId, out ModRunRngSnapshot state)
        {
            if (RngStates.TryGetValue(modId, out var streams) &&
                streams.TryGetValue(streamId, out var found))
            {
                state = found;
                return true;
            }

            state = null!;
            return false;
        }

        internal bool TryGetPlayerRngState(
            ulong netId,
            string modId,
            string streamId,
            out ModRunRngSnapshot state)
        {
            if (PlayerRngStates.TryGetValue(netId, out var mods) &&
                mods.TryGetValue(modId, out var streams) &&
                streams.TryGetValue(streamId, out var found))
            {
                state = found;
                return true;
            }

            state = null!;
            return false;
        }

        internal void SetRunCounter(string modId, string streamId, int counter)
        {
            if (!Counters.TryGetValue(modId, out var streams))
            {
                streams = new(StringComparer.OrdinalIgnoreCase);
                Counters[modId] = streams;
            }

            streams[streamId] = Math.Max(0, counter);
        }

        internal void SetPlayerCounter(ulong netId, string modId, string streamId, int counter)
        {
            if (!PlayerCounters.TryGetValue(netId, out var mods))
            {
                mods = new(StringComparer.OrdinalIgnoreCase);
                PlayerCounters[netId] = mods;
            }

            if (!mods.TryGetValue(modId, out var streams))
            {
                streams = new(StringComparer.OrdinalIgnoreCase);
                mods[modId] = streams;
            }

            streams[streamId] = Math.Max(0, counter);
        }

        internal void SetRunRngState(string modId, string streamId, ModRunRngSnapshot state)
        {
            if (!RngStates.TryGetValue(modId, out var streams))
            {
                streams = new(StringComparer.OrdinalIgnoreCase);
                RngStates[modId] = streams;
            }

            streams[streamId] = state;
        }

        internal void SetPlayerRngState(
            ulong netId,
            string modId,
            string streamId,
            ModRunRngSnapshot state)
        {
            if (!PlayerRngStates.TryGetValue(netId, out var mods))
            {
                mods = new(StringComparer.OrdinalIgnoreCase);
                PlayerRngStates[netId] = mods;
            }

            if (!mods.TryGetValue(modId, out var streams))
            {
                streams = new(StringComparer.OrdinalIgnoreCase);
                mods[modId] = streams;
            }

            streams[streamId] = state;
        }
    }

    /// <summary>
    ///     Serializable full state for a mod RNG stream.
    ///     Mod RNG 流的可序列化完整状态。
    /// </summary>
    public sealed class ModRunRngSnapshot
    {
        /// <summary>
        ///     Gets or sets the number of values generated by the stream.
        ///     获取或设置该随机数流已生成的值数量。
        /// </summary>
        public int Counter { get; set; }

        /// <summary>
        ///     Gets or sets the first word of the generator state.
        ///     获取或设置生成器状态的第一个字。
        /// </summary>
        public ulong State0 { get; set; }

        /// <summary>
        ///     Gets or sets the second word of the generator state.
        ///     获取或设置生成器状态的第二个字。
        /// </summary>
        public ulong State1 { get; set; }

        /// <summary>
        ///     Gets or sets the third word of the generator state.
        ///     获取或设置生成器状态的第三个字。
        /// </summary>
        public ulong State2 { get; set; }

        /// <summary>
        ///     Gets or sets the fourth word of the generator state.
        ///     获取或设置生成器状态的第四个字。
        /// </summary>
        public ulong State3 { get; set; }

#if STS2_AT_LEAST_0_109_0
        internal static ModRunRngSnapshot From(StsRng rng)
        {
            var state = rng.ToSerializable();
            return new()
            {
                Counter = state.counter,
                State0 = state.state0,
                State1 = state.state1,
                State2 = state.state2,
                State3 = state.state3,
            };
        }

        internal SerializableRng ToSerializable()
        {
            return new()
            {
                counter = Counter,
                state0 = State0,
                state1 = State1,
                state2 = State2,
                state3 = State3,
            };
        }
#endif
    }

    internal static class ModRunRngExtensions
    {
        public static int GetCounter(this StsRng rng)
        {
#if STS2_AT_LEAST_0_109_0
            return rng.ToSerializable().counter;
#else
            return rng.Counter;
#endif
        }

        public static StsRng FastForwarded(this StsRng rng, int counter)
        {
#if STS2_AT_LEAST_0_109_0
            for (var i = 0; i < counter; i++)
                rng.NextUnsignedLong();
#else
            if (counter > 0)
                rng.FastForwardCounter(counter);
#endif

            return rng;
        }
    }
}
