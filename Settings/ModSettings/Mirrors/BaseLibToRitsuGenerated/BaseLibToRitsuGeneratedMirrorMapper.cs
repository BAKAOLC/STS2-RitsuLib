using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
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
            Type? colorPickerAttrType,
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
                            var mapped = TryMapProperty(modId, property, host, colorPickerAttrType, hoverTipAttrType,
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
                    VisibleWhen: BuildSectionVisibility(entries)));
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
            BaseLibToRitsuGeneratedMirrorHost host, Type? colorPickerAttrType, Type? hoverTipAttrType,
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
                return new(id, ModSettingsMirrorEntryKind.Color, label, colorBinding, description, EditAlpha: true,
                    EditIntensity: false, VisibleWhen: visibilityPredicate);
            }

            var asColor = colorPickerAttrType != null && prop.GetCustomAttribute(colorPickerAttrType, false) != null;
            if (type == typeof(string) && asColor)
                return new(id, ModSettingsMirrorEntryKind.Color, label,
                    CallbackForStaticProperty<string>(modId, dataKey, prop, host), description, EditAlpha: true,
                    EditIntensity: false, VisibleWhen: visibilityPredicate);

            if (type == typeof(string))
                return new(id, ModSettingsMirrorEntryKind.String, label,
                    CallbackForStaticProperty<string>(modId, dataKey, prop, host), description,
                    VisibleWhen: visibilityPredicate);

            if (type == typeof(int))
            {
                var intBinding = ModSettingsBindings.Callback(modId, dataKey,
                    () => (int)prop.GetValue(null)!,
                    value =>
                    {
                        prop.SetValue(null, value);
                        host.NotifyChanged();
                    },
                    host.Save);
                return new(id, ModSettingsMirrorEntryKind.IntSlider, label, intBinding, description,
                    new(0d, 100d, 1d), VisibleWhen: visibilityPredicate);
            }

            if (type == typeof(float))
                return new(id, ModSettingsMirrorEntryKind.Slider, label,
                    CallbackForStaticProperty<float>(modId, dataKey, prop, host), description,
                    new(0d, 100d, 1d, null, value => value.ToString("0.##")), VisibleWhen: visibilityPredicate);

            if (type == typeof(double))
                return new(id, ModSettingsMirrorEntryKind.Slider, label,
                    CallbackForStaticProperty<double>(modId, dataKey, prop, host), description,
                    new(0d, 100d, 1d, value => value.ToString("0.##")), VisibleWhen: visibilityPredicate);

            if (!type.IsEnum)
                return null;

            var enumBinding = typeof(BaseLibToRitsuGeneratedMirrorMapper)
                .GetMethod(nameof(CallbackForStaticProperty), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type)
                .Invoke(null, [modId, dataKey, prop, host]);
            return new(id, ModSettingsMirrorEntryKind.EnumChoice, label, enumBinding, description,
                ChoicePresentation: ModSettingsChoicePresentation.Stepper, EnumType: type,
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

        private static Func<bool>? BuildSectionVisibility(IReadOnlyList<ModSettingsMirrorEntryDefinition> entries)
        {
            if (entries.Count == 0)
                return null;

            return () => entries.Any(static entry => EvaluateVisibility(entry.VisibleWhen));
        }

        private static bool EvaluateVisibility(Func<bool>? predicate)
        {
            if (predicate == null)
                return true;

            try
            {
                return predicate();
            }
            catch
            {
                return true;
            }
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

            var key = prefix + StringHelper.Slugify(member.Name) + ".hover.desc";
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
