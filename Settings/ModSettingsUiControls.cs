using System.Globalization;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;
using Array = System.Array;

namespace STS2RitsuLib.Settings
{
    internal sealed class ModSettingsUiContext(RitsuModSettingsSubmenu submenu)
    {
        public static string Resolve(ModSettingsText? text, string fallback = "")
        {
            return text?.Resolve() ?? fallback;
        }

        public static string ResolvePageTitle(ModSettingsPage page)
        {
            return ModSettingsLocalization.ResolvePageDisplayName(page);
        }

        public string? ResolvePageDescription(ModSettingsPage page)
        {
            var resolved = page.Description?.Resolve();
            if (!string.IsNullOrWhiteSpace(resolved))
                return resolved;

            return ModManager.AllMods
                .FirstOrDefault(mod => string.Equals(mod.manifest?.id, page.ModId, StringComparison.OrdinalIgnoreCase))
                ?.manifest?.description;
        }

        public string ComposeBindingDescription(ModSettingsText? description, IModSettingsBinding binding)
        {
            if (binding is ITransientModSettingsBinding)
            {
                var transientText = ModSettingsLocalization.Get("scope.transient", "Preview only - not persisted");
                var transientDescription = Resolve(description);
                return string.IsNullOrWhiteSpace(transientDescription)
                    ? transientText
                    : $"{transientDescription}  [color=#B9B09A]- {transientText}[/color]";
            }

            var scopeText = binding.Scope == SaveScope.Profile
                ? ModSettingsLocalization.Get("scope.profile", "Stored per profile")
                : ModSettingsLocalization.Get("scope.global", "Stored globally");

            var resolvedDescription = Resolve(description);
            return string.IsNullOrWhiteSpace(resolvedDescription)
                ? scopeText
                : $"{resolvedDescription}  [color=#B9B09A]- {scopeText}[/color]";
        }

        public void MarkDirty(IModSettingsBinding binding)
        {
            submenu.MarkDirty(binding);
        }

        public void RequestRefresh()
        {
            submenu.RequestRefresh();
        }

        public void RegisterRefresh(Action action)
        {
            submenu.RegisterRefreshAction(action);
        }

        public void NavigateToPage(string pageId)
        {
            submenu.NavigateToPage(pageId);
        }
    }

    internal static class ModSettingsUiFactory
    {
        private const float EntryControlWidth = 248f;
        private const float PageContentWidth = 0f;

        public static Control CreatePageContent(ModSettingsUiContext context, ModSettingsPage page)
        {
            var container = new VBoxContainer
            {
                Name = $"Page_{SanitizeName(page.ModId)}_{SanitizeName(page.Id)}",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };

            container.AddThemeConstantOverride("separation", 8);

            var pageDescription =
                CreateRefreshableDescriptionLabel(context, () => context.ResolvePageDescription(page) ?? string.Empty);
            container.AddChild(pageDescription);
            pageDescription.Visible = !string.IsNullOrWhiteSpace(pageDescription.Text);

            for (var index = 0; index < page.Sections.Count; index++)
            {
                var section = page.Sections[index];
                if (index > 0)
                    container.AddChild(CreateDivider());

                container.AddChild(CreateSection(context, section));
            }

            return container;
        }

        public static Control CreateToggleEntry(ModSettingsUiContext context, ToggleModSettingsEntryDefinition entry)
        {
            var control = new ModSettingsToggleControl(
                entry.Binding.Read(),
                value =>
                {
                    entry.Binding.Write(value);
                    context.MarkDirty(entry.Binding);
                    context.RequestRefresh();
                });
            context.RegisterRefresh(() => control.SetValue(entry.Binding.Read()));

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => context.ComposeBindingDescription(entry.Description, entry.Binding),
                control);
        }

        public static Control CreateSliderEntry(ModSettingsUiContext context, SliderModSettingsEntryDefinition entry)
        {
            var control = new ModSettingsSliderControl(
                entry.Binding.Read(),
                entry.MinValue,
                entry.MaxValue,
                entry.Step,
                FormatValue,
                value =>
                {
                    entry.Binding.Write(value);
                    context.MarkDirty(entry.Binding);
                    context.RequestRefresh();
                });
            context.RegisterRefresh(() => control.SetValue(entry.Binding.Read()));

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => context.ComposeBindingDescription(entry.Description, entry.Binding),
                control);

