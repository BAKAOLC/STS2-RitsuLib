using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;
using STS2RitsuLib.Utils.Persistence.Context;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data
{
    /// <summary>
    ///     Unified data store for all mod persistent data.
    ///     Uses key-based registration to avoid hardcoded per-data properties and methods.
    ///     所有 mod 持久化数据的统一数据存储。
    ///     使用基于键的注册，避免为每种数据硬编码属性和方法。
    /// </summary>
    public class ModDataStore
    {
        private static readonly Lock StoresLock = new();

        private static readonly Dictionary<string, ModDataStore> Stores =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, IRegisteredDataEntry> _entries =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Logger _logger;
        private readonly MigrationManager _migrationManager;
        private bool _profileEventsSubscribed;
        private int _registrationScopeDepth;
        private bool _registrationScopeInitializeProfileIfReady;

        private ModDataStore(string modId)
        {
            ModId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
            _jsonOptions = new()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                IncludeFields = false,
            };

            _migrationManager = new();
        }

        /// <summary>
        ///     Owning mod id for this store instance.
        ///     此存储实例所属的 mod ID。
        /// </summary>
        public string ModId { get; }

        internal static bool HasAnyProfileScopedEntries
        {
            get { return GetStoresSnapshot().Any(store => store.HasProfileScopedEntries); }
        }

        /// <summary>
        ///     True after every global-scoped entry has completed initialization and load.
        ///     所有全局作用域条目完成初始化和加载后为 true。
        /// </summary>
        public bool IsGlobalInitialized { get; private set; }

        /// <summary>
        ///     True after profile-scoped entries for the active profile are initialized.
        ///     当前活动档案的档案作用域条目初始化后为 true。
        /// </summary>
        public bool IsProfileInitialized { get; private set; }

        /// <summary>
        ///     Whether this store has at least one <see cref="SaveScope.Profile" /> registration.
        ///     此存储是否至少有一个 <see cref="SaveScope.Profile" /> 注册。
        /// </summary>
        public bool HasProfileScopedEntries => _entries.Values.Any(e => e.Scope == SaveScope.Profile);

        internal event Action<string>? EntryReloaded;

        /// <summary>
        ///     Defers eager initialization of newly registered entries until the scope is disposed.
        ///     将新注册条目的急切初始化延迟到作用域释放时执行。
        /// </summary>
        /// <param name="initializeProfileIfReady">
        ///     When true and profile data is already initialized, profile-scoped registrations initialize on scope end.
        ///     为 true 且档案数据已初始化时，档案作用域注册会在作用域结束时初始化。
        /// </param>
        public IDisposable BeginRegistrationScope(bool initializeProfileIfReady = true)
        {
            _registrationScopeDepth++;
            _registrationScopeInitializeProfileIfReady |= initializeProfileIfReady;
            return new RegistrationScope(this);
        }

        /// <summary>
        ///     Returns the process-wide store for <paramref name="modId" />.
        ///     返回 <paramref name="modId" /> 对应的进程级存储。
        /// </summary>
        public static ModDataStore For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (StoresLock)
            {
                if (Stores.TryGetValue(modId, out var store))
                    return store;

                store = new(modId);
                Stores[modId] = store;
                return store;
            }
        }

        internal static void InitializeAllProfileScoped()
        {
            foreach (var store in GetStoresSnapshot())
                store.InitializeProfileScoped();
        }

        internal static bool ReloadAllIfPathChanged()
        {
            return GetStoresSnapshot().Aggregate(false, (current, store) => current | store.ReloadIfPathChanged());
        }

        internal static void DeleteAllProfileData(int profileId)
        {
            foreach (var store in GetStoresSnapshot())
                ProfileManager.DeleteProfileData(profileId, store.ModId);
        }

        private static ModDataStore[] GetStoresSnapshot()
        {
            lock (StoresLock)
            {
                return [.. Stores.Values];
            }
        }

        /// <summary>
        ///     Initializes and loads every global-scoped entry that is not yet initialized (safe during early startup).
        ///     初始化并加载所有尚未初始化的全局作用域条目（可安全用于早期启动阶段）。
        /// </summary>
        public void InitializeGlobal()
        {
            foreach (var entry in _entries.Values.Where(e => e is { Scope: SaveScope.Global, IsInitialized: false }))
            {
                entry.Initialize(_jsonOptions, _migrationManager);
                entry.Load();
            }

            RefreshGlobalInitializationState();
        }

        /// <summary>
        ///     Initializes and loads profile-scoped entries once the profile path is valid (subscribes to profile changes).
        ///     在档案路径有效后初始化并加载档案作用域条目（同时订阅档案变更）。
        /// </summary>
        public void InitializeProfileScoped()
        {
            if (!IsGlobalInitialized)
                InitializeGlobal();

            ProfileManager.Instance.Initialize();
            if (!_profileEventsSubscribed)
            {
                ProfileManager.Instance.ProfileChanged += OnProfileChanged;
                _profileEventsSubscribed = true;
            }

            foreach (var entry in _entries.Values.Where(e =>
                         e is { IsInitialized: false, Scope: SaveScope.Profile }))
            {
                entry.Initialize(_jsonOptions, _migrationManager);
                entry.Load();
            }

            IsProfileInitialized = _entries.Values
                .Where(e => e.Scope == SaveScope.Profile)
                .All(e => e.IsInitialized);
        }

        /// <summary>
        ///     Registers a JSON-backed persistence slot identified by <paramref name="key" />.
        ///     注册一个由 JSON 支持、以 <c>key</c> 标识的持久化槽。
        /// </summary>
        /// <param name="key">
        ///     Logical key used with <see cref="Get{T}" />, <see cref="Modify{T}" />, and <see cref="Save" />.
        ///     与 <see cref="Get{T}" />、<see cref="Modify{T}" /> 和 <see cref="Save" /> 一起使用的逻辑键。
        /// </param>
        /// <param name="fileName">
        ///     File name segment passed to <see cref="ProfileManager" /> path resolution.
        ///     传递给 <see cref="ProfileManager" /> 路径解析的文件名片段。
        /// </param>
        /// <param name="scope">
        ///     Global or profile persistence scope.
        ///     全局或档案持久化作用域。
        /// </param>
        /// <param name="defaultFactory">
        ///     Factory for the in-memory default when no file exists.
        ///     文件不存在时用于创建内存默认值的工厂。
        /// </param>
        /// <param name="autoCreateIfMissing">
        ///     When true, creates the on-disk file if absent after first save.
        ///     为 true 时，首次保存后如果磁盘文件不存在则创建它。
        /// </param>
        /// <param name="migrationConfig">
        ///     Optional schema versioning configuration for migrations.
        ///     用于迁移的可选 schema 版本配置。
        /// </param>
        /// <param name="migrations">
        ///     Optional migration steps; requires <paramref name="migrationConfig" />.
        ///     可选迁移步骤；需要 <paramref name="migrationConfig" />。
        /// </param>
        public void Register<T>(
            string key,
            string fileName,
            SaveScope scope,
            Func<T>? defaultFactory = null,
            bool autoCreateIfMissing = false,
            ModDataMigrationConfig? migrationConfig = null,
            IEnumerable<IMigration>? migrations = null)
            where T : class, new()
        {
            if (_entries.ContainsKey(key))
                throw new InvalidOperationException($"Data key '{key}' is already registered.");

            ConfigureMigration<T>(migrationConfig, migrations);

            if (scope == SaveScope.InMemory)
            {
                var memory = new InMemoryDataEntry<T>(key, scope, defaultFactory ?? (() => new()));
                _entries[key] = memory;
                return;
            }

            var registration = new RegisteredDataEntry<T>(
                ModId,
                key,
                fileName,
                scope,
                defaultFactory ?? (() => new()),
                autoCreateIfMissing,
                _logger
            );

            _entries[key] = registration;
            ModCloudSyncPathRegistry.RegisterModDataSlot(ModId, fileName, scope);

            if (_registrationScopeDepth > 0)
                return;

            if (!IsGlobalInitialized && scope == SaveScope.Global) return;
            if (!IsProfileInitialized && scope == SaveScope.Profile) return;
            registration.Initialize(_jsonOptions, _migrationManager);
            registration.Load();
        }

        /// <summary>
        ///     Registers a JSON-backed persistence slot identified by <paramref name="key" /> using an explicit
        ///     <see cref="StorageContext" /> provider for path resolution.
        ///     注册一个由 JSON 支持、以 <paramref name="key" /> 标识的持久化槽，并使用显式
        ///     <see cref="StorageContext" /> 提供器解析路径。
        /// </summary>
        public void Register<T>(
            string key,
            string fileName,
            SaveScope scope,
            Func<StorageContext> contextProvider,
            Func<T>? defaultFactory = null,
            bool autoCreateIfMissing = false,
            ModDataMigrationConfig? migrationConfig = null,
            IEnumerable<IMigration>? migrations = null)
            where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(contextProvider);

            if (_entries.ContainsKey(key))
                throw new InvalidOperationException($"Data key '{key}' is already registered.");

            ConfigureMigration<T>(migrationConfig, migrations);

            if (scope == SaveScope.InMemory)
                throw new InvalidOperationException("SaveScope.InMemory does not support contextProvider overload.");

            var registration = new RegisteredDataEntry<T>(
                ModId,
                key,
                fileName,
                scope,
                defaultFactory ?? (() => new()),
                autoCreateIfMissing,
                _logger,
                contextProvider
            );

            _entries[key] = registration;
            ModCloudSyncPathRegistry.RegisterModDataSlot(ModId, fileName, scope);

            if (_registrationScopeDepth > 0)
                return;

            if (!IsGlobalInitialized && scope == SaveScope.Global) return;
            if (!IsProfileInitialized && scope == SaveScope.Profile) return;
            registration.Initialize(_jsonOptions, _migrationManager);
            registration.Load();
        }

        private void ConfigureMigration<T>(
            ModDataMigrationConfig? migrationConfig,
            IEnumerable<IMigration>? migrations)
            where T : class, new()
        {
            if (migrationConfig != null)
                _migrationManager.RegisterConfig<T>(
                    migrationConfig.CurrentDataVersion,
                    migrationConfig.MinimumSupportedDataVersion,
                    migrationConfig.SchemaVersionProperty
                );

            if (migrations == null)
                return;

            if (migrationConfig == null)
                throw new InvalidOperationException(
                    $"Migration config for type '{typeof(T).Name}' requires a current version.");

            foreach (var migration in migrations)
                _migrationManager.RegisterMigration<T>(migration);
        }

        /// <summary>
        ///     Returns the live instance for <paramref name="key" />.
        ///     Profile reloads may replace this root instance; use <see cref="CreateCache{T}" /> for cached access.
        ///     返回 <c>key</c> 对应的实时实例。
        ///     档案重新加载可能替换此根实例；缓存访问请使用 <see cref="CreateCache{T}" />。
        /// </summary>
        public T Get<T>(string key) where T : class, new()
        {
            var entry = GetEntry(key);
            return entry switch
            {
                RegisteredDataEntry<T> persisted => persisted.Data,
                InMemoryDataEntry<T> memory => memory.Data,
                _ => throw new InvalidOperationException(
                    $"Data key '{key}' is registered as '{entry.DataType.Name}', not '{typeof(T).Name}'."),
            };
        }

        /// <summary>
        ///     Creates a small cache wrapper that invalidates itself when this store reloads <paramref name="key" />.
        ///     创建一个小型缓存包装器，在此存储重新加载 <paramref name="key" /> 时自动失效。
        /// </summary>
        public ModDataStoreCache<T> CreateCache<T>(string key) where T : class, new()
        {
            return new(this, key);
        }

        /// <summary>
        ///     Mutates the instance for <paramref name="key" /> in place (persists via <see cref="Save" />).
        ///     原地修改 <paramref name="key" /> 对应的实例（通过 <see cref="Save" /> 持久化）。
        /// </summary>
        public void Modify<T>(string key, Action<T> modifier) where T : class, new()
        {
            var entry = GetEntry(key);
            switch (entry)
            {
                case RegisteredDataEntry<T> persisted:
                    persisted.Modify(modifier);
                    break;
                case InMemoryDataEntry<T> memory:
                    memory.Modify(modifier);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Data key '{key}' is registered as '{entry.DataType.Name}', not '{typeof(T).Name}'.");
            }
        }

        /// <summary>
        ///     Writes the entry for <paramref name="key" /> to disk.
        ///     将 <c>key</c> 对应的条目写入磁盘。
        /// </summary>
        public void Save(string key)
        {
            var entry = GetEntry(key);
            if (entry.Scope == SaveScope.InMemory)
                return;
            entry.Save();
        }

        /// <summary>
        ///     Whether a file already existed when the entry was first loaded.
        ///     条目首次加载时文件是否已经存在。
        /// </summary>
        public bool HasExistingData(string key)
        {
            return GetEntry(key).HadExistingData;
        }

        /// <summary>
        ///     Reloads entries whose resolved path changed (e.g. after profile switch).
        ///     重新加载解析路径发生变化的条目（例如档案切换后）。
        /// </summary>
        /// <returns>
        ///     True if any entry reloaded.
        ///     如果有任何条目被重新加载，则为 true。
        /// </returns>
        public bool ReloadIfPathChanged()
        {
            if (!IsGlobalInitialized) return false;

            var reloaded = false;
            foreach (var (key, entry) in _entries.Where(pair => pair.Value.IsInitialized))
                if (entry.ReloadIfPathChanged())
                {
                    reloaded = true;
                    OnEntryReloaded(key);
                }

            return reloaded;
        }

        /// <summary>
        ///     Persists every registered entry.
        ///     持久化所有已注册条目。
        /// </summary>
        public void SaveAll()
        {
            foreach (var entry in _entries.Values)
                entry.Save();
        }

        private void OnProfileChanged(int oldProfileId, int newProfileId)
        {
            if (!IsProfileInitialized) return;

            _logger.Info(
                $"[{ModId}] Profile changed from {oldProfileId} to {newProfileId}, handling data transition...");

            foreach (var (key, entry) in _entries.Where(pair => pair.Value.Scope == SaveScope.Profile))
            {
                entry.SaveToProfilePath(oldProfileId);
                entry.Load();
                OnEntryReloaded(key);
            }
        }

        private void OnEntryReloaded(string key)
        {
            EntryReloaded?.Invoke(key);
        }

        private IRegisteredDataEntry GetEntry(string key)
        {
            if (!_entries.TryGetValue(key, out var entry))
                throw new KeyNotFoundException($"Data key '{key}' is not registered.");

            if (entry is not { IsInitialized: false, Scope: SaveScope.Global }) return entry;
            entry.Initialize(_jsonOptions, _migrationManager);
            entry.Load();
            RefreshGlobalInitializationState();

            return entry;
        }

        private void RefreshGlobalInitializationState()
        {
            IsGlobalInitialized = _entries.Values
                .Where(entry => entry.Scope == SaveScope.Global)
                .All(entry => entry.IsInitialized);
        }

        private void EndRegistrationScope()
        {
            if (_registrationScopeDepth <= 0)
                throw new InvalidOperationException("Registration scope was disposed more times than created.");

            _registrationScopeDepth--;
            if (_registrationScopeDepth > 0)
                return;

            var initializeProfileIfReady = _registrationScopeInitializeProfileIfReady;
            _registrationScopeInitializeProfileIfReady = false;

            InitializeGlobal();

            if (initializeProfileIfReady && IsProfileInitialized)
                InitializeProfileScoped();
        }

        private RegisteredDataEntry<T> GetEntry<T>(string key) where T : class, new()
        {
            var entry = GetEntry(key);
            if (entry is not RegisteredDataEntry<T> typed)
                throw new InvalidOperationException(
                    $"Data key '{key}' is registered as '{entry.DataType.Name}', not '{typeof(T).Name}'.");

            return typed;
        }

        private InMemoryDataEntry<T> GetMemoryEntry<T>(string key) where T : class, new()
        {
            var entry = GetEntry(key);
            if (entry is not InMemoryDataEntry<T> typed)
                throw new InvalidOperationException(
                    $"Data key '{key}' is registered as '{entry.DataType.Name}', not '{typeof(T).Name}'.");

            return typed;
        }

        private sealed class RegistrationScope(ModDataStore store) : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                store.EndRegistrationScope();
            }
        }

        private sealed class InMemoryDataEntry<T>(string key, SaveScope scope, Func<T> defaultFactory)
            : IRegisteredDataEntry where T : class, new()
        {
            private T _data = defaultFactory();

            public T Data => IsInitialized
                ? _data
                : throw new InvalidOperationException(
                    $"Data entry '{key}' is not initialized.");

            public SaveScope Scope { get; } = scope;
            public Type DataType => typeof(T);
            public bool HadExistingData => false;
            public bool IsInitialized { get; private set; }

            public void Initialize(JsonSerializerOptions jsonOptions, MigrationManager migrationManager)
            {
                if (IsInitialized) return;
                _data = defaultFactory();
                IsInitialized = true;
            }

            public void Load()
            {
                if (!IsInitialized)
                    throw new InvalidOperationException($"Data entry '{key}' is not initialized.");
            }

            public void Save()
            {
                // no-op (in-memory)
            }

            public void SaveToProfilePath(int profileId)
            {
                // no-op (in-memory)
            }

            public bool ReloadIfPathChanged()
            {
                return false;
            }

            public void Modify(Action<T> modifier)
            {
                if (!IsInitialized)
                    throw new InvalidOperationException($"Data entry '{key}' is not initialized.");

                modifier(_data);
            }
        }

        private interface IRegisteredDataEntry
        {
            SaveScope Scope { get; }
            Type DataType { get; }
            bool HadExistingData { get; }
            bool IsInitialized { get; }
            void Initialize(JsonSerializerOptions jsonOptions, MigrationManager migrationManager);
            void Load();
            void Save();
            void SaveToProfilePath(int profileId);
            bool ReloadIfPathChanged();
        }

        private sealed class RegisteredDataEntry<T>(
            string modId,
            string key,
            string fileName,
            SaveScope scope,
            Func<T> defaultFactory,
            bool autoCreateIfMissing,
            Logger logger,
            Func<StorageContext>? contextProvider = null)
            : IRegisteredDataEntry where T : class, new()
        {
            private PersistentDataEntry<T>? _entry;
            private string? _lastLoadedPath;

            public T Data => _entry?.Data ?? throw new InvalidOperationException(
                $"Data entry '{key}' is not initialized.");

            public SaveScope Scope { get; } = scope;
            public Type DataType => typeof(T);
            public bool HadExistingData { get; private set; }
            public bool IsInitialized => _entry != null;

            public void Initialize(JsonSerializerOptions jsonOptions, MigrationManager migrationManager)
            {
                if (_entry != null) return;

                _entry = new(
                    modId,
                    fileName,
                    Scope,
                    defaultFactory(),
                    jsonOptions,
                    migrationManager,
                    autoCreateIfMissing,
                    contextProvider
                );
            }

            public void Load()
            {
                if (_entry == null)
                    throw new InvalidOperationException($"Data entry '{key}' is not initialized.");

                var currentPath = _entry.FilePath;
                _lastLoadedPath = currentPath;
                HadExistingData = FileOperations.FileExists(currentPath);
                _entry.Load();
            }

            public bool ReloadIfPathChanged()
            {
                if (_entry == null)
                    throw new InvalidOperationException($"Data entry '{key}' is not initialized.");

                var currentPath = _entry.FilePath;
                if (string.Equals(_lastLoadedPath, currentPath, StringComparison.Ordinal))
                    return false;

                logger.Info(
                    $"[{modId}] Data path changed for '{key}': '{_lastLoadedPath ?? "<none>"}' -> '{currentPath}', reloading");
                Load();
                return true;
            }

            public void Save()
            {
                _entry?.Save();
            }

            public void SaveToProfilePath(int profileId)
            {
                if (_entry == null || Scope != SaveScope.Profile) return;

                var oldPath = ProfileManager.GetFilePath(fileName, Scope, profileId, modId);
                _entry.SaveTo(oldPath);
            }

            public void Modify(Action<T> modifier)
            {
                if (_entry == null)
                    throw new InvalidOperationException($"Data entry '{key}' is not initialized.");

                _entry.Modify(modifier);
            }
        }
    }
}
