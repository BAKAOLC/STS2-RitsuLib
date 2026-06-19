using System.Collections;
using System.Reflection;
using Godot;
using STS2RitsuLib.Compat;
using STS2RitsuLib.RuntimeInput;

namespace STS2RitsuLib.Settings
{
    internal static class JmcModLibMirrorSource
    {
        internal const string ConfigManagerTypeName = "JmcModLib.Config.ConfigManager";
        private const string ModRegistryTypeName = "JmcModLib.Core.ModRegistry";
        private const string JmcKeyBindingTypeName = "JmcModLib.Config.UI.JmcKeyBinding";
        private const string ActionPrefix = "action:";

        private static readonly Lock Gate = new();
        private static readonly HashSet<string> RegisteredPageKeys = new(StringComparer.Ordinal);

        public static bool IsJmcModLibPresent =>
            ExternalFrameworkRegistry.IsFrameworkPresent(ExternalFrameworkIds.JmcModLib);

        public static int TryRegisterMirroredPages(string pageId = "jmcmodlib", int sortOrder = 10_200)
        {
            lock (Gate)
            {
                var configManagerType = ExternalFrameworkRegistry.ResolveType(ConfigManagerTypeName);
                if (configManagerType == null)
                    return 0;

                var getEntries = configManagerType.GetMethod("GetEntries", BindingFlags.Public | BindingFlags.Static,
                    null, [typeof(Assembly)], null);
                var getGroups = configManagerType.GetMethod("GetGroups", BindingFlags.Public | BindingFlags.Static,
                    null, [typeof(Assembly)], null);
                var flush = configManagerType.GetMethod("Flush", BindingFlags.Public | BindingFlags.Static,
                    null, [typeof(Assembly)], null);
                var resetAssembly = configManagerType.GetMethod("ResetAssembly",
                    BindingFlags.Public | BindingFlags.Static,
                    null, [typeof(Assembly)], null);
                if (getEntries == null || flush == null)
                    return 0;

                var count = 0;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var entries = ReadEntries(getEntries, assembly);
                    if (entries.Count == 0)
                        continue;

                    var context = TryReadContext(assembly);
                    var modId = context.ModId ?? assembly.GetName().Name;
                    if (string.IsNullOrWhiteSpace(modId))
                        continue;

                    var pageKey = $"{pageId}:{modId}";
                    if (RegisteredPageKeys.Contains(pageKey))
                        continue;

                    if (!ModSettingsMirrorInteropPolicy.ShouldMirror(ModSettingsMirrorSource.JmcModLib, modId))
                        continue;

                    var page = TryCreatePage(
                        modId,
                        context.DisplayName,
                        pageId,
                        sortOrder,
                        assembly,
                        entries,
                        getGroups,
                        flush,
                        resetAssembly);
                    if (page == null)
                        continue;

                    if (!ModSettingsMirrorRegistrar.TryRegister(page, ModSettingsMirrorSource.JmcModLib))
                        continue;

                    RegisteredPageKeys.Add(pageKey);
                    count++;
                }

                return count;
            }
        }

        private static ModSettingsMirrorPageDefinition? TryCreatePage(
            string modId,
            string? displayName,
            string pageId,
            int sortOrder,
            Assembly assembly,
            IReadOnlyList<object> entries,
            MethodInfo? getGroups,
            MethodInfo flush,
            MethodInfo? resetAssembly)
        {
            var groups = ReadGroups(getGroups, assembly, entries);
            var sections = new List<ModSettingsMirrorSectionDefinition>();

            foreach (var group in groups)
            {
                var sectionEntries = entries
                    .Where(entry => string.Equals(ReadStringProperty(entry, "Group"), group, StringComparison.Ordinal))
                    .Select(TryCreateEntry)
                    .Where(static entry => entry != null)
                    .Select(static entry => entry!)
                    .ToArray();

                if (sectionEntries.Length == 0)
                    continue;

                sections.Add(new(
                    ModSettingsMirrorSlugPolicy.Normalize(group),
                    sectionEntries,
                    JmcText(() => ResolveJmcGroupName(assembly, group, entries))));
            }

            if (sections.Count == 0)
                return null;

            ModSettingsMirrorButtonDefinition? restore = null;
            if (resetAssembly != null)
                restore = new(
                    "jmc_restore_defaults",
                    ModSettingsLocalization.Text("jmc.mirror.restore.label", "JmcModLib defaults"),
                    ModSettingsLocalization.Text("button.restoreDefaults", "Restore defaults"),
                    () =>
                    {
                        resetAssembly.Invoke(null, [assembly]);
                        flush.Invoke(null, [assembly]);
                    },
                    ModSettingsButtonTone.Danger,
                    ModSettingsLocalization.Text("jmc.mirror.restore.description",
                        "Restores all JmcModLib-managed config entries for this mod."));

            return new(
                modId,
                $"{pageId}_{ModSettingsMirrorSlugPolicy.Normalize(modId)}",
                sortOrder,
                sections,
                ModSettingsText.Literal(displayName ?? modId),
                ModSettingsLocalization.Text("jmc.mirror.page.description",
                    "Auto-generated proxy settings for JmcModLib-managed config entries."),
                string.IsNullOrWhiteSpace(displayName) ? null : ModSettingsText.Literal(displayName),
                null,
                null,
                restore);
        }

