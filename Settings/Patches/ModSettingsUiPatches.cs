using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Settings.Patches
{
    public class ModSettingsSubmenuPatch : IPatchMethod
    {
        private static readonly ConditionalWeakTable<NMainMenuSubmenuStack, RitsuModSettingsSubmenu> Submenus = new();

        public static string PatchId => "ritsulib_mod_settings_submenu";
        public static string Description => "Inject RitsuLib mod settings submenu into the main menu stack";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMainMenuSubmenuStack), nameof(NMainMenuSubmenuStack.GetSubmenuType), [typeof(Type)])];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(NMainMenuSubmenuStack __instance, Type type, ref NSubmenu __result)
            // ReSharper restore InconsistentNaming
        {
            if (type != typeof(RitsuModSettingsSubmenu))
                return true;

            __result = Submenus.GetValue(__instance, CreateSubmenu);
            return false;
        }

        private static RitsuModSettingsSubmenu CreateSubmenu(NMainMenuSubmenuStack stack)
        {
            var submenu = new RitsuModSettingsSubmenu
            {
                Visible = false,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };

            stack.AddChildSafely(submenu);
            return submenu;
        }
    }

    public class SettingsScreenModSettingsButtonPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_mod_settings_button";
        public static string Description => "Add RitsuLib mod settings entry point to the settings screen";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NSettingsScreen), nameof(NSettingsScreen._Ready)),
                new(typeof(NSettingsScreen), nameof(NSettingsScreen.OnSubmenuOpened)),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NSettingsScreen __instance)
        {
            if (!ModSettingsRegistry.HasPages)
                return;

            try
            {
                var line = EnsureEntryPoint(__instance);
                RefreshState(line);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Settings] Failed to add mod settings entry point: {ex.Message}");
            }
        }

        private static MarginContainer EnsureEntryPoint(NSettingsScreen screen)
        {
            var panel = screen.GetNode<NSettingsPanel>("%GeneralSettings");
            var content = panel.Content;

            if (content.GetNodeOrNull<MarginContainer>("RitsuLibModSettings") is { } existing)
                return existing;

            var divider = ModSettingsUiFactory.CreateDivider();
            divider.Name = "RitsuLibModSettingsDivider";

            var line = ModSettingsUiFactory.CreateModdingScreenButtonLine(OpenSubmenu);

            content.AddChild(divider);
            content.AddChild(line);

            var creditsDivider = content.GetNodeOrNull<Control>("CreditsDivider");
            if (creditsDivider != null)
            {
                var targetIndex = creditsDivider.GetIndex();
                content.MoveChild(divider, targetIndex);
                content.MoveChild(line, targetIndex + 1);
            }

            RefreshPanelSize(panel);
            return line;

            void OpenSubmenu()
            {
                screen.GetAncestorOfType<NMainMenuSubmenuStack>()?.PushSubmenuType(typeof(RitsuModSettingsSubmenu));
            }
        }

        private static void RefreshState(MarginContainer line)
        {
            line.Visible = true;

            if (line.GetNodeOrNull<MegaRichTextLabel>("ContentRow/Label") is { } label)
                label.SetTextAutoSize(ModSettingsLocalization.Get("entry.title", "Mod Settings (RitsuLib)"));

            if (line.GetNodeOrNull<NButton>("ContentRow/RitsuLibModSettingsButton") is { } button)
                button.Enable();
        }

        private static void RefreshPanelSize(NSettingsPanel panel)
        {
            try
            {
                var content = panel.Content;
                content.QueueSort();

                var parentSize = panel.GetParent<Control>().Size;
                var minimumSize = content.GetMinimumSize();
                const float minPadding = 50f;
                panel.Size = minimumSize.Y + minPadding >= parentSize.Y
                    ? new(content.Size.X, minimumSize.Y + parentSize.Y * 0.4f)
                    : new Vector2(content.Size.X, minimumSize.Y);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Settings] Failed to refresh settings panel size: {ex.Message}");
            }
        }
    }
}
