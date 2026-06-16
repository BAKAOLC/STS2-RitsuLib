using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Ui.Toast;
using CurrentModEntry = STS2RitsuLib.Compat.ContentModLoadOrderInventory.CurrentModEntry;

namespace STS2RitsuLib.Settings
{
    internal static class ContentModLoadOrderCoordinator
    {
        private const string LogPrefix = "[ContentModLoadOrder]";
        private const string ToastTitleKey = "ritsulib.contentModLoadOrder.toast.title";
        private const string ToastTitleFallback = "Content mod load order";

        internal static void SortDeterministically()
        {
            var currentOrder = ContentModLoadOrderInventory.BuildCurrentOrder();
            var relevantKeys = ContentModLoadOrderInventory.BuildRelevantKeys(currentOrder);
            if (relevantKeys.Count == 0)
            {
                ShowWarning("ritsulib.contentModLoadOrder.toast.noContentMods",
                    "No content-affecting mods were found.");
                return;
            }

            var priorityById = BuildDeterministicPriorityById(currentOrder, relevantKeys);
            var applied = ApplyPriorityOrder(currentOrder, priorityById);
            SaveAndLog("deterministic-sort", currentOrder, applied, priorityById.Keys.ToArray());
            ShowInfo("ritsulib.contentModLoadOrder.toast.sorted",
                "Saved deterministic order for {0} relevant mod(s). Restart the game to apply it.",
                relevantKeys.Count);
        }

        internal static void CopyCurrentOrder()
        {
            var currentOrder = ContentModLoadOrderInventory.BuildCurrentOrder();
            var relevantKeys = ContentModLoadOrderInventory.BuildRelevantKeys(currentOrder);
            var copiedOrder = currentOrder
                .Where(entry => relevantKeys.Contains(entry.Key))
                .Select(entry => entry.Id)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (copiedOrder.Length == 0)
            {
                ShowWarning("ritsulib.contentModLoadOrder.toast.noContentMods",
                    "No content-affecting mods were found.");
                return;
            }

            DisplayServer.ClipboardSet(string.Join('\n', copiedOrder));
            ModSettingsClipboardAccess.InvalidateCache();
            ShowInfo("ritsulib.contentModLoadOrder.toast.copied",
                "Copied {0} relevant mod id(s) to the clipboard.",
                copiedOrder.Length);
        }

        internal static void ApplyClipboardOrder()
        {
            if (!ModSettingsClipboardAccess.TryGetText(out var text) || string.IsNullOrWhiteSpace(text))
            {
                ShowWarning("ritsulib.contentModLoadOrder.toast.clipboardEmpty",
                    "Clipboard is empty or unavailable.");
                return;
            }

            var requestedOrder = ParseOrderText(text);
            if (requestedOrder.Count == 0)
            {
                ShowWarning("ritsulib.contentModLoadOrder.toast.noIds",
                    "Clipboard does not contain any mod ids.");
                return;
            }

            var before = ContentModLoadOrderInventory.BuildCurrentOrder();
            var currentIds = before
                .Select(entry => entry.Id)
                .ToHashSet(StringComparer.Ordinal);
            var filteredOrder = requestedOrder
                .Where(currentIds.Contains)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (filteredOrder.Length == 0)
            {
                ShowWarning("ritsulib.contentModLoadOrder.toast.noMatchingIds",
                    "Clipboard order did not match any installed mods.");
                return;
            }

            var priorityById = filteredOrder
                .Select((id, index) => (id, index))
                .ToDictionary(item => item.id, item => item.index, StringComparer.Ordinal);
            var applied = ApplyPriorityOrder(before, priorityById);
            SaveAndLog("clipboard-order", before, applied, filteredOrder);

            var missingCount = requestedOrder.Distinct(StringComparer.Ordinal)
                .Count(id => !currentIds.Contains(id));
            ShowInfo("ritsulib.contentModLoadOrder.toast.applied",
                "Applied order for {0} installed mod(s). Missing ids ignored: {1}. Restart the game to apply it.",
                filteredOrder.Length,
                missingCount);
        }

