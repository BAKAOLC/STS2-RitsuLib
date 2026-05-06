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
    }
}
