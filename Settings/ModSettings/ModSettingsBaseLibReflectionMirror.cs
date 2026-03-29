using System.Collections;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Mirrors BaseLib <c>ModConfig</c> / <c>SimpleModConfig</c> static properties into RitsuLib&apos;s mod settings
    ///     UI via reflection (no compile-time reference to BaseLib). <see cref="TryRegisterMirroredPages" /> is invoked
    ///     automatically from <see cref="RitsuModSettingsSubmenu" /> on each UI rebuild until pages are registered or
    ///     BaseLib is determined absent; mods may also call it explicitly after registering configs.
    /// </summary>
    public static class ModSettingsBaseLibReflectionMirror
    {
        private const string RegistryTypeName = "BaseLib.Config.ModConfigRegistry";
        private const string ModConfigTypeName = "BaseLib.Config.ModConfig";
        private const string ConfigSectionAttributeName = "BaseLib.Config.ConfigSectionAttribute";
        private const string ConfigHideInUiAttributeName = "BaseLib.Config.ConfigHideInUI";
        private const string ConfigButtonAttributeName = "BaseLib.Config.ConfigButtonAttribute";
        private const string SliderRangeAttributeName = "BaseLib.Config.SliderRangeAttribute";
        private const string SliderLabelFormatAttributeName = "BaseLib.Config.SliderLabelFormatAttribute";
        private const string ConfigTextInputAttributeName = "BaseLib.Config.ConfigTextInputAttribute";
        private const string ConfigHoverTipAttributeName = "BaseLib.Config.ConfigHoverTipAttribute";
        private const string HoverTipsByDefaultAttributeName = "BaseLib.Config.HoverTipsByDefaultAttribute";

        private static readonly Lock Gate = new();
        private static bool _pagesRegistered;

        /// <summary>
        ///     True when an assembly exposes BaseLib&apos;s <c>ModConfigRegistry</c> type.
        /// </summary>
        public static bool IsBaseLibPresent => ResolveType(RegistryTypeName) != null;

        /// <summary>
        ///     Registers one RitsuLib settings page per BaseLib registry entry, approximating <c>SimpleModConfig</c>
        ///     layout (sections, bool / double / string / enum, and <c>[ConfigButton]</c> methods). Returns the number
        ///     of pages registered. Subsequent calls are ignored until the process restarts.
        /// </summary>
        /// <param name="pageId">Stable page id under each mod (default <c>baselib</c>).</param>
        /// <param name="sortOrder">Sidebar ordering for mirrored pages (higher appears after native Ritsu pages).</param>
        /// <param name="pageTitle">
        ///     Optional page title; defaults to a short label for the page that proxies BaseLib ModConfig (not the
        ///     BaseLib framework mod itself).
        /// </param>
        public static int TryRegisterMirroredPages(
            string pageId = "baselib",
            int sortOrder = 10_000,
            ModSettingsText? pageTitle = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pageId);

            lock (Gate)
            {
                if (_pagesRegistered)
                    return 0;

                var registryType = ResolveType(RegistryTypeName);
                var modConfigType = ResolveType(ModConfigTypeName);
                if (registryType == null || modConfigType == null)
                {
                    _pagesRegistered = true;
                    return 0;
                }

                var configsField = registryType.GetField("ModConfigs",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (configsField?.GetValue(null) is not IDictionary rawMap)
                {
                    _pagesRegistered = true;
                    return 0;
                }

                var configPropsField = modConfigType.GetField("ConfigProperties",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var getLabel = modConfigType.GetMethod("GetLabelText",
                    BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(string)], null);
                var changed = modConfigType.GetMethod("Changed", BindingFlags.Instance | BindingFlags.Public);
                var save = modConfigType.GetMethod("Save", BindingFlags.Instance | BindingFlags.Public);
                var restore = modConfigType.GetMethod("RestoreDefaultsNoConfirm",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var baseLibLabel = modConfigType.GetMethod("GetBaseLibLabelText",
                    BindingFlags.Static | BindingFlags.NonPublic, null, [typeof(string)], null);
                if (configPropsField == null || getLabel == null || changed == null || save == null || restore == null)
                {
                    _pagesRegistered = true;
                    return 0;
                }

                var sectionAttrType = ResolveType(ConfigSectionAttributeName);
                var hideUiAttrType = ResolveType(ConfigHideInUiAttributeName);
                var buttonAttrType = ResolveType(ConfigButtonAttributeName);
                var sliderRangeType = ResolveType(SliderRangeAttributeName);
                var sliderFormatType = ResolveType(SliderLabelFormatAttributeName);
                var textInputAttrType = ResolveType(ConfigTextInputAttributeName);
                var hoverTipAttrType = ResolveType(ConfigHoverTipAttributeName);
                var hoverTipsByDefaultAttrType = ResolveType(HoverTipsByDefaultAttributeName);

                pageTitle ??= ModSettingsText.I18N(ModSettingsLocalization.Instance, "baselib.mirroredPage.title",
                    "Mod config");
                var pageDescription = ModSettingsText.I18N(ModSettingsLocalization.Instance,
                    "baselib.mirroredPage.description",
                    "This page is an auto-generated proxy settings page for mods built on BaseLib.");

                var count = 0;
                foreach (DictionaryEntry entry in rawMap)
                {
                    var modId = entry.Key as string;
                    var config = entry.Value;
                    if (string.IsNullOrWhiteSpace(modId) || config == null)
                        continue;

                    if (configPropsField.GetValue(config) is not List<PropertyInfo> configProps)
                        continue;

                    if (configProps.Count == 0)
                        continue;

                    var host = new ConfigHost(config, changed, save, restore, getLabel, baseLibLabel);
                    if (!TryBuildPage(modId, pageId, sortOrder, pageTitle, pageDescription, host, configProps,
                            sectionAttrType,
                            hideUiAttrType, buttonAttrType, sliderRangeType, sliderFormatType, textInputAttrType,
                            hoverTipAttrType,
                            hoverTipsByDefaultAttrType,
                            config.GetType()))
                        continue;

                    count++;
                }

                if (count > 0)
                    _pagesRegistered = true;

                return count;
            }
        }

        private static bool TryBuildPage(
            string modId,
            string pageId,
            int sortOrder,
            ModSettingsText pageTitle,
            ModSettingsText pageDescription,
            ConfigHost host,
            List<PropertyInfo> configProps,
            Type? sectionAttrType,
            Type? hideUiAttrType,
            Type? buttonAttrType,
            Type? sliderRangeType,
            Type? sliderFormatType,
            Type? textInputAttrType,
            Type? hoverTipAttrType,
            Type? hoverTipsByDefaultAttrType,
            Type configConcreteType)
        {
            var members = configConcreteType
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                            BindingFlags.Instance)
                .Where(m => IsVisibleMember(m, configProps, hideUiAttrType, buttonAttrType))
                .OrderBy(GetSourceOrder)
                .ToList();

            if (members.Count == 0)
                return false;

            var sections = new List<PendingSection>();
            PendingSection current = new("main", null, []);
            string? currentSectionName = null;

            foreach (var member in members)
            {
                if (sectionAttrType != null && member is PropertyInfo pi)
                {
                    var sa = pi.GetCustomAttribute(sectionAttrType, false);
                    if (sa != null)
                    {
                        var nameProp = sectionAttrType.GetProperty("Name");
                        var sectionName = nameProp?.GetValue(sa) as string;
                        if (!string.IsNullOrWhiteSpace(sectionName) && sectionName != currentSectionName)
                        {
                            if (current.Entries.Count > 0)
                                sections.Add(current);

                            currentSectionName = sectionName;
                            var sectionId = $"sec_{StringHelper.Slugify(sectionName)}_{sections.Count}";
                            current = new(sectionId, sectionName, []);
                        }
                    }
                }

                current.Entries.Add((member, currentSectionName));
            }

            if (current.Entries.Count > 0)
                sections.Add(current);

            if (sections.Count == 0)
                return false;

            try
            {
                ModSettingsRegistry.Register(modId, builder =>
                {
                    builder
                        .WithTitle(pageTitle)
                        .WithDescription(pageDescription)
                        .WithSortOrder(sortOrder);

                    for (var i = 0; i < sections.Count; i++)
                    {
                        var sec = sections[i];
                        var isLastSection = i == sections.Count - 1;
                        builder.AddSection(sec.Id, section =>
                        {
                            if (!string.IsNullOrWhiteSpace(sec.SectionTitle))
                                section.WithTitle(ModSettingsText.Dynamic(() => host.ResolveLabel(sec.SectionTitle!)));

                            foreach (var (member, _) in sec.Entries)
                                switch (member)
                                {
                                    case PropertyInfo prop:
                                        AppendPropertyEntry(section, modId, prop, host, sliderRangeType,
                                            sliderFormatType, textInputAttrType, hoverTipAttrType,
                                            hoverTipsByDefaultAttrType, configConcreteType);
                                        break;
                                    case MethodInfo method:
                                        AppendButtonEntry(section, modId, method, host, buttonAttrType,
                                            hoverTipAttrType, hoverTipsByDefaultAttrType, configConcreteType);
                                        break;
                                }

                            if (!isLastSection)
                                return;

                            var restoreLabel = host.ResolveBaseLibLabel("RestoreDefaultsButton");
                            section.AddButton(
                                "baselib_restore_defaults",
                                ModSettingsText.Literal(restoreLabel),
                                ModSettingsText.Literal(restoreLabel),
                                () => ConfirmAndRestoreDefaults(host),
                                ModSettingsButtonTone.Danger);
                        });
                    }
                }, pageId);

                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModSettingsBaseLibReflectionMirror] Failed to register page '{modId}::{pageId}': {ex.Message}");
                return false;
            }
        }

        private static void AppendPropertyEntry(
            ModSettingsSectionBuilder section,
            string modId,
            PropertyInfo prop,
            ConfigHost host,
            Type? sliderRangeType,
            Type? sliderFormatType,
            Type? textInputAttrType,
            Type? hoverTipAttrType,
            Type? hoverTipsByDefaultAttrType,
            Type configConcreteType)
        {
            var id = $"bl_{StringHelper.Slugify(prop.Name)}";
            var label = ModSettingsText.Dynamic(() => host.ResolveLabel(prop.Name));
            var dataKey = $"baselib::{prop.Name}";
            var hoverTipDescription = TryBaseLibHoverTipDescription(prop, configConcreteType, host, hoverTipAttrType,
                hoverTipsByDefaultAttrType);

            var pt = prop.PropertyType;
            if (pt == typeof(bool))
            {
                var binding = CallbackForStaticProperty<bool>(modId, dataKey, prop, host);
                section.AddToggle(id, label, binding, hoverTipDescription);
                return;
            }

            if (pt == typeof(double))
            {
                var binding = CallbackForStaticProperty<double>(modId, dataKey, prop, host);
                var min = 0d;
                var max = 1d;
                var step = 0.01d;
                if (sliderRangeType != null &&
                    prop.GetCustomAttribute(sliderRangeType) is { } rangeAttr)
                {
                    min = (double)(rangeAttr.GetType().GetProperty("Min")?.GetValue(rangeAttr) ?? 0.0);
                    max = (double)(rangeAttr.GetType().GetProperty("Max")?.GetValue(rangeAttr) ?? 1.0);
                    step = (double)(rangeAttr.GetType().GetProperty("Step")?.GetValue(rangeAttr) ?? 0.01);
                }

                Func<double, string>? fmt = null;
                if (sliderFormatType != null &&
                    prop.GetCustomAttribute(sliderFormatType) is { } fmtAttr)
                {
                    var format = fmtAttr.GetType().GetProperty("Format")?.GetValue(fmtAttr) as string;
                    if (!string.IsNullOrEmpty(format))
                        fmt = v => string.Format(format, v);
                }

                section.AddSlider(id, label, binding, min, max, step, fmt, hoverTipDescription);
                return;
            }

            if (pt == typeof(string))
            {
                var binding = CallbackForStaticProperty<string>(modId, dataKey, prop, host);
                int? maxLen = null;
                if (textInputAttrType != null &&
                    prop.GetCustomAttribute(textInputAttrType) is { } tiAttr)
                {
                    var ml = tiAttr.GetType().GetProperty("MaxLength")?.GetValue(tiAttr) as int? ?? 0;
                    if (ml > 0)
                        maxLen = ml;
                }

                section.AddString(id, label, binding, maxLength: maxLen, description: hoverTipDescription);
                return;
            }

            if (!pt.IsEnum) return;
            var bindingObj = typeof(ModSettingsBaseLibReflectionMirror)
                .GetMethod(nameof(CallbackForStaticProperty), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(pt)
                .Invoke(null, [modId, dataKey, prop, host]);
            var addEnum = typeof(ModSettingsSectionBuilder).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Single(m =>
                    m is
                    {
                        Name: nameof(ModSettingsSectionBuilder.AddEnumChoice), IsGenericMethodDefinition: true,
                    });
            var concrete = addEnum.MakeGenericMethod(pt);
            concrete.Invoke(section,
            [
                id,
                label,
                bindingObj,
                null,
                hoverTipDescription,
                ModSettingsChoicePresentation.Stepper,
            ]);
        }

        private static ModSettingsText? TryBaseLibHoverTipDescription(MemberInfo member, Type configConcreteType,
            ConfigHost host, Type? configHoverTipAttrType, Type? hoverTipsByDefaultAttrType)
        {
            if (!ShouldShowBaseLibHoverTip(member, configConcreteType, configHoverTipAttrType,
                    hoverTipsByDefaultAttrType))
                return null;

            var modPrefix = host.GetModPrefix();
            if (string.IsNullOrEmpty(modPrefix))
                return null;

            var slug = StringHelper.Slugify(member.Name);
            var descKey = modPrefix + slug + ".hover.desc";
            if (!LocString.Exists("settings_ui", descKey))
                return null;

            var key = descKey;
            return ModSettingsText.Dynamic(() =>
                LocString.GetIfExists("settings_ui", key)?.GetFormattedText() ?? "");
        }

        private static bool ShouldShowBaseLibHoverTip(MemberInfo member, Type configConcreteType,
            Type? configHoverTipAttrType, Type? hoverTipsByDefaultAttrType)
        {
            object? hoverAttr = null;
            if (configHoverTipAttrType != null)
                hoverAttr = member.GetCustomAttribute(configHoverTipAttrType, false);

            bool? explicitEnabled = null;
            if (hoverAttr != null && configHoverTipAttrType != null)
            {
                var enabledProp = configHoverTipAttrType.GetProperty("Enabled");
                if (enabledProp?.GetValue(hoverAttr) is bool b)
                    explicitEnabled = b;
            }

            var hoverTipsByDefault = hoverTipsByDefaultAttrType != null &&
                                     configConcreteType.GetCustomAttribute(hoverTipsByDefaultAttrType, false) != null;

            return explicitEnabled ?? hoverTipsByDefault;
        }

        private static void AppendButtonEntry(
            ModSettingsSectionBuilder section,
            string modId,
            MethodInfo method,
            ConfigHost host,
            Type? buttonAttrType,
            Type? hoverTipAttrType,
            Type? hoverTipsByDefaultAttrType,
            Type configConcreteType)
        {
            if (buttonAttrType == null)
                return;

            var attr = method.GetCustomAttribute(buttonAttrType, false);
            if (attr == null)
                return;

            var key = buttonAttrType.GetProperty("ButtonLabelKey")?.GetValue(attr) as string ?? method.Name;
            var id = $"bl_btn_{StringHelper.Slugify(method.Name)}";
            var rowLabel = ModSettingsText.Dynamic(() => host.ResolveLabel(method.Name));
            var buttonLabel = ModSettingsText.Dynamic(() => host.ResolveLabel(key));
            var hoverTipDescription = TryBaseLibHoverTipDescription(method, configConcreteType, host, hoverTipAttrType,
                hoverTipsByDefaultAttrType);

            section.AddButton(id, rowLabel, buttonLabel,
                () => InvokeConfigButton(method, host),
                ModSettingsButtonTone.Normal,
                hoverTipDescription);
        }

        private static void InvokeConfigButton(MethodInfo method, ConfigHost host)
        {
            try
            {
                var modConfigType = ResolveType(ModConfigTypeName);
                var args = method.GetParameters();
                var values = new object?[args.Length];
                for (var i = 0; i < args.Length; i++)
                {
                    var t = args[i].ParameterType;
                    if (modConfigType != null && modConfigType.IsAssignableFrom(t))
                    {
                        values[i] = host.Instance;
                        continue;
                    }

                    values[i] = t.IsValueType ? Activator.CreateInstance(t) : null;
                }

                method.Invoke(method.IsStatic ? null : host.Instance, values);
                host.NotifyChanged();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModSettingsBaseLibReflectionMirror] ConfigButton '{method.Name}' failed: {ex.Message}");
            }
        }

        private static ModSettingsCallbackValueBinding<T> CallbackForStaticProperty<T>(
            string modId,
            string dataKey,
            PropertyInfo prop,
            ConfigHost host)
        {
            return ModSettingsBindings.Callback(modId, dataKey,
                () => (T)prop.GetValue(null)!,
                v =>
                {
                    prop.SetValue(null, v);
                    host.NotifyChanged();
                },
                host.Save);
        }

        private static bool IsVisibleMember(MemberInfo member, List<PropertyInfo> configProps, Type? hideUiAttrType,
            Type? buttonAttrType)
        {
            return member switch
            {
                PropertyInfo p => configProps.Contains(p) &&
                                  (hideUiAttrType == null || p.GetCustomAttribute(hideUiAttrType) == null),
                MethodInfo m => buttonAttrType != null && m.GetCustomAttribute(buttonAttrType) != null,
                _ => false,
            };
        }

        private static int GetSourceOrder(MemberInfo member)
        {
            return member switch
            {
                MethodInfo m => m.MetadataToken,
                PropertyInfo p => p.GetMethod?.MetadataToken ?? p.SetMethod?.MetadataToken ?? 0,
                _ => 0,
            };
        }

        private static void ConfirmAndRestoreDefaults(ConfigHost host)
        {
            if (Engine.GetMainLoop() is not SceneTree { Root: { } root })
            {
                host.RestoreDefaultsNoConfirm();
                return;
            }

            string body;
            string header;
            try
            {
                body = new LocString("settings_ui", "BASELIB-RESTORE_MODCONFIG_CONFIRMATION.body").GetFormattedText();
                header =
                    new LocString("settings_ui", "BASELIB-RESTORE_MODCONFIG_CONFIRMATION.header").GetFormattedText();
            }
            catch
            {
                body = "Reset all options for this mod to their default values?";
                header = "Restore defaults";
            }

            var submenu = FindRitsuModSettingsSubmenu(root);
            var attachParent = (Node?)submenu ?? root;
            var cancelText = ModSettingsLocalization.Get("baselib.restoreDefaults.cancel", "Cancel");
            var confirmText = ModSettingsLocalization.Get("baselib.restoreDefaults.confirm", "Restore defaults");

            ModSettingsUiFactory.ShowStyledConfirm(
                attachParent,
                header,
                body,
                cancelText,
                confirmText,
                true,
                () =>
                {
                    host.RestoreDefaultsNoConfirm();
                    host.NotifyChanged();
                    host.Save();
                    submenu?.RequestRefresh();
                });
        }

        private static RitsuModSettingsSubmenu? FindRitsuModSettingsSubmenu(Node root)
        {
            var queue = new Queue<Node>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                if (n is RitsuModSettingsSubmenu sm)
                    return sm;

                foreach (var child in n.GetChildren())
                    queue.Enqueue(child);
            }

            return null;
        }

        private static Type? ResolveType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? t = null;
                try
                {
                    t = asm.GetType(fullName, false);
                }
                catch
                {
                    // ignored
                }

                if (t != null)
                    return t;
            }

            return null;
        }

        private sealed record PendingSection(
            string Id,
            string? SectionTitle,
            List<(MemberInfo Member, string? Section)> Entries);

        private sealed class ConfigHost(
            object instance,
            MethodInfo changed,
            MethodInfo save,
            MethodInfo restore,
            MethodInfo getLabel,
            MethodInfo? baseLibLabel)
        {
            public object Instance { get; } = instance;

            public void NotifyChanged()
            {
                changed.Invoke(Instance, []);
            }

            public void Save()
            {
                save.Invoke(Instance, []);
            }

            public void RestoreDefaultsNoConfirm()
            {
                restore.Invoke(Instance, []);
            }

            public string ResolveLabel(string name)
            {
                return (string)getLabel.Invoke(Instance, [name])!;
            }

            public string ResolveBaseLibLabel(string name)
            {
                if (baseLibLabel != null)
                    return (string)baseLibLabel.Invoke(null, [name])!;

                return name;
            }

            public string GetModPrefix()
            {
                var p = Instance.GetType().GetProperty("ModPrefix",
                    BindingFlags.Public | BindingFlags.Instance);
                return p?.GetValue(Instance) as string ?? "";
            }
        }
    }
}
