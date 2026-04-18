using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Row injected into the vanilla <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Settings.NSettingsScreen" />
    ///     General tab. Intentionally separate from <see cref="ModSettingsUiFactory" />, which builds only the
    ///     RitsuLib mod settings submenu UI.
    /// </summary>
    public static class ModSettingsGameSettingsEntryLine
    {
        /// <summary>
        ///     Builds the General-tab row; <paramref name="openAction" /> opens the RitsuLib mod settings submenu.
        /// </summary>
        public static MarginContainer Create(Action openAction)
        {
            var line = new MarginContainer
            {
                Name = "RitsuLibModSettings",
                CustomMinimumSize = new(0f, 64f),
            };

            line.AddThemeConstantOverride("margin_left", 12);
            line.AddThemeConstantOverride("margin_right", 12);
            line.AddThemeConstantOverride("margin_top", 0);
            line.AddThemeConstantOverride("margin_bottom", 0);

            var row = new HBoxContainer
            {
                Name = "ContentRow",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                Alignment = BoxContainer.AlignmentMode.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 0);
            line.AddChild(row);

            var label = CreateVanillaGeneralSettingsRowLabel(
                ModSettingsLocalization.Get("entry.title", "Mod Settings (RitsuLib)"));
            label.Name = "Label";
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(label);

            var button = new ModSettingsGameSettingsEntryButton(
                ModSettingsLocalization.Get("button.open", "Open"), openAction)
            {
                Name = "RitsuLibModSettingsButton",
            };
            button.CustomMinimumSize = new(320f, 64f);
            button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            button.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
            row.AddChild(button);

            return line;
        }

        /// <summary>
        ///     Same RichText setup as vanilla <c>settings_screen.tscn</c> SendFeedback row (not mod submenu styling).
        /// </summary>
        private static MegaRichTextLabel CreateVanillaGeneralSettingsRowLabel(string text)
        {
            const int fontSize = 28;
            var label = new MegaRichTextLabel
            {
                BbcodeEnabled = true,
                AutoSizeEnabled = false,
                FitContent = true,
                ScrollActive = false,
                ClipContents = false,
                FocusMode = Control.FocusModeEnum.None,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Theme = ModSettingsUiResources.SettingsLineTheme,
                IsHorizontallyBound = true,
                Modulate = Colors.White,
            };

            label.AddThemeFontOverride("normal_font", ModSettingsUiResources.KreonRegular);
            label.AddThemeFontOverride("bold_font", ModSettingsUiResources.KreonBold);
            label.AddThemeFontSizeOverride("normal_font_size", fontSize);
            label.AddThemeFontSizeOverride("bold_font_size", fontSize);
            label.AddThemeFontSizeOverride("italics_font_size", fontSize);
            label.AddThemeFontSizeOverride("bold_italics_font_size", fontSize);
            label.AddThemeFontSizeOverride("mono_font_size", fontSize);
            label.MinFontSize = 18;
            label.MaxFontSize = fontSize;
            label.SetTextAutoSize(text);
            return label;
        }
    }

    /// <summary>
    ///     NSettingsButton-styled control used only on the vanilla settings screen entry (not submenu rows).
    /// </summary>
    internal sealed partial class ModSettingsGameSettingsEntryButton : NSettingsButton
    {
        private readonly Action? _action;
        private readonly string? _text;
        private MegaLabel? _buttonLabel;

        public ModSettingsGameSettingsEntryButton(string text, Action action)
        {
            _text = text;
            _action = action;

            CustomMinimumSize = new(320f, 64f);
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            SizeFlagsVertical = SizeFlags.ShrinkBegin;
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
            label.AddThemeColorOverride("font_color", new(0.91f, 0.86359f, 0.7462f));
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

            // selection_reticle.tscn root ships with large default offsets for other UIs; must match
            // settings_screen FeedbackButton / NConfigButton: full rect with zero margins.
            var reticle = ModSettingsUiResources.SelectionReticleScene.Instantiate<NSelectionReticle>();
            reticle.Name = "SelectionReticle";
            reticle.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(reticle);
        }

        public ModSettingsGameSettingsEntryButton()
        {
        }

        public override void _Ready()
        {
            ConnectSignals();
            _buttonLabel = GetNode<MegaLabel>("Label");
            if (_text != null)
                _buttonLabel.SetTextAutoSize(_text);

            Callable.From(SyncLayoutDependentPivots).CallDeferred();
        }

        private void SyncLayoutDependentPivots()
        {
            if (!IsInsideTree())
                return;

            PivotOffset = Size * 0.5f;
            if (GetNodeOrNull<TextureRect>("Image") is { } image)
                image.PivotOffset = image.Size * 0.5f;
            if (_buttonLabel != null)
                _buttonLabel.PivotOffset = _buttonLabel.Size * 0.5f;
        }

        protected override void OnRelease()
        {
            base.OnRelease();
            _action?.Invoke();
            ReleaseFocus();
        }
    }
}
