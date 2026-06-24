namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsUiHostSurfacePolicy
    {
        public static ModSettingsHostSurface MergeReadOnlyMask(ModSettingsPage? page, ModSettingsSection? section,
            ModSettingsEntryDefinition? entry)
        {
            var mask = ModSettingsHostSurface.None;
            if (page != null)
                mask |= page.ReadOnlyOnHostSurfaces;
            if (section != null)
                mask |= section.ReadOnlyOnHostSurfaces;
            if (entry != null)
                mask |= entry.ReadOnlyOnHostSurfaces;
            return mask;
        }
    }
}