        private static ModSettingsMirrorEntryDefinition? TryCreateEntry(object entry)
        {
            try
            {
                var id = ModSettingsMirrorSlugPolicy.Normalize(ReadStringProperty(entry, "Key") ??
                                                               ReadStringProperty(entry, "StorageKey") ??
                                                               ReadStringProperty(entry, "DisplayName") ??
                                                               entry.GetHashCode().ToString());
                var label = JmcText(() => ResolveJmcDisplayName(entry));
                var description = JmcTextOrNull(() => ResolveJmcDescription(entry));
                var valueType = ReadTypeProperty(entry, "ValueType");
                if (valueType == typeof(void) || entry.GetType().Name.Equals("ButtonEntry", StringComparison.Ordinal))
                    return CreateButtonEntry(id, label, description, entry);

                var uiAttribute = ReadProperty(entry, "UIAttribute");
                var uiName = uiAttribute?.GetType().Name ?? string.Empty;

                if (uiName == "UIToggleAttribute" || valueType == typeof(bool))
                    return new(id, ModSettingsMirrorEntryKind.Toggle, label, CreateBinding(
                        entry,
                        value => value is bool b && b,
                        value => value));

                if (uiName == "UIKeybindAttribute" || valueType == typeof(Key) ||
                    valueType?.FullName == JmcKeyBindingTypeName)
                    return CreateKeybindEntry(id, label, description, entry, valueType, uiAttribute);

                if (uiName == "UIInputAttribute" || valueType == typeof(string))
                    return CreateStringEntry(id, label, description, entry, uiAttribute);

                if (uiName == "UIColorAttribute" || valueType == typeof(Color))
                    return CreateColorEntry(id, label, description, entry, uiAttribute, valueType);

                if (uiName is "UISliderAttribute" or "UIIntSliderAttribute")
                    return CreateSliderEntry(id, label, description, entry, uiAttribute, valueType);

                if (uiName == "UIDropdownAttribute")
                    return CreateChoiceEntry(id, label, description, entry, uiAttribute, valueType);

                return CreateFallbackEntry(id, label, description, entry, valueType);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Settings] Failed to mirror JmcModLib config entry: {ex.Message}");
                return null;
            }
        }

        private static ModSettingsMirrorEntryDefinition CreateButtonEntry(
            string id,
            ModSettingsText label,
            ModSettingsText? description,
            object entry)
        {
            var invoke = entry.GetType().GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public);
            return new(
                id,
                ModSettingsMirrorEntryKind.Button,
                label,
                Description: description,
                ButtonLabel: JmcText(() => ResolveJmcButtonText(entry)),
                OnClick: () => invoke?.Invoke(entry, null));
        }

        private static ModSettingsMirrorEntryDefinition CreateKeybindEntry(
            string id,
            ModSettingsText label,
            ModSettingsText? description,
            object entry,
            Type? valueType,
            object? uiAttribute)
        {
            var allowController = ReadBoolProperty(uiAttribute, "AllowController");
            var allowKeyboard = ReadBoolProperty(uiAttribute, "AllowKeyboard", true);

            if (valueType == typeof(Key))
                return new(
                    id,
                    ModSettingsMirrorEntryKind.KeyBinding,
                    label,
                    CreateBinding(
                        entry,
                        value => value is Key key && key != Key.None ? key.ToString() : string.Empty,
                        value => Enum.TryParse<Key>(StripActionBinding(value), true, out var key) ? key : Key.None),
                    description,
                    AllowModifierCombos: false,
                    AllowModifierOnly: false);

            return new(
                id,
                ModSettingsMirrorEntryKind.InputBinding,
                label,
                CreateBinding(
                    entry,
                    JmcBindingToRitsuBinding,
                    value => RitsuBindingToJmcValue(value, entry)),
                description,
                AllowModifierCombos: allowKeyboard,
                AllowModifierOnly: false,
                AllowActionBindings: allowController);
        }

        private static ModSettingsMirrorEntryDefinition CreateStringEntry(
            string id,
            ModSettingsText label,
            ModSettingsText? description,
            object entry,
            object? uiAttribute)
        {
            var multiline = ReadBoolProperty(uiAttribute, "Multiline");
            var maxLength = ReadIntProperty(uiAttribute, "CharacterLimit");
            return new(
                id,
                multiline ? ModSettingsMirrorEntryKind.MultilineString : ModSettingsMirrorEntryKind.String,
                label,
                CreateBinding(entry, value => value?.ToString() ?? string.Empty, value => value),
                description,
                MaxLength: maxLength > 0 ? maxLength : null);
        }

        private static ModSettingsMirrorEntryDefinition CreateColorEntry(
            string id,
            ModSettingsText label,
            ModSettingsText? description,
            object entry,
            object? uiAttribute,
            Type? valueType)
        {
            return new(
                id,
                ModSettingsMirrorEntryKind.Color,
                label,
                CreateBinding(
                    entry,
                    value => value is Color color
                        ? ModSettingsColorControl.FormatStoredColorString(color)
                        : value?.ToString() ?? string.Empty,
                    value =>
                    {
                        if (valueType == typeof(Color) &&
                            ModSettingsColorControl.TryDeserializeColorForSettings(value, out var color))
                            return color;
                        return value;
                    }),
                description,
                EditAlpha: ReadBoolProperty(uiAttribute, "AllowAlpha", true));
        }

        private static ModSettingsMirrorEntryDefinition CreateSliderEntry(
            string id,
            ModSettingsText label,
            ModSettingsText? description,
            object entry,
            object? uiAttribute,
            Type? valueType)
        {
            var min = ReadDoubleProperty(uiAttribute, "Min");
            var max = ReadDoubleProperty(uiAttribute, "Max", 100.0);
            var step = ReadDoubleProperty(uiAttribute, "Step", 1.0);

            if (valueType == typeof(int))
                return new(
                    id,
                    ModSettingsMirrorEntryKind.IntSlider,
                    label,
                    CreateBinding(
                        entry,
                        value => Convert.ToInt32(value ?? 0),
                        value => value),
                    description,
                    new(min, max, step));

            return new(
                id,
                ModSettingsMirrorEntryKind.Slider,
                label,
                CreateBinding(
                    entry,
                    value => Convert.ToDouble(value ?? 0.0),
                    value => Convert.ChangeType(value, Nullable.GetUnderlyingType(valueType!) ?? valueType!)),
                description,
                new(min, max, step));
        }

        private static ModSettingsMirrorEntryDefinition CreateChoiceEntry(
            string id,
            ModSettingsText label,
            ModSettingsText? description,
            object entry,
            object? uiAttribute,
            Type? valueType)
        {
            var actualType = Nullable.GetUnderlyingType(valueType ?? typeof(string)) ?? valueType ?? typeof(string);
            if (actualType.IsEnum)
                return new(
                    id,
                    ModSettingsMirrorEntryKind.EnumChoice,
                    label,
                    CreateEnumBinding(entry, actualType),
                    description,
                    ChoicePresentation: ModSettingsChoicePresentation.Dropdown,
                    EnumType: actualType);

            var options = ReadStringListProperty(uiAttribute, "Options");
            if (options.Count == 0)
                return CreateStringEntry(id, label, description, entry, null);

            return new(
                id,
                ModSettingsMirrorEntryKind.Choice,
                label,
                CreateBinding(entry, value => value?.ToString() ?? string.Empty, value => value),
                description,
                ChoiceOptions: options
                    .Select(option => new ModSettingsMirrorChoiceOption(option,
                        JmcText(() => ResolveJmcOptionText(entry, option))))
                    .ToArray(),
                ChoicePresentation: ModSettingsChoicePresentation.Dropdown);
        }

        private static ModSettingsMirrorEntryDefinition CreateFallbackEntry(
            string id,
            ModSettingsText label,
            ModSettingsText? description,
            object entry,
            Type? valueType)
        {
            if (valueType?.IsEnum == true)
                return new(
                    id,
                    ModSettingsMirrorEntryKind.EnumChoice,
                    label,
                    CreateEnumBinding(entry, valueType),
                    description,
                    ChoicePresentation: ModSettingsChoicePresentation.Dropdown,
                    EnumType: valueType);

            return CreateStringEntry(id, label, description, entry, null);
        }

        private static IModSettingsValueBinding<T> CreateBinding<T>(
            object entry,
            Func<object?, T> read,
            Func<T, object?> write)
        {
            var assembly = ReadProperty(entry, "Assembly") as Assembly;
            var modId = TryReadContext(assembly).ModId ?? assembly?.GetName().Name ?? "jmcmodlib";
            var dataKey = ReadStringProperty(entry, "Key") ??
                          ReadStringProperty(entry, "StorageKey") ?? entry.GetHashCode().ToString();
            return ModSettingsBindings.Callback(
                modId,
                dataKey,
                () => read(Invoke(entry, "GetValue")),
                value => Invoke(entry, "SetValue", write(value)),
                () =>
                {
                    if (assembly != null &&
                        ExternalFrameworkRegistry.ResolveType(ConfigManagerTypeName)
                                ?.GetMethod("Flush", BindingFlags.Public | BindingFlags.Static, null,
                                    [typeof(Assembly)], null) is
                            { } flush)
                        flush.Invoke(null, [assembly]);
                });
        }

        private static object CreateEnumBinding(object entry, Type enumType)
        {
            var method = typeof(JmcModLibMirrorSource)
                .GetMethod(nameof(CreateEnumBindingGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(enumType);
            return method.Invoke(null, [entry])!;
        }

        private static IModSettingsValueBinding<TEnum> CreateEnumBindingGeneric<TEnum>(object entry)
            where TEnum : struct, Enum
        {
            return CreateBinding(
                entry,
                value => value is TEnum enumValue
                    ? enumValue
                    : Enum.TryParse<TEnum>(value?.ToString(), true, out var parsed)
                        ? parsed
                        : default,
                value => value);
        }

        private static string JmcBindingToRitsuBinding(object? value)
        {
            if (value == null)
                return string.Empty;

            var type = value.GetType();
            var keyboard = ReadProperty(value, "Keyboard");
            var controller = ReadStringProperty(value, "Controller");
            if (keyboard is Key key && key != Key.None)
            {
                var modifiers = ReadProperty(value, "Modifiers");
                var parts = new List<string>();
                AddModifier(parts, modifiers, "Ctrl");
                AddModifier(parts, modifiers, "Alt");
                AddModifier(parts, modifiers, "Shift");
                AddModifier(parts, modifiers, "Meta");
                parts.Add(key.ToString());
                return RuntimeHotkeyService.NormalizeOrDefault(string.Join('+', parts), key.ToString());
            }

            if (!string.IsNullOrWhiteSpace(controller))
                return RuntimeHotkeyService.ActionBinding(controller);

            return type == typeof(Key) && value is Key directKey && directKey != Key.None
                ? directKey.ToString()
                : string.Empty;
        }

        private static object? RitsuBindingToJmcValue(string binding, object entry)
        {
            var valueType = ReadTypeProperty(entry, "ValueType");
            if (valueType == typeof(Key))
                return Enum.TryParse<Key>(StripActionBinding(binding), true, out var key) ? key : Key.None;

            if (valueType?.FullName != JmcKeyBindingTypeName)
                return binding;

            var current = Invoke(entry, "GetValue");
            var keyboard = ReadProperty(current, "Keyboard") is Key existingKey ? existingKey : Key.None;
            var controller = ReadStringProperty(current, "Controller") ?? string.Empty;
            var modifiers = ReadProperty(current, "Modifiers") ?? CreateEnumValue(valueType, "Modifiers", "None");
            var enabled = ReadBoolProperty(current, "Enabled", true);

            if (binding.StartsWith(ActionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                controller = StripActionBinding(binding);
            }
            else if (TryParseKeyBindingForJmc(binding, valueType, out var parsedKey, out var parsedModifiers))
            {
                keyboard = parsedKey;
                modifiers = parsedModifiers;
            }
            else
            {
                keyboard = Key.None;
            }

            return Activator.CreateInstance(valueType, keyboard, controller, modifiers, enabled);
        }

        private static bool TryParseKeyBindingForJmc(string binding, Type keyBindingType, out Key key,
            out object modifiers)
        {
            key = Key.None;
            modifiers = CreateEnumValue(keyBindingType, "Modifiers", "None")!;
            if (string.IsNullOrWhiteSpace(binding))
                return true;

            var modifierType = keyBindingType.Assembly.GetType("JmcModLib.Config.UI.JmcKeyModifiers");
            if (modifierType == null)
                return false;

            var parts = binding.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("Alt", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("Shift", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("Meta", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers = CombineEnumFlags(modifierType, modifiers, part);
                    continue;
                }

                if (Enum.TryParse(part, true, out key))
                    continue;

                key = Key.None;
                return false;
            }

            return true;
        }

        private static void AddModifier(List<string> parts, object? modifiers, string name)
        {
            if (modifiers == null)
                return;

            var flag = Enum.Parse(modifiers.GetType(), name);
            var value = Convert.ToInt64(modifiers);
            var flagValue = Convert.ToInt64(flag);
            if ((value & flagValue) == flagValue)
                parts.Add(name);
        }

        private static object? CreateEnumValue(Type keyBindingType, string enumPropertyName, string name)
        {
            var enumType = keyBindingType.GetProperty(enumPropertyName, BindingFlags.Instance | BindingFlags.Public)
                ?.PropertyType;
            return enumType == null ? null : Enum.Parse(enumType, name);
        }

        private static object CombineEnumFlags(Type enumType, object current, string name)
        {
            var flag = Enum.Parse(enumType, name);
            return Enum.ToObject(enumType, Convert.ToInt64(current) | Convert.ToInt64(flag));
        }

        private static string StripActionBinding(string value)
        {
            return value.StartsWith(ActionPrefix, StringComparison.OrdinalIgnoreCase)
                ? value[ActionPrefix.Length..].Trim()
                : value.Trim();
        }

        private static IReadOnlyList<object> ReadEntries(MethodInfo getEntries, Assembly assembly)
        {
            if (getEntries.Invoke(null, [assembly]) is not IEnumerable enumerable)
                return [];

            var entries = new List<object>();
            foreach (var item in enumerable)
                if (item != null)
                    entries.Add(item);
            return entries;
        }

        private static IReadOnlyList<string> ReadGroups(MethodInfo? getGroups, Assembly assembly,
            IReadOnlyList<object> entries)
        {
            if (getGroups?.Invoke(null, [assembly]) is IEnumerable enumerable)
            {
                var groups = new List<string>();
                foreach (var item in enumerable)
                    if (item?.ToString() is { Length: > 0 } group)
                        groups.Add(group);
                if (groups.Count > 0)
                    return groups;
            }

            return entries
                .Select(entry => ReadStringProperty(entry, "Group"))
                .Where(static group => !string.IsNullOrWhiteSpace(group))
                .Distinct(StringComparer.Ordinal)
                .ToArray()!;
        }

        private static JmcModContext TryReadContext(Assembly? assembly)
        {
            if (assembly == null)
                return default;

            try
            {
                var modRegistryType = ExternalFrameworkRegistry.ResolveType(ModRegistryTypeName);
                var getContext = modRegistryType?.GetMethod("GetContext", BindingFlags.Public | BindingFlags.Static,
                    null, [typeof(Assembly)], null);
                var context = getContext?.Invoke(null, [assembly]);
                return new(
                    ReadStringProperty(context, "ModId"),
                    ReadStringProperty(context, "DisplayName"));
            }
            catch
            {
                return default;
            }
        }

        private static object? Invoke(object target, string method, params object?[] args)
        {
            return target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(target, args);
        }

        private static object? ReadProperty(object? target, string property)
        {
            return target?.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(target);
        }

        private static string? ReadStringProperty(object? target, string property)
        {
            return ReadProperty(target, property)?.ToString();
        }

        private static Type? ReadTypeProperty(object? target, string property)
        {
            return ReadProperty(target, property) as Type;
        }

        private static bool ReadBoolProperty(object? target, string property, bool fallback = false)
        {
            return ReadProperty(target, property) is bool value ? value : fallback;
        }

        private static int ReadIntProperty(object? target, string property, int fallback = 0)
        {
            return ReadProperty(target, property) is { } value ? Convert.ToInt32(value) : fallback;
        }

        private static double ReadDoubleProperty(object? target, string property, double fallback = 0.0)
        {
            return ReadProperty(target, property) is { } value ? Convert.ToDouble(value) : fallback;
        }

        private static IReadOnlyList<string> ReadStringListProperty(object? target, string property)
        {
            if (ReadProperty(target, property) is not IEnumerable enumerable)
                return [];

            var values = new List<string>();
            foreach (var item in enumerable)
                if (item?.ToString() is { Length: > 0 } value)
                    values.Add(value);
            return values;
        }

        private static ModSettingsText JmcText(Func<string?> resolve)
        {
            return ModSettingsText.Dynamic(() => resolve() ?? string.Empty);
        }

        private static ModSettingsText? JmcTextOrNull(Func<string?> resolve)
        {
            return ModSettingsText.Dynamic(() => resolve() ?? string.Empty);
        }

        private static string ResolveJmcDisplayName(object entry)
        {
            return InvokeConfigLocalization("GetDisplayName", [entry]) ??
                   ReadStringProperty(entry, "DisplayName") ??
                   ReadStringProperty(entry, "StorageKey") ??
                   string.Empty;
        }

        private static string? ResolveJmcDescription(object entry)
        {
            return InvokeConfigLocalization("GetDescription", [entry]) ??
                   ReadStringProperty(ReadProperty(entry, "Attribute"), "Description");
        }

        private static string ResolveJmcButtonText(object entry)
        {
            return InvokeConfigLocalization("GetButtonText", [entry]) ??
                   ReadStringProperty(entry, "ButtonText") ??
                   "Run";
        }

        private static string ResolveJmcOptionText(object entry, string option)
        {
            return InvokeConfigLocalization("GetOptionText", [entry, option]) ?? option;
        }

        private static string ResolveJmcGroupName(Assembly assembly, string group, IReadOnlyList<object> entries)
        {
            return InvokeConfigLocalization("GetGroupName", [assembly, group, entries]) ?? group;
        }

        private static string? InvokeConfigLocalization(string methodName, object?[] args)
        {
            try
            {
                var localizationType = ExternalFrameworkRegistry.ResolveType("JmcModLib.Config.ConfigLocalization");
                var methods = localizationType
                    ?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(method => method.Name == methodName && method.GetParameters().Length == args.Length);
                if (methods == null)
                    return null;

                foreach (var method in methods)
                {
                    var coerced = CoerceLocalizationArgs(method, args);
                    if (coerced == null)
                        continue;
                    return method.Invoke(null, coerced)?.ToString();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static object?[]? CoerceLocalizationArgs(MethodInfo method, object?[] args)
        {
            var parameters = method.GetParameters();
            var result = new object?[args.Length];
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                var parameterType = parameters[i].ParameterType;
                if (arg == null || parameterType.IsInstanceOfType(arg))
                {
                    result[i] = arg;
                    continue;
                }

                if (arg is IEnumerable<object> objects &&
                    parameterType.IsGenericType &&
                    parameterType.GetGenericArguments() is [{ } itemType] &&
                    parameterType.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>))
                {
                    var items = objects.Where(item => itemType.IsInstanceOfType(item)).ToArray();
                    var array = Array.CreateInstance(itemType, items.Length);
                    for (var itemIndex = 0; itemIndex < items.Length; itemIndex++)
                        array.SetValue(items[itemIndex], itemIndex);
                    result[i] = array;
                    continue;
                }

                return null;
            }

            return result;
        }

        private readonly record struct JmcModContext(string? ModId, string? DisplayName);
    }
}
