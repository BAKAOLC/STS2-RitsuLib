namespace STS2RitsuLib.Search
{
    /// <summary>
    ///     <para xml:lang="en">Registers optional transliteration and search-text expansion providers.</para>
    ///     <para xml:lang="zh-CN">注册可选的转写和搜索文本扩展提供器。</para>
    /// </summary>
    public static class RitsuSearchExpansionRegistry
    {
        private const int MaximumExpansionsPerCall = 64;
        private const int MaximumModIdLength = 128;
        private const int MaximumProviderIdLength = 128;
        private const int MaximumDisplayNameLength = 256;
        private const int MaximumTotalExpansionCharacters = 8192;
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<string, ProviderEntry> Providers = new(StringComparer.OrdinalIgnoreCase);
        private static long _generation;
        private static long _nextToken;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a case-insensitively unique provider owned by <paramref name="modId" />. Registration reads
        ///         provider metadata but does not call <see cref="IRitsuSearchExpansionProvider.Expand" />. Dispose the
        ///         returned handle before the owner unloads.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册由 <paramref name="modId" /> 持有且 ID 不区分大小写全局唯一的提供器。注册会读取提供器元数据，
        ///         但不会调用 <see cref="IRitsuSearchExpansionProvider.Expand" />。所属 mod 卸载前应释放返回的句柄。
        ///     </para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The non-empty owning mod ID, at most 128 characters.</para>
        ///     <para xml:lang="zh-CN">非空且不超过 128 个字符的所属 mod ID。</para>
        /// </param>
        /// <param name="provider">
        ///     <para xml:lang="en">The provider instance. The registry does not take ownership of it.</para>
        ///     <para xml:lang="zh-CN">提供器实例；注册表不会取得其所有权。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A handle that invalidates caches or unregisters this exact registration.</para>
        ///     <para xml:lang="zh-CN">用于使缓存失效或注销本次确切注册的句柄。</para>
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">The mod ID or provider metadata is invalid.</para>
        ///     <para xml:lang="zh-CN">mod ID 或提供器元数据无效。</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="provider" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="provider" /> 为 null。</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">The provider ID is already registered.</para>
        ///     <para xml:lang="zh-CN">提供器 ID 已被注册。</para>
        /// </exception>
        public static RitsuSearchExpansionRegistration Register(
            string modId,
            IRitsuSearchExpansionProvider provider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(provider);
            var normalizedModId = modId.Trim();
            if (normalizedModId.Length > MaximumModIdLength)
                throw new ArgumentException($"Owning mod IDs cannot exceed {MaximumModIdLength} characters.",
                    nameof(modId));
            var providerId = ValidateProviderId(provider.Id);
            ValidateDisplayName(provider.DisplayName);
            var enabledByDefault = provider.EnabledByDefault;

            lock (SyncRoot)
            {
                if (Providers.ContainsKey(providerId))
                    throw new InvalidOperationException(
                        $"Search expansion provider '{providerId}' is already registered.");

                var token = ++_nextToken;
                Providers.Add(providerId, new(providerId, normalizedModId, provider, enabledByDefault, token));
                _generation++;
                return new(providerId, token);
            }
        }

        internal static long Generation
        {
            get
            {
                lock (SyncRoot)
                {
                    return _generation;
                }
            }
        }

        internal static IReadOnlyList<RitsuSearchExpansion> Expand(string text, string languageCode)
        {
            ProviderEntry[] providers;
            lock (SyncRoot)
            {
                providers = [.. Providers.Values];
            }

            providers =
            [
                .. providers.Where(entry =>
                    RitsuSearchSettingsStore.IsProviderEnabled(entry.Id, entry.EnabledByDefault)),
            ];

            if (providers.Length == 0)
                return [];

            var context = new RitsuSearchExpansionContext(languageCode);
            var expansions = new List<RitsuSearchExpansion>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var totalCharacters = 0;
            foreach (var entry in providers)
            {
                IReadOnlyList<RitsuSearchExpansion>? supplied;
                try
                {
                    supplied = entry.Provider.Expand(text, context);
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Search] Expansion provider '{entry.Id}' from '{entry.ModId}' failed: {ex.Message}");
                    continue;
                }

                if (supplied == null)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Search] Expansion provider '{entry.Id}' from '{entry.ModId}' returned null.");
                    continue;
                }

                var acceptedForProvider = 0;
                foreach (var expansion in supplied)
                {
                    if (acceptedForProvider >= MaximumExpansionsPerCall ||
                        totalCharacters >= MaximumTotalExpansionCharacters)
                        break;
                    if (expansion == null ||
                        string.Equals(expansion.Text, text, StringComparison.OrdinalIgnoreCase) ||
                        !seen.Add(expansion.Text))
                        continue;

                    if (totalCharacters + expansion.Text.Length > MaximumTotalExpansionCharacters)
                        break;
                    expansions.Add(expansion);
                    acceptedForProvider++;
                    totalCharacters += expansion.Text.Length;
                }
            }

            return expansions;
        }

        internal static IReadOnlyList<ProviderSnapshot> GetProviderSnapshots()
        {
            ProviderEntry[] providers;
            lock (SyncRoot)
            {
                providers = [.. Providers.Values.OrderBy(static entry => entry.Id, StringComparer.OrdinalIgnoreCase)];
            }

            return
            [
                .. providers.Select(entry => new ProviderSnapshot(
                    entry.Id,
                    entry.ModId,
                    ResolveDisplayName(entry),
                    RitsuSearchSettingsStore.IsProviderEnabled(entry.Id, entry.EnabledByDefault))),
            ];
        }

        internal static void NotifyConfigurationChanged()
        {
            lock (SyncRoot)
            {
                _generation++;
            }
        }

        internal static void Invalidate(string providerId, long token)
        {
            lock (SyncRoot)
            {
                if (Providers.TryGetValue(providerId, out var entry) && entry.Token == token)
                    _generation++;
            }
        }

        internal static void Unregister(string providerId, long token)
        {
            lock (SyncRoot)
            {
                if (!Providers.TryGetValue(providerId, out var entry) || entry.Token != token)
                    return;
                Providers.Remove(providerId);
                _generation++;
            }
        }

        private static string ResolveDisplayName(ProviderEntry entry)
        {
            try
            {
                var value = entry.Provider.DisplayName;
                return string.IsNullOrWhiteSpace(value) || value.Length > MaximumDisplayNameLength
                    ? entry.Id
                    : value.Trim();
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Search] Could not resolve display name for provider '{entry.Id}': {ex.Message}");
                return entry.Id;
            }
        }

        private static string ValidateProviderId(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            var trimmed = value.Trim();
            if (trimmed.Length > MaximumProviderIdLength ||
                trimmed.Any(static character => !IsProviderIdCharacter(character)))
                throw new ArgumentException(
                    "Search expansion provider IDs may contain at most 128 ASCII letters, digits, periods, hyphens, or underscores.",
                    nameof(value));
            return trimmed;
        }

        private static void ValidateDisplayName(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (value.Length > MaximumDisplayNameLength)
                throw new ArgumentException(
                    $"Search expansion provider display names cannot exceed {MaximumDisplayNameLength} characters.",
                    nameof(value));
        }

        private static bool IsProviderIdCharacter(char character)
        {
            return character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '.' or '-' or '_';
        }

        internal sealed record ProviderSnapshot(string Id, string ModId, string DisplayName, bool Enabled);

        private sealed record ProviderEntry(
            string Id,
            string ModId,
            IRitsuSearchExpansionProvider Provider,
            bool EnabledByDefault,
            long Token);
    }
}
