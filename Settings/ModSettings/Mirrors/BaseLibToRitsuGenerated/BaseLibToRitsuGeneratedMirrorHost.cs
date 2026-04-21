using System.Reflection;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Settings
{
    internal sealed class BaseLibToRitsuGeneratedMirrorHost(
        object instance,
        MethodInfo changed,
        MethodInfo save,
        MethodInfo restore)
    {
        public object Instance { get; } = instance;

        public string ModPrefix => ResolveRootNamespace() is { Length: > 0 } root ? root.ToUpperInvariant() + "-" : "";

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
            var key = ModPrefix + StringHelper.Slugify(name) + ".title";
            return LocString.GetIfExists("settings_ui", key)?.GetFormattedText() ?? name;
        }

        public string ResolveBaseLibLabel(string name)
        {
            var key = "BASELIB-" + StringHelper.Slugify(name) + ".title";
            return LocString.GetIfExists("settings_ui", key)?.GetFormattedText() ?? name;
        }

        public ModSettingsText ResolveModDisplayNameText(string fallback)
        {
            return ModSettingsText.Dynamic(() =>
            {
                var root = ResolveRootNamespace();
                if (!string.IsNullOrWhiteSpace(root))
                {
                    var key = root.ToUpperInvariant() + ".mod_title";
                    var localized = LocString.GetIfExists("settings_ui", key)?.GetFormattedText();
                    if (!string.IsNullOrWhiteSpace(localized))
                        return localized;
                }

                var manifestName = Sts2ModManagerCompat.EnumerateModsForManifestLookup()
                    .FirstOrDefault(mod =>
                        string.Equals(mod.manifest?.id, fallback, StringComparison.OrdinalIgnoreCase))
                    ?.manifest?.name;
                if (!string.IsNullOrWhiteSpace(manifestName))
                    return manifestName;

                return !string.IsNullOrWhiteSpace(root) ? root : fallback;
            });
        }

        private string ResolveRootNamespace()
        {
            var type = Instance.GetType();
            if (!string.IsNullOrWhiteSpace(type.Namespace))
            {
                var dot = type.Namespace.IndexOf('.');
                return dot < 0 ? type.Namespace : type.Namespace[..dot];
            }

            var assemblyName = type.Assembly.GetName().Name ?? "";
            var assemblyDot = assemblyName.IndexOf('.');
            return assemblyDot < 0 ? assemblyName : assemblyName[..assemblyDot];
        }
    }
}
