using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data
{
    /// <summary>
    ///     Unified data store for all mod persistent data.
    ///     Uses key-based registration to avoid hardcoded per-data properties and methods.
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
        /// </summary>
        public string ModId { get; }

        internal static bool HasAnyProfileScopedEntries
        {
            get { return GetStoresSnapshot().Any(store => store.HasProfileScopedEntries); }
        }

        /// <summary>
        ///     True after every global-scoped entry has completed initialization and load.
        /// </summary>
        public bool IsGlobalInitialized { get; private set; }

        /// <summary>
        ///     True after profile-scoped entries for the active profile are initialized.
        /// </summary>
        public bool IsProfileInitialized { get; private set; }

        /// <summary>
        ///     Whether this store has at least one <see cref="SaveScope.Profile" /> registration.
        /// </summary>
        public bool HasProfileScopedEntries => _entries.Values.Any(e => e.Scope == SaveScope.Profile);

        /// <summary>
        ///     Defers eager initialization of newly <see cref="Register{T}" /> calls until the scope is disposed.
        /// </summary>
        /// <param name="initializeProfileIfReady">
        ///     When true and profile data is already initialized, profile-scoped registrations initialize on scope end.
        /// </param>
        public IDisposable BeginRegistrationScope(bool initializeProfileIfReady = true)
        {
            _registrationScopeDepth++;
            _registrationScopeInitializeProfileIfReady |= initializeProfileIfReady;
            return new RegistrationScope(this);
        }

        /// <summary>
        ///     Returns the process-wide store for <paramref name="modId" />.
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

            foreach (var entry in _entries.Values.Where(e => e is { Scope: SaveScope.Profile, IsInitialized: false }))
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
        /// </summary>
        /// <param name="key">
        ///     Logical key used with <see cref="Get{T}" />, <see cref="Modify{T}" />, and <see cref="Save" />.
        /// </param>
        /// <param name="fileName">
        ///     File name segment passed to <see cref="ProfileManager" /> path resolution.
        /// </param>
        /// <param name="scope">
        ///     Global or profile persistence scope.
        /// </param>
        /// <param name="defaultFactory">
        ///     Factory for the in-memory default when no file exists.
        /// </param>
        /// <param name="autoCreateIfMissing">
        ///     When true, creates the on-disk file if absent after first save.
        /// </param>
        /// <param name="migrationConfig">
        ///     Optional schema versioning configuration for migrations.
        /// </param>
        /// <param name="migrations">
        ///     Optional migration steps; requires <paramref name="migrationConfig" />.
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
        /// </summary>
        public T Get<T>(string key) where T : class, new()
        {
            return GetEntry<T>(key).Data;
        }

        /// <summary>
        ///     Mutates the instance for <paramref name="key" /> in place (persists via <see cref="Save" />).
        /// </summary>
        public void Modify<T>(string key, Action<T> modifier) where T : class, new()
        {
            GetEntry<T>(key).Modify(modifier);
        }

        /// <summary>
        ///     Writes the entry for <paramref name="key" /> to disk.
        /// </summary>
        public void Save(string key)
        {
            GetEntry(key).Save();
        }

        /// <summary>
        ///     Whether a file already existed when the entry was first loaded.
        /// </summary>
        public bool HasExistingData(string key)
        {
            return GetEntry(key).HadExistingData;
        }

        /// <summary>
        ///     Reloads entries whose resolved path changed (e.g. after profile switch).
        /// </summary>
        /// <returns>
        ///     True if any entry reloaded.
        /// </returns>
        public bool ReloadIfPathChanged()
        {
            if (!IsGlobalInitialized) return false;

            var reloaded = false;
            var result = _entries.Values
                .Where(entry => entry.IsInitialized)
                .Where(entry => entry.ReloadIfPathChanged());
            if (result.Any()) reloaded = true;

            return reloaded;
        }

        /// <summary>
        ///     Persists every registered entry.
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

            foreach (var entry in _entries.Values.Where(e => e.Scope == SaveScope.Profile))
            {
                entry.SaveToProfilePath(oldProfileId);
                entry.Load();
            }
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
            Logger logger)
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
                    autoCreateIfMissing
                );
            }

            public void Load()
            {
                if (_entry == null)
                    throw new InvalidOperationException($"Data entry '{key}' is not initialized.");

                var currentPath = ProfileManager.Instance.GetFilePath(fileName, Scope, modId);
                _lastLoadedPath = currentPath;
                HadExistingData = FileOperations.FileExists(currentPath);
                _entry.Load();
            }

            public bool ReloadIfPathChanged()
            {
                if (_entry == null)
                    throw new InvalidOperationException($"Data entry '{key}' is not initialized.");

                var currentPath = ProfileManager.Instance.GetFilePath(fileName, Scope, modId);
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
