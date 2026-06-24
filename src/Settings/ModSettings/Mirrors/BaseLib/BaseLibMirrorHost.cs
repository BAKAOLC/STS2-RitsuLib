using System.Reflection;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Settings
{
    internal sealed class BaseLibMirrorHost(
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
            return baseLibLabel != null ? (string)baseLibLabel.Invoke(null, [name])! : name;
        }

        public string GetModPrefix()
        {
            var property = Instance.GetType().GetProperty("ModPrefix", BindingFlags.Public | BindingFlags.Instance);
            return property?.GetValue(Instance) as string ?? "";
        }

        public ModSettingsText ResolveModDisplayNameText(string modId)
        {
            return ModSettingsText.Dynamic(() =>
            {
                var prefix = GetModPrefix();
                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    var locKey = prefix[..^1] + ".mod_title";
                    var localized = LocString.GetIfExists("settings_ui", locKey)?.GetFormattedText();
                    if (!string.IsNullOrWhiteSpace(localized))
                        return localized;
                }

                var manifestName = Sts2ModManagerCompat.EnumerateModsForManifestLookup()
                    .FirstOrDefault(mod => string.Equals(mod.manifest?.id, modId, StringComparison.OrdinalIgnoreCase))
                    ?.manifest?.name;
                if (!string.IsNullOrWhiteSpace(manifestName))
                    return manifestName;

                var type = Instance.GetType();
                if (!string.IsNullOrWhiteSpace(type.Namespace))
                {
                    var dot = type.Namespace.IndexOf('.');
                    var root = dot < 0 ? type.Namespace : type.Namespace[..dot];
                    if (!string.IsNullOrWhiteSpace(root))
                        return root;
                }

                var assemblyName = type.Assembly.GetName().Name ?? "";
                return !string.IsNullOrWhiteSpace(assemblyName) ? assemblyName : modId;
            });
        }
    }
}
