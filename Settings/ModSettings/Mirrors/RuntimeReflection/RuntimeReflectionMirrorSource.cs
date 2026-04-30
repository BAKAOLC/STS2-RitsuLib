using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    internal static class RuntimeReflectionMirrorSource
    {
        private const string ProviderTypeMetadataKey = "RitsuLib.ModSettingsReflection.ProviderType";
        private const string DefaultLocTable = "settings_ui";
        private static readonly Lock Gate = new();
        [ThreadStatic] private static string? _currentReflectionModId;
        [ThreadStatic] private static I18N? _currentReflectionI18N;
        [ThreadStatic] private static Type? _currentProviderType;
        [ThreadStatic] private static object? _currentProviderInstance;

        private static readonly Dictionary<string, string?>
            RuntimeRegisteredProviderTypes = new(StringComparer.Ordinal);

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
                if (providers.Count == 0)
                    return 0;

                var added = 0;
                foreach (var provider in providers)
                    try
                    {
                        if (!TryCreateMirror(provider, out var page))
                            continue;
                        if (!ModSettingsMirrorInteropPolicy.ShouldMirror(ModSettingsMirrorSource.RuntimeReflection,
                                page.ModId, provider))
                            continue;

                        if (!ModSettingsMirrorRegistrar.TryRegister(page, ModSettingsMirrorSource.RuntimeReflection))
                            continue;
                        added++;
                    }
                    catch (Exception ex)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[RuntimeReflectionMirrorSource] Register failed for '{provider.FullName}': {ex.Message}");
                    }

                return added;
            }
        }

        private static List<Type> DiscoverProviders()
        {
            var providers = new List<Type>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var providerType in ReadProviderTypeNames(asm)
                             .Select(typeName => asm.GetType(typeName, false)))
                {
                    if (providerType?.GetCustomAttribute<ModSettingsPageAttribute>() == null)
                        continue;
                    if (!seen.Add(providerType.FullName ?? providerType.Name))
                        continue;
                    providers.Add(providerType);
                }

                foreach (var type in SafeGetTypes(asm))
                {
                    if (type.GetCustomAttribute<ModSettingsPageAttribute>() == null)
                        continue;
                    if (!seen.Add(type.FullName ?? type.Name))
                        continue;
                    providers.Add(type);
                }
            }

            foreach (var (providerTypeName, assemblyName) in RuntimeRegisteredProviderTypes)
            {
                var providerType = ResolveProviderType(providerTypeName, assemblyName);
                if (providerType?.GetCustomAttribute<ModSettingsPageAttribute>() == null)
                    continue;
                if (!seen.Add(providerType.FullName ?? providerType.Name))
                    continue;
                providers.Add(providerType);
            }

            return providers;
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch
            {
                return [];
            }
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

        private static Type? ResolveProviderType(string providerTypeName, string? assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
                return AppDomain.CurrentDomain.GetAssemblies()
                    .Select(asm => asm.GetType(providerTypeName, false))
                    .OfType<Type>()
                    .FirstOrDefault();

            return (from asm in AppDomain.CurrentDomain.GetAssemblies()
                where string.Equals(asm.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase)
                select asm.GetType(providerTypeName, false)).FirstOrDefault();
        }

        private static bool TryCreateMirror(Type providerType, out ModSettingsMirrorPageDefinition page)
        {
            page = null!;
            var pageAttr = providerType.GetCustomAttribute<ModSettingsPageAttribute>();
            if (pageAttr == null || string.IsNullOrWhiteSpace(pageAttr.ModId))
                return false;

            var pageId = string.IsNullOrWhiteSpace(pageAttr.PageId) ? pageAttr.ModId : pageAttr.PageId.Trim();
            if (ModSettingsRegistry.TryGetPage(pageAttr.ModId, pageId, out _))
                return false;

            if (!TryCreateProviderInstance(providerType, out var instance))
                return false;

            var previousModId = _currentReflectionModId;
            var previousI18N = _currentReflectionI18N;
            var previousProviderType = _currentProviderType;
            var previousProviderInstance = _currentProviderInstance;
            _currentReflectionModId = pageAttr.ModId;
            _currentProviderType = providerType;
            _currentProviderInstance = instance;
            _currentReflectionI18N = ResolvePageI18NProvider(providerType, instance, pageAttr);
            try
            {
                var sections = new Dictionary<string, MutableSection>(StringComparer.OrdinalIgnoreCase);
                foreach (var sectionAttr in providerType.GetCustomAttributes<ModSettingsSectionAttribute>())
                    sections[sectionAttr.Id] = new(sectionAttr);

                AppendPropertyEntries(providerType, instance, pageAttr.ModId, sections);
                AppendMethodEntries(providerType, instance, pageAttr.ModId, sections);

                var realizedSections = sections.Values
                    .Where(static section => section.Entries.Count > 0)
                    .OrderBy(static section => section.SortOrder)
                    .ThenBy(static section => section.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(static section => section.Build())
                    .ToArray();
                if (realizedSections.Length == 0)
                    return false;

                page = new(
                    pageAttr.ModId,
                    pageId,
                    pageAttr.SortOrder,
                    realizedSections,
                    ToTextOrNull(pageAttr, nameof(pageAttr.Title), pageAttr.Title),
                    ToTextOrNull(pageAttr, nameof(pageAttr.Description), pageAttr.Description),
                    ToTextOrNull(pageAttr, nameof(pageAttr.ModDisplayName), pageAttr.ModDisplayName),
                    pageAttr.ModSidebarOrder,
                    pageAttr.ParentPageId);
                return true;
            }
            finally
            {
                _currentReflectionModId = previousModId;
                _currentReflectionI18N = previousI18N;
                _currentProviderType = previousProviderType;
                _currentProviderInstance = previousProviderInstance;
            }
        }

        private static I18N? ResolvePageI18NProvider(Type providerType, object? instance,
            ModSettingsPageAttribute pageAttr)
        {
            if (string.IsNullOrWhiteSpace(pageAttr.I18NProviderUsing))
                return null;

            var methodName = pageAttr.I18NProviderUsing.Trim();
            var method = providerType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException(
                    $"I18N provider method '{methodName}' was not found on '{providerType.FullName}'.");
            if (method.ReturnType != typeof(I18N) || method.GetParameters().Length != 0)
                throw new InvalidOperationException(
                    $"I18N provider method '{providerType.FullName}.{method.Name}' must have signature 'I18N ()'.");
            if (!method.IsStatic && instance == null)
                throw new InvalidOperationException(
                    $"I18N provider method '{providerType.FullName}.{method.Name}' requires instance context.");

            return FastMethodInvoker.Invoke0<I18N>(method, instance);
        }

        private static bool TryCreateProviderInstance(Type providerType, out object? instance)
        {
            instance = null;
            if (providerType is { IsAbstract: true, IsSealed: true })
                return true;

            var ctor = providerType.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RuntimeReflectionMirrorSource] Type '{providerType.FullName}' needs parameterless ctor or be static.");
                return false;
            }

            instance = FastMethodInvoker.CreateWithDefaultCtor(ctor);
            return true;
        }

        private static void AppendPropertyEntries(
            Type providerType,
            object? instance,
            string modId,
            IDictionary<string, MutableSection> sections)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                       BindingFlags.NonPublic;

            foreach (var property in providerType.GetProperties(flags))
            {
                if (!property.CanRead || !property.CanWrite)
                    continue;

                var toggleAttr = property.GetCustomAttribute<ModSettingsToggleAttribute>();
                if (toggleAttr != null && property.PropertyType == typeof(bool))
                {
                    var binding = CreateBinding<bool>(modId, property, instance);
                    AddEntry(sections, toggleAttr.SectionId, toggleAttr.Order, new(
                        toggleAttr.Id,
                        ModSettingsMirrorEntryKind.Toggle,
                        ToText(toggleAttr, nameof(toggleAttr.Label), toggleAttr.Label, property.Name),
                        binding,
                        ToTextOrNull(toggleAttr, nameof(toggleAttr.Description), toggleAttr.Description),
                        VisibleWhen: BuildVisibleWhen(providerType, instance, toggleAttr.VisibleWhen)));
                    continue;
                }

                var sliderAttr = property.GetCustomAttribute<ModSettingsSliderAttribute>();
                if (sliderAttr != null &&
                    (property.PropertyType == typeof(double) || property.PropertyType == typeof(float)))
                {
                    object binding = property.PropertyType == typeof(float)
                        ? CreateBinding<float>(modId, property, instance)
                        : CreateBinding<double>(modId, property, instance);
                    AddEntry(sections, sliderAttr.SectionId, sliderAttr.Order, new(
                        sliderAttr.Id,
                        ModSettingsMirrorEntryKind.Slider,
                        ToText(sliderAttr, nameof(sliderAttr.Label), sliderAttr.Label, property.Name),
                        binding,
                        ToTextOrNull(sliderAttr, nameof(sliderAttr.Description), sliderAttr.Description),
                        new(sliderAttr.Min, sliderAttr.Max, sliderAttr.Step),
                        VisibleWhen: BuildVisibleWhen(providerType, instance, sliderAttr.VisibleWhen)));
                    continue;
                }

                var intSliderAttr = property.GetCustomAttribute<ModSettingsIntSliderAttribute>();
                if (intSliderAttr != null && property.PropertyType == typeof(int))
                {
                    var binding = CreateBinding<int>(modId, property, instance);
                    AddEntry(sections, intSliderAttr.SectionId, intSliderAttr.Order, new(
                        intSliderAttr.Id,
                        ModSettingsMirrorEntryKind.IntSlider,
                        ToText(intSliderAttr, nameof(intSliderAttr.Label), intSliderAttr.Label, property.Name),
                        binding,
                        ToTextOrNull(intSliderAttr, nameof(intSliderAttr.Description), intSliderAttr.Description),
                        new(intSliderAttr.Min, intSliderAttr.Max, intSliderAttr.Step),
                        VisibleWhen: BuildVisibleWhen(providerType, instance, intSliderAttr.VisibleWhen)));
                    continue;
                }

                var stringAttr = property.GetCustomAttribute<ModSettingsStringAttribute>();
                if (stringAttr != null && property.PropertyType == typeof(string))
                {
                    var binding = CreateBinding<string>(modId, property, instance);
                    AddEntry(sections, stringAttr.SectionId, stringAttr.Order, new(
                        stringAttr.Id,
                        ModSettingsMirrorEntryKind.String,
                        ToText(stringAttr, nameof(stringAttr.Label), stringAttr.Label, property.Name),
                        binding,
                        ToTextOrNull(stringAttr, nameof(stringAttr.Description), stringAttr.Description),
                        Placeholder: ToTextOrNull(stringAttr, nameof(stringAttr.Placeholder), stringAttr.Placeholder),
                        MaxLength: stringAttr.MaxLength > 0 ? stringAttr.MaxLength : null,
                        ValidationVisual: BuildStringValidator(providerType, instance, stringAttr.ValidateUsing),
                        VisibleWhen: BuildVisibleWhen(providerType, instance, stringAttr.VisibleWhen)));
                    continue;
                }

                var multilineAttr = property.GetCustomAttribute<ModSettingsMultilineStringAttribute>();
                if (multilineAttr != null && property.PropertyType == typeof(string))
                {
                    var binding = CreateBinding<string>(modId, property, instance);
                    AddEntry(sections, multilineAttr.SectionId, multilineAttr.Order, new(
                        multilineAttr.Id,
                        ModSettingsMirrorEntryKind.MultilineString,
                        ToText(multilineAttr, nameof(multilineAttr.Label), multilineAttr.Label, property.Name),
                        binding,
                        ToTextOrNull(multilineAttr, nameof(multilineAttr.Description), multilineAttr.Description),
                        Placeholder: ToTextOrNull(multilineAttr, nameof(multilineAttr.Placeholder),
                            multilineAttr.Placeholder),
                        MaxLength: multilineAttr.MaxLength > 0 ? multilineAttr.MaxLength : null,
                        VisibleWhen: BuildVisibleWhen(providerType, instance, multilineAttr.VisibleWhen)));
                    continue;
                }

                var colorAttr = property.GetCustomAttribute<ModSettingsColorAttribute>();
                if (colorAttr != null && property.PropertyType == typeof(string))
                {
                    var binding = CreateBinding<string>(modId, property, instance);
                    AddEntry(sections, colorAttr.SectionId, colorAttr.Order, new(
                        colorAttr.Id,
                        ModSettingsMirrorEntryKind.Color,
                        ToText(colorAttr, nameof(colorAttr.Label), colorAttr.Label, property.Name),
                        binding,
                        ToTextOrNull(colorAttr, nameof(colorAttr.Description), colorAttr.Description),
                        EditAlpha: colorAttr.EditAlpha,
                        EditIntensity: colorAttr.EditIntensity,
                        VisibleWhen: BuildVisibleWhen(providerType, instance, colorAttr.VisibleWhen)));
                    continue;
                }

                var keyAttr = property.GetCustomAttribute<ModSettingsKeyBindingAttribute>();
                if (keyAttr != null)
                {
                    if (keyAttr.Multiple && property.PropertyType == typeof(List<string>))
                    {
                        var binding = CreateBinding<List<string>>(modId, property, instance);
                        AddEntry(sections, keyAttr.SectionId, keyAttr.Order, new(
                            keyAttr.Id,
                            ModSettingsMirrorEntryKind.MultiKeyBinding,
                            ToText(keyAttr, nameof(keyAttr.Label), keyAttr.Label, property.Name),
                            binding,
                            ToTextOrNull(keyAttr, nameof(keyAttr.Description), keyAttr.Description),
                            AllowModifierCombos: keyAttr.AllowModifierCombos,
                            AllowModifierOnly: keyAttr.AllowModifierOnly,
                            DistinguishModifierSides: keyAttr.DistinguishModifierSides,
                            VisibleWhen: BuildVisibleWhen(providerType, instance, keyAttr.VisibleWhen)));
                        continue;
                    }

                    if (property.PropertyType == typeof(string))
                    {
                        var binding = CreateBinding<string>(modId, property, instance);
                        AddEntry(sections, keyAttr.SectionId, keyAttr.Order, new(
                            keyAttr.Id,
                            ModSettingsMirrorEntryKind.KeyBinding,
                            ToText(keyAttr, nameof(keyAttr.Label), keyAttr.Label, property.Name),
                            binding,
                            ToTextOrNull(keyAttr, nameof(keyAttr.Description), keyAttr.Description),
                            AllowModifierCombos: keyAttr.AllowModifierCombos,
                            AllowModifierOnly: keyAttr.AllowModifierOnly,
                            DistinguishModifierSides: keyAttr.DistinguishModifierSides,
                            VisibleWhen: BuildVisibleWhen(providerType, instance, keyAttr.VisibleWhen)));
                    }

                    continue;
                }

                var choiceAttr = property.GetCustomAttribute<ModSettingsChoiceAttribute>();
                if (choiceAttr == null) continue;
                {
                    if (property.PropertyType.IsEnum)
                    {
                        var binding = CreateEnumBinding(modId, property, instance);
                        AddEntry(sections, choiceAttr.SectionId, choiceAttr.Order, new(
                            choiceAttr.Id,
                            ModSettingsMirrorEntryKind.EnumChoice,
                            ToText(choiceAttr, nameof(choiceAttr.Label), choiceAttr.Label, property.Name),
                            binding,
                            ToTextOrNull(choiceAttr, nameof(choiceAttr.Description), choiceAttr.Description),
                            ChoicePresentation: choiceAttr.Presentation,
                            EnumType: property.PropertyType,
                            VisibleWhen: BuildVisibleWhen(providerType, instance, choiceAttr.VisibleWhen)));
                        continue;
                    }

                    if (property.PropertyType != typeof(string)) continue;
                    {
                        var values = choiceAttr.Options ?? [];
                        if (values.Length == 0)
                            continue;
                        var labels = choiceAttr.OptionLabels ?? [];
                        var options = values
                            .Select((value, index) => new ModSettingsMirrorChoiceOption(
                                value,
                                ResolveIndexedText(choiceAttr, "OptionLabel", index,
                                    index < labels.Length ? labels[index] : value, value)))
                            .ToArray();
                        var binding = CreateBinding<string>(modId, property, instance);
                        AddEntry(sections, choiceAttr.SectionId, choiceAttr.Order, new(
                            choiceAttr.Id,
                            ModSettingsMirrorEntryKind.Choice,
                            ToText(choiceAttr, nameof(choiceAttr.Label), choiceAttr.Label, property.Name),
                            binding,
                            ToTextOrNull(choiceAttr, nameof(choiceAttr.Description), choiceAttr.Description),
                            ChoiceOptions: options,
                            ChoicePresentation: choiceAttr.Presentation,
                            VisibleWhen: BuildVisibleWhen(providerType, instance, choiceAttr.VisibleWhen)));
                    }
                }
            }

            foreach (var field in providerType.GetFields(flags))
            {
                var toggleAttr = field.GetCustomAttribute<ModSettingsToggleAttribute>();
                if (toggleAttr != null && field.FieldType == typeof(bool))
                {
                    var binding = CreateBinding<bool>(modId, field, instance);
                    AddEntry(sections, toggleAttr.SectionId, toggleAttr.Order, new(
                        toggleAttr.Id,
                        ModSettingsMirrorEntryKind.Toggle,
                        ToText(toggleAttr, nameof(toggleAttr.Label), toggleAttr.Label, field.Name),
                        binding,
                        ToTextOrNull(toggleAttr, nameof(toggleAttr.Description), toggleAttr.Description),
                        VisibleWhen: BuildVisibleWhen(providerType, instance, toggleAttr.VisibleWhen)));
                    continue;
                }

                var sliderAttr = field.GetCustomAttribute<ModSettingsSliderAttribute>();
                if (sliderAttr != null && (field.FieldType == typeof(double) || field.FieldType == typeof(float)))
                {
                    object binding = field.FieldType == typeof(float)
                        ? CreateBinding<float>(modId, field, instance)
                        : CreateBinding<double>(modId, field, instance);
                    AddEntry(sections, sliderAttr.SectionId, sliderAttr.Order, new(
                        sliderAttr.Id,
                        ModSettingsMirrorEntryKind.Slider,
                        ToText(sliderAttr, nameof(sliderAttr.Label), sliderAttr.Label, field.Name),
                        binding,
                        ToTextOrNull(sliderAttr, nameof(sliderAttr.Description), sliderAttr.Description),
                        new(sliderAttr.Min, sliderAttr.Max, sliderAttr.Step),
                        VisibleWhen: BuildVisibleWhen(providerType, instance, sliderAttr.VisibleWhen)));
                    continue;
                }

                var intSliderAttr = field.GetCustomAttribute<ModSettingsIntSliderAttribute>();
                if (intSliderAttr != null && field.FieldType == typeof(int))
                {
                    var binding = CreateBinding<int>(modId, field, instance);
                    AddEntry(sections, intSliderAttr.SectionId, intSliderAttr.Order, new(
                        intSliderAttr.Id,
                        ModSettingsMirrorEntryKind.IntSlider,
                        ToText(intSliderAttr, nameof(intSliderAttr.Label), intSliderAttr.Label, field.Name),
                        binding,
                        ToTextOrNull(intSliderAttr, nameof(intSliderAttr.Description), intSliderAttr.Description),
                        new(intSliderAttr.Min, intSliderAttr.Max, intSliderAttr.Step),
                        VisibleWhen: BuildVisibleWhen(providerType, instance, intSliderAttr.VisibleWhen)));
                    continue;
                }

                var stringAttr = field.GetCustomAttribute<ModSettingsStringAttribute>();
                if (stringAttr != null && field.FieldType == typeof(string))
                {
                    var binding = CreateBinding<string>(modId, field, instance);
                    AddEntry(sections, stringAttr.SectionId, stringAttr.Order, new(
                        stringAttr.Id,
                        ModSettingsMirrorEntryKind.String,
                        ToText(stringAttr, nameof(stringAttr.Label), stringAttr.Label, field.Name),
                        binding,
                        ToTextOrNull(stringAttr, nameof(stringAttr.Description), stringAttr.Description),
                        Placeholder: ToTextOrNull(stringAttr, nameof(stringAttr.Placeholder), stringAttr.Placeholder),
                        MaxLength: stringAttr.MaxLength > 0 ? stringAttr.MaxLength : null,
                        ValidationVisual: BuildStringValidator(providerType, instance, stringAttr.ValidateUsing),
                        VisibleWhen: BuildVisibleWhen(providerType, instance, stringAttr.VisibleWhen)));
                    continue;
                }

                var multilineAttr = field.GetCustomAttribute<ModSettingsMultilineStringAttribute>();
                if (multilineAttr != null && field.FieldType == typeof(string))
                {
                    var binding = CreateBinding<string>(modId, field, instance);
                    AddEntry(sections, multilineAttr.SectionId, multilineAttr.Order, new(
                        multilineAttr.Id,
                        ModSettingsMirrorEntryKind.MultilineString,
                        ToText(multilineAttr, nameof(multilineAttr.Label), multilineAttr.Label, field.Name),
                        binding,
                        ToTextOrNull(multilineAttr, nameof(multilineAttr.Description), multilineAttr.Description),
                        Placeholder: ToTextOrNull(multilineAttr, nameof(multilineAttr.Placeholder),
                            multilineAttr.Placeholder),
                        MaxLength: multilineAttr.MaxLength > 0 ? multilineAttr.MaxLength : null,
                        VisibleWhen: BuildVisibleWhen(providerType, instance, multilineAttr.VisibleWhen)));
                    continue;
                }

                var colorAttr = field.GetCustomAttribute<ModSettingsColorAttribute>();
                if (colorAttr != null && field.FieldType == typeof(string))
                {
                    var binding = CreateBinding<string>(modId, field, instance);
                    AddEntry(sections, colorAttr.SectionId, colorAttr.Order, new(
                        colorAttr.Id,
                        ModSettingsMirrorEntryKind.Color,
                        ToText(colorAttr, nameof(colorAttr.Label), colorAttr.Label, field.Name),
                        binding,
                        ToTextOrNull(colorAttr, nameof(colorAttr.Description), colorAttr.Description),
                        EditAlpha: colorAttr.EditAlpha,
                        EditIntensity: colorAttr.EditIntensity,
                        VisibleWhen: BuildVisibleWhen(providerType, instance, colorAttr.VisibleWhen)));
                    continue;
                }

                var keyAttr = field.GetCustomAttribute<ModSettingsKeyBindingAttribute>();
                if (keyAttr != null)
                {
                    if (keyAttr.Multiple && field.FieldType == typeof(List<string>))
                    {
                        var binding = CreateBinding<List<string>>(modId, field, instance);
                        AddEntry(sections, keyAttr.SectionId, keyAttr.Order, new(
                            keyAttr.Id,
                            ModSettingsMirrorEntryKind.MultiKeyBinding,
                            ToText(keyAttr, nameof(keyAttr.Label), keyAttr.Label, field.Name),
                            binding,
                            ToTextOrNull(keyAttr, nameof(keyAttr.Description), keyAttr.Description),
                            AllowModifierCombos: keyAttr.AllowModifierCombos,
                            AllowModifierOnly: keyAttr.AllowModifierOnly,
                            DistinguishModifierSides: keyAttr.DistinguishModifierSides,
                            VisibleWhen: BuildVisibleWhen(providerType, instance, keyAttr.VisibleWhen)));
                        continue;
                    }

                    if (field.FieldType == typeof(string))
                    {
                        var binding = CreateBinding<string>(modId, field, instance);
                        AddEntry(sections, keyAttr.SectionId, keyAttr.Order, new(
                            keyAttr.Id,
                            ModSettingsMirrorEntryKind.KeyBinding,
                            ToText(keyAttr, nameof(keyAttr.Label), keyAttr.Label, field.Name),
                            binding,
                            ToTextOrNull(keyAttr, nameof(keyAttr.Description), keyAttr.Description),
                            AllowModifierCombos: keyAttr.AllowModifierCombos,
                            AllowModifierOnly: keyAttr.AllowModifierOnly,
                            DistinguishModifierSides: keyAttr.DistinguishModifierSides,
                            VisibleWhen: BuildVisibleWhen(providerType, instance, keyAttr.VisibleWhen)));
                    }

                    continue;
                }

                var choiceAttr = field.GetCustomAttribute<ModSettingsChoiceAttribute>();
                if (choiceAttr == null)
                    continue;

                if (field.FieldType.IsEnum)
                {
                    var binding = CreateEnumBinding(modId, field, instance);
                    AddEntry(sections, choiceAttr.SectionId, choiceAttr.Order, new(
                        choiceAttr.Id,
                        ModSettingsMirrorEntryKind.EnumChoice,
                        ToText(choiceAttr, nameof(choiceAttr.Label), choiceAttr.Label, field.Name),
                        binding,
                        ToTextOrNull(choiceAttr, nameof(choiceAttr.Description), choiceAttr.Description),
                        ChoicePresentation: choiceAttr.Presentation,
                        EnumType: field.FieldType,
                        VisibleWhen: BuildVisibleWhen(providerType, instance, choiceAttr.VisibleWhen)));
                    continue;
                }

                if (field.FieldType != typeof(string))
                    continue;
                var values = choiceAttr.Options ?? [];
                if (values.Length == 0)
                    continue;
                var labels = choiceAttr.OptionLabels ?? [];
                var options = values
                    .Select((value, index) => new ModSettingsMirrorChoiceOption(
                        value,
                        ResolveIndexedText(choiceAttr, "OptionLabel", index,
                            index < labels.Length ? labels[index] : value, value)))
                    .ToArray();
                var stringBinding = CreateBinding<string>(modId, field, instance);
                AddEntry(sections, choiceAttr.SectionId, choiceAttr.Order, new(
                    choiceAttr.Id,
                    ModSettingsMirrorEntryKind.Choice,
                    ToText(choiceAttr, nameof(choiceAttr.Label), choiceAttr.Label, field.Name),
                    stringBinding,
                    ToTextOrNull(choiceAttr, nameof(choiceAttr.Description), choiceAttr.Description),
                    ChoiceOptions: options,
                    ChoicePresentation: choiceAttr.Presentation,
                    VisibleWhen: BuildVisibleWhen(providerType, instance, choiceAttr.VisibleWhen)));
            }
        }

        private static void AppendMethodEntries(
            Type providerType,
            object? instance,
            string modId,
            IDictionary<string, MutableSection> sections)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                       BindingFlags.NonPublic;
            foreach (var method in providerType.GetMethods(flags))
            {
                if (method.IsSpecialName)
                    continue;

                var headerAttr = method.GetCustomAttribute<ModSettingsHeaderAttribute>();
                if (headerAttr != null)
                    AddEntry(sections, headerAttr.SectionId, headerAttr.Order, new(
                        headerAttr.Id,
                        ModSettingsMirrorEntryKind.Header,
                        ToText(headerAttr, nameof(headerAttr.Label), headerAttr.Label, method.Name),
                        Description: ToTextOrNull(headerAttr, nameof(headerAttr.Description), headerAttr.Description),
                        VisibleWhen: BuildVisibleWhen(providerType, instance, headerAttr.VisibleWhen)));

                var paragraphAttr = method.GetCustomAttribute<ModSettingsParagraphAttribute>();
                if (paragraphAttr != null)
                    AddEntry(sections, paragraphAttr.SectionId, paragraphAttr.Order, new(
                        paragraphAttr.Id,
                        ModSettingsMirrorEntryKind.Paragraph,
                        BuildMethodBackedMirrorText(paragraphAttr, nameof(paragraphAttr.Text), paragraphAttr.Text,
                            method, instance),
                        Description: ToTextOrNull(paragraphAttr, nameof(paragraphAttr.Description),
                            paragraphAttr.Description),
                        MaxBodyHeight: paragraphAttr.MaxBodyHeight > 0 ? paragraphAttr.MaxBodyHeight : null,
                        VisibleWhen: BuildVisibleWhen(providerType, instance, paragraphAttr.VisibleWhen)));

                var infoAttr = method.GetCustomAttribute<ModSettingsInfoCardAttribute>();
                if (infoAttr != null)
                    AddEntry(sections, infoAttr.SectionId, infoAttr.Order, new(
                        infoAttr.Id,
                        ModSettingsMirrorEntryKind.InfoCard,
                        ToText(infoAttr, nameof(infoAttr.Label), infoAttr.Label, method.Name),
                        Description: ToTextOrNull(infoAttr, nameof(infoAttr.Description), infoAttr.Description),
                        Body: BuildMethodBackedMirrorText(infoAttr, nameof(infoAttr.Body), infoAttr.Body, method,
                            instance),
                        VisibleWhen: BuildVisibleWhen(providerType, instance, infoAttr.VisibleWhen)));

                var hotkeyAttr = method.GetCustomAttribute<ModSettingsRuntimeHotkeySummaryAttribute>();
                if (hotkeyAttr != null)
                {
                    var bindings = hotkeyAttr.Bindings.Select(static binding => ModSettingsText.Literal(binding))
                        .ToArray();
                    for (var index = 0; index < bindings.Length; index++)
                        bindings[index] = ResolveIndexedText(
                            hotkeyAttr,
                            "Binding",
                            index,
                            hotkeyAttr.Bindings[index],
                            hotkeyAttr.Bindings[index]);
                    AddEntry(sections, hotkeyAttr.SectionId, hotkeyAttr.Order, new(
                        hotkeyAttr.Id,
                        ModSettingsMirrorEntryKind.RuntimeHotkeySummary,
                        ToText(hotkeyAttr, nameof(hotkeyAttr.Label), hotkeyAttr.Label, method.Name),
                        Body: BuildMethodBackedMirrorText(hotkeyAttr, nameof(hotkeyAttr.Body), hotkeyAttr.Body, method,
                            instance),
                        HotkeyBindings: bindings,
                        HotkeyIdSuffix: ToTextOrNull(hotkeyAttr, nameof(hotkeyAttr.IdSuffix), hotkeyAttr.IdSuffix),
                        VisibleWhen: BuildVisibleWhen(providerType, instance, hotkeyAttr.VisibleWhen)));
                }

                var imageAttr = method.GetCustomAttribute<ModSettingsImageAttribute>();
                if (imageAttr != null)
                    AddEntry(sections, imageAttr.SectionId, imageAttr.Order, new(
                        imageAttr.Id,
                        ModSettingsMirrorEntryKind.Image,
                        ToText(imageAttr, nameof(imageAttr.Label), imageAttr.Label, method.Name),
                        Description: ToTextOrNull(imageAttr, nameof(imageAttr.Description), imageAttr.Description),
                        TextureProvider: BuildTextureProvider(method, instance),
                        PreviewHeight: imageAttr.PreviewHeight,
                        VisibleWhen: BuildVisibleWhen(providerType, instance, imageAttr.VisibleWhen)));

                var buttonAttr = method.GetCustomAttribute<ModSettingsButtonAttribute>();
                if (buttonAttr != null)
                {
                    var label = ToText(buttonAttr, nameof(buttonAttr.Label), buttonAttr.Label, method.Name);
                    var buttonText = ToText(buttonAttr, nameof(buttonAttr.ButtonText), buttonAttr.ButtonText,
                        method.Name);
                    var visibleWhen = BuildVisibleWhen(providerType, instance, buttonAttr.VisibleWhen);
                    if (buttonAttr.UseHostContext)
                        AddEntry(sections, buttonAttr.SectionId, buttonAttr.Order, new(
                            buttonAttr.Id,
                            ModSettingsMirrorEntryKind.HostContextButton,
                            label,
                            Description: ToTextOrNull(buttonAttr, nameof(buttonAttr.Description),
                                buttonAttr.Description),
                            ButtonLabel: buttonText,
                            HostContextOnClick: host => InvokeAction(method, instance, host),
                            ButtonTone: buttonAttr.Tone,
                            VisibleWhen: visibleWhen));
                    else
                        AddEntry(sections, buttonAttr.SectionId, buttonAttr.Order, new(
                            buttonAttr.Id,
                            ModSettingsMirrorEntryKind.Button,
                            label,
                            Description: ToTextOrNull(buttonAttr, nameof(buttonAttr.Description),
                                buttonAttr.Description),
                            ButtonLabel: buttonText,
                            OnClick: () => InvokeAction(method, instance, null),
                            ButtonTone: buttonAttr.Tone,
                            VisibleWhen: visibleWhen));
                }

                var subpageAttr = method.GetCustomAttribute<ModSettingsSubpageAttribute>();
                if (subpageAttr != null)
                    AddEntry(sections, subpageAttr.SectionId, subpageAttr.Order, new(
                        subpageAttr.Id,
                        ModSettingsMirrorEntryKind.Subpage,
                        ToText(subpageAttr, nameof(subpageAttr.Label), subpageAttr.Label, method.Name),
                        Description: ToTextOrNull(subpageAttr, nameof(subpageAttr.Description),
                            subpageAttr.Description),
                        ButtonLabel: ToTextOrNull(subpageAttr, nameof(subpageAttr.ButtonText), subpageAttr.ButtonText),
                        TargetPageId: subpageAttr.TargetPageId,
                        VisibleWhen: BuildVisibleWhen(providerType, instance, subpageAttr.VisibleWhen)));

                var customAttr = method.GetCustomAttribute<ModSettingsCustomEntryAttribute>();
                if (customAttr != null)
                    AddEntry(sections, customAttr.SectionId, customAttr.Order, new(
                        customAttr.Id,
                        ModSettingsMirrorEntryKind.Custom,
                        ToText(customAttr, nameof(customAttr.Label), customAttr.Label, method.Name),
                        Description: ToTextOrNull(customAttr, nameof(customAttr.Description), customAttr.Description),
                        CustomControlFactory: host => InvokeCustomControl(method, instance, host),
                        VisibleWhen: BuildVisibleWhen(providerType, instance, customAttr.VisibleWhen)));
            }
        }

        private static ModSettingsText BuildMethodBackedMirrorText(
            object attribute,
            string textFieldName,
            string? configuredRawText,
            MethodInfo method,
            object? instance)
        {
            if (!string.IsNullOrWhiteSpace(configuredRawText)
                || TryGetLocStringSource(attribute, textFieldName, out _, out _)
                || TryGetI18NKey(attribute, textFieldName, out _))
                return ToText(attribute, textFieldName, configuredRawText,
                    InvokeText(method, instance) ?? method.Name);

            return ModSettingsText.Dynamic(() => InvokeText(method, instance) ?? string.Empty);
        }

        private static ModSettingsText ToText(string? raw, string fallback)
        {
            if (string.IsNullOrWhiteSpace(raw) && TryResolveDefaultSlugLocString(fallback, false, out var slugLoc))
                return slugLoc;
            return ModSettingsText.Literal(string.IsNullOrWhiteSpace(raw) ? fallback : raw);
        }

        private static ModSettingsText ToText(object source, string textFieldName, string? raw, string fallback)
        {
            if (TryGetLocStringSource(source, textFieldName, out var table, out var key))
                return ModSettingsText.LocString(table, key, string.IsNullOrWhiteSpace(raw) ? fallback : raw);

            // ReSharper disable once InvertIf
            if (TryGetI18NKey(source, textFieldName, out key))
            {
                var i18N = ResolveI18NForSource(source, textFieldName, null);
                return ModSettingsText.I18N(
                    i18N,
                    key,
                    string.IsNullOrWhiteSpace(raw) ? fallback : raw);
            }

            return ToText(raw, fallback);
        }

        private static ModSettingsText? ToTextOrNull(string? raw)
        {
            return string.IsNullOrWhiteSpace(raw) ? null : ModSettingsText.Literal(raw);
        }

        private static ModSettingsText? ToTextOrNull(object source, string textFieldName, string? raw)
        {
            if (TryGetLocStringSource(source, textFieldName, out var table, out var key))
            {
                var fallback = string.IsNullOrWhiteSpace(raw) ? key : raw;
                return ModSettingsText.LocString(table, key, fallback);
            }

            if (TryGetI18NKey(source, textFieldName, out key))
            {
                var i18N = ResolveI18NForSource(source, textFieldName, null);
                var fallback = string.IsNullOrWhiteSpace(raw) ? key : raw;
                return ModSettingsText.I18N(i18N, key, fallback);
            }

            if (string.IsNullOrWhiteSpace(raw) &&
                TryResolveDefaultSlugLocString(GetSlugSeedFor(source, textFieldName), IsDescriptionField(textFieldName),
                    out var slugLoc))
                return slugLoc;

            return ToTextOrNull(raw);
        }

        private static bool TryResolveDefaultSlugLocString(string? seed, bool isDescription, out ModSettingsText text)
        {
            if (string.IsNullOrWhiteSpace(seed) || string.IsNullOrWhiteSpace(_currentReflectionModId))
            {
                text = null!;
                return false;
            }

            var prefix = ModSettingsMirrorSlugPolicy.PrefixForModId(_currentReflectionModId);
            var slug = ModSettingsMirrorIds.Slug(seed);
            if (string.IsNullOrWhiteSpace(slug))
            {
                text = null!;
                return false;
            }

            string[] suffixes = isDescription ? [".description", ".hover.desc"] : [".title"];
            foreach (var suffix in suffixes)
            {
                var key = prefix + slug + suffix;
                if (!LocString.Exists("settings_ui", key))
                    continue;
                text = ModSettingsText.LocString("settings_ui", key, seed);
                return true;
            }

            text = null!;
            return false;
        }

        private static bool IsDescriptionField(string fieldName)
        {
            return fieldName.EndsWith("Description", StringComparison.OrdinalIgnoreCase) ||
                   fieldName.EndsWith("Body", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetSlugSeedFor(object source, string textFieldName)
        {
            if (TryGetStringProperty(source, "Id", out var id))
                return id;
            if (TryGetStringProperty(source, "PageId", out var pageId))
                return pageId;
            return TryGetStringProperty(source, "SectionId", out var sectionId) ? sectionId : textFieldName;
        }

        private static ModSettingsText ResolveIndexedText(
            object source,
            string fieldBaseName,
            int index,
            string? raw,
            string fallback)
        {
            if (TryGetIndexedLocStringSource(source, fieldBaseName, index, out var table, out var key))
                return ModSettingsText.LocString(table, key,
                    string.IsNullOrWhiteSpace(raw) ? fallback : raw ?? fallback);

            // ReSharper disable once InvertIf
            if (TryGetIndexedI18NKey(source, fieldBaseName, index, out key))
            {
                var i18N = ResolveI18NForSource(source, fieldBaseName, index);
                return ModSettingsText.I18N(
                    i18N,
                    key,
                    string.IsNullOrWhiteSpace(raw) ? fallback : raw ?? fallback);
            }

            return ToText(raw, fallback);
        }

        private static bool TryGetIndexedI18NKey(object source, string fieldBaseName, int index, out string key)
        {
            return TryGetStringArrayValue(source, $"{fieldBaseName}Keys", index, out key);
        }

        private static bool TryGetIndexedLocStringSource(
            object source,
            string fieldBaseName,
            int index,
            out string table,
            out string key)
        {
            if (TryGetStringArrayValue(source, $"{fieldBaseName}LocKeys", index, out key))
            {
                table = TryGetStringProperty(source, $"{fieldBaseName}LocTable", out var explicitTable)
                    ? explicitTable
                    : DefaultLocTable;
                return true;
            }

            table = string.Empty;
            key = string.Empty;
            return false;
        }

        private static bool TryGetStringArrayValue(object source, string propertyName, int index, out string value)
        {
            var property = source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property?.GetValue(source) is not string[] raw || index < 0 || index >= raw.Length ||
                string.IsNullOrWhiteSpace(raw[index]))
            {
                value = string.Empty;
                return false;
            }

            value = raw[index].Trim();
            return true;
        }

        private static bool TryGetI18NKey(object source, string textFieldName, out string key)
        {
            return TryGetStringProperty(source, $"{textFieldName}Key", out key);
        }

        private static bool TryGetLocStringSource(
            object source,
            string textFieldName,
            out string table,
            out string key)
        {
            if (TryGetStringProperty(source, $"{textFieldName}LocKey", out key))
            {
                table = TryGetStringProperty(source, $"{textFieldName}LocTable", out var explicitTable)
                    ? explicitTable
                    : DefaultLocTable;
                return true;
            }

            table = string.Empty;
            key = string.Empty;
            return false;
        }

        private static bool TryGetStringProperty(object source, string propertyName, out string value)
        {
            var property = source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            var raw = property?.GetValue(source) as string;
            if (string.IsNullOrWhiteSpace(raw))
            {
                value = string.Empty;
                return false;
            }

            value = raw.Trim();
            return true;
        }

        private static I18N ResolveI18NForSource(object source, string fieldName, int? index)
        {
            if (TryGetStringProperty(source, "I18NProviderUsing", out var providerMethod))
            {
                var scoped = ResolveI18NByProviderMethod(providerMethod, source.GetType().Name, fieldName, index);
                if (scoped == null)
                    throw new InvalidOperationException(
                        BuildI18NMissingMessage(source, fieldName, index, providerMethod));
                return scoped;
            }

            if (_currentReflectionI18N != null)
                return _currentReflectionI18N;

            throw new InvalidOperationException(
                BuildI18NMissingMessage(source, fieldName, index, null));
        }

        private static string BuildI18NMissingMessage(object source, string fieldName, int? index,
            string? providerMethod)
        {
            var memberPath = index.HasValue
                ? $"{source.GetType().Name}.{fieldName}Keys[{index.Value}]"
                : $"{source.GetType().Name}.{fieldName}Key";
            var providerHint = string.IsNullOrWhiteSpace(providerMethod)
                ? "Set I18NProviderUsing on the page or current attribute."
                : $"Configured I18NProviderUsing '{providerMethod}' returned null or could not be resolved.";
            return $"I18N key path '{memberPath}' is invalid because no I18N instance is available. {providerHint}";
        }

        private static I18N? ResolveI18NByProviderMethod(
            string methodName,
            string sourceTypeName,
            string fieldName,
            int? index)
        {
            var providerType = _currentProviderType
                               ?? throw new InvalidOperationException(
                                   "I18N provider resolution failed: current provider type is unavailable.");
            var method = providerType.GetMethod(
                methodName.Trim(),
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException(
                    $"I18N provider method '{methodName}' referenced by '{sourceTypeName}.{fieldName}' was not found on '{providerType.FullName}'.");
            if (method.ReturnType != typeof(I18N) || method.GetParameters().Length != 0)
                throw new InvalidOperationException(
                    $"I18N provider method '{providerType.FullName}.{method.Name}' must have signature 'I18N ()'.");
            if (!method.IsStatic && _currentProviderInstance == null)
                throw new InvalidOperationException(
                    $"I18N provider method '{providerType.FullName}.{method.Name}' requires instance context.");

            return FastMethodInvoker.Invoke0<I18N>(method, _currentProviderInstance);
        }

        private static Func<bool>? BuildVisibleWhen(Type providerType, object? instance, string? methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName))
                return null;

            var method = providerType.GetMethod(methodName.Trim(),
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null || method.ReturnType != typeof(bool) || method.GetParameters().Length != 0)
                return null;

            return () => FastMethodInvoker.Invoke0<bool>(method, instance);
        }

        private static Func<string, bool>? BuildStringValidator(Type providerType, object? instance, string? methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName))
                return null;

            var method = providerType.GetMethod(methodName.Trim(),
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null || method.ReturnType != typeof(bool))
                return null;

            var ps = method.GetParameters();
            if (ps.Length != 1 || ps[0].ParameterType != typeof(string))
                return null;

            return value => FastMethodInvoker.Invoke1<string, bool>(method, instance, value);
        }

        private static IModSettingsValueBinding<TValue> CreateBinding<TValue>(string modId, PropertyInfo property,
            object? instance)
        {
            var key = $"reflect::{property.DeclaringType?.FullName}.{property.Name}";
            var attr = property.GetCustomAttribute<ModSettingsBindingAttribute>();
            return CreateBindingCore(
                modId,
                key,
                () => (TValue)(property.GetValue(instance) ?? default(TValue)!),
                value => property.SetValue(instance, value),
                property,
                instance,
                attr);
        }

        private static object CreateEnumBinding(string modId, PropertyInfo property, object? instance)
        {
            var method = typeof(RuntimeReflectionMirrorSource)
                .GetMethod(nameof(CreateEnumBindingGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(property.PropertyType);
            return method.Invoke(null, [modId, property, instance])!;
        }

        private static object CreateEnumBinding(string modId, FieldInfo field, object? instance)
        {
            var method = typeof(RuntimeReflectionMirrorSource)
                .GetMethod(nameof(CreateEnumBindingFieldGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(field.FieldType);
            return method.Invoke(null, [modId, field, instance])!;
        }

        private static IModSettingsValueBinding<TEnum> CreateEnumBindingGeneric<TEnum>(string modId,
            PropertyInfo property,
            object? instance) where TEnum : struct, Enum
        {
            var key = $"reflect::{property.DeclaringType?.FullName}.{property.Name}";
            var attr = property.GetCustomAttribute<ModSettingsBindingAttribute>();
            return CreateBindingCore(
                modId,
                key,
                () => (TEnum)(property.GetValue(instance) ?? default(TEnum)),
                value => property.SetValue(instance, value),
                property,
                instance,
                attr);
        }

        private static IModSettingsValueBinding<TEnum> CreateEnumBindingFieldGeneric<TEnum>(string modId,
            FieldInfo field,
            object? instance) where TEnum : struct, Enum
        {
            var key = $"reflect::{field.DeclaringType?.FullName}.{field.Name}";
            var attr = field.GetCustomAttribute<ModSettingsBindingAttribute>();
            return CreateBindingCore(
                modId,
                key,
                () => (TEnum)(field.GetValue(instance) ?? default(TEnum)),
                value => field.SetValue(instance, value),
                field,
                instance,
                attr);
        }

        private static IModSettingsValueBinding<TValue> CreateBinding<TValue>(string modId, FieldInfo field,
            object? instance)
        {
            var key = $"reflect::{field.DeclaringType?.FullName}.{field.Name}";
            var attr = field.GetCustomAttribute<ModSettingsBindingAttribute>();
            return CreateBindingCore(
                modId,
                key,
                () => (TValue)(field.GetValue(instance) ?? default(TValue)!),
                value => field.SetValue(instance, value),
                field,
                instance,
                attr);
        }

        private static IModSettingsValueBinding<TValue> CreateBindingCore<TValue>(
            string modId,
            string defaultDataKey,
            Func<TValue> readMember,
            Action<TValue> writeMember,
            MemberInfo member,
            object? instance,
            ModSettingsBindingAttribute? attr)
        {
            var dataKey = string.IsNullOrWhiteSpace(attr?.DataKey) ? defaultDataKey : attr.DataKey.Trim();
            var source = attr?.Source ?? ModSettingsReflectionBindingSource.Auto;
            var binding = source switch
            {
                ModSettingsReflectionBindingSource.Auto or
                    ModSettingsReflectionBindingSource.Global => BuildScopedBinding(
                        modId, dataKey, SaveScope.Global, readMember, writeMember),
                ModSettingsReflectionBindingSource.Profile => BuildScopedBinding(
                    modId, dataKey, SaveScope.Profile, readMember, writeMember),
                ModSettingsReflectionBindingSource.RunSidecar => BuildRunSidecarBinding(
                    modId, dataKey, readMember, writeMember),
                ModSettingsReflectionBindingSource.InMemory => BuildInMemoryBinding(
                    modId, dataKey, readMember, writeMember),
                ModSettingsReflectionBindingSource.Callback => BuildCallbackBinding(
                    modId, dataKey, member, instance, readMember, writeMember, attr),
                ModSettingsReflectionBindingSource.Project => BuildProjectedBinding<TValue>(
                    modId, dataKey, member, instance, attr),
                _ => throw new InvalidOperationException(
                    $"Unsupported binding source '{source}' on '{member.DeclaringType?.FullName}.{member.Name}'."),
            };

            if (!string.IsNullOrWhiteSpace(attr?.DefaultUsing))
            {
                var createDefault = ResolveFuncNoArg<TValue>(member, instance, attr.DefaultUsing!, "DefaultUsing");
                binding = ModSettingsBindings.WithDefault(binding, createDefault);
            }

            // ReSharper disable once InvertIf
            if (!string.IsNullOrWhiteSpace(attr?.AdapterUsing))
            {
                var adapter = ResolveFuncNoArg<IStructuredModSettingsValueAdapter<TValue>>(
                    member, instance, attr.AdapterUsing!, "AdapterUsing")();
                binding = ModSettingsBindings.WithAdapter(binding, adapter);
            }

            if (source != ModSettingsReflectionBindingSource.Callback)
                binding = new AutoSaveModSettingsValueBinding<TValue>(binding);

            return binding;
        }

        private static IModSettingsValueBinding<TValue> BuildScopedBinding<TValue>(
            string modId,
            string dataKey,
            SaveScope scope,
            Func<TValue> readMember,
            Action<TValue> writeMember)
        {
            EnsureScopedStoreRegistration(modId, dataKey, scope, readMember);
            var storeBinding = scope switch
            {
                SaveScope.Global => ModSettingsBindings.Global<ReflectionBindingBox<TValue>, TValue>(
                    modId, dataKey, box => box.Value, (box, value) => box.Value = value),
                SaveScope.Profile => ModSettingsBindings.Profile<ReflectionBindingBox<TValue>, TValue>(
                    modId, dataKey, box => box.Value, (box, value) => box.Value = value),
                _ => throw new InvalidOperationException($"Unsupported scoped source '{scope}'."),
            };

            return new MemberSynchronizedModSettingsValueBinding<TValue>(storeBinding, writeMember);
        }

        private static void EnsureScopedStoreRegistration<TValue>(
            string modId,
            string dataKey,
            SaveScope scope,
            Func<TValue> readMember)
        {
            var store = ModDataStore.For(modId);
            try
            {
                store.Register(
                    dataKey,
                    dataKey,
                    scope,
                    () => new ReflectionBindingBox<TValue> { Value = readMember() });
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("already registered", StringComparison.OrdinalIgnoreCase))
            {
            }
        }

        private static IModSettingsValueBinding<TValue> BuildRunSidecarBinding<TValue>(
            string modId,
            string dataKey,
            Func<TValue> readMember,
            Action<TValue> writeMember)
        {
            var runSidecar = ModSettingsBindings.RunSidecar<ReflectionBindingBox<TValue>, TValue>(
                modId,
                dataKey,
                box => box.Value,
                (box, value) => box.Value = value);
            return new MemberSynchronizedModSettingsValueBinding<TValue>(runSidecar, writeMember);
        }

        private static IModSettingsValueBinding<TValue> BuildInMemoryBinding<TValue>(
            string modId,
            string dataKey,
            Func<TValue> readMember,
            Action<TValue> writeMember)
        {
            var inMemory = ModSettingsBindings.InMemory(modId, dataKey, readMember());
            return new MemberSynchronizedModSettingsValueBinding<TValue>(inMemory, writeMember);
        }

        private static IModSettingsValueBinding<TValue> BuildCallbackBinding<TValue>(
            string modId,
            string dataKey,
            MemberInfo member,
            object? instance,
            Func<TValue> fallbackRead,
            Action<TValue> fallbackWrite,
            ModSettingsBindingAttribute? attr)
        {
            if (attr == null)
                throw new InvalidOperationException(
                    $"Callback source requires ModSettingsBindingAttribute on '{member.DeclaringType?.FullName}.{member.Name}'.");

            var read = !string.IsNullOrWhiteSpace(attr.ReadUsing)
                ? ResolveFuncNoArg<TValue>(member, instance, attr.ReadUsing!, "ReadUsing")
                : fallbackRead;
            var write = !string.IsNullOrWhiteSpace(attr.WriteUsing)
                ? ResolveActionOneArg<TValue>(member, instance, attr.WriteUsing!, "WriteUsing")
                : fallbackWrite;
            var save = !string.IsNullOrWhiteSpace(attr.SaveUsing)
                ? ResolveActionNoArg(member, instance, attr.SaveUsing!, "SaveUsing")
                : static () => { };

            return ModSettingsBindings.Callback(modId, dataKey, read, write, save);
        }

        private static IModSettingsValueBinding<TValue> BuildProjectedBinding<TValue>(
            string modId,
            string dataKey,
            MemberInfo member,
            object? instance,
            ModSettingsBindingAttribute? attr)
        {
            if (attr == null)
                throw new InvalidOperationException(
                    $"Project source requires ModSettingsBindingAttribute on '{member.DeclaringType?.FullName}.{member.Name}'.");
            if (string.IsNullOrWhiteSpace(attr.ProjectParentReadUsing) ||
                string.IsNullOrWhiteSpace(attr.ProjectParentWriteUsing) ||
                string.IsNullOrWhiteSpace(attr.ProjectGetUsing) ||
                string.IsNullOrWhiteSpace(attr.ProjectSetUsing))
                throw new InvalidOperationException(
                    $"Project source on '{member.DeclaringType?.FullName}.{member.Name}' requires ProjectParentReadUsing, ProjectParentWriteUsing, ProjectGetUsing and ProjectSetUsing.");

            var parentReadMethod =
                RuntimeReflectionMethodBinder.Resolve(member, instance, attr.ProjectParentReadUsing!,
                    "ProjectParentReadUsing");
            var parentType = parentReadMethod.ReturnType;
            var builder = typeof(RuntimeReflectionMirrorSource)
                .GetMethod(nameof(BuildProjectedBindingGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(typeof(TValue), parentType);
            return (IModSettingsValueBinding<TValue>)builder.Invoke(null, [modId, dataKey, member, instance, attr])!;
        }

        private static IModSettingsValueBinding<TValue> BuildProjectedBindingGeneric<TValue, TParent>(
            string modId,
            string dataKey,
            MemberInfo member,
            object? instance,
            ModSettingsBindingAttribute attr)
        {
            var parentRead = ResolveFuncNoArg<TParent>(member, instance, attr.ProjectParentReadUsing!,
                "ProjectParentReadUsing");
            var parentWrite = ResolveActionOneArg<TParent>(member, instance, attr.ProjectParentWriteUsing!,
                "ProjectParentWriteUsing");
            var parentSave = !string.IsNullOrWhiteSpace(attr.ProjectParentSaveUsing)
                ? ResolveActionNoArg(member, instance, attr.ProjectParentSaveUsing!, "ProjectParentSaveUsing")
                : (Action)(static () => { });
            var getter = ResolveFuncOneArg<TParent, TValue>(member, instance, attr.ProjectGetUsing!, "ProjectGetUsing");
            var setter =
                ResolveFuncTwoArgs<TParent, TValue, TParent>(member, instance, attr.ProjectSetUsing!,
                    "ProjectSetUsing");
            var parentBinding = ModSettingsBindings.Callback(
                modId,
                dataKey + ".parent",
                parentRead,
                parentWrite,
                parentSave);

            return ModSettingsBindings.Project(
                parentBinding,
                string.IsNullOrWhiteSpace(attr.ProjectDataKey) ? member.Name : attr.ProjectDataKey!.Trim(),
                getter,
                setter);
        }

        private static Func<T> ResolveFuncNoArg<T>(
            MemberInfo member,
            object? instance,
            string methodName,
            string propertyName)
        {
            var method = RuntimeReflectionMethodBinder.Resolve(member, instance, methodName, propertyName);
            if (method.ReturnType != typeof(T) || method.GetParameters().Length != 0)
                throw new InvalidOperationException(
                    $"Method '{method.DeclaringType?.FullName}.{method.Name}' configured by '{propertyName}' must be '{typeof(T).Name} ()'.");
            return () => FastMethodInvoker.Invoke0<T>(method, instance)!;
        }

        private static Func<T1, TResult> ResolveFuncOneArg<T1, TResult>(
            MemberInfo member,
            object? instance,
            string methodName,
            string propertyName)
        {
            var method = RuntimeReflectionMethodBinder.Resolve(member, instance, methodName, propertyName);
            var ps = method.GetParameters();
            if (method.ReturnType != typeof(TResult) || ps.Length != 1 || ps[0].ParameterType != typeof(T1))
                throw new InvalidOperationException(
                    $"Method '{method.DeclaringType?.FullName}.{method.Name}' configured by '{propertyName}' must be '{typeof(TResult).Name} ({typeof(T1).Name})'.");
            return arg => FastMethodInvoker.Invoke1<T1, TResult>(method, instance, arg)!;
        }

        private static Func<T1, T2, TResult> ResolveFuncTwoArgs<T1, T2, TResult>(
            MemberInfo member,
            object? instance,
            string methodName,
            string propertyName)
        {
            var method = RuntimeReflectionMethodBinder.Resolve(member, instance, methodName, propertyName);
            var ps = method.GetParameters();
            if (method.ReturnType != typeof(TResult) || ps.Length != 2 ||
                ps[0].ParameterType != typeof(T1) || ps[1].ParameterType != typeof(T2))
                throw new InvalidOperationException(
                    $"Method '{method.DeclaringType?.FullName}.{method.Name}' configured by '{propertyName}' must be '{typeof(TResult).Name} ({typeof(T1).Name}, {typeof(T2).Name})'.");
            return (arg1, arg2) => FastMethodInvoker.Invoke2<T1, T2, TResult>(method, instance, arg1, arg2)!;
        }

        private static Action<T> ResolveActionOneArg<T>(
            MemberInfo member,
            object? instance,
            string methodName,
            string propertyName)
        {
            var method = RuntimeReflectionMethodBinder.Resolve(member, instance, methodName, propertyName);
            var ps = method.GetParameters();
            if (method.ReturnType != typeof(void) || ps.Length != 1 || ps[0].ParameterType != typeof(T))
                throw new InvalidOperationException(
                    $"Method '{method.DeclaringType?.FullName}.{method.Name}' configured by '{propertyName}' must be 'void ({typeof(T).Name})'.");
            return value => FastMethodInvoker.Invoke1Void(method, instance, value);
        }

        private static Action ResolveActionNoArg(
            MemberInfo member,
            object? instance,
            string methodName,
            string propertyName)
        {
            var method = RuntimeReflectionMethodBinder.Resolve(member, instance, methodName, propertyName);
            if (method.ReturnType != typeof(void) || method.GetParameters().Length != 0)
                throw new InvalidOperationException(
                    $"Method '{method.DeclaringType?.FullName}.{method.Name}' configured by '{propertyName}' must be 'void ()'.");
            return () => FastMethodInvoker.Invoke0Void(method, instance);
        }

        private static string? InvokeText(MethodInfo method, object? instance)
        {
            if (method.ReturnType != typeof(string) || method.GetParameters().Length != 0)
                return null;

            try
            {
                return FastMethodInvoker.Invoke0(method, instance)?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static Func<Texture2D?> BuildTextureProvider(MethodInfo method, object? instance)
        {
            return () =>
            {
                if (method.GetParameters().Length != 0)
                    return null;
                var result = FastMethodInvoker.Invoke0(method, instance);
                return result as Texture2D;
            };
        }

        private static void InvokeAction(MethodInfo method, object? instance, IModSettingsUiActionHost? host)
        {
            var ps = method.GetParameters();
            switch (ps.Length)
            {
                case 0:
                    FastMethodInvoker.Invoke0Void(method, instance);
                    return;
                case 1 when host != null && ps[0].ParameterType.IsInstanceOfType(host):
                    FastMethodInvoker.Invoke1Void(method, instance, host);
                    return;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported action signature: {method.DeclaringType?.FullName}.{method.Name}");
            }
        }

        private static Control InvokeCustomControl(MethodInfo method, object? instance, IModSettingsUiActionHost host)
        {
            var ps = method.GetParameters();
            var result = ps.Length switch
            {
                0 => FastMethodInvoker.Invoke0(method, instance),
                1 when ps[0].ParameterType == typeof(IModSettingsUiActionHost) => FastMethodInvoker.Invoke1(method,
                    instance, host),
                _ => throw new InvalidOperationException(
                    $"Unsupported custom entry signature: {method.DeclaringType?.FullName}.{method.Name}"),
            };

            if (result is Control control)
                return control;
            throw new InvalidOperationException(
                $"Custom entry method must return Godot.Control: {method.DeclaringType?.FullName}.{method.Name}");
        }

        private static void AddEntry(IDictionary<string, MutableSection> sections, string sectionId, int order,
            ModSettingsMirrorEntryDefinition entry)
        {
            if (string.IsNullOrWhiteSpace(sectionId))
                return;

            if (!sections.TryGetValue(sectionId, out var section))
            {
                section = new(new(sectionId));
                sections[sectionId] = section;
            }

            section.Entries.Add(new(order, entry));
        }

        private sealed class ReflectionBindingBox<TValue>
        {
            public TValue Value { get; set; } = default!;
        }

        private sealed class MutableSection(ModSettingsSectionAttribute source)
        {
            public string Id { get; } = source.Id;
            public int SortOrder { get; } = source.SortOrder;
            public ModSettingsText? Title { get; } = ToTextOrNull(source, nameof(source.Title), source.Title);

            public ModSettingsText? Description { get; } =
                ToTextOrNull(source, nameof(source.Description), source.Description);

            public bool IsCollapsible { get; } = source.IsCollapsible;
            public bool StartCollapsed { get; } = source.StartCollapsed;
            public List<EntryWithOrder> Entries { get; } = [];

            public ModSettingsMirrorSectionDefinition Build()
            {
                var entries = Entries.OrderBy(static e => e.Order).Select(static e => e.Entry).ToArray();
                return new(Id, entries, Title, Description, IsCollapsible, StartCollapsed);
            }
        }

        private sealed record EntryWithOrder(int Order, ModSettingsMirrorEntryDefinition Entry);
    }
}
