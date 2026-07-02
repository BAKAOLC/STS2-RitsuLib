using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Row injected into the vanilla <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Settings.NSettingsScreen" />
    ///     General tab. Intentionally separate from <see cref="ModSettingsUiFactory" />, which builds only the
    ///     RitsuLib mod settings submenu UI.
    ///     注入到原版 <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Settings.NSettingsScreen" /> General 标签页的行。刻意与
    ///     <see cref="ModSettingsUiFactory" /> 分离，后者只构建 RitsuLib mod 设置子菜单 UI。
    /// </summary>
    public static class ModSettingsGameSettingsEntryLine
    {
        /// <summary>
        ///     Builds the General-tab row; <paramref name="openAction" /> opens the RitsuLib mod settings submenu.
        ///     构建 General 标签页行；<paramref name="openAction" /> 打开 RitsuLib mod 设置子菜单。
        /// </summary>
        public static MarginContainer Create(Action openAction)
        {
            return Create(openAction, null);
        }

        /// <summary>
        ///     Builds the General-tab row with an optional vanilla-style settings hover tip.
        ///     构建 General 标签页行，并可选添加原版设置风格的 hover tip。
        /// </summary>
        public static MarginContainer Create(Action openAction, IHoverTip? hoverTip)
        {
            var title = ModSettingsLocalization.Get("entry.title", "Mod Settings (RitsuLib)");
            return Create(
                "RitsuLibModSettings",
                "RitsuLibModSettingsButton",
                title,
                ModSettingsLocalization.Get("button.open", "Open"),
                openAction,
                hoverTip);
        }

        /// <summary>
        ///     Builds the General-tab row that opens the game log folder.
        ///     构建 General 标签页中用于打开游戏日志文件夹的行。
        /// </summary>
        public static MarginContainer CreateOpenLogs(Action openAction)
        {
            return CreateOpenLogs(openAction, null);
        }

        /// <summary>
        ///     Builds the General-tab row that opens the game log folder with an optional vanilla-style settings hover tip.
        ///     构建 General 标签页中用于打开游戏日志文件夹的行，并可选添加原版设置风格的 hover tip。
        /// </summary>
        public static MarginContainer CreateOpenLogs(Action openAction, IHoverTip? hoverTip)
        {
            var title = ModSettingsLocalization.Get("entry.openLogs.title", "Open Logs Folder");
            return Create(
                "RitsuLibOpenLogs",
                "RitsuLibOpenLogsButton",
                title,
                ModSettingsLocalization.Get("entry.openLogs.button", "Open Logs"),
                openAction,
                hoverTip);
        }

        private static MarginContainer Create(
            string lineName,
            string buttonName,
            string labelText,
            string buttonText,
            Action openAction,
            IHoverTip? hoverTip = null)
        {
            var line = new MarginContainer
            {
                Name = lineName,
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.gameSettingsEntry.layout.lineMinSize",
                    new(0f, 64f)),
            };

            var lineMargins =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.gameSettingsEntry.layout.margin", 12);
            lineMargins = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.gameSettingsEntry.layout.margin.left",
                    lineMargins.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.gameSettingsEntry.layout.margin.top", 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.gameSettingsEntry.layout.margin.right",
                    lineMargins.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.gameSettingsEntry.layout.margin.bottom", 0));
            line.AddThemeConstantOverride("margin_left", lineMargins.Left);
            line.AddThemeConstantOverride("margin_right", lineMargins.Right);
            line.AddThemeConstantOverride("margin_top", lineMargins.Top);
            line.AddThemeConstantOverride("margin_bottom", lineMargins.Bottom);

            var row = new HBoxContainer
            {
                Name = "ContentRow",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                Alignment = BoxContainer.AlignmentMode.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.gameSettingsEntry.layout.rowSeparation", 0));
            line.AddChild(row);

            var label = CreateVanillaGeneralSettingsRowLabel(
                labelText);
            label.Name = "Label";
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(label);

            var button = new ModSettingsGameSettingsEntryButton(
                buttonText, openAction)
            {
                Name = buttonName,
            };
            button.CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.gameSettingsEntry.layout.buttonMinSize",
                new(320f, 64f));
            button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            button.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
            row.AddChild(button);

            AttachSettingsHoverTip(line, hoverTip);

            return line;
        }

        private static void AttachSettingsHoverTip(Control owner, IHoverTip? hoverTip)
        {
            if (hoverTip == null)
                return;

            owner.Connect(Control.SignalName.MouseEntered, Callable.From(OnHovered));
            owner.Connect(Control.SignalName.MouseExited, Callable.From(OnUnhovered));
            owner.Connect(Node.SignalName.TreeExiting, Callable.From(OnUnhovered));

            return;

            void OnHovered()
            {
                NHoverTipSet.CreateAndShow(owner, hoverTip)
                    ?.SetGlobalPosition(owner.GlobalPosition + NSettingsScreen.settingTipsOffset);
            }

            void OnUnhovered()
            {
                NHoverTipSet.Remove(owner);
            }
        }

        /// <summary>
        ///     Same RichText setup as vanilla <c>settings_screen.tscn</c> SendFeedback row (not mod submenu styling).
        ///     与原版 <c>settings_screen.tscn</c> SendFeedback 行使用相同的 RichText 设置（不是 mod 子菜单样式）。
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

            label.AddThemeFontOverride("normal_font", RitsuShellTheme.Current.Font.Body);
            label.AddThemeFontOverride("bold_font", RitsuShellTheme.Current.Font.BodyBold);
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
    ///     采用 NSettingsButton 样式的控件，仅用于原版设置界面入口（不是子菜单行）。
    /// </summary>
    internal sealed partial class ModSettingsGameSettingsEntryButton : NSettingsButton
    {
        private const string SelectionReticleScenePath = "res://scenes/ui/selection_reticle.tscn";

        private readonly Action? _action;
        private readonly string? _text;
        private MegaLabel? _buttonLabel;

        public ModSettingsGameSettingsEntryButton(string text, Action action)
        {
            _text = text;
            _action = action;

            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.gameSettingsEntry.layout.buttonMinSize",
                new(320f, 64f));
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            SizeFlagsVertical = SizeFlags.ShrinkBegin;
            FocusMode = FocusModeEnum.All;

            var image = new TextureRect
            {
                Name = "Image",
                Material = ModSettingsUiResources.CreateToneMaterial(ModSettingsButtonTone.Accent),
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.gameSettingsEntry.layout.buttonImageMinSize",
                    new(64f, 64f)),
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
            label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Button);
            label.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.SettingsEntryButton);
            label.MinFontSize = 16;
            label.MaxFontSize = 28;
            AddChild(label);

            AddChild(CreateSelectionReticle());
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
            this.ReleaseFocusIfInsideTree();
        }

        private static Control CreateSelectionReticle()
        {
            var reticle = PreloadManager.Cache.GetScene(SelectionReticleScenePath).Instantiate<Control>();
            reticle.Name = "SelectionReticle";
            reticle.AnchorRight = 1f;
            reticle.AnchorBottom = 1f;
            reticle.OffsetLeft = 0f;
            reticle.OffsetTop = 0f;
            reticle.OffsetRight = 0f;
            reticle.OffsetBottom = 0f;
            reticle.GrowHorizontal = GrowDirection.Both;
            reticle.GrowVertical = GrowDirection.Both;
            reticle.MouseFilter = MouseFilterEnum.Ignore;
            return reticle;
        }
    }
}
