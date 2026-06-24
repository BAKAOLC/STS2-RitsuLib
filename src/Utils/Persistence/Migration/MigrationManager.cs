using System.Text.Json;
using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Persistence.Migration
{
    /// <summary>
    ///     Manages data migrations between schema versions
    ///     Manages 数据 迁移s between schema 版本s
    /// </summary>
    public class MigrationManager
    {
        private readonly Dictionary<Type, MigrationConfig> _configs = new();
        private readonly Dictionary<Type, List<IMigration>> _migrations = new();

        /// <summary>
        ///     Register migration configuration for a data type
        ///     为数据类型注册迁移配置。
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
        ///     Register a migration for a data type
        ///     为数据类型注册迁移。
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
        ///     Attempt to migrate JSON data to the current version
        ///     尝试将 JSON 数据迁移到当前版本。
        /// </summary>
        /// <returns>
        ///     Migration result with migrated data or error information
        ///     包含迁移后数据或错误信息的迁移结果。
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

                if (_migrations.TryGetValue(type, out var migrations) && migrations.Count > 0)
                {
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
        ///     Get the current version for a data type
        ///     获取数据类型的当前版本。
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
        ///     Breadth-first search on version states: from current version <paramref name="startVersion" />, a
        ///     registered migration applies when the version lies in <c>[FromVersion, ToVersion)</c>, advancing the
        ///     state to <c>ToVersion</c>.
        ///     Edges whose <c>ToVersion</c> exceeds <paramref name="targetVersion" /> are skipped so the plan ends at
        ///     the configured current schema. The returned path uses the minimum number of migration steps; ties are
        ///     broken by iteration order (registration order, then sort by FromVersion, ToVersion).
        ///     对版本状态执行广度优先搜索：从当前版本 <paramref name="startVersion" /> 开始，
        ///     当版本位于 <c>[FromVersion, ToVersion)</c> 时，已注册迁移适用，并将
        ///     状态推进到 <c>ToVersion</c>。
        ///     会跳过 <c>ToVersion</c> 超过 <paramref name="targetVersion" /> 的边，使计划结束于
        ///     配置的当前 schema。返回路径使用最少迁移步骤数；平局时
        ///     按迭代顺序打破（注册顺序，然后按 FromVersion、ToVersion 排序）。
        /// </summary>
        private static bool TryBuildShortestMigrationPath(
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
    ///     Result of a migration operation
    ///     迁移操作的结果。
    /// </summary>
    public class MigrationResult<T>
    {
        /// <summary>
        ///     True when JSON was parsed and optional migrations succeeded.
        ///     当 JSON 已解析且可选迁移成功时为 true。
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        ///     Migrated instance when <see cref="Success" /> is true.
        ///     <see cref="Success" /> 为 true 时的迁移后实例。
        /// </summary>
        public T? Data { get; init; }

        /// <summary>
        ///     Failure explanation when <see cref="Success" /> is false.
        ///     <see cref="Success" /> 为 false 时的失败说明。
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        ///     True when at least one migration step ran.
        ///     当 at least one 迁移 step ran.
        /// </summary>
        public bool WasMigrated { get; init; }

        /// <summary>
        ///     Schema version after migration (or the detected version when no migrations ran).
        ///     迁移后的 schema 版本（未执行迁移时为检测到的版本）。
        /// </summary>
        public int FinalVersion { get; init; }

        /// <summary>
        ///     True when the on-disk file should be quarantined or reset (corrupt or unsupported version).
        ///     当磁盘文件应被隔离或重置时为 true（损坏或版本不受支持）。
        /// </summary>
        public bool RequiresRecovery { get; init; }
    }
}
