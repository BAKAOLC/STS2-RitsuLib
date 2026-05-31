using System.Collections;
using System.Reflection;
using System.Text.Json;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    internal static class RuntimeInteropMirrorSource
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
        private static readonly HashSet<string> InteropMethodWarningDedup = new(StringComparer.Ordinal);
        private static readonly Lock DefaultWriteGate = new();
        private static readonly HashSet<string> DefaultWriteDedup = new(StringComparer.Ordinal);

        private static readonly Dictionary<string, string?> RuntimeRegisteredProviderTypes =
            new(StringComparer.Ordinal);

        private static List<InteropProvider>? _cachedDiscoveredProviders;
        private static int _cachedDiscoveryAssemblyCount = -1;
        private static readonly HashSet<string> ProcessedProviderNames = new(StringComparer.Ordinal);

        public static bool RegisterProviderType(string providerTypeFullName, string? assemblyName = null)
        {
            if (string.IsNullOrWhiteSpace(providerTypeFullName))
                return false;

            lock (Gate)
            {
                RuntimeRegisteredProviderTypes[providerTypeFullName.Trim()] =
                    string.IsNullOrWhiteSpace(assemblyName) ? null : assemblyName.Trim();
                _cachedDiscoveredProviders = null;
                _cachedDiscoveryAssemblyCount = -1;
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

        public static int RegisterProviderTypeAndTryRegister(string providerTypeFullName, string? assemblyName = null)
        {
            return !RegisterProviderType(providerTypeFullName, assemblyName) ? 0 : TryRegisterMirroredPages();
        }

        public static int RegisterProviderTypeAndTryRegister(Type providerType)
        {
            return !RegisterProviderType(providerType) ? 0 : TryRegisterMirroredPages();
        }

        public static int RegisterProviderTypeAndTryRegister<TProvider>()
        {
            return RegisterProviderTypeAndTryRegister(typeof(TProvider));
        }

        public static int TryRegisterMirroredPages()
        {
            lock (Gate)
            {
                var providers = DiscoverProviders();
                return providers.Count == 0 ? 0 : providers.Sum(TryRegisterProvider);
            }
        }

        private static List<InteropProvider> DiscoverProviders()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (_cachedDiscoveredProviders != null && _cachedDiscoveryAssemblyCount == assemblies.Length)
                return [.. _cachedDiscoveredProviders];

            var providers = new List<InteropProvider>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var asm in assemblies)
                DiscoverAssemblyProviders(asm, providers, seen);

            foreach (var (providerTypeName, assemblyName) in RuntimeRegisteredProviderTypes)
                AddRuntimeRegisteredProvider(providerTypeName, assemblyName, providers, seen);

            CacheDiscoveredProviders(providers, assemblies.Length);
            return providers;
        }

        private static void DiscoverAssemblyProviders(Assembly asm, List<InteropProvider> providers,
            HashSet<string> seen)
        {
            var typeNames = ReadProviderTypeNames(asm);
            if (typeNames.Count == 0)
                return;

            foreach (var typeName in typeNames)
            {
                if (string.IsNullOrWhiteSpace(typeName))
                    continue;

                var providerType = asm.GetType(typeName, false);
                if (providerType == null)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RuntimeInteropMirrorSource] Provider type not found: {asm.GetName().Name}::{typeName}");
                    continue;
                }

                AddProvider(providerType, providers, seen, true);
            }
        }

        private static void AddRuntimeRegisteredProvider(string providerTypeName, string? assemblyName,
            List<InteropProvider> providers, HashSet<string> seen)
        {
            var providerType = ResolveProviderType(providerTypeName, assemblyName);
            if (providerType == null)
                return;

            AddProvider(providerType, providers, seen, false);
        }

        private static void AddProvider(Type providerType, List<InteropProvider> providers, HashSet<string> seen,
            bool warnMissingSchema)
        {
            var schemaMethod = providerType.GetMethod(SchemaMethodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (schemaMethod == null)
            {
                if (warnMissingSchema)
                    RitsuLibFramework.Logger.Warn(
                        $"[RuntimeInteropMirrorSource] Missing static method '{SchemaMethodName}' on {providerType.FullName}.");
                return;
            }

            var providerName = providerType.FullName ?? providerType.Name;
            if (!seen.Add(providerName))
                return;
            providers.Add(new(providerType, schemaMethod));
        }

        private static void CacheDiscoveredProviders(List<InteropProvider> providers, int assemblyCount)
        {
            _cachedDiscoveredProviders = [.. providers];
            _cachedDiscoveryAssemblyCount = assemblyCount;
        }

        private static int TryRegisterProvider(InteropProvider provider)
        {
            var providerName = provider.ProviderType.FullName ?? provider.ProviderType.Name;
            if (ProcessedProviderNames.Contains(providerName))
                return 0;

            try
            {
                if (!TryReadSchema(provider, out var schema))
                    return 0;

                if (!TryRegisterFromSchema(provider, schema))
                    return 0;

                ProcessedProviderNames.Add(providerName);
                return 1;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RuntimeInteropMirrorSource] Provider '{provider.ProviderType.FullName}' failed but was isolated: {ex.Message}");
                return 0;
            }
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
                    $"[RuntimeInteropMirrorSource] Schema invoke failed for {provider.ProviderType.FullName}: {ex.Message}");
                return false;
            }

            if (!TryResolveSchemaRoot(rawSchema, out var root))
            {
                WarnSchemaPayloadInvalidOnce(provider.ProviderType);
                return false;
            }

            if (TryParseSchema(root, provider.ProviderType, out schema)) return true;
            RitsuLibFramework.Logger.Warn(
                $"[RuntimeInteropMirrorSource] Schema parse failed for {provider.ProviderType.FullName}.");
            return false;
        }

        private static void WarnSchemaPayloadInvalidOnce(Type providerType)
        {
            var providerName = providerType.FullName ?? providerType.Name;
            if (!SchemaPayloadWarningDedup.Add(providerName))
                return;

            RitsuLibFramework.Logger.Warn(
                $"[RuntimeInteropMirrorSource] Schema payload is null/invalid for {providerName}.");
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
                        page.WithTitle(pageSchema.Title);
                        if (pageSchema.Description != null)
                            page.WithDescription(pageSchema.Description);
                        page.WithSortOrder(pageSchema.SortOrder);
                        if (!string.IsNullOrWhiteSpace(pageSchema.ParentPageId))
                            page.AsChildOf(pageSchema.ParentPageId);
                        if (schema.ModDisplayName != null)
                            page.WithModDisplayName(schema.ModDisplayName);
                        if (schema.ModSidebarOrder is { } sidebarOrder)
                            page.WithModSidebarOrder(sidebarOrder);

                        foreach (var section in pageSchema.Sections)
                            page.AddSection(section.Id, sb =>
                            {
                                if (section.Title != null)
                                    sb.WithTitle(section.Title);
                                if (section.Description != null)
                                    sb.WithDescription(section.Description);

                                foreach (var entry in section.Entries)
                                    AppendEntry(sb, schema.ModId, entry, access, saveAction);
                            });
                    }, pageSchema.PageId);
                    ModSettingsMirrorSyncPolicyRegistry.RegisterPage(schema.ModId, pageSchema.PageId,
                        ModSettingsMirrorSource.RuntimeInterop);

                    addedAny = true;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RuntimeInteropMirrorSource] Register failed for {schema.ModId}::{pageSchema.PageId}: {ex.Message}");
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
            var label = entry.Label;
            var description = entry.Description;
            var dataKey = $"interop::{entry.Key}";
            var defaultWriteKey = $"{modId}::{entry.Scope}::{entry.Key}";

            switch (entry.Type)
            {
                case InteropEntryType.Header:
                    section.AddHeader(entry.Id, label, description);
                    ApplyEntryVisibilityHook(section, entry, access);
                    return;
                case InteropEntryType.Paragraph:
                    section.AddParagraph(entry.Id, label, description, entry.MaxBodyHeight);
                    ApplyEntryVisibilityHook(section, entry, access);
                    return;
                case InteropEntryType.Subpage:
                    if (string.IsNullOrWhiteSpace(entry.TargetPageId))
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[RuntimeInteropMirrorSource] Skipping subpage entry '{entry.Id}' because targetPageId is missing.");
                        return;
                    }

                    section.AddSubpage(entry.Id, label, entry.TargetPageId,
                        entry.ButtonText,
                        description);
                    ApplyEntryVisibilityHook(section, entry, access);
                    return;
                case InteropEntryType.Toggle:
                {
                    var binding = ModSettingsBindings.Callback(modId, dataKey,
                        () => ReadBoolWithDefault(defaultWriteKey, entry, access, saveAction),
                        value => WriteBool(entry.Key, value, access),
                        saveAction,
                        entry.Scope);
                    section.AddToggle(entry.Id, label, binding, description);
                    ApplyEntryVisibilityHook(section, entry, access);
                    return;
                }
                case InteropEntryType.Slider:
                {
                    var binding = ModSettingsBindings.Callback(modId, dataKey,
                        () => ReadDoubleWithDefault(defaultWriteKey, entry, access, saveAction),
                        value => WriteDouble(entry.Key, value, access),
                        saveAction,
                        entry.Scope);
                    section.AddSlider(entry.Id, label, binding, entry.Min, entry.Max, entry.Step, null, description);
                    ApplyEntryVisibilityHook(section, entry, access);
                    return;
                }
                case InteropEntryType.IntSlider:
                {
                    var binding = ModSettingsBindings.Callback(modId, dataKey,
                        () => ReadIntWithDefault(defaultWriteKey, entry, access, saveAction),
                        value => WriteInt(entry.Key, value, access),
                        saveAction,
                        entry.Scope);
                    section.AddIntSlider(entry.Id, label, binding, (int)Math.Round(entry.Min),
                        (int)Math.Round(entry.Max),
                        Math.Max(1, (int)Math.Round(entry.Step)), null, description);
                    ApplyEntryVisibilityHook(section, entry, access);
                    return;
                }
                case InteropEntryType.String:
                {
                    var binding = ModSettingsBindings.Callback(modId, dataKey,
                        () => ReadStringWithDefault(defaultWriteKey, entry, access, saveAction),
                        value => WriteString(entry.Key, value, access),
                        saveAction,
                        entry.Scope);
                    section.AddString(entry.Id, label, binding, entry.Placeholder, NormalizeMaxLength(entry.MaxLength),
                        description);
                    ApplyEntryVisibilityHook(section, entry, access);
                    return;
                }
                case InteropEntryType.MultilineString:
                {
                    var binding = ModSettingsBindings.Callback(modId, dataKey,
                        () => ReadStringWithDefault(defaultWriteKey, entry, access, saveAction),
                        value => WriteString(entry.Key, value, access),
                        saveAction,
                        entry.Scope);
                    section.AddMultilineString(entry.Id, label, binding, entry.Placeholder,
                        NormalizeMaxLength(entry.MaxLength), description);
                    ApplyEntryVisibilityHook(section, entry, access);
                    return;
                }
                case InteropEntryType.Color:
                {
                    var binding = ModSettingsBindings.Callback(modId, dataKey,
                        () => ReadStringWithDefault(defaultWriteKey, entry, access, saveAction),
                        value => WriteString(entry.Key, value, access),
                        saveAction,
                        entry.Scope);
                    var editAlpha = entry.EditAlpha ?? true;
                    var editIntensity = entry.EditIntensity ?? false;
                    section.AddColor(entry.Id, label, binding, description, editAlpha, editIntensity);
                    ApplyEntryVisibilityHook(section, entry, access);
                    return;
                }
                case InteropEntryType.KeyBinding:
                {
                    var binding = ModSettingsBindings.Callback(modId, dataKey,
                        () => ReadStringWithDefault(defaultWriteKey, entry, access, saveAction),
                        value => WriteString(entry.Key, value, access),
                        saveAction,
                        entry.Scope);
                    var allowModifierCombos = entry.AllowModifierCombos ?? true;
                    var allowModifierOnly = entry.AllowModifierOnly ?? true;
                    var distinguishSides = entry.DistinguishModifierSides ?? false;
                    section.AddKeyBinding(entry.Id, label, binding, allowModifierCombos, allowModifierOnly,
                        distinguishSides, description);
                    ApplyEntryVisibilityHook(section, entry, access);
                    return;
                }
                case InteropEntryType.InfoCard:
                {
                    var bodyText = entry.Body ?? ModSettingsText.Literal(string.Empty);
                    section.AddInfoCard(entry.Id, label, bodyText, description);
                    ApplyEntryVisibilityHook(section, entry, access);
                    return;
                }
                case InteropEntryType.RuntimeHotkeySummary:
                {
                    if (entry.HotkeyBindings.Count == 0)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[RuntimeInteropMirrorSource] Skipping runtime-hotkey-summary entry '{entry.Id}' because bindings is empty.");
                        return;
                    }

                    var bodyText = entry.Body ?? ModSettingsText.Literal(string.Empty);
                    var bindingChips = entry.HotkeyBindings.ToArray();
                    var idSuffix = entry.SummaryIdSuffix;
                    section.AddRuntimeHotkeySummary(entry.Id, label, bodyText, bindingChips, idSuffix);
                    ApplyEntryVisibilityHook(section, entry, access);
                    return;
                }
                case InteropEntryType.Choice:
                {
                    var optionsSource = ResolveChoiceOptions(entry, access);
                    if (optionsSource.Count == 0)
                        return;
                    var options = optionsSource
                        .Select(o => new ModSettingsChoiceOption<string>(o.Value, o.Label))
                        .ToArray();
                    var firstValue = options[0].Value;
                    var binding = ModSettingsBindings.Callback(modId, dataKey,
                        () => ReadChoiceWithDefault(defaultWriteKey, entry, options, firstValue, access,
                            saveAction),
                        value => WriteString(entry.Key, string.IsNullOrWhiteSpace(value) ? firstValue : value, access),
                        saveAction,
                        entry.Scope);
                    section.AddChoice(entry.Id, label, binding, options, description,
                        entry.ChoicePresentation == "dropdown"
                            ? ModSettingsChoicePresentation.Dropdown
                            : ModSettingsChoicePresentation.Stepper);
                    ApplyEntryVisibilityHook(section, entry, access);
                    return;
                }
                case InteropEntryType.Button:
                {
                    var buttonText = entry.ButtonText ?? label;
                    section.AddButton(entry.Id, label, buttonText, () => access.InvokeAction(entry.Key),
                        entry.ButtonTone, description);
                    ApplyEntryVisibilityHook(section, entry, access);
                    return;
                }
            }
        }

        private static void ApplyEntryVisibilityHook(
            ModSettingsSectionBuilder section,
            InteropEntry entry,
            InteropAccessor access)
        {
            if (string.IsNullOrWhiteSpace(entry.VisibleWhenMethod))
                return;

            section.WithEntryVisibleWhen(entry.Id, () => EvaluateVisibleWhen(entry, access));
        }

        private static bool EvaluateVisibleWhen(InteropEntry entry, InteropAccessor access)
        {
            if (!TryResolveInteropMethod(access.ProviderType, entry.VisibleWhenMethod, out var method))
                return true;

            try
            {
                var result = method.GetParameters().Length == 0
                    ? FastMethodInvoker.InvokeStatic0(method)
                    : FastMethodInvoker.InvokeStatic1(method, entry.Key);
                return result is bool visible ? visible : CoerceBool(result);
            }
            catch (Exception ex)
            {
                WarnInteropMethodOnce(access.ProviderType,
                    $"visibleWhenMethod '{entry.VisibleWhenMethod}' for entry '{entry.Id}' failed: {ex.Message}");
                return true;
            }
        }

        private static List<InteropChoiceOption> ResolveChoiceOptions(InteropEntry entry, InteropAccessor access)
        {
            if (string.IsNullOrWhiteSpace(entry.OptionsMethod) ||
                !TryResolveInteropMethod(access.ProviderType, entry.OptionsMethod, out var method))
                return entry.Options;

            try
            {
                var raw = method.GetParameters().Length == 0
                    ? FastMethodInvoker.InvokeStatic0(method)
                    : FastMethodInvoker.InvokeStatic1(method, entry.Key);
                var parsed = ParseOptionsFromRaw(raw);
                if (parsed.Count > 0)
                    return parsed;

                WarnInteropMethodOnce(access.ProviderType,
                    $"optionsMethod '{entry.OptionsMethod}' for entry '{entry.Id}' returned no valid options; falling back to static options.");
                return entry.Options;
            }
            catch (Exception ex)
            {
                WarnInteropMethodOnce(access.ProviderType,
                    $"optionsMethod '{entry.OptionsMethod}' for entry '{entry.Id}' failed: {ex.Message}");
                return entry.Options;
            }
        }

        private static List<InteropChoiceOption> ParseOptionsFromRaw(object? raw)
        {
            if (raw is not IEnumerable enumerable || raw is string)
                return [];

            var options = new List<InteropChoiceOption>();
            foreach (var optionRaw in enumerable.Cast<object?>())
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
                    options.Add(new(value, ModSettingsText.Literal(label)));
                    continue;
                }

                var str = optionRaw.ToString();
                if (string.IsNullOrWhiteSpace(str))
                    continue;
                options.Add(new(str, ModSettingsText.Literal(str)));
            }

            return options;
        }

        private static bool TryResolveInteropMethod(Type providerType, string? rawMethodName, out MethodInfo method)
        {
            method = null!;
            if (string.IsNullOrWhiteSpace(rawMethodName))
                return false;

            var candidate = rawMethodName.Trim();
            var methodName = ExtractMethodName(providerType, candidate);
            if (methodName == null)
                return false;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            method = providerType.GetMethod(methodName, flags, Type.EmptyTypes)
                     ?? providerType.GetMethod(methodName, flags, [typeof(string)])!;
            if (method == null)
            {
                WarnInteropMethodOnce(providerType, $"Method '{candidate}' was not found.");
                return false;
            }

            var ps = method.GetParameters();
            switch (ps.Length)
            {
                case 0:
                case 1 when ps[0].ParameterType == typeof(string):
                    return true;
                default:
                    WarnInteropMethodOnce(providerType,
                        $"Method '{candidate}' has unsupported signature. Allowed: {methodName}() or {methodName}(string).");
                    return false;
            }
        }

        private static string? ExtractMethodName(Type providerType, string candidate)
        {
            if (!candidate.Contains('.'))
                return candidate;

            var qualifiedPrefix = providerType.FullName + ".";
            var shortPrefix = providerType.Name + ".";
            if (candidate.StartsWith(qualifiedPrefix, StringComparison.Ordinal))
                return candidate[qualifiedPrefix.Length..];
            if (candidate.StartsWith(shortPrefix, StringComparison.Ordinal))
                return candidate[shortPrefix.Length..];

            WarnInteropMethodOnce(providerType,
                $"Method '{candidate}' is outside provider type '{providerType.FullName}'. Only provider-owned static methods are allowed.");
            return null;
        }

        private static void WarnInteropMethodOnce(Type providerType, string message)
        {
            var key = $"{providerType.FullName}::{message}";
            if (!InteropMethodWarningDedup.Add(key))
                return;
            RitsuLibFramework.Logger.Warn($"[RuntimeInteropMirrorSource] {message}");
        }

        private static bool ReadBool(string key, InteropAccessor access)
        {
            return access.GetBool?.Invoke(key) ?? CoerceBool(access.GetObject(key));
        }

        private static bool ReadBoolWithDefault(
            string defaultWriteKey,
            InteropEntry entry,
            InteropAccessor access,
            Action saveAction)
        {
            if (!IsMissing(entry.Key, access))
                return ReadBool(entry.Key, access);

            if (!TryCoerceDefaultBool(entry.DefaultValue, out var dv))
                return false;

            EnsureDefaultWritten(defaultWriteKey, () => WriteBool(entry.Key, dv, access), saveAction);
            return dv;
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

        private static double ReadDoubleWithDefault(
            string defaultWriteKey,
            InteropEntry entry,
            InteropAccessor access,
            Action saveAction)
        {
            if (!IsMissing(entry.Key, access))
                return ReadDouble(entry.Key, access);

            if (!TryCoerceDefaultDouble(entry.DefaultValue, out var dv))
                return 0d;

            EnsureDefaultWritten(defaultWriteKey, () => WriteDouble(entry.Key, dv, access), saveAction);
            return dv;
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

        private static int ReadIntWithDefault(
            string defaultWriteKey,
            InteropEntry entry,
            InteropAccessor access,
            Action saveAction)
        {
            if (!IsMissing(entry.Key, access))
                return ReadInt(entry.Key, access);

            if (!TryCoerceDefaultInt(entry.DefaultValue, out var dv))
                return 0;

            EnsureDefaultWritten(defaultWriteKey, () => WriteInt(entry.Key, dv, access), saveAction);
            return dv;
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

        private static string ReadStringWithDefault(
            string defaultWriteKey,
            InteropEntry entry,
            InteropAccessor access,
            Action saveAction)
        {
            if (!IsMissing(entry.Key, access))
                return ReadString(entry.Key, access);

            if (!TryCoerceDefaultString(entry.DefaultValue, out var dv))
                return string.Empty;

            EnsureDefaultWritten(defaultWriteKey, () => WriteString(entry.Key, dv, access), saveAction);
            return dv;
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

        private static string ReadChoiceWithDefault(
            string defaultWriteKey,
            InteropEntry entry,
            ModSettingsChoiceOption<string>[] options,
            string firstValue,
            InteropAccessor access,
            Action saveAction)
        {
            if (!IsMissing(entry.Key, access))
            {
                var current = ReadString(entry.Key, access);
                return string.IsNullOrWhiteSpace(current) ? firstValue : current;
            }

            var chosen = firstValue;
            if (TryCoerceDefaultString(entry.DefaultValue, out var dv) &&
                options.Any(o => string.Equals(o.Value, dv, StringComparison.OrdinalIgnoreCase)))
                chosen = dv;

            EnsureDefaultWritten(defaultWriteKey, () => WriteString(entry.Key, chosen, access), saveAction);
            return chosen;
        }

        private static bool IsMissing(string key, InteropAccessor access)
        {
            try
            {
                return access.GetObject(key) == null;
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureDefaultWritten(string dedupKey, Action writeDefault, Action saveAction)
        {
            lock (DefaultWriteGate)
            {
                if (!DefaultWriteDedup.Add(dedupKey))
                    return;
            }

            try
            {
                writeDefault();
                saveAction();
            }
            catch
            {
                // If write/save fails, allow retry later.
                lock (DefaultWriteGate)
                {
                    DefaultWriteDedup.Remove(dedupKey);
                }
            }
        }

        private static bool TryCoerceDefaultBool(object? raw, out bool value)
        {
            value = false;
            try
            {
                switch (raw)
                {
                    case null:
                        return false;
                    case bool b:
                        value = b;
                        return true;
                    case string s when bool.TryParse(s, out var parsed):
                        value = parsed;
                        return true;
                    default:
                        value = Convert.ToBoolean(raw);
                        return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool TryCoerceDefaultDouble(object? raw, out double value)
        {
            value = 0d;
            try
            {
                switch (raw)
                {
                    case null:
                        return false;
                    case double d:
                        value = d;
                        return true;
                    case float f:
                        value = f;
                        return true;
                    case int i:
                        value = i;
                        return true;
                    case long l:
                        value = l;
                        return true;
                    default:
                        value = Convert.ToDouble(raw);
                        return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool TryCoerceDefaultInt(object? raw, out int value)
        {
            value = 0;
            try
            {
                switch (raw)
                {
                    case null:
                        return false;
                    case int i:
                        value = i;
                        return true;
                    case long l:
                        value = (int)l;
                        return true;
                    case double d:
                        value = (int)Math.Round(d);
                        return true;
                    case float f:
                        value = (int)Math.Round(f);
                        return true;
                    default:
                        value = Convert.ToInt32(raw);
                        return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool TryCoerceDefaultString(object? raw, out string value)
        {
            value = "";
            if (raw == null)
                return false;
            try
            {
                var s = raw.ToString();
                if (string.IsNullOrWhiteSpace(s))
                    return false;
                value = s.Trim();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static InteropAccessor BuildAccessor(Type providerType)
        {
            ReflectionStaticChannel channel;
            try
            {
                channel = ReflectionStaticChannelBinder.Bind(providerType,
                    ReflectionInteropConvention.SettingsRuntimeInterop);
            }
            catch (InvalidOperationException ex)
            {
                throw ModSettingsMirrorDiagnostics.InvalidConfig(
                    $"Provider {providerType.FullName} requires static {ResolverGetMethodName}(string) and {ResolverSetMethodName}(string, object). ({ex.Message})");
            }

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

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

            return new(
                channel.ProviderType,
                channel.GetObject,
                channel.SetObject,
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

        private static bool TryParseSchema(
            IDictionary<string, object?> root,
            Type providerType,
            out InteropSchemaRoot schema)
        {
            schema = null!;
            if (!TryGetString(root, "modId", out var modId) || string.IsNullOrWhiteSpace(modId))
                return false;

            var i18N = TryResolveI18N(root, providerType, modId);
            var modDisplayName = TryGetText(root, "modDisplayName", modId, i18N, out var mdn) ? mdn : null;
            var modSidebarOrder = TryGetInt(root, "modSidebarOrder", out var mso) ? mso : null;

            var pages = new List<InteropPage>();
            if (TryGetEnumerable(root, "pages", out var pagesRaw))
                foreach (var pageRaw in pagesRaw)
                {
                    if (pageRaw == null || !TryAsMap(pageRaw, out var pageMap))
                        continue;
                    var pageI18N = TryResolveI18NOverride(pageMap, providerType, modId, i18N);
                    if (!TryParsePage(pageMap, providerType, modId, pageI18N, out var page))
                        continue;
                    pages.Add(page);
                }
            else if (TryParseLegacySinglePage(root, providerType, modId, i18N, out var legacyPage))
                pages.Add(legacyPage);

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
            var read = FileOperations.ReadText(trimmed, "RuntimeInteropMirrorSource");
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

        private static bool TryParseLegacySinglePage(
            IDictionary<string, object?> root,
            Type providerType,
            string modId,
            I18N? i18N,
            out InteropPage page)
        {
            page = null!;
            var pageId = TryGetString(root, "pageId", out var p) && !string.IsNullOrWhiteSpace(p)
                ? p
                : "interop";
            var title = ReadTextOrDefault(root, "title", "Settings", i18N);
            var description = ReadTextOrNull(root, "description", "", i18N);
            var sortOrder = TryGetInt(root, "sortOrder", out var so) ? so ?? 10_040 : 10_040;

            if (!TryGetEnumerable(root, "sections", out var sectionsRaw))
                return false;

            var sections = new List<InteropSection>();
            foreach (var sectionRaw in sectionsRaw)
            {
                if (sectionRaw == null || !TryAsMap(sectionRaw, out var sectionMap))
                    continue;
                var sectionI18N = TryResolveI18NOverride(sectionMap, providerType, modId, i18N);
                if (!TryParseSection(sectionMap, providerType, modId, sectionI18N, out var section))
                    continue;
                sections.Add(section);
            }

            if (sections.Count == 0)
                return false;

            page = new(pageId, null, title, description, sortOrder, sections);
            return true;
        }

        private static bool TryParsePage(
            IDictionary<string, object?> map,
            Type providerType,
            string modId,
            I18N? i18N,
            out InteropPage page)
        {
            page = null!;
            var pageId = TryGetString(map, "pageId", out var p) && !string.IsNullOrWhiteSpace(p)
                ? p
                : "interop";
            var title = ReadTextOrDefault(map, "title", "Settings", i18N);
            var description = ReadTextOrNull(map, "description", "", i18N);
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
                var sectionI18N = TryResolveI18NOverride(sectionMap, providerType, modId, i18N);
                if (!TryParseSection(sectionMap, providerType, modId, sectionI18N, out var section))
                    continue;
                sections.Add(section);
            }

            if (sections.Count == 0)
                return false;

            page = new(pageId, parentPageId, title, description, sortOrder, sections);
            return true;
        }

        private static bool TryParseSection(
            IDictionary<string, object?> map,
            Type providerType,
            string modId,
            I18N? i18N,
            out InteropSection section)
        {
            section = null!;
            if (!TryGetString(map, "id", out var id) || string.IsNullOrWhiteSpace(id))
                return false;

            var title = ReadTextOrNull(map, "title", "", i18N);
            var description = ReadTextOrNull(map, "description", "", i18N);
            if (!TryGetEnumerable(map, "entries", out var entriesRaw))
                return false;

            var entries = new List<InteropEntry>();
            foreach (var entryRaw in entriesRaw)
            {
                if (entryRaw == null || !TryAsMap(entryRaw, out var entryMap))
                    continue;
                var entryI18N = TryResolveI18NOverride(entryMap, providerType, modId, i18N);
                if (!TryParseEntry(entryMap, providerType, modId, entryI18N, out var entry))
                    continue;
                entries.Add(entry);
            }

            if (entries.Count == 0)
                return false;

            section = new(id, title, description, entries);
            return true;
        }

        private static bool TryParseEntry(
            IDictionary<string, object?> map,
            Type providerType,
            string modId,
            I18N? i18N,
            out InteropEntry entry)
        {
            entry = null!;
            if (!TryGetString(map, "id", out var id) || string.IsNullOrWhiteSpace(id))
                return false;
            if (!TryGetString(map, "type", out var typeName) || !TryParseEntryType(typeName, out var type))
                return false;

            var key = TryGetString(map, "key", out var k) && !string.IsNullOrWhiteSpace(k) ? k : id;
            var label = ReadTextOrDefault(map, "label", id, i18N);
            var description = ReadTextOrNull(map, "description", "", i18N);
            var buttonText = ReadTextOrNull(map, "buttonText", "", i18N);
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
            var options = ParseOptions(map, providerType, modId, i18N);
            var visibleWhenMethod = TryGetString(map, "visibleWhenMethod", out var visibleWhenMethodRaw)
                ? visibleWhenMethodRaw
                : null;
            var optionsMethod = TryGetString(map, "optionsMethod", out var optionsMethodRaw)
                ? optionsMethodRaw
                : null;
            var body = ReadTextOrNull(map, "body", "", i18N);
            var placeholder = ReadTextOrNull(map, "placeholder", "", i18N);
            var editAlpha = TryGetNullableBool(map, "editAlpha");
            var editIntensity = TryGetNullableBool(map, "editIntensity");
            var allowModifierCombos = TryGetNullableBool(map, "allowModifierCombos");
            var allowModifierOnly = TryGetNullableBool(map, "allowModifierOnly");
            var distinguishModifierSides = TryGetNullableBool(map, "distinguishModifierSides");
            var hotkeyBindings = ParseTextList(map, "bindings", i18N);
            var summaryIdSuffix = ReadTextOrNull(map, "idSuffix", "", i18N);
            map.TryGetValue("defaultValue", out var defaultValue);

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
                presentation,
                body,
                editAlpha,
                editIntensity,
                allowModifierCombos,
                allowModifierOnly,
                distinguishModifierSides,
                placeholder,
                hotkeyBindings,
                summaryIdSuffix,
                defaultValue,
                visibleWhenMethod,
                optionsMethod);
            return true;
        }

        private static int? NormalizeMaxLength(int? maxLength)
        {
            return maxLength is >= 1 ? maxLength : null;
        }

        private static bool? TryGetNullableBool(IDictionary<string, object?> map, string key)
        {
            if (!map.TryGetValue(key, out var raw) || raw == null)
                return null;

            try
            {
                return raw switch
                {
                    bool b => b,
                    string s when bool.TryParse(s, out var pb) => pb,
                    int i => i != 0,
                    long l => l != 0,
                    double d => Math.Abs(d) > double.Epsilon,
                    _ => null,
                };
            }
            catch
            {
                return null;
            }
        }

        private static List<ModSettingsText> ParseTextList(IDictionary<string, object?> map, string key, I18N? i18N)
        {
            var list = new List<ModSettingsText>();
            if (!TryGetEnumerable(map, key, out var raw))
                return list;

            list.AddRange(from item in raw
                let fallback = item?.ToString() ?? string.Empty
                select ReadTextFromRaw(item, fallback, i18N)
                into text
                where !string.IsNullOrWhiteSpace(text.Resolve())
                select text);

            return list;
        }

        private static SaveScope ParseScope(string? value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                "profile" => SaveScope.Profile,
                "inmemory" or "in-memory" or "in_memory" => SaveScope.InMemory,
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

        private static List<InteropChoiceOption> ParseOptions(
            IDictionary<string, object?> entryMap,
            Type providerType,
            string modId,
            I18N? i18N)
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
                    var optionI18N = TryResolveI18NOverride(optionMap, providerType, modId, i18N);
                    optionMap.TryGetValue("label", out var labelRaw);
                    var label = ReadTextFromRaw(labelRaw, value, optionI18N);
                    options.Add(new(value, label));
                    continue;
                }

                var str = optionRaw.ToString();
                if (string.IsNullOrWhiteSpace(str))
                    continue;
                options.Add(new(str, ModSettingsText.Literal(str)));
            }

            return options;
        }

        private static ModSettingsText ReadTextOrDefault(
            IDictionary<string, object?> map,
            string key,
            string fallback,
            I18N? i18N)
        {
            map.TryGetValue(key, out var raw);
            return ReadTextFromRaw(raw, fallback, i18N);
        }

        private static ModSettingsText? ReadTextOrNull(
            IDictionary<string, object?> map,
            string key,
            string fallback,
            I18N? i18N)
        {
            if (!map.TryGetValue(key, out var raw) || raw == null)
                return null;

            var text = ReadTextFromRaw(raw, fallback, i18N);
            return string.IsNullOrWhiteSpace(text.Resolve()) ? null : text;
        }

        private static bool TryGetText(
            IDictionary<string, object?> map,
            string key,
            string fallback,
            I18N? i18N,
            out ModSettingsText text)
        {
            if (!map.TryGetValue(key, out var raw) || raw == null)
            {
                text = null!;
                return false;
            }

            text = ReadTextFromRaw(raw, fallback, i18N);
            return true;
        }

        private static ModSettingsText ReadTextFromRaw(object? raw, string fallback, I18N? i18N)
        {
            fallback ??= string.Empty;

            return raw switch
            {
                null => ModSettingsText.Literal(fallback),
                string s => ModSettingsText.Literal(string.IsNullOrWhiteSpace(s) ? fallback : s),
                IDictionary dict => BuildTextFromMap(dict, fallback, i18N),
                _ => ModSettingsText.Literal(raw.ToString() ?? fallback),
            };
        }

        private static ModSettingsText BuildTextFromMap(IDictionary map, string fallback, I18N? i18N)
        {
            var fb = fallback;
            var dict = map;

            if (dict.Contains("locString") && dict["locString"] is { } locObj && TryAsMap(locObj, out var locMap))
            {
                var table = TryGetString(locMap, "table", out var t) && !string.IsNullOrWhiteSpace(t)
                    ? t.Trim()
                    : "settings_ui";
                var key = TryGetString(locMap, "key", out var k) && !string.IsNullOrWhiteSpace(k) ? k.Trim() : "";
                var locFallback = TryGetString(locMap, "fallback", out var lf) ? lf : fb;
                return string.IsNullOrWhiteSpace(key)
                    ? ModSettingsText.Literal(locFallback)
                    : ModSettingsText.LocString(table, key, locFallback);
            }

            if (dict.Contains("i18n") && dict["i18n"] is { } i18NObj && TryAsMap(i18NObj, out var i18NMap))
            {
                var key = TryGetString(i18NMap, "key", out var k) && !string.IsNullOrWhiteSpace(k) ? k.Trim() : "";
                var innerFallback = TryGetString(i18NMap, "fallback", out var ifb) ? ifb : null;
                var outerFallback = dict.Contains("fallback") && dict["fallback"] is string ofb ? ofb : null;
                var resolvedFallback = innerFallback ?? outerFallback ?? fb;

                if (string.IsNullOrWhiteSpace(key) || i18N == null)
                    return ModSettingsText.Literal(resolvedFallback);

                return ModSettingsText.I18N(i18N, key, resolvedFallback);
            }

            if (!dict.Contains("langMap") || dict["langMap"] is not IDictionary langMap)
                return ModSettingsText.Dynamic(() =>
                    ModSettingsMirrorTextPolicy.ResolveLangMap(dict, fb, TryResolveCurrentLang));
            {
                var outerFallback = dict.Contains("fallback") && dict["fallback"] is string ofb ? ofb : null;
                var resolvedFallback = outerFallback ?? fb;
                return ModSettingsText.Dynamic(() =>
                    ModSettingsMirrorTextPolicy.ResolveLangMap(langMap, resolvedFallback, TryResolveCurrentLang));
            }
        }

        private static I18N? TryResolveI18N(IDictionary<string, object?> root, Type providerType, string modId)
        {
            if (!root.TryGetValue("i18nSource", out var rawSource) || rawSource == null)
                return null;

            if (!TryAsMap(rawSource, out var map))
                return null;

            var instanceName = TryGetString(map, "instanceName", out var name) && !string.IsNullOrWhiteSpace(name)
                ? name.Trim()
                : $"{modId}-Settings-I18N";

            var fsFolders = TryGetStringArray(map, "fsFolders");
            var resourceFolders = TryGetStringArray(map, "resourceFolders");
            var pckFolders = TryGetStringArray(map, "pckFolders");

            var resourceAssembly = ResolveResourceAssembly(map, providerType.Assembly) ?? providerType.Assembly;

            if (fsFolders.Length == 0 && resourceFolders.Length == 0 && pckFolders.Length == 0)
                return null;

            return new(
                instanceName,
                fsFolders,
                resourceFolders,
                pckFolders,
                resourceAssembly);
        }

        private static I18N? TryResolveI18NOverride(
            IDictionary<string, object?> node,
            Type providerType,
            string modId,
            I18N? inherited)
        {
            if (!node.ContainsKey("i18nSource"))
                return inherited;

            // If i18nSource is explicitly present but invalid/empty, treat it as "disable i18n here".
            var resolved = TryResolveI18N(node, providerType, modId);
            return resolved;
        }

        private static Assembly? ResolveResourceAssembly(IDictionary<string, object?> map, Assembly providerAssembly)
        {
            if (!TryGetString(map, "resourceAssembly", out var name) || string.IsNullOrWhiteSpace(name))
                return null;

            var trimmed = name.Trim();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                try
                {
                    var asmName = asm.GetName().Name;
                    if (string.Equals(asmName, trimmed, StringComparison.OrdinalIgnoreCase))
                        return asm;
                }
                catch
                {
                    // ignored
                }

            return providerAssembly;
        }

        private static string[] TryGetStringArray(IDictionary<string, object?> map, string key)
        {
            if (!TryGetEnumerable(map, key, out var raw))
                return [];

            return raw
                .Select(static v => v?.ToString())
                .Where(static s => !string.IsNullOrWhiteSpace(s))
                .Select(static s => s!.Trim())
                .ToArray();
        }

        private static string TryResolveCurrentLang()
        {
            return I18N.ResolveCurrentLanguageCode();
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
                "multiline-string" or "multiline" => InteropEntryType.MultilineString,
                "color" => InteropEntryType.Color,
                "key-binding" or "keybinding" => InteropEntryType.KeyBinding,
                "info-card" or "infocard" => InteropEntryType.InfoCard,
                "runtime-hotkey-summary" or "runtimehotkeysummary" or "hotkey-summary" =>
                    InteropEntryType.RuntimeHotkeySummary,
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
                    // ignored
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
            ModSettingsText? ModDisplayName,
            int? ModSidebarOrder,
            List<InteropPage> Pages);

        private sealed record InteropPage(
            string PageId,
            string? ParentPageId,
            ModSettingsText Title,
            ModSettingsText? Description,
            int SortOrder,
            List<InteropSection> Sections);

        private sealed record InteropSection(
            string Id,
            ModSettingsText? Title,
            ModSettingsText? Description,
            List<InteropEntry> Entries);

        private sealed record InteropEntry(
            string Id,
            InteropEntryType Type,
            string Key,
            ModSettingsText Label,
            ModSettingsText? Description,
            double Min,
            double Max,
            double Step,
            int? MaxLength,
            float? MaxBodyHeight,
            List<InteropChoiceOption> Options,
            ModSettingsText? ButtonText,
            string? TargetPageId,
            ModSettingsButtonTone ButtonTone,
            SaveScope Scope,
            string ChoicePresentation,
            ModSettingsText? Body,
            bool? EditAlpha,
            bool? EditIntensity,
            bool? AllowModifierCombos,
            bool? AllowModifierOnly,
            bool? DistinguishModifierSides,
            ModSettingsText? Placeholder,
            List<ModSettingsText> HotkeyBindings,
            ModSettingsText? SummaryIdSuffix,
            object? DefaultValue,
            string? VisibleWhenMethod,
            string? OptionsMethod);

        private sealed record InteropChoiceOption(string Value, ModSettingsText Label);

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
            MultilineString,
            Color,
            KeyBinding,
            InfoCard,
            RuntimeHotkeySummary,
            Button,
        }

        private sealed record InteropAccessor(
            Type ProviderType,
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
