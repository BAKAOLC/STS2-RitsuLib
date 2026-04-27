using System.Globalization;
using STS2RitsuLib.Diagnostics.CardExport;
using STS2RitsuLib.Diagnostics.CompendiumExport;

namespace STS2RitsuLib.Settings
{
    internal static partial class RitsuLibModSettingsBootstrap
    {
        private static void RegisterImagePngExportPage(RitsuLibModSettingsUiBindings ui)
        {
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .AsChildOf(Const.ModId)
                    .WithSortOrder(-200)
                    .WithTitle(T("ritsulib.page.imagePngExport.title", "Image PNG export (dev)"))
                    .WithDescription(T("ritsulib.page.imagePngExport.description",
                        "Batch export for library cards, relic inspection popups, and potion lab focus panels. Detail content uses unlocked / seen appearance; no save gating."))
                    .AddSection("image_export_cards", section => section
                        .WithTitle(T("ritsulib.section.imagePngExport.cards", "Cards"))
                        .Collapsible(true)
                        .AddString(
                            "card_png_export_output_path",
                            T("ritsulib.cardPngExport.path.label", "Output folder"),
                            ui.CardPngExportOutputPath,
                            T("ritsulib.cardPngExport.path.placeholder",
                                "Absolute path or user://… (e.g. user://ritsu_card_png)"),
                            1024)
                        .AddButton(
                            "card_png_export_browse",
                            T("ritsulib.cardPngExport.browse.label", "Choose output folder"),
                            T("ritsulib.cardPngExport.browse.button", "Browse…"),
                            host => ModSettingsOpenFolderDialog.Show(
                                ui.CardPngExportOutputPath,
                                host,
                                "CardPngExport",
                                "ritsulib.cardPngExport.browseTitle",
                                "Choose card PNG export folder"))
                        .AddToggle(
                            "card_png_export_include_hover",
                            T("ritsulib.cardPngExport.hover.label", "Include hover-tip panel"),
                            ui.CardPngExportIncludeHover)
                        .AddToggle(
                            "card_png_export_include_upgrades",
                            T("ritsulib.cardPngExport.upgrades.label", "Export upgraded variants"),
                            ui.CardPngExportIncludeUpgrades)
                        .AddToggle(
                            "card_png_export_include_hidden",
                            T("ritsulib.cardPngExport.hidden.label", "Include non-library cards"),
                            ui.CardPngExportIncludeHiddenFromLibrary)
                        .AddSlider(
                            "card_png_export_scale",
                            T("ritsulib.cardPngExport.scale.label", "Render scale"),
                            ui.CardPngExportScale,
                            0.25d,
                            4d,
                            0.25d,
                            v => v.ToString("0.##", CultureInfo.InvariantCulture))
                        .AddString(
                            "card_png_export_id_filter",
                            T("ritsulib.cardPngExport.filter.label", "Card id contains (optional)"),
                            ui.CardPngExportIdFilter,
                            T("ritsulib.cardPngExport.filter.placeholder", "Empty = all cards; e.g. WINE_"),
                            256,
                            T("ritsulib.cardPngExport.filter.description",
                                "Substring match, case-insensitive."))
                        .AddButton(
                            "card_png_export_start",
                            T("ritsulib.cardPngExport.start.label", "Start export"),
                            T("ritsulib.cardPngExport.start.button", "Start export"),
                            () => CardPngExportSettingsActions.TryBeginFromSettings(
                                ui.CardPngExportOutputPath,
                                ui.CardPngExportIncludeHover,
                                ui.CardPngExportIncludeUpgrades,
                                ui.CardPngExportScale,
                                ui.CardPngExportIdFilter,
                                ui.CardPngExportIncludeHiddenFromLibrary),
                            ModSettingsButtonTone.Accent))
                    .AddSection("image_export_relics", section => section
                        .WithTitle(T("ritsulib.section.imagePngExport.relics", "Relics"))
                        .Collapsible(true)
                        .AddString(
                            "relic_detail_png_export_output_path",
                            T("ritsulib.relicDetailPngExport.path.label", "Output folder"),
                            ui.RelicDetailPngExportOutputPath,
                            T("ritsulib.relicDetailPngExport.path.placeholder",
                                "Absolute path or user://… (e.g. user://ritsu_relic_png)"),
                            1024)
                        .AddButton(
                            "relic_detail_png_export_browse",
                            T("ritsulib.relicDetailPngExport.browse.label", "Choose output folder"),
                            T("ritsulib.relicDetailPngExport.browse.button", "Browse…"),
                            host => ModSettingsOpenFolderDialog.Show(
                                ui.RelicDetailPngExportOutputPath,
                                host,
                                "RelicDetailPngExport",
                                "ritsulib.relicDetailPngExport.browseTitle",
                                "Choose relic detail PNG export folder"))
                        .AddSlider(
                            "relic_detail_png_export_scale",
                            T("ritsulib.relicDetailPngExport.scale.label", "Render scale"),
                            ui.RelicDetailPngExportScale,
                            0.25d,
                            4d,
                            0.25d,
                            v => v.ToString("0.##", CultureInfo.InvariantCulture))
                        .AddString(
                            "relic_detail_png_export_id_filter",
                            T("ritsulib.relicDetailPngExport.filter.label", "Relic id contains (optional)"),
                            ui.RelicDetailPngExportIdFilter,
                            T("ritsulib.relicDetailPngExport.filter.placeholder", "Empty = all; e.g. CIRCLET"),
                            256,
                            T("ritsulib.relicDetailPngExport.filter.description",
                                "Substring on <c>ModelId.Entry</c>, case-insensitive."))
                        .AddToggle(
                            "relic_detail_png_export_include_hover",
                            T("ritsulib.relicDetailPngExport.includeHover.label", "Include hover-tip panel"),
                            ui.RelicDetailPngExportIncludeHover,
                            T("ritsulib.relicDetailPngExport.includeHover.description",
                                "When true, export layout includes a right-hand hover-tip style column."))
                        .AddButton(
                            "relic_detail_png_export_start",
                            T("ritsulib.relicDetailPngExport.start.label", "Start relic export"),
                            T("ritsulib.relicDetailPngExport.start.button", "Start export"),
                            () => CompendiumPngExportSettingsActions.TryBeginRelicDetailFromSettings(
                                ui.RelicDetailPngExportOutputPath,
                                ui.RelicDetailPngExportScale,
                                ui.RelicDetailPngExportIdFilter,
                                ui.RelicDetailPngExportIncludeHover),
                            ModSettingsButtonTone.Accent))
                    .AddSection("image_export_potions", section => section
                        .WithTitle(T("ritsulib.section.imagePngExport.potions", "Potions"))
                        .Collapsible(true)
                        .AddString(
                            "potion_detail_png_export_output_path",
                            T("ritsulib.potionDetailPngExport.path.label", "Output folder"),
                            ui.PotionDetailPngExportOutputPath,
                            T("ritsulib.potionDetailPngExport.path.placeholder",
                                "Absolute path or user://… (e.g. user://ritsu_potion_png)"),
                            1024)
                        .AddButton(
                            "potion_detail_png_export_browse",
                            T("ritsulib.potionDetailPngExport.browse.label", "Choose output folder"),
                            T("ritsulib.potionDetailPngExport.browse.button", "Browse…"),
                            host => ModSettingsOpenFolderDialog.Show(
                                ui.PotionDetailPngExportOutputPath,
                                host,
                                "PotionDetailPngExport",
                                "ritsulib.potionDetailPngExport.browseTitle",
                                "Choose potion detail PNG export folder"))
                        .AddSlider(
                            "potion_detail_png_export_scale",
                            T("ritsulib.potionDetailPngExport.scale.label", "Render scale"),
                            ui.PotionDetailPngExportScale,
                            0.25d,
                            4d,
                            0.25d,
                            v => v.ToString("0.##", CultureInfo.InvariantCulture))
                        .AddString(
                            "potion_detail_png_export_id_filter",
                            T("ritsulib.potionDetailPngExport.filter.label", "Potion id contains (optional)"),
                            ui.PotionDetailPngExportIdFilter,
                            T("ritsulib.potionDetailPngExport.filter.placeholder", "Empty = all; e.g. STRENGTH"),
                            256,
                            T("ritsulib.potionDetailPngExport.filter.description",
                                "Substring on <c>ModelId.Entry</c>, case-insensitive."))
                        .AddButton(
                            "potion_detail_png_export_start",
                            T("ritsulib.potionDetailPngExport.start.label", "Start potion export"),
                            T("ritsulib.potionDetailPngExport.start.button", "Start export"),
                            () => CompendiumPngExportSettingsActions.TryBeginPotionDetailFromSettings(
                                ui.PotionDetailPngExportOutputPath,
                                ui.PotionDetailPngExportScale,
                                ui.PotionDetailPngExportIdFilter),
                            ModSettingsButtonTone.Accent)),
                "image-png-export");
        }
    }
}
