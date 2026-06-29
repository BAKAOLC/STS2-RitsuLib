using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using STS2RitsuLib.Data;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings.Patches
{
    /// <summary>
    ///     Injects the RitsuLib mod settings shortcut under the vanilla patch notes button on the main menu.
    ///     在主菜单原版更新日志按钮下方注入 RitsuLib 模组设置快捷入口。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class MainMenuModSettingsButtonPatch : IPatchMethod
    {
        private const string GroupNodeName = "RitsuLibMainMenuModSettings";
        private const string ButtonNodeName = "RitsuLibMainMenuModSettingsButton";
        private const string VersionLabelNodeName = "RitsuLibMainMenuModSettingsVersion";
        private const string VisibilitySyncNodeName = "RitsuLibMainMenuModSettingsVisibilitySync";

        internal const float ButtonSize = 64f;
        internal const float GearBadgeSize = 24f;
        internal const float GearBadgeInset = -1f;
        private const float GapBelowPatchNotes = 8f;
        private const float VersionLabelTop = 2f;
        private const float VersionLabelHeight = 45f;
        private const float VersionLabelLeft = -218f;
        private const float VersionLabelRight = -6f;
        public static string PatchId => "ritsulib_main_menu_mod_settings_button";

        public static string Description =>
            "Add the RitsuLib mod settings shortcut under the main menu patch notes button";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NMainMenu), nameof(NMainMenu._Ready)),
                new(typeof(NMainMenu), "OnSubmenuStackChanged"),
            ];
        }

        public static void Postfix(NMainMenu __instance)
        {
            try
            {
                var group = EnsureEntry(__instance);
                SyncState(__instance, group);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Settings] Failed to add main menu mod settings shortcut: {ex.Message}");
            }
        }

        private static Control? EnsureEntry(NMainMenu mainMenu)
        {
            if (!GodotObject.IsInstanceValid(mainMenu))
                return null;

            var patchNotesButton = mainMenu.GetNodeOrNull<Control>("%PatchNotesButton");
            if (patchNotesButton == null || !GodotObject.IsInstanceValid(patchNotesButton))
                return null;

            if (mainMenu.GetNodeOrNull<Control>(GroupNodeName) is { } existing)
            {
                SyncPlacement(existing, patchNotesButton);
                ApplyReleaseInfoTypography(existing, mainMenu);
                RefreshVersionLabel(existing);
                EnsureVisibilitySynchronizer(existing, mainMenu);
                return existing;
            }

            RitsuLibModImageResourceLoader.EnsureRegistered();
            var group = new Control
            {
                Name = GroupNodeName,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                CustomMinimumSize = new(ButtonSize, ButtonSize),
            };

            var button = RitsuLibMainMenuModSettingsButton.Create();
            button.Name = ButtonNodeName;
            button.Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NClickableControl>(_ => OpenModSettings(mainMenu)));
            group.AddChild(button);

            group.AddChild(CreateVersionLabel());
            RitsuGodotTreeCompat.AddChildSafely(mainMenu, group);
            SyncPlacement(group, patchNotesButton);
            ApplyReleaseInfoTypography(group, mainMenu);
            EnsureVisibilitySynchronizer(group, mainMenu);
            return group;
        }

        private static void EnsureVisibilitySynchronizer(Control group, NMainMenu mainMenu)
        {
            if (group.GetNodeOrNull<RitsuLibMainMenuModSettingsVisibilitySync>(VisibilitySyncNodeName) is { } existing)
            {
                existing.Configure(mainMenu, group);
                return;
            }

            var sync = new RitsuLibMainMenuModSettingsVisibilitySync
            {
                Name = VisibilitySyncNodeName,
            };
            sync.Configure(mainMenu, group);
            group.AddChild(sync);
        }

        private static void SyncPlacement(Control group, Control patchNotesButton)
        {
            group.AnchorLeft = patchNotesButton.AnchorLeft;
            group.AnchorTop = patchNotesButton.AnchorTop;
            group.AnchorRight = patchNotesButton.AnchorRight;
            group.AnchorBottom = patchNotesButton.AnchorBottom;
            group.GrowHorizontal = patchNotesButton.GrowHorizontal;
            group.GrowVertical = patchNotesButton.GrowVertical;
            group.OffsetLeft = patchNotesButton.OffsetLeft;
            group.OffsetRight = patchNotesButton.OffsetRight;
            group.OffsetTop = patchNotesButton.OffsetBottom + GapBelowPatchNotes;
            group.OffsetBottom = group.OffsetTop + ButtonSize;

            if (group.GetNodeOrNull<RitsuLibMainMenuModSettingsButton>(ButtonNodeName) is { } button)
            {
                button.OffsetLeft = 0f;
                button.OffsetTop = 0f;
                button.OffsetRight = ButtonSize;
                button.OffsetBottom = ButtonSize;
            }

            if (group.GetNodeOrNull<Label>(VersionLabelNodeName) is not { } label) return;
            label.OffsetLeft = VersionLabelLeft;
            label.OffsetTop = VersionLabelTop;
            label.OffsetRight = VersionLabelRight;
            label.OffsetBottom = VersionLabelTop + VersionLabelHeight;
        }

        private static Label CreateVersionLabel()
        {
            var label = new Label
            {
                Name = VersionLabelNodeName,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                AutowrapMode = TextServer.AutowrapMode.Off,
            };
            label.AddThemeColorOverride("font_color", new(0.91f, 0.86359f, 0.7462f));
            label.AddThemeColorOverride("font_shadow_color", new(0f, 0f, 0f, 0.72f));
            label.AddThemeConstantOverride("shadow_offset_x", 2);
            label.AddThemeConstantOverride("shadow_offset_y", 2);
            label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            label.AddThemeFontSizeOverride("font_size", 16);
            RefreshVersionLabel(label);
            return label;
        }

        private static void ApplyReleaseInfoTypography(Control group, NMainMenu mainMenu)
        {
            if (group.GetNodeOrNull<Label>(VersionLabelNodeName) is not { } label ||
                mainMenu.GetNodeOrNull<Label>("%ReleaseInfo") is not { } releaseInfo)
                return;

            label.AddThemeFontOverride("font", releaseInfo.GetThemeFont("font"));
            label.AddThemeFontSizeOverride("font_size", releaseInfo.GetThemeFontSize("font_size"));
        }

        private static void RefreshVersionLabel(Control group)
        {
            if (group.GetNodeOrNull<Label>(VersionLabelNodeName) is { } label)
                RefreshVersionLabel(label);
        }

        private static void RefreshVersionLabel(Label label)
        {
            label.Text = $"v{Const.Version}\ncompat {RitsuLibFramework.GetCompatBranchLabel()}";
        }

        internal static void SyncState(NMainMenu mainMenu, Control? group)
        {
            if (group == null || !GodotObject.IsInstanceValid(group))
                return;

            var shouldShow = RitsuLibSettingsStore.IsMainMenuModSettingsButtonEnabled() &&
                             IsMainMenuShortcutSurfaceVisible(mainMenu);
            group.Visible = shouldShow;
            if (group.GetNodeOrNull<RitsuLibMainMenuModSettingsButton>(ButtonNodeName) is { } button)
                button.SetEnabled(shouldShow);
        }

        private static bool IsMainMenuShortcutSurfaceVisible(NMainMenu mainMenu)
        {
            if (!GodotObject.IsInstanceValid(mainMenu) ||
                mainMenu.SubmenuStack.SubmenusOpen)
                return false;

            if (mainMenu.GetNodeOrNull<Control>("%PatchNotesButton") is not { } patchNotesButton ||
                !GodotObject.IsInstanceValid(patchNotesButton) ||
                !patchNotesButton.Visible)
                return false;

            return mainMenu.PatchNotesScreen is not { } patchNotesScreen ||
                   !GodotObject.IsInstanceValid(patchNotesScreen) ||
                   (!patchNotesScreen.IsOpen && !patchNotesScreen.Visible);
        }

        private static void OpenModSettings(NMainMenu mainMenu)
        {
            if (!GodotObject.IsInstanceValid(mainMenu))
                return;

            RitsuLibModSettingsBootstrap.EnsureFrameworkPagesRegistered();
            mainMenu.SubmenuStack.PushSubmenuType<RitsuModSettingsSubmenu>();
        }
    }

    internal sealed partial class RitsuLibMainMenuModSettingsVisibilitySync : Node
    {
        private Control? _group;
        private NMainMenu? _mainMenu;

        public void Configure(NMainMenu mainMenu, Control group)
        {
            _mainMenu = mainMenu;
            _group = group;
        }

        public override void _Process(double delta)
        {
            if (_mainMenu == null ||
                _group == null ||
                !IsInstanceValid(_mainMenu) ||
                !IsInstanceValid(_group))
            {
                QueueFree();
                return;
            }

            MainMenuModSettingsButtonPatch.SyncState(_mainMenu, _group);
        }
    }

    internal sealed partial class RitsuLibMainMenuModSettingsButton : NButton
    {
        private const string HsvShaderPath = "res://shaders/hsv.gdshader";
        private const string GearTexturePath = "res://images/atlases/ui_atlas.sprites/top_bar/top_bar_settings.tres";
        private static readonly StringName ShaderParamV = new("v");
        private Control? _gear;
        private ShaderMaterial? _gearHsv;

        private ShaderMaterial? _hsv;
        private Control? _icon;

        public static RitsuLibMainMenuModSettingsButton Create()
        {
            var button = new RitsuLibMainMenuModSettingsButton
            {
                CustomMinimumSize = new(MainMenuModSettingsButtonPatch.ButtonSize,
                    MainMenuModSettingsButtonPatch.ButtonSize),
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Stop,
                PivotOffset = new(MainMenuModSettingsButtonPatch.ButtonSize / 2f,
                    MainMenuModSettingsButtonPatch.ButtonSize / 2f),
            };

            var material = CreateHsvMaterial();
            var icon = new TextureRect
            {
                Name = "Icon",
                Material = material,
                AnchorRight = 1f,
                AnchorBottom = 1f,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
                PivotOffset = button.PivotOffset,
                MouseFilter = MouseFilterEnum.Ignore,
                Texture = ResourceLoader.Load<Texture2D>(RitsuLibModImageResourceLoader.ModImagePath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            button.AddChild(icon);
            button.AddChild(CreateGearBadge());
            return button;
        }

        public override void _Ready()
        {
            ConnectSignals();
            _icon = GetNode<Control>("Icon");
            _hsv = _icon.Material as ShaderMaterial;
            _gear = GetNodeOrNull<Control>("GearBadge");
            _gearHsv = _gear?.Material as ShaderMaterial;
        }

        protected override void OnFocus()
        {
            base.OnFocus();
            _hsv?.SetShaderParameter(ShaderParamV, 1.2f);
            _gearHsv?.SetShaderParameter(ShaderParamV, 1.2f);
            if (_icon != null)
                _icon.RotationDegrees = 5f;
            if (_gear != null)
                _gear.RotationDegrees = 5f;
        }

        protected override void OnUnfocus()
        {
            base.OnUnfocus();
            _hsv?.SetShaderParameter(ShaderParamV, 1f);
            _gearHsv?.SetShaderParameter(ShaderParamV, 1f);
            if (_icon != null)
                _icon.RotationDegrees = 0f;
            if (_gear != null)
                _gear.RotationDegrees = 0f;
        }

        private static TextureRect CreateGearBadge()
        {
            const float size = MainMenuModSettingsButtonPatch.GearBadgeSize;
            const float right = MainMenuModSettingsButtonPatch.ButtonSize -
                                MainMenuModSettingsButtonPatch.GearBadgeInset;
            const float bottom = MainMenuModSettingsButtonPatch.ButtonSize -
                                 MainMenuModSettingsButtonPatch.GearBadgeInset;
            return new()
            {
                Name = "GearBadge",
                Material = CreateHsvMaterial(),
                AnchorLeft = 0f,
                AnchorTop = 0f,
                AnchorRight = 0f,
                AnchorBottom = 0f,
                OffsetLeft = right - size,
                OffsetTop = bottom - size,
                OffsetRight = right,
                OffsetBottom = bottom,
                PivotOffset = new(size / 2f, size / 2f),
                MouseFilter = MouseFilterEnum.Ignore,
                Texture = ResourceLoader.Load<Texture2D>(GearTexturePath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
        }

        private static ShaderMaterial CreateHsvMaterial()
        {
            var material = new ShaderMaterial
            {
                ResourceLocalToScene = true,
                Shader = ResourceLoader.Load<Shader>(HsvShaderPath),
            };
            material.SetShaderParameter("h", 1f);
            material.SetShaderParameter("s", 1f);
            material.SetShaderParameter(ShaderParamV, 1f);
            return material;
        }
    }
}
