using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsMirrorUiActions
    {
        public static void ConfirmAndRestoreDefaults(Action restoreDefaults, Action? afterRestore, string header,
            string body,
            string cancelText, string confirmText)
        {
            if (Engine.GetMainLoop() is not SceneTree { Root: { } root })
            {
                restoreDefaults();
                afterRestore?.Invoke();
                return;
            }

            var submenu = FindRitsuModSettingsSubmenu(root);
            var attachParent = (Node?)submenu ?? root;
            ModSettingsUiFactory.ShowStyledConfirm(attachParent, header, body, cancelText, confirmText, true, () =>
            {
                restoreDefaults();
                afterRestore?.Invoke();
                submenu?.RequestRefresh();
            });
        }

        public static RitsuModSettingsSubmenu? FindRitsuModSettingsSubmenu(Node root)
        {
            var queue = new Queue<Node>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node is RitsuModSettingsSubmenu submenu)
                    return submenu;

                foreach (var child in node.GetChildren())
                    queue.Enqueue(child);
            }

            return null;
        }

        public static string GetLocalizedOrFallback(string key, string fallback)
        {
            return LocString.GetIfExists("settings_ui", key)?.GetFormattedText() ?? fallback;
        }
    }
}
