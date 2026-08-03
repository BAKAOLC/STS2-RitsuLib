namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsInteractionPolicy
    {
        public static bool CanMutateAnyEntry(ModSettingsPage page)
        {
            return page.Sections.Any(section => section.Entries.Any(entry => CanMutateEntry(page, section, entry)));
        }

        public static bool CanMutateAnyEntry(ModSettingsPage page, ModSettingsSection section)
        {
            return section.Entries.Any(entry => CanMutateEntry(page, section, entry));
        }

        public static bool CanMutateEntry(ModSettingsPage page, ModSettingsSection section,
            ModSettingsEntryDefinition entry)
        {
            return ModSettingsHostSurfaceResolver.IsVisibleOnCurrentHost(page.VisibleOnHostSurfaces) &&
                   ModSettingsPredicate.Evaluate(page.VisibleWhen) &&
                   ModSettingsHostSurfaceResolver.IsVisibleOnCurrentHost(section.VisibleOnHostSurfaces) &&
                   ModSettingsPredicate.Evaluate(section.VisibleWhen) &&
                   ModSettingsVisibility.IsEntryVisible(page, entry) &&
                   ModSettingsPredicate.Evaluate(page.EnabledWhen) &&
                   ModSettingsPredicate.Evaluate(section.EnabledWhen) &&
                   ModSettingsPredicate.Evaluate(entry.EnabledPredicate) &&
                   !ModSettingsHostSurfaceResolver.IsReadOnlyOnCurrentHost(
                       ModSettingsUiHostSurfacePolicy.MergeReadOnlyMask(page, section, entry));
        }
    }
}
