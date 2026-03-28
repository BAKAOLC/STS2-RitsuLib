using System.Text.Json;
using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Persistence.Migration
{
    /// <summary>
    ///     Manages data migrations between schema versions
    /// </summary>
    public class MigrationManager
    {
        private readonly Dictionary<Type, MigrationConfig> _configs = new();
        private readonly Dictionary<Type, List<IMigration>> _migrations = new();

        /// <summary>
        ///     Register migration configuration for a data type
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
        /// </summary>
        public void RegisterMigration<T>(IMigration migration)
        {
            var type = typeof(T);
            if (!_migrations.ContainsKey(type))
                _migrations[type] = [];

            _migrations[type].Add(migration);
            _migrations[type].Sort((a, b) => a.FromVersion.CompareTo(b.FromVersion));
        }

        /// <summary>
        ///     Attempt to migrate JSON data to the current version
        /// </summary>
        /// <returns>Migration result with migrated data or error information</returns>
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

                if (_migrations.TryGetValue(type, out var migrations))
                    foreach (var migration in migrations.Where(migration =>
                                 version >= migration.FromVersion && version < migration.ToVersion))
                    {
                        RitsuLibFramework.Logger.Info(
                            $"Applying migration {migration.FromVersion} -> {migration.ToVersion} for {type.Name}");

                        if (!migration.Migrate(jsonObject))
                            return new()
                            {
                                Success = false,
                                ErrorMessage = $"Migration {migration.FromVersion} -> {migration.ToVersion} failed",
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
        ///     Get the current version for a data type
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

        private class MigrationConfig
        {
            public int CurrentVersion { get; init; }
            public int MinimumSupportedVersion { get; init; }
            public string SchemaVersionProperty { get; init; } = ModDataVersion.SchemaVersionProperty;
        }
    }

    /// <summary>
    ///     Result of a migration operation
    /// </summary>
    public class MigrationResult<T>
    {
        /// <summary>
        ///     True when JSON was parsed and optional migrations succeeded.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        ///     Migrated instance when <see cref="Success" /> is true.
        /// </summary>
        public T? Data { get; init; }

        /// <summary>
        ///     Failure explanation when <see cref="Success" /> is false.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        ///     True when at least one migration step ran.
        /// </summary>
        public bool WasMigrated { get; init; }

        /// <summary>
        ///     Schema version after migration (or the detected version when no migrations ran).
        /// </summary>
        public int FinalVersion { get; init; }

        /// <summary>
        ///     True when the on-disk file should be quarantined or reset (corrupt or unsupported version).
        /// </summary>
        public bool RequiresRecovery { get; init; }
    }
}
