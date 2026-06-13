using System.Reflection;
using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.Settings
{
    internal static class BaseLibToRitsuGeneratedMirrorMapper
    {
        public static ModSettingsMirrorPageDefinition? TryCreatePage(
            string modId,
            string pageId,
            int sortOrder,
            ModSettingsText pageTitle,
            ModSettingsText pageDescription,
            BaseLibToRitsuGeneratedMirrorHost host,
            IReadOnlySet<string> propertyNames,
            Type? sectionAttrType,
            Type? hideUiAttrType,
            Type? buttonAttrType,
            Type? sliderAttrType,
            Type? sliderRangeAttrType,
            Type? sliderFormatAttrType,
            Type? textInputAttrType,
            Type? colorPickerAttrType,
            Type? dropdownOverrideAttrType,
            Type? hoverTipAttrType,
            Type? hoverTipsByDefaultAttrType,
            Type? legacyHoverTipsByDefaultAttrType,
            Type? visibleIfAttrType,
            Type configType,
            Type modConfigType)
        {
            var members = configType
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                .Where(member => IsVisibleMember(member, propertyNames, hideUiAttrType, buttonAttrType))
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
                            var mapped = TryMapProperty(modId, property, host, sliderAttrType, sliderRangeAttrType,
                                sliderFormatAttrType, textInputAttrType, colorPickerAttrType, dropdownOverrideAttrType,
                                hoverTipAttrType,
                                hoverTipsByDefaultAttrType, legacyHoverTipsByDefaultAttrType, visibleIfAttrType,
                                configType, modConfigType);
                            if (mapped != null)
                                entries.Add(mapped);
                            break;
                        }
                        case MethodInfo method:
                        {
                            var mapped = TryMapButton(method, host, buttonAttrType, hoverTipAttrType,
                                hoverTipsByDefaultAttrType, legacyHoverTipsByDefaultAttrType, visibleIfAttrType,
                                configType, modConfigType);
                            if (mapped != null)
                                entries.Add(mapped);
                            break;
                        }
                    }

                if (entries.Count == 0)
                    continue;

                mappedSections.Add(new(sourceSection.Id, entries,
                    string.IsNullOrWhiteSpace(sourceSection.Title)
                        ? null
                        : ModSettingsText.Dynamic(() => host.ResolveLabel(sourceSection.Title!)),
                    IsCollapsible: !string.IsNullOrWhiteSpace(sourceSection.Title),
                    StartCollapsed: false,
                    VisibleWhen: ModSettingsMirrorVisibilityPolicy.BuildSectionVisibility(entries)));
            }

            if (mappedSections.Count == 0)
                return null;

            var restoreLabel = host.ResolveBaseLibLabel("RestoreDefaultsButton");
            var restoreButton = new ModSettingsMirrorButtonDefinition(
                "baselib_generated_restore_defaults",
                ModSettingsText.Literal(restoreLabel),
                ModSettingsText.Literal(restoreLabel),
                () => ConfirmAndRestoreDefaults(host),
                ModSettingsButtonTone.Danger);

            return new(modId, pageId, sortOrder, mappedSections, pageTitle, pageDescription,
                host.ResolveModDisplayNameText(modId), null, null, restoreButton);
        }

        private static List<PendingSection> BuildSections(List<MemberInfo> members, Type? sectionAttrType)
        {
            var result = new List<PendingSection>();
            PendingSection current = new("main", null, []);
            string? currentTitle = null;
            foreach (var member in members)
            {
                if (sectionAttrType != null && member.GetCustomAttribute(sectionAttrType, false) is { } attribute)
                {
                    var title = sectionAttrType.GetProperty("Name")?.GetValue(attribute) as string;
                    if (!string.IsNullOrWhiteSpace(title) && title != currentTitle)
                    {
                        if (current.Entries.Count > 0)
                            result.Add(current);
                        currentTitle = title;
                        current = new(ModSettingsMirrorIds.Section(title, result.Count), title, []);
                    }
                }

                current.Entries.Add(member);
            }

            if (current.Entries.Count > 0)
                result.Add(current);
            return result;
        }

        private static ModSettingsMirrorEntryDefinition? TryMapProperty(string modId, PropertyInfo prop,
            BaseLibToRitsuGeneratedMirrorHost host, Type? sliderAttrType, Type? sliderRangeAttrType,
            Type? sliderFormatAttrType, Type? textInputAttrType, Type? colorPickerAttrType,
            Type? dropdownOverrideAttrType, Type? hoverTipAttrType,
            Type? hoverTipsByDefaultAttrType, Type? legacyHoverTipsByDefaultAttrType, Type? visibleIfAttrType,
            Type configType, Type modConfigType)
        {
            var id = ModSettingsMirrorIds.Entry("blg", prop.Name);
            var label = ModSettingsText.Dynamic(() => host.ResolveLabel(prop.Name));
            var description = TryHoverTip(prop, configType, host, hoverTipAttrType, hoverTipsByDefaultAttrType,
                legacyHoverTipsByDefaultAttrType);
            var dataKey = $"baselib-generated::{prop.Name}";
            var type = prop.PropertyType;
            var visibilityPredicate = BaseLibVisibleIfPredicateFactory.TryCreate(prop, host.Instance, configType,
                modConfigType, visibleIfAttrType);

            if (type == typeof(bool))
                return new(id, ModSettingsMirrorEntryKind.Toggle, label,
                    CallbackForStaticProperty<bool>(modId, dataKey, prop, host), description,
                    VisibleWhen: visibilityPredicate);

            if (type == typeof(Color))
            {
                var (editAlpha, editIntensity) =
                    ResolveColorPickerUiOptions(prop, colorPickerAttrType, typeof(Color));
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

            var asColor = colorPickerAttrType != null && prop.GetCustomAttribute(colorPickerAttrType, false) != null;
            if (type == typeof(string) && asColor)
            {
                var (editAlpha, _) = ResolveColorPickerUiOptions(prop, colorPickerAttrType, typeof(string));
                return new(id, ModSettingsMirrorEntryKind.Color, label,
                    CallbackForStaticProperty<string>(modId, dataKey, prop, host), description, EditAlpha: editAlpha,
                    EditIntensity: false, VisibleWhen: visibilityPredicate);
            }

            if (type == typeof(string))
            {
                var (maxLength, placeholder, validator) = ResolveTextInputOptions(prop, textInputAttrType, host);
                return new(id, ModSettingsMirrorEntryKind.String, label,
                    CallbackForStaticProperty<string>(modId, dataKey, prop, host), description,
                    Placeholder: placeholder, MaxLength: maxLength, ValidationVisual: validator,
                    ValidationCommit: validator,
                    VisibleWhen: visibilityPredicate);
            }

            ReadSliderRange(prop, sliderAttrType, sliderRangeAttrType, out var min, out var max, out var step);
            var sliderFormat = ResolveSliderDisplayFormat(prop, host, sliderAttrType, sliderFormatAttrType);

            if (type == typeof(int))
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

            if (type == typeof(float))
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

            if (type == typeof(double))
            {
                var doubleStep = step <= 0d ? 1d : step;
                if (max < min)
                    (min, max) = (max, min);
                return new(id, ModSettingsMirrorEntryKind.Slider, label,
                    CallbackForStaticProperty<double>(modId, dataKey, prop, host), description,
                    new(min, max, doubleStep, TryGetSliderLabelFormatterDouble(sliderFormat)),
                    VisibleWhen: visibilityPredicate);
            }

            if (!type.IsEnum)
                return null;

            var enumBinding = typeof(BaseLibToRitsuGeneratedMirrorMapper)
                .GetMethod(nameof(CallbackForStaticProperty), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type)
                .Invoke(null, [modId, dataKey, prop, host]);
            return new(id, ModSettingsMirrorEntryKind.EnumChoice, label, enumBinding, description,
                ChoicePresentation: ModSettingsChoicePresentation.Stepper, EnumType: type,
                EnumOptionLabel: value => ModSettingsText.Dynamic(() =>
                    ResolveEnumOptionLabel(prop, host, dropdownOverrideAttrType, value)),
                VisibleWhen: visibilityPredicate);
        }

        private static ModSettingsMirrorEntryDefinition? TryMapButton(MethodInfo method,
            BaseLibToRitsuGeneratedMirrorHost host, Type? buttonAttrType, Type? hoverTipAttrType,
            Type? hoverTipsByDefaultAttrType, Type? legacyHoverTipsByDefaultAttrType, Type? visibleIfAttrType,
            Type configType, Type modConfigType)
        {
            if (buttonAttrType == null || method.GetCustomAttribute(buttonAttrType, false) is not { } attribute)
                return null;

            var visibilityPredicate = BaseLibVisibleIfPredicateFactory.TryCreate(method, host.Instance, configType,
                modConfigType, visibleIfAttrType);
            var key = buttonAttrType.GetProperty("ButtonLabelKey")?.GetValue(attribute) as string ?? method.Name;
            return new(
                ModSettingsMirrorIds.Button("blg", method.Name),
                ModSettingsMirrorEntryKind.Button,
                ModSettingsText.Dynamic(() => host.ResolveLabel(method.Name)),
                Description: TryHoverTip(method, configType, host, hoverTipAttrType, hoverTipsByDefaultAttrType,
                    legacyHoverTipsByDefaultAttrType),
                ButtonLabel: ModSettingsText.Dynamic(() => host.ResolveLabel(key)),
                OnClick: () => InvokeConfigButton(method, host),
                VisibleWhen: visibilityPredicate);
        }

        private static void InvokeConfigButton(MethodInfo method, BaseLibToRitsuGeneratedMirrorHost host)
        {
            try
            {
                var parameters = method.GetParameters();
                var values = new object?[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                    values[i] = parameters[i].ParameterType.IsInstanceOfType(host.Instance)
                        ? host.Instance
                        : parameters[i].ParameterType.IsValueType
                            ? Activator.CreateInstance(parameters[i].ParameterType)
                            : null;

                method.Invoke(method.IsStatic ? null : host.Instance, values);
                host.NotifyChanged();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[BaseLibToRitsuGeneratedMirrorSource] ConfigButton '{method.Name}' failed: {ex.Message}");
            }
        }

        private static ModSettingsCallbackValueBinding<T> CallbackForStaticProperty<T>(string modId, string dataKey,
            PropertyInfo prop, BaseLibToRitsuGeneratedMirrorHost host)
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

        private static void ReadSliderRange(PropertyInfo prop, Type? sliderAttrType, Type? sliderRangeAttrType,
            out double min, out double max, out double step)
        {
            min = 0;
            max = 100;
            step = 1;
            object? rangeAttribute = null;
            if (sliderAttrType != null)
                rangeAttribute = prop.GetCustomAttribute(sliderAttrType, false);
            if (rangeAttribute == null && sliderRangeAttrType != null)
                rangeAttribute = prop.GetCustomAttribute(sliderRangeAttrType, false);
            if (rangeAttribute == null)
                return;

            var type = rangeAttribute.GetType();
            min = Convert.ToDouble(type.GetProperty("Min")?.GetValue(rangeAttribute) ?? 0.0);
            max = Convert.ToDouble(type.GetProperty("Max")?.GetValue(rangeAttribute) ?? 100.0);
            step = Convert.ToDouble(type.GetProperty("Step")?.GetValue(rangeAttribute) ?? 1.0);
        }

        private static string? ResolveSliderDisplayFormat(PropertyInfo prop, BaseLibToRitsuGeneratedMirrorHost host,
            Type? sliderAttrType, Type? sliderFormatAttrType)
        {
            var fallback = TryGetSliderFormat(prop, sliderAttrType, sliderFormatAttrType);
            var prefix = host.ModPrefix;
            if (string.IsNullOrWhiteSpace(prefix))
                return fallback;

            var key = prefix + ModSettingsMirrorSlugPolicy.Normalize(prop.Name) + ".sliderFormat";
            return LocString.GetIfExists("settings_ui", key)?.GetRawText() ?? fallback;
        }

        private static string? TryGetSliderFormat(PropertyInfo prop, Type? sliderAttrType, Type? sliderFormatAttrType)
        {
            if (sliderAttrType != null && prop.GetCustomAttribute(sliderAttrType, false) is { } sliderAttribute)
            {
                var format = sliderAttrType.GetProperty("Format")?.GetValue(sliderAttribute) as string;
                if (!string.IsNullOrEmpty(format))
                    return format;
            }

            if (sliderFormatAttrType == null ||
                prop.GetCustomAttribute(sliderFormatAttrType, false) is not { } formatAttribute)
                return null;

            var legacyFormat = sliderFormatAttrType.GetProperty("Format")?.GetValue(formatAttribute) as string;
            return string.IsNullOrEmpty(legacyFormat) ? null : legacyFormat;
        }

        private static Func<double, string>? TryGetSliderLabelFormatterDouble(string? sliderFormat)
        {
            return string.IsNullOrEmpty(sliderFormat) ? null : value => string.Format(sliderFormat, value);
        }

        private static (bool EditAlpha, bool EditIntensity) ResolveColorPickerUiOptions(PropertyInfo prop,
            Type? colorPickerAttrType, Type storageType)
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
            ResolveTextInputOptions(PropertyInfo prop, Type? textInputAttrType, BaseLibToRitsuGeneratedMirrorHost host)
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
                    $"[BaseLibToRitsuGeneratedMirrorSource] Invalid ConfigTextInput regex for '{prop.Name}': {ex.Message}");
            }

            return (maxLength, placeholder, validator);
        }

        private static ModSettingsText? TryResolvePlaceholder(PropertyInfo prop,
            BaseLibToRitsuGeneratedMirrorHost host)
        {
            var prefix = host.ModPrefix;
            if (string.IsNullOrWhiteSpace(prefix))
                return null;

            var key = prefix + ModSettingsMirrorSlugPolicy.Normalize(prop.Name) + ".placeholder";
            return LocString.Exists("settings_ui", key)
                ? ModSettingsText.Dynamic(() => LocString.GetIfExists("settings_ui", key)?.GetFormattedText() ?? "")
                : null;
        }

        private static string ResolveEnumOptionLabel(PropertyInfo prop, BaseLibToRitsuGeneratedMirrorHost host,
            Type? dropdownOverrideAttrType, object value)
        {
            var fallback = value.ToString() ?? "UNKNOWN";
            var prefix = host.ModPrefix;
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

        private static ModSettingsText? TryHoverTip(MemberInfo member, Type configType,
            BaseLibToRitsuGeneratedMirrorHost host, Type? hoverTipAttrType, Type? hoverTipsByDefaultAttrType,
            Type? legacyHoverTipsByDefaultAttrType)
        {
            if (!ShouldShowHoverTip(member, configType, hoverTipAttrType, hoverTipsByDefaultAttrType,
                    legacyHoverTipsByDefaultAttrType))
                return null;

            var prefix = host.ModPrefix;
            if (string.IsNullOrWhiteSpace(prefix))
                return null;

            var key = prefix + ModSettingsMirrorSlugPolicy.Normalize(member.Name) + ".hover.desc";
            return !LocString.Exists("settings_ui", key)
                ? null
                : ModSettingsText.Dynamic(() => LocString.GetIfExists("settings_ui", key)?.GetFormattedText() ?? "");
        }

        private static bool ShouldShowHoverTip(MemberInfo member, Type configType, Type? hoverTipAttrType,
            Type? hoverTipsByDefaultAttrType, Type? legacyHoverTipsByDefaultAttrType)
        {
            bool? explicitFlag = null;
            if (hoverTipAttrType != null && member.GetCustomAttribute(hoverTipAttrType, false) is { } attribute &&
                hoverTipAttrType.GetProperty("Enabled")?.GetValue(attribute) is bool enabled)
                explicitFlag = enabled;

            var byDefault =
                (hoverTipsByDefaultAttrType != null &&
                 configType.GetCustomAttribute(hoverTipsByDefaultAttrType, false) != null) ||
                (legacyHoverTipsByDefaultAttrType != null &&
                 configType.GetCustomAttribute(legacyHoverTipsByDefaultAttrType, false) != null);
            return explicitFlag ?? byDefault;
        }

        private static bool IsVisibleMember(MemberInfo member, IReadOnlySet<string> propertyNames,
            Type? hideUiAttrType, Type? buttonAttrType)
        {
            return member switch
            {
                PropertyInfo property => propertyNames.Contains(property.Name) &&
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

        private static void ConfirmAndRestoreDefaults(BaseLibToRitsuGeneratedMirrorHost host)
        {
            var body = ModSettingsMirrorUiActions.GetLocalizedOrFallback(
                "BASELIB-RESTORE_MODCONFIG_CONFIRMATION.body",
                "Reset all options for this mod to their default values?");
            var header = ModSettingsMirrorUiActions.GetLocalizedOrFallback(
                "BASELIB-RESTORE_MODCONFIG_CONFIRMATION.header",
                "Restore defaults");
            var cancelText = ModSettingsLocalization.Get("baselib.restoreDefaults.cancel", "Cancel");
            var confirmText = ModSettingsLocalization.Get("baselib.restoreDefaults.confirm", "Restore defaults");
            ModSettingsMirrorUiActions.ConfirmAndRestoreDefaults(host.RestoreDefaultsNoConfirm, () =>
            {
                host.NotifyChanged();
                host.Save();
            }, header, body, cancelText, confirmText);
        }

        private sealed record PendingSection(string Id, string? Title, List<MemberInfo> Entries);
    }
}
