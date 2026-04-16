using System.Collections;
using System.Reflection;
using System.Text.Json;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Mirrors settings pages declared by third-party assemblies through a reflection-only protocol
    ///     (no compile-time reference to RitsuLib required).
    /// </summary>
    public static class ModSettingsRuntimeReflectionInteropMirror
    {
        private const string ProviderTypeMetadataKey = "RitsuLib.ModSettingsInterop.ProviderType";
        private const string SchemaMethodName = "CreateRitsuLibSettingsSchema";
        private const string ResolverGetMethodName = "GetRitsuLibSettingValue";
        private const string ResolverSetMethodName = "SetRitsuLibSettingValue";
        private const string ResolverSaveMethodName = "SaveRitsuLibSettings";
        private const string ActionInvokeMethodName = "InvokeRitsuLibSettingAction";
        private const string TypedGetBoolMethodName = "GetRitsuLibSettingBool";
        private const string TypedSetBoolMethodName = "SetRitsuLibSettingBool";
        private const string TypedGetDoubleMethodName = "GetRitsuLibSettingDouble";
        private const string TypedSetDoubleMethodName = "SetRitsuLibSettingDouble";
        private const string TypedGetIntMethodName = "GetRitsuLibSettingInt";
        private const string TypedSetIntMethodName = "SetRitsuLibSettingInt";
        private const string TypedGetStringMethodName = "GetRitsuLibSettingString";
        private const string TypedSetStringMethodName = "SetRitsuLibSettingString";

        private static readonly Lock Gate = new();
        private static readonly HashSet<string> SchemaPayloadWarningDedup = new(StringComparer.Ordinal);

        private static readonly Dictionary<string, string?> RuntimeRegisteredProviderTypes =
            new(StringComparer.Ordinal);

        /// <summary>
        ///     Registers an interop provider type name explicitly for runtime discovery.
        ///     This is intended for reflection-based callers that do not reference RitsuLib at compile time.
        /// </summary>
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

        /// <summary>
        ///     Registers an interop provider type explicitly for runtime discovery.
        /// </summary>
        public static bool RegisterProviderType(Type providerType)
        {
            ArgumentNullException.ThrowIfNull(providerType);
            return !string.IsNullOrWhiteSpace(providerType.FullName) &&
                   RegisterProviderType(providerType.FullName, providerType.Assembly.GetName().Name);
        }

        /// <summary>
        ///     Registers an interop provider type explicitly for runtime discovery.
        /// </summary>
        public static bool RegisterProviderType<TProvider>()
        {
            return RegisterProviderType(typeof(TProvider));
        }

        /// <summary>
        ///     Registers a provider type and immediately attempts mirror registration.
        /// </summary>
        public static int RegisterProviderTypeAndTryRegister(string providerTypeFullName, string? assemblyName = null)
        {
            return !RegisterProviderType(providerTypeFullName, assemblyName) ? 0 : TryRegisterMirroredPages();
        }

        /// <summary>
        ///     Registers a provider type and immediately attempts mirror registration.
        /// </summary>
        public static int RegisterProviderTypeAndTryRegister(Type providerType)
        {
            return !RegisterProviderType(providerType) ? 0 : TryRegisterMirroredPages();
        }

        /// <summary>
        ///     Registers a provider type and immediately attempts mirror registration.
        /// </summary>
        public static int RegisterProviderTypeAndTryRegister<TProvider>()
        {
            return RegisterProviderTypeAndTryRegister(typeof(TProvider));
        }

        /// <summary>
        ///     Discovers reflection providers and registers mirrored pages from their declared schemas.
        /// </summary>
        public static int TryRegisterMirroredPages()
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
                            $"[ModSettingsRuntimeReflectionInteropMirror] Provider '{provider.ProviderType.FullName}' failed but was isolated: {ex.Message}");
                    }

                return added;
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
                            $"[ModSettingsRuntimeReflectionInteropMirror] Provider type not found: {asm.GetName().Name}::{typeName}");
                        continue;
                    }

                    var schemaMethod = providerType.GetMethod(SchemaMethodName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (schemaMethod == null)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[ModSettingsRuntimeReflectionInteropMirror] Missing static method '{SchemaMethodName}' on {providerType.FullName}.");
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
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var asmName = asm.GetName().Name;
                    if (!string.Equals(asmName, assemblyName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var inAsm = asm.GetType(providerTypeName, false);
                    if (inAsm != null)
                        return inAsm;
                }
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
                    $"[ModSettingsRuntimeReflectionInteropMirror] Schema invoke failed for {provider.ProviderType.FullName}: {ex.Message}");
                return false;
            }

            if (!TryResolveSchemaRoot(rawSchema, out var root))
            {
                WarnSchemaPayloadInvalidOnce(provider.ProviderType);
                return false;
            }

            if (TryParseSchema(root, out schema)) return true;
            RitsuLibFramework.Logger.Warn(
                $"[ModSettingsRuntimeReflectionInteropMirror] Schema parse failed for {provider.ProviderType.FullName}.");
            return false;
        }

        private static void WarnSchemaPayloadInvalidOnce(Type providerType)
        {
            var providerName = providerType.FullName ?? providerType.Name;
            if (!SchemaPayloadWarningDedup.Add(providerName))
                return;

            RitsuLibFramework.Logger.Warn(
                $"[ModSettingsRuntimeReflectionInteropMirror] Schema payload is null/invalid for {providerName}.");
        }

        private static bool TryRegisterFromSchema(InteropProvider provider, InteropSchemaRoot schema)
        {
            if (!ModSettingsMirrorInteropPolicy.ShouldMirror(ModSettingsMirrorSource.RuntimeInterop, schema.ModId,
                    provider.ProviderType))
                return false;

            var access = BuildAccessor(provider.ProviderType);
            var saveAction = access.SaveAction;
            var addedAny = false;

            foreach (var pageSchema in schema.Pages.Where(pageSchema =>
                         !ModSettingsRegistry.TryGetPage(schema.ModId, pageSchema.PageId, out _)))
                try
                {
                    ModSettingsRegistry.Register(schema.ModId, page =>
                    {
                        page.WithTitle(ModSettingsText.Literal(pageSchema.Title));
                        if (!string.IsNullOrWhiteSpace(pageSchema.Description))
                            page.WithDescription(ModSettingsText.Literal(pageSchema.Description));
                        page.WithSortOrder(pageSchema.SortOrder);
                        if (!string.IsNullOrWhiteSpace(pageSchema.ParentPageId))
                            page.AsChildOf(pageSchema.ParentPageId);
                        if (!string.IsNullOrWhiteSpace(schema.ModDisplayName))
                            page.WithModDisplayName(ModSettingsText.Literal(schema.ModDisplayName));
                        if (schema.ModSidebarOrder is { } sidebarOrder)
                            page.WithModSidebarOrder(sidebarOrder);

                        foreach (var section in pageSchema.Sections)
                            page.AddSection(section.Id, sb =>
                            {
                                if (!string.IsNullOrWhiteSpace(section.Title))
                                    sb.WithTitle(ModSettingsText.Literal(section.Title));
                                if (!string.IsNullOrWhiteSpace(section.Description))
                                    sb.WithDescription(ModSettingsText.Literal(section.Description));

                                foreach (var entry in section.Entries)
                                    AppendEntry(sb, schema.ModId, entry, access, saveAction);
                            });
                    }, pageSchema.PageId);

                    addedAny = true;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[ModSettingsRuntimeReflectionInteropMirror] Register failed for {schema.ModId}::{pageSchema.PageId}: {ex.Message}");
                }

            return addedAny;
        }

        private static void AppendEntry(
            ModSettingsSectionBuilder section,
            string modId,
            InteropEntry entry,
            InteropAccessor access,
            Action saveAction)
        {
            var label = ModSettingsText.Literal(entry.Label);
            var description = string.IsNullOrWhiteSpace(entry.Description)
                ? null
                : ModSettingsText.Literal(entry.Description);
            var dataKey = $"interop::{entry.Key}";

            switch (entry.Type)
            {
                case InteropEntryType.Header:
                    section.AddHeader(entry.Id, label, description);
                    return;
                case InteropEntryType.Paragraph:
                    section.AddParagraph(entry.Id, label, description, entry.MaxBodyHeight);
                    return;
                case InteropEntryType.Subpage:
                    if (string.IsNullOrWhiteSpace(entry.TargetPageId))
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[ModSettingsRuntimeReflectionInteropMirror] Skipping subpage entry '{entry.Id}' because targetPageId is missing.");
                        return;
                    }

                    section.AddSubpage(entry.Id, label, entry.TargetPageId,
                        string.IsNullOrWhiteSpace(entry.ButtonText) ? null : ModSettingsText.Literal(entry.ButtonText),
                        description);
                    return;
                case InteropEntryType.Toggle:
                {
                    var binding = ModSettingsBindings.Callback(modId, dataKey,
                        () => ReadBool(entry.Key, access),
                        value => WriteBool(entry.Key, value, access),
                        saveAction,
                        entry.Scope);
                    section.AddToggle(entry.Id, label, binding, description);
                    return;
                }
                case InteropEntryType.Slider:
                {
                    var binding = ModSettingsBindings.Callback(modId, dataKey,
                        () => ReadDouble(entry.Key, access),
                        value => WriteDouble(entry.Key, value, access),
                        saveAction,
                        entry.Scope);
                    section.AddSlider(entry.Id, label, binding, entry.Min, entry.Max, entry.Step, null, description);
                    return;
                }
                case InteropEntryType.IntSlider:
                {
                    var binding = ModSettingsBindings.Callback(modId, dataKey,
                        () => ReadInt(entry.Key, access),
                        value => WriteInt(entry.Key, value, access),
                        saveAction,
                        entry.Scope);
                    section.AddIntSlider(entry.Id, label, binding, (int)Math.Round(entry.Min),
                        (int)Math.Round(entry.Max),
                        Math.Max(1, (int)Math.Round(entry.Step)), null, description);
                    return;
                }
                case InteropEntryType.String:
                {
                    var binding = ModSettingsBindings.Callback(modId, dataKey,
                        () => ReadString(entry.Key, access),
                        value => WriteString(entry.Key, value, access),
                        saveAction,
                        entry.Scope);
                    section.AddString(entry.Id, label, binding, null, entry.MaxLength, description);
                    return;
                }
                case InteropEntryType.Choice:
                {
                    if (entry.Options.Count == 0)
                        return;
                    var options = entry.Options
                        .Select(o => new ModSettingsChoiceOption<string>(o.Value, ModSettingsText.Literal(o.Label)))
                        .ToArray();
                    var firstValue = options[0].Value;
                    var binding = ModSettingsBindings.Callback(modId, dataKey,
                        () =>
                        {
                            var current = ReadString(entry.Key, access);
                            return string.IsNullOrWhiteSpace(current) ? firstValue : current;
                        },
                        value => WriteString(entry.Key, string.IsNullOrWhiteSpace(value) ? firstValue : value, access),
                        saveAction,
                        entry.Scope);
                    section.AddChoice(entry.Id, label, binding, options, description,
                        entry.ChoicePresentation == "dropdown"
                            ? ModSettingsChoicePresentation.Dropdown
                            : ModSettingsChoicePresentation.Stepper);
                    return;
                }
                case InteropEntryType.Button:
                {
                    var buttonText = ModSettingsText.Literal(string.IsNullOrWhiteSpace(entry.ButtonText)
                        ? entry.Label
                        : entry.ButtonText);
                    section.AddButton(entry.Id, label, buttonText, () => access.InvokeAction(entry.Key),
                        entry.ButtonTone, description);
                    return;
                }
            }
        }

        private static bool ReadBool(string key, InteropAccessor access)
        {
            return access.GetBool?.Invoke(key) ?? CoerceBool(access.GetObject(key));
        }

        private static void WriteBool(string key, bool value, InteropAccessor access)
        {
            if (access.SetBool != null)
            {
                access.SetBool(key, value);
                return;
            }

            access.SetObject(key, value);
        }

        private static double ReadDouble(string key, InteropAccessor access)
        {
            return access.GetDouble?.Invoke(key) ?? CoerceDouble(access.GetObject(key));
        }

        private static void WriteDouble(string key, double value, InteropAccessor access)
        {
            if (access.SetDouble != null)
            {
                access.SetDouble(key, value);
                return;
            }

            access.SetObject(key, value);
        }

        private static int ReadInt(string key, InteropAccessor access)
        {
            return access.GetInt?.Invoke(key) ?? CoerceInt(access.GetObject(key));
        }

        private static void WriteInt(string key, int value, InteropAccessor access)
        {
            if (access.SetInt != null)
            {
                access.SetInt(key, value);
                return;
            }

            access.SetObject(key, value);
        }

        private static string ReadString(string key, InteropAccessor access)
        {
            if (access.GetString != null)
                return access.GetString(key) ?? string.Empty;
            return access.GetObject(key)?.ToString() ?? string.Empty;
        }

        private static void WriteString(string key, string value, InteropAccessor access)
        {
            if (access.SetString != null)
            {
                access.SetString(key, value);
                return;
            }

            access.SetObject(key, value);
        }

        private static bool CoerceBool(object? value)
        {
            try
            {
                return value switch
                {
                    null => false,
                    bool b => b,
                    string s when bool.TryParse(s, out var b) => b,
                    _ => Convert.ToBoolean(value),
                };
            }
            catch
            {
                return false;
            }
        }

        private static double CoerceDouble(object? value)
        {
            try
            {
                return value switch
                {
                    null => 0d,
                    double d => d,
                    float f => f,
                    int i => i,
                    long l => l,
                    _ => Convert.ToDouble(value),
                };
            }
            catch
            {
                return 0d;
            }
        }

        private static int CoerceInt(object? value)
        {
            try
            {
                return value switch
                {
                    null => 0,
                    int i => i,
                    long l => (int)l,
                    double d => (int)Math.Round(d),
                    float f => (int)Math.Round(f),
                    _ => Convert.ToInt32(value),
                };
            }
            catch
            {
                return 0;
            }
        }

        private static InteropAccessor BuildAccessor(Type providerType)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            var getObject = providerType.GetMethod(ResolverGetMethodName, flags, [typeof(string)]);
            var setObject = providerType.GetMethod(ResolverSetMethodName, flags, [typeof(string), typeof(object)]);
            var save = providerType.GetMethod(ResolverSaveMethodName, flags, Type.EmptyTypes);
            var action = providerType.GetMethod(ActionInvokeMethodName, flags, [typeof(string)]);

            var getBool = providerType.GetMethod(TypedGetBoolMethodName, flags, [typeof(string)]);
            var setBool = providerType.GetMethod(TypedSetBoolMethodName, flags, [typeof(string), typeof(bool)]);
            var getDouble = providerType.GetMethod(TypedGetDoubleMethodName, flags, [typeof(string)]);
            var setDouble = providerType.GetMethod(TypedSetDoubleMethodName, flags,
                [typeof(string), typeof(double)]);
            var getInt = providerType.GetMethod(TypedGetIntMethodName, flags, [typeof(string)]);
            var setInt = providerType.GetMethod(TypedSetIntMethodName, flags, [typeof(string), typeof(int)]);
            var getString = providerType.GetMethod(TypedGetStringMethodName, flags, [typeof(string)]);
            var setString = providerType.GetMethod(TypedSetStringMethodName, flags,
                [typeof(string), typeof(string)]);

            if (getObject == null || setObject == null)
                throw new InvalidOperationException(
                    $"Provider {providerType.FullName} requires static {ResolverGetMethodName}(string) and {ResolverSetMethodName}(string, object).");

            return new(
                key => getObject.Invoke(null, [key]),
                (key, value) => setObject.Invoke(null, [key, value]),
                key =>
                {
                    if (getBool == null) throw new InvalidOperationException();
                    return (bool)(getBool.Invoke(null, [key]) ?? false);
                },
                (key, value) => setBool?.Invoke(null, [key, value]),
                key => getDouble == null
                    ? throw new InvalidOperationException()
                    : Convert.ToDouble(getDouble.Invoke(null, [key]) ?? 0d),
                (key, value) => setDouble?.Invoke(null, [key, value]),
                key => getInt == null
                    ? throw new InvalidOperationException()
                    : Convert.ToInt32(getInt.Invoke(null, [key]) ?? 0),
                (key, value) => setInt?.Invoke(null, [key, value]),
                key => getString?.Invoke(null, [key]) as string,
                (key, value) => setString?.Invoke(null, [key, value]),
                () => save?.Invoke(null, []),
                key => action?.Invoke(null, [key]));
        }

        private static bool TryParseSchema(IDictionary<string, object?> root, out InteropSchemaRoot schema)
        {
            schema = null!;
            if (!TryGetString(root, "modId", out var modId) || string.IsNullOrWhiteSpace(modId))
                return false;

            var modDisplayName = TryGetString(root, "modDisplayName", out var mdn) ? mdn : null;
            var modSidebarOrder = TryGetInt(root, "modSidebarOrder", out var mso) ? mso : null;

            var pages = new List<InteropPage>();
            if (TryGetEnumerable(root, "pages", out var pagesRaw))
                foreach (var pageRaw in pagesRaw)
                {
                    if (pageRaw == null || !TryAsMap(pageRaw, out var pageMap))
                        continue;
                    if (!TryParsePage(pageMap, out var page))
                        continue;
                    pages.Add(page);
                }
            else if (TryParseLegacySinglePage(root, out var legacyPage)) pages.Add(legacyPage);

            if (pages.Count == 0)
                return false;

            schema = new(modId, modDisplayName, modSidebarOrder, pages);
            return true;
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
            var read = FileOperations.ReadText(trimmed, "ModSettingsRuntimeReflectionInteropMirror");
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

        private static bool TryParseLegacySinglePage(IDictionary<string, object?> root, out InteropPage page)
        {
            page = null!;
            var pageId = TryGetString(root, "pageId", out var p) && !string.IsNullOrWhiteSpace(p)
                ? p
                : "interop";
            var title = TryGetString(root, "title", out var t) && !string.IsNullOrWhiteSpace(t) ? t : "Settings";
            var description = TryGetString(root, "description", out var d) ? d : null;
            var sortOrder = TryGetInt(root, "sortOrder", out var so) ? so ?? 10_040 : 10_040;

            if (!TryGetEnumerable(root, "sections", out var sectionsRaw))
                return false;

            var sections = new List<InteropSection>();
            foreach (var sectionRaw in sectionsRaw)
            {
                if (sectionRaw == null || !TryAsMap(sectionRaw, out var sectionMap))
                    continue;
                if (!TryParseSection(sectionMap, out var section))
                    continue;
                sections.Add(section);
            }

            if (sections.Count == 0)
                return false;

            page = new(pageId, null, title, description, sortOrder, sections);
            return true;
        }

        private static bool TryParsePage(IDictionary<string, object?> map, out InteropPage page)
        {
            page = null!;
            var pageId = TryGetString(map, "pageId", out var p) && !string.IsNullOrWhiteSpace(p)
                ? p
                : "interop";
            var title = TryGetString(map, "title", out var t) && !string.IsNullOrWhiteSpace(t) ? t : "Settings";
            var description = TryGetString(map, "description", out var d) ? d : null;
            var parentPageId = TryGetString(map, "parentPageId", out var parent) && !string.IsNullOrWhiteSpace(parent)
                ? parent
                : null;
            var sortOrder = TryGetInt(map, "sortOrder", out var so) ? so ?? 10_040 : 10_040;

            if (!TryGetEnumerable(map, "sections", out var sectionsRaw))
                return false;

            var sections = new List<InteropSection>();
            foreach (var sectionRaw in sectionsRaw)
            {
                if (sectionRaw == null || !TryAsMap(sectionRaw, out var sectionMap))
                    continue;
                if (!TryParseSection(sectionMap, out var section))
                    continue;
                sections.Add(section);
            }

            if (sections.Count == 0)
                return false;

            page = new(pageId, parentPageId, title, description, sortOrder, sections);
            return true;
        }

        private static bool TryParseSection(IDictionary<string, object?> map, out InteropSection section)
        {
            section = null!;
            if (!TryGetString(map, "id", out var id) || string.IsNullOrWhiteSpace(id))
                return false;

            var title = TryGetString(map, "title", out var t) ? t : null;
            var description = TryGetString(map, "description", out var d) ? d : null;
            if (!TryGetEnumerable(map, "entries", out var entriesRaw))
                return false;

            var entries = new List<InteropEntry>();
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

            section = new(id, title, description, entries);
            return true;
        }

        private static bool TryParseEntry(IDictionary<string, object?> map, out InteropEntry entry)
        {
            entry = null!;
            if (!TryGetString(map, "id", out var id) || string.IsNullOrWhiteSpace(id))
                return false;
            if (!TryGetString(map, "type", out var typeName) || !TryParseEntryType(typeName, out var type))
                return false;

            var key = TryGetString(map, "key", out var k) && !string.IsNullOrWhiteSpace(k) ? k : id;
            var label = TryGetString(map, "label", out var l) && !string.IsNullOrWhiteSpace(l) ? l : id;
            var description = TryGetString(map, "description", out var d) ? d : null;
            var buttonText = TryGetString(map, "buttonText", out var bt) ? bt : null;
            var targetPageId = TryGetString(map, "targetPageId", out var target) ? target : null;
            var min = TryGetDouble(map, "min", out var minValue) ? minValue : 0d;
            var max = TryGetDouble(map, "max", out var maxValue) ? maxValue : 100d;
            var step = TryGetDouble(map, "step", out var stepValue) ? stepValue : 1d;
            if (max < min)
                (min, max) = (max, min);
            if (step <= 0d)
                step = 1d;
            var maxLength = TryGetInt(map, "maxLength", out var ml) ? ml : null;
            var maxBodyHeight = TryGetDouble(map, "maxBodyHeight", out var maxBodyHeightValue)
                ? (float?)maxBodyHeightValue
                : null;
            var scope = ParseScope(TryGetString(map, "scope", out var scopeRaw) ? scopeRaw : null);
            var presentation = TryGetString(map, "presentation", out var p) ? p : "stepper";
            var tone = ParseButtonTone(TryGetString(map, "tone", out var toneRaw) ? toneRaw : null);
            var options = ParseOptions(map);

            entry = new(
                id,
                type,
                key,
                label,
                description,
                min,
                max,
                step,
                maxLength,
                maxBodyHeight,
                options,
                buttonText,
                targetPageId,
                tone,
                scope,
                presentation);
            return true;
        }

        private static SaveScope ParseScope(string? value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                "profile" => SaveScope.Profile,
                _ => SaveScope.Global,
            };
        }

        private static ModSettingsButtonTone ParseButtonTone(string? value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                "accent" => ModSettingsButtonTone.Accent,
                "danger" => ModSettingsButtonTone.Danger,
                _ => ModSettingsButtonTone.Normal,
            };
        }

        private static List<InteropChoiceOption> ParseOptions(IDictionary<string, object?> entryMap)
        {
            var options = new List<InteropChoiceOption>();
            if (!TryGetEnumerable(entryMap, "options", out var optionsRaw))
                return options;

            foreach (var optionRaw in optionsRaw)
            {
                if (optionRaw == null)
                    continue;

                if (TryAsMap(optionRaw, out var optionMap))
                {
                    if (!TryGetString(optionMap, "value", out var value) || string.IsNullOrWhiteSpace(value))
                        continue;
                    var label = TryGetString(optionMap, "label", out var optionLabel) &&
                                !string.IsNullOrWhiteSpace(optionLabel)
                        ? optionLabel
                        : value;
                    options.Add(new(value, label));
                    continue;
                }

                var str = optionRaw.ToString();
                if (string.IsNullOrWhiteSpace(str))
                    continue;
                options.Add(new(str, str));
            }

            return options;
        }

        private static bool TryParseEntryType(string raw, out InteropEntryType type)
        {
            type = raw.Trim().ToLowerInvariant() switch
            {
                "header" => InteropEntryType.Header,
                "paragraph" => InteropEntryType.Paragraph,
                "subpage" => InteropEntryType.Subpage,
                "toggle" => InteropEntryType.Toggle,
                "slider" => InteropEntryType.Slider,
                "int-slider" or "intslider" => InteropEntryType.IntSlider,
                "choice" => InteropEntryType.Choice,
                "string" => InteropEntryType.String,
                "button" => InteropEntryType.Button,
                _ => (InteropEntryType)(-1),
            };
            return Enum.IsDefined(type);
        }

        private static bool TryAsMap(object obj, out IDictionary<string, object?> map)
        {
            if (obj is string)
            {
                map = null!;
                return false;
            }

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
                map = null!;
                return false;
            }

            if (props.Length == 0)
            {
                map = null!;
                return false;
            }

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
                    // ignore unreadable reflected property and continue
                }
            }

            if (converted.Count == 0)
            {
                map = null!;
                return false;
            }

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

        private static bool TryGetDouble(IDictionary<string, object?> map, string key, out double value)
        {
            value = 0d;
            if (!map.TryGetValue(key, out var raw) || raw == null)
                return false;
            try
            {
                value = raw switch
                {
                    double d => d,
                    float f => f,
                    int i => i,
                    long l => l,
                    _ => Convert.ToDouble(raw),
                };
                return true;
            }
            catch
            {
                return false;
            }
        }

        private sealed record InteropProvider(Type ProviderType, MethodInfo SchemaMethod);

        private sealed record InteropSchemaRoot(
            string ModId,
            string? ModDisplayName,
            int? ModSidebarOrder,
            List<InteropPage> Pages);

        private sealed record InteropPage(
            string PageId,
            string? ParentPageId,
            string Title,
            string? Description,
            int SortOrder,
            List<InteropSection> Sections);

        private sealed record InteropSection(string Id, string? Title, string? Description, List<InteropEntry> Entries);

        private sealed record InteropEntry(
            string Id,
            InteropEntryType Type,
            string Key,
            string Label,
            string? Description,
            double Min,
            double Max,
            double Step,
            int? MaxLength,
            float? MaxBodyHeight,
            List<InteropChoiceOption> Options,
            string? ButtonText,
            string? TargetPageId,
            ModSettingsButtonTone ButtonTone,
            SaveScope Scope,
            string ChoicePresentation);

        private sealed record InteropChoiceOption(string Value, string Label);

        private enum InteropEntryType
        {
            Header,
            Paragraph,
            Subpage,
            Toggle,
            Slider,
            IntSlider,
            Choice,
            String,
            Button,
        }

        private sealed record InteropAccessor(
            Func<string, object?> GetObject,
            Action<string, object?> SetObject,
            Func<string, bool>? GetBool,
            Action<string, bool>? SetBool,
            Func<string, double>? GetDouble,
            Action<string, double>? SetDouble,
            Func<string, int>? GetInt,
            Action<string, int>? SetInt,
            Func<string, string?>? GetString,
            Action<string, string>? SetString,
            Action SaveAction,
            Action<string> InvokeAction);
    }
}
