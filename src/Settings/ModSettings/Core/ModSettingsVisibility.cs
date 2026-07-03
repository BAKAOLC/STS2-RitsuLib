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
                section.Entries.All(entry => entry.VisibilityPredicate == null &&
                                             entry is not SubpageModSettingsEntryDefinition))
                return null;

            return () => IsSectionVisible(page, section);
        }

        internal static bool SafePredicate(Func<bool>? predicate)
        {
            if (predicate == null)
                return true;

            try
            {
                return predicate();
            }
            catch
            {
                return true;
            }
        }

        private static bool IsPageVisible(ModSettingsPage page, HashSet<string> visitingPages)
        {
            var pageKey = CreatePageKey(page.ModId, page.Id);
            if (!visitingPages.Add(pageKey))
                return true;

            try
            {
                return ModSettingsHostSurfaceResolver.IsVisibleOnCurrentHost(page.VisibleOnHostSurfaces) &&
                       SafePredicate(page.VisibleWhen) &&
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
                   SafePredicate(section.VisibleWhen) &&
                   section.Entries.Any(entry => IsEntryVisible(page, entry, visitingPages));
        }

        private static bool IsEntryVisible(ModSettingsPage page, ModSettingsEntryDefinition entry,
            HashSet<string> visitingPages)
        {
            if (!SafePredicate(entry.VisibilityPredicate))
                return false;

            return entry is not SubpageModSettingsEntryDefinition subpage ||
                   IsTargetPageVisible(page.ModId, subpage.TargetPageId, visitingPages);
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
