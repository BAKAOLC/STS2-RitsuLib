using STS2RitsuLib.RuntimeInput;

namespace STS2RitsuLib.Settings
{
    internal static partial class RitsuLibModSettingsBootstrap
    {
        // ReSharper disable once UnusedMethodReturnValue.Local
        private static ModSettingsPageBuilder ConfigureRuntimeHotkeysPage(
            ModSettingsPageBuilder page,
            IReadOnlyList<string> categoryOrder)
        {
            page
                .AsChildOf(Const.ModId)
                .WithSortOrder(-175)
                .WithMenuCapabilities(ModSettingsMenuCapabilities.Copy)
                .WithTitle(T("ritsulib.page.runtimeHotkeys.title", "Registered hotkeys"))
                .WithDescription(ModSettingsText.Dynamic(() => string.Format(
                                                                   L("ritsulib.runtimeHotkeys.summary",
                                                                       "{0} runtime hotkeys are currently registered."),
                                                                   RuntimeHotkeyService.GetRegisteredHotkeys().Count) +
                                                               " " +
                                                               L("ritsulib.runtimeHotkeys.groups.description",
                                                                   "View shortcuts by category. Change bindings in each mod's settings.")));

            var groups = GetOrderedRuntimeHotkeyGroups(categoryOrder);
            if (groups.Count == 0)
            {
                page.AddSection("runtime_hotkeys_empty", section => section
                    .WithMenuCapabilities(ModSettingsMenuCapabilities.Copy)
                    .WithTitle(T("ritsulib.section.runtimeHotkeys.empty.title", "No registered hotkeys"))
                    .AddParagraph(
                        "runtime_hotkeys_empty_text",
                        T("ritsulib.runtimeHotkeys.empty", "No runtime hotkeys are currently registered.")));
                return page;
            }

            foreach (var (category, runtimeHotkeyRegistrationInfos) in groups)
            {
                var sectionId = $"runtime_hotkeys_group_{SanitizeRuntimeHotkeySectionId(category)}";
                page.AddSection(sectionId, section =>
                {
                    section
                        .WithMenuCapabilities(ModSettingsMenuCapabilities.Copy)
                        .WithTitle(ModSettingsText.Dynamic(() =>
                            $"{category} · {CountRuntimeHotkeysInCategory(category)}"))
                        .Collapsible();

                    for (var i = 0; i < runtimeHotkeyRegistrationInfos.Count; i++)
                    {
                        var hotkey = runtimeHotkeyRegistrationInfos[i];
                        var entryIdBase = $"{i}_{SanitizeRuntimeHotkeySectionId(GetRuntimeHotkeyStableKey(hotkey))}";
                        section.AddRuntimeHotkeySummary(
                            $"{entryIdBase}_card",
                            ModSettingsText.Dynamic(() => GetRuntimeHotkeyCardTitle(hotkey)),
                            ModSettingsText.Dynamic(() => FormatRuntimeHotkeyDetails(hotkey.Id, hotkey.CurrentBinding,
                                hotkey.DisplayName)),
                            GetRuntimeHotkeyBindingTexts(hotkey));
                    }
                });
            }

            return page;
        }

        private static List<RuntimeHotkeyCategoryGroup> GetOrderedRuntimeHotkeyGroups(
            IReadOnlyList<string> categoryOrder)
        {
            var hotkeys = RuntimeHotkeyService.GetRegisteredHotkeys();
            return
            [
                .. hotkeys
                    .GroupBy(info => string.IsNullOrWhiteSpace(info.Category)
                        ? L("ritsulib.runtimeHotkeys.category.other", "Other")
                        : info.Category!)
                    .OrderBy(group =>
                    {
                        var index = -1;
                        for (var i = 0; i < categoryOrder.Count; i++)
                        {
                            if (!string.Equals(categoryOrder[i], group.Key, StringComparison.Ordinal))
                                continue;
                            index = i;
                            break;
                        }

                        return index < 0 ? int.MaxValue : index;
                    })
                    .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(group => new RuntimeHotkeyCategoryGroup(
                        group.Key,
                        [
                            .. group.OrderBy(info => info.DisplayName ?? info.Id ?? info.CurrentBinding,
                                StringComparer.OrdinalIgnoreCase),
                        ])),
            ];
        }

        private static int CountRuntimeHotkeysInCategory(string category)
        {
            return RuntimeHotkeyService.GetRegisteredHotkeys().Count(info => string.Equals(
                string.IsNullOrWhiteSpace(info.Category)
                    ? L("ritsulib.runtimeHotkeys.category.other", "Other")
                    : info.Category,
                category,
                StringComparison.Ordinal));
        }

        private static string GetRuntimeHotkeyCardTitle(RuntimeHotkeyRegistrationInfo hotkey)
        {
            return FindRuntimeHotkey(hotkey.Id, hotkey.CurrentBinding, hotkey.DisplayName) is { } liveHotkey
                ? liveHotkey.DisplayName ?? liveHotkey.Id ?? liveHotkey.CurrentBinding
                : hotkey.DisplayName ?? hotkey.Id ?? hotkey.CurrentBinding;
        }

        private static IReadOnlyList<ModSettingsText> GetRuntimeHotkeyBindingTexts(RuntimeHotkeyRegistrationInfo hotkey)
        {
            return
            [
                .. Enumerable.Range(0, Math.Max(1, hotkey.CurrentBindings.Count))
                    .Select(index => ModSettingsText.Dynamic(() =>
                    {
                        var liveHotkey = FindRuntimeHotkey(hotkey.Id, hotkey.CurrentBinding, hotkey.DisplayName);
                        return liveHotkey != null && index < liveHotkey.CurrentBindings.Count
                            ? liveHotkey.CurrentBindings[index]
                            : L("keybinding.unbound", "Unbound");
                    })),
            ];
        }

        private static string GetRuntimeHotkeyStableKey(RuntimeHotkeyRegistrationInfo hotkey)
        {
            return hotkey.Id ?? hotkey.DisplayName ?? hotkey.CurrentBinding;
        }

        private static string FormatRuntimeHotkeyDetails(string? id, string binding, string? displayName)
        {
            var hotkey = FindRuntimeHotkey(id, binding, displayName);
            return hotkey?.Description ?? string.Empty;
        }

        private static RuntimeHotkeyRegistrationInfo? FindRuntimeHotkey(string? id, string binding, string? displayName)
        {
            var hotkeys = RuntimeHotkeyService.GetRegisteredHotkeys();
            if (!string.IsNullOrWhiteSpace(id))
                foreach (var info in hotkeys)
                    if (string.Equals(info.Id, id, StringComparison.Ordinal))
                        return info;

            foreach (var info in hotkeys)
            {
                if (!string.Equals(info.CurrentBinding, binding, StringComparison.Ordinal))
                    continue;
                if (!string.Equals(info.DisplayName, displayName, StringComparison.Ordinal))
                    continue;
                return info;
            }

            return hotkeys.FirstOrDefault(info =>
                string.Equals(info.CurrentBinding, binding, StringComparison.Ordinal));
        }

        private static string SanitizeRuntimeHotkeySectionId(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "other";

            var chars = value
                .Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_')
                .ToArray();
            var sanitized = new string(chars).Trim('_');
            return string.IsNullOrWhiteSpace(sanitized) ? "other" : sanitized;
        }

        private sealed record RuntimeHotkeyCategoryGroup(string Category, List<RuntimeHotkeyRegistrationInfo> Hotkeys);
    }
}
