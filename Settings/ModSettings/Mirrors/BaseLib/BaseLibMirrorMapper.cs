using System.Reflection;
using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.Settings
{
    internal static class BaseLibMirrorMapper
    {
        public static ModSettingsMirrorPageDefinition? TryCreatePage(
            string modId,
            string pageId,
            int sortOrder,
            ModSettingsText pageTitle,
            ModSettingsText pageDescription,
            BaseLibMirrorHost host,
            List<PropertyInfo> configProps,
            Type? sectionAttrType,
            Type? hideUiAttrType,
            Type? buttonAttrType,
            Type? configSliderType,
            Type? sliderRangeType,
            Type? sliderFormatType,
            Type? textInputAttrType,
            Type? colorPickerAttrType,
            Type? dropdownOverrideAttrType,
            Type? hoverTipAttrType,
            Type? configHoverTipsByDefaultAttrType,
            Type? hoverTipsByDefaultAttrType,
            Type? visibleIfAttrType,
            Type configConcreteType,
            Type modConfigType)
        {
            var members = configConcreteType
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                .Where(member => IsVisibleMember(member, configProps, hideUiAttrType, buttonAttrType))
                .OrderBy(GetSourceOrder)
                .ToList();
            if (members.Count == 0)
                return null;

            var sections = BuildSections(members, sectionAttrType);
            if (sections.Count == 0)
                return null;

            var mappedSections = new List<ModSettingsMirrorSectionDefinition>();
            foreach (var sourceSection in sections)
            {
                var entries = new List<ModSettingsMirrorEntryDefinition>();
                foreach (var member in sourceSection.Entries)
                    switch (member)
                    {
                        case PropertyInfo property:
                        {
                            var mapped = TryMapProperty(modId, property, host, configSliderType, sliderRangeType,
                                sliderFormatType, textInputAttrType, colorPickerAttrType, dropdownOverrideAttrType,
                                hoverTipAttrType,
                                configHoverTipsByDefaultAttrType, hoverTipsByDefaultAttrType, visibleIfAttrType,
                                configConcreteType, modConfigType);
                            if (mapped != null)
                                entries.Add(mapped);
                            break;
                        }
                        case MethodInfo method:
                        {
                            var mapped = TryMapButton(method, host, buttonAttrType, hoverTipAttrType,
                                configHoverTipsByDefaultAttrType, hoverTipsByDefaultAttrType, visibleIfAttrType,
                                configConcreteType, modConfigType);
                            if (mapped != null)
                                entries.Add(mapped);
                            break;
                        }
                    }

                if (entries.Count == 0)
                    continue;

                var section = sourceSection;
                mappedSections.Add(new(sourceSection.Id, entries,
                    string.IsNullOrWhiteSpace(sourceSection.SectionTitle)
                        ? null
                        : ModSettingsText.Dynamic(() => host.ResolveLabel(section.SectionTitle!)),
                    IsCollapsible: !string.IsNullOrWhiteSpace(sourceSection.SectionTitle),
                    StartCollapsed: false,
                    VisibleWhen: ModSettingsMirrorVisibilityPolicy.BuildSectionVisibility(entries)));
            }

            if (mappedSections.Count == 0)
                return null;

            var restoreLabel = host.ResolveBaseLibLabel("RestoreDefaultsButton");
            var restoreButton = new ModSettingsMirrorButtonDefinition(
                "baselib_restore_defaults",
                ModSettingsText.Literal(restoreLabel),
                ModSettingsText.Literal(restoreLabel),
                () => ConfirmAndRestoreDefaults(host),
                ModSettingsButtonTone.Danger);

            return new(modId, pageId, sortOrder, mappedSections, pageTitle, pageDescription,
                host.ResolveModDisplayNameText(modId), null, null, restoreButton);
        }

        private static List<PendingSection> BuildSections(List<MemberInfo> members, Type? sectionAttrType)
        {
            var sections = new List<PendingSection>();
            PendingSection current = new("main", null, []);
            string? currentSectionName = null;
            foreach (var member in members)
            {
                if (sectionAttrType != null)
                {
                    var sectionAttribute = member.GetCustomAttribute(sectionAttrType, false);
                    if (sectionAttribute != null)
                    {
                        var sectionName = sectionAttrType.GetProperty("Name")?.GetValue(sectionAttribute) as string;
                        if (!string.IsNullOrWhiteSpace(sectionName) && sectionName != currentSectionName)
                        {
                            if (current.Entries.Count > 0)
                                sections.Add(current);
                            currentSectionName = sectionName;
                            current = new(ModSettingsMirrorIds.Section(sectionName, sections.Count), sectionName, []);
                        }
                    }
                }

                current.Entries.Add(member);
            }

            if (current.Entries.Count > 0)
                sections.Add(current);
            return sections;
        }

        private static ModSettingsMirrorEntryDefinition? TryMapProperty(
            string modId,
            PropertyInfo prop,
            BaseLibMirrorHost host,
            Type? configSliderType,
            Type? sliderRangeType,
            Type? sliderFormatType,
            Type? textInputAttrType,
            Type? colorPickerAttrType,
            Type? dropdownOverrideAttrType,
            Type? hoverTipAttrType,
            Type? configHoverTipsByDefaultAttrType,
            Type? hoverTipsByDefaultAttrType,
            Type? visibleIfAttrType,
            Type configConcreteType,
            Type modConfigType)
        {
            var id = ModSettingsMirrorIds.Entry("bl", prop.Name);
            var label = ModSettingsText.Dynamic(() => host.ResolveLabel(prop.Name));
            var description = TryBaseLibHoverTipDescription(prop, configConcreteType, host, hoverTipAttrType,
                configHoverTipsByDefaultAttrType, hoverTipsByDefaultAttrType);
            var dataKey = $"baselib::{prop.Name}";
            var propertyType = prop.PropertyType;
            var visibilityPredicate = BaseLibVisibleIfPredicateFactory.TryCreate(prop, host.Instance,
                configConcreteType,
                modConfigType, visibleIfAttrType);

            if (propertyType == typeof(bool))
                return new(id, ModSettingsMirrorEntryKind.Toggle, label,
                    CallbackForStaticProperty<bool>(modId, dataKey, prop, host), description,
                    VisibleWhen: visibilityPredicate);

            if (propertyType == typeof(Color))
            {
                var (editAlpha, editIntensity) =
                    ResolveConfigColorPickerUiOptions(prop, colorPickerAttrType, typeof(Color));
                var colorBinding = ModSettingsBindings.Callback(modId, dataKey,
                    () => ModSettingsColorControl.FormatStoredColorString((Color)prop.GetValue(null)!),
                    value =>
                    {
                        if (string.IsNullOrWhiteSpace(value) ||
                            !ModSettingsColorControl.TryDeserializeColorForSettings(value, out var color))
                            return;
                        prop.SetValue(null, color);
                        host.NotifyChanged();
                    },
                    host.Save);
                return new(id, ModSettingsMirrorEntryKind.Color, label, colorBinding, description, EditAlpha: editAlpha,
                    EditIntensity: editIntensity, VisibleWhen: visibilityPredicate);
            }

            var hasColorPickerAttribute = colorPickerAttrType != null &&
                                          prop.GetCustomAttribute(colorPickerAttrType, false) != null;
            if (propertyType == typeof(string) && hasColorPickerAttribute)
            {
                var (editAlpha, _) = ResolveConfigColorPickerUiOptions(prop, colorPickerAttrType, typeof(string));
                return new(id, ModSettingsMirrorEntryKind.Color, label,
                    CallbackForStaticProperty<string>(modId, dataKey, prop, host), description, EditAlpha: editAlpha,
                    EditIntensity: false, VisibleWhen: visibilityPredicate);
            }

            if (propertyType == typeof(string))
            {
                var (maxLength, placeholder, validator) = ResolveTextInputOptions(prop, textInputAttrType, host);

                return new(id, ModSettingsMirrorEntryKind.String, label,
                    CallbackForStaticProperty<string>(modId, dataKey, prop, host), description,
                    Placeholder: placeholder, MaxLength: maxLength, ValidationVisual: validator,
                    ValidationCommit: validator,
                    VisibleWhen: visibilityPredicate);
            }

            ReadSliderRange(prop, configSliderType, sliderRangeType, out var min, out var max, out var step);
            var sliderFormat = ResolveSliderDisplayFormat(prop, host, configSliderType, sliderFormatType);
            var sliderFormatDouble = TryGetSliderLabelFormatterDouble(sliderFormat);

            if (propertyType == typeof(int))
            {
                var minInt = Mathf.RoundToInt(min);
                var maxInt = Mathf.RoundToInt(max);
                var stepInt = Mathf.Max(1, Mathf.RoundToInt(step));
                if (maxInt < minInt)
                    (minInt, maxInt) = (maxInt, minInt);
                var intBinding = ModSettingsBindings.Callback(modId, dataKey,
                    () => (int)prop.GetValue(null)!,
                    value =>
                    {
                        var intValue = Mathf.Clamp(value, minInt, maxInt);
                        intValue = minInt + (intValue - minInt) / stepInt * stepInt;
                        prop.SetValue(null, intValue);
                        host.NotifyChanged();
                    },
                    host.Save);
                return new(id, ModSettingsMirrorEntryKind.IntSlider, label, intBinding, description,
                    new(minInt, maxInt, stepInt), VisibleWhen: visibilityPredicate);
            }

            if (propertyType == typeof(float))
            {
                var floatMin = (float)min;
                var floatMax = (float)max;
                var floatStep = step <= 0d ? 1f : (float)step;
                if (floatMax < floatMin)
                    (floatMin, floatMax) = (floatMax, floatMin);
                Func<float, string>? formatFloat = string.IsNullOrEmpty(sliderFormat)
                    ? null
                    : value => string.Format(sliderFormat, value);
                return new(id, ModSettingsMirrorEntryKind.Slider, label,
                    CallbackForStaticProperty<float>(modId, dataKey, prop, host), description,
                    new(floatMin, floatMax, floatStep, null, formatFloat), VisibleWhen: visibilityPredicate);
            }

            if (propertyType == typeof(double))
            {
                var doubleStep = step <= 0d ? 1d : step;
                if (max < min)
                    (min, max) = (max, min);
                return new(id, ModSettingsMirrorEntryKind.Slider, label,
                    CallbackForStaticProperty<double>(modId, dataKey, prop, host), description,
                    new(min, max, doubleStep, sliderFormatDouble), VisibleWhen: visibilityPredicate);
            }

            if (!propertyType.IsEnum)
                return null;

            var binding = typeof(BaseLibMirrorMapper)
                .GetMethod(nameof(CallbackForStaticProperty), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(propertyType)
                .Invoke(null, [modId, dataKey, prop, host]);
            return new(id, ModSettingsMirrorEntryKind.EnumChoice, label, binding, description,
                ChoicePresentation: ModSettingsChoicePresentation.Stepper, EnumType: propertyType,
                EnumOptionLabel: value => ModSettingsText.Dynamic(() =>
                    ResolveEnumOptionLabel(prop, host, dropdownOverrideAttrType, value)),
                VisibleWhen: visibilityPredicate);
        }

        private static ModSettingsMirrorEntryDefinition? TryMapButton(
            MethodInfo method,
            BaseLibMirrorHost host,
            Type? buttonAttrType,
            Type? hoverTipAttrType,
            Type? configHoverTipsByDefaultAttrType,
            Type? hoverTipsByDefaultAttrType,
            Type? visibleIfAttrType,
            Type configConcreteType,
            Type modConfigType)
        {
            if (buttonAttrType == null)
                return null;

            var attribute = method.GetCustomAttribute(buttonAttrType, false);
            if (attribute == null)
                return null;

            var visibilityPredicate = BaseLibVisibleIfPredicateFactory.TryCreate(method, host.Instance,
                configConcreteType,
                modConfigType, visibleIfAttrType);
            var key = buttonAttrType.GetProperty("ButtonLabelKey")?.GetValue(attribute) as string ?? method.Name;
            return new(
                ModSettingsMirrorIds.Button("bl", method.Name),
                ModSettingsMirrorEntryKind.Button,
                ModSettingsText.Dynamic(() => host.ResolveLabel(method.Name)),
                Description: TryBaseLibHoverTipDescription(method, configConcreteType, host, hoverTipAttrType,
                    configHoverTipsByDefaultAttrType, hoverTipsByDefaultAttrType),
                ButtonLabel: ModSettingsText.Dynamic(() => host.ResolveLabel(key)),
                OnClick: () => InvokeConfigButton(method, host),
                VisibleWhen: visibilityPredicate);
        }

        private static void InvokeConfigButton(MethodInfo method, BaseLibMirrorHost host)
        {
            try
            {
                var modConfigType = BaseLibMirrorSource.ResolveType(BaseLibMirrorSource.ModConfigTypeName);
                var buttonType = BaseLibMirrorSource.ResolveType("BaseLib.Config.UI.NConfigButton");
                var optionRowType = BaseLibMirrorSource.ResolveType("BaseLib.Config.UI.NConfigOptionRow");
                var buttonContext = CreateButtonContext(method, host, buttonType, optionRowType);
                var args = method.GetParameters();
                var values = new object?[args.Length];
                for (var i = 0; i < args.Length; i++)
                {
                    var parameterType = args[i].ParameterType;
                    if (modConfigType != null && modConfigType.IsAssignableFrom(parameterType))
                    {
                        values[i] = host.Instance;
                        continue;
                    }

                    if (buttonType != null && parameterType == buttonType)
                    {
                        values[i] = buttonContext.Button;
                        continue;
                    }

                    if (optionRowType != null && parameterType == optionRowType)
                    {
                        values[i] = buttonContext.OptionRow;
                        continue;
                    }

                    values[i] = parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
                }

                method.Invoke(method.IsStatic ? null : host.Instance, values);
                host.NotifyChanged();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[BaseLibMirrorSource] ConfigButton '{method.Name}' failed: {ex.Message}");
            }
        }

        private static (object? Button, object? OptionRow) CreateButtonContext(MethodInfo method,
            BaseLibMirrorHost host,
            Type? buttonType, Type? optionRowType)
        {
            if (buttonType == null && optionRowType == null)
                return (null, null);

            try
            {
                var button = buttonType != null ? Activator.CreateInstance(buttonType) : null;
                if (button != null)
                    buttonType!.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public,
                            null, [typeof(string), typeof(Action)], null)
                        ?.Invoke(button, [host.ResolveLabel(method.Name), () => { }]);

                object? row = null;
                if (optionRowType == null || button is not Control buttonControl) return (button, row);
                var label = CreateButtonContextLabel(host, method);
                row = optionRowType.GetConstructor([
                        typeof(string), typeof(string), typeof(Control),
                        typeof(Control),
                    ])
                    ?.Invoke([host.GetModPrefix(), method.Name, label, buttonControl]);

                return (button, row);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Debug(
                    $"[BaseLibMirrorSource] Unable to create ConfigButton context for '{method.Name}': {ex.Message}");
                return (null, null);
            }
        }

        private static Control CreateButtonContextLabel(BaseLibMirrorHost host, MethodInfo method)
        {
            var modConfigType = BaseLibMirrorSource.ResolveType(BaseLibMirrorSource.ModConfigTypeName);
            var labelFactory = modConfigType?.GetMethod("CreateRawLabelControl",
                BindingFlags.Static | BindingFlags.Public,
                null, [typeof(string), typeof(int)], null);
            if (labelFactory?.Invoke(null, [host.ResolveLabel(method.Name), 28]) is Control label)
                return label;

            return new Label { Text = host.ResolveLabel(method.Name) };
        }

        private static ModSettingsCallbackValueBinding<T> CallbackForStaticProperty<T>(
            string modId,
            string dataKey,
            PropertyInfo prop,
            BaseLibMirrorHost host)
        {
            return ModSettingsBindings.Callback(modId, dataKey,
                () => (T)prop.GetValue(null)!,
                value =>
                {
                    prop.SetValue(null, value);
                    host.NotifyChanged();
                },
                host.Save);
        }

        private static void ConfirmAndRestoreDefaults(BaseLibMirrorHost host)
        {
            var body = ModSettingsMirrorUiActions.GetLocalizedOrFallback(
                "BASELIB-RESTORE_MODCONFIG_CONFIRMATION.body",
                "Reset all options for this mod to their default values?");
            var header = ModSettingsMirrorUiActions.GetLocalizedOrFallback(
                "BASELIB-RESTORE_MODCONFIG_CONFIRMATION.header",
                "Restore defaults");
            var cancelText = ModSettingsLocalization.Get("baselib.restoreDefaults.cancel", "Cancel");
            var confirmText = ModSettingsLocalization.Get("baselib.restoreDefaults.confirm", "Restore defaults");
            ModSettingsMirrorUiActions.ConfirmAndRestoreDefaults(
                host.RestoreDefaultsNoConfirm,
                () =>
                {
                    host.NotifyChanged();
                    host.Save();
                },
                header,
                body,
                cancelText,
                confirmText);
        }

        private static void ReadSliderRange(PropertyInfo prop, Type? configSliderType, Type? sliderRangeType,
            out double min, out double max, out double step)
        {
            min = 0;
            max = 100;
            step = 1;
            object? rangeAttribute = null;
            if (configSliderType != null)
                rangeAttribute = prop.GetCustomAttribute(configSliderType, false);
            if (rangeAttribute == null && sliderRangeType != null)
                rangeAttribute = prop.GetCustomAttribute(sliderRangeType, false);
            if (rangeAttribute == null)
                return;

            var type = rangeAttribute.GetType();
            min = Convert.ToDouble(type.GetProperty("Min")?.GetValue(rangeAttribute) ?? 0.0);
            max = Convert.ToDouble(type.GetProperty("Max")?.GetValue(rangeAttribute) ?? 100.0);
            step = Convert.ToDouble(type.GetProperty("Step")?.GetValue(rangeAttribute) ?? 1.0);
        }

        private static string? TryGetSliderFormat(PropertyInfo prop, Type? configSliderType, Type? sliderFormatType)
        {
            if (configSliderType != null && prop.GetCustomAttribute(configSliderType, false) is { } sliderAttribute)
            {
                var format = sliderAttribute.GetType().GetProperty("Format")?.GetValue(sliderAttribute) as string;
                if (!string.IsNullOrEmpty(format))
                    return format;
            }

            if (sliderFormatType == null || prop.GetCustomAttribute(sliderFormatType, false) is not { } formatAttribute)
                return null;

            var legacyFormat = formatAttribute.GetType().GetProperty("Format")?.GetValue(formatAttribute) as string;
            return string.IsNullOrEmpty(legacyFormat) ? null : legacyFormat;
        }

        private static string? ResolveSliderDisplayFormat(PropertyInfo prop, BaseLibMirrorHost host,
            Type? configSliderType, Type? sliderFormatType)
        {
            var fallback = TryGetSliderFormat(prop, configSliderType, sliderFormatType);
            var prefix = host.GetModPrefix();
            if (string.IsNullOrWhiteSpace(prefix))
                return fallback;

            var key = prefix + ModSettingsMirrorSlugPolicy.Normalize(prop.Name) + ".sliderFormat";
            return LocString.GetIfExists("settings_ui", key)?.GetRawText() ?? fallback;
        }

        private static Func<double, string>? TryGetSliderLabelFormatterDouble(string? sliderFormat)
        {
            return string.IsNullOrEmpty(sliderFormat) ? null : value => string.Format(sliderFormat, value);
        }

        private static (bool EditAlpha, bool EditIntensity) ResolveConfigColorPickerUiOptions(
            PropertyInfo prop,
            Type? colorPickerAttrType,
            Type storageType)
        {
            var editAlpha = true;
            var editIntensity = false;
            if (colorPickerAttrType == null || prop.GetCustomAttribute(colorPickerAttrType, false) is not { } attribute)
                return (editAlpha, editIntensity);

            if (colorPickerAttrType.GetProperty("EditAlpha")?.GetValue(attribute) is bool editAlphaValue)
                editAlpha = editAlphaValue;
            if (storageType == typeof(Color) &&
                colorPickerAttrType.GetProperty("EditIntensity")?.GetValue(attribute) is bool editIntensityValue)
                editIntensity = editIntensityValue;
            return (editAlpha, editIntensity);
        }

        private static (int? MaxLength, ModSettingsText? Placeholder, Func<string, bool>? Validator)
            ResolveTextInputOptions(PropertyInfo prop, Type? textInputAttrType, BaseLibMirrorHost host)
        {
            int? maxLength = null;
            Func<string, bool>? validator = null;
            var placeholder = TryResolvePlaceholder(prop, host);

            if (textInputAttrType == null || prop.GetCustomAttribute(textInputAttrType, false) is not { } attribute)
                return (maxLength, placeholder, validator);

            if (textInputAttrType.GetProperty("MaxLength")?.GetValue(attribute) is int maxLengthValue and > 0)
                maxLength = maxLengthValue;

            var pattern = textInputAttrType.GetProperty("AllowedCharactersRegex")?.GetValue(attribute) as string;
            if (string.IsNullOrEmpty(pattern) || pattern == ".*")
                return (maxLength, placeholder, validator);

            try
            {
                var regex = new Regex($"^(?:{pattern})$", RegexOptions.Compiled);
                validator = regex.IsMatch;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[BaseLibMirrorSource] Invalid ConfigTextInput regex for '{prop.Name}': {ex.Message}");
            }

            return (maxLength, placeholder, validator);
        }

        private static ModSettingsText? TryResolvePlaceholder(PropertyInfo prop, BaseLibMirrorHost host)
        {
            var prefix = host.GetModPrefix();
            if (string.IsNullOrWhiteSpace(prefix))
                return null;

            var key = prefix + ModSettingsMirrorSlugPolicy.Normalize(prop.Name) + ".placeholder";
            return LocString.Exists("settings_ui", key)
                ? ModSettingsText.Dynamic(() => LocString.GetIfExists("settings_ui", key)?.GetFormattedText() ?? "")
                : null;
        }

        private static string ResolveEnumOptionLabel(PropertyInfo prop, BaseLibMirrorHost host,
            Type? dropdownOverrideAttrType, object value)
        {
            var fallback = value.ToString() ?? "UNKNOWN";
            var prefix = host.GetModPrefix();
            if (string.IsNullOrWhiteSpace(prefix))
                return fallback;

            var propertyName = ModSettingsMirrorSlugPolicy.Normalize(prop.Name);
            if (dropdownOverrideAttrType != null &&
                prop.GetCustomAttribute(dropdownOverrideAttrType, false) is { } overrideAttribute &&
                dropdownOverrideAttrType.GetProperty("OverridePropertyName")?.GetValue(overrideAttribute) is string
                    overridePropertyName &&
                !string.IsNullOrWhiteSpace(overridePropertyName))
                propertyName = overridePropertyName;

            var key = $"{prefix}{propertyName}.{fallback}";
            return LocString.GetIfExists("settings_ui", key)?.GetRawText() ?? fallback;
        }

        private static ModSettingsText? TryBaseLibHoverTipDescription(MemberInfo member, Type configConcreteType,
            BaseLibMirrorHost host, Type? configHoverTipAttrType, Type? configHoverTipsByDefaultAttrType,
            Type? hoverTipsByDefaultAttrType)
        {
            if (!ShouldShowBaseLibHoverTip(member, configConcreteType, configHoverTipAttrType,
                    configHoverTipsByDefaultAttrType, hoverTipsByDefaultAttrType))
                return null;

            var modPrefix = host.GetModPrefix();
            if (string.IsNullOrEmpty(modPrefix))
                return null;

            var descriptionKey = modPrefix + ModSettingsMirrorSlugPolicy.Normalize(member.Name) + ".hover.desc";
            if (!LocString.Exists("settings_ui", descriptionKey))
                return null;

            return ModSettingsText.Dynamic(() =>
                LocString.GetIfExists("settings_ui", descriptionKey)?.GetFormattedText() ?? "");
        }

        private static bool ShouldShowBaseLibHoverTip(MemberInfo member, Type configConcreteType,
            Type? configHoverTipAttrType, Type? configHoverTipsByDefaultAttrType, Type? hoverTipsByDefaultAttrType)
        {
            object? hoverAttribute = null;
            if (configHoverTipAttrType != null)
                hoverAttribute = member.GetCustomAttribute(configHoverTipAttrType, false);

            bool? explicitEnabled = null;
            if (hoverAttribute != null && configHoverTipAttrType != null)
            {
                var enabledProperty = configHoverTipAttrType.GetProperty("Enabled");
                if (enabledProperty?.GetValue(hoverAttribute) is bool enabled)
                    explicitEnabled = enabled;
            }

            var hoverTipsByDefault = (configHoverTipsByDefaultAttrType != null &&
                                      configConcreteType.GetCustomAttribute(configHoverTipsByDefaultAttrType, false) !=
                                      null) ||
                                     (hoverTipsByDefaultAttrType != null &&
                                      configConcreteType.GetCustomAttribute(hoverTipsByDefaultAttrType, false) != null);
            return explicitEnabled ?? hoverTipsByDefault;
        }

        private static bool IsVisibleMember(MemberInfo member, List<PropertyInfo> configProps, Type? hideUiAttrType,
            Type? buttonAttrType)
        {
            return member switch
            {
                PropertyInfo property => configProps.Contains(property) &&
                                         (hideUiAttrType == null ||
                                          property.GetCustomAttribute(hideUiAttrType) == null),
                MethodInfo method => buttonAttrType != null && method.GetCustomAttribute(buttonAttrType) != null,
                _ => false,
            };
        }

        private static int GetSourceOrder(MemberInfo member)
        {
            return member switch
            {
                MethodInfo method => method.MetadataToken,
                PropertyInfo property => property.GetMethod?.MetadataToken ?? property.SetMethod?.MetadataToken ?? 0,
                _ => 0,
            };
        }

        private sealed record PendingSection(string Id, string? SectionTitle, List<MemberInfo> Entries);
    }
}
