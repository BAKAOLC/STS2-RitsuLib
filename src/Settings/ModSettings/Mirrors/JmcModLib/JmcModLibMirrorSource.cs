using System.Collections;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using STS2RitsuLib.Compat;
using STS2RitsuLib.RuntimeInput;

namespace STS2RitsuLib.Settings
{
    internal static class JmcModLibMirrorSource
    {
        internal const string ConfigManagerTypeName = "JmcModLib.Config.ConfigManager";
        private const string ModRegistryTypeName = "JmcModLib.Core.ModRegistry";
        private const string JmcKeyBindingTypeName = "JmcModLib.Config.UI.JmcKeyBinding";
        private const string JmcSecretEntryTypeName = "JmcModLib.Security.SecretEntry";
        private const string ActionPrefix = "action:";

        private static readonly Lock Gate = new();
        private static readonly HashSet<string> RegisteredPageKeys = new(StringComparer.Ordinal);
        private static readonly HashSet<Assembly> RestartPendingAssemblies = [];

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
            var sections = (from @group in groups
                let sectionEntries = entries.Where(entry =>
                        string.Equals(ReadStringProperty(entry, "Group"), @group, StringComparison.Ordinal))
                    .Select(TryCreateEntry)
                    .Where(static entry => entry != null)
                    .Select(static entry => entry!)
                    .ToArray()
                where sectionEntries.Length != 0
                select new ModSettingsMirrorSectionDefinition(ModSettingsMirrorSlugPolicy.Normalize(@group),
                    sectionEntries, JmcText(() => ResolveJmcGroupName(assembly, @group, entries)))).ToList();

            if (sections.Count == 0)
                return null;

            if (entries.Any(IsRestartRequired))
                sections.Add(new(
                    "jmc_restart",
                    [CreateRestartEntry(assembly)],
                    ModSettingsLocalization.Text("jmc.mirror.restart.section", "Restart"),
                    VisibleWhen: () => RestartPendingAssemblies.Contains(assembly)));

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
                        if (entries.Any(IsRestartRequired))
                            RestartPendingAssemblies.Add(assembly);
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
                if (entry.GetType().FullName == JmcSecretEntryTypeName)
                    return CreateSecretEntry(id, label, description, entry) with
                    {
                        VisibleWhen = CreateVisibilityPredicate(entry),
                    };

                ModSettingsMirrorEntryDefinition definition;
                if (valueType == typeof(void) || entry.GetType().Name.Equals("ButtonEntry", StringComparison.Ordinal))
                {
                    definition = CreateButtonEntry(id, label, description, entry);
                }
                else
                {
                    var uiAttribute = ReadProperty(entry, "UIAttribute");
                    var uiName = uiAttribute?.GetType().Name ?? string.Empty;

                    if (uiName == "UIToggleAttribute" || valueType == typeof(bool))
                        definition = new(id, ModSettingsMirrorEntryKind.Toggle, label, CreateBinding(
                                entry,
                                value => value is true,
                                value => value),
                            description);
                    else if (uiName == "UIKeybindAttribute" || valueType == typeof(Key) ||
                             valueType?.FullName == JmcKeyBindingTypeName)
                        definition = CreateKeybindEntry(id, label, description, entry, valueType, uiAttribute);
                    else if (uiName == "UIInputAttribute" || valueType == typeof(string))
                        definition = CreateStringEntry(id, label, description, entry, uiAttribute);
                    else if (uiName == "UIColorAttribute" || valueType == typeof(Color))
                        definition = CreateColorEntry(id, label, description, entry, uiAttribute, valueType);
                    else
                        definition = uiName switch
                        {
                            "UISliderAttribute" or "UIIntSliderAttribute" => CreateSliderEntry(id, label,
                                description, entry, uiAttribute, valueType),
                            "UIDropdownAttribute" => CreateChoiceEntry(id, label, description, entry, uiAttribute,
                                valueType),
                            _ => CreateFallbackEntry(id, label, description, entry, valueType),
                        };
                }

