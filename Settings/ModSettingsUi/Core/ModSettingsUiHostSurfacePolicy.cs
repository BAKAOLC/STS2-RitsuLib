using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsUiHostSurfacePolicy
    {
        public static ModSettingsHostSurface MergeReadOnlyMask(ModSettingsPage? page, ModSettingsSection? section)
        {
            var mask = ModSettingsHostSurface.None;
            if (page != null)
                mask |= page.ReadOnlyOnHostSurfaces;
            if (section != null)
                mask |= section.ReadOnlyOnHostSurfaces;
            return mask;
        }

        public static string ResolveScopeChipText(IModSettingsBinding binding)
        {
            return binding switch
            {
                ITransientModSettingsBinding => ModSettingsLocalization.Get("scope.transient",
                    "Preview only - not persisted"),
                IRunSidecarModSettingsBinding => ModSettingsLocalization.Get("scope.runSidecar", "Run sidecar"),
                IModSettingsBindingSemantics { Semantics: ModSettingsValueSemantics.RunSnapshot } =>
                    ModSettingsLocalization.Get("scope.runSnapshot", "Run snapshot"),
                IModSettingsBindingSemantics { Semantics: ModSettingsValueSemantics.SessionCombat } =>
                    ModSettingsLocalization.Get("scope.sessionCombat", "Combat/session only"),
                _ => binding.Scope == SaveScope.Profile
                    ? ModSettingsLocalization.Get("scope.profile", "Stored per profile")
                    : ModSettingsLocalization.Get("scope.global", "Stored globally"),
            };
        }
    }
}
