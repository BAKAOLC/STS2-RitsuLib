using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Ui.Toast;

namespace STS2RitsuLib.Settings
{
    internal static class ContentModLoadOrderCoordinator
    {
        private const string LogPrefix = "[ContentModLoadOrder]";
        private const string ToastTitleKey = "ritsulib.contentModLoadOrder.toast.title";
        private const string ToastTitleFallback = "Content mod load order";

        internal static void SortDeterministically()
        {
            var currentOrder = BuildCurrentOrder();
            var relevantKeys = BuildRelevantKeys(currentOrder);
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
            var currentOrder = BuildCurrentOrder();
            var relevantKeys = BuildRelevantKeys(currentOrder);
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

            var before = BuildCurrentOrder();
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
            var currentOrder = BuildCurrentOrder();
            var relevantKeys = BuildRelevantKeys(currentOrder);
            var entries = currentOrder
                .Where(entry => relevantKeys.Contains(entry.Key))
                .Select(entry => entry.Id)
                .ToArray();
            return FormatOrderPreview(entries);
        }

        internal static string FormatDeterministicOrderPreview()
        {
            return FormatOrderPreview(BuildRelevantTargetOrder(BuildCurrentOrder()).Select(entry => entry.Id)
                .ToArray());
        }

        internal static ContentModLoadOrderPreview BuildPreview()
        {
            var currentOrder = BuildCurrentOrder();
            var relevantKeys = BuildRelevantKeys(currentOrder);
            var relevantDependencyIds = BuildRelevantDependencyIds(currentOrder, relevantKeys);
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
            var relevantKeys = BuildRelevantKeys(currentOrder);
            var priorityById = BuildDeterministicPriorityById(currentOrder, relevantKeys);
            return BuildDependencyValidPriorityOrder(currentOrder, priorityById)
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
            var relevantDependencyIds = BuildRelevantDependencyIds(currentOrder, relevantKeys);
            return currentOrder
                .Where(entry => relevantKeys.Contains(entry.Key))
                .GroupBy(entry => entry.Id, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(GetDeterministicGroup)
                .ThenBy(entry => entry.Id, ModIdComparer.Instance)
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

            var ordered = BuildDependencyValidPriorityOrder(currentOrder, priorityById);

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

        private static IReadOnlyList<CurrentModEntry> BuildDependencyValidPriorityOrder(
            IReadOnlyList<CurrentModEntry> currentOrder,
            IReadOnlyDictionary<string, int> priorityById)
        {
            var entriesByKey = currentOrder.ToDictionary(entry => entry.Key, StringComparer.Ordinal);
            var firstKeyById = currentOrder
                .GroupBy(entry => entry.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().Key, StringComparer.Ordinal);
            var currentPriority = currentOrder
                .Select((entry, index) => (entry.Key, index))
                .ToDictionary(item => item.Key, item => item.index, StringComparer.Ordinal);
            var dependentsByKey =
                entriesByKey.Keys.ToDictionary(key => key, _ => new List<string>(), StringComparer.Ordinal);
            var dependencyCountByKey = entriesByKey.Keys.ToDictionary(key => key, _ => 0, StringComparer.Ordinal);

            foreach (var entry in entriesByKey.Values)
            foreach (var dependencyId in entry.Dependencies.Distinct(StringComparer.Ordinal))
            {
                if (!firstKeyById.TryGetValue(dependencyId, out var dependencyKey) || string.Equals(
                        dependencyKey,
                        entry.Key,
                        StringComparison.Ordinal))
                    continue;

                dependentsByKey[dependencyKey].Add(entry.Key);
                dependencyCountByKey[entry.Key]++;
            }

            var result = new List<CurrentModEntry>(entriesByKey.Count);
            var emitted = new HashSet<string>(StringComparer.Ordinal);
            while (result.Count < entriesByKey.Count)
            {
                var nextKey = entriesByKey.Keys
                    .Where(key => !emitted.Contains(key) && dependencyCountByKey[key] == 0)
                    .OrderBy(GetPriority)
                    .ThenBy(key => currentPriority.GetValueOrDefault(key, int.MaxValue))
                    .ThenBy(key => entriesByKey[key].Id, ModIdComparer.Instance)
                    .ThenBy(key => key, StringComparer.Ordinal)
                    .FirstOrDefault();

                if (nextKey == null)
                {
                    var cycleRemainder = entriesByKey.Keys
                        .Where(key => !emitted.Contains(key))
                        .OrderBy(GetPriority)
                        .ThenBy(key => currentPriority.GetValueOrDefault(key, int.MaxValue))
                        .ThenBy(key => entriesByKey[key].Id, ModIdComparer.Instance)
                        .ThenBy(key => key, StringComparer.Ordinal)
                        .ToArray();
                    RitsuLibFramework.Logger.Warn(
                        $"{LogPrefix} Dependency cycle or unresolved dependency ordering among: {string.Join(", ", cycleRemainder.Select(key => entriesByKey[key].Id))}");
                    foreach (var key in cycleRemainder)
                    {
                        emitted.Add(key);
                        result.Add(entriesByKey[key]);
                    }

                    break;
                }

                emitted.Add(nextKey);
                result.Add(entriesByKey[nextKey]);
                foreach (var dependentKey in dependentsByKey[nextKey])
                    dependencyCountByKey[dependentKey]--;
            }

            return result;

            int GetPriority(string key)
            {
                return priorityById.TryGetValue(entriesByKey[key].Id, out var priority)
                    ? priority
                    : priorityById.Count + currentPriority.GetValueOrDefault(key, int.MaxValue / 2);
            }
        }

        private static IReadOnlySet<string> BuildRelevantKeys(IReadOnlyList<CurrentModEntry> currentOrder)
        {
            var byKey = currentOrder.ToDictionary(entry => entry.Key, StringComparer.Ordinal);
            var firstKeyById = currentOrder
                .GroupBy(entry => entry.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().Key, StringComparer.Ordinal);
            var relevantKeys = new HashSet<string>(StringComparer.Ordinal);
            var pending =
                new Queue<string>(currentOrder.Where(entry => entry.AffectsGameplay).Select(entry => entry.Key));

            while (pending.Count > 0)
            {
                var key = pending.Dequeue();
                if (!relevantKeys.Add(key) || !byKey.TryGetValue(key, out var entry))
                    continue;

                foreach (var dependencyId in entry.Dependencies)
                    if (firstKeyById.TryGetValue(dependencyId, out var dependencyKey))
                        pending.Enqueue(dependencyKey);
            }

            return relevantKeys;
        }

        private static IReadOnlySet<string> BuildRelevantDependencyIds(
            IReadOnlyList<CurrentModEntry> currentOrder,
            IReadOnlySet<string> relevantKeys)
        {
            var relevantIds = currentOrder
                .Where(entry => relevantKeys.Contains(entry.Key))
                .Select(entry => entry.Id)
                .ToHashSet(StringComparer.Ordinal);
            var dependencyIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var entry in currentOrder.Where(entry => relevantKeys.Contains(entry.Key)))
            foreach (var dependencyId in entry.Dependencies)
                if (relevantIds.Contains(dependencyId))
                    dependencyIds.Add(dependencyId);

            return dependencyIds;
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

        private static IReadOnlyList<CurrentModEntry> BuildCurrentOrder()
        {
            var knownMods = ModManager.Mods
                .Select(TryCreateCurrentModEntry)
                .Where(entry => entry != null)
                .Select(entry => entry!)
                .GroupBy(entry => CreateKey(entry.Id, entry.Source), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            var settings = SaveManager.Instance.SettingsSave.ModSettings;
            var result = new List<CurrentModEntry>(knownMods.Count);
            var usedKeys = new HashSet<string>(StringComparer.Ordinal);

            if (settings != null)
                foreach (var savedMod in settings.ModList)
                {
                    var key = CreateKey(savedMod.Id, savedMod.Source);
                    if (!usedKeys.Add(key) || !knownMods.TryGetValue(key, out var entry))
                        continue;

                    result.Add(entry with { IsEnabled = savedMod.IsEnabled });
                }

            result.AddRange(from entry in knownMods.Values.OrderBy(entry => entry.DiscoveryIndex)
                let key = CreateKey(entry.Id, entry.Source)
                where usedKeys.Add(key)
                select entry);

            return result;
        }

        private static CurrentModEntry? TryCreateCurrentModEntry(Mod mod, int discoveryIndex)
        {
            var manifest = mod.manifest;
            var id = manifest?.id?.Trim();
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var settings = SaveManager.Instance.SettingsSave.ModSettings;
            var isEnabled = settings?.IsModDisabled(id, mod.modSource) != true;
            var dependencies = ReadDependencyIds(manifest);
            var displayName = string.IsNullOrWhiteSpace(manifest?.name) ? id : manifest.name.Trim();

            return new(
                CreateKey(id, mod.modSource),
                id,
                displayName,
                mod.modSource,
                isEnabled,
                manifest?.affectsGameplay ?? true,
                dependencies,
                discoveryIndex);
        }

        private static IReadOnlyList<string> ReadDependencyIds(ModManifest? manifest)
        {
            if (manifest?.dependencies == null)
                return [];

            var result = manifest.dependencies.Select(dependency => ReadDependencyId(dependency)?.Trim())
                .Where(id => !string.IsNullOrWhiteSpace(id)).ToList();

            return result.Distinct(StringComparer.Ordinal).ToArray()!;
        }

        private static string? ReadDependencyId(object? dependency)
        {
            if (dependency is string id)
                return id;

            var type = dependency?.GetType();
            return type?.GetField("id")?.GetValue(dependency) as string ??
                   type?.GetProperty("id")?.GetValue(dependency) as string;
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

            var namesById = BuildCurrentOrder()
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

        private static string CreateKey(string id, ModSource source)
        {
            return $"{source}\0{id}";
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

        private sealed record CurrentModEntry(
            string Key,
            string Id,
            string DisplayName,
            ModSource Source,
            bool IsEnabled,
            bool AffectsGameplay,
            IReadOnlyList<string> Dependencies,
            int DiscoveryIndex);

        private sealed class ModIdComparer : IComparer<string>
        {
            internal static readonly ModIdComparer Instance = new();

            public int Compare(string? x, string? y)
            {
                var ignoreCase = StringComparer.OrdinalIgnoreCase.Compare(x, y);
                return ignoreCase != 0 ? ignoreCase : StringComparer.Ordinal.Compare(x, y);
            }
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
