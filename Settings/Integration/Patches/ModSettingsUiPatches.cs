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
    public class ModSettingsSubmenuPatch : IPatchMethod
    {
        internal static readonly ConditionalWeakTable<NSubmenuStack, RitsuModSettingsSubmenu> Submenus = new();

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
        ///     当请求的类型匹配时，为该 stack 返回缓存的 <see cref="RitsuModSettingsSubmenu" />。
        /// </summary>
        public static bool Prefix(NMainMenuSubmenuStack __instance, Type type, ref NSubmenu __result)
            // ReSharper restore InconsistentNaming
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
    public class ModSettingsRunSubmenuStackPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_mod_settings_submenu_run_stack";

        /// <inheritdoc />
        public static string Description => "Inject RitsuLib mod settings submenu into the run submenu stack";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRunSubmenuStack), nameof(NRunSubmenuStack.GetSubmenuType), [typeof(Type)])];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Returns a cached <see cref="RitsuModSettingsSubmenu" /> for the run stack when the requested type matches.
        ///     当请求的类型匹配时，为跑局 stack 返回缓存的 <see cref="RitsuModSettingsSubmenu" />。
        /// </summary>
        public static bool Prefix(NRunSubmenuStack __instance, Type type, ref NSubmenu __result)
            // ReSharper restore InconsistentNaming
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
    public class SettingsScreenModSettingsButtonPatch : IPatchMethod
    {
        private const string GeneralSettingsResizeHookMeta = "ritsulib_general_settings_content_resize_hook";

        private const string PrewarmScheduledMeta = "ritsulib_mod_settings_prewarm_scheduled";

        private const string EntryLineNodeName = "RitsuLibModSettings";

        private const string EntryDividerNodeName = "RitsuLibModSettingsDivider";

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
                new(typeof(NSettingsScreen), "OnSubmenuShown"),
            ];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Ensures the entry line exists and refreshes chrome on ready, open, and show.
        ///     在 ready、open、show 时确保入口行存在并刷新外观。
        /// </summary>
        public static void Postfix(NSettingsScreen __instance)
        {
            RitsuLibModSettingsBootstrap.EnsureFrameworkPagesRegistered();

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

            TrySchedulePrewarm(__instance);
        }

        /// <summary>
        ///     Pre-warms the mod settings UI while the user is still on the vanilla settings screen — before they
        ///     click "Mod Settings (RitsuLib)". The first open otherwise runs a concentrated one-time
        ///     initialization (reflection-based mirror registration, sidebar build, first page build) all at once,
        ///     producing a visible stall. The work is spread across idle frames so the vanilla screen does not
        ///     stall either, and is scheduled once per screen instance.
        ///     在用户仍处于原版设置界面时(即点击 “Mod Settings (RitsuLib)” 之前)预热 mod 设置 UI。否则首次打开会一次性执行
        ///     集中的一次性初始化(基于反射的镜像注册、侧边栏构建、首页构建),造成可见卡顿。该工作被分散到多个空闲帧,使原版界面
        ///     也不卡,并对每个界面实例只调度一次。
        /// </summary>
        private static void TrySchedulePrewarm(NSettingsScreen screen)
        {
            if (screen.HasMeta(PrewarmScheduledMeta))
                return;
            screen.SetMeta(PrewarmScheduledMeta, true);
            Callable.From(() => PrewarmStep(screen, 0)).CallDeferred();
        }

        private static void PrewarmStep(NSettingsScreen screen, int step)
        {
            if (!GodotObject.IsInstanceValid(screen))
                return;

            try
            {
                switch (step)
                {
                    case 0:
                        RitsuLibModSettingsBootstrap.EnsureFrameworkPagesRegistered();
                        break;
                    case 1:
                        ModSettingsMirrorRegistrarBootstrap.TryRegisterMirroredPages();
                        RitsuLibModSettingsBootstrap.RefreshDynamicPages();
                        break;
                    case 2:
                    {
                        var stack = screen.GetAncestorOfType<NSubmenuStack>();
                        if (stack != null)
                            ModSettingsSubmenuPatch.Submenus.GetValue(stack, ModSettingsSubmenuPatch.CreateSubmenu);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Settings] Mod settings prewarm step {step} failed: {ex.Message}");
            }

            Callable.From(() => PrewarmStep(screen, step + 1)).CallDeferred();
        }

        private static MarginContainer EnsureEntryPoint(NSettingsScreen screen)
        {
            var panel = screen.GetNode<NSettingsPanel>("%GeneralSettings");
            var content = panel.Content;
            EnsureGeneralSettingsContentTracksChildAdds(content);

            if (TryGetEntryLine(content) is { } existing)
                return existing;

            RemoveStaleEntryNodes(content);

            var divider = ModSettingsUiFactory.CreateDivider();
            divider.Name = EntryDividerNodeName;

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
                screen.GetAncestorOfType<NSubmenuStack>()?.PushSubmenuType(typeof(RitsuModSettingsSubmenu));
            }
        }

        internal static MarginContainer? TryGetEntryLine(VBoxContainer content)
        {
            var line = content.GetNodeOrNull<MarginContainer>(EntryLineNodeName);
            if (line is null || !GodotObject.IsInstanceValid(line) || line.GetParent() != content)
                return null;

            return line;
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
            if (TryGetEntryLine(content) != null)
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
        ///     复刻 <see cref="NSettingsPanel" /> 的私有刷新逻辑：当内容超过视口（加 padding）时，面板
        ///     高度变为 <c>contentMinY + parentHeight * 0.4f</c>，为底部滚动留出余量（游戏默认值）。
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
                TryRebuildEntireGeneralFocusChain(content);
                Callable.From(() => TryRebuildEntireGeneralFocusChain(content)).CallDeferred();
            }).CallDeferred();
        }

        internal static void TryRebuildEntireGeneralFocusChain(VBoxContainer content)
        {
            if (SettingsScreenModSettingsButtonPatch.TryGetEntryLine(content) == null)
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
