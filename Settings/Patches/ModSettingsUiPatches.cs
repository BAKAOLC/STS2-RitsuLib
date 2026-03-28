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
    /// <summary>
    ///     Harmony patch that reuses one <see cref="RitsuModSettingsSubmenu" /> per
    ///     <see cref="NMainMenuSubmenuStack" /> instance.
    /// </summary>
    public class ModSettingsSubmenuPatch : IPatchMethod
    {
        private static readonly ConditionalWeakTable<NMainMenuSubmenuStack, RitsuModSettingsSubmenu> Submenus = new();

        /// <inheritdoc />
        public static string PatchId => "ritsulib_mod_settings_submenu";

        /// <inheritdoc />
        public static string Description => "Inject RitsuLib mod settings submenu into the main menu stack";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMainMenuSubmenuStack), nameof(NMainMenuSubmenuStack.GetSubmenuType), [typeof(Type)])];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Returns a cached <see cref="RitsuModSettingsSubmenu" /> for the stack when the requested type matches.
        /// </summary>
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

    /// <summary>
    ///     Injects the “Mod Settings (RitsuLib)” row into the vanilla settings screen and keeps general panel height in sync.
    /// </summary>
    public class SettingsScreenModSettingsButtonPatch : IPatchMethod
    {
        private const string GeneralSettingsResizeHookMeta = "ritsulib_general_settings_content_resize_hook";

        /// <inheritdoc />
        public static string PatchId => "ritsulib_mod_settings_button";

        /// <inheritdoc />
        public static string Description => "Add RitsuLib mod settings entry point to the settings screen";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NSettingsScreen), nameof(NSettingsScreen._Ready)),
                new(typeof(NSettingsScreen), nameof(NSettingsScreen.OnSubmenuOpened)),
            ];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Ensures the entry line exists, refreshes copy, and schedules panel height refresh when mod pages exist.
        /// </summary>
        public static void Postfix(NSettingsScreen __instance)
        {
            if (!ModSettingsRegistry.HasPages)
                return;

            try
            {
                var line = EnsureEntryPoint(__instance);
                RefreshState(line);
                var generalPanel = __instance.GetNode<NSettingsPanel>("%GeneralSettings");
                ScheduleRefreshGeneralSettingsPanelSize(generalPanel);
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
            EnsureGeneralSettingsContentTracksChildAdds(content);

            if (content.GetNodeOrNull<MarginContainer>("RitsuLibModSettings") is { } existing)
                return existing;

            var divider = ModSettingsUiFactory.CreateDivider();
            divider.Name = "RitsuLibModSettingsDivider";

            var line = ModSettingsGameSettingsEntryLine.Create(OpenSubmenu);

            content.AddChild(divider);
            content.AddChild(line);

            var creditsDivider = content.GetNodeOrNull<Control>("CreditsDivider");
            if (creditsDivider == null) return line;
            var targetIndex = creditsDivider.GetIndex();
            content.MoveChild(divider, targetIndex);
            content.MoveChild(line, targetIndex + 1);

            return line;

            void OpenSubmenu()
            {
                screen.GetAncestorOfType<NMainMenuSubmenuStack>()?.PushSubmenuType(typeof(RitsuModSettingsSubmenu));
            }
        }

        /// <summary>
        ///     Vanilla <see cref="NSettingsPanel" /> only recomputes height on ready and viewport resize; injected rows
        ///     (this mod and others) never trigger <c>RefreshSize</c>. Hooks <c>ChildEnteredTree</c> on the content
        ///     <see cref="VBoxContainer" /> so we recalculate after layout and whenever direct children change.
        /// </summary>
        private static void EnsureGeneralSettingsContentTracksChildAdds(VBoxContainer content)
        {
            if (content.HasMeta(GeneralSettingsResizeHookMeta))
                return;

            content.SetMeta(GeneralSettingsResizeHookMeta, true);
            content.ChildEnteredTree += OnGeneralSettingsContentChildEntered;
        }

        private static void OnGeneralSettingsContentChildEntered(Node child)
        {
            var content = child.GetParentOrNull<VBoxContainer>();
            var panel = content?.GetParentOrNull<NSettingsPanel>();
            if (panel is null)
                return;

            ScheduleRefreshGeneralSettingsPanelSize(panel);
        }

        private static void ScheduleRefreshGeneralSettingsPanelSize(NSettingsPanel panel)
        {
            Callable.From(() => RefreshPanelSize(panel)).CallDeferred();
        }

        private static void RefreshState(MarginContainer line)
        {
            line.Visible = true;

            if (line.GetNodeOrNull<MegaRichTextLabel>("ContentRow/Label") is { } label)
                label.SetTextAutoSize(ModSettingsLocalization.Get("entry.title", "Mod Settings (RitsuLib)"));

            if (line.GetNodeOrNull<NButton>("ContentRow/RitsuLibModSettingsButton") is { } button)
                button.Enable();
        }

        /// <summary>
        ///     Mirrors <see cref="NSettingsPanel" />'s private refresh: when content exceeds the viewport (plus padding), panel
        ///     height becomes <c>contentMinY + parentHeight * 0.4f</c> for bottom scroll slack (game default).
        /// </summary>
        private static void RefreshPanelSize(NSettingsPanel panel)
        {
            try
            {
                var content = panel.Content;
                content.QueueSort();

                var parent = panel.GetParent<Control>();
                if (parent is null)
                    return;

                var parentSize = parent.Size;
                var minimumSize = content.GetMinimumSize();
                var stackedMinY = ComputeVBoxContentMinHeight(content);
                var needHeightY = Mathf.Max(minimumSize.Y, stackedMinY);
                const float minPadding = 50f;
                var width = content.Size.X > 1f ? content.Size.X : parentSize.X;
                panel.Size = needHeightY + minPadding >= parentSize.Y
                    ? new(width, needHeightY + parentSize.Y * 0.4f)
                    : new Vector2(width, needHeightY);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Settings] Failed to refresh settings panel size: {ex.Message}");
            }
        }

        /// <summary>
        ///     Sum of visible direct children's <see cref="Control.GetCombinedMinimumSize" /> and VBox separation;
        ///     fallback when <see cref="Control.GetMinimumSize" /> on the root VBox is temporarily too small.
        /// </summary>
        private static float ComputeVBoxContentMinHeight(VBoxContainer box)
        {
            var sep = box.GetThemeConstant("separation");
            var y = 0f;
            var first = true;
            foreach (var node in box.GetChildren())
            {
                if (node is not Control { Visible: true } c)
                    continue;

                if (!first)
                    y += sep;
                first = false;
                y += c.GetCombinedMinimumSize().Y;
            }

            return y;
        }
    }
}
