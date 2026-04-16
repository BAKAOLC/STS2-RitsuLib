using System.Collections;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Mirrors converter-generated BaseLib compatibility configs into RitsuLib's settings UI.
    /// </summary>
    public static class ModSettingsBaseLibToRitsuGeneratedReflectionMirror
    {
        private const string RegistryTypeName = "BaseLibToRitsu.Generated.ModConfigRegistry";
        private const string ModConfigTypeName = "BaseLibToRitsu.Generated.ModConfig";
        private const string SectionAttrName = "BaseLibToRitsu.Generated.ConfigSectionAttribute";
        private const string HideUiAttrName = "BaseLibToRitsu.Generated.ConfigHideInUI";
        private const string ButtonAttrName = "BaseLibToRitsu.Generated.ConfigButtonAttribute";
        private const string ColorPickerAttrName = "BaseLibToRitsu.Generated.ConfigColorPickerAttribute";
        private const string HoverTipAttrName = "BaseLibToRitsu.Generated.ConfigHoverTipAttribute";
        private const string HoverTipsByDefaultAttrName =
            "BaseLibToRitsu.Generated.ConfigHoverTipsByDefaultAttribute";
        private const string LegacyHoverTipsByDefaultAttrName =
            "BaseLibToRitsu.Generated.HoverTipsByDefaultAttribute";

        private static readonly Lock Gate = new();

        /// <summary>
        ///     Registers mirrored settings pages for converter-generated BaseLib compatibility configs discovered in
        ///     loaded mod assemblies.
        /// </summary>
        /// <param name="pageId">Stable page id under each mod.</param>
        /// <param name="sortOrder">Sidebar ordering for mirrored pages.</param>
        /// <param name="pageTitle">Optional page title.</param>
        public static int TryRegisterMirroredPages(
            string pageId = "baselib",
            int sortOrder = 10_000,
            ModSettingsText? pageTitle = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pageId);

            lock (Gate)
            {
                pageTitle ??= ModSettingsText.I18N(ModSettingsLocalization.Instance, "baselib.mirroredPage.title",
                    "Mod config");
                var pageDescription = ModSettingsText.I18N(ModSettingsLocalization.Instance,
                    "baselib.mirroredPage.description",
                    "This page mirrors BaseLib-generated mod configuration entries.");

                var added = 0;
                foreach (var ctx in EnumerateContexts())
                    added += RegisterFromContext(ctx, pageId, sortOrder, pageTitle, pageDescription);
                return added;
            }
        }

        private static int RegisterFromContext(
            MirrorContext ctx,
            string pageId,
            int sortOrder,
            ModSettingsText pageTitle,
            ModSettingsText pageDescription)
        {
            var modIdProp = ctx.ModConfigType.GetProperty("ModId", BindingFlags.Instance | BindingFlags.Public);
            var propsField = ctx.ModConfigType.GetField("_configProperties", BindingFlags.Instance | BindingFlags.NonPublic);
            var changed = ctx.ModConfigType.GetMethod("Changed", BindingFlags.Instance | BindingFlags.Public);
            var save = ctx.ModConfigType.GetMethod("Save", BindingFlags.Instance | BindingFlags.Public);
            var restore = ctx.ModConfigType.GetMethod("RestoreDefaultsNoConfirm",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (modIdProp == null || propsField == null || changed == null || save == null || restore == null)
                return 0;

            var added = 0;
            foreach (var config in EnumerateConfigs(ctx))
            {
                var modId = modIdProp.GetValue(config) as string;
                if (string.IsNullOrWhiteSpace(modId) || ModSettingsRegistry.TryGetPage(modId, pageId, out _))
                    continue;

                var configType = config.GetType();
                if (!ModSettingsMirrorInteropPolicy.ShouldMirror(ModSettingsMirrorSource.BaseLib, modId, configType))
                    continue;

                var host = new Host(config, changed, save, restore);
                var propNames = ReadPropertyNames(propsField, config);
                if (!TryBuildPage(modId, pageId, sortOrder, pageTitle, pageDescription, host, propNames, ctx, configType))
                    continue;

                added++;
                RitsuLibFramework.Logger.Info(
                    $"[ModSettingsBaseLibToRitsuGeneratedReflectionMirror] Registered '{modId}::{pageId}' from '{ctx.Assembly.GetName().Name}'.");
            }

            return added;
        }

        private static bool TryBuildPage(
            string modId,
            string pageId,
            int sortOrder,
            ModSettingsText pageTitle,
            ModSettingsText pageDescription,
            Host host,
            IReadOnlySet<string> propNames,
            MirrorContext ctx,
            Type configType)
        {
            var members = configType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                .Where(m => IsVisibleMember(m, propNames, ctx.HideUiAttrType, ctx.ButtonAttrType))
                .OrderBy(GetSourceOrder)
                .ToList();
            if (members.Count == 0)
                return false;

            var sections = BuildSections(members, ctx.SectionAttrType);
            if (sections.Count == 0)
                return false;

            try
            {
                ModSettingsRegistry.Register(modId, builder =>
                {
                    builder.WithTitle(pageTitle)
                        .WithDescription(pageDescription)
                        .WithSortOrder(sortOrder)
                        .WithModDisplayName(ModSettingsText.Dynamic(() => host.ResolveModDisplayName(modId)));

                    for (var i = 0; i < sections.Count; i++)
                    {
                        var sec = sections[i];
                        var isLast = i == sections.Count - 1;
                        builder.AddSection(sec.Id, section =>
                        {
                            if (!string.IsNullOrWhiteSpace(sec.Title))
                                section.WithTitle(ModSettingsText.Dynamic(() => host.ResolveLabel(sec.Title!)));

                            foreach (var member in sec.Entries)
                            {
                                if (member is PropertyInfo prop)
                                    AddProperty(section, modId, prop, host, ctx, configType);
                                else if (member is MethodInfo method)
                                    AddButton(section, method, host, ctx, configType);
                            }

                            if (!isLast)
                                return;

                            var label = host.ResolveBaseLibLabel("RestoreDefaultsButton");
                            section.AddButton("baselib_restore_defaults", ModSettingsText.Literal(label),
                                ModSettingsText.Literal(label), () => ConfirmAndRestoreDefaults(host),
                                ModSettingsButtonTone.Danger);
                        });
                    }
                }, pageId);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModSettingsBaseLibToRitsuGeneratedReflectionMirror] Failed to register '{modId}::{pageId}': {ex.Message}");
                return false;
            }
        }

        private static List<PendingSection> BuildSections(List<MemberInfo> members, Type? sectionAttrType)
        {
            var result = new List<PendingSection>();
            PendingSection current = new("main", null, []);
            string? currentTitle = null;

            foreach (var member in members)
            {
                if (sectionAttrType != null && member.GetCustomAttribute(sectionAttrType, false) is { } attr)
                {
                    var title = sectionAttrType.GetProperty("Name")?.GetValue(attr) as string;
                    if (!string.IsNullOrWhiteSpace(title) && title != currentTitle)
                    {
                        if (current.Entries.Count > 0)
                            result.Add(current);
                        currentTitle = title;
                        current = new($"sec_{StringHelper.Slugify(title)}_{result.Count}", title, []);
                    }
                }
                current.Entries.Add(member);
            }

            if (current.Entries.Count > 0)
                result.Add(current);
            return result;
        }

        private static void AddProperty(
            ModSettingsSectionBuilder section,
            string modId,
            PropertyInfo prop,
            Host host,
            MirrorContext ctx,
            Type configType)
        {
            var id = $"bl_{StringHelper.Slugify(prop.Name)}";
            var label = ModSettingsText.Dynamic(() => host.ResolveLabel(prop.Name));
            var desc = TryHoverTip(prop, configType, host, ctx.HoverTipAttrType, ctx.HoverTipsByDefaultAttrType,
                ctx.LegacyHoverTipsByDefaultAttrType);
            var dataKey = $"baselib::{prop.Name}";
            var type = prop.PropertyType;

            if (type == typeof(bool))
            {
                section.AddToggle(id, label, CallbackForStaticProperty<bool>(modId, dataKey, prop, host), desc);
                return;
            }

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
                section.AddColor(id, label, colorBinding, desc, true, false);
                return;
            }

            var asColor = ctx.ColorPickerAttrType != null && prop.GetCustomAttribute(ctx.ColorPickerAttrType, false) != null;
            if (type == typeof(string) && asColor)
            {
                section.AddColor(id, label, CallbackForStaticProperty<string>(modId, dataKey, prop, host), desc, true, false);
                return;
            }

            if (type == typeof(string))
            {
                section.AddString(id, label, CallbackForStaticProperty<string>(modId, dataKey, prop, host), description: desc);
                return;
            }

            if (type == typeof(int))
            {
                var intBinding = ModSettingsBindings.Callback(modId, dataKey,
                    () => Convert.ToDouble((int)prop.GetValue(null)!),
                    value => { prop.SetValue(null, (int)Math.Round(value)); host.NotifyChanged(); },
                    host.Save);
                section.AddSlider(id, label, intBinding, 0d, 100d, 1d, value => ((int)Math.Round(value)).ToString(), desc);
                return;
            }

            if (type == typeof(float))
            {
#pragma warning disable CS0618
                section.AddSlider(id, label, CallbackForStaticProperty<float>(modId, dataKey, prop, host), 0f, 100f, 1f,
                    value => value.ToString("0.##"), desc);
#pragma warning restore CS0618
                return;
            }

            if (type == typeof(double))
            {
                section.AddSlider(id, label, CallbackForStaticProperty<double>(modId, dataKey, prop, host), 0d, 100d, 1d,
                    value => value.ToString("0.##"), desc);
                return;
            }

            if (!type.IsEnum)
                return;

            var enumBinding = typeof(ModSettingsBaseLibToRitsuGeneratedReflectionMirror)
                .GetMethod(nameof(CallbackForStaticProperty), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type)
                .Invoke(null, [modId, dataKey, prop, host]);
            typeof(ModSettingsSectionBuilder).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Single(m => m is { Name: nameof(ModSettingsSectionBuilder.AddEnumChoice), IsGenericMethodDefinition: true })
                .MakeGenericMethod(type)
                .Invoke(section, [id, label, enumBinding, null, desc, ModSettingsChoicePresentation.Stepper]);
        }

        private static void AddButton(
            ModSettingsSectionBuilder section,
            MethodInfo method,
            Host host,
            MirrorContext ctx,
            Type configType)
        {
            if (ctx.ButtonAttrType == null || method.GetCustomAttribute(ctx.ButtonAttrType, false) is not { } attr)
                return;

            var key = ctx.ButtonAttrType.GetProperty("ButtonLabelKey")?.GetValue(attr) as string ?? method.Name;
            var id = $"bl_btn_{StringHelper.Slugify(method.Name)}";
            var label = ModSettingsText.Dynamic(() => host.ResolveLabel(method.Name));
            var button = ModSettingsText.Dynamic(() => host.ResolveLabel(key));
            var desc = TryHoverTip(method, configType, host, ctx.HoverTipAttrType, ctx.HoverTipsByDefaultAttrType,
                ctx.LegacyHoverTipsByDefaultAttrType);
            section.AddButton(id, label, button, () => InvokeConfigButton(method, host), ModSettingsButtonTone.Normal, desc);
        }

        private static void InvokeConfigButton(MethodInfo method, Host host)
        {
            try
            {
                var args = method.GetParameters();
                var values = new object?[args.Length];
                for (var i = 0; i < args.Length; i++)
                    values[i] = args[i].ParameterType.IsInstanceOfType(host.Instance)
                        ? host.Instance
                        : (args[i].ParameterType.IsValueType ? Activator.CreateInstance(args[i].ParameterType) : null);
                method.Invoke(method.IsStatic ? null : host.Instance, values);
                host.NotifyChanged();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModSettingsBaseLibToRitsuGeneratedReflectionMirror] ConfigButton '{method.Name}' failed: {ex.Message}");
            }
        }

        private static ModSettingsText? TryHoverTip(
            MemberInfo member,
            Type configType,
            Host host,
            Type? hoverTipAttrType,
            Type? hoverTipsByDefaultAttrType,
            Type? legacyHoverTipsByDefaultAttrType)
        {
            if (!ShouldShowHoverTip(member, configType, hoverTipAttrType, hoverTipsByDefaultAttrType,
                    legacyHoverTipsByDefaultAttrType))
                return null;

            var prefix = host.ModPrefix;
            if (string.IsNullOrWhiteSpace(prefix))
                return null;

            var key = prefix + StringHelper.Slugify(member.Name) + ".hover.desc";
            if (!LocString.Exists("settings_ui", key))
                return null;
            return ModSettingsText.Dynamic(() => LocString.GetIfExists("settings_ui", key)?.GetFormattedText() ?? "");
        }

        private static bool ShouldShowHoverTip(
            MemberInfo member,
            Type configType,
            Type? hoverTipAttrType,
            Type? hoverTipsByDefaultAttrType,
            Type? legacyHoverTipsByDefaultAttrType)
        {
            bool? explicitFlag = null;
            if (hoverTipAttrType != null && member.GetCustomAttribute(hoverTipAttrType, false) is { } attr &&
                hoverTipAttrType.GetProperty("Enabled")?.GetValue(attr) is bool enabled)
            {
                explicitFlag = enabled;
            }

            var byDefault =
                (hoverTipsByDefaultAttrType != null && configType.GetCustomAttribute(hoverTipsByDefaultAttrType, false) != null) ||
                (legacyHoverTipsByDefaultAttrType != null &&
                 configType.GetCustomAttribute(legacyHoverTipsByDefaultAttrType, false) != null);
            return explicitFlag ?? byDefault;
        }

        private static ModSettingsCallbackValueBinding<T> CallbackForStaticProperty<T>(
            string modId,
            string dataKey,
            PropertyInfo prop,
            Host host)
        {
            return ModSettingsBindings.Callback(modId, dataKey, () => (T)prop.GetValue(null)!,
                value => { prop.SetValue(null, value); host.NotifyChanged(); }, host.Save);
        }

        private static bool IsVisibleMember(MemberInfo member, IReadOnlySet<string> propNames, Type? hideUiAttrType, Type? buttonAttrType)
        {
            return member switch
            {
                PropertyInfo p => propNames.Contains(p.Name) && (hideUiAttrType == null || p.GetCustomAttribute(hideUiAttrType) == null),
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

        private static IReadOnlySet<string> ReadPropertyNames(FieldInfo propsField, object config)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (propsField.GetValue(config) is not IEnumerable enumerable)
                return result;
            foreach (var item in enumerable)
                if (item is PropertyInfo prop)
                    result.Add(prop.Name);
            return result;
        }

        private static IEnumerable<object> EnumerateConfigs(MirrorContext ctx)
        {
            var getAll = ctx.RegistryType.GetMethod("GetAll", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            if (getAll?.Invoke(null, null) is IEnumerable all)
            {
                foreach (var item in all)
                    if (item != null)
                        yield return item;
                yield break;
            }

            if (ctx.RegistryType.GetField("Configs", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?.GetValue(null) is not IDictionary map)
                yield break;
            foreach (DictionaryEntry entry in map)
                if (entry.Value != null)
                    yield return entry.Value;
        }

        private static void ConfirmAndRestoreDefaults(Host host)
        {
            if (Engine.GetMainLoop() is not SceneTree { Root: { } root })
            {
                host.RestoreDefaultsNoConfirm();
                return;
            }

            var body = LocString.GetIfExists("settings_ui", "BASELIB-RESTORE_MODCONFIG_CONFIRMATION.body")?.GetFormattedText()
                       ?? "Reset all options for this mod to their default values?";
            var header = LocString.GetIfExists("settings_ui", "BASELIB-RESTORE_MODCONFIG_CONFIRMATION.header")?.GetFormattedText()
                         ?? "Restore defaults";
            var submenu = FindSubmenu(root);
            var attachParent = (Node?)submenu ?? root;
            ModSettingsUiFactory.ShowStyledConfirm(attachParent, header, body,
                ModSettingsLocalization.Get("baselib.restoreDefaults.cancel", "Cancel"),
                ModSettingsLocalization.Get("baselib.restoreDefaults.confirm", "Restore defaults"), true, () =>
                {
                    host.RestoreDefaultsNoConfirm();
                    host.NotifyChanged();
                    host.Save();
                    submenu?.RequestRefresh();
                });
        }

        private static RitsuModSettingsSubmenu? FindSubmenu(Node root)
        {
            var queue = new Queue<Node>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node is RitsuModSettingsSubmenu submenu)
                    return submenu;
                foreach (var child in node.GetChildren())
                    queue.Enqueue(child);
            }
            return null;
        }

        private static IEnumerable<MirrorContext> EnumerateContexts()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? registryType;
                Type? modConfigType;
                try
                {
                    registryType = asm.GetType(RegistryTypeName, false);
                    modConfigType = asm.GetType(ModConfigTypeName, false);
                }
                catch
                {
                    continue;
                }

                if (registryType == null || modConfigType == null)
                    continue;

                yield return new(asm, registryType, modConfigType, asm.GetType(SectionAttrName, false),
                    asm.GetType(HideUiAttrName, false), asm.GetType(ButtonAttrName, false),
                    asm.GetType(ColorPickerAttrName, false), asm.GetType(HoverTipAttrName, false),
                    asm.GetType(HoverTipsByDefaultAttrName, false), asm.GetType(LegacyHoverTipsByDefaultAttrName, false));
            }
        }

        private sealed record MirrorContext(
            Assembly Assembly,
            Type RegistryType,
            Type ModConfigType,
            Type? SectionAttrType,
            Type? HideUiAttrType,
            Type? ButtonAttrType,
            Type? ColorPickerAttrType,
            Type? HoverTipAttrType,
            Type? HoverTipsByDefaultAttrType,
            Type? LegacyHoverTipsByDefaultAttrType);

        private sealed record PendingSection(string Id, string? Title, List<MemberInfo> Entries);

        private sealed class Host(object instance, MethodInfo changed, MethodInfo save, MethodInfo restore)
        {
            public object Instance { get; } = instance;
            public string ModPrefix => ResolveRootNamespace() is { Length: > 0 } root ? root.ToUpperInvariant() + "-" : "";

            public void NotifyChanged() => changed.Invoke(Instance, []);
            public void Save() => save.Invoke(Instance, []);
            public void RestoreDefaultsNoConfirm() => restore.Invoke(Instance, []);

            public string ResolveLabel(string name)
            {
                var key = ModPrefix + StringHelper.Slugify(name) + ".title";
                return LocString.GetIfExists("settings_ui", key)?.GetFormattedText() ?? name;
            }

            public string ResolveBaseLibLabel(string name)
            {
                var key = "BASELIB-" + StringHelper.Slugify(name) + ".title";
                return LocString.GetIfExists("settings_ui", key)?.GetFormattedText() ?? name;
            }

            public string ResolveModDisplayName(string fallback)
            {
                var root = ResolveRootNamespace();
                if (!string.IsNullOrWhiteSpace(root))
                {
                    var key = root.ToUpperInvariant() + ".mod_title";
                    var localized = LocString.GetIfExists("settings_ui", key)?.GetFormattedText();
                    if (!string.IsNullOrWhiteSpace(localized))
                        return localized;
                }

                return Sts2ModManagerCompat.EnumerateModsForManifestLookup().FirstOrDefault(mod =>
                           string.Equals(mod.manifest?.id, fallback, StringComparison.OrdinalIgnoreCase))?.manifest?.name
                       ?? fallback;
            }

            private string ResolveRootNamespace()
            {
                var type = Instance.GetType();
                if (!string.IsNullOrWhiteSpace(type.Namespace))
                {
                    var dot = type.Namespace.IndexOf('.');
                    return dot < 0 ? type.Namespace : type.Namespace[..dot];
                }

                var asm = type.Assembly.GetName().Name ?? "";
                var asmDot = asm.IndexOf('.');
                return asmDot < 0 ? asm : asm[..asmDot];
            }
        }
    }
}
