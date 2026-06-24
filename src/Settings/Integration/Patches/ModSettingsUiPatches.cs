using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using STS2RitsuLib.Diagnostics;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Settings.Patches
{
    /// <summary>
    ///     Harmony patch that reuses one <see cref="RitsuModSettingsSubmenu" /> per
    ///     <see cref="NMainMenuSubmenuStack" /> instance.
    ///     Harmony patch：每个 <see cref="NMainMenuSubmenuStack" /> 实例复用一个
    ///     <see cref="RitsuModSettingsSubmenu" />。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class ModSettingsSubmenuPatch : IPatchMethod
    {
        internal static readonly ConditionalWeakTable<NSubmenuStack, RitsuModSettingsSubmenu> Submenus = new();
        public static string PatchId => "ritsulib_mod_settings_submenu";
        public static string Description => "Inject RitsuLib mod settings submenu into the main menu stack";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMainMenuSubmenuStack), nameof(NMainMenuSubmenuStack.GetSubmenuType), [typeof(Type)])];
        }

        public static bool Prefix(NMainMenuSubmenuStack __instance, Type type, ref NSubmenu __result)
        {
            if (type != typeof(RitsuModSettingsSubmenu))
                return true;

            __result = Submenus.GetValue(__instance, CreateSubmenu);
            return false;
        }

        internal static RitsuModSettingsSubmenu CreateSubmenu(NSubmenuStack stack)
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
    ///     Harmony patch that reuses one <see cref="RitsuModSettingsSubmenu" /> per
    ///     <see cref="NRunSubmenuStack" /> (in-run pause / settings), mirroring <see cref="ModSettingsSubmenuPatch" />.
    ///     Harmony patch：每个 <see cref="NRunSubmenuStack" />（跑局中暂停 / 设置）复用一个
    ///     <see cref="RitsuModSettingsSubmenu" />，对应 <see cref="ModSettingsSubmenuPatch" /> 的做法。
    /// </summary>
    internal class ModSettingsRunSubmenuStackPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_mod_settings_submenu_run_stack";
        public static string Description => "Inject RitsuLib mod settings submenu into the run submenu stack";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRunSubmenuStack), nameof(NRunSubmenuStack.GetSubmenuType), [typeof(Type)])];
        }

        public static bool Prefix(NRunSubmenuStack __instance, Type type, ref NSubmenu __result)
        {
            if (type != typeof(RitsuModSettingsSubmenu))
                return true;

            __result = ModSettingsSubmenuPatch.Submenus.GetValue(__instance, ModSettingsSubmenuPatch.CreateSubmenu);
            return false;
        }
    }

    /// <summary>
    ///     Injects the “Mod Settings (RitsuLib)” row into the vanilla settings screen and keeps general panel height in sync.
    ///     The entry is always shown regardless of registered page count; in-run access stays enabled.
    ///     将 “Mod Settings (RitsuLib)” 行注入原版设置屏幕，并保持 General 面板高度同步。
    ///     入口固定显示，与是否注册设置页无关；对局中保持可打开。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class SettingsScreenModSettingsButtonPatch : IPatchMethod
    {
        private const string GeneralSettingsResizeHookMeta = "ritsulib_general_settings_content_resize_hook";

        private const string EntryLineNodeName = "RitsuLibModSettings";

        private const string EntryDividerNodeName = "RitsuLibModSettingsDivider";

        private const string LogsLineNodeName = "RitsuLibOpenLogs";

        private const string LogsDividerNodeName = "RitsuLibOpenLogsDivider";

        public static string PatchId => "ritsulib_mod_settings_button";
        public static string Description => "Add RitsuLib mod settings entry point to the settings screen";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NSettingsScreen), nameof(NSettingsScreen._Ready)),
                new(typeof(NSettingsScreen), nameof(NSettingsScreen.OnSubmenuOpened)),
                new(typeof(NSettingsScreen), "OnSubmenuShown"),
            ];
        }

        public static void Postfix(NSettingsScreen __instance)
        {
            RitsuLibModSettingsBootstrap.EnsureFrameworkPagesRegistered();

            try
            {
                var line = EnsureEntryPoint(__instance);
                var logsLine = EnsureLogsEntryPoint(__instance);
                RefreshState(line, __instance);
                RefreshLogsState(logsLine);
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

            if (TryGetEntryLine(content) is { } existing)
            {
                if (content.GetNodeOrNull<Control>(EntryDividerNodeName) is { } existingDivider)
                    MoveEntryAboveNativeModSettings(content, existingDivider, existing);
                return existing;
            }

            RemoveStaleEntryNodes(content);

            var divider = ModSettingsUiFactory.CreateDivider();
            divider.Name = EntryDividerNodeName;

            var line = ModSettingsGameSettingsEntryLine.Create(OpenSubmenu);

            content.AddChild(divider);
            content.AddChild(line);

            MoveEntryAboveNativeModSettings(content, divider, line);

            return line;

            void OpenSubmenu()
            {
                screen.GetAncestorOfType<NSubmenuStack>()?.PushSubmenuType(typeof(RitsuModSettingsSubmenu));
            }
        }

        private static MarginContainer EnsureLogsEntryPoint(NSettingsScreen screen)
        {
            var panel = screen.GetNode<NSettingsPanel>("%GeneralSettings");
            var content = panel.Content;
            EnsureGeneralSettingsContentTracksChildAdds(content);

            if (TryGetLogsLine(content) is { } existing)
            {
                if (content.GetNodeOrNull<Control>(LogsDividerNodeName) is { } existingDivider)
                    MoveLogsEntryAboveCredits(content, existingDivider, existing);
                return existing;
            }

            RemoveStaleLogsNodes(content);

            var divider = ModSettingsUiFactory.CreateDivider();
            divider.Name = LogsDividerNodeName;

            var line = ModSettingsGameSettingsEntryLine.CreateOpenLogs(OpenLogsFolder);

            content.AddChild(divider);
            content.AddChild(line);

            MoveLogsEntryAboveCredits(content, divider, line);

            return line;

            static void OpenLogsFolder()
            {
                GameLogFolderOpener.OpenFromUi(
                    ModSettingsLocalization.Get("entry.openLogs.toastTitle", "Logs"),
                    "[Settings][OpenLogs]");
            }
        }

        private static void MoveEntryAboveNativeModSettings(
            VBoxContainer content,
            Control divider,
            MarginContainer line)
        {
            var anchor = content.GetNodeOrNull<Control>("ModdingDivider") ??
                         content.GetNodeOrNull<Control>("CreditsDivider");
            if (anchor == null)
                return;

            MoveChildBefore(content, line, anchor);
            MoveChildBefore(content, divider, line);
        }

        private static void MoveLogsEntryAboveCredits(
            VBoxContainer content,
            Control divider,
            MarginContainer line)
        {
            var anchor = content.GetNodeOrNull<Control>("CreditsDivider") ??
                         content.GetNodeOrNull<Control>("Credits");
            if (anchor == null)
                return;

            MoveChildBefore(content, line, anchor);
            MoveChildBefore(content, divider, line);
        }

        private static void MoveChildBefore(VBoxContainer content, Control child, Control anchor)
        {
            var targetIndex = anchor.GetIndex();
            if (child.GetIndex() < targetIndex)
                targetIndex--;

            content.MoveChild(child, targetIndex);
        }

        internal static MarginContainer? TryGetEntryLine(VBoxContainer content)
        {
            var line = content.GetNodeOrNull<MarginContainer>(EntryLineNodeName);
            if (line is null || !GodotObject.IsInstanceValid(line) || line.GetParent() != content)
                return null;

            return line;
        }

        internal static MarginContainer? TryGetLogsLine(VBoxContainer content)
        {
            var line = content.GetNodeOrNull<MarginContainer>(LogsLineNodeName);
            if (line is null || !GodotObject.IsInstanceValid(line) || line.GetParent() != content)
                return null;

            return line;
        }

        internal static bool HasInjectedGeneralSettingsEntry(VBoxContainer content)
        {
            return TryGetEntryLine(content) != null || TryGetLogsLine(content) != null;
        }

        private static void RemoveStaleEntryNodes(VBoxContainer content)
        {
            var divider = content.GetNodeOrNull<Control>(EntryDividerNodeName);
            if (divider != null && GodotObject.IsInstanceValid(divider))
                divider.QueueFree();

            var line = content.GetNodeOrNull<Control>(EntryLineNodeName);
            if (line != null && GodotObject.IsInstanceValid(line))
                line.QueueFree();
        }

        private static void RemoveStaleLogsNodes(VBoxContainer content)
        {
            var divider = content.GetNodeOrNull<Control>(LogsDividerNodeName);
            if (divider != null && GodotObject.IsInstanceValid(divider))
                divider.QueueFree();

            var line = content.GetNodeOrNull<Control>(LogsLineNodeName);
            if (line != null && GodotObject.IsInstanceValid(line))
                line.QueueFree();
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
            if (HasInjectedGeneralSettingsEntry(content))
                GeneralSettingsModEntryFocusWire.ScheduleTryWire(content);
        }

        private static void ScheduleRefreshGeneralSettingsPanelSize(NSettingsPanel panel)
        {
            Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(panel))
                    RefreshPanelSize(panel);
            }).CallDeferred();
        }

        private static void RefreshState(MarginContainer line, NSettingsScreen screen)
        {
            line.Visible = true;

            if (line.GetNodeOrNull<MegaRichTextLabel>("ContentRow/Label") is { } label)
                label.SetTextAutoSize(ModSettingsLocalization.Get("entry.title", "Mod Settings (RitsuLib)"));

            if (line.GetNodeOrNull<NButton>("ContentRow/RitsuLibModSettingsButton") is { } button)
                button.Enable();
        }

        private static void RefreshLogsState(MarginContainer line)
        {
            line.Visible = true;

            if (line.GetNodeOrNull<MegaRichTextLabel>("ContentRow/Label") is { } label)
                label.SetTextAutoSize(ModSettingsLocalization.Get("entry.openLogs.title", "Open Logs Folder"));

            if (line.GetNodeOrNull<NButton>("ContentRow/RitsuLibOpenLogsButton") is { } button)
                button.Enable();
        }

        /// <summary>
        ///     Mirrors <see cref="NSettingsPanel" />'s private refresh: when content exceeds the viewport (plus padding), panel
        ///     height becomes <c>contentMinY + parentHeight * 0.4f</c> for bottom scroll slack (game default).
        ///     复刻 <see cref="NSettingsPanel" /> 的私有刷新逻辑：当内容超过视口（加 padding）时，面板
        ///     高度变为 <c>contentMinY + parentHeight * 0.4f</c>，为底部滚动留出余量（游戏默认值）。
        /// </summary>
        private static void RefreshPanelSize(NSettingsPanel panel)
        {
            try
            {
                if (!GodotObject.IsInstanceValid(panel))
                    return;

                var content = panel.Content;
                if (!GodotObject.IsInstanceValid(content))
                    return;

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
        ///     可见直接子节点的 <see cref="Control.GetCombinedMinimumSize" /> 与 VBox 间距之和；
        ///     当根 VBox 上的 <see cref="Control.GetMinimumSize" /> 暂时过小时作为回退。
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
    ///     按 <see cref="NSettingsPanel" /> 相同的方式重建 General 标签页的垂直焦点链，
    ///     在注入我们的行后于 <c>_Ready</c> 中执行（原版不会看到这些新控件）。
    /// </summary>
    internal static class GeneralSettingsModEntryFocusWire
    {
        internal static void ScheduleTryWire(VBoxContainer content)
        {
            Callable.From(() =>
            {
                if (!GodotObject.IsInstanceValid(content))
                    return;

                TryRebuildEntireGeneralFocusChain(content);
                Callable.From(() =>
                {
                    if (GodotObject.IsInstanceValid(content))
                        TryRebuildEntireGeneralFocusChain(content);
                }).CallDeferred();
            }).CallDeferred();
        }

        internal static void TryRebuildEntireGeneralFocusChain(VBoxContainer content)
        {
            if (!GodotObject.IsInstanceValid(content))
                return;

            if (!SettingsScreenModSettingsButtonPatch.HasInjectedGeneralSettingsEntry(content))
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
