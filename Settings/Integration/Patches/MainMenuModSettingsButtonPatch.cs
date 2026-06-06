using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using STS2RitsuLib.Data;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Godot;

namespace STS2RitsuLib.Settings.Patches
{
    /// <summary>
    ///     Injects the RitsuLib mod settings shortcut under the vanilla patch notes button on the main menu.
    ///     在主菜单原版更新日志按钮下方注入 RitsuLib 模组设置快捷入口。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    public class MainMenuModSettingsButtonPatch : IPatchMethod
    {
        private const string GroupNodeName = "RitsuLibMainMenuModSettings";
        private const string ButtonNodeName = "RitsuLibMainMenuModSettingsButton";
        private const string VersionLabelNodeName = "RitsuLibMainMenuModSettingsVersion";

        internal const float ButtonSize = 64f;
        private const float GapBelowPatchNotes = 8f;
        private const float VersionLabelTop = 68f;
        private const float VersionLabelHeight = 34f;
        private const float VersionLabelHorizontalBleed = 28f;

        /// <inheritdoc />
        public static string PatchId => "ritsulib_main_menu_mod_settings_button";

        /// <inheritdoc />
        public static string Description =>
            "Add the RitsuLib mod settings shortcut under the main menu patch notes button";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NMainMenu), nameof(NMainMenu._Ready)),
                new(typeof(NMainMenu), "OnSubmenuStackChanged"),
            ];
        }

        /// <summary>
        ///     Ensures the shortcut exists and mirrors vanilla main-menu submenu visibility state.
        ///     确保快捷入口存在，并同步原版主菜单子菜单打开时的可见状态。
        /// </summary>
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
                RefreshVersionLabel(existing);
                return existing;
            }

            RitsuLibModImageResourceLoader.EnsureRegistered();
            var group = new Control
            {
                Name = GroupNodeName,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                CustomMinimumSize = new(ButtonSize, VersionLabelTop + VersionLabelHeight),
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
            return group;
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
            group.OffsetBottom = group.OffsetTop + VersionLabelTop + VersionLabelHeight;

            if (group.GetNodeOrNull<RitsuLibMainMenuModSettingsButton>(ButtonNodeName) is { } button)
            {
                button.OffsetLeft = 0f;
                button.OffsetTop = 0f;
                button.OffsetRight = ButtonSize;
                button.OffsetBottom = ButtonSize;
            }

            if (group.GetNodeOrNull<Label>(VersionLabelNodeName) is not { } label) return;
            label.OffsetLeft = -VersionLabelHorizontalBleed;
            label.OffsetTop = VersionLabelTop;
            label.OffsetRight = ButtonSize + VersionLabelHorizontalBleed;
            label.OffsetBottom = VersionLabelTop + VersionLabelHeight;
        }

        private static Label CreateVersionLabel()
        {
            var label = new Label
            {
                Name = VersionLabelNodeName,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                AutowrapMode = TextServer.AutowrapMode.Off,
            };
            label.AddThemeColorOverride("font_color", new(0.91f, 0.86359f, 0.7462f));
            label.AddThemeColorOverride("font_shadow_color", new(0f, 0f, 0f, 0.72f));
            label.AddThemeConstantOverride("shadow_offset_x", 2);
            label.AddThemeConstantOverride("shadow_offset_y", 2);
            label.AddThemeFontSizeOverride("font_size", 12);
            RefreshVersionLabel(label);
            return label;
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

        private static void SyncState(NMainMenu mainMenu, Control? group)
        {
            if (group == null || !GodotObject.IsInstanceValid(group))
                return;

            var shouldShow = RitsuLibSettingsStore.IsMainMenuModSettingsButtonEnabled() &&
                             !mainMenu.SubmenuStack.SubmenusOpen;
            group.Visible = shouldShow;
            if (group.GetNodeOrNull<RitsuLibMainMenuModSettingsButton>(ButtonNodeName) is { } button)
                button.SetEnabled(shouldShow);
        }

        private static void OpenModSettings(NMainMenu mainMenu)
        {
            if (!GodotObject.IsInstanceValid(mainMenu))
                return;

            RitsuLibModSettingsBootstrap.EnsureFrameworkPagesRegistered();
            mainMenu.SubmenuStack.PushSubmenuType<RitsuModSettingsSubmenu>();
        }
    }

    internal sealed partial class RitsuLibMainMenuModSettingsButton : NButton
    {
        private const string HsvShaderPath = "res://shaders/hsv.gdshader";
        private static readonly StringName ShaderParamV = new("v");

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
            return button;
        }

        public override void _Ready()
        {
            ConnectSignals();
            _icon = GetNode<Control>("Icon");
            _hsv = _icon.Material as ShaderMaterial;
        }

        protected override void OnFocus()
        {
            base.OnFocus();
            _hsv?.SetShaderParameter(ShaderParamV, 1.2f);
            if (_icon != null)
                _icon.RotationDegrees = 5f;
        }

        protected override void OnUnfocus()
        {
            base.OnUnfocus();
            _hsv?.SetShaderParameter(ShaderParamV, 1f);
            if (_icon != null)
                _icon.RotationDegrees = 0f;
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
