using System.Text.Json;
using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Builds a <see cref="RitsuShellTheme" /> snapshot from a merged + reference-resolved DTFM token tree.
    ///     Reads concrete values at canonical paths to populate typed records.
    /// </summary>
    internal static class RitsuShellThemeBuilder
    {
        /// <summary>
        ///     Builds a snapshot for <paramref name="resolvedId" /> from the merged tree.
        /// </summary>
        public static RitsuShellTheme Build(string resolvedId,
            Dictionary<string, object?> root,
            Dictionary<string, JsonElement> extensions)
        {
            var color = BuildColorTokens(root);
            var text = BuildTextTokens(root);
            var surface = BuildSurfaceTokens(root);
            var component = BuildComponentTokens(root);
            var metric = BuildMetricTokens(root);
            var font = BuildFontTokens(root);
            return new(resolvedId, root, color, text, surface, component, metric, font, extensions);
        }

        private static ColorTokens BuildColorTokens(Dictionary<string, object?> root)
        {
            return new(
                ReadColor(root, "semantic.color.palette.white"),
                ReadColor(root, "semantic.color.palette.transparent"),
                ReadColor(root, "semantic.color.palette.divider"),
                ReadColor(root, "semantic.color.palette.unsetPreview"),
                ReadColor(root, "semantic.color.palette.modalBackdrop"),
                new(ReadColor(root, "semantic.color.shadow.ambient")));
        }

        private static TextTokens BuildTextTokens(Dictionary<string, object?> root)
        {
            return new(
                ReadColor(root, "semantic.color.text.richTitle"),
                ReadColor(root, "semantic.color.text.richBody"),
                ReadColor(root, "semantic.color.text.richSecondary"),
                ReadColor(root, "semantic.color.text.richMuted"),
                ReadColor(root, "semantic.color.text.labelPrimary"),
                ReadColor(root, "semantic.color.text.labelSecondary"),
                ReadColor(root, "semantic.color.text.sidebarSection"),
                ReadColor(root, "semantic.color.text.hoverHighlight"),
                ReadColor(root, "semantic.color.text.number"),
                ReadColor(root, "semantic.color.text.grip"),
                ReadColor(root, "semantic.color.text.hint"),
                ReadColor(root, "semantic.color.text.dropdownRow"));
        }

        private static SurfaceTokens BuildSurfaceTokens(Dictionary<string, object?> root)
        {
            return new(
                ReadColor(root, "semantic.color.surface.sidebar"),
                ReadColor(root, "semantic.color.surface.content"),
                ReadColor(root, "semantic.color.surface.contentBuildOverlay"),
                new(ReadColor(root, "semantic.color.surface.entry.bg"),
                    ReadColor(root, "semantic.color.surface.entry.border"),
                    ReadColor(root, "semantic.color.surface.entry.shadow")),
                new(ReadColor(root, "semantic.color.surface.inset.bg"),
                    ReadColor(root, "semantic.color.surface.inset.border")),
                new(ReadColor(root, "semantic.color.surface.framed.border"),
                    ReadColor(root, "semantic.color.surface.framed.shadow")));
        }

        private static ComponentTokens BuildComponentTokens(Dictionary<string, object?> root)
        {
            return new(
                BuildSidebarCard(root),
                BuildChromeMenu(root),
                BuildPageToolbarTray(root),
                BuildListShell(root),
                BuildListItem(root),
                BuildListEditor(root),
                BuildPill(root),
                BuildToggle(root),
                BuildSlider(root),
                BuildDropdown(root),
                BuildStepper(root),
                BuildDragHandle(root),
                BuildCollapsible(root),
                BuildSidebarBtn(root),
                BuildSidebarRail(root),
                BuildTextButton(root),
                BuildStringValidation(root),
                BuildOverlayPanel(root),
                BuildChoiceCenter(root));
        }

        private static SidebarCardTokens BuildSidebarCard(Dictionary<string, object?> root)
        {
            return new(
                ReadBgBorder(root, "components.sidebarCard.default"),
                ReadBgBorder(root, "components.sidebarCard.selected"),
                ReadColor(root, "components.sidebarCard.shadow"));
        }

        private static ChromeMenuTokens BuildChromeMenu(Dictionary<string, object?> root)
        {
            return new(
                ReadBgBorder(root, "components.chromeMenu.default"),
                ReadBgBorder(root, "components.chromeMenu.hover"));
        }

        private static PageToolbarTrayTokens BuildPageToolbarTray(Dictionary<string, object?> root)
        {
            return new(
                ReadColor(root, "components.pageToolbarTray.bg"),
                ReadColor(root, "components.pageToolbarTray.border"));
        }

        private static ListShellTokens BuildListShell(Dictionary<string, object?> root)
        {
            return new(
                ReadColor(root, "components.listShell.bg"),
                ReadColor(root, "components.listShell.border"),
                ReadColor(root, "components.listShell.shadow"));
        }

        private static ListItemTokens BuildListItem(Dictionary<string, object?> root)
        {
            return new(
                ReadBgBorder(root, "components.listItem.default"),
                ReadBgBorder(root, "components.listItem.accent"),
                ReadColor(root, "components.listItem.shadow"));
        }

        private static ListEditorTokens BuildListEditor(Dictionary<string, object?> root)
        {
            return new(
                ReadColor(root, "components.listEditor.bg"),
                ReadColor(root, "components.listEditor.border"));
        }

        private static PillTokens BuildPill(Dictionary<string, object?> root)
        {
            return new(
                ReadBgBorder(root, "components.pill.default"),
                ReadBgBorder(root, "components.pill.hover"));
        }

        private static ToggleTokens BuildToggle(Dictionary<string, object?> root)
        {
            var on = ReadBgBorder(root, "components.toggle.on");
            var off = ReadBgBorder(root, "components.toggle.off");
            var offHoverBg = ReadColor(root, "components.toggle.offHover.bg");
            var offHoverBorder = TryReadColor(root, "components.toggle.offHover.border", out var b) ? b : off.Border;
            var disabled = ReadBgBorder(root, "components.toggle.disabled");
            return new(on, off, new(offHoverBg, offHoverBorder), disabled,
                ReadColor(root, "components.toggle.shadow"));
        }

        private static SliderTokens BuildSlider(Dictionary<string, object?> root)
        {
            return new(
                ReadColor(root, "components.slider.grab.highlight"),
                ReadColor(root, "components.slider.grab.shadow"));
        }

        private static DropdownTokens BuildDropdown(Dictionary<string, object?> root)
        {
            var open = ReadBgBorder(root, "components.dropdown.open");
            var hoverBg = ReadColor(root, "components.dropdown.hover.bg");
            var hoverBorder = TryReadColor(root, "components.dropdown.hover.border", out var hb) ? hb : open.Border;
            var pressedBg = ReadColor(root, "components.dropdown.pressed.bg");
            var pressedBorder = TryReadColor(root, "components.dropdown.pressed.border", out var pb) ? pb : open.Border;
            var focus = ReadBgBorder(root, "components.dropdown.focus");
            return new(open, new(hoverBg, hoverBorder), new(pressedBg, pressedBorder), focus);
        }

        private static StepperTokens BuildStepper(Dictionary<string, object?> root)
        {
            return new(
                ReadBgBorder(root, "components.stepper.default"),
                ReadBgBorder(root, "components.stepper.hover"),
                ReadBgBorder(root, "components.stepper.neutral"));
        }

        private static DragHandleTokens BuildDragHandle(Dictionary<string, object?> root)
        {
            return new(
                ReadBgBorder(root, "components.dragHandle.default"),
                ReadBgBorder(root, "components.dragHandle.selected"));
        }

        private static CollapsibleTokens BuildCollapsible(Dictionary<string, object?> root)
        {
            return new(
                ReadBgBorder(root, "components.collapsible.default"),
                ReadBgBorder(root, "components.collapsible.hover"),
                ReadBgBorder(root, "components.collapsible.selected"),
                ReadBgBorderShared(root, "components.collapsible.disabled", "components.collapsible.default"));
        }

        private static SidebarBtnTokens BuildSidebarBtn(Dictionary<string, object?> root)
        {
            return new(
                ReadBgBorder(root, "components.sidebarBtn.default"),
                ReadBgBorder(root, "components.sidebarBtn.hover"),
                ReadBgBorder(root, "components.sidebarBtn.selected"),
                ReadBgBorder(root, "components.sidebarBtn.selectedHover"),
                ReadBgBorderShared(root, "components.sidebarBtn.utilitySelected", "components.sidebarBtn.selected"),
                ReadBgBorderShared(root, "components.sidebarBtn.idleDeep", "components.sidebarBtn.deep"),
                ReadBgBorderShared(root, "components.sidebarBtn.idleDeepHover", "components.sidebarBtn.deepHover"),
                ReadBgBorderShared(root, "components.sidebarBtn.idleDeeper", "components.sidebarBtn.deep"),
                ReadBgBorderShared(root, "components.sidebarBtn.idleDeeperHover", "components.sidebarBtn.deepHover"),
                ReadBgBorderShared(root, "components.sidebarBtn.mod", "components.sidebarBtn.default"),
                ReadBgBorderShared(root, "components.sidebarBtn.modHover", "components.sidebarBtn.hover"),
                ReadBgBorderShared(root, "components.sidebarBtn.modDeep", "components.sidebarBtn.deep"),
                ReadColor(root, "components.sidebarBtn.deep.border"),
                ReadColor(root, "components.sidebarBtn.deepHover.border"),
                ReadColor(root, "components.sidebarBtn.shadow"));
        }

        private static SidebarRailTokens BuildSidebarRail(Dictionary<string, object?> root)
        {
            return new(
                ReadColor(root, "components.sidebarRail.bg"),
                ReadColor(root, "components.sidebarRail.border"));
        }

        private static TextButtonTokens BuildTextButton(Dictionary<string, object?> root)
        {
            return new(
                BuildTextButtonTone(root, "components.textButton.accent"),
                BuildTextButtonTone(root, "components.textButton.danger"),
                BuildTextButtonTone(root, "components.textButton.neutral"));
        }

        private static TextButtonToneTokens BuildTextButtonTone(Dictionary<string, object?> root, string basePath)
        {
            return new(
                ReadColor(root, basePath + ".fg"),
                ReadColor(root, basePath + ".bg"),
                ReadColor(root, basePath + ".bgHover"));
        }

        private static StringValidationTokens BuildStringValidation(Dictionary<string, object?> root)
        {
            return new(
                ReadBgBorder(root, "components.stringValidation.neutral"),
                ReadBgBorder(root, "components.stringValidation.invalid"));
        }

        private static OverlayPanelTokens BuildOverlayPanel(Dictionary<string, object?> root)
        {
            return new(
                ReadColor(root, "components.overlayPanel.bg"),
                ReadColor(root, "components.overlayPanel.border"));
        }

        private static ChoiceCenterTokens BuildChoiceCenter(Dictionary<string, object?> root)
        {
            return new(
                ReadColor(root, "components.choiceCenter.highlight.top"),
                ReadColor(root, "components.choiceCenter.highlight.bottom"));
        }

        private static FontSizeMetrics BuildFontSizeMetrics(Dictionary<string, object?> root)
        {
            var tooltipFont = ReadInt(root, "semantic.fontSize.tooltip");
            if (tooltipFont <= 0)
                tooltipFont = ReadInt(root, "semantic.fontSize.popupRow");

            return new(
                ReadInt(root, "semantic.fontSize.button"),
                ReadInt(root, "semantic.fontSize.miniButton"),
                ReadInt(root, "semantic.fontSize.valueLabel"),
                ReadInt(root, "semantic.fontSize.popupRow"),
                ReadInt(root, "semantic.fontSize.hintSmall"),
                tooltipFont,
                ReadInt(root, "semantic.fontSize.grip"),
                ReadInt(root, "semantic.fontSize.pillCount"),
                ReadInt(root, "semantic.fontSize.secondary"),
                ReadInt(root, "semantic.fontSize.headerArrow"),
                ReadInt(root, "semantic.fontSize.headerTitle"),
                ReadInt(root, "semantic.fontSize.headerSubtitle"),
                ReadInt(root, "semantic.fontSize.pageDescription"),
                ReadInt(root, "semantic.fontSize.overlayTitle"),
                ReadInt(root, "semantic.fontSize.overlayBody"),
                ReadInt(root, "semantic.fontSize.overlayPath"),
                ReadInt(root, "semantic.fontSize.settingsEntryButton"),
                ReadInt(root, "semantic.fontSize.settingLineTitle"));
        }

        private static MetricTokens BuildMetricTokens(Dictionary<string, object?> root)
        {
            return new(
                new(ReadInt(root, "semantic.metrics.radius.default"),
                    ReadInt(root, "semantic.metrics.radius.validation"),
                    ReadInt(root, "semantic.metrics.radius.overlay")),
                new(ReadInt(root, "semantic.metrics.borderWidth.thin"),
                    ReadInt(root, "semantic.metrics.borderWidth.normal"),
                    ReadInt(root, "semantic.metrics.borderWidth.thick"),
                    ReadInt(root, "semantic.metrics.borderWidth.overlay")),
                new(ReadFloat(root, "components.entry.layout.valueMinWidth"),
                    ReadFloat(root, "components.entry.layout.valueMinHeight"),
                    ReadInt(root, "components.entry.layout.miniStepperButtonSize")),
                new(ReadFloat(root, "components.slider.layout.rowMinWidth"),
                    ReadFloat(root, "components.slider.layout.trackMinWidth"),
                    ReadFloat(root, "components.slider.layout.valueFieldWidth"),
                    ReadFloat(root, "components.slider.layout.valueFieldHeight")),
                new(ReadFloat(root, "components.choice.layout.rowMinWidth"),
                    ReadFloat(root, "components.choice.layout.centerMinWidth")),
                new(ReadFloat(root, "components.color.layout.rowMinWidth"),
                    ReadFloat(root, "components.color.layout.swatchSize")),
                new(ReadFloat(root, "components.stringEntry.layout.minWidth"),
                    ReadFloat(root, "components.stringEntry.layout.multilineMinHeight")),
                new(ReadFloat(root, "components.keybinding.layout.blockWidth"),
                    ReadFloat(root, "components.keybinding.layout.captureMinWidth"),
                    ReadInt(root, "components.keybinding.layout.hintFontSize")),
                new(ReadInt(root, "components.overlayPanel.layout.paddingH"),
                    ReadInt(root, "components.overlayPanel.layout.paddingV")),
                new(ReadFloat(root, "components.sidebar.layout.width"),
                    ReadFloat(root, "components.sidebar.layout.pageRowMinHeight"),
                    ReadFloat(root, "components.sidebar.layout.sectionRowMinHeight"),
                    ReadInt(root, "components.sidebar.layout.modListSeparation"),
                    ReadInt(root, "components.sidebar.layout.modCardInnerSeparation"),
                    ReadInt(root, "components.sidebar.layout.pageTreeSeparation"),
                    ReadInt(root, "components.sidebar.layout.sectionRailSeparation"),
                    ReadInt(root, "components.sidebar.layout.cardInnerMargin"),
                    ReadBool(root, "components.sidebar.layout.showInlinePageCount")),
                BuildFontSizeMetrics(root));
        }

        private static FontTokens BuildFontTokens(Dictionary<string, object?> root)
        {
            return new(
                ReadFont(root, "semantic.fontFamily.body"),
                ReadFont(root, "semantic.fontFamily.bodyBold"),
                ReadFont(root, "semantic.fontFamily.button"));
        }

        private static Color ReadColor(Dictionary<string, object?> root, string path)
        {
            if (RitsuShellThemeReferenceResolver.TryFindLeaf(root, path, out var leaf) &&
                RitsuShellThemeValueCoerce.TryAsColor(leaf, out var color))
                return color;
            return Colors.Magenta;
        }

        private static bool TryReadColor(Dictionary<string, object?> root, string path, out Color color)
        {
            color = Colors.Transparent;
            return RitsuShellThemeReferenceResolver.TryFindLeaf(root, path, out var leaf) &&
                   RitsuShellThemeValueCoerce.TryAsColor(leaf, out color);
        }

        private static BgBorder ReadBgBorder(Dictionary<string, object?> root, string basePath)
        {
            return new(ReadColor(root, basePath + ".bg"), ReadColor(root, basePath + ".border"));
        }

        private static BgBorder ReadBgBorderShared(Dictionary<string, object?> root, string basePath,
            string borderFallbackBasePath)
        {
            var bg = ReadColor(root, basePath + ".bg");
            var border = TryReadColor(root, basePath + ".border", out var b)
                ? b
                : ReadColor(root, borderFallbackBasePath + ".border");
            return new(bg, border);
        }

        private static int ReadInt(Dictionary<string, object?> root, string path)
        {
            if (RitsuShellThemeReferenceResolver.TryFindLeaf(root, path, out var leaf) &&
                RitsuShellThemeValueCoerce.TryAsDouble(leaf, out var v))
                return (int)Math.Round(v, MidpointRounding.AwayFromZero);
            return 0;
        }

        private static float ReadFloat(Dictionary<string, object?> root, string path)
        {
            if (RitsuShellThemeReferenceResolver.TryFindLeaf(root, path, out var leaf) &&
                RitsuShellThemeValueCoerce.TryAsDouble(leaf, out var v))
                return (float)v;
            return 0f;
        }

        private static bool ReadBool(Dictionary<string, object?> root, string path)
        {
            if (RitsuShellThemeReferenceResolver.TryFindLeaf(root, path, out var leaf) &&
                RitsuShellThemeValueCoerce.TryAsBool(leaf, out var v))
                return v;
            return false;
        }

        private static Font ReadFont(Dictionary<string, object?> root, string path)
        {
            RitsuShellThemeReferenceResolver.TryFindLeaf(root, path, out var leaf);
            return RitsuShellThemeValueCoerce.AsFont(leaf);
        }
    }
}