        internal static string FormatCurrentOrderPreview()
        {
            var currentOrder = ContentModLoadOrderInventory.BuildCurrentOrder();
            var relevantKeys = ContentModLoadOrderInventory.BuildRelevantKeys(currentOrder);
            var entries = currentOrder
                .Where(entry => relevantKeys.Contains(entry.Key))
                .Select(entry => entry.Id)
                .ToArray();
            return FormatOrderPreview(entries);
        }

        internal static string FormatDeterministicOrderPreview()
        {
            return FormatOrderPreview(BuildRelevantTargetOrder(ContentModLoadOrderInventory.BuildCurrentOrder())
                .Select(entry => entry.Id)
                .ToArray());
        }

        internal static ContentModLoadOrderPreview BuildPreview()
        {
            var currentOrder = ContentModLoadOrderInventory.BuildCurrentOrder();
            var relevantKeys = ContentModLoadOrderInventory.BuildRelevantKeys(currentOrder);
            var relevantDependencyIds =
                ContentModLoadOrderInventory.BuildRelevantDependencyIds(currentOrder, relevantKeys);
            var currentEntries = currentOrder
                .Where(entry => relevantKeys.Contains(entry.Key))
                .ToArray();
            var currentById = currentEntries
                .Select((entry, index) => (entry, position: index + 1))
                .GroupBy(item => item.entry.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var targetEntries = BuildRelevantTargetOrder(currentOrder).ToArray();
            var targetById = targetEntries
                .Select((entry, index) => (entry.Id, position: index + 1))
                .GroupBy(item => item.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().position, StringComparer.Ordinal);

            var currentPreview = currentEntries
                .Select((entry, index) => new ContentModLoadOrderPreviewEntry(
                    entry.Id,
                    entry.DisplayName,
                    entry.AffectsGameplay,
                    relevantDependencyIds.Contains(entry.Id),
                    index + 1,
                    targetById.GetValueOrDefault(entry.Id)))
                .ToArray();
            var targetPreview = targetEntries
                .Select((entry, index) =>
                {
                    var source = currentById[entry.Id];
                    return new ContentModLoadOrderPreviewEntry(
                        entry.Id,
                        entry.DisplayName,
                        entry.AffectsGameplay,
                        relevantDependencyIds.Contains(entry.Id),
                        index + 1,
                        source.position);
                })
                .ToArray();

            return new(currentPreview, targetPreview);
        }

        private static IReadOnlyList<CurrentModEntry> BuildRelevantTargetOrder(
            IReadOnlyList<CurrentModEntry> currentOrder)
        {
            var relevantKeys = ContentModLoadOrderInventory.BuildRelevantKeys(currentOrder);
            var priorityById = BuildDeterministicPriorityById(currentOrder, relevantKeys);
            return ContentModLoadOrderInventory.BuildDependencyValidPriorityOrder(currentOrder, priorityById, LogPrefix)
                .Where(entry => relevantKeys.Contains(entry.Key))
                .ToArray();
        }

        private static IReadOnlyDictionary<string, int> BuildDeterministicPriorityById(
            IReadOnlyList<CurrentModEntry> currentOrder,
            IReadOnlySet<string> relevantKeys)
        {
            var relevantIds = currentOrder
                .Where(entry => relevantKeys.Contains(entry.Key))
                .Select(entry => entry.Id)
                .ToHashSet(StringComparer.Ordinal);
            var relevantDependencyIds =
                ContentModLoadOrderInventory.BuildRelevantDependencyIds(currentOrder, relevantKeys);
            return currentOrder
                .Where(entry => relevantKeys.Contains(entry.Key))
                .GroupBy(entry => entry.Id, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(GetDeterministicGroup)
                .ThenBy(entry => entry.Id, ContentModLoadOrderInventory.ModIdComparer.Instance)
                .Select((entry, index) => (entry.Id, index))
                .ToDictionary(item => item.Id, item => item.index, StringComparer.Ordinal);

            int GetDeterministicGroup(CurrentModEntry entry)
            {
                if (relevantDependencyIds.Contains(entry.Id))
                    return 0;

                return entry.Dependencies.Any(relevantIds.Contains) ? 1 : 2;
            }
        }

        private static IReadOnlyList<CurrentModEntry> ApplyPriorityOrder(
            IReadOnlyList<CurrentModEntry> currentOrder,
            IReadOnlyDictionary<string, int> priorityById)
        {
            var settings = SaveManager.Instance.SettingsSave;
            settings.ModSettings ??= new();

            var ordered =
                ContentModLoadOrderInventory.BuildDependencyValidPriorityOrder(currentOrder, priorityById, LogPrefix);

            settings.ModSettings.ModList = ordered
                .Select(entry => new SettingsSaveMod
                {
                    Id = entry.Id,
                    Source = entry.Source,
                    IsEnabled = entry.IsEnabled,
                })
                .ToList();
            return ordered;
        }

        private static void SaveAndLog(
            string operation,
            IReadOnlyList<CurrentModEntry> before,
            IReadOnlyList<CurrentModEntry> applied,
            IReadOnlyList<string> requestedPriorityOrder)
        {
            RitsuLibFramework.Logger.Info(
                $"{LogPrefix} Applying {operation}. requestedPriority=[{string.Join(", ", requestedPriorityOrder)}]");
            RitsuLibFramework.Logger.Info(
                $"{LogPrefix} Before mod_list priority=[{FormatLogOrder(before)}]");
            RitsuLibFramework.Logger.Info(
                $"{LogPrefix} Written mod_list priority=[{FormatLogOrder(applied)}]");
            SaveManager.Instance.SaveSettings();
            var saved = SaveManager.Instance.SettingsSave.ModSettings?.ModList ?? [];
            RitsuLibFramework.Logger.Info(
                $"{LogPrefix} SaveSettings called. inMemorySavedModList=[{string.Join(", ", saved.Select(entry => entry.Id))}]");
        }

        private static IReadOnlyList<string> ParseOrderText(string text)
        {
            try
            {
                if (JsonSerializer.Deserialize<string[]>(text) is { Length: > 0 } jsonIds)
                    return NormalizeIds(jsonIds);
            }
            catch
            {
                // Plain line-delimited text is the primary format.
            }

            return NormalizeIds(text.Split(['\r', '\n', ',', ';', '\t'], StringSplitOptions.RemoveEmptyEntries));
        }

        private static IReadOnlyList<string> NormalizeIds(IEnumerable<string> ids)
        {
            return ids
                .Select(id => id.Trim())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static string FormatOrderPreview(IReadOnlyList<string> orderedIds)
        {
            if (orderedIds.Count == 0)
                return L("ritsulib.contentModLoadOrder.preview.empty",
                    "No content-affecting mods were found.");

            var namesById = ContentModLoadOrderInventory.BuildCurrentOrder()
                .GroupBy(entry => entry.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().DisplayName, StringComparer.Ordinal);
            return string.Join('\n', orderedIds.Select((id, index) =>
            {
                var name = namesById.GetValueOrDefault(id, id);
                return string.Equals(name, id, StringComparison.Ordinal)
                    ? $"{index + 1:00}. {id}"
                    : $"{index + 1:00}. {name} ({id})";
            }));
        }

        private static string FormatLogOrder(IEnumerable<CurrentModEntry> entries)
        {
            return string.Join(", ", entries.Select(entry => entry.Id));
        }

        private static void ShowInfo(string key, string fallback, params object[] args)
        {
            RitsuToastService.ShowInfo(string.Format(L(key, fallback), args), L(ToastTitleKey, ToastTitleFallback));
        }

        private static void ShowWarning(string key, string fallback, params object[] args)
        {
            RitsuToastService.ShowWarning(string.Format(L(key, fallback), args), L(ToastTitleKey, ToastTitleFallback));
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }
    }

    internal sealed record ContentModLoadOrderPreview(
        IReadOnlyList<ContentModLoadOrderPreviewEntry> Current,
        IReadOnlyList<ContentModLoadOrderPreviewEntry> Target);

    internal sealed record ContentModLoadOrderPreviewEntry(
        string Id,
        string DisplayName,
        bool AffectsGameplay,
        bool IsDependency,
        int Position,
        int RelatedPosition);
}
