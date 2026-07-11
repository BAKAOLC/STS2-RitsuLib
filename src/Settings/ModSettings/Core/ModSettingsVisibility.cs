namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsVisibility
    {
        internal static bool IsPageVisible(ModSettingsPage page)
        {
            return IsPageVisible(page, []);
        }

        internal static bool IsSectionVisible(ModSettingsPage page, ModSettingsSection section)
        {
            return IsSectionVisible(page, section, []);
        }

        internal static bool IsEntryVisible(ModSettingsPage page, ModSettingsEntryDefinition entry)
        {
            return IsEntryVisible(page, entry, []);
        }

        internal static Func<bool>? CreateSectionVisibilityPredicate(ModSettingsPage page, ModSettingsSection section)
        {
            if (section.VisibleWhen == null &&
                section.VisibleOnHostSurfaces == ModSettingsHostSurface.All &&
                section.Entries.Count > 0 &&
                section.Entries.All(entry => !RequiresDynamicEvaluation(entry)))
                return null;

            return () => IsSectionVisible(page, section);
        }

        private static bool IsPageVisible(ModSettingsPage page, HashSet<string> visitingPages)
        {
            var pageKey = CreatePageKey(page.ModId, page.Id);
            if (!visitingPages.Add(pageKey))
                return true;

            try
            {
                return ModSettingsHostSurfaceResolver.IsVisibleOnCurrentHost(page.VisibleOnHostSurfaces) &&
                       ModSettingsPredicate.Evaluate(page.VisibleWhen) &&
                       page.Sections.Any(section => IsSectionVisible(page, section, visitingPages));
            }
            finally
            {
                visitingPages.Remove(pageKey);
            }
        }

        private static bool IsSectionVisible(ModSettingsPage page, ModSettingsSection section,
            HashSet<string> visitingPages)
        {
            return ModSettingsHostSurfaceResolver.IsVisibleOnCurrentHost(section.VisibleOnHostSurfaces) &&
                   ModSettingsPredicate.Evaluate(section.VisibleWhen) &&
                   section.Entries.Any(entry => IsEntryVisible(page, entry, visitingPages));
        }

        private static bool IsEntryVisible(ModSettingsPage page, ModSettingsEntryDefinition entry,
            HashSet<string> visitingPages)
        {
            if (!ModSettingsPredicate.Evaluate(entry.VisibilityPredicate))
                return false;

            return entry.VisibilityTargetPageId is not { } targetPageId ||
                   IsTargetPageVisible(page.ModId, targetPageId, visitingPages);
        }

        internal static bool RequiresDynamicEvaluation(ModSettingsEntryDefinition entry)
        {
            return entry.VisibilityPredicate != null || entry.VisibilityTargetPageId != null;
        }

        private static bool IsTargetPageVisible(string modId, string pageId, HashSet<string> visitingPages)
        {
            return ModSettingsRegistry.GetPages().FirstOrDefault(page =>
                       string.Equals(page.ModId, modId, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(page.Id, pageId, StringComparison.OrdinalIgnoreCase)) is not { } target ||
                   IsPageVisible(target, visitingPages);
        }

        private static string CreatePageKey(string modId, string pageId)
        {
            return $"{modId}::{pageId}";
        }
    }
}
