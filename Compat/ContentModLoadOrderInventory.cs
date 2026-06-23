using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Compat
{
    internal static class ContentModLoadOrderInventory
    {
        internal static IReadOnlyList<CurrentModEntry> BuildCurrentOrder()
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

        internal static IReadOnlyList<ContentModInventoryEntry> BuildRelevantInventory()
        {
            return BuildRelevantInventory(BuildCurrentOrder());
        }

        internal static IReadOnlyList<ContentModInventoryEntry> BuildRuntimeRelevantInventory()
        {
            return BuildRelevantInventory(BuildRuntimeLoadedOrder());
        }

        private static IReadOnlyList<ContentModInventoryEntry> BuildRelevantInventory(
            IReadOnlyList<CurrentModEntry> currentOrder)
        {
            var relevantKeys = BuildRelevantKeys(currentOrder);
            var relevantDependencyIds = BuildRelevantDependencyIds(currentOrder, relevantKeys);
            return currentOrder
                .Where(entry => relevantKeys.Contains(entry.Key))
                .Select((entry, index) => new ContentModInventoryEntry(
                    index,
                    entry.Id,
                    entry.Version,
                    entry.DisplayName,
                    entry.Source.ToString(),
                    entry.WorkshopItemId,
                    entry.IsEnabled,
                    entry.AffectsGameplay,
                    relevantDependencyIds.Contains(entry.Id)))
                .ToArray();
        }

        private static IReadOnlyList<CurrentModEntry> BuildRuntimeLoadedOrder()
        {
            return ModManager.GetLoadedMods()
                .Select(TryCreateCurrentModEntry)
                .Where(entry => entry != null)
                .Select(entry => entry! with { IsEnabled = true })
                .ToArray();
        }

        internal static IReadOnlyList<CurrentModEntry> BuildDependencyValidPriorityOrder(
            IReadOnlyList<CurrentModEntry> currentOrder,
            IReadOnlyDictionary<string, int> priorityById,
            string logPrefix)
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
                        $"{logPrefix} Dependency cycle or unresolved dependency ordering among: {string.Join(", ", cycleRemainder.Select(key => entriesByKey[key].Id))}");
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

        internal static IReadOnlySet<string> BuildRelevantKeys(IReadOnlyList<CurrentModEntry> currentOrder)
        {
            var byKey = currentOrder.ToDictionary(entry => entry.Key, StringComparer.Ordinal);
            var firstKeyById = currentOrder
                .GroupBy(entry => entry.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().Key, StringComparer.Ordinal);
            var relevantKeys = new HashSet<string>(StringComparer.Ordinal);
            var pending =
                new Queue<string>(currentOrder
                    .Where(entry => entry.AffectsGameplay || IsDependencyLibraryEntry(entry))
                    .Select(entry => entry.Key));

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

        internal static IReadOnlySet<string> BuildRelevantDependencyIds(
            IReadOnlyList<CurrentModEntry> currentOrder,
            IReadOnlySet<string> relevantKeys)
        {
            var relevantIds = currentOrder
                .Where(entry => relevantKeys.Contains(entry.Key))
                .Select(entry => entry.Id)
                .ToHashSet(StringComparer.Ordinal);
            var dependencyIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var entry in currentOrder.Where(entry => relevantKeys.Contains(entry.Key)))
            {
                if (IsDependencyLibraryEntry(entry))
                    dependencyIds.Add(entry.Id);

                foreach (var dependencyId in entry.Dependencies)
                    if (relevantIds.Contains(dependencyId))
                        dependencyIds.Add(dependencyId);
            }

            return dependencyIds;
        }

        private static bool IsDependencyLibraryEntry(CurrentModEntry entry)
        {
            return IsDependencyLibraryId(entry.Id);
        }

        internal static bool IsDependencyLibraryId(string id)
        {
            return id.Contains("lib", StringComparison.OrdinalIgnoreCase) ||
                   id.Contains("library", StringComparison.OrdinalIgnoreCase) ||
                   id.Contains("api", StringComparison.OrdinalIgnoreCase);
        }

        private static CurrentModEntry? TryCreateCurrentModEntry(Mod mod, int discoveryIndex)
        {
            var manifest = mod.manifest;
            var id = manifest?.id?.Trim();
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var settings = SaveManager.Instance.SettingsSave.ModSettings;
            var isEnabled = settings?.IsModDisabled(id, mod.modSource) != true;
            var displayName = string.IsNullOrWhiteSpace(manifest?.name) ? id : manifest.name.Trim();
            var version = manifest?.version?.Trim() ?? string.Empty;

            return new(
                CreateKey(id, mod.modSource),
                id,
                version,
                displayName,
                mod.modSource,
                Sts2ModManagerCompat.TryGetWorkshopItemId(mod),
                isEnabled,
                manifest?.affectsGameplay ?? true,
                ReadDependencyIds(manifest),
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

        internal static string CreateKey(string id, ModSource source)
        {
            return $"{source}\0{id}";
        }

        internal sealed record CurrentModEntry(
            string Key,
            string Id,
            string Version,
            string DisplayName,
            ModSource Source,
            ulong? WorkshopItemId,
            bool IsEnabled,
            bool AffectsGameplay,
            IReadOnlyList<string> Dependencies,
            int DiscoveryIndex);

        internal sealed class ModIdComparer : IComparer<string>
        {
            internal static readonly ModIdComparer Instance = new();

            public int Compare(string? x, string? y)
            {
                var ignoreCase = StringComparer.OrdinalIgnoreCase.Compare(x, y);
                return ignoreCase != 0 ? ignoreCase : StringComparer.Ordinal.Compare(x, y);
            }
        }
    }

    internal sealed record ContentModInventoryEntry(
        int Index,
        string Id,
        string Version,
        string Name,
        string Source,
        ulong? WorkshopItemId,
        bool IsEnabled,
        bool AffectsGameplay,
        bool IsDependency);
}
