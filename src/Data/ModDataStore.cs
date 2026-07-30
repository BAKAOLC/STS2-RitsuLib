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
    ///     <para xml:lang="en">Provides key-based registration and access for a mod's persistent and in-memory data.</para>
    ///     <para xml:lang="zh-CN">为模组的持久化数据和内存数据提供基于键的注册与访问。</para>
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
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the mod that owns this store.</para>
        ///     <para xml:lang="zh-CN">获取此存储所属的模组 ID。</para>
        /// </summary>
        public string ModId { get; }

        internal static bool HasAnyProfileScopedEntries
        {
            get { return GetStoresSnapshot().Any(store => store.HasProfileScopedEntries); }
        }

        /// <summary>
        ///     <para xml:lang="en">Gets whether every global entry has been initialized and loaded.</para>
        ///     <para xml:lang="zh-CN">获取所有全局条目是否均已初始化并加载。</para>
        /// </summary>
        public bool IsGlobalInitialized { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether entries for the active profile have been initialized.</para>
        ///     <para xml:lang="zh-CN">获取当前档案的条目是否已初始化。</para>
        /// </summary>
        public bool IsProfileInitialized { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether this store contains any <see cref="SaveScope.Profile" /> registrations.</para>
        ///     <para xml:lang="zh-CN">获取此存储是否包含任何 <see cref="SaveScope.Profile" /> 注册。</para>
        /// </summary>
        public bool HasProfileScopedEntries => _entries.Values.Any(e => e.Scope == SaveScope.Profile);

        internal event Action<string>? EntryReloaded;

        /// <summary>
        ///     <para xml:lang="en">Defers eager initialization of newly registered entries until the returned scope is disposed.</para>
        ///     <para xml:lang="zh-CN">将新注册条目的立即初始化推迟到返回的作用域被释放时。</para>
        /// </summary>
        /// <param name="initializeProfileIfReady">
        ///     <para xml:lang="en">Whether new profile entries should initialize at scope end when profile data is already ready.</para>
        ///     <para xml:lang="zh-CN">档案数据已就绪时，是否在作用域结束时初始化新的档案条目。</para>
        /// </param>
        public IDisposable BeginRegistrationScope(bool initializeProfileIfReady = true)
        {
            _registrationScopeDepth++;
            _registrationScopeInitializeProfileIfReady |= initializeProfileIfReady;
            return new RegistrationScope(this);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the process-wide store for <paramref name="modId" />.</para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="modId" /> 对应的进程级存储。</para>
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
        ///     <para xml:lang="en">Initializes and loads every uninitialized global entry.</para>
        ///     <para xml:lang="zh-CN">初始化并加载所有尚未初始化的全局条目。</para>
        /// </summary>
        public void InitializeGlobal()
        {
            foreach (var entry in _entries.Values.Where(e => e is { Scope: SaveScope.Global, IsInitialized: false }))
            {
                entry.Initialize(_jsonOptions);
                entry.Load();
            }

            RefreshGlobalInitializationState();
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes and loads profile entries, then subscribes to profile changes.</para>
        ///     <para xml:lang="zh-CN">初始化并加载档案条目，然后订阅档案变更事件。</para>
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
                entry.Initialize(_jsonOptions);
                entry.Load();
            }

            IsProfileInitialized = _entries.Values
                .Where(e => e.Scope == SaveScope.Profile)
                .All(e => e.IsInitialized);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a JSON-backed data slot identified by <paramref name="key" />.</para>
        ///     <para xml:lang="zh-CN">注册由 JSON 支持、以 <paramref name="key" /> 标识的数据槽。</para>
        /// </summary>
        /// <param name="key">
        ///     <para xml:lang="en">The logical key used to access the data slot.</para>
        ///     <para xml:lang="zh-CN">用于访问数据槽的逻辑键。</para>
        /// </param>
        /// <param name="fileName">
        ///     <para xml:lang="en">The file-name segment supplied to <see cref="ProfileManager" />.</para>
        ///     <para xml:lang="zh-CN">传递给 <see cref="ProfileManager" /> 的文件名片段。</para>
        /// </param>
        /// <param name="scope">
        ///     <para xml:lang="en">The data slot's save scope.</para>
        ///     <para xml:lang="zh-CN">数据槽的保存作用域。</para>
        /// </param>
        /// <param name="defaultFactory">
        ///     <para xml:lang="en">An optional factory for the default value when no file exists.</para>
        ///     <para xml:lang="zh-CN">文件不存在时，用于创建默认值的可选工厂。</para>
        /// </param>
        /// <param name="autoCreateIfMissing">
        ///     <para xml:lang="en">Whether a missing file should be created automatically.</para>
        ///     <para xml:lang="zh-CN">文件缺失时是否自动创建。</para>
        /// </param>
        /// <param name="migrationConfig">
        ///     <para xml:lang="en">Optional schema-version configuration for migrations.</para>
        ///     <para xml:lang="zh-CN">用于迁移的可选架构版本配置。</para>
        /// </param>
        /// <param name="migrations">
        ///     <para xml:lang="en">Optional migration steps; requires <paramref name="migrationConfig" />.</para>
        ///     <para xml:lang="zh-CN">可选的迁移步骤；需要同时提供 <paramref name="migrationConfig" />。</para>
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
            Register(key, fileName, scope, true, defaultFactory, autoCreateIfMissing, migrationConfig, migrations);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a JSON-backed data slot with an explicit cloud-sync policy.</para>
        ///     <para xml:lang="zh-CN">注册由 JSON 支持的数据槽，并显式指定云同步策略。</para>
        /// </summary>
        /// <param name="key">
        ///     <para xml:lang="en">The logical key used to access the data slot.</para>
        ///     <para xml:lang="zh-CN">用于访问数据槽的逻辑键。</para>
        /// </param>
        /// <param name="fileName">
        ///     <para xml:lang="en">The file-name segment supplied to <see cref="ProfileManager" />.</para>
        ///     <para xml:lang="zh-CN">传递给 <see cref="ProfileManager" /> 的文件名片段。</para>
        /// </param>
        /// <param name="scope">
        ///     <para xml:lang="en">The data slot's save scope.</para>
        ///     <para xml:lang="zh-CN">数据槽的保存作用域。</para>
        /// </param>
        /// <param name="defaultFactory">
        ///     <para xml:lang="en">An optional factory for the default value when no file exists.</para>
        ///     <para xml:lang="zh-CN">文件不存在时，用于创建默认值的可选工厂。</para>
        /// </param>
        /// <param name="autoCreateIfMissing">
        ///     <para xml:lang="en">Whether a missing file should be created automatically.</para>
        ///     <para xml:lang="zh-CN">文件缺失时是否自动创建。</para>
        /// </param>
        /// <param name="migrationConfig">
        ///     <para xml:lang="en">Optional schema-version configuration for migrations.</para>
        ///     <para xml:lang="zh-CN">用于迁移的可选架构版本配置。</para>
        /// </param>
        /// <param name="migrations">
        ///     <para xml:lang="en">Optional migration steps; requires <paramref name="migrationConfig" />.</para>
        ///     <para xml:lang="zh-CN">可选的迁移步骤；需要同时提供 <paramref name="migrationConfig" />。</para>
        /// </param>
        /// <param name="syncToCloud">
        ///     <para xml:lang="en">Whether this persisted slot participates in RitsuLib's mod-data cloud sync.</para>
        ///     <para xml:lang="zh-CN">此持久化数据槽是否参与 RitsuLib 的模组数据云同步。</para>
        /// </param>
        public void Register<T>(
            string key,
            string fileName,
            SaveScope scope,
            bool syncToCloud,
            Func<T>? defaultFactory = null,
            bool autoCreateIfMissing = false,
            ModDataMigrationConfig? migrationConfig = null,
            IEnumerable<IMigration>? migrations = null)
            where T : class, new()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            if (_entries.ContainsKey(key))
                throw new InvalidOperationException($"Data key '{key}' is already registered.");

            var migrationManager = CreateMigrationManager<T>(migrationConfig, migrations);

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
                migrationManager,
                _logger
            );

            _entries[key] = registration;
            if (syncToCloud)
                ModCloudSyncPathRegistry.RegisterModDataSlot(ModId, fileName, scope);

            if (_registrationScopeDepth > 0)
                return;

            if (!IsGlobalInitialized && scope == SaveScope.Global) return;
            if (!IsProfileInitialized && scope == SaveScope.Profile) return;
            registration.Initialize(_jsonOptions);
            registration.Load();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a JSON-backed data slot using an explicit <see cref="StorageContext" /> provider for path
        ///         resolution.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册由 JSON 支持的数据槽，并使用显式的 <see cref="StorageContext" /> 提供器解析路径。
        ///     </para>
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
            Register(key, fileName, scope, contextProvider, true, defaultFactory, autoCreateIfMissing, migrationConfig,
                migrations);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a JSON-backed data slot using an explicit <see cref="StorageContext" /> provider and
        ///         cloud-sync policy.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册由 JSON 支持的数据槽，并使用显式的 <see cref="StorageContext" /> 提供器和云同步策略。
        ///     </para>
        /// </summary>
        /// <param name="key">
        ///     <para xml:lang="en">The logical key used to access the data slot.</para>
        ///     <para xml:lang="zh-CN">用于访问数据槽的逻辑键。</para>
        /// </param>
        /// <param name="fileName">
        ///     <para xml:lang="en">The file-name segment supplied to path resolution.</para>
        ///     <para xml:lang="zh-CN">传递给路径解析流程的文件名片段。</para>
        /// </param>
        /// <param name="scope">
        ///     <para xml:lang="en">The data slot's save scope.</para>
        ///     <para xml:lang="zh-CN">数据槽的保存作用域。</para>
        /// </param>
        /// <param name="contextProvider">
        ///     <para xml:lang="en">The provider used to resolve the current storage context.</para>
        ///     <para xml:lang="zh-CN">用于解析当前存储上下文的提供器。</para>
        /// </param>
        /// <param name="syncToCloud">
        ///     <para xml:lang="en">Whether this persisted slot participates in RitsuLib's mod-data cloud sync.</para>
        ///     <para xml:lang="zh-CN">此持久化数据槽是否参与 RitsuLib 的模组数据云同步。</para>
        /// </param>
        /// <param name="defaultFactory">
        ///     <para xml:lang="en">An optional factory for the default value when no file exists.</para>
        ///     <para xml:lang="zh-CN">文件不存在时，用于创建默认值的可选工厂。</para>
        /// </param>
        /// <param name="autoCreateIfMissing">
        ///     <para xml:lang="en">Whether a missing file should be created automatically.</para>
        ///     <para xml:lang="zh-CN">文件缺失时是否自动创建。</para>
        /// </param>
        /// <param name="migrationConfig">
        ///     <para xml:lang="en">Optional schema-version configuration for migrations.</para>
        ///     <para xml:lang="zh-CN">用于迁移的可选架构版本配置。</para>
        /// </param>
        /// <param name="migrations">
        ///     <para xml:lang="en">Optional migration steps; requires <paramref name="migrationConfig" />.</para>
        ///     <para xml:lang="zh-CN">可选的迁移步骤；需要同时提供 <paramref name="migrationConfig" />。</para>
        /// </param>
        public void Register<T>(
            string key,
            string fileName,
            SaveScope scope,
            Func<StorageContext> contextProvider,
            bool syncToCloud,
            Func<T>? defaultFactory = null,
            bool autoCreateIfMissing = false,
            ModDataMigrationConfig? migrationConfig = null,
            IEnumerable<IMigration>? migrations = null)
            where T : class, new()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            ArgumentNullException.ThrowIfNull(contextProvider);

            if (_entries.ContainsKey(key))
                throw new InvalidOperationException($"Data key '{key}' is already registered.");

            var migrationManager = CreateMigrationManager<T>(migrationConfig, migrations);

            if (scope == SaveScope.InMemory)
                throw new InvalidOperationException("SaveScope.InMemory does not support contextProvider overload.");

            var registration = new RegisteredDataEntry<T>(
                ModId,
                key,
                fileName,
                scope,
                defaultFactory ?? (() => new()),
                autoCreateIfMissing,
                migrationManager,
                _logger,
                contextProvider
            );

            _entries[key] = registration;
            if (syncToCloud)
                ModCloudSyncPathRegistry.RegisterModDataSlot(ModId, fileName, scope);

            if (_registrationScopeDepth > 0)
                return;

            if (!IsGlobalInitialized && scope == SaveScope.Global) return;
            if (!IsProfileInitialized && scope == SaveScope.Profile) return;
            registration.Initialize(_jsonOptions);
            registration.Load();
        }

        private static MigrationManager CreateMigrationManager<T>(
            ModDataMigrationConfig? migrationConfig,
            IEnumerable<IMigration>? migrations)
            where T : class, new()
        {
            var migrationManager = new MigrationManager();

            if (migrationConfig != null)
                migrationManager.RegisterConfig<T>(
                    migrationConfig.CurrentDataVersion,
                    migrationConfig.MinimumSupportedDataVersion,
                    migrationConfig.SchemaVersionProperty
                );

            if (migrations == null)
                return migrationManager;

            if (migrationConfig == null)
                throw new InvalidOperationException(
                    $"Migration config for type '{typeof(T).Name}' requires a current version.");

            foreach (var migration in migrations)
                migrationManager.RegisterMigration<T>(migration);

            return migrationManager;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the live instance for <paramref name="key" />. Profile reloads can replace the root instance;
        ///         use <see cref="CreateCache{T}" /> for cache-aware access.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="key" /> 对应的实时实例。重新加载档案可能替换其根实例；需要缓存感知的访问时，
        ///         请使用 <see cref="CreateCache{T}" />。
        ///     </para>
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
        ///     <para xml:lang="en">Creates a cache wrapper that invalidates itself when <paramref name="key" /> is reloaded.</para>
        ///     <para xml:lang="zh-CN">创建缓存包装器，并在重新加载 <paramref name="key" /> 时自动使其失效。</para>
        /// </summary>
        public ModDataStoreCache<T> CreateCache<T>(string key) where T : class, new()
        {
            return new(this, key);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Mutates the instance for <paramref name="key" /> in place; call <see cref="Save" /> to persist
        ///         it.
        ///     </para>
        ///     <para xml:lang="zh-CN">原地修改 <paramref name="key" /> 对应的实例；调用 <see cref="Save" /> 可将其持久化。</para>
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
        ///     <para xml:lang="en">Writes the entry for <paramref name="key" /> to disk.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="key" /> 对应的条目写入磁盘。</para>
        /// </summary>
        public void Save(string key)
        {
            var entry = GetEntry(key);
            if (entry.Scope == SaveScope.InMemory)
                return;
            entry.Save();
        }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the entry's file existed when it was first loaded.</para>
        ///     <para xml:lang="zh-CN">获取首次加载条目时，其文件是否已经存在。</para>
        /// </summary>
        public bool HasExistingData(string key)
        {
            return GetEntry(key).HadExistingData;
        }

        /// <summary>
        ///     <para xml:lang="en">Reloads entries whose resolved path has changed, such as after a profile switch.</para>
        ///     <para xml:lang="zh-CN">重新加载解析路径已发生变化的条目，例如切换档案后的条目。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if any entry was reloaded; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">如果重新加载了任何条目，则为 <see langword="true" />；否则为 <see langword="false" />。</para>
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
        ///     <para xml:lang="en">Persists every registered entry.</para>
        ///     <para xml:lang="zh-CN">持久化所有已注册的条目。</para>
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
            entry.Initialize(_jsonOptions);
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

            public void Initialize(JsonSerializerOptions jsonOptions)
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
            void Initialize(JsonSerializerOptions jsonOptions);
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
            MigrationManager migrationManager,
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

            public void Initialize(JsonSerializerOptions jsonOptions)
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
