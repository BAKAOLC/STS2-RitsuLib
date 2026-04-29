namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsBindingSavePolicy
    {
        public static IModSettingsValueBinding<TValue> Apply<TValue>(
            IModSettingsValueBinding<TValue> binding,
            ModSettingsReflectionSavePolicy policy)
        {
            return policy switch
            {
                ModSettingsReflectionSavePolicy.Auto => new AutoSaveModSettingsValueBinding<TValue>(binding),
                ModSettingsReflectionSavePolicy.Manual => binding,
                _ => throw ModSettingsMirrorDiagnostics.InvalidConfig(
                    $"Unsupported save policy '{policy}'."),
            };
        }
    }
}