            string FormatValue(float value)
            {
                return entry.ValueFormatter?.Invoke(value) ?? value.ToString("0.##");
            }
        }

        public static Control CreateChoiceEntry<TValue>(ModSettingsUiContext context,
            ChoiceModSettingsEntryDefinition<TValue> entry)
        {
            var resolvedOptions = entry.Options
                .Select(option => (option.Value, Label: ModSettingsUiContext.Resolve(option.Label)))
                .ToArray();

            var control = new ModSettingsChoiceControl<TValue>(
                resolvedOptions,
                entry.Binding.Read(),
                value =>
                {
                    entry.Binding.Write(value);
                    context.MarkDirty(entry.Binding);
                    context.RequestRefresh();
                });
            context.RegisterRefresh(() => control.SetValue(entry.Binding.Read()));

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => context.ComposeBindingDescription(entry.Description, entry.Binding),
                control);
        }

        public static Control CreateButtonEntry(ModSettingsUiContext context, ButtonModSettingsEntryDefinition entry)
        {
            var control = new ModSettingsTextButton(
                ModSettingsUiContext.Resolve(entry.ButtonText),
                entry.Tone,
                () =>
                {
                    entry.Action();
                    context.RequestRefresh();
                });

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => ModSettingsUiContext.Resolve(entry.Description),
                control);
        }

        public static Control CreateHeaderEntry(ModSettingsUiContext context, HeaderModSettingsEntryDefinition entry)
        {
            var container = new VBoxContainer
            {
                CustomMinimumSize = new(0f, 48f),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            container.AddThemeConstantOverride("separation", 2);
            container.AddChild(CreateRefreshableSectionTitle(context, () => ModSettingsUiContext.Resolve(entry.Label)));
            if (entry.Description != null)
                container.AddChild(CreateRefreshableDescriptionLabel(context,
                    () => ModSettingsUiContext.Resolve(entry.Description)));
            return container;
        }

        public static Control CreateParagraphEntry(ModSettingsUiContext context,
            ParagraphModSettingsEntryDefinition entry)
        {
            return CreateRefreshableDescriptionLabel(context, () => ModSettingsUiContext.Resolve(entry.Label));
        }

        public static Control CreateImageEntry(ModSettingsUiContext context, ImageModSettingsEntryDefinition entry)
        {
            var container = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            container.AddThemeConstantOverride("separation", 8);
            container.AddChild(CreateRefreshableSectionTitle(context, () => ModSettingsUiContext.Resolve(entry.Label)));

            if (entry.Description != null)
                container.AddChild(CreateRefreshableDescriptionLabel(context,
                    () => ModSettingsUiContext.Resolve(entry.Description)));

            var surface = new PanelContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new(0f, entry.PreviewHeight),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            surface.AddThemeStyleboxOverride("panel", CreateEntrySurfaceStyle());

            var preview = new TextureRect
            {
                Texture = entry.TextureProvider(),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new(0f, entry.PreviewHeight),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            context.RegisterRefresh(() => preview.Texture = entry.TextureProvider());
            surface.AddChild(preview);
            container.AddChild(surface);
            return container;
        }

        public static Control CreateListEntry<TItem>(ModSettingsUiContext context,
            ListModSettingsEntryDefinition<TItem> entry)
        {
            return new ModSettingsListControl<TItem>(context, entry);
        }

        public static Control CreateIntSliderEntry(ModSettingsUiContext context,
            IntSliderModSettingsEntryDefinition entry)
        {
            var control = new ModSettingsSliderControl(
                entry.Binding.Read(),
                entry.MinValue,
                entry.MaxValue,
                entry.Step,
                FormatValue,
                value =>
                {
                    entry.Binding.Write(Mathf.RoundToInt(value));
                    context.MarkDirty(entry.Binding);
                    context.RequestRefresh();
                });
            context.RegisterRefresh(() => control.SetValue(entry.Binding.Read()));

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => context.ComposeBindingDescription(entry.Description, entry.Binding),
                control);

            string FormatValue(float value)
            {
                var intValue = Mathf.RoundToInt(value);
                return entry.ValueFormatter?.Invoke(intValue) ?? intValue.ToString();
            }
        }

        public static Control CreateSubpageEntry(ModSettingsUiContext context, SubpageModSettingsEntryDefinition entry)
        {
            var control = new ModSettingsTextButton(
                ModSettingsUiContext.Resolve(entry.ButtonText, ModSettingsLocalization.Get("button.open", "Open")),
                ModSettingsButtonTone.Accent,
                () => context.NavigateToPage(entry.TargetPageId));
            control.CustomMinimumSize = new(248f, 56f);

            return CreateSettingLine(
                context,
                () => ModSettingsUiContext.Resolve(entry.Label),
                () => ModSettingsUiContext.Resolve(entry.Description),
                control);
        }

        public static MarginContainer CreateModdingScreenButtonLine(Action openAction)
        {
            var line = new MarginContainer
            {
                Name = "RitsuLibModSettings",
                CustomMinimumSize = new(0f, 64f),
            };

            line.AddThemeConstantOverride("margin_left", 12);
            line.AddThemeConstantOverride("margin_right", 12);

            var row = new HBoxContainer
            {
                Name = "ContentRow",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.Fill,
                Alignment = BoxContainer.AlignmentMode.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 24);
            line.AddChild(row);

            var label = CreateHeaderLabel(
                ModSettingsLocalization.Get("entry.title", "Mod Settings (RitsuLib)"),
                28,
                HorizontalAlignment.Left);
            label.Name = "Label";
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(label);

            var button =
                new ModSettingsSettingsEntryButton(ModSettingsLocalization.Get("button.open", "Open"), openAction)
                {
                    Name = "RitsuLibModSettingsButton",
                    FocusNeighborLeft = new("."),
                    FocusNeighborRight = new("."),
                };
            button.CustomMinimumSize = new(320f, 64f);
            row.AddChild(button);

            return line;
        }

        public static ModSettingsSidebarButton CreateSidebarButton(string text, Action onPressed,
            ModSettingsSidebarItemKind kind = ModSettingsSidebarItemKind.Page,
            string? prefix = null,
            int indentLevel = 0)
        {
            return new(text, onPressed, kind, prefix, indentLevel);
        }

        public static ColorRect CreateDivider()
        {
            return new()
            {
                CustomMinimumSize = new(0f, 2f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Color = new(0.909804f, 0.862745f, 0.745098f, 0.25098f),
            };
        }

        private static MarginContainer CreateSettingLine(ModSettingsUiContext context, Func<string> labelProvider,
            Func<string> descriptionProvider, Control valueControl)
        {
            var descriptionText = descriptionProvider();
            var line = new MarginContainer
            {
                CustomMinimumSize = new(0f, string.IsNullOrWhiteSpace(descriptionText) ? 64f : 86f),
            };

            line.AddThemeConstantOverride("margin_left", 6);
            line.AddThemeConstantOverride("margin_right", 6);
            line.AddThemeConstantOverride("margin_top", 4);
            line.AddThemeConstantOverride("margin_bottom", 4);

            var surface = new PanelContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            surface.AddThemeStyleboxOverride("panel", CreateEntrySurfaceStyle());
            line.AddChild(surface);

            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            row.AddThemeConstantOverride("separation", 24);
            surface.AddChild(row);

            var leftColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            leftColumn.AddThemeConstantOverride("separation", 4);

            var label = CreateRefreshableHeaderLabel(context, labelProvider, 28, HorizontalAlignment.Left);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            leftColumn.AddChild(label);

            var descriptionLabel = CreateRefreshableDescriptionLabel(context, descriptionProvider);
            descriptionLabel.Visible = !string.IsNullOrWhiteSpace(descriptionText);
            leftColumn.AddChild(descriptionLabel);

            row.AddChild(leftColumn);

            valueControl.CustomMinimumSize = new(Math.Max(EntryControlWidth, valueControl.CustomMinimumSize.X),
                valueControl.CustomMinimumSize.Y);
            valueControl.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
            row.AddChild(valueControl);
            return line;
        }

        private static Control CreateSection(ModSettingsUiContext context, ModSettingsSection section)
        {
            if (section.IsCollapsible)
                return new ModSettingsCollapsibleSection(
                    section.Title != null
                        ? ModSettingsUiContext.Resolve(section.Title)
                        : ModSettingsLocalization.Get("section.default", "Section"),
                    section.Id,
                    section.Description != null ? ModSettingsUiContext.Resolve(section.Description) : null,
                    section.StartCollapsed,
                    section.Entries.Select(entry => entry.CreateControl(context)).ToArray());
            {
                var container = new VBoxContainer
                {
                    Name = $"Section_{section.Id}",
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                };
                container.AddThemeConstantOverride("separation", 6);
                if (section.Title != null)
                    container.AddChild(CreateRefreshableSectionTitle(context,
                        () => ModSettingsUiContext.Resolve(section.Title)));
                if (section.Description != null)
                    container.AddChild(CreateRefreshableDescriptionLabel(context,
                        () => ModSettingsUiContext.Resolve(section.Description)));
                foreach (var entry in section.Entries)
                    container.AddChild(entry.CreateControl(context));
                return container;
            }
        }

        internal static MegaRichTextLabel CreateSectionTitle(string text)
        {
            var label = CreateHeaderLabel(text, 24, HorizontalAlignment.Left);
            label.CustomMinimumSize = new(0f, 40f);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            return label;
        }

        internal static MegaRichTextLabel CreateRefreshableSectionTitle(ModSettingsUiContext context,
            Func<string> textProvider)
        {
            var label = CreateSectionTitle(textProvider());
            context.RegisterRefresh(() => label.SetTextAutoSize(textProvider()));
            return label;
        }

        private static MegaRichTextLabel CreateRefreshableHeaderLabel(ModSettingsUiContext context,
            Func<string> textProvider,
            int fontSize, HorizontalAlignment alignment)
        {
            var label = CreateHeaderLabel(textProvider(), fontSize, alignment);
            context.RegisterRefresh(() => label.SetTextAutoSize(textProvider()));
            return label;
        }

        private static MegaRichTextLabel CreateHeaderLabel(string text, int fontSize, HorizontalAlignment alignment)
        {
            var label = new MegaRichTextLabel
            {
                BbcodeEnabled = true,
                AutoSizeEnabled = false,
                ScrollActive = false,
                FocusMode = Control.FocusModeEnum.None,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = alignment,
                Theme = ModSettingsUiResources.SettingsLineTheme,
            };

            label.AddThemeFontOverride("normal_font", ModSettingsUiResources.KreonRegular);
            label.AddThemeFontOverride("bold_font", ModSettingsUiResources.KreonBold);
            label.AddThemeFontSizeOverride("normal_font_size", fontSize);
            label.AddThemeFontSizeOverride("bold_font_size", fontSize);
            label.AddThemeFontSizeOverride("italics_font_size", fontSize);
            label.AddThemeFontSizeOverride("bold_italics_font_size", fontSize);
            label.AddThemeFontSizeOverride("mono_font_size", fontSize);
            label.MinFontSize = Math.Min(fontSize, 18);
            label.MaxFontSize = fontSize;
            label.SetTextAutoSize(text);
            return label;
        }

        internal static MegaRichTextLabel CreateInlineDescription(string text)
        {
            var label = CreateHeaderLabel(text, 16, HorizontalAlignment.Left);
            label.CustomMinimumSize = new(0f, 24f);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            label.Modulate = new(0.82f, 0.79f, 0.72f, 0.92f);
            return label;
        }

        private static MegaRichTextLabel CreateDescriptionLabel(string text)
        {
            return CreateInlineDescription(text);
        }

        internal static MegaRichTextLabel CreateRefreshableDescriptionLabel(ModSettingsUiContext context,
            Func<string> textProvider)
        {
            var label = CreateDescriptionLabel(textProvider());
            context.RegisterRefresh(() =>
            {
                var text = textProvider();
                label.SetTextAutoSize(text);
                label.Visible = !string.IsNullOrWhiteSpace(text);
            });
            return label;
        }

        private static string SanitizeName(string text)
        {
            return string.Join("_", text.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        }

        internal static StyleBoxFlat CreateSurfaceStyle()
        {
            return new()
            {
                BgColor = new(0.095f, 0.115f, 0.15f, 0.965f),
                BorderColor = new(0.38f, 0.58f, 0.70f, 0.42f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 14,
                CornerRadiusTopRight = 14,
                CornerRadiusBottomRight = 14,
                CornerRadiusBottomLeft = 14,
                ShadowColor = new(0f, 0f, 0f, 0.18f),
                ShadowSize = 4,
                ContentMarginLeft = 16,
                ContentMarginTop = 12,
                ContentMarginRight = 16,
                ContentMarginBottom = 12,
            };
        }

        private static StyleBoxFlat CreateEntrySurfaceStyle()
        {
            return CreateSurfaceStyle();
        }

        internal static StyleBoxFlat CreateInsetSurfaceStyle()
        {
            return new()
            {
                BgColor = new(0.07f, 0.085f, 0.11f, 0.98f),
                BorderColor = new(0.30f, 0.44f, 0.56f, 0.34f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 12,
                CornerRadiusTopRight = 12,
                CornerRadiusBottomRight = 12,
                CornerRadiusBottomLeft = 12,
                ContentMarginLeft = 14,
                ContentMarginTop = 12,
                ContentMarginRight = 14,
                ContentMarginBottom = 12,
            };
        }

        internal static StyleBoxFlat CreateListShellStyle()
        {
            return new()
            {
                BgColor = new(0.06f, 0.075f, 0.098f, 0.98f),
                BorderColor = new(0.34f, 0.52f, 0.64f, 0.38f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 18,
                CornerRadiusTopRight = 18,
                CornerRadiusBottomRight = 18,
                CornerRadiusBottomLeft = 18,
                ShadowColor = new(0f, 0f, 0f, 0.22f),
                ShadowSize = 6,
                ContentMarginLeft = 18,
                ContentMarginTop = 18,
                ContentMarginRight = 18,
                ContentMarginBottom = 18,
            };
        }

        internal static StyleBoxFlat CreateListItemCardStyle(bool accent = false)
        {
            return new()
            {
                BgColor = accent
                    ? new(0.115f, 0.16f, 0.205f, 0.985f)
                    : new Color(0.09f, 0.11f, 0.145f, 0.975f),
                BorderColor = accent
                    ? new(0.52f, 0.77f, 0.90f, 0.70f)
                    : new Color(0.33f, 0.50f, 0.62f, 0.34f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 16,
                CornerRadiusTopRight = 16,
                CornerRadiusBottomRight = 16,
                CornerRadiusBottomLeft = 16,
                ShadowColor = new(0f, 0f, 0f, 0.18f),
                ShadowSize = 4,
                ContentMarginLeft = 16,
                ContentMarginTop = 16,
                ContentMarginRight = 16,
                ContentMarginBottom = 16,
            };
        }

        internal static StyleBoxFlat CreateListEditorSurfaceStyle()
        {
            return new()
            {
                BgColor = new(0.055f, 0.068f, 0.09f, 0.985f),
                BorderColor = new(0.30f, 0.46f, 0.58f, 0.42f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 16,
                CornerRadiusTopRight = 16,
                CornerRadiusBottomRight = 16,
                CornerRadiusBottomLeft = 16,
                ShadowColor = new(0f, 0f, 0f, 0.16f),
                ShadowSize = 3,
                ContentMarginLeft = 18,
                ContentMarginTop = 16,
                ContentMarginRight = 18,
                ContentMarginBottom = 16,
            };
        }

        internal static StyleBoxFlat CreatePillStyle(bool highlighted = false)
        {
            return new()
            {
                BgColor = highlighted
                    ? new(0.17f, 0.28f, 0.34f, 0.98f)
                    : new Color(0.12f, 0.16f, 0.21f, 0.96f),
                BorderColor = highlighted
                    ? new(0.60f, 0.82f, 0.92f, 0.78f)
                    : new Color(0.38f, 0.54f, 0.66f, 0.40f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 999,
                CornerRadiusTopRight = 999,
                CornerRadiusBottomRight = 999,
                CornerRadiusBottomLeft = 999,
                ContentMarginLeft = 12,
                ContentMarginTop = 6,
                ContentMarginRight = 12,
                ContentMarginBottom = 6,
            };
        }
    }

    internal static class ModSettingsUiResources
    {
        public static Theme SettingsLineTheme =>
            PreloadManager.Cache.GetAsset<Theme>("res://themes/settings_screen_line_header.tres");

        public static Font KreonRegular =>
            PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_regular_shared.tres");

        public static Font KreonBold =>
            PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_bold_shared.tres");

        public static Font KreonButton =>
            PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_bold_glyph_space_two.tres");

        public static PackedScene SelectionReticleScene =>
            PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/selection_reticle"));

        public static PackedScene TickboxVisualScene =>
            PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/tickbox"));

        public static PackedScene SliderScene =>
            PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/volume_slider"));

        public static PackedScene ScrollbarScene =>
            PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/scrollbar"));

        public static Texture2D SettingsButtonTexture =>
            PreloadManager.Cache.GetAsset<Texture2D>("res://images/ui/reward_screen/reward_skip_button.png");

        public static Texture2D LeftArrowTexture =>
            PreloadManager.Cache.GetAsset<Texture2D>(
                "res://images/atlases/ui_atlas.sprites/settings_tiny_left_arrow.tres");

        public static Texture2D RightArrowTexture =>
            PreloadManager.Cache.GetAsset<Texture2D>("res://images/packed/common_ui/settings_tiny_right_arrow.png");

        public static ShaderMaterial CreateToneMaterial(ModSettingsButtonTone tone)
        {
            return tone switch
            {
                ModSettingsButtonTone.Accent => MaterialUtils.CreateHsvShaderMaterial(0.82f, 1.4f, 0.8f),
                ModSettingsButtonTone.Danger => MaterialUtils.CreateHsvShaderMaterial(0.45f, 1.5f, 0.8f),
                _ => MaterialUtils.CreateHsvShaderMaterial(0.61f, 1.6f, 1.3f),
            };
        }

        public static Color GetToneOutlineColor(ModSettingsButtonTone tone)
        {
            return tone switch
            {
                ModSettingsButtonTone.Accent => new(0.1274f, 0.26f, 0.14066f),
                ModSettingsButtonTone.Danger => new(0.29f, 0.14703f, 0.1421f),
                _ => new(0.2f, 0.1575f, 0.098f),
            };
        }
    }

    internal sealed partial class ModSettingsToggleControl : Button
    {
        private readonly bool _initialValue;
        private readonly Action<bool>? _onChanged;
        private bool _isOn;

        public ModSettingsToggleControl(bool initialValue, Action<bool> onChanged)
        {
            _initialValue = initialValue;
            _onChanged = onChanged;

            CustomMinimumSize = new(248f, 56f);
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            AddThemeFontSizeOverride("font_size", 20);
            AddThemeColorOverride("font_color", new(0.95f, 0.98f, 1f));
            AddThemeColorOverride("font_hover_color", new(1f, 1f, 1f));
            AddThemeColorOverride("font_pressed_color", new(1f, 1f, 1f));
            AddThemeColorOverride("font_focus_color", new(1f, 1f, 1f));
            Pressed += ToggleValue;
        }

        public ModSettingsToggleControl()
        {
        }

        public override void _Ready()
        {
            _isOn = _initialValue;
            ApplyVisualState();
        }

        public void SetValue(bool value)
        {
            _isOn = value;
            ApplyVisualState();
        }

        private void ToggleValue()
        {
            _isOn = !_isOn;
            ApplyVisualState();
            _onChanged?.Invoke(_isOn);
            ReleaseFocus();
        }

        private void ApplyVisualState()
        {
            Text = _isOn
                ? ModSettingsLocalization.Get("toggle.on", "On")
                : ModSettingsLocalization.Get("toggle.off", "Off");
            AddThemeStyleboxOverride("normal", CreateStyle(_isOn, false));
            AddThemeStyleboxOverride("hover", CreateStyle(_isOn, true));
            AddThemeStyleboxOverride("pressed", CreateStyle(true, true));
            AddThemeStyleboxOverride("focus", CreateStyle(_isOn, true));
            AddThemeStyleboxOverride("disabled", CreateDisabledStyle());
        }

        private static StyleBoxFlat CreateStyle(bool on, bool hovered)
        {
            return new()
            {
                BgColor = on
                    ? new(0.18f, 0.42f, 0.31f, 0.98f)
                    : hovered
                        ? new(0.18f, 0.22f, 0.28f, 0.98f)
                        : new Color(0.12f, 0.15f, 0.19f, 0.98f),
                BorderColor = on
                    ? new(0.52f, 0.87f, 0.69f, 0.95f)
                    : new Color(0.34f, 0.46f, 0.58f, 0.45f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                CornerRadiusTopLeft = 12,
                CornerRadiusTopRight = 12,
                CornerRadiusBottomRight = 12,
                CornerRadiusBottomLeft = 12,
                ContentMarginLeft = 20,
                ContentMarginTop = 10,
                ContentMarginRight = 20,
                ContentMarginBottom = 10,
            };
        }

        private static StyleBoxFlat CreateDisabledStyle()
        {
            return new()
            {
                BgColor = new(0.10f, 0.10f, 0.12f, 0.7f),
                BorderColor = new(0.25f, 0.25f, 0.28f, 0.35f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                CornerRadiusTopLeft = 12,
                CornerRadiusTopRight = 12,
                CornerRadiusBottomRight = 12,
                CornerRadiusBottomLeft = 12,
                ContentMarginLeft = 20,
                ContentMarginTop = 10,
                ContentMarginRight = 20,
                ContentMarginBottom = 10,
            };
        }
    }

    internal sealed partial class ModSettingsSliderControl : HBoxContainer
    {
        private readonly Func<float, string>? _formatter;
        private readonly Action<float>? _onChanged;
        private HSlider? _slider;
        private bool _suppressCallbacks;
        private LineEdit? _valueEdit;

        public ModSettingsSliderControl(
            float initialValue,
            float minValue,
            float maxValue,
            float step,
            Func<float, string> formatter,
            Action<float> onChanged)
        {
            _formatter = formatter;
            _onChanged = onChanged;

            CustomMinimumSize = new(248f, 56f);
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            Alignment = AlignmentMode.Center;
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeConstantOverride("separation", 10);

            var valueEdit = new LineEdit
            {
                Name = "SliderValue",
                CustomMinimumSize = new(80f, 56f),
                Alignment = HorizontalAlignment.Center,
                SelectAllOnFocus = true,
                CaretBlink = true,
            };
            valueEdit.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
            valueEdit.AddThemeFontSizeOverride("font_size", 18);
            valueEdit.AddThemeColorOverride("font_color", new(1f, 0.964706f, 0.886275f));
            valueEdit.AddThemeStyleboxOverride("normal", ModSettingsUiFactory.CreateSurfaceStyle());
            valueEdit.AddThemeStyleboxOverride("focus", ModSettingsUiFactory.CreateSurfaceStyle());
            AddChild(valueEdit);
            _valueEdit = valueEdit;

            var sliderPanel = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new(240f, 56f),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            sliderPanel.AddThemeConstantOverride("margin_top", 16);
            sliderPanel.AddThemeConstantOverride("margin_bottom", 16);
            AddChild(sliderPanel);

            var slider = new HSlider
            {
                Name = "Slider",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                CustomMinimumSize = new(0f, 24f),
                FocusMode = FocusModeEnum.Click,
                MouseFilter = MouseFilterEnum.Pass,
                MinValue = minValue,
                MaxValue = maxValue,
                Step = step,
                Value = initialValue,
            };
            slider.AddThemeStyleboxOverride("slider", CreateSliderStyle(false));
            slider.AddThemeStyleboxOverride("grabber_area", CreateSliderStyle(false));
            slider.AddThemeStyleboxOverride("grabber_area_highlight", CreateSliderStyle(true));
            sliderPanel.AddChild(slider);
            _slider = slider;
        }

        public ModSettingsSliderControl()
        {
        }

        public override void _Ready()
        {
            if (_slider == null)
                return;

            RefreshValueLabel((float)_slider.Value);
            _slider.ValueChanged += OnSliderValueChanged;
            _slider.DragEnded += _ => _slider.ReleaseFocus();
            if (_valueEdit == null) return;
            _valueEdit.TextSubmitted += OnValueSubmitted;
            _valueEdit.FocusExited += OnValueFocusExited;
        }

        private void OnSliderValueChanged(double value)
        {
            if (_suppressCallbacks)
                return;
            var floatValue = (float)value;
            RefreshValueLabel(floatValue);
            _onChanged?.Invoke(floatValue);
        }

        public void SetValue(float value)
        {
            if (_slider == null)
                return;

            _suppressCallbacks = true;
            _slider.Value = value;
            RefreshValueLabel((float)_slider.Value);
            _suppressCallbacks = false;
        }

        private void RefreshValueLabel(float value)
        {
            if (_valueEdit == null || _formatter == null)
                return;
            _valueEdit.Text = _formatter(value);
        }

        private void OnValueSubmitted(string text)
        {
            TryApplyTypedValue(text);
            _valueEdit?.ReleaseFocus();
        }

        private void OnValueFocusExited()
        {
            if (_valueEdit != null)
                TryApplyTypedValue(_valueEdit.Text);
        }

        private void TryApplyTypedValue(string text)
        {
            if (_slider == null)
                return;

            if (!float.TryParse(text, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var value) &&
                !float.TryParse(text, out value))
            {
                RefreshValueLabel((float)_slider.Value);
                return;
            }

            value = Mathf.Clamp(value, (float)_slider.MinValue, (float)_slider.MaxValue);
            if (_slider.Step > 0)
                value = Mathf.Snapped(value, (float)_slider.Step);
            _slider.Value = value;
        }

        private static StyleBoxFlat CreateSliderStyle(bool highlighted)
        {
            return new()
            {
                BgColor = highlighted
                    ? new(0.48f, 0.73f, 0.92f, 0.95f)
                    : new Color(0.26f, 0.34f, 0.43f, 0.98f),
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomRight = 6,
                CornerRadiusBottomLeft = 6,
                ContentMarginLeft = 8,
                ContentMarginTop = 6,
                ContentMarginRight = 8,
                ContentMarginBottom = 6,
            };
        }
    }

    internal sealed partial class ModSettingsChoiceControl<TValue> : HBoxContainer
    {
        private readonly TValue? _currentValue;
        private readonly Action<TValue>? _onChanged;
        private readonly (TValue Value, string Label)[]? _optionsWithValues;
        private int _currentIndex;
        private Label? _label;
        private bool _suppressCallbacks;

        public ModSettingsChoiceControl(
            IReadOnlyList<(TValue Value, string Label)> options,
            TValue currentValue,
            Action<TValue> onChanged)
        {
            _optionsWithValues = options.ToArray();
            _currentValue = currentValue;
            _onChanged = onChanged;

            CustomMinimumSize = new(248f, 56f);
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            MouseFilter = MouseFilterEnum.Ignore;
            Alignment = AlignmentMode.Center;
            AddThemeConstantOverride("separation", 8);

            AddChild(new ModSettingsMiniButton("<", () => Shift(-1)) { CustomMinimumSize = new(44f, 56f) });

            var center = new PanelContainer
            {
                CustomMinimumSize = new(152f, 56f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            center.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateSurfaceStyle());
            AddChild(center);

            var label = new Label
            {
                Name = "Label",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            label.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            label.AddThemeFontSizeOverride("font_size", 18);
            label.AddThemeColorOverride("font_color", new(0.95f, 0.98f, 1f));
            center.AddChild(label);
            _label = label;

            AddChild(new ModSettingsMiniButton(">", () => Shift(1)) { CustomMinimumSize = new(44f, 56f) });
        }

        public ModSettingsChoiceControl()
        {
        }

        public override void _Ready()
        {
            if (_optionsWithValues == null)
                return;

            var startingIndex = Array.FindIndex(_optionsWithValues,
                option => EqualityComparer<TValue>.Default.Equals(option.Value, _currentValue));
            if (startingIndex < 0)
                startingIndex = 0;
            _currentIndex = startingIndex;
            RefreshCurrentLabel();
        }

        private void Shift(int delta)
        {
            if (_optionsWithValues == null || _optionsWithValues.Length == 0)
                return;

            _currentIndex = (_currentIndex + delta + _optionsWithValues.Length) % _optionsWithValues.Length;
            RefreshCurrentLabel();
            if (!_suppressCallbacks)
                _onChanged?.Invoke(_optionsWithValues[_currentIndex].Value);
        }

        public void SetValue(TValue value)
        {
            if (_optionsWithValues == null)
                return;
            var index = Array.FindIndex(_optionsWithValues,
                option => EqualityComparer<TValue>.Default.Equals(option.Value, value));
            if (index < 0)
                return;
            _suppressCallbacks = true;
            _currentIndex = index;
            RefreshCurrentLabel();
            _suppressCallbacks = false;
        }

        private void RefreshCurrentLabel()
        {
            if (_optionsWithValues == null || _label == null)
                return;
            _label.Text = _optionsWithValues[_currentIndex].Label;
        }
    }

    internal sealed partial class ModSettingsMiniButton : Button
    {
        public ModSettingsMiniButton(string text, Action action)
        {
            Text = text;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            AddThemeFontSizeOverride("font_size", 16);
            AddThemeColorOverride("font_color", new(0.95f, 0.98f, 1f));
            AddThemeColorOverride("font_hover_color", new(1f, 1f, 1f));
            AddThemeColorOverride("font_pressed_color", new(1f, 1f, 1f));
            AddThemeStyleboxOverride("normal", CreateStyle(false, false));
            AddThemeStyleboxOverride("hover", CreateStyle(true, false));
            AddThemeStyleboxOverride("pressed", CreateStyle(true, false));
            AddThemeStyleboxOverride("focus", CreateStyle(true, false));
            AddThemeStyleboxOverride("disabled", CreateStyle(false, true));
            Pressed += () =>
            {
                action();
                ReleaseFocus();
            };
        }

        public ModSettingsMiniButton()
        {
        }

        private static StyleBoxFlat CreateStyle(bool highlighted, bool disabled)
        {
            return new()
            {
                BgColor = disabled
                    ? new(0.11f, 0.14f, 0.18f, 0.82f)
                    : highlighted
                        ? new(0.17f, 0.28f, 0.34f, 0.98f)
                        : new Color(0.12f, 0.16f, 0.21f, 0.96f),
                BorderColor = disabled
                    ? new(0.28f, 0.36f, 0.43f, 0.40f)
                    : highlighted
                        ? new(0.60f, 0.82f, 0.92f, 0.78f)
                        : new Color(0.38f, 0.54f, 0.66f, 0.40f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 999,
                CornerRadiusTopRight = 999,
                CornerRadiusBottomRight = 999,
                CornerRadiusBottomLeft = 999,
                ContentMarginLeft = 12,
                ContentMarginTop = 6,
                ContentMarginRight = 12,
                ContentMarginBottom = 6,
            };
        }
    }

    internal sealed partial class ModSettingsDragHandle : Button
    {
        private readonly Func<Dictionary>? _dragDataProvider;

        public ModSettingsDragHandle(string indexLabel, Func<Dictionary> dragDataProvider)
        {
            _dragDataProvider = dragDataProvider;

            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            CustomMinimumSize = new(76f, 0f);
            SizeFlagsVertical = SizeFlags.ExpandFill;
            AddThemeStyleboxOverride("normal", CreateRailStyle(false));
            AddThemeStyleboxOverride("hover", CreateRailStyle(true));
            AddThemeStyleboxOverride("pressed", CreateRailStyle(true));
            AddThemeStyleboxOverride("focus", CreateRailStyle(true));
            MouseDefaultCursorShape = CursorShape.Drag;

            var content = new VBoxContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            content.AddThemeConstantOverride("separation", 8);
            AddChild(content);

            var number = new Label
            {
                Text = indexLabel,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            number.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            number.AddThemeFontSizeOverride("font_size", 26);
            number.AddThemeColorOverride("font_color", new(0.96f, 0.98f, 1f));
            content.AddChild(number);

            var grip = new Label
            {
                Text = "::::",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            grip.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            grip.AddThemeFontSizeOverride("font_size", 18);
            grip.AddThemeColorOverride("font_color", new(0.78f, 0.88f, 0.94f, 0.95f));
            content.AddChild(grip);

            var hint = new Label
            {
                Text = ModSettingsLocalization.Get("list.dragShort", "Drag"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            hint.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
            hint.AddThemeFontSizeOverride("font_size", 13);
            hint.AddThemeColorOverride("font_color", new(0.80f, 0.89f, 0.94f, 0.90f));
            content.AddChild(hint);
        }

        public ModSettingsDragHandle()
        {
        }

        public override Variant _GetDragData(Vector2 atPosition)
        {
            if (_dragDataProvider == null)
                return default;

            var preview = new PanelContainer
            {
                CustomMinimumSize = new(120f, 40f),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            preview.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreatePillStyle(true));

            var label = new Label
            {
                Text = ModSettingsLocalization.Get("list.dragHint", "Drag to reorder"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            label.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            label.AddThemeFontSizeOverride("font_size", 16);
            label.AddThemeColorOverride("font_color", new(0.93f, 0.97f, 1f));
            preview.AddChild(label);
            SetDragPreview(preview);
            return Variant.From(_dragDataProvider());
        }

        private static StyleBoxFlat CreateRailStyle(bool highlighted)
        {
            return new()
            {
                BgColor = highlighted
                    ? new(0.16f, 0.28f, 0.36f, 0.98f)
                    : new Color(0.12f, 0.20f, 0.27f, 0.96f),
                BorderColor = highlighted
                    ? new(0.65f, 0.86f, 0.94f, 0.88f)
                    : new Color(0.40f, 0.60f, 0.71f, 0.58f),
                BorderWidthLeft = 0,
                BorderWidthTop = 0,
                BorderWidthRight = 1,
                BorderWidthBottom = 0,
                CornerRadiusTopLeft = 14,
                CornerRadiusTopRight = 0,
                CornerRadiusBottomRight = 0,
                CornerRadiusBottomLeft = 14,
                ContentMarginLeft = 10,
                ContentMarginTop = 16,
                ContentMarginRight = 10,
                ContentMarginBottom = 16,
            };
        }
    }

    internal sealed partial class ModSettingsListControl<TItem> : VBoxContainer
    {
        private readonly ModSettingsUiContext _context;
        private readonly string _dragToken = Guid.NewGuid().ToString("N");
        private readonly System.Collections.Generic.Dictionary<int, ModSettingsListDropSlot<TItem>> _dropSlots = [];
        private readonly ListModSettingsEntryDefinition<TItem> _entry;
        private ModSettingsListDropSlot<TItem>? _activeDropSlot;
        private Label? _countLabel;
        private int _currentDragIndex = -1;
        private bool _dropCommitted;
        private PanelContainer? _emptyState;
        private VBoxContainer? _rows;

        public ModSettingsListControl(ModSettingsUiContext context, ListModSettingsEntryDefinition<TItem> entry)
        {
            _context = context;
            _entry = entry;

            MouseFilter = MouseFilterEnum.Ignore;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddThemeConstantOverride("separation", 10);
        }

        public ModSettingsListControl()
        {
            _context = null!;
            _entry = null!;
        }

        public override void _Notification(int what)
        {
            if (what != NotificationDragEnd) return;
            if (!_dropCommitted && _activeDropSlot != null && _currentDragIndex >= 0)
                MoveDraggedItemTo(_activeDropSlot.TargetIndex);

            _currentDragIndex = -1;
            _dropCommitted = false;
            ClearActiveDropSlot();
        }

        public override void _Process(double delta)
        {
            if (_currentDragIndex < 0 || !Input.IsMouseButtonPressed(MouseButton.Left) || _rows == null)
                return;

            var mouse = GetViewport().GetMousePosition();
            var nearestTargetIndex = -1;
            var nearestDistance = float.MaxValue;

            foreach (var pair in _dropSlots)
            {
                var rect = pair.Value.GetGlobalRect();
                var center = rect.Position + rect.Size * 0.5f;
                var dx = mouse.X < rect.Position.X
                    ? rect.Position.X - mouse.X
                    : mouse.X > rect.End.X
                        ? mouse.X - rect.End.X
                        : 0f;
                var dy = MathF.Abs(mouse.Y - center.Y);
                var distance = dx * 0.25f + dy;
                if (!(distance < nearestDistance)) continue;
                nearestDistance = distance;
                nearestTargetIndex = pair.Key;
            }

            if (nearestTargetIndex >= 0)
                PreviewDropAtIndex(nearestTargetIndex);
        }

        public override void _Ready()
        {
            var shell = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            shell.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListShellStyle());
            AddChild(shell);

            var root = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddThemeConstantOverride("separation", 14);
            shell.AddChild(root);

            var header = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = AlignmentMode.Center,
            };
            header.AddThemeConstantOverride("separation", 12);
            root.AddChild(header);

            var textColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            textColumn.AddThemeConstantOverride("separation", 4);
            header.AddChild(textColumn);

            textColumn.AddChild(ModSettingsUiFactory.CreateRefreshableSectionTitle(_context,
                () => ModSettingsUiContext.Resolve(_entry.Label)));

            var descriptionLabel = ModSettingsUiFactory.CreateRefreshableDescriptionLabel(_context,
                () => _context.ComposeBindingDescription(_entry.Description, _entry.Binding));
            textColumn.AddChild(descriptionLabel);

            var summary = new PanelContainer
            {
                CustomMinimumSize = new(108f, 38f),
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            summary.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreatePillStyle());
            header.AddChild(summary);

            var countLabel = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            countLabel.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            countLabel.AddThemeFontSizeOverride("font_size", 16);
            countLabel.AddThemeColorOverride("font_color", new(0.90f, 0.96f, 1f));
            summary.AddChild(countLabel);
            _countLabel = countLabel;

            var addButton = new ModSettingsTextButton(ModSettingsUiContext.Resolve(_entry.AddButtonText),
                ModSettingsButtonTone.Accent,
                () => Mutate(items => items.Add(_entry.CreateItem())))
            {
                CustomMinimumSize = new(184f, 56f),
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            };
            header.AddChild(addButton);

            var body = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            body.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateInsetSurfaceStyle());
            root.AddChild(body);

            var bodyContent = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            bodyContent.AddThemeConstantOverride("separation", 8);
            body.AddChild(bodyContent);

            var hintLabel = ModSettingsUiFactory.CreateInlineDescription(
                ModSettingsLocalization.Get("list.dragHint", "Drag to reorder"));
            bodyContent.AddChild(hintLabel);

            _rows = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _rows.AddThemeConstantOverride("separation", 10);
            bodyContent.AddChild(_rows);

            var emptyState = new PanelContainer
            {
                Visible = false,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            emptyState.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreatePillStyle());
            bodyContent.AddChild(emptyState);

            var emptyLabel = new Label
            {
                Text = ModSettingsLocalization.Get("list.empty", "No items yet. Add one to start editing."),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            emptyLabel.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
            emptyLabel.AddThemeFontSizeOverride("font_size", 18);
            emptyLabel.AddThemeColorOverride("font_color", new(0.83f, 0.89f, 0.94f, 0.92f));
            emptyState.AddChild(emptyLabel);
            _emptyState = emptyState;

            _context.RegisterRefresh(RebuildRows);
            RebuildRows();
        }

        private void RebuildRows()
        {
            if (_rows == null)
                return;

            ClearActiveDropSlot();
            _dropSlots.Clear();
            _rows.FreeChildren();

            var items = _entry.Binding.Read();
            _countLabel?.Text = string.Format(
                ModSettingsLocalization.Get("list.count", "{0} items"),
                items.Count);

            _emptyState?.Visible = items.Count == 0;

            _rows.AddChild(RegisterDropSlot(new(this, 0), 0));
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                _rows.AddChild(CreateRow(index, item, items.Count));
                _rows.AddChild(RegisterDropSlot(new(this, index + 1), index + 1));
            }
        }

        private Control CreateRow(int index, TItem item, int itemCount)
        {
            var itemContext = new ModSettingsListItemContext<TItem>(
                _context,
                CreateItemBinding(index),
                index,
                itemCount,
                item,
                updatedItem => Mutate(items => items[index] = updatedItem),
                index > 0 ? () => Mutate(items => MoveItem(items, index, index - 1)) : null,
                index < itemCount - 1 ? () => Mutate(items => MoveItem(items, index, index + 1)) : null,
                () => Mutate(items => DuplicateItem(items, index)),
                () => Mutate(items => items.RemoveAt(index)),
                _context.RequestRefresh);

            return new ModSettingsListItemCard<TItem>(
                this,
                index,
                itemCount,
                ModSettingsUiContext.Resolve(_entry.ItemLabel(item)),
                _entry.ItemDescription?.Invoke(item) is { } description
                    ? ModSettingsUiContext.Resolve(description)
                    : null,
                itemContext,
                _entry.ItemEditorFactory?.Invoke(itemContext));
        }

        private void Mutate(Action<List<TItem>> mutate)
        {
            var clone = CloneBindingValue(_entry.Binding.Read());
            mutate(clone);
            _entry.Binding.Write(clone);
            _context.MarkDirty(_entry.Binding);
            _context.RequestRefresh();
        }

        private IModSettingsValueBinding<TItem> CreateItemBinding(int index)
        {
            var itemAdapter = _entry.ItemDataAdapter;
            return ModSettingsBindings.Project(
                _entry.Binding,
                $"items[{index}]",
                items => items[index],
                (items, item) => ReplaceAt(items, index, item),
                itemAdapter);
        }

        internal Dictionary CreateDragData(int index)
        {
            _currentDragIndex = index;
            _dropCommitted = false;
            return new()
            {
                ["token"] = _dragToken,
                ["index"] = index,
            };
        }

        internal bool CanAcceptDrop(Variant data)
        {
            return data.VariantType == Variant.Type.Dictionary
                   && data.AsGodotDictionary().TryGetValue("token", out var token)
                   && token.AsString() == _dragToken;
        }

        internal void HandleDrop(Variant data, int targetIndex)
        {
            if (!CanAcceptDrop(data))
                return;

            var dragIndex = data.AsGodotDictionary()["index"].AsInt32();
            _dropCommitted = true;
            ClearActiveDropSlot();
            Mutate(items => MoveItemToSlot(items, dragIndex, targetIndex));
        }

        internal void SetActiveDropSlot(ModSettingsListDropSlot<TItem>? slot, bool active)
        {
            if (!active)
            {
                if (_activeDropSlot == slot)
                    ClearActiveDropSlot();
                else
                    slot?.SetHighlighted(false);
                return;
            }

            if (_activeDropSlot != null && _activeDropSlot != slot)
                _activeDropSlot.SetHighlighted(false);

            _activeDropSlot = slot;
            _activeDropSlot?.SetHighlighted(true);
        }

        internal void ClearActiveDropSlot()
        {
            _activeDropSlot?.SetHighlighted(false);
            _activeDropSlot = null;
        }

        internal void PreviewDropAtIndex(int targetIndex)
        {
            if (_dropSlots.TryGetValue(targetIndex, out var slot))
                SetActiveDropSlot(slot, true);
        }

        internal void DropAtIndex(Variant data, int targetIndex)
        {
            HandleDrop(data, targetIndex);
        }

        private void MoveDraggedItemTo(int targetIndex)
        {
            var dragIndex = _currentDragIndex;
            if (dragIndex < 0)
                return;

            _dropCommitted = true;
            Mutate(items => MoveItemToSlot(items, dragIndex, targetIndex));
        }

        private ModSettingsListDropSlot<TItem> RegisterDropSlot(ModSettingsListDropSlot<TItem> slot, int index)
        {
            _dropSlots[index] = slot;
            return slot;
        }

        private List<TItem> CloneBindingValue(List<TItem> items)
        {
            return _entry.Binding is IStructuredModSettingsValueBinding<List<TItem>> structured
                ? structured.Adapter.Clone(items)
                : items.ToList();
        }

        private static List<TItem> ReplaceAt(List<TItem> items, int index, TItem item)
        {
            var clone = items.ToList();
            clone[index] = item;
            return clone;
        }

        private void DuplicateItem(List<TItem> items, int index)
        {
            if (index < 0 || index >= items.Count)
                return;

            var item = items[index];
            if (_entry.ItemDataAdapter != null)
                item = _entry.ItemDataAdapter.Clone(item);
            items.Insert(index + 1, item);
        }

        private static void MoveItem(List<TItem> items, int from, int to)
        {
            if (from < 0 || from >= items.Count || to < 0 || to >= items.Count || from == to)
                return;

            var item = items[from];
            items.RemoveAt(from);
            items.Insert(to, item);
        }

        private static void MoveItemToSlot(List<TItem> items, int from, int slotIndex)
        {
            if (from < 0 || from >= items.Count)
                return;

            slotIndex = Mathf.Clamp(slotIndex, 0, items.Count);
            var normalizedIndex = slotIndex;
            if (from < normalizedIndex)
                normalizedIndex--;

            if (normalizedIndex == from)
                return;

            var item = items[from];
            items.RemoveAt(from);
            items.Insert(normalizedIndex, item);
        }
    }

    internal sealed partial class ModSettingsListDropSlot<TItem> : PanelContainer
    {
        private readonly ModSettingsListControl<TItem> _owner;

        public ModSettingsListDropSlot(ModSettingsListControl<TItem> owner, int targetIndex)
        {
            _owner = owner;
            TargetIndex = targetIndex;

            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            MouseFilter = MouseFilterEnum.Stop;
            CustomMinimumSize = new(0f, 8f);
            AddThemeStyleboxOverride("panel", CreateStyle(false));
        }

        public ModSettingsListDropSlot()
        {
            _owner = null!;
        }

        internal int TargetIndex { get; }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            var canDrop = _owner.CanAcceptDrop(data);
            _owner.SetActiveDropSlot(this, canDrop);
            return canDrop;
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            _owner.HandleDrop(data, TargetIndex);
        }

        public override void _Notification(int what)
        {
            if (what == NotificationDragEnd)
                _owner.ClearActiveDropSlot();
        }

        internal void SetHighlighted(bool highlighted)
        {
            AddThemeStyleboxOverride("panel", CreateStyle(highlighted));
        }

        private static StyleBoxFlat CreateStyle(bool highlighted)
        {
            return new()
            {
                BgColor = highlighted
                    ? new(0.55f, 0.80f, 0.90f, 0.95f)
                    : new Color(0f, 0f, 0f, 0f),
                BorderColor = highlighted
                    ? new(0.75f, 0.90f, 0.96f, 0.98f)
                    : new Color(0f, 0f, 0f, 0f),
                BorderWidthTop = highlighted ? 1 : 0,
                BorderWidthBottom = highlighted ? 1 : 0,
                BorderWidthLeft = 0,
                BorderWidthRight = 0,
                CornerRadiusTopLeft = 999,
                CornerRadiusTopRight = 999,
                CornerRadiusBottomRight = 999,
                CornerRadiusBottomLeft = 999,
                ContentMarginLeft = 0,
                ContentMarginTop = highlighted ? 2 : 0,
                ContentMarginRight = 0,
                ContentMarginBottom = highlighted ? 2 : 0,
            };
        }
    }

    internal sealed partial class ModSettingsListItemCard<TItem> : PanelContainer
    {
        private readonly int _index;
        private readonly ModSettingsListControl<TItem> _owner;

        public ModSettingsListItemCard(
            ModSettingsListControl<TItem> owner,
            int index,
            int itemCount,
            string title,
            string? subtitle,
            ModSettingsListItemContext<TItem> itemContext,
            Control? editorContent)
        {
            _owner = owner;
            _index = index;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            MouseFilter = MouseFilterEnum.Stop;
            AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListItemCardStyle(index == 0));

            var outer = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Begin,
            };
            outer.AddThemeConstantOverride("separation", 14);
            AddChild(outer);

            outer.AddChild(new ModSettingsDragHandle((index + 1).ToString(), () => owner.CreateDragData(index)));

            var root = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddThemeConstantOverride("separation", 12);
            outer.AddChild(root);

            var header = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            header.AddThemeConstantOverride("separation", 12);
            root.AddChild(header);

            var textColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            textColumn.AddThemeConstantOverride("separation", 3);
            header.AddChild(textColumn);

            textColumn.AddChild(ModSettingsUiFactory.CreateSectionTitle(title));
            if (!string.IsNullOrWhiteSpace(subtitle))
                textColumn.AddChild(ModSettingsUiFactory.CreateInlineDescription(subtitle));

            var actions = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            actions.AddThemeConstantOverride("separation", 8);
            header.AddChild(actions);

            var moveUpButton = new ModSettingsMiniButton(ModSettingsLocalization.Get("button.moveUpShort", "Up"),
                itemContext.MoveUp)
            {
                CustomMinimumSize = new(68f, 42f),
                Disabled = !itemContext.CanMoveUp,
                TooltipText = ModSettingsLocalization.Get("button.moveUp", "Move up"),
            };
            actions.AddChild(moveUpButton);

            var moveDownButton = new ModSettingsMiniButton(ModSettingsLocalization.Get("button.moveDownShort", "Down"),
                itemContext.MoveDown)
            {
                CustomMinimumSize = new(68f, 42f),
                Disabled = !itemContext.CanMoveDown,
                TooltipText = ModSettingsLocalization.Get("button.moveDown", "Move down"),
            };
            actions.AddChild(moveDownButton);

            var duplicateButton = new ModSettingsMiniButton(
                ModSettingsLocalization.Get("button.duplicateShort", "Dup"),
                itemContext.Duplicate)
            {
                CustomMinimumSize = new(72f, 42f),
                Disabled = !itemContext.SupportsStructuredClipboard,
                TooltipText = ModSettingsLocalization.Get("button.duplicate", "Duplicate"),
            };
            actions.AddChild(duplicateButton);

            var copyButton = new ModSettingsMiniButton(
                ModSettingsLocalization.Get("button.copyShort", "Copy"),
                () =>
                {
                    if (itemContext.TryCopyToClipboard())
                        itemContext.RequestRefresh();
                })
            {
                CustomMinimumSize = new(78f, 42f),
                Disabled = !itemContext.SupportsStructuredClipboard,
                TooltipText = ModSettingsLocalization.Get("button.copy", "Copy data"),
            };
            actions.AddChild(copyButton);

            var pasteButton = new ModSettingsMiniButton(
                ModSettingsLocalization.Get("button.pasteShort", "Paste"),
                () =>
                {
                    if (itemContext.TryPasteFromClipboard())
                        itemContext.RequestRefresh();
                })
            {
                CustomMinimumSize = new(82f, 42f),
                Disabled = !itemContext.CanPasteFromClipboard(),
                TooltipText = ModSettingsLocalization.Get("button.paste", "Paste data"),
            };
            actions.AddChild(pasteButton);

            var deleteButton = new ModSettingsMiniButton(
                ModSettingsLocalization.Get("button.remove", "Remove"),
                itemContext.Remove)
            {
                CustomMinimumSize = new(96f, 42f),
            };
            actions.AddChild(deleteButton);

            if (editorContent != null)
            {
                var editorSurface = new PanelContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                editorSurface.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListEditorSurfaceStyle());
                root.AddChild(editorSurface);
                editorSurface.AddChild(editorContent);
            }

            var footer = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            footer.AddThemeConstantOverride("separation", 8);
            root.AddChild(footer);

            var statusLabel = ModSettingsUiFactory.CreateInlineDescription(string.Format(
                ModSettingsLocalization.Get("list.position", "Item {0} of {1}"),
                index + 1,
                itemCount));
            footer.AddChild(statusLabel);
        }

        public ModSettingsListItemCard()
        {
            _owner = null!;
        }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            if (!_owner.CanAcceptDrop(data))
                return false;

            _owner.PreviewDropAtIndex(atPosition.Y < Size.Y * 0.5f ? _index : _index + 1);
            return true;
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            _owner.DropAtIndex(data, atPosition.Y < Size.Y * 0.5f ? _index : _index + 1);
        }
    }

    internal sealed partial class ModSettingsCollapsibleHeaderButton : Button
    {
        private readonly Action? _action;
        private readonly string? _subtitle;
        private readonly string _title = string.Empty;
        private Label? _arrowLabel;
        private bool _selected;
        private Label? _subtitleLabel;
        private Label? _titleLabel;

        public ModSettingsCollapsibleHeaderButton(string title, string? subtitle, Action action)
        {
            _title = title;
            _subtitle = subtitle;
            _action = action;

            FocusMode = FocusModeEnum.Click;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            CustomMinimumSize = new(0f, string.IsNullOrWhiteSpace(subtitle) ? 56f : 84f);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;

            AddThemeStyleboxOverride("normal", CreateHeaderStyle(false, false));
            AddThemeStyleboxOverride("hover", CreateHeaderStyle(false, true));
            AddThemeStyleboxOverride("pressed", CreateHeaderStyle(true, true));
            AddThemeStyleboxOverride("focus", CreateHeaderStyle(false, true));

            var frame = new MarginContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            frame.AddThemeConstantOverride("margin_left", 14);
            frame.AddThemeConstantOverride("margin_top", 10);
            frame.AddThemeConstantOverride("margin_right", 14);
            frame.AddThemeConstantOverride("margin_bottom", 10);
            AddChild(frame);

            var root = new HBoxContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            root.AddThemeConstantOverride("separation", 12);
            frame.AddChild(root);

            var arrowLabel = new Label
            {
                CustomMinimumSize = new(28f, 28f),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            arrowLabel.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            arrowLabel.AddThemeFontSizeOverride("font_size", 20);
            arrowLabel.AddThemeColorOverride("font_color", new(0.90f, 0.95f, 0.98f));
            root.AddChild(arrowLabel);
            _arrowLabel = arrowLabel;

            var textColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            textColumn.AddThemeConstantOverride("separation", 2);
            root.AddChild(textColumn);

            var titleLabel = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            titleLabel.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            titleLabel.AddThemeFontSizeOverride("font_size", 22);
            titleLabel.AddThemeColorOverride("font_color", new(0.96f, 0.98f, 1f));
            textColumn.AddChild(titleLabel);
            _titleLabel = titleLabel;

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                var subtitleLabel = new Label
                {
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                subtitleLabel.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
                subtitleLabel.AddThemeFontSizeOverride("font_size", 15);
                subtitleLabel.AddThemeColorOverride("font_color", new(0.75f, 0.84f, 0.90f, 0.94f));
                textColumn.AddChild(subtitleLabel);
                _subtitleLabel = subtitleLabel;
            }

            Pressed += () =>
            {
                _action?.Invoke();
                ReleaseFocus();
            };
        }

        public ModSettingsCollapsibleHeaderButton()
        {
        }

        public override void _Ready()
        {
            _titleLabel?.Text = _title;
            _subtitleLabel?.Text = _subtitle ?? string.Empty;
            ApplySelectedState();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplySelectedState();
        }

        private void ApplySelectedState()
        {
            AddThemeStyleboxOverride("normal", CreateHeaderStyle(_selected, false));
            AddThemeStyleboxOverride("hover", CreateHeaderStyle(_selected, true));
            AddThemeStyleboxOverride("pressed", CreateHeaderStyle(true, true));
            AddThemeStyleboxOverride("focus", CreateHeaderStyle(_selected, true));
            _arrowLabel?.Text = _selected ? "▼" : "▶";
        }

        private static StyleBoxFlat CreateHeaderStyle(bool selected, bool hovered)
        {
            return new()
            {
                BgColor = selected
                    ? new(0.14f, 0.19f, 0.24f, 0.96f)
                    : hovered
                        ? new(0.12f, 0.17f, 0.22f, 0.96f)
                        : new Color(0.10f, 0.14f, 0.19f, 0.94f),
                BorderColor = selected
                    ? new(0.55f, 0.70f, 0.80f, 0.72f)
                    : new Color(0.37f, 0.48f, 0.57f, 0.52f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                CornerRadiusTopLeft = 10,
                CornerRadiusTopRight = 10,
                CornerRadiusBottomRight = 10,
                CornerRadiusBottomLeft = 10,
            };
        }
    }

    internal sealed partial class ModSettingsCollapsibleSection : VBoxContainer
    {
        private readonly Control[]? _contentControls;
        private readonly string? _description;
        private readonly string? _sectionId;
        private readonly bool _startCollapsed;
        private readonly string? _title;
        private bool _collapsed;
        private VBoxContainer? _content;
        private ModSettingsCollapsibleHeaderButton? _toggle;

        public ModSettingsCollapsibleSection(string title, string? sectionId, string? description, bool startCollapsed,
            Control[] contentControls)
        {
            _title = title;
            _sectionId = sectionId;
            _description = description;
            _startCollapsed = startCollapsed;
            _contentControls = contentControls;
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeConstantOverride("separation", 6);
        }

        public ModSettingsCollapsibleSection()
        {
        }

        public override void _Ready()
        {
            if (!string.IsNullOrWhiteSpace(_sectionId))
                Name = $"Section_{_sectionId}";

            var card = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            card.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateSurfaceStyle());
            AddChild(card);

            var cardContent = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            cardContent.AddThemeConstantOverride("separation", 6);
            card.AddChild(cardContent);

            if (_title != null)
                _toggle = new(_title, _description, ToggleCollapsed)
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };
            if (_toggle != null)
                cardContent.AddChild(_toggle);

            _content = new() { MouseFilter = MouseFilterEnum.Ignore };
            _content.AddThemeConstantOverride("separation", 6);
            if (_contentControls != null)
                foreach (var control in _contentControls)
                    _content.AddChild(control);
            cardContent.AddChild(_content);

            _collapsed = _startCollapsed;
            ApplyCollapsedState();
        }

        private void ToggleCollapsed()
        {
            _collapsed = !_collapsed;
            ApplyCollapsedState();
        }

        private void ApplyCollapsedState()
        {
            _content?.Visible = !_collapsed;
            _toggle?.SetSelected(!_collapsed);
        }
    }

    internal enum ModSettingsSidebarItemKind
    {
        ModGroup,
        Page,
        Section,
        Utility,
    }

    internal sealed partial class ModSettingsSidebarButton : Button
    {
        private readonly int _indentLevel;
        private readonly ModSettingsSidebarItemKind _kind;
        private readonly string? _prefix;
        private readonly string? _rawText;
        private bool _selected;

        public ModSettingsSidebarButton(string text, Action? action,
            ModSettingsSidebarItemKind kind = ModSettingsSidebarItemKind.Page,
            string? prefix = null,
            int indentLevel = 0)
        {
            _rawText = text;
            _indentLevel = Math.Max(0, indentLevel);
            _kind = kind;
            _prefix = prefix;
            Text = text;
            TooltipText = text;
            CustomMinimumSize = new(0f, kind switch
            {
                ModSettingsSidebarItemKind.ModGroup => 62f,
                ModSettingsSidebarItemKind.Page => 48f,
                ModSettingsSidebarItemKind.Section => 38f,
                _ => 44f,
            });
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            Alignment = HorizontalAlignment.Left;
            IconAlignment = HorizontalAlignment.Left;

            AddThemeFontOverride("font", kind == ModSettingsSidebarItemKind.ModGroup
                ? ModSettingsUiResources.KreonBold
                : ModSettingsUiResources.KreonRegular);
            AddThemeFontSizeOverride("font_size", kind switch
            {
                ModSettingsSidebarItemKind.ModGroup => 22,
                ModSettingsSidebarItemKind.Page => 19,
                ModSettingsSidebarItemKind.Section => 16,
                _ => 17,
            });
            AddThemeColorOverride("font_color", kind == ModSettingsSidebarItemKind.Section
                ? new(0.82f, 0.89f, 0.94f)
                : new Color(0.93f, 0.96f, 0.98f));
            AddThemeColorOverride("font_hover_color", new(0.98f, 1f, 1f));
            AddThemeColorOverride("font_pressed_color", new(1f, 1f, 1f));
            AddThemeColorOverride("font_focus_color", new(1f, 1f, 1f));

            AddThemeStyleboxOverride("normal", CreateStyle(false, false, _kind, _indentLevel));
            AddThemeStyleboxOverride("hover", CreateStyle(false, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("pressed", CreateStyle(true, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("focus", CreateStyle(false, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("disabled", CreateDisabledStyle());

            Pressed += () =>
            {
                action?.Invoke();
                ReleaseFocus();
            };
        }

        public ModSettingsSidebarButton()
        {
        }

        public override void _Ready()
        {
            Text = string.IsNullOrWhiteSpace(_prefix) ? _rawText ?? string.Empty : $"{_prefix}  {_rawText}";
            SetSelected(_selected);
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            AddThemeStyleboxOverride("normal", CreateStyle(_selected, false, _kind, _indentLevel));
            AddThemeStyleboxOverride("hover", CreateStyle(_selected, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("pressed", CreateStyle(true, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("focus", CreateStyle(_selected, true, _kind, _indentLevel));
        }

        internal static StyleBoxFlat CreateStyle(bool selected, bool hovered,
            ModSettingsSidebarItemKind kind = ModSettingsSidebarItemKind.Page,
            int indentLevel = 0)
        {
            var bg = kind switch
            {
                ModSettingsSidebarItemKind.ModGroup => selected
                    ? new(0.17f, 0.28f, 0.36f, 0.99f)
                    : hovered
                        ? new(0.14f, 0.23f, 0.30f, 0.98f)
                        : new Color(0.11f, 0.18f, 0.24f, 0.97f),
                ModSettingsSidebarItemKind.Section => selected
                    ? new(0.12f, 0.22f, 0.29f, 0.98f)
                    : hovered
                        ? new(0.10f, 0.18f, 0.24f, 0.95f)
                        : new Color(0.07f, 0.11f, 0.16f, 0.92f),
                ModSettingsSidebarItemKind.Utility => selected
                    ? new(0.16f, 0.24f, 0.31f, 0.98f)
                    : hovered
                        ? new(0.12f, 0.19f, 0.26f, 0.97f)
                        : new Color(0.09f, 0.14f, 0.20f, 0.95f),
                _ => selected
                    ? new(0.15f, 0.25f, 0.32f, 0.985f)
                    : hovered
                        ? new(0.11f, 0.19f, 0.26f, 0.975f)
                        : new Color(0.08f, 0.13f, 0.19f, 0.94f),
            };

            var border = kind switch
            {
                ModSettingsSidebarItemKind.ModGroup => selected
                    ? new(0.72f, 0.88f, 0.95f, 0.90f)
                    : new Color(0.47f, 0.63f, 0.73f, 0.62f),
                ModSettingsSidebarItemKind.Section => selected
                    ? new(0.56f, 0.80f, 0.90f, 0.84f)
                    : new Color(0.27f, 0.42f, 0.52f, 0.45f),
                _ => selected
                    ? new(0.63f, 0.82f, 0.92f, 0.86f)
                    : new Color(0.41f, 0.56f, 0.67f, 0.56f),
            };

            var leftBorder = selected
                ? kind == ModSettingsSidebarItemKind.Section ? 3 : 4
                : kind == ModSettingsSidebarItemKind.ModGroup
                    ? 2
                    : 1;

            return new()
            {
                BgColor = bg,
                BorderColor = border,
                BorderWidthLeft = leftBorder,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = kind == ModSettingsSidebarItemKind.Section ? 10 : 16,
                CornerRadiusTopRight = kind == ModSettingsSidebarItemKind.Section ? 10 : 16,
                CornerRadiusBottomRight = kind == ModSettingsSidebarItemKind.Section ? 10 : 16,
                CornerRadiusBottomLeft = kind == ModSettingsSidebarItemKind.Section ? 10 : 16,
                ShadowColor = new(0f, 0f, 0f, 0.18f),
                ShadowSize = kind == ModSettingsSidebarItemKind.ModGroup ? 4 : 2,
                ContentMarginLeft = (kind == ModSettingsSidebarItemKind.Section ? 14 : 18) + indentLevel * 14,
                ContentMarginTop = kind == ModSettingsSidebarItemKind.Section ? 8 : 10,
                ContentMarginRight = kind == ModSettingsSidebarItemKind.Section ? 14 : 18,
                ContentMarginBottom = kind == ModSettingsSidebarItemKind.Section ? 8 : 10,
            };
        }

        internal static StyleBoxFlat CreateDisabledStyle()
        {
            return new()
            {
                BgColor = new(0.09f, 0.10f, 0.12f, 0.7f),
                BorderColor = new(0.24f, 0.27f, 0.30f, 0.4f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                CornerRadiusTopLeft = 12,
                CornerRadiusTopRight = 12,
                CornerRadiusBottomRight = 12,
                CornerRadiusBottomLeft = 12,
                ContentMarginLeft = 18,
                ContentMarginTop = 10,
                ContentMarginRight = 18,
                ContentMarginBottom = 10,
            };
        }
    }

    internal sealed partial class ModSettingsSettingsEntryButton : NSettingsButton
    {
        private readonly Action? _action;
        private readonly string? _text;
        private MegaLabel? _buttonLabel;

        public ModSettingsSettingsEntryButton(string text, Action action)
        {
            _text = text;
            _action = action;

            CustomMinimumSize = new(320f, 64f);
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.Fill;
            FocusMode = FocusModeEnum.All;

            var image = new TextureRect
            {
                Name = "Image",
                Material = ModSettingsUiResources.CreateToneMaterial(ModSettingsButtonTone.Accent),
                CustomMinimumSize = new(64f, 64f),
                AnchorRight = 1f,
                AnchorBottom = 1f,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
                PivotOffset = new(140f, 32f),
                Texture = ModSettingsUiResources.SettingsButtonTexture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            AddChild(image);

            var label = new MegaLabel
            {
                Name = "Label",
                AnchorRight = 1f,
                AnchorBottom = 1f,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
                PivotOffset = new(140f, 32f),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            label.AddThemeColorOverride("font_color", new(0.82f, 0.94f, 0.78f));
            label.AddThemeColorOverride("font_shadow_color", new(0f, 0f, 0f, 0.25098f));
            label.AddThemeColorOverride("font_outline_color",
                ModSettingsUiResources.GetToneOutlineColor(ModSettingsButtonTone.Accent));
            label.AddThemeConstantOverride("shadow_offset_x", 4);
            label.AddThemeConstantOverride("shadow_offset_y", 3);
            label.AddThemeConstantOverride("outline_size", 12);
            label.AddThemeConstantOverride("shadow_outline_size", 0);
            label.AddThemeFontOverride("font", ModSettingsUiResources.KreonButton);
            label.AddThemeFontSizeOverride("font_size", 28);
            label.MinFontSize = 16;
            label.MaxFontSize = 28;
            AddChild(label);

            var reticle = ModSettingsUiResources.SelectionReticleScene.Instantiate<Control>();
            reticle.Name = "SelectionReticle";
            reticle.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(reticle);
        }

        public ModSettingsSettingsEntryButton()
        {
        }

        public override void _Ready()
        {
            ConnectSignals();
            _buttonLabel = GetNode<MegaLabel>("Label");
            if (_text != null)
                _buttonLabel.SetTextAutoSize(_text);
        }

        protected override void OnRelease()
        {
            base.OnRelease();
            _action?.Invoke();
            ReleaseFocus();
        }
    }

    internal partial class ModSettingsTextButton : Button
    {
        private readonly string? _text;
        private readonly ModSettingsButtonTone _tone;
        private bool _selected;

        public ModSettingsTextButton(string text, ModSettingsButtonTone tone, Action? action)
        {
            _text = text;
            _tone = tone;

            Text = text;
            CustomMinimumSize = new(248f, 56f);
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.Fill;
            FocusMode = FocusModeEnum.Click;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            AddThemeFontSizeOverride("font_size", 20);
            AddThemeColorOverride("font_color", new(0.95f, 0.98f, 1f));
            AddThemeColorOverride("font_hover_color", new(1f, 1f, 1f));
            AddThemeColorOverride("font_pressed_color", new(1f, 1f, 1f));
            AddThemeColorOverride("font_focus_color", new(1f, 1f, 1f));
            ApplyVisualState();
            Pressed += () =>
            {
                action?.Invoke();
                ReleaseFocus();
            };
        }

        public ModSettingsTextButton()
        {
        }

        public override void _Ready()
        {
            Text = _text ?? string.Empty;
            ApplyVisualState();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            AddThemeStyleboxOverride("normal", CreateStyle(_selected, false, _tone));
            AddThemeStyleboxOverride("hover", CreateStyle(_selected, true, _tone));
            AddThemeStyleboxOverride("pressed", CreateStyle(true, true, _tone));
            AddThemeStyleboxOverride("focus", CreateStyle(_selected, true, _tone));
            AddThemeStyleboxOverride("disabled", ModSettingsSidebarButton.CreateDisabledStyle());
        }

        private static StyleBoxFlat CreateStyle(bool selected, bool hovered, ModSettingsButtonTone tone)
        {
            var borderColor = tone switch
            {
                ModSettingsButtonTone.Accent => new(0.53f, 0.76f, 0.86f, 0.86f),
                ModSettingsButtonTone.Danger => new(0.87f, 0.48f, 0.46f, 0.84f),
                _ => new Color(0.45f, 0.60f, 0.70f, 0.60f),
            };

            var backgroundColor = tone switch
            {
                ModSettingsButtonTone.Accent => selected || hovered
                    ? new(0.14f, 0.27f, 0.33f, 0.985f)
                    : new Color(0.10f, 0.21f, 0.26f, 0.97f),
                ModSettingsButtonTone.Danger => selected || hovered
                    ? new(0.29f, 0.12f, 0.12f, 0.985f)
                    : new Color(0.23f, 0.095f, 0.10f, 0.97f),
                _ => selected || hovered ? new(0.13f, 0.21f, 0.28f, 0.985f) : new Color(0.10f, 0.16f, 0.22f, 0.97f),
            };

            return new()
            {
                BgColor = backgroundColor,
                BorderColor = borderColor,
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 14,
                CornerRadiusTopRight = 14,
                CornerRadiusBottomRight = 14,
                CornerRadiusBottomLeft = 14,
                ShadowColor = new(0f, 0f, 0f, 0.18f),
                ShadowSize = 4,
                ContentMarginLeft = 18,
                ContentMarginTop = 10,
                ContentMarginRight = 18,
                ContentMarginBottom = 10,
            };
        }
    }
}
