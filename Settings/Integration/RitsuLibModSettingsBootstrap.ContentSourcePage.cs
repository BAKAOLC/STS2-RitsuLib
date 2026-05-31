using Godot;
using STS2RitsuLib.Data;

namespace STS2RitsuLib.Settings
{
    internal static partial class RitsuLibModSettingsBootstrap
    {
        private static void RegisterContentSourceHoverTipsPage(RitsuLibModSettingsUiBindings ui)
        {
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .AsChildOf(Const.ModId)
                    .WithSidebarVisibleOnlyWhenActive()
                    .WithSortOrder(-900)
                    .WithTitle(T("ritsulib.page.contentSourceHoverTips.title", "Content source display"))
                    .WithDescription(T("ritsulib.page.contentSourceHoverTips.description",
                        "Configure where RitsuLib shows the mod that provided game content."))
                    .AddSection("content_source_behavior", section => section
                        .WithTitle(T("ritsulib.section.contentSourceBehavior.title", "Behavior"))
                        .AddToggle(
                            "mod_source_hover_tips_enabled",
                            T("ritsulib.modSourceHoverTips.enabled.label", "Show content source hover tips"),
                            ui.ModSourceHoverTipsEnabled,
                            T("ritsulib.modSourceHoverTips.enabled.description",
                                "Shows which mod provides cards, relics, potions, events, keywords, and other model hover tips."))
                        .AddToggle(
                            "mod_source_hover_tips_include_vanilla",
                            T("ritsulib.modSourceHoverTips.includeVanilla.label", "Include vanilla content source"),
                            ui.ModSourceHoverTipsIncludeVanilla,
                            T("ritsulib.modSourceHoverTips.includeVanilla.description",
                                "Also shows source hover tips for base-game content."),
                            RitsuLibSettingsStore.IsModSourceHoverTipsEnabled)
                        .AddToggle(
                            "mod_source_hover_tips_include_non_details",
                            T("ritsulib.modSourceHoverTips.includeNonDetails.label", "Show sources outside details"),
                            ui.ModSourceHoverTipsIncludeNonDetails,
                            T("ritsulib.modSourceHoverTips.includeNonDetails.description",
                                "Also adds source tips to card hovers, card-preview hovers, and relic-option hovers outside inspect/detail screens."),
                            RitsuLibSettingsStore.IsModSourceHoverTipsEnabled))
                    .AddSection("content_source_groups", section => section
                        .WithTitle(T("ritsulib.section.contentSourceGroups.title", "Content groups"))
                        .WithDescription(T("ritsulib.section.contentSourceGroups.description",
                            "Turn source tips on or off for each content group."))
                        .WithVisibleWhen(RitsuLibSettingsStore.IsModSourceHoverTipsEnabled)
                        .AddCustom(
                            "content_source_group_toggles",
                            T("ritsulib.modSourceHoverTips.groups.label", "Groups"),
                            host => CreateContentSourceGroupToggles(ui, host))),
                "content-source-hover-tips");
        }

        private static Control CreateContentSourceGroupToggles(
            RitsuLibModSettingsUiBindings ui,
            IModSettingsUiActionHost host)
        {
            var grid = new GridContainer
            {
                Columns = 3,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            grid.AddThemeConstantOverride("h_separation", 8);
            grid.AddThemeConstantOverride("v_separation", 8);

            AddToggle("cards", "Cards", ui.ModSourceHoverTipsCards);
            AddToggle("relics", "Relics", ui.ModSourceHoverTipsRelics);
            AddToggle("potions", "Potions", ui.ModSourceHoverTipsPotions);
            AddToggle("powers", "Powers", ui.ModSourceHoverTipsPowers);
            AddToggle("orbs", "Orbs", ui.ModSourceHoverTipsOrbs);
            AddToggle("enchantments", "Enchantments", ui.ModSourceHoverTipsEnchantments);
            AddToggle("afflictions", "Afflictions", ui.ModSourceHoverTipsAfflictions);
            AddToggle("keywords", "Keywords", ui.ModSourceHoverTipsKeywords);
            AddToggle("events", "Events", ui.ModSourceHoverTipsEvents);
            AddToggle("creatures", "Creatures", ui.ModSourceHoverTipsCreatures);
            AddToggle("gameTerms", "Game terms", ui.ModSourceHoverTipsGameTerms);
            return grid;

            void AddToggle(string key, string fallback, IModSettingsValueBinding<bool> binding)
            {
                var toggle = ModSettingsUiControlTheming.CreateCompactStateToggle(binding.Read(), value =>
                {
                    binding.Write(value);
                    host.MarkDirty(binding);
                    host.RequestRefresh();
                });
                grid.AddChild(ModSettingsUiControlTheming.CreateCompactToggleField(
                    L($"ritsulib.modSourceHoverTips.group.{key}", fallback),
                    toggle));
            }
        }
    }
}
