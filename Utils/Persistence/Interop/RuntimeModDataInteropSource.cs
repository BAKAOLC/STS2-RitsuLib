using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using STS2RitsuLib.Data;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Utils.Persistence.Interop
{
    internal static class RuntimeModDataInteropSource
    {
        private const string ProviderTypeMetadataKey = "RitsuLib.ModDataInterop.ProviderType";
        private const string SchemaMethodName = "CreateRitsuLibModDataSchema";

        private static readonly Lock Gate = new();

        private static readonly Dictionary<string, string?> RuntimeRegisteredProviderTypes =
            new(StringComparer.Ordinal);

        private static readonly HashSet<string> RegisteredInteropSlotKeys = new(StringComparer.OrdinalIgnoreCase);

        private static readonly List<InteropSlot> Slots = [];

        private static bool _profileChangedHooked;

        private static readonly Dictionary<string, ReflectionStaticChannel> ProviderAccessors =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly MethodInfo ModDataStoreRegisterOpen =
            typeof(ModDataStore).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Single(m =>
                    m is { Name: nameof(ModDataStore.Register), IsGenericMethodDefinition: true } &&
                    m.GetGenericArguments().Length == 1 &&
                    m.GetParameters().Length == 7 &&
                    m.GetParameters()[0].ParameterType == typeof(string));

        private static readonly MethodInfo ModDataStoreGetOpen =
            typeof(ModDataStore).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Single(m =>
                    m is { Name: nameof(ModDataStore.Get), IsGenericMethodDefinition: true } &&
                    m.GetParameters().Length == 1);

        private static readonly MethodInfo ApplySyncTypedCoreOpen =
            typeof(RuntimeModDataInteropSource).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Single(m =>
                    m is { Name: nameof(ApplySyncTypedCore), IsGenericMethodDefinition: true } &&
                    m.GetGenericArguments().Length == 1);

        private static readonly MethodInfo RegisterJsonDocumentClosed =
            ModDataStoreRegisterOpen.MakeGenericMethod(typeof(ModDataInteropJsonDocument));

        private static readonly ConcurrentDictionary<Type, MethodInfo> RegisterClosedByDataType = new();

        private static readonly ConcurrentDictionary<Type, MethodInfo> GetClosedByDataType = new();

        private static readonly ConcurrentDictionary<Type, MethodInfo> ApplySyncTypedClosedByDataType = new();

        private static readonly ConcurrentDictionary<string, Type?> ResolvedExternalTypeByName =
            new(StringComparer.Ordinal);

        public static bool RegisterProviderType(string providerTypeFullName, string? assemblyName = null)
        {
            if (string.IsNullOrWhiteSpace(providerTypeFullName))
                return false;

            lock (Gate)
            {
                RuntimeRegisteredProviderTypes[providerTypeFullName.Trim()] =
                    string.IsNullOrWhiteSpace(assemblyName) ? null : assemblyName.Trim();
                return true;
            }
        }

        public static bool RegisterProviderType(Type providerType)
        {
            ArgumentNullException.ThrowIfNull(providerType);
            return !string.IsNullOrWhiteSpace(providerType.FullName) &&
                   RegisterProviderType(providerType.FullName, providerType.Assembly.GetName().Name);
        }

        public static bool RegisterProviderType<TProvider>()
        {
            return RegisterProviderType(typeof(TProvider));
        }

        public static int RegisterProviderTypeAndTryRegister<TProvider>()
        {
            return RegisterProviderTypeAndTryRegister(typeof(TProvider));
        }

        public static int RegisterProviderTypeAndTryRegister(string providerTypeFullName, string? assemblyName = null)
        {
            return !RegisterProviderType(providerTypeFullName, assemblyName) ? 0 : TryRegisterAll();
        }

        public static int RegisterProviderTypeAndTryRegister(Type providerType)
        {
            return !RegisterProviderType(providerType) ? 0 : TryRegisterAll();
        }

        public static int TryRegisterAll()
        {
            lock (Gate)
            {
                var providers = DiscoverProviders();
                if (providers.Count == 0)
                    return 0;

                var added = 0;
                foreach (var provider in providers)
                    try
                    {
                        if (!TryReadSchema(provider, out var schema))
                            continue;

                        if (!TryRegisterFromSchema(provider, schema))
                            continue;

                        added++;
                    }
                    catch (Exception ex)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[RuntimeModDataInteropSource] Provider '{provider.ProviderType.FullName}' failed but was isolated: {ex.Message}");
                    }

                return added;
            }
        }

        public static void SyncAllFromProviders()
        {
            InteropSlot[] slotsSnapshot;

            lock (Gate)
            {
                slotsSnapshot = [.. Slots];
            }

            foreach (var slot in slotsSnapshot)
                try
                {
                    if (!ProviderAccessors.TryGetValue(slot.ProviderKey, out var channel))
                        continue;

                    var store = ModDataStore.For(slot.ModId);
                    if (slot.Scope == SaveScope.Profile && !store.IsProfileInitialized)
                        continue;

                    if (slot.IsJsonChannel)
                        SyncJsonSlot(store, slot, channel);
                    else
                        SyncTypedSlot(store, slot.ModId, slot.Key, slot.DataType, channel);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RuntimeModDataInteropSource] Sync failed for '{slot.ModId}'::{slot.Key}: {ex.Message}");
                }
        }

        public static void PushLoadedDataToAllProviders()
        {
            InteropSlot[] slotsSnapshot;

            lock (Gate)
            {
                slotsSnapshot = [.. Slots];
            }

            foreach (var slot in slotsSnapshot)
                try
                {
                    if (!ProviderAccessors.TryGetValue(slot.ProviderKey, out var channel))
                        continue;

                    var store = ModDataStore.For(slot.ModId);
                    if (slot.Scope == SaveScope.Profile && !store.IsProfileInitialized)
                        continue;

                    if (slot.IsJsonChannel)
                    {
                        PushJsonChannel(store, slot, channel);
                        continue;
                    }

                    var obj = CallGenericGet(store, slot.Key, slot.DataType);
                    channel.SetObject(slot.Key, obj);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RuntimeModDataInteropSource] Push-after-load failed for '{slot.ModId}'::{slot.Key}: {ex.Message}");
                }
        }

        private static void OnProfileChangedSyncFromProviders(int oldProfileId, int newProfileId)
        {
            SyncAllFromProviders();
        }

        internal static void EnsureProfileChangedHook()
        {
            lock (Gate)
            {
                if (_profileChangedHooked)
                    return;

                _profileChangedHooked = true;
                ProfileManager.Instance.ProfileChanged += OnProfileChangedSyncFromProviders;
            }
        }

        private static List<InteropProvider> DiscoverProviders()
        {
            var providers = new List<InteropProvider>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var typeNames = ReadProviderTypeNames(asm);
                if (typeNames.Count == 0)
                    continue;

                foreach (var typeName in typeNames)
                {
                    if (string.IsNullOrWhiteSpace(typeName))
                        continue;

                    var providerType = asm.GetType(typeName, false);
                    if (providerType == null)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[RuntimeModDataInteropSource] Provider type not found: {asm.GetName().Name}::{typeName}");
                        continue;
                    }

                    var schemaMethod = providerType.GetMethod(SchemaMethodName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (schemaMethod == null)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[RuntimeModDataInteropSource] Missing static method '{SchemaMethodName}' on {providerType.FullName}.");
                        continue;
                    }

                    var providerName = providerType.FullName ?? providerType.Name;
                    if (!seen.Add(providerName))
                        continue;
                    providers.Add(new(providerType, schemaMethod));
                }
            }

            foreach (var (providerTypeName, assemblyName) in RuntimeRegisteredProviderTypes)
            {
                var providerType = ResolveProviderType(providerTypeName, assemblyName);
                if (providerType == null)
                    continue;

                var schemaMethod = providerType.GetMethod(SchemaMethodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (schemaMethod == null)
                    continue;

                var providerName = providerType.FullName ?? providerType.Name;
                if (!seen.Add(providerName))
                    continue;
                providers.Add(new(providerType, schemaMethod));
            }

            return providers;
        }

        private static Type? ResolveProviderType(string providerTypeName, string? assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
                return AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetType(providerTypeName, false))
                    .OfType<Type>().FirstOrDefault();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var asmName = asm.GetName().Name;
                if (!string.Equals(asmName, assemblyName, StringComparison.OrdinalIgnoreCase))
                    continue;
                var inAsm = asm.GetType(providerTypeName, false);
                if (inAsm != null)
                    return inAsm;
            }

            return AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetType(providerTypeName, false))
                .OfType<Type>().FirstOrDefault();
        }

        private static HashSet<string> ReadProviderTypeNames(Assembly asm)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            object[] attrs;
            try
            {
                attrs = asm.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false);
            }
            catch
            {
                return result;
            }

            foreach (var attr in attrs)
            {
                if (attr is not AssemblyMetadataAttribute metadata)
                    continue;
                if (!string.Equals(metadata.Key, ProviderTypeMetadataKey, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.IsNullOrWhiteSpace(metadata.Value))
                    continue;
                result.Add(metadata.Value.Trim());
            }

            return result;
        }

        private static bool TryReadSchema(InteropProvider provider, out InteropSchemaRoot schema)
        {
            schema = null!;
            object? rawSchema;
            try
            {
                rawSchema = provider.SchemaMethod.Invoke(null, []);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RuntimeModDataInteropSource] Schema invoke failed for {provider.ProviderType.FullName}: {ex.Message}");
                return false;
            }

            return TryResolveSchemaRoot(rawSchema, out var root) && TryParseSchema(root, out schema);
        }

        private static bool TryResolveSchemaRoot(object? rawSchema, out IDictionary<string, object?> root)
        {
            root = null!;
            try
            {
                switch (rawSchema)
                {
                    case null:
                    case string text when string.IsNullOrWhiteSpace(text):
                        return false;
                    case string text when TryParseJsonSchemaPayload(text, out root):
                        return true;
                    case string text:
                        return TryReadSchemaTextFromFile(text, out var fileContent) &&
                               TryParseJsonSchemaPayload(fileContent, out root);
                    default:
                        return TryAsMap(rawSchema, out root);
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool TryReadSchemaTextFromFile(string filePath, out string content)
        {
            content = "";
            var trimmed = filePath.Trim();
            var read = FileOperations.ReadText(trimmed, "RuntimeModDataInteropSource");
            if (!read.Success || string.IsNullOrWhiteSpace(read.Content))
                return false;

            content = read.Content;
            return true;
        }

        private static bool TryParseJsonSchemaPayload(string json, out IDictionary<string, object?> root)
        {
            root = null!;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return false;
                root = JsonObjectToDictionary(doc.RootElement);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Dictionary<string, object?> JsonObjectToDictionary(JsonElement element)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in element.EnumerateObject())
                result[prop.Name] = JsonElementToObject(prop.Value);
            return result;
        }

        private static object? JsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => JsonObjectToDictionary(element),
                JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToArray(),
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.ToString(),
            };
        }

        private static bool TryParseSchema(IDictionary<string, object?> root, out InteropSchemaRoot schema)
        {
            schema = null!;
            if (!TryGetString(root, "modId", out var modId) || string.IsNullOrWhiteSpace(modId))
                return false;

            var entries = new List<InteropEntry>();
            if (!TryGetEnumerable(root, "entries", out var entriesRaw))
                return false;

            foreach (var entryRaw in entriesRaw)
            {
                if (entryRaw == null || !TryAsMap(entryRaw, out var entryMap))
                    continue;
                if (!TryParseEntry(entryMap, out var entry))
                    continue;
                entries.Add(entry);
            }

            if (entries.Count == 0)
                return false;

            schema = new(modId.Trim(), entries);
            return true;
        }

        private static bool TryParseEntry(IDictionary<string, object?> map, out InteropEntry entry)
        {
            entry = null!;
            if (!TryGetString(map, "key", out var key) || string.IsNullOrWhiteSpace(key))
                return false;
            if (!TryGetString(map, "fileName", out var fileName) || string.IsNullOrWhiteSpace(fileName))
                return false;

            var scope = ParseScope(TryGetString(map, "scope", out var scopeRaw) ? scopeRaw : null);
            var autoCreate = TryGetBool(map, "autoCreateIfMissing", out var ac) && ac;

            var dataTypeName = TryGetString(map, "dataType", out var dt) && !string.IsNullOrWhiteSpace(dt)
                ? dt.Trim()
                : null;

            var defaultFactory = TryGetString(map, "defaultFactory", out var df) && !string.IsNullOrWhiteSpace(df)
                ? df.Trim()
                : null;

            ModDataMigrationConfig? migrationConfig = null;
            if (map.TryGetValue("migrationConfig", out var mcRaw) && mcRaw != null &&
                TryAsMap(mcRaw, out var mcMap))
                migrationConfig = ParseMigrationConfig(mcMap);

            List<IMigration>? migrations = null;
            if (TryGetEnumerable(map, "migrations", out var migRaw))
            {
                migrations = [];
                foreach (var migItem in migRaw)
                {
                    if (!TryResolveMigrationTypeName(migItem, out var migTypeName) ||
                        string.IsNullOrWhiteSpace(migTypeName))
                        continue;

                    var migType = ResolveExternalType(migTypeName);
                    if (migType == null)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[RuntimeModDataInteropSource] Migration type not found: {migTypeName}");
                        continue;
                    }

                    if (!InteropMigrationAdapter.TryCreateFromType(migType, out var adapter) || adapter == null)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[RuntimeModDataInteropSource] Migration type could not be instantiated: {migTypeName}");
                        continue;
                    }

                    migrations.Add(adapter);
                }

                if (migrations.Count == 0)
                    migrations = null;
            }

            string[]? jsonPathPull = null;
            if (TryGetStringPaths(map, "jsonPathPull", out var jp))
                jsonPathPull = jp;

            string[]? jsonPathPush = null;
            if (TryGetStringPaths(map, "jsonPathPush", out var jpush))
                jsonPathPush = jpush;

            string[]? jsonPathMergePush = null;
            if (TryGetStringPaths(map, "jsonPathMergePush", out var jmp))
                jsonPathMergePush = jmp;

            entry = new(key.Trim(), fileName.Trim(), scope, autoCreate, dataTypeName, defaultFactory,
                migrationConfig, migrations, jsonPathPull, jsonPathPush, jsonPathMergePush);
            return true;
        }

        private static bool TryGetStringPaths(IDictionary<string, object?> map, string key, out string[]? paths)
        {
            paths = null;
            if (!TryGetEnumerable(map, key, out var raw))
                return false;

            var list = raw.OfType<object>()
                .Select(item => item.ToString()?.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .OfType<string>()
                .ToList();

            if (list.Count == 0)
                return false;

            paths = list.ToArray();
            return true;
        }

        private static ModDataMigrationConfig ParseMigrationConfig(IDictionary<string, object?> map)
        {
            var current = TryGetInt(map, "currentDataVersion", out var cv) ? cv!.Value : 0;
            var min = TryGetInt(map, "minimumSupportedDataVersion", out var mv) ? mv!.Value : 0;
            var prop = TryGetString(map, "schemaVersionProperty", out var sp) && !string.IsNullOrWhiteSpace(sp)
                ? sp.Trim()
                : ModDataVersion.SchemaVersionProperty;

            return new()
            {
                CurrentDataVersion = current,
                MinimumSupportedDataVersion = min,
                SchemaVersionProperty = prop,
            };
        }

        private static bool TryResolveMigrationTypeName(object? raw, out string? typeName)
        {
            typeName = null;
            switch (raw)
            {
                case string s when !string.IsNullOrWhiteSpace(s):
                    typeName = s.Trim();
                    return true;
                default:
                    if (raw == null || !TryAsMap(raw, out var map))
                        return false;
                    if (!TryGetString(map, "type", out var t) || string.IsNullOrWhiteSpace(t))
                        return false;
                    typeName = t.Trim();
                    return true;
            }
        }

        private static Type? ResolveExternalType(string typeName)
        {
            return ResolvedExternalTypeByName.GetOrAdd(typeName,
                static name =>
                {
                    return AppDomain.CurrentDomain.GetAssemblies()
                        .Select(asm => asm.GetType(name, false))
                        .OfType<Type>().FirstOrDefault();
                });
        }

        private static SaveScope ParseScope(string? value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                "profile" => SaveScope.Profile,
                _ => SaveScope.Global,
            };
        }

        private static bool TryRegisterFromSchema(InteropProvider provider, InteropSchemaRoot schema)
        {
            var channel = ReflectionStaticChannelBinder.Bind(provider.ProviderType,
                ReflectionInteropConvention.ModData);
            var providerKey = provider.ProviderType.FullName ?? provider.ProviderType.Name;
            lock (Gate)
            {
                ProviderAccessors[providerKey] = channel;
            }

            var store = ModDataStore.For(schema.ModId);
            var registeredAny = false;
            using (RitsuLibFramework.BeginModDataRegistration(schema.ModId, false))
            {
                foreach (var entry in schema.Entries)
                {
                    var slotKey = $"{schema.ModId}\u001f{entry.Key}";
                    lock (Gate)
                    {
                        if (RegisteredInteropSlotKeys.Contains(slotKey))
                        {
                            RitsuLibFramework.Logger.Warn(
                                $"[RuntimeModDataInteropSource] Skipping duplicate interop data key '{schema.ModId}'::{entry.Key}.");
                            continue;
                        }
                    }

                    try
                    {
                        if (string.IsNullOrWhiteSpace(entry.DataTypeName))
                        {
                            RegisterJsonEntry(store, schema.ModId, providerKey, entry, channel);
                        }
                        else
                        {
                            var dataType = ResolveExternalType(entry.DataTypeName);
                            if (dataType == null)
                            {
                                RitsuLibFramework.Logger.Warn(
                                    $"[RuntimeModDataInteropSource] dataType not found: {entry.DataTypeName}");
                                continue;
                            }

                            RegisterTypedEntry(store, schema.ModId, providerKey, entry, dataType, channel);
                        }

                        lock (Gate)
                        {
                            RegisteredInteropSlotKeys.Add(slotKey);
                        }

                        registeredAny = true;
                    }
                    catch (Exception ex)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[RuntimeModDataInteropSource] Register failed for '{schema.ModId}'::{entry.Key}: {ex.Message}");
                    }
                }
            }

            if (!registeredAny)
                return false;

            PushLoadedForMod(schema.ModId, providerKey);
            return true;
        }

        private static void RegisterJsonEntry(
            ModDataStore store,
            string modId,
            string providerKey,
            InteropEntry entry,
            ReflectionStaticChannel channel)
        {
            Func<ModDataInteropJsonDocument> defaultFactory = static () => new();

            if (!string.IsNullOrWhiteSpace(entry.DefaultFactoryMethodName))
            {
                var factoryMethod = ResolveStaticFactory(providerKey, entry.DefaultFactoryMethodName,
                    typeof(ModDataInteropJsonDocument));
                if (factoryMethod != null)
                    try
                    {
                        defaultFactory =
                            (Func<ModDataInteropJsonDocument>)Delegate.CreateDelegate(
                                typeof(Func<ModDataInteropJsonDocument>), factoryMethod);
                    }
                    catch (ArgumentException)
                    {
                        defaultFactory = () =>
                        {
                            var raw = factoryMethod.Invoke(null, []);
                            return raw as ModDataInteropJsonDocument ?? new();
                        };
                    }
            }

            RegisterJsonDocumentClosed.Invoke(store,
            [
                entry.Key,
                entry.FileName,
                entry.Scope,
                defaultFactory,
                entry.AutoCreateIfMissing,
                entry.MigrationConfig,
                entry.Migrations,
            ]);

            lock (Gate)
            {
                Slots.Add(new(modId, entry.Key, providerKey, typeof(ModDataInteropJsonDocument), true, entry.Scope,
                    entry.JsonPathPull, entry.JsonPathPush, entry.JsonPathMergePush));
            }
        }

        private static void RegisterTypedEntry(
            ModDataStore store,
            string modId,
            string providerKey,
            InteropEntry entry,
            Type dataType,
            ReflectionStaticChannel channel)
        {
            if (dataType is not { IsClass: true } || dataType.IsAbstract)
                throw new InvalidOperationException($"dataType must be a concrete class: {dataType.FullName}");

            var ctor = dataType.GetConstructor(Type.EmptyTypes);
            if (ctor == null && string.IsNullOrWhiteSpace(entry.DefaultFactoryMethodName))
                throw new InvalidOperationException(
                    $"dataType '{dataType.FullName}' requires a parameterless ctor or defaultFactory method.");

            var typedRegister = RegisterClosedByDataType.GetOrAdd(dataType,
                static t => ModDataStoreRegisterOpen.MakeGenericMethod(t));

            var defaultFactory = BuildDefaultFactory(providerKey, dataType, entry.DefaultFactoryMethodName);

            typedRegister.Invoke(store,
            [
                entry.Key,
                entry.FileName,
                entry.Scope,
                defaultFactory,
                entry.AutoCreateIfMissing,
                entry.MigrationConfig,
                entry.Migrations,
            ]);

            lock (Gate)
            {
                Slots.Add(new(modId, entry.Key, providerKey, dataType, false, entry.Scope, null, null, null));
            }
        }

        private static Delegate BuildDefaultFactory(string providerKey, Type dataType, string? defaultFactoryMethodName)
        {
            ReflectionStaticChannel channel;
            lock (Gate)
            {
                ProviderAccessors.TryGetValue(providerKey, out channel!);
            }

            if (string.IsNullOrWhiteSpace(defaultFactoryMethodName)) return CreateDefaultDelegate(dataType);
            var providerType = channel.ProviderType;
            var method = providerType.GetMethod(defaultFactoryMethodName.Trim(),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null || method.GetParameters().Length != 0)
                throw new InvalidOperationException(
                    $"defaultFactory '{defaultFactoryMethodName}' must be static parameterless on '{providerType.FullName}'.");

            var ret = method.Invoke(null, []);
            if (ret == null || !dataType.IsInstanceOfType(ret))
                throw new InvalidOperationException(
                    $"defaultFactory '{defaultFactoryMethodName}' must return non-null '{dataType.FullName}'.");

            var factoryType = typeof(Func<>).MakeGenericType(dataType);
            return Delegate.CreateDelegate(factoryType, method);
        }

        private static Delegate CreateDefaultDelegate(Type dataType)
        {
            var ctor = dataType.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
                throw new InvalidOperationException(
                    $"Type '{dataType.FullName}' requires a parameterless constructor.");

            var newExpr = Expression.New(ctor);
            var lambda = Expression.Lambda(typeof(Func<>).MakeGenericType(dataType), newExpr);
            return lambda.Compile();
        }

        private static MethodInfo? ResolveStaticFactory(string providerKey, string methodName, Type expectedReturn)
        {
            lock (Gate)
            {
                if (!ProviderAccessors.TryGetValue(providerKey, out var channel))
                    return null;

                var method = channel.ProviderType.GetMethod(methodName.Trim(),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method == null || method.GetParameters().Length != 0)
                    return null;
                return !expectedReturn.IsAssignableFrom(method.ReturnType) ? null : method;
            }
        }

        private static void PushLoadedForMod(string modId, string providerKey)
        {
            InteropSlot[] snapshot;
            lock (Gate)
            {
                snapshot = [.. Slots.Where(s => s.ModId == modId && s.ProviderKey == providerKey)];
            }

            if (!ProviderAccessors.TryGetValue(providerKey, out var channel))
                return;

            var store = ModDataStore.For(modId);
            foreach (var slot in snapshot)
                try
                {
                    if (slot.Scope == SaveScope.Profile && !store.IsProfileInitialized)
                        continue;

                    if (slot.IsJsonChannel)
                    {
                        PushJsonChannel(store, slot, channel);
                        continue;
                    }

                    var obj = CallGenericGet(store, slot.Key, slot.DataType);
                    channel.SetObject(slot.Key, obj);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RuntimeModDataInteropSource] Initial push to provider failed for '{modId}'::{slot.Key}: {ex.Message}");
                }
        }

        private static object? CallGenericGet(ModDataStore store, string key, Type dataType)
        {
            var typed = GetClosedByDataType.GetOrAdd(dataType,
                static t => ModDataStoreGetOpen.MakeGenericMethod(t));
            return typed.Invoke(store, [key]);
        }

        private static void SyncJsonSlot(ModDataStore store, InteropSlot slot, ReflectionStaticChannel channel)
        {
            var routing = new KeyedJsonPathRouting(slot.JsonPathPull, slot.JsonPathPush, slot.JsonPathMergePush);
            store.Modify<ModDataInteropJsonDocument>(slot.Key, holder =>
                KeyedJsonDomTransport.PullFromProviderIntoRoot(slot.Key, channel, holder.Root, routing));
        }

        private static void PushJsonChannel(ModDataStore store, InteropSlot slot, ReflectionStaticChannel channel)
        {
            var doc = store.Get<ModDataInteropJsonDocument>(slot.Key);
            var routing = new KeyedJsonPathRouting(slot.JsonPathPull, slot.JsonPathPush, slot.JsonPathMergePush);
            KeyedJsonDomTransport.PushRootToProvider(slot.Key, channel, doc.Root, routing);
        }

        private static void SyncTypedSlot(ModDataStore store, string modId, string key, Type dataType,
            ReflectionStaticChannel channel)
        {
            var raw = channel.GetObject(key);
            if (raw == null)
                return;

            if (!dataType.IsInstanceOfType(raw))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RuntimeModDataInteropSource] Provider returned incompatible type for '{modId}'::{key}: expected '{dataType.FullName}', got '{raw.GetType().FullName}'.");
                return;
            }

            ApplySyncTypedClosedByDataType.GetOrAdd(dataType, static t => ApplySyncTypedCoreOpen.MakeGenericMethod(t))
                .Invoke(null, [store, key, raw]);
        }

        private static void ApplySyncTypedCore<T>(ModDataStore store, string key, object raw) where T : class, new()
        {
            if (raw is not T typed)
                return;

            store.Modify<T>(key, data => CopySerializableProperties(typed, data));
        }

        private static void CopySerializableProperties(object from, object to)
        {
            var type = to.GetType();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite)
                    continue;

                try
                {
                    prop.SetValue(to, prop.GetValue(from));
                }
                catch
                {
                    // ignored
                }
            }
        }

        private static bool TryAsMap(object? obj, out IDictionary<string, object?> map)
        {
            map = null!;
            if (obj is string or null)
                return false;

            switch (obj)
            {
                case IDictionary<string, object?> direct:
                    map = new Dictionary<string, object?>(direct, StringComparer.OrdinalIgnoreCase);
                    return true;
                case IDictionary dict:
                {
                    var tmp = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    foreach (DictionaryEntry de in dict)
                    {
                        if (de.Key == null)
                            continue;
                        tmp[de.Key.ToString() ?? ""] = de.Value;
                    }

                    map = tmp;
                    return true;
                }
            }

            PropertyInfo[] props;
            try
            {
                props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            }
            catch
            {
                return false;
            }

            if (props.Length == 0)
                return false;

            var converted = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in props)
            {
                if (!prop.CanRead)
                    continue;
                if (prop.GetIndexParameters().Length != 0)
                    continue;

                try
                {
                    converted[prop.Name] = prop.GetValue(obj);
                }
                catch
                {
                    // ignored
                }
            }

            if (converted.Count == 0)
                return false;

            map = converted;
            return true;
        }

        private static bool TryGetEnumerable(IDictionary<string, object?> map, string key,
            out IEnumerable<object?> values)
        {
            values = [];
            if (!map.TryGetValue(key, out var raw) || raw == null || raw is string)
                return false;
            if (raw is not IEnumerable enumerable)
                return false;

            var list = enumerable.Cast<object?>().ToList();
            values = list;
            return true;
        }

        private static bool TryGetString(IDictionary<string, object?> map, string key, out string value)
        {
            value = "";
            if (!map.TryGetValue(key, out var raw) || raw == null)
                return false;
            value = raw.ToString() ?? "";
            return true;
        }

        private static bool TryGetInt(IDictionary<string, object?> map, string key, out int? value)
        {
            value = null;
            if (!map.TryGetValue(key, out var raw) || raw == null)
                return false;
            try
            {
                value = raw switch
                {
                    int i => i,
                    long l => (int)l,
                    double d => (int)Math.Round(d),
                    float f => (int)Math.Round(f),
                    _ => Convert.ToInt32(raw),
                };
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetBool(IDictionary<string, object?> map, string key, out bool value)
        {
            value = false;
            if (!map.TryGetValue(key, out var raw) || raw == null)
                return false;
            try
            {
                value = raw switch
                {
                    bool b => b,
                    string s when bool.TryParse(s, out var pb) => pb,
                    int i => i != 0,
                    long l => l != 0,
                    double d => Math.Abs(d) > double.Epsilon,
                    _ => false,
                };
                return true;
            }
            catch
            {
                return false;
            }
        }

        private sealed record InteropSlot(
            string ModId,
            string Key,
            string ProviderKey,
            Type DataType,
            bool IsJsonChannel,
            SaveScope Scope,
            string[]? JsonPathPull,
            string[]? JsonPathPush,
            string[]? JsonPathMergePush);

        private sealed record InteropProvider(Type ProviderType, MethodInfo SchemaMethod);

        private sealed record InteropSchemaRoot(string ModId, List<InteropEntry> Entries);

        private sealed record InteropEntry(
            string Key,
            string FileName,
            SaveScope Scope,
            bool AutoCreateIfMissing,
            string? DataTypeName,
            string? DefaultFactoryMethodName,
            ModDataMigrationConfig? MigrationConfig,
            List<IMigration>? Migrations,
            string[]? JsonPathPull,
            string[]? JsonPathPush,
            string[]? JsonPathMergePush);
    }
}
