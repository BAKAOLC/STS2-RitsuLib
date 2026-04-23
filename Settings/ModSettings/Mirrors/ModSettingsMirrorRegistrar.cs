namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsMirrorRegistrar
    {
        public static bool TryRegister(ModSettingsMirrorPageDefinition page)
        {
            if (ModSettingsRegistry.TryGetPage(page.ModId, page.PageId, out _))
                return false;

            try
            {
                ModSettingsRegistry.Register(page.ModId, builder =>
                {
                    if (page.Title != null)
                        builder.WithTitle(page.Title);
                    if (page.Description != null)
                        builder.WithDescription(page.Description);
                    builder.WithSortOrder(page.SortOrder);
                    if (page.ModDisplayName != null)
                        builder.WithModDisplayName(page.ModDisplayName);
                    if (page.ModSidebarOrder is { } modSidebarOrder)
                        builder.WithModSidebarOrder(modSidebarOrder);
                    if (!string.IsNullOrWhiteSpace(page.ParentPageId))
                        builder.AsChildOf(page.ParentPageId!);

                    for (var i = 0; i < page.Sections.Count; i++)
                    {
                        var sectionDefinition = page.Sections[i];
                        var appendRestoreDefaults = i == page.Sections.Count - 1 ? page.RestoreDefaultsButton : null;
                        builder.AddSection(sectionDefinition.Id, section =>
                        {
                            if (sectionDefinition.Title != null)
                                section.WithTitle(sectionDefinition.Title);
                            if (sectionDefinition.Description != null)
                                section.WithDescription(sectionDefinition.Description);
                            if (sectionDefinition.IsCollapsible)
                                section.Collapsible(sectionDefinition.StartCollapsed);
                            if (sectionDefinition.VisibleWhen != null)
                                section.WithVisibleWhen(sectionDefinition.VisibleWhen);

                            foreach (var entry in sectionDefinition.Entries)
                                ModSettingsMirrorEntryAppender.Append(section, entry);

                            if (appendRestoreDefaults != null)
                                ModSettingsMirrorEntryAppender.AppendButton(section, appendRestoreDefaults);
                        });
                    }
                }, page.PageId);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
