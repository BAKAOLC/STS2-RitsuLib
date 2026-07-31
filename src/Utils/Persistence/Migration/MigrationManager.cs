using System.Text.Json;
using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Persistence.Migration
{
    /// <summary>
    ///     <para xml:lang="en">Coordinates schema-version migrations for persisted data types.</para>
    ///     <para xml:lang="zh-CN">协调持久化数据类型的架构版本迁移。</para>
    /// </summary>
    public class MigrationManager
    {
        private readonly Dictionary<Type, MigrationConfig> _configs = new();
        private readonly Dictionary<Type, List<IMigration>> _migrations = new();

        /// <summary>
        ///     <para xml:lang="en">Registers migration configuration for a data type.</para>
        ///     <para xml:lang="zh-CN">为数据类型注册迁移配置。</para>
        /// </summary>
        public void RegisterConfig<T>(int currentVersion, int minimumSupportedVersion,
            string schemaVersionProperty = ModDataVersion.SchemaVersionProperty)
        {
            _configs[typeof(T)] = new()
            {
                CurrentVersion = currentVersion,
                MinimumSupportedVersion = minimumSupportedVersion,
                SchemaVersionProperty = schemaVersionProperty,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a migration for a data type.</para>
        ///     <para xml:lang="zh-CN">为数据类型注册迁移。</para>
        /// </summary>
        public void RegisterMigration<T>(IMigration migration)
        {
            var type = typeof(T);
            if (!_migrations.ContainsKey(type))
                _migrations[type] = [];

            _migrations[type].Add(migration);
            _migrations[type].Sort((a, b) =>
            {
                var c = a.FromVersion.CompareTo(b.FromVersion);
                return c != 0 ? c : a.ToVersion.CompareTo(b.ToVersion);
            });
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to deserialize JSON data and migrate it to the configured current version.</para>
        ///     <para xml:lang="zh-CN">尝试反序列化 JSON 数据，并将其迁移到配置的当前版本。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">Result containing the deserialized, optionally migrated data or error information.</para>
        ///     <para xml:lang="zh-CN">包含反序列化后（可能已迁移）的数据或错误信息的结果。</para>
        /// </returns>
        public MigrationResult<T> Migrate<T>(string jsonContent, JsonSerializerOptions? options = null)
            where T : class, new()
        {
            var type = typeof(T);

            if (!_configs.TryGetValue(type, out var config))
                return DeserializeWithoutMigration<T>(jsonContent, options);

            try
            {
                var jsonNode = JsonNode.Parse(jsonContent);
                if (jsonNode is not JsonObject jsonObject)
                    return new()
                    {
                        Success = false,
                        ErrorMessage = "Invalid JSON: root must be an object",
                    };

                var version = GetVersion(jsonObject, config.SchemaVersionProperty);

                if (version < config.MinimumSupportedVersion)
                    return new()
                    {
                        Success = false,
                        ErrorMessage =
                            $"Data version {version} is below minimum supported version {config.MinimumSupportedVersion}",
                        RequiresRecovery = true,
                    };

                if (version > config.CurrentVersion)
                    return new()
                    {
                        Success = false,
                        ErrorMessage = $"Data version {version} is newer than current version {config.CurrentVersion}",
                    };

                var migrations = _migrations.TryGetValue(type, out var registeredMigrations)
                    ? registeredMigrations
                    : [];
                if (!TryBuildShortestMigrationPath(
                        version,
                        config.CurrentVersion,
                        migrations,
                        out var plan))
                    return new()
                    {
                        Success = false,
                        ErrorMessage =
                            $"No migration path from data version {version} to current version {config.CurrentVersion} for {type.Name}.",
                    };

                for (var i = 0; i < plan.Count; i++)
                {
                    var migration = plan[i];
                    RitsuLibFramework.Logger.Info(
                        $"Applying migration {migration.FromVersion} -> {migration.ToVersion} for {type.Name} (shortest path: step {i + 1}/{plan.Count})");

                    if (!migration.Migrate(jsonObject))
                        return new()
                        {
                            Success = false,
                            ErrorMessage =
                                $"Migration {migration.FromVersion} -> {migration.ToVersion} failed",
                        };

                    version = migration.ToVersion;
                    SetVersion(jsonObject, config.SchemaVersionProperty, version);
                }

                var migratedJson = jsonObject.ToJsonString();
                var data = JsonSerializer.Deserialize<T>(migratedJson, options);

                if (data == null)
                    return new()
                    {
                        Success = false,
                        ErrorMessage = "Deserialization resulted in null",
                    };

                return new()
                {
                    Success = true,
                    Data = data,
                    WasMigrated = version != GetVersion(JsonNode.Parse(jsonContent) as JsonObject,
                        config.SchemaVersionProperty),
                    FinalVersion = version,
                };
            }
            catch (JsonException ex)
            {
                return new()
                {
                    Success = false,
                    ErrorMessage = $"JSON parsing error: {ex.Message}",
                    RequiresRecovery = true,
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    Success = false,
                    ErrorMessage = $"Migration error: {ex.Message}",
                };
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the configured current schema version for a data type.</para>
        ///     <para xml:lang="zh-CN">获取为数据类型配置的当前架构版本。</para>
        /// </summary>
        public int GetCurrentVersion<T>()
        {
            return _configs.TryGetValue(typeof(T), out var config) ? config.CurrentVersion : 0;
        }

        private static MigrationResult<T> DeserializeWithoutMigration<T>(string jsonContent,
            JsonSerializerOptions? options)
            where T : class, new()
        {
            try
            {
                var data = JsonSerializer.Deserialize<T>(jsonContent, options);
                return data == null
                    ? new()
                    {
                        Success = false,
                        ErrorMessage = "Deserialization resulted in null",
                    }
                    : new()
                    {
                        Success = true,
                        Data = data,
                    };
            }
            catch (JsonException ex)
            {
                return new()
                {
                    Success = false,
                    ErrorMessage = $"JSON parsing error: {ex.Message}",
                    RequiresRecovery = true,
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    Success = false,
                    ErrorMessage = $"Deserialization error: {ex.Message}",
                };
            }
        }

        private static int GetVersion(JsonObject? obj, string propertyName)
        {
            if (obj == null) return 0;
            return obj.TryGetPropertyValue(propertyName, out var versionNode) && versionNode != null
                ? versionNode.GetValue<int>()
                : 0;
        }

        private static void SetVersion(JsonObject obj, string propertyName, int version)
        {
            obj[propertyName] = version;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Finds a shortest migration path from <paramref name="startVersion" /> to
        ///         <paramref name="targetVersion" /> by breadth-first search. A migration may run when the current version is in
        ///         <c>[FromVersion, ToVersion)</c>, and migrations that overshoot the target are ignored. Equally short paths
        ///         follow the order of <paramref name="migrations" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过广度优先搜索查找从 <paramref name="startVersion" /> 到 <paramref name="targetVersion" />
        ///         的最短迁移路径。当前版本位于 <c>[FromVersion, ToVersion)</c> 时迁移可执行，超过目标版本的迁移会被忽略。长度相同的路径按 <paramref name="migrations" />
        ///         中的顺序选择。
        ///     </para>
        /// </summary>
        internal static bool TryBuildShortestMigrationPath(
            int startVersion,
            int targetVersion,
            List<IMigration> migrations,
            out List<IMigration> path)
        {
            path = [];
            if (startVersion == targetVersion)
                return true;

            var queue = new Queue<int>();
            var visited = new HashSet<int>();
            var predecessor = new Dictionary<int, (int PrevVersion, IMigration Via)>();

            queue.Enqueue(startVersion);
            visited.Add(startVersion);

            var found = false;

            while (queue.Count > 0 && !found)
            {
                var v = queue.Dequeue();
                foreach (var m in migrations)
                {
                    if (v < m.FromVersion || v >= m.ToVersion)
                        continue;

                    var next = m.ToVersion;
                    if (next > targetVersion)
                        continue;

                    if (!visited.Add(next))
                        continue;

                    predecessor[next] = (v, m);
                    if (next == targetVersion)
                    {
                        found = true;
                        break;
                    }

                    queue.Enqueue(next);
                }
            }

            if (!found)
                return false;

            path = [];
            var cur = targetVersion;
            while (cur != startVersion)
            {
                var (prev, via) = predecessor[cur];
                path.Add(via);
                cur = prev;
            }

            path.Reverse();
            return true;
        }

        private class MigrationConfig
        {
            public int CurrentVersion { get; init; }
            public int MinimumSupportedVersion { get; init; }
            public string SchemaVersionProperty { get; init; } = ModDataVersion.SchemaVersionProperty;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Outcome of a JSON deserialization and optional migration operation.</para>
    ///     <para xml:lang="zh-CN">JSON 反序列化及可选迁移操作的结果。</para>
    /// </summary>
    public class MigrationResult<T>
    {
        /// <summary>
        ///     <para xml:lang="en"><see langword="true" /> when JSON deserialization and any required migrations succeeded.</para>
        ///     <para xml:lang="zh-CN">JSON 反序列化及所有必要迁移均成功时为 <see langword="true" />。</para>
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Deserialized, optionally migrated instance when <see cref="Success" /> is
        ///         <see langword="true" />.
        ///     </para>
        ///     <para xml:lang="zh-CN"><see cref="Success" /> 为 <see langword="true" /> 时反序列化得到的实例；该实例可能已经迁移。</para>
        /// </summary>
        public T? Data { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Failure explanation when <see cref="Success" /> is <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN"><see cref="Success" /> 为 <see langword="false" /> 时的失败说明。</para>
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        ///     <para xml:lang="en"><see langword="true" /> when at least one migration step ran.</para>
        ///     <para xml:lang="zh-CN">至少执行了一个迁移步骤时为 <see langword="true" />。</para>
        /// </summary>
        public bool WasMigrated { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Final schema version reported for a configured migration operation.</para>
        ///     <para xml:lang="zh-CN">已配置迁移操作所报告的最终架构版本。</para>
        /// </summary>
        public int FinalVersion { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when malformed JSON or an obsolete schema version indicates that the
        ///         stored file should be quarantined or reset.
        ///     </para>
        ///     <para xml:lang="zh-CN">JSON 格式错误或架构版本过旧，表明应隔离或重置存储文件时为 <see langword="true" />。</para>
        /// </summary>
        public bool RequiresRecovery { get; init; }
    }
}
