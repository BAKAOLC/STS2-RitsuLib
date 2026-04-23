using System.Collections;
using System.Reflection;

namespace STS2RitsuLib.Settings
{
    internal static class BaseLibToRitsuGeneratedMirrorSource
    {
        private const string RegistryTypeName = "BaseLibToRitsu.Generated.ModConfigRegistry";
        private const string ModConfigTypeName = "BaseLibToRitsu.Generated.ModConfig";
        private const string SectionAttrName = "BaseLibToRitsu.Generated.ConfigSectionAttribute";
        private const string HideUiAttrName = "BaseLibToRitsu.Generated.ConfigHideInUI";
        private const string ButtonAttrName = "BaseLibToRitsu.Generated.ConfigButtonAttribute";
        private const string ColorPickerAttrName = "BaseLibToRitsu.Generated.ConfigColorPickerAttribute";
        private const string HoverTipAttrName = "BaseLibToRitsu.Generated.ConfigHoverTipAttribute";
        private const string HoverTipsByDefaultAttrName = "BaseLibToRitsu.Generated.ConfigHoverTipsByDefaultAttribute";
        private const string LegacyHoverTipsByDefaultAttrName = "BaseLibToRitsu.Generated.HoverTipsByDefaultAttribute";
        private const string VisibleIfAttrName = "BaseLibToRitsu.Generated.ConfigVisibleIfAttribute";

        private static readonly Lock Gate = new();

        public static int TryRegisterMirroredPages(string pageId = "baselib-generated", int sortOrder = 10_010,
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

                return EnumerateContexts().Sum(context =>
                    RegisterFromContext(context, pageId, sortOrder, pageTitle, pageDescription));
            }
        }

        private static int RegisterFromContext(MirrorContext context, string pageId, int sortOrder,
            ModSettingsText pageTitle, ModSettingsText pageDescription)
        {
            var modIdProperty = context.ModConfigType.GetProperty("ModId", BindingFlags.Instance | BindingFlags.Public);
            var propertiesField =
                context.ModConfigType.GetField("_configProperties", BindingFlags.Instance | BindingFlags.NonPublic);
            var changed = context.ModConfigType.GetMethod("Changed", BindingFlags.Instance | BindingFlags.Public);
            var save = context.ModConfigType.GetMethod("Save", BindingFlags.Instance | BindingFlags.Public);
            var restore = context.ModConfigType.GetMethod("RestoreDefaultsNoConfirm",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (modIdProperty == null || propertiesField == null || changed == null || save == null || restore == null)
                return 0;

            return (from config in EnumerateConfigs(context)
                    let modId = modIdProperty.GetValue(config) as string
                    where !string.IsNullOrWhiteSpace(modId) && !ModSettingsRegistry.TryGetPage(modId, pageId, out _)
                    let configType = config.GetType()
                    where
                        ModSettingsMirrorInteropPolicy.ShouldMirror(ModSettingsMirrorSource.BaseLibToRitsuGenerated,
                            modId,
                            configType)
                    let host = new BaseLibToRitsuGeneratedMirrorHost(config, changed, save, restore)
                    let propertyNames = ReadPropertyNames(propertiesField, config)
                    select BaseLibToRitsuGeneratedMirrorMapper.TryCreatePage(modId, pageId, sortOrder, pageTitle,
                        pageDescription, host, propertyNames, context.SectionAttrType, context.HideUiAttrType,
                        context.ButtonAttrType, context.ColorPickerAttrType, context.HoverTipAttrType,
                        context.HoverTipsByDefaultAttrType, context.LegacyHoverTipsByDefaultAttrType,
                        context.VisibleIfAttrType, configType, context.ModConfigType))
                .Count(ModSettingsMirrorRegistrar.TryRegister);
        }

        private static IReadOnlySet<string> ReadPropertyNames(FieldInfo propertiesField, object config)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (propertiesField.GetValue(config) is not IEnumerable enumerable)
                return result;
            foreach (var item in enumerable)
                if (item is PropertyInfo property)
                    result.Add(property.Name);
            return result;
        }

        private static IEnumerable<object> EnumerateConfigs(MirrorContext context)
        {
            var getAll = context.RegistryType.GetMethod("GetAll",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            if (getAll?.Invoke(null, null) is IEnumerable all)
            {
                foreach (var item in all)
                    if (item != null)
                        yield return item;
                yield break;
            }

            if (context.RegistryType
                    .GetField("Configs", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    ?.GetValue(null) is not IDictionary map)
                yield break;
            foreach (DictionaryEntry entry in map)
                if (entry.Value != null)
                    yield return entry.Value;
        }

        private static IEnumerable<MirrorContext> EnumerateContexts()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? registryType;
                Type? modConfigType;
                try
                {
                    registryType = assembly.GetType(RegistryTypeName, false);
                    modConfigType = assembly.GetType(ModConfigTypeName, false);
                }
                catch
                {
                    continue;
                }

                if (registryType == null || modConfigType == null)
                    continue;

                yield return new(assembly, registryType, modConfigType, assembly.GetType(SectionAttrName, false),
                    assembly.GetType(HideUiAttrName, false), assembly.GetType(ButtonAttrName, false),
                    assembly.GetType(ColorPickerAttrName, false), assembly.GetType(HoverTipAttrName, false),
                    assembly.GetType(HoverTipsByDefaultAttrName, false),
                    assembly.GetType(LegacyHoverTipsByDefaultAttrName, false),
                    assembly.GetType(VisibleIfAttrName, false));
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
            Type? LegacyHoverTipsByDefaultAttrType,
            Type? VisibleIfAttrType);
    }
}
