using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
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
                FocusMode = Control.FocusModeEnum.None,
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
                if (generalPanel.Content is { } generalVBox)
                    GeneralSettingsModEntryFocusWire.ScheduleTryWire(generalVBox);
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
            // ReSharper disable once UseNullPropagation
            if (content is null)
                return;

            var panel = content.GetParentOrNull<NSettingsPanel>();
            if (panel is null)
                return;

            ScheduleRefreshGeneralSettingsPanelSize(panel);
            if (content.GetNodeOrNull("RitsuLibModSettings") != null)
                GeneralSettingsModEntryFocusWire.ScheduleTryWire(content);
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

    /// <summary>
    ///     Rebuilds the General tab vertical focus chain the same way <see cref="NSettingsPanel" /> does in
    ///     <c>_Ready</c>, after our row is injected (vanilla never sees the new controls).
    /// </summary>
    internal static class GeneralSettingsModEntryFocusWire
    {
        internal static void ScheduleTryWire(VBoxContainer content)
        {
            Callable.From(() =>
            {
                TryRebuildEntireGeneralFocusChain(content);
                Callable.From(() => TryRebuildEntireGeneralFocusChain(content)).CallDeferred();
            }).CallDeferred();
        }

        internal static void TryRebuildEntireGeneralFocusChain(VBoxContainer content)
        {
            if (content.GetNodeOrNull("RitsuLibModSettings") == null)
                return;

            var list = new List<Control>();
            GetSettingsOptionsRecursive(content, list);
            if (list.Count == 0)
                return;

            for (var i = 0; i < list.Count; i++)
            {
                var current = list[i];
                current.FocusNeighborLeft = current.GetPath();
                current.FocusNeighborRight = current.GetPath();
                current.FocusNeighborTop = i > 0 ? list[i - 1].GetPath() : current.GetPath();
                current.FocusNeighborBottom = i < list.Count - 1 ? list[i + 1].GetPath() : current.GetPath();
            }
        }

        private static void GetSettingsOptionsRecursive(Control parent, List<Control> ancestors)
        {
            foreach (var child in parent.GetChildren())
            {
                if (child is not Control item)
                    continue;

                if (!IsVanillaGeneralSettingsFocusTarget(item))
                    GetSettingsOptionsRecursive(item, ancestors);
                else if (item.GetParent<Control>() is { } itemParent &&
                         itemParent.IsVisible() &&
                         item.FocusMode == Control.FocusModeEnum.All)
                    ancestors.Add(item);
            }
        }

        private static bool IsVanillaGeneralSettingsFocusTarget(Control c)
        {
            if (c is NButton nButton)
                return nButton.IsEnabled;

            return c is NPaginator or NTickbox or NButton or NDropdownPositioner or NSettingsSlider;
        }
    }
}
