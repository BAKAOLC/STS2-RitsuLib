using System.Collections;
using System.Globalization;
using System.Reflection;
using Godot;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
    internal static class ModConfigMirrorSource
    {
        private const string ApiTypeName = "ModConfig.ModConfigApi";
        private const string ManagerTypeName = "ModConfig.ModConfigManager";
        private const string I18NTypeName = "ModConfig.I18n";

        private static readonly Lock Gate = new();

        private static Type? _cachedApiType;
        private static MethodInfo? _cachedGetValueOpen;
        private static MethodInfo? _cachedSetValue;
        private static MethodInfo? _cachedResetDefaults;
        private static MethodInfo? _cachedI18NGet;
        private static PropertyInfo? _cachedRegistrationsProp;

        public static bool IsModConfigPresent => ResolveType(ApiTypeName) != null;

        public static int TryRegisterMirroredPages(
            string pageId = "modconfig",
            int sortOrder = 10_020,
            ModSettingsText? pageTitle = null)
        {
            return TryRegisterMirroredPages(pageId, sortOrder, pageTitle, null);
        }

        public static int TryRegisterMirroredPages(
            string pageId,
            int sortOrder,
            ModSettingsText? pageTitle,
            ModConfigMirrorRegistrationOptions? options)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pageId);

            var o = options ?? ModConfigMirrorRegistrationOptions.Default;

            lock (Gate)
            {
                if (!TryResolveInterop(out _, out var getValueOpen, out var setValue, out var resetDefaults,
                        out var registrationsProp, out var i18NGet))
                    return 0;

                var registrations = registrationsProp.GetValue(null);
                if (registrations is not IEnumerable enumerable)
                    return 0;

                pageTitle ??= ModSettingsText.I18N(ModSettingsLocalization.Instance, "modconfig.mirroredPage.title",
                    "Mod config (ModConfig)");
                var pageDescription = ModSettingsText.I18N(ModSettingsLocalization.Instance,
                    "modconfig.mirroredPage.description",
                    "Proxy page for a mod registered with the ModConfig library. Values are stored by ModConfig.");

                var added = 0;
                foreach (var item in enumerable)
                {
                    if (!TryUnwrapDictionaryItem(item, out var modId, out var reg))
                        continue;

                    if (!ModSettingsMirrorInteropPolicy.ShouldMirror(ModSettingsMirrorSource.ModConfig, modId))
                        continue;

                    if (ModSettingsRegistry.TryGetPage(modId, pageId, out _))
                        continue;

                    if (!TryRegisterOneMod(modId, pageId, sortOrder, pageTitle, pageDescription, reg,
                            getValueOpen, setValue, resetDefaults, i18NGet, o))
                        continue;

                    added++;
                }

                return added;
            }
        }

        public static void ClearModConfigReflectionCache()
        {
            lock (Gate)
            {
                _cachedApiType = null;
                _cachedGetValueOpen = null;
                _cachedSetValue = null;
                _cachedResetDefaults = null;
                _cachedRegistrationsProp = null;
                _cachedI18NGet = null;
            }
        }

        private static bool TryResolveInterop(
            out Type apiType,
            out MethodInfo getValueOpen,
            out MethodInfo setValue,
            out MethodInfo resetDefaults,
            out PropertyInfo registrationsProp,
            out MethodInfo? i18NGet)
        {
            if (_cachedApiType != null && _cachedGetValueOpen != null && _cachedSetValue != null &&
                _cachedResetDefaults != null && _cachedRegistrationsProp != null)
            {
                apiType = _cachedApiType;
                getValueOpen = _cachedGetValueOpen;
                setValue = _cachedSetValue;
                resetDefaults = _cachedResetDefaults;
                registrationsProp = _cachedRegistrationsProp;
                i18NGet = _cachedI18NGet;
                return true;
            }

            var resolvedApi = ResolveType(ApiTypeName);
            if (resolvedApi == null)
            {
#pragma warning disable CS8601
                apiType = null!;
                getValueOpen = null!;
                setValue = null!;
                resetDefaults = null!;
                registrationsProp = null!;
#pragma warning restore CS8601
                i18NGet = null;
                return false;
            }

            apiType = resolvedApi;
            var asm = apiType.Assembly;

            var resolvedGetValue = apiType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m is { Name: "GetValue", IsGenericMethodDefinition: true } &&
                                     m.GetParameters() is [{ ParameterType: var p0 }, { ParameterType: var p1 }] &&
                                     p0 == typeof(string) && p1 == typeof(string));
            var resolvedSetValue = apiType.GetMethod("SetValue",
                BindingFlags.Public | BindingFlags.Static, null, [typeof(string), typeof(string), typeof(object)],
                null);

            var managerType = asm.GetType(ManagerTypeName);
            var resolvedReset = managerType?.GetMethod("ResetToDefaults",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, [typeof(string)], null);
            var resolvedRegs =
                managerType?.GetProperty("Registrations",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (resolvedGetValue == null || resolvedSetValue == null || resolvedReset == null || resolvedRegs == null)
            {
#pragma warning disable CS8601
                getValueOpen = null!;
                setValue = null!;
                resetDefaults = null!;
                registrationsProp = null!;
#pragma warning restore CS8601
                i18NGet = null;
                return false;
            }

            getValueOpen = resolvedGetValue;
            setValue = resolvedSetValue;
            resetDefaults = resolvedReset;
            registrationsProp = resolvedRegs;

            var i18NType = asm.GetType(I18NTypeName);
            i18NGet = i18NType?.GetMethod("Get",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                [typeof(string), typeof(string)], null);

            _cachedApiType = apiType;
            _cachedGetValueOpen = getValueOpen;
            _cachedSetValue = setValue;
            _cachedResetDefaults = resetDefaults;
            _cachedRegistrationsProp = registrationsProp;
            _cachedI18NGet = i18NGet;

            return true;
        }

        private static bool TryUnwrapDictionaryItem(object item, out string modId, out object reg)
        {
            modId = "";
            reg = null!;

            switch (item)
            {
                case DictionaryEntry de:
                {
                    if (de.Key is not string ks || string.IsNullOrWhiteSpace(ks) || de.Value == null)
                        return false;

                    modId = ks;
                    reg = de.Value;
                    break;
                }
                default:
                {
                    var t = item.GetType();
                    if (!t.IsGenericType || !t.Name.StartsWith("KeyValuePair`", StringComparison.Ordinal))
                        return false;

                    var k = t.GetProperty("Key")?.GetValue(item) as string;
                    var v = t.GetProperty("Value")?.GetValue(item);
                    if (string.IsNullOrWhiteSpace(k) || v == null)
                        return false;

                    modId = k;
                    reg = v;
                    break;
                }
            }

            return true;
        }

        private static bool TryRegisterOneMod(
            string modId,
            string pageId,
            int sortOrder,
            ModSettingsText pageTitle,
            ModSettingsText pageDescription,
            object registration,
            MethodInfo getValueOpen,
            MethodInfo setValue,
            MethodInfo resetDefaults,
            MethodInfo? i18NGet,
            ModConfigMirrorRegistrationOptions mirrorOptions)
        {
            var regType = registration.GetType();
            var entriesObj = regType.GetProperty("Entries")?.GetValue(registration);
            if (entriesObj is not Array entries || entries.Length == 0)
                return false;

            var sectionPlans = BuildMirroredSectionPlans(entries, i18NGet);
            if (sectionPlans.Count == 0)
                return false;

            var hasRenderable = false;
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries.GetValue(i) is not { } entry)
                    continue;
                if (!IsRenderableConfigEntry(entry)) continue;
                hasRenderable = true;
                break;
            }

            if (!hasRenderable)
                return false;

            try
            {
                ModSettingsRegistry.Register(modId, builder =>
                {
                    builder
                        .WithTitle(pageTitle)
                        .WithDescription(pageDescription)
                        .WithSortOrder(sortOrder)
                        .WithModDisplayName(ModSettingsText.Dynamic(() =>
                            ResolveRegistrationDisplayName(registration, i18NGet)));

                    for (var sectionIndex = 0; sectionIndex < sectionPlans.Count; sectionIndex++)
                    {
                        var plan = sectionPlans[sectionIndex];
                        var appendRestoreDefaults = sectionIndex == sectionPlans.Count - 1;

                        builder.AddSection(plan.Id, section =>
                        {
                            if (plan.Title != null)
                                section.WithTitle(plan.Title);

                            for (var entryIndex = 0; entryIndex < plan.Entries.Count; entryIndex++)
                            {
                                var mapped = plan.Entries[entryIndex];
                                AppendConfigEntry(section, modId, mapped.Entry, mapped.SourceIndex, getValueOpen,
                                    setValue,
                                    i18NGet, mirrorOptions);
                            }

                            if (!appendRestoreDefaults)
                                return;

                            section.AddButton(
                                "modconfig_restore_defaults",
                                ModSettingsText.Literal(ModSettingsLocalization.Get("modconfig.restoreDefaults.row",
                                    "Defaults")),
                                ModSettingsText.Literal(ModSettingsLocalization.Get("modconfig.restoreDefaults.button",
                                    "Restore defaults")),
                                () => ConfirmAndResetModConfig(modId, resetDefaults),
                                ModSettingsButtonTone.Danger);
                        });
                    }
                }, pageId);
                ModSettingsMirrorSyncPolicyRegistry.RegisterPage(modId, pageId, ModSettingsMirrorSource.ModConfig);

                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModConfigMirrorSource] Failed to register page '{modId}::{pageId}': {ex.Message}");
                return false;
            }
        }

        private static bool IsRenderableConfigEntry(object entry)
        {
            var typeName = ResolveModConfigEntryTypeName(entry);
            return typeName is not ("" or "Header" or "Separator");
        }

        private static List<ModConfigMirrorSectionPlan> BuildMirroredSectionPlans(Array entries, MethodInfo? i18NGet)
        {
            var plans = new List<ModConfigMirrorSectionPlan>();
            var sectionCounter = 0;

            var current = new ModConfigMirrorSectionPlan(
                "modconfig_main",
                ModSettingsText.Literal(ModSettingsLocalization.Get("modconfig.section.main", "General")));
            plans.Add(current);

            for (var i = 0; i < entries.Length; i++)
            {
                if (entries.GetValue(i) is not { } entry)
                    continue;

                var typeName = ResolveModConfigEntryTypeName(entry);
                if (typeName == "Header")
                {
                    var headerTitle = ModSettingsText.Dynamic(() => ResolveEntryLabel(entry, i18NGet));
                    if (current.Entries.Count == 0)
                    {
                        current.Title = headerTitle;
                        continue;
                    }

                    sectionCounter++;
                    current = new($"modconfig_sec_{sectionCounter}", headerTitle);
                    plans.Add(current);
                    continue;
                }

                current.Entries.Add(new(entry, i));
            }

            plans.RemoveAll(static plan => plan.Entries.Count == 0);
            return plans;
        }

        private static string ResolveModConfigEntryTypeName(object entry)
        {
            var t = entry.GetType();
            var typeObj = t.GetProperty("Type")?.GetValue(entry);
            switch (typeObj)
            {
                case null:
                    return "";
                case Enum e:
                {
                    var et = e.GetType();
                    if (Enum.IsDefined(et, e))
                        return e.ToString();
                    break;
                }
            }

            var name = typeObj.ToString();
            if (!string.IsNullOrEmpty(name) && int.TryParse(name, NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out var asInt))
                return MapModConfigTypeCodeToName(asInt);

            return name ?? "";
        }

        private static string MapModConfigTypeCodeToName(int code)
        {
            return code switch
            {
                0 => "Toggle",
                1 => "Slider",
                2 => "Dropdown",
                3 => "KeyBind",
                4 => "TextInput",
                5 => "Header",
                6 => "Separator",
                7 => "Button",
                8 => "ColorPicker",
                _ => "",
            };
        }

        private static void AppendConfigEntry(
            ModSettingsSectionBuilder section,
            string modId,
            object entry,
            int index,
            MethodInfo getValueOpen,
            MethodInfo setValue,
            MethodInfo? i18NGet,
            ModConfigMirrorRegistrationOptions mirrorOptions)
        {
            var t = entry.GetType();
            var typeName = ResolveModConfigEntryTypeName(entry);

            switch (typeName)
            {
                case "Header":
                {
                    var id = $"mc_hdr_{index}";
                    var label = ModSettingsText.Dynamic(() => ResolveEntryLabel(entry, i18NGet));
                    section.AddHeader(id, label);
                    return;
                }
                case "Separator":
                {
                    section.AddHeader($"mc_sep_{index}",
                        ModSettingsText.Literal(ModSettingsLocalization.Get("modconfig.separator", "—")));
                    return;
                }
            }

            var key = t.GetProperty("Key")?.GetValue(entry) as string;
            if (string.IsNullOrWhiteSpace(key))
                key = $"entry_{index}";

            var slug = ModSettingsMirrorSlugPolicy.Normalize(key);
            var idBase = $"mc_{slug}";
            var labelText = ModSettingsText.Dynamic(() => ResolveEntryLabel(entry, i18NGet));
            var descText = ResolveEntryDescription(entry, i18NGet);
            var dataKey = $"modconfig::{key}";

            try
            {
                switch (typeName)
                {
                    case "Toggle":
                    {
                        var binding = ModSettingsBindings.Callback(modId, dataKey,
                            () => CoerceModConfigToggle(getValueOpen, modId, key),
                            v => InvokeSetValue(setValue, modId, key, v),
                            () => { });
                        section.AddToggle(idBase, labelText, binding, descText);
                        return;
                    }
                    case "Slider":
                    {
                        var min = Convert.ToDouble(t.GetProperty("Min")?.GetValue(entry) ?? 0d);
                        var max = Convert.ToDouble(t.GetProperty("Max")?.GetValue(entry) ?? 100d);
                        var step = Convert.ToDouble(t.GetProperty("Step")?.GetValue(entry) ?? 1d);
                        if (max < min)
                            (min, max) = (max, min);
                        if (step <= 0d)
                            step = 1d;

                        var format = t.GetProperty("Format")?.GetValue(entry) as string ?? "F0";

                        var binding = ModSettingsBindings.Callback(modId, dataKey,
                            () => CoerceModConfigSliderDouble(getValueOpen, modId, key),
                            v => InvokeSetValue(setValue, modId, key, (float)v),
                            () => { });
                        section.AddSlider(idBase, labelText, binding, min, max, step, Fmt, descText);
                        return;

                        string Fmt(double v)
                        {
                            try
                            {
                                return ((float)v).ToString(format, CultureInfo.InvariantCulture);
                            }
                            catch
                            {
                                return v.ToString("F2", CultureInfo.InvariantCulture);
                            }
                        }
                    }
                    case "Dropdown":
                    {
                        var optValues = t.GetProperty("Options")?.GetValue(entry) as string[] ?? [];
                        if (optValues.Length == 0)
                            return;

                        var rawCurrent = InvokeGetValue<string>(getValueOpen, modId, key);
                        var orphan = !string.IsNullOrEmpty(rawCurrent) &&
                                     Array.IndexOf(optValues, rawCurrent) < 0;

                        ModSettingsChoiceOption<string>[] choiceOptions;
                        if (orphan && rawCurrent != null)
                        {
                            choiceOptions = new ModSettingsChoiceOption<string>[optValues.Length + 1];
                            choiceOptions[0] = new(rawCurrent,
                                ModSettingsText.Literal(rawCurrent + ModSettingsLocalization.Get(
                                    "modconfig.dropdown.orphanSuffix", " (saved)")));
                            for (var o = 0; o < optValues.Length; o++)
                            {
                                var captured = o;
                                var optVal = optValues[captured];
                                choiceOptions[captured + 1] = new(optVal, ModSettingsText.Dynamic(() =>
                                    ResolveOptionLabel(entry, captured, optVal, i18NGet)));
                            }
                        }
                        else
                        {
                            choiceOptions = new ModSettingsChoiceOption<string>[optValues.Length];
                            for (var o = 0; o < optValues.Length; o++)
                            {
                                var captured = o;
                                var optVal = optValues[captured];
                                choiceOptions[captured] = new(optVal, ModSettingsText.Dynamic(() =>
                                    ResolveOptionLabel(entry, captured, optVal, i18NGet)));
                            }
                        }

                        var binding = ModSettingsBindings.Callback(modId, dataKey,
                            () =>
                            {
                                var cur = InvokeGetValue<string>(getValueOpen, modId, key) ?? "";
                                return string.IsNullOrEmpty(cur) ? optValues[0] : cur;
                            },
                            v => InvokeSetValue(setValue, modId, key, v ?? optValues[0]),
                            () => { });

                        var presentation = choiceOptions.Length > 5
                            ? ModSettingsChoicePresentation.Dropdown
                            : ModSettingsChoicePresentation.Stepper;

                        section.AddChoice(idBase, labelText, binding, choiceOptions, descText, presentation);
                        return;
                    }
                    case "KeyBind":
                    {
                        var binding = ModSettingsBindings.Callback(modId, dataKey,
                            () => KeyCodeToBindingString(CoerceModConfigKeyCode(getValueOpen, modId, key)),
                            v =>
                            {
                                var code = ParseBindingStringToKeyCode(v);
                                InvokeSetValue(setValue, modId, key, code);
                            },
                            () => { });
                        section.AddKeyBinding(idBase, labelText, binding,
                            mirrorOptions.KeyBindAllowModifierCombos,
                            mirrorOptions.KeyBindAllowModifierOnly,
                            mirrorOptions.KeyBindDistinguishModifierSides,
                            descText);
                        return;
                    }
                    case "TextInput":
                    {
                        var maxLen = t.GetProperty("MaxLength")?.GetValue(entry) as int? ?? 64;
                        var placeholderRaw = t.GetProperty("Placeholder")?.GetValue(entry) as string;
                        var placeholder = string.IsNullOrEmpty(placeholderRaw)
                            ? null
                            : ModSettingsText.Literal(placeholderRaw);

                        var validatorDel = t.GetProperty("Validator")?.GetValue(entry) as Delegate;
                        Func<string, bool>? validationVisual = null;
                        if (validatorDel != null)
                            validationVisual = text =>
                            {
                                try
                                {
                                    var r = validatorDel.DynamicInvoke(text);
                                    return r is true;
                                }
                                catch
                                {
                                    return false;
                                }
                            };

                        var binding = ModSettingsBindings.Callback(modId, dataKey,
                            () => InvokeGetValue<string>(getValueOpen, modId, key) ?? "",
                            v => InvokeSetValue(setValue, modId, key, v ?? ""),
                            () => { });
                        section.AddString(idBase, labelText, binding, placeholder,
                            maxLen > 0 ? maxLen : null, descText, validationVisual);
                        return;
                    }
                    case "Button":
                    {
                        var buttonText = ModSettingsText.Dynamic(() => ResolveButtonText(entry, i18NGet));
                        section.AddButton(idBase, labelText, buttonText,
                            () => InvokeConfigButtonCallback(entry),
                            ModSettingsButtonTone.Normal,
                            descText);
                        return;
                    }
                    case "ColorPicker":
                    {
                        var editAlpha = t.GetProperty("EditAlpha")?.GetValue(entry) as bool? ?? false;
                        var editIntensity = t.GetProperty("EditIntensity")?.GetValue(entry) as bool? ?? false;
                        var binding = ModSettingsBindings.Callback(modId, dataKey,
                            () => InvokeGetValue<string>(getValueOpen, modId, key) ?? "#FFFFFF",
                            v => InvokeSetValue(setValue, modId, key, v ?? "#FFFFFF"),
                            () => { });
                        section.AddColor(idBase, labelText, binding, descText, editAlpha, editIntensity);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModConfigMirrorSource] Failed to append entry '{modId}.{key}' ({typeName}): {ex.Message}");
            }
        }

        private static string ResolveRegistrationDisplayName(object registration, MethodInfo? i18NGet)
        {
            var regType = registration.GetType();
            var fallback = regType.GetProperty("DisplayName")?.GetValue(registration) as string ?? "";
            if (regType.GetProperty("DisplayNames")?.GetValue(registration) is not IDictionary { Count: > 0 } map)
                return string.IsNullOrEmpty(fallback) ? "Mod" : fallback;
            var lang = TryModConfigCurrentLang();
            if (!string.IsNullOrEmpty(lang) && map.Contains(lang) && map[lang] is string exact)
                return exact;

            foreach (DictionaryEntry e in map)
            {
                if (e.Key is not string k || e.Value is not string v)
                    continue;
                if (lang != null && (lang.StartsWith(k, StringComparison.OrdinalIgnoreCase) ||
                                     k.StartsWith(lang, StringComparison.OrdinalIgnoreCase)))
                    return v;
            }

            if (map.Contains("en") && map["en"] is string en)
                return en;

            return string.IsNullOrEmpty(fallback) ? "Mod" : fallback;
        }

        private static string? TryModConfigCurrentLang()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? i18N;
                try
                {
                    i18N = asm.GetType(I18NTypeName, false);
                }
                catch
                {
                    continue;
                }

                var p = i18N?.GetProperty("CurrentLang",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (p?.GetValue(null) is string lang)
                    return lang;
            }

            return null;
        }

        private static string ResolveEntryLabel(object entry, MethodInfo? i18NGet)
        {
            var t = entry.GetType();
            var fallback = t.GetProperty("Label")?.GetValue(entry) as string ?? "";
            var labelKey = t.GetProperty("LabelKey")?.GetValue(entry) as string;
            var fromI18N = TryI18N(labelKey, fallback, i18NGet);
            return fromI18N ?? ResolveLangMap(t.GetProperty("Labels")?.GetValue(entry) as IDictionary, fallback);
        }

        private static ModSettingsText? ResolveEntryDescription(object entry, MethodInfo? i18NGet)
        {
            var t = entry.GetType();
            var fallback = t.GetProperty("Description")?.GetValue(entry) as string ?? "";
            var mapResolved = ResolveLangMap(t.GetProperty("Descriptions")?.GetValue(entry) as IDictionary, "");
            var body = !string.IsNullOrWhiteSpace(mapResolved) ? mapResolved : fallback;

            if (!string.IsNullOrWhiteSpace(mapResolved))
                return string.IsNullOrWhiteSpace(body) ? null : ModSettingsText.Literal(body);
            var dk = t.GetProperty("DescriptionKey")?.GetValue(entry) as string;
            body = TryI18N(dk, fallback, i18NGet) ?? fallback;

            return string.IsNullOrWhiteSpace(body) ? null : ModSettingsText.Literal(body);
        }

        private static string ResolveButtonText(object entry, MethodInfo? i18NGet)
        {
            var t = entry.GetType();
            var buttonFallback = t.GetProperty("ButtonText")?.GetValue(entry) as string ?? "";
            var resolved = ResolveLangMap(t.GetProperty("ButtonTexts")?.GetValue(entry) as IDictionary, buttonFallback);
            return !string.IsNullOrWhiteSpace(resolved) ? resolved : ResolveEntryLabel(entry, i18NGet);
        }

        private static string ResolveOptionLabel(object entry, int index, string fallback, MethodInfo? i18NGet)
        {
            var t = entry.GetType();
            if (t.GetProperty("OptionsKeys")?.GetValue(entry) is not string[] keys || index < 0 || index >= keys.Length)
                return fallback;
            var k = keys[index];
            var fromI18N = TryI18N(k, fallback, i18NGet);
            return fromI18N ?? fallback;
        }

        private static string? TryI18N(string? key, string fallback, MethodInfo? i18NGet)
        {
            return ModSettingsMirrorTextPolicy.TryI18N(key, fallback, i18NGet);
        }

        private static string ResolveLangMap(IDictionary? map, string fallback)
        {
            return ModSettingsMirrorTextPolicy.ResolveLangMap(map, fallback, TryModConfigCurrentLang);
        }

        private static T InvokeGetValue<T>(MethodInfo getValueOpen, string modId, string key)
        {
            try
            {
                var m = getValueOpen.MakeGenericMethod(typeof(T));
                var r = FastMethodInvoker.InvokeStatic2(m, modId, key);
                return r switch
                {
                    T ok => ok,
                    null => default!,
                    _ => (T)Convert.ChangeType(r, typeof(T)),
                };
            }
            catch
            {
                return default!;
            }
        }

        private static void InvokeSetValue(MethodInfo setValue, string modId, string key, object value)
        {
            try
            {
                FastMethodInvoker.InvokeStatic3Void(setValue, modId, key, value);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModConfigMirrorSource] SetValue failed [{modId}.{key}]: {ex.Message}");
            }
        }

        private static void InvokeConfigButtonCallback(object entry)
        {
            if (entry.GetType().GetProperty("OnChanged")?.GetValue(entry) is not Delegate onChanged)
                return;

            try
            {
                var inv = onChanged.Method;
                var ps = inv.GetParameters();
                switch (ps.Length)
                {
                    case 0:
                        _ = onChanged.DynamicInvoke();
                        return;
                    case 1:
                    {
                        var p0 = ps[0].ParameterType;
                        object? arg;
                        if (p0 == typeof(bool) || p0 == typeof(object))
                            arg = true;
                        else if (!p0.IsValueType)
                            arg = null;
                        else
                            arg = Activator.CreateInstance(p0);

                        _ = onChanged.DynamicInvoke(arg);
                        return;
                    }
                    default:
                        var args = new object?[ps.Length];
                        for (var i = 0; i < ps.Length; i++)
                        {
                            var pi = ps[i].ParameterType;
                            if (pi == typeof(bool))
                                args[i] = true;
                            else if (!pi.IsValueType)
                                args[i] = null;
                            else
                                args[i] = Activator.CreateInstance(pi);
                        }

                        _ = onChanged.DynamicInvoke(args);
                        return;
                }
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModConfigMirrorSource] Config button callback failed: {ex.Message}");
            }
        }

        private static bool CoerceModConfigToggle(MethodInfo getValueOpen, string modId, string key)
        {
            try
            {
                var m = getValueOpen.MakeGenericMethod(typeof(object));
                var r = FastMethodInvoker.InvokeStatic2(m, modId, key);
                return r switch
                {
                    null => false,
                    bool b => b,
                    string s => bool.TryParse(s, out var bs) && bs,
                    _ => Convert.ToBoolean(r, CultureInfo.InvariantCulture),
                };
            }
            catch
            {
                return false;
            }
        }

        private static double CoerceModConfigSliderDouble(MethodInfo getValueOpen, string modId, string key)
        {
            try
            {
                var m = getValueOpen.MakeGenericMethod(typeof(object));
                var r = FastMethodInvoker.InvokeStatic2(m, modId, key);
                return r switch
                {
                    null => 0d,
                    double d => d,
                    float f => f,
                    int i => i,
                    long l => l,
                    _ => Convert.ToDouble(r, CultureInfo.InvariantCulture),
                };
            }
            catch
            {
                return 0d;
            }
        }

        private static long CoerceModConfigKeyCode(MethodInfo getValueOpen, string modId, string key)
        {
            try
            {
                var m = getValueOpen.MakeGenericMethod(typeof(object));
                var r = FastMethodInvoker.InvokeStatic2(m, modId, key);
                return r switch
                {
                    null => 0,
                    long l => l,
                    int i => i,
                    double d => (long)Math.Round(d),
                    _ => Convert.ToInt64(r, CultureInfo.InvariantCulture),
                };
            }
            catch
            {
                return 0;
            }
        }

        private static string KeyCodeToBindingString(long code)
        {
            if (code == 0)
                return string.Empty;

            try
            {
                var key = (Key)code;
                var s = OS.GetKeycodeString(key);
                return string.IsNullOrEmpty(s) ? key.ToString() : s;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static long ParseBindingStringToKeyCode(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return 0;

            var tail = s.Contains('+', StringComparison.Ordinal)
                ? s.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last()
                : s.Trim();

            if (Enum.TryParse<Key>(tail, true, out var direct))
                return (long)direct;

            foreach (var candidate in Enum.GetValues<Key>())
            {
                if (string.Equals(candidate.ToString(), tail, StringComparison.OrdinalIgnoreCase))
                    return (long)candidate;

                try
                {
                    var label = OS.GetKeycodeString(candidate);
                    if (!string.IsNullOrEmpty(label) &&
                        string.Equals(label, tail, StringComparison.OrdinalIgnoreCase))
                        return (long)candidate;
                }
                catch
                {
                    // ignored
                }
            }

            return 0;
        }

        private static void ConfirmAndResetModConfig(string modId, MethodInfo resetDefaults)
        {
            var body = ModSettingsLocalization.Get("modconfig.restoreDefaults.body",
                "Reset all ModConfig options for this mod to their default values?");
            var header = ModSettingsLocalization.Get("modconfig.restoreDefaults.header", "Restore defaults");
            var cancelText = ModSettingsLocalization.Get("modconfig.restoreDefaults.cancel", "Cancel");
            var confirmText = ModSettingsLocalization.Get("modconfig.restoreDefaults.confirm", "Restore defaults");

            ModSettingsMirrorUiActions.ConfirmAndRestoreDefaults(
                () => TryResetDefaults(resetDefaults, modId),
                null,
                header,
                body,
                cancelText,
                confirmText);
        }

        private static void TryResetDefaults(MethodInfo resetDefaults, string modId)
        {
            try
            {
                _ = FastMethodInvoker.InvokeStatic1(resetDefaults, modId);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModConfigMirrorSource] ResetToDefaults failed for '{modId}': {ex.Message}");
            }
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

        private sealed class ModConfigMirrorSectionPlan(string id, ModSettingsText? title)
        {
            public string Id { get; } = id;
            public ModSettingsText? Title { get; set; } = title;
            public List<ModConfigMappedEntry> Entries { get; } = [];
        }

        private readonly record struct ModConfigMappedEntry(object Entry, int SourceIndex);
    }
}