                return definition with { VisibleWhen = CreateVisibilityPredicate(entry) };
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
                OnClick: () => invoke?.Invoke(entry, null),
                ButtonTone: ResolveJmcButtonTone(entry));
        }

        private static ModSettingsMirrorEntryDefinition CreateSecretEntry(
            string id,
            ModSettingsText label,
            ModSettingsText? description,
            object entry)
        {
            return new(
                id,
                ModSettingsMirrorEntryKind.Custom,
                label,
                Description: description,
                CustomControlFactory: host => CreateSecretControl(entry, label, description, host));
        }

        private static Control CreateSecretControl(
            object entry,
            ModSettingsText label,
            ModSettingsText? description,
            IModSettingsUiActionHost host)
        {
            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateSurfaceStyle());

            var margin = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            margin.AddThemeConstantOverride("margin_left", 16);
            margin.AddThemeConstantOverride("margin_top", 12);
            margin.AddThemeConstantOverride("margin_right", 16);
            margin.AddThemeConstantOverride("margin_bottom", 12);
            panel.AddChild(margin);

            var content = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            content.AddThemeConstantOverride("separation", 8);
            margin.AddChild(content);

            var title = ModSettingsUiFactory.CreateHeaderLabel(
                ModSettingsUiContext.Resolve(label),
                18,
                HorizontalAlignment.Left);
            title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            content.AddChild(title);

            if (description != null)
            {
                var body = ModSettingsUiFactory.CreateHeaderLabel(
                    ModSettingsUiContext.Resolve(description),
                    15,
                    HorizontalAlignment.Left);
                body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                body.FitContent = true;
                content.AddChild(body);
            }

            var status = ModSettingsUiFactory.CreateHeaderLabel(
                ResolveJmcSecretStatusText(entry),
                15,
                HorizontalAlignment.Left);
            status.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            status.FitContent = true;
            content.AddChild(status);

            var actions = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                Alignment = BoxContainer.AlignmentMode.End,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            actions.AddThemeConstantOverride("separation", 10);
            content.AddChild(actions);

            var setButton = new ModSettingsTextButton(
                ResolveJmcSecretButtonText(entry, true),
                ModSettingsButtonTone.Accent,
                () => OpenJmcSecretInput(entry, status, host));
            var clearButton = new ModSettingsTextButton(
                ResolveJmcSecretButtonText(entry, false),
                ModSettingsButtonTone.Danger,
                () => ConfirmClearJmcSecret(entry, status, host));
            actions.AddChild(setButton);
            actions.AddChild(clearButton);
            return panel;
        }

        private static async void OpenJmcSecretInput(
            object entry,
            MegaRichTextLabel status,
            IModSettingsUiActionHost host)
        {
            try
            {
                var assembly = entry.GetType().Assembly;
                var popupType = assembly.GetType("JmcModLib.Prefabs.JmcSecretInputPopup");
                var optionsType = assembly.GetType("JmcModLib.Prefabs.JmcSecretInputPopupOptions");
                var slot = ReadProperty(entry, "Slot");
                var protectionLevel = ReadProperty(slot, "ProtectionLevel");
                if (popupType == null || optionsType == null || protectionLevel == null)
                    return;
                if (protectionLevel.ToString() == "Unavailable")
                {
                    ShowJmcNotice(
                        InvokeJmcUiText("SecretInputUnavailableTitle") ?? "Secret storage unavailable",
                        InvokeJmcUiText("SecretInputUnavailableBody") ??
                        "This platform does not currently provide secure Secret storage for this entry.");
                    return;
                }

                var options = Activator.CreateInstance(optionsType)!;
                SetProperty(options, "Title", ResolveJmcDisplayName(entry));
                var popupDescription = ResolveJmcDescription(entry);
                if (protectionLevel.ToString() == "WeakFileProtection")
                {
                    var warning = Colorize("#e0b24f", InvokeJmcUiText("SecretInputWeakWarning") ??
                                                      "Saving will use weak file protection.");
                    popupDescription = string.IsNullOrWhiteSpace(popupDescription)
                        ? warning
                        : $"{popupDescription}\n{warning}";
                }

                SetProperty(options, "Description", popupDescription);
                SetProperty(options, "Placeholder", InvokeJmcUiText("SecretInputPlaceholder"));
                SetProperty(options, "ConfirmText", InvokeJmcUiText("SecretInputConfirm"));
                SetProperty(options, "CancelText", InvokeJmcUiText("SecretInputCancel"));
                SetProperty(options, "EmptyText", InvokeJmcUiText("SecretInputEmpty"));
                SetProperty(options, "ProtectionLevel", protectionLevel);

                if (popupType.GetMethod("PromptAsync", BindingFlags.Public | BindingFlags.Static)
                        ?.Invoke(null, [options, ReadProperty(entry, "Assembly")]) is not Task task)
                    return;

                await task;
                var value = task.GetType().GetProperty("Result")?.GetValue(task) as string;
                if (value == null)
                    return;

                var args = new object?[] { value, null };
                var saved = entry.GetType().GetMethod("TrySave", BindingFlags.Public | BindingFlags.Instance)
                    ?.Invoke(entry, args) is true;
                if (!saved)
                    ShowJmcSecretNotice("SecretInputUnavailableTitle", "SecretSaveFailed", args[1]);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Settings] Failed to update JmcModLib Secret: {ex.Message}");
            }
            finally
            {
                status.Text = ResolveJmcSecretStatusText(entry);
                host.RequestRefresh();
            }
        }

        private static void ConfirmClearJmcSecret(
            object entry,
            MegaRichTextLabel status,
            IModSettingsUiActionHost host)
        {
            if (Engine.GetMainLoop() is not SceneTree { Root: { } root })
                return;

            var submenu = ModSettingsMirrorUiActions.FindRitsuModSettingsSubmenu(root);
            ModSettingsUiFactory.ShowStyledConfirm(
                submenu is null ? root : submenu,
                InvokeJmcUiText("SecretClearTitle") ?? "Clear Secret",
                InvokeJmcUiText("SecretClearBody", ResolveJmcDisplayName(entry)) ??
                $"Clear the saved Secret for {ResolveJmcDisplayName(entry)}?",
                InvokeJmcUiText("SecretInputCancel") ?? "Cancel",
                InvokeJmcUiText("SecretClearButton") ?? "Clear",
                true,
                () =>
                {
                    var args = new object?[] { null };
                    var cleared = entry.GetType().GetMethod("TryDelete", BindingFlags.Public | BindingFlags.Instance)
                        ?.Invoke(entry, args) is true;
                    if (!cleared)
                        ShowJmcSecretNotice("SecretClearTitle", "SecretClearFailed", args[0]);
                    status.Text = ResolveJmcSecretStatusText(entry);
                    host.RequestRefresh();
                });
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

            IReadOnlyList<ModSettingsMirrorChoiceOption> ResolveOptions()
            {
                return ResolveJmcDropdownOptions(entry, uiAttribute, actualType)
                    .Select(option => new ModSettingsMirrorChoiceOption(option,
                        JmcText(() => ResolveJmcOptionText(entry, option))))
                    .ToArray();
            }

            var options = ResolveOptions();
            if (options.Count == 0)
                return CreateStringEntry(id, label, description, entry, null);

            return new(
                id,
                ModSettingsMirrorEntryKind.Choice,
                label,
                CreateBinding(entry, value => value?.ToString() ?? string.Empty,
                    value => ConvertChoiceValue(value, actualType)),
                description,
                ChoiceOptions: options,
                ChoicePresentation: ModSettingsChoicePresentation.Dropdown,
                ChoiceOptionsProvider: ResolveOptions);
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
            var binding = ModSettingsBindings.Callback(
                modId,
                dataKey,
                () => read(Invoke(entry, "GetValue")),
                value =>
                {
                    Invoke(entry, "SetValue", write(value));
                    if (assembly != null && IsRestartRequired(entry))
                        RestartPendingAssemblies.Add(assembly);
                },
                () =>
                {
                    if (assembly != null &&
                        ExternalFrameworkRegistry.ResolveType(ConfigManagerTypeName)
                                ?.GetMethod("Flush", BindingFlags.Public | BindingFlags.Static, null,
                                    [typeof(Assembly)], null) is
                            { } flush)
                        flush.Invoke(null, [assembly]);
                });

            return TryReadDefault(entry, out var defaultValue)
                ? ModSettingsBindings.WithDefault(binding, () => read(defaultValue))
                : binding;
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

        private static Func<bool>? CreateVisibilityPredicate(object entry)
        {
            if (ReadProperty(entry, "VisibleWhenAttribute") == null)
                return null;

            var resolver = entry.GetType().Assembly
                .GetType("JmcModLib.Config.UI.UIVisibilityResolver")
                ?.GetMethod("IsVisible", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return resolver == null
                ? null
                : () =>
                {
                    try
                    {
                        return resolver.Invoke(null, [entry]) is true;
                    }
                    catch (Exception ex)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[Settings] Failed to evaluate JmcModLib visibility for '{ReadStringProperty(entry, "Key")}': {ex.Message}");
                        return false;
                    }
                };
        }

        private static IReadOnlyList<string> ResolveJmcDropdownOptions(object entry, object? uiAttribute,
            Type valueType)
        {
            try
            {
                var resolver = entry.GetType().Assembly
                    .GetType("JmcModLib.Config.UI.DropdownOptionsResolver")
                    ?.GetMethod("Resolve", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                var resolved = resolver?.Invoke(null, [entry, uiAttribute, valueType]);
                var options = resolved is IEnumerable enumerable
                    ? enumerable.OfType<object>().Select(option => option.ToString() ?? string.Empty)
                        .Where(static option => !string.IsNullOrWhiteSpace(option))
                        .Distinct(StringComparer.Ordinal)
                        .ToList()
                    : [];
                if (options.Count == 0)
                    return options;

                var current = Invoke(entry, "GetValue")?.ToString() ?? string.Empty;
                if (options.Contains(current, StringComparer.OrdinalIgnoreCase))
                    return options;

                var provider = ReadProperty(entry, "DropdownOptionsProviderAttribute");
                switch (ReadStringProperty(provider, "InvalidValuePolicy"))
                {
                    case "SelectFirstAvailable":
                        Invoke(entry, "SetValue", ConvertChoiceValue(options[0], valueType));
                        break;
                    case "ResetToDefault":
                    {
                        var defaultValue = ReadProperty(entry, "DefaultValue");
                        Invoke(entry, "SetValue", defaultValue);
                        var defaultText = defaultValue?.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(defaultText) &&
                            !options.Contains(defaultText, StringComparer.OrdinalIgnoreCase))
                            options.Add(defaultText);
                        break;
                    }
                    default:
                        if (!string.IsNullOrWhiteSpace(current))
                            options.Add(current);
                        break;
                }

                return options;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Settings] Failed to resolve JmcModLib dropdown options for '{ReadStringProperty(entry, "Key")}': {ex.Message}");
                return [];
            }
        }

        private static object ConvertChoiceValue(string value, Type valueType)
        {
            return valueType.IsEnum ? Enum.Parse(valueType, value, true) : value;
        }

        private static bool IsRestartRequired(object entry)
        {
            return ReadBoolProperty(ReadProperty(entry, "Attribute"), "RestartRequired");
        }

        private static ModSettingsButtonTone ResolveJmcButtonTone(object entry)
        {
            return ReadStringProperty(entry, "Color") switch
            {
                "Red" or "Reset" => ModSettingsButtonTone.Danger,
                "Green" or "Gold" or "Blue" => ModSettingsButtonTone.Accent,
                _ => ModSettingsButtonTone.Normal,
            };
        }

        private static ModSettingsMirrorEntryDefinition CreateRestartEntry(Assembly assembly)
        {
            return new(
                "jmc_restart_game",
                ModSettingsMirrorEntryKind.Button,
                ModSettingsLocalization.Text("jmc.mirror.restart.label", "Restart required"),
                Description: ModSettingsLocalization.Text("jmc.mirror.restart.description",
                    "Restart the game to fully apply the changed settings."),
                ButtonLabel: ModSettingsLocalization.Text("jmc.mirror.restart.button", "Restart game"),
                OnClick: () => TryShowJmcRestartConfirmation(assembly),
                ButtonTone: ModSettingsButtonTone.Danger);
        }

        private static void TryShowJmcRestartConfirmation(Assembly assembly)
        {
            try
            {
                assembly.GetType("JmcModLib.Utils.GameRestart")
                    ?.GetMethod("ShowRestartConfirmationAsync", BindingFlags.Public | BindingFlags.Static)
                    ?.Invoke(null, [true, assembly]);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Settings] Failed to open JmcModLib restart confirmation: {ex.Message}");
            }
        }

        private static string ResolveJmcSecretButtonText(object entry, bool set)
        {
            var fallbackMethod = set ? "SecretSetButton" : "SecretClearButton";
            var localizationMethod = set ? "GetSecretSetButtonText" : "GetSecretClearButtonText";
            var fallback = InvokeJmcUiText(fallbackMethod) ?? (set ? "Set / Update" : "Clear");
            return InvokeConfigLocalization(localizationMethod, [entry, fallback]) ?? fallback;
        }

        private static string ResolveJmcSecretStatusText(object entry)
        {
            var slot = ReadProperty(entry, "Slot");
            var protection = ReadStringProperty(slot, "ProtectionLevel");
            var exists = Invoke(entry, "Exists") is true;
            return protection switch
            {
                "Unavailable" => Colorize("#d07f7f", InvokeJmcUiText("SecretStatusUnavailable") ??
                                                     "Secure storage is not supported on this platform."),
                "WeakFileProtection" when exists =>
                    Colorize("#e0b24f", InvokeJmcUiText("SecretStatusSavedWeak") ??
                                        "Saved with weak file protection."),
                "WeakFileProtection" =>
                    Colorize("#e0b24f", InvokeJmcUiText("SecretStatusWeak") ??
                                        "Only weak file protection is available."),
                _ when exists => Colorize("#7ee787", InvokeJmcUiText("SecretStatusSaved") ?? "Saved"),
                _ => Colorize("#aab7bc", InvokeJmcUiText("SecretStatusMissing") ?? "Not saved"),
            };
        }

        private static string Colorize(string color, string text)
        {
            return $"[color={color}]{text}[/color]";
        }

        private static void ShowJmcSecretNotice(string titleMethod, string bodyMethod, object? status)
        {
            if (Engine.GetMainLoop() is not SceneTree { Root: { } root })
                return;

            var title = InvokeJmcUiText(titleMethod) ?? "Secret storage error";
            var body = InvokeJmcUiText(bodyMethod, FormatJmcSecretWriteStatus(status)) ??
                       $"Secret operation failed: {FormatJmcSecretWriteStatus(status)}";
            ModSettingsUiFactory.ShowStyledNotice(
                ModSettingsMirrorUiActions.FindRitsuModSettingsSubmenu(root) is { } submenu ? submenu : root,
                title,
                body,
                InvokeJmcUiText("Close") ?? "Close");
        }

        private static void ShowJmcNotice(string title, string body)
        {
            if (Engine.GetMainLoop() is not SceneTree { Root: { } root })
                return;

            ModSettingsUiFactory.ShowStyledNotice(
                ModSettingsMirrorUiActions.FindRitsuModSettingsSubmenu(root) is { } submenu ? submenu : root,
                title,
                body,
                InvokeJmcUiText("Close") ?? "Close");
        }

        private static string FormatJmcSecretWriteStatus(object? status)
        {
            return status?.ToString() switch
            {
                "Unavailable" => InvokeJmcUiText("SecretStatusUnavailable") ?? "Unavailable",
                "AccessDenied" => InvokeJmcUiText("SecretStatusAccessDenied") ?? "Access denied",
                "WeakProtectionNotAllowed" => InvokeJmcUiText("SecretStatusWeakNotAllowed") ??
                                              "Weak file protection is not allowed",
                "BackendError" => InvokeJmcUiText("SecretStatusBackendError") ?? "Secret backend error",
                _ => status?.ToString() ?? "Unknown error",
            };
        }

        private static IReadOnlyList<object> ReadEntries(MethodInfo getEntries, Assembly assembly)
        {
            return getEntries.Invoke(null, [assembly]) is not IEnumerable enumerable
                ? []
                : enumerable.OfType<object>().ToList();
        }

        private static IReadOnlyList<string> ReadGroups(MethodInfo? getGroups, Assembly assembly,
            IReadOnlyList<object> entries)
        {
            if (getGroups?.Invoke(null, [assembly]) is not IEnumerable enumerable)
                return entries
                    .Select(entry => ReadStringProperty(entry, "Group"))
                    .Where(static group => !string.IsNullOrWhiteSpace(group))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray()!;
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

        private static void SetProperty(object target, string property, object? value)
        {
            target.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(target, value);
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

        private static bool TryReadDefault(object entry, out object? value)
        {
            try
            {
                value = ReadProperty(entry, "DefaultValue");
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        private static ModSettingsText JmcText(Func<string?> resolve)
        {
            return ModSettingsText.Dynamic(() => resolve() ?? string.Empty);
        }

        private static ModSettingsText JmcTextOrNull(Func<string?> resolve)
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
            var description = InvokeConfigLocalization("GetDescription", [entry]) ??
                              ReadStringProperty(ReadProperty(entry, "Attribute"), "Description");
            if (!ReadBoolProperty(ReadProperty(entry, "Attribute"), "RestartRequired"))
                return description;

            var restartText = $"[color=#e0b24f]{ResolveJmcRestartRequiredText()}[/color]";
            return string.IsNullOrWhiteSpace(description)
                ? restartText
                : $"{description}\n{restartText}";
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

        private static string ResolveJmcRestartRequiredText()
        {
            return InvokeJmcUiText("RestartRequired") ??
                   ModSettingsLocalization.Get("jmc.mirror.restartRequired", "Requires restart to fully apply.");
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

                // ReSharper disable once LoopCanBeConvertedToQuery
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

        private static string? InvokeJmcUiText(string methodName, params object?[] args)
        {
            try
            {
                var method = ExternalFrameworkRegistry.ResolveType("JmcModLib.Config.UI.ModSettingsText")
                    ?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .FirstOrDefault(candidate => candidate.Name == methodName &&
                                                 candidate.GetParameters().Length == args.Length);
                return method?.Invoke(null, args)?.ToString();
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

                if (arg is not IEnumerable<object> objects ||
                    !parameterType.IsGenericType ||
                    parameterType.GetGenericArguments() is not [{ } itemType] ||
                    parameterType.GetGenericTypeDefinition() != typeof(IReadOnlyCollection<>)) return null;
                var items = objects.Where(item => itemType.IsInstanceOfType(item)).ToArray();
                var array = Array.CreateInstance(itemType, items.Length);
                for (var itemIndex = 0; itemIndex < items.Length; itemIndex++)
                    array.SetValue(items[itemIndex], itemIndex);
                result[i] = array;
            }

            return result;
        }

        private readonly record struct JmcModContext(string? ModId, string? DisplayName);
    }
}
