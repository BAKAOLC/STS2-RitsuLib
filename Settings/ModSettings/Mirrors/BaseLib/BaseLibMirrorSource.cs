using System.Collections;
using System.Reflection;

namespace STS2RitsuLib.Settings
{
    internal static class BaseLibMirrorSource
    {
        internal const string RegistryTypeName = "BaseLib.Config.ModConfigRegistry";
        internal const string ModConfigTypeName = "BaseLib.Config.ModConfig";
        private const string ConfigSectionAttributeName = "BaseLib.Config.ConfigSectionAttribute";
        private const string ConfigHideInUiAttributeName = "BaseLib.Config.ConfigHideInUI";
        private const string ConfigButtonAttributeName = "BaseLib.Config.ConfigButtonAttribute";
        private const string ConfigSliderAttributeName = "BaseLib.Config.ConfigSliderAttribute";
        private const string SliderRangeAttributeName = "BaseLib.Config.SliderRangeAttribute";
        private const string SliderLabelFormatAttributeName = "BaseLib.Config.SliderLabelFormatAttribute";
        private const string ConfigTextInputAttributeName = "BaseLib.Config.ConfigTextInputAttribute";
        private const string ConfigColorPickerAttributeName = "BaseLib.Config.ConfigColorPickerAttribute";
        private const string ConfigHoverTipAttributeName = "BaseLib.Config.ConfigHoverTipAttribute";
        private const string ConfigHoverTipsByDefaultAttributeName = "BaseLib.Config.ConfigHoverTipsByDefaultAttribute";
        private const string HoverTipsByDefaultAttributeName = "BaseLib.Config.HoverTipsByDefaultAttribute";
        private const string ConfigVisibleIfAttributeName = "BaseLib.Config.ConfigVisibleIfAttribute";

        private static readonly Lock Gate = new();
        private static bool _pagesRegistered;

        public static bool IsBaseLibPresent => ResolveType(RegistryTypeName) != null;

        public static int TryRegisterMirroredPages(string pageId = "baselib", int sortOrder = 10_000,
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

                var configsField = registryType.GetField("ModConfigs", BindingFlags.Static | BindingFlags.NonPublic);
                if (configsField?.GetValue(null) is not IDictionary rawMap)
                {
                    _pagesRegistered = true;
                    return 0;
                }

                var configPropsField =
                    modConfigType.GetField("ConfigProperties", BindingFlags.Instance | BindingFlags.NonPublic);
                var getLabel = modConfigType.GetMethod("GetLabelText", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, [typeof(string)], null);
                var changed = modConfigType.GetMethod("Changed", BindingFlags.Instance | BindingFlags.Public);
                var save = modConfigType.GetMethod("Save", BindingFlags.Instance | BindingFlags.Public);
                var restore = modConfigType.GetMethod("RestoreDefaultsNoConfirm",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var baseLibLabel = modConfigType.GetMethod("GetBaseLibLabelText",
                    BindingFlags.Static | BindingFlags.NonPublic,
                    null, [typeof(string)], null);
                if (configPropsField == null || getLabel == null || changed == null || save == null || restore == null)
                {
                    _pagesRegistered = true;
                    return 0;
                }

                var sectionAttrType = ResolveType(ConfigSectionAttributeName);
                var hideUiAttrType = ResolveType(ConfigHideInUiAttributeName);
                var buttonAttrType = ResolveType(ConfigButtonAttributeName);
                var configSliderType = ResolveType(ConfigSliderAttributeName);
                var sliderRangeType = ResolveType(SliderRangeAttributeName);
                var sliderFormatType = ResolveType(SliderLabelFormatAttributeName);
                var textInputAttrType = ResolveType(ConfigTextInputAttributeName);
                var colorPickerAttrType = ResolveType(ConfigColorPickerAttributeName);
                var hoverTipAttrType = ResolveType(ConfigHoverTipAttributeName);
                var configHoverTipsByDefaultAttrType = ResolveType(ConfigHoverTipsByDefaultAttributeName);
                var hoverTipsByDefaultAttrType = ResolveType(HoverTipsByDefaultAttributeName);
                var visibleIfAttrType = ResolveType(ConfigVisibleIfAttributeName);

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

                    var configConcreteType = config.GetType();
                    if (!ModSettingsMirrorInteropPolicy.ShouldMirror(ModSettingsMirrorSource.BaseLib, modId,
                            configConcreteType))
                        continue;

                    if (configPropsField.GetValue(config) is not List<PropertyInfo> configProps ||
                        configProps.Count == 0)
                        continue;

                    var host = new BaseLibMirrorHost(config, changed, save, restore, getLabel, baseLibLabel);
                    var page = BaseLibMirrorMapper.TryCreatePage(modId, pageId, sortOrder, pageTitle, pageDescription,
                        host,
                        configProps, sectionAttrType, hideUiAttrType, buttonAttrType, configSliderType, sliderRangeType,
                        sliderFormatType, textInputAttrType, colorPickerAttrType, hoverTipAttrType,
                        configHoverTipsByDefaultAttrType, hoverTipsByDefaultAttrType, visibleIfAttrType,
                        configConcreteType, modConfigType);
                    if (page == null)
                        continue;

                    if (!ModSettingsMirrorRegistrar.TryRegister(page))
                        continue;

                    count++;
                }

                if (count > 0)
                    _pagesRegistered = true;

                return count;
            }
        }

        internal static Type? ResolveType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? type = null;
                try
                {
                    type = assembly.GetType(fullName, false);
                }
                catch
                {
                    // ignored
                }

                if (type != null)
                    return type;
            }

            return null;
        }
    }
}
