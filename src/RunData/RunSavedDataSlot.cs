using System.Text.Json;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.RunData
{
    internal interface IRunSavedDataSlot
    {
        string ModId { get; }
        string Key { get; }
        RunSavedDataKind Kind { get; }
        RunSavedDataOptions Options { get; }
        RunSavedDataSlotKey SlotKey { get; }
        void Import(RunState runState, RunSavedDataDocument document);
        void Export(RunState runState, RunSavedDataDocument document);

        bool TryExportLobbyContribution(
            RunSavedDataLobbySession session,
            ulong netId,
            bool isHostNetId,
            RunSavedDataDocument document);

        bool TryExportLobbyStaging(RunSavedDataLobbySession session, RunSavedDataDocument document);
        void ImportLobbyContribution(RunSavedDataLobbySession session, ulong netId, bool isHostNetId, JsonObject entry);
        void CommitLobbyStaging(RunSavedDataLobbySession session, RunState runState);
    }

    internal abstract class RunSavedDataSlot<T>(
        string modId,
        string key,
        Func<T>? defaultFactory,
        RunSavedDataOptions? options,
        RunSavedDataKind kind)
        : IRunSavedDataSlot where T : class, new()
    {
        private const string SchemaPropertyName = "schema";
        private const string KindPropertyName = "kind";
        private const string DataPropertyName = "data";
        private const string PlayersPropertyName = "players";

        private readonly Func<T> _defaultFactory = defaultFactory ?? (() => new());

        public string ModId { get; } = modId;
        public string Key { get; } = key;
        public RunSavedDataKind Kind { get; } = kind;
        public RunSavedDataOptions Options { get; } = options ?? new();
        public RunSavedDataSlotKey SlotKey { get; } = new(modId, key, kind);

        public void Import(RunState runState, RunSavedDataDocument document)
        {
            if (!document.TryGetRaw(ModId, Key, out var entry))
                return;

            try
            {
                ImportCore(runState, entry);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RunSavedData] Failed to import '{ModId}'::{Key}: {ex.Message}");
            }
        }

        public virtual bool TryExportLobbyContribution(
            RunSavedDataLobbySession session,
            ulong netId,
            bool isHostNetId,
            RunSavedDataDocument document)
        {
            return false;
        }

        public virtual void ImportLobbyContribution(
            RunSavedDataLobbySession session,
            ulong netId,
            bool isHostNetId,
            JsonObject entry)
        {
        }

        public virtual bool TryExportLobbyStaging(RunSavedDataLobbySession session, RunSavedDataDocument document)
        {
            return false;
        }

        public virtual void CommitLobbyStaging(RunSavedDataLobbySession session, RunState runState)
        {
        }

        public void Export(RunState runState, RunSavedDataDocument document)
        {
            if (!RunSavedDataRuntime.TryGetBag(runState, out var bag))
                return;

            if (!TryBuildEntry(runState, bag, out var entry))
            {
                if (bag.IsDirty(SlotKey))
                    document.Remove(ModId, Key);
                return;
            }

            document.SetRaw(ModId, Key, entry);
        }

        public T GetOrCreate(RunState runState)
        {
            var bag = RunSavedDataRuntime.GetBag(runState);
            if (bag.TryGet(SlotKey, out var value) && value is T typed)
                return typed;

            var created = _defaultFactory();
            bag.Set(SlotKey, created, false);
            return created;
        }

        public bool TryGet(RunState runState, out T value)
        {
            if (RunSavedDataRuntime.TryGetBag(runState, out var bag))
                if (bag.TryGet(SlotKey, out var raw) && raw is T typed)
                {
                    value = typed;
                    return true;
                }

            value = null!;
            return false;
        }

        public void Set(RunState runState, T value)
        {
            RunSavedDataRuntime.GetBag(runState).Set(SlotKey, value);
        }

        public bool Remove(RunState runState)
        {
            return RunSavedDataRuntime.GetBag(runState).Remove(SlotKey);
        }

        protected abstract bool TryBuildEntry(RunState runState, RunSavedDataBag bag, out JsonObject entry);
        protected abstract void ImportCore(RunState runState, JsonObject entry);

        protected JsonObject CreateRunEntry(T value)
        {
            return new()
            {
                [SchemaPropertyName] = Options.SchemaVersion,
                [KindPropertyName] = "run",
                [DataPropertyName] = JsonSerializer.SerializeToNode(value, RunSavedDataJson.Options),
            };
        }

        protected JsonObject CreatePlayerEntry(JsonObject players)
        {
            return new()
            {
                [SchemaPropertyName] = Options.SchemaVersion,
                [KindPropertyName] = "player",
                [PlayersPropertyName] = players,
            };
        }

        protected bool TryReadData(JsonObject entry, out T value)
        {
            value = null!;
            var schema = entry[SchemaPropertyName]?.GetValue<int>() ?? 1;
            if (!TryMigrate(entry, schema, out var migrated))
                return false;

            var dataNode = migrated[DataPropertyName];

            var deserialized = dataNode?.Deserialize<T>(RunSavedDataJson.Options);
            if (deserialized == null)
                return false;

            value = deserialized;
            return true;
        }

        protected bool TryReadPlayer(JsonNode? node, out T value)
        {
            value = null!;

            var deserialized = node?.Deserialize<T>(RunSavedDataJson.Options);
            if (deserialized == null)
                return false;

            value = deserialized;
            return true;
        }

        protected JsonNode? ToNode(T value)
        {
            return JsonSerializer.SerializeToNode(value, RunSavedDataJson.Options);
        }

        protected bool ShouldWrite(RunSavedDataBag bag, object? value)
        {
            return Options.WritePolicy switch
            {
                RunSavedDataWritePolicy.AlwaysWhenRegistered => value != null,
                RunSavedDataWritePolicy.WhenNonDefault => value != null && !IsDefaultValue((T)value),
                _ => bag.IsDirty(SlotKey) && value != null,
            };
        }

        protected static string PlayerKey(ulong netId)
        {
            return netId.ToString();
        }

        private bool TryMigrate(JsonObject entry, int schema, out JsonObject migrated)
        {
            migrated = entry;
            if (schema == Options.SchemaVersion)
                return true;

            if (schema > Options.SchemaVersion)
                return false;

            if (Options.Migrations == null || Options.Migrations.Count == 0)
                return false;

            migrated = entry.DeepClone().AsObject();
            var current = schema;
            while (current != Options.SchemaVersion)
            {
                var migration = Options.Migrations.FirstOrDefault(m => m.FromVersion == current);
                if (migration == null || !migration.Migrate(migrated))
                    return false;

                current = migration.ToVersion;
                migrated[SchemaPropertyName] = current;
            }

            return true;
        }

        private bool IsDefaultValue(T value)
        {
            try
            {
                var left = JsonSerializer.SerializeToNode(value, RunSavedDataJson.Options)?.ToJsonString();
                var right = JsonSerializer.SerializeToNode(_defaultFactory(), RunSavedDataJson.Options)?.ToJsonString();
                return string.Equals(left, right, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }
    }

    internal sealed class RunSavedDataRunSlot<T>(
        string modId,
        string key,
        Func<T>? defaultFactory,
        RunSavedDataOptions? options)
        : RunSavedDataSlot<T>(modId, key, defaultFactory, options, RunSavedDataKind.Run) where T : class, new()
    {
        protected override bool TryBuildEntry(RunState runState, RunSavedDataBag bag, out JsonObject entry)
        {
            entry = null!;
            if (!bag.TryGet(SlotKey, out var raw) || raw is not T value || !ShouldWrite(bag, value))
                return false;

            entry = CreateRunEntry(value);
            return true;
        }

        protected override void ImportCore(RunState runState, JsonObject entry)
        {
            if (TryReadData(entry, out var value))
            {
                RunSavedDataRuntime.GetBag(runState).Set(SlotKey, value, false);
                return;
            }

            RitsuLibFramework.Logger.Warn($"[RunSavedData] Failed to read run data '{ModId}'::{Key}.");
        }

        public override bool TryExportLobbyContribution(
            RunSavedDataLobbySession session,
            ulong netId,
            bool isHostNetId,
            RunSavedDataDocument document)
        {
            if (!isHostNetId || !session.TryGetRun(SlotKey, out var raw) || raw is not T typed)
                return false;

            document.SetRaw(ModId, Key, CreateRunEntry(typed));
            return true;
        }

        public override void ImportLobbyContribution(
            RunSavedDataLobbySession session,
            ulong netId,
            bool isHostNetId,
            JsonObject entry)
        {
            if (!isHostNetId || !TryReadData(entry, out var value))
                return;

            session.SetRun(SlotKey, value);
        }

        public override bool TryExportLobbyStaging(RunSavedDataLobbySession session, RunSavedDataDocument document)
        {
            if (!session.TryGetRun(SlotKey, out var raw) || raw is not T typed)
                return false;

            document.SetRaw(ModId, Key, CreateRunEntry(typed));
            return true;
        }

        public override void CommitLobbyStaging(RunSavedDataLobbySession session, RunState runState)
        {
            if (!session.TryGetRun(SlotKey, out var raw) || raw is not T typed)
                return;

            RunSavedDataRuntime.GetBag(runState).Set(SlotKey, typed, false);
        }
    }

    internal sealed class RunSavedDataPlayerSlot<T>(
        string modId,
        string key,
        Func<T>? defaultFactory,
        RunSavedDataOptions? options)
        : RunSavedDataSlot<Dictionary<ulong, T>>(modId, key, null, options, RunSavedDataKind.Player)
        where T : class, new()
    {
        private readonly Func<T> _defaultFactory = defaultFactory ?? (() => new());

        public T GetOrCreate(RunState runState, ulong netId)
        {
            var values = GetOrCreate(runState);
            if (values.TryGetValue(netId, out var value))
                return value;

            value = _defaultFactory();
            values[netId] = value;
            return value;
        }

        public bool TryGet(RunState runState, ulong netId, out T value)
        {
            if (TryGet(runState, out var values) && values.TryGetValue(netId, out value!))
                return true;

            value = null!;
            return false;
        }

        public void Set(RunState runState, ulong netId, T value)
        {
            var values = GetOrCreate(runState);
            values[netId] = value;
            RunSavedDataRuntime.GetBag(runState).Set(SlotKey, values);
        }

        public bool Remove(RunState runState, ulong netId)
        {
            if (!TryGet(runState, out var values))
                return false;

            var removed = values.Remove(netId);
            if (removed)
                RunSavedDataRuntime.GetBag(runState).Set(SlotKey, values);
            return removed;
        }

        public T Modify(Player player, Action<T> mutate)
        {
            return Modify(GetRunState(player), player.NetId, mutate);
        }

        public T Modify(RunState runState, ulong netId, Action<T> mutate)
        {
            ArgumentNullException.ThrowIfNull(mutate);
            var value = GetOrCreate(runState, netId);
            mutate(value);
            Set(runState, netId, value);
            return value;
        }

        protected override bool TryBuildEntry(RunState runState, RunSavedDataBag bag, out JsonObject entry)
        {
            entry = null!;
            if (!bag.TryGet(SlotKey, out var raw) ||
                raw is not Dictionary<ulong, T> values ||
                values.Count == 0 ||
                !ShouldWrite(bag, values))
                return false;

            var players = new JsonObject();
            foreach (var (netId, value) in values.OrderBy(pair => pair.Key))
                players[PlayerKey(netId)] = JsonSerializer.SerializeToNode(value, RunSavedDataJson.Options);

            entry = CreatePlayerEntry(players);
            return true;
        }

        protected override void ImportCore(RunState runState, JsonObject entry)
        {
            if (entry["players"] is not JsonObject players)
                return;

            var values = new Dictionary<ulong, T>();
            foreach (var (key, node) in players)
                if (ulong.TryParse(key, out var netId) && TryReadPlayerValue(node, out var value))
                    values[netId] = value;

            if (values.Count > 0)
                RunSavedDataRuntime.GetBag(runState).Set(SlotKey, values, false);
        }

        public override bool TryExportLobbyContribution(
            RunSavedDataLobbySession session,
            ulong netId,
            bool isHostNetId,
            RunSavedDataDocument document)
        {
            if (!session.TryGetPlayer(SlotKey, netId, out var raw) || raw is not T typed)
                return false;

            var players = new JsonObject
                { [PlayerKey(netId)] = JsonSerializer.SerializeToNode(typed, RunSavedDataJson.Options)! };
            document.SetRaw(ModId, Key, CreatePlayerEntry(players));
            return true;
        }

        public override void ImportLobbyContribution(
            RunSavedDataLobbySession session,
            ulong netId,
            bool isHostNetId,
            JsonObject entry)
        {
            if (entry["players"] is not JsonObject players ||
                !players.TryGetPropertyValue(PlayerKey(netId), out var node) ||
                !TryReadPlayerValue(node, out var value))
                return;

            session.SetPlayer(SlotKey, netId, value);
        }

        public override bool TryExportLobbyStaging(RunSavedDataLobbySession session, RunSavedDataDocument document)
        {
            if (!session.HasPlayers(SlotKey))
                return false;

            var players = new JsonObject();
            foreach (var (key, entries) in session.PlayerEntries())
            {
                if (key != SlotKey)
                    continue;

                foreach (var (netId, raw) in entries.OrderBy(pair => pair.Key))
                    if (raw is T typed)
                        players[PlayerKey(netId)] = JsonSerializer.SerializeToNode(typed, RunSavedDataJson.Options);
            }

            if (players.Count == 0)
                return false;

            document.SetRaw(ModId, Key, CreatePlayerEntry(players));
            return true;
        }

        public override void CommitLobbyStaging(RunSavedDataLobbySession session, RunState runState)
        {
            if (!session.HasPlayers(SlotKey))
                return;

            var values = new Dictionary<ulong, T>();
            foreach (var (key, players) in session.PlayerEntries())
            {
                if (key != SlotKey)
                    continue;

                foreach (var (netId, raw) in players)
                    if (raw is T typed)
                        values[netId] = typed;
            }

            if (values.Count > 0)
                RunSavedDataRuntime.GetBag(runState).Set(SlotKey, values, false);
        }

        private bool TryReadPlayerValue(JsonNode? node, out T value)
        {
            value = null!;

            var deserialized = node?.Deserialize<T>(RunSavedDataJson.Options);
            if (deserialized == null)
                return false;

            value = deserialized;
            return true;
        }

        private static RunState GetRunState(Player player)
        {
            if (player.RunState is RunState runState)
                return runState;

            throw new InvalidOperationException("Player does not belong to a concrete RunState.");
        }
    }
}
