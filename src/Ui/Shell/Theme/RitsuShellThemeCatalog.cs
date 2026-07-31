using System.Text.Json;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Loads embedded and on-disk <c>.theme.json</c> documents into a catalog, then resolves inheritance,
    ///         scope overlays, and token references when building a snapshot.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将内嵌及磁盘上的 <c>.theme.json</c> 文档加载到主题目录，并在构建快照时解析继承关系、
    ///         作用域覆盖及令牌引用。
    ///     </para>
    /// </summary>
    public static class RitsuShellThemeCatalog
    {
        private const string DefaultThemeId = "default";

        private static readonly Lock Gate = new();

        private static Dictionary<string, RitsuShellThemeDocument>? _byId;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets a new ordinally sorted list of the normalized theme identifiers in the current catalog.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取当前目录中规范化主题标识符的新列表，并按序数顺序排列。
        ///     </para>
        /// </summary>
        public static IReadOnlyList<string> RegisteredThemeIds
        {
            get
            {
                var catalog = GetLoadedCatalogSnapshot();
                var keys = catalog.Keys.ToArray();
                Array.Sort(keys, StringComparer.Ordinal);
                return keys;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Invalidates the in-memory catalog so the next access reloads all themes.</para>
        ///     <para xml:lang="zh-CN">使内存中的主题目录失效，以便下次访问时重新加载全部主题。</para>
        /// </summary>
        public static void InvalidateCache()
        {
            lock (Gate)
            {
                _byId = null;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Loads embedded themes and theme files from disk. Missing embedded files are extracted beside
        ///         user-authored themes, and newer embedded revisions replace older disk copies after a
        ///         best-effort backup.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         加载内嵌主题及磁盘主题文件。缺失的内嵌主题会提取到用户主题所在目录；若内嵌修订较新，
        ///         则会在尽力备份后替换磁盘上的旧副本。
        ///     </para>
        /// </summary>
        public static void EnsureLoaded()
        {
            lock (Gate)
            {
                if (_byId != null)
                    return;

                var map = new Dictionary<string, RitsuShellThemeDocument>(StringComparer.Ordinal);
                var asm = typeof(RitsuShellThemeCatalog).Assembly;
                var extractedPairs = new List<(string Id, byte[] Bytes, int Version)>();

                foreach (var manifestName in asm.GetManifestResourceNames())
                {
                    if (!manifestName.EndsWith(".theme.json", StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        using var stream = asm.GetManifestResourceStream(manifestName);
                        if (stream == null)
                            continue;

                        using var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        var bytes = ms.ToArray();
                        var doc = RitsuShellThemeDocument.Deserialize(new MemoryStream(bytes));
                        if (doc == null || string.IsNullOrWhiteSpace(doc.Id))
                            continue;

                        var id = doc.Id.Trim().ToLowerInvariant();
                        map[id] = doc;
                        extractedPairs.Add((id, bytes, NormalizeThemeVersion(doc)));
                    }
                    catch (Exception ex)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[ShellTheme] Could not load embedded theme resource '{manifestName}': {ex}");
                    }
                }

                if (RitsuShellThemePaths.TryEnsureShellThemesDirectory(out var themesAbs))
                {
                    foreach (var (id, bytes, embeddedVersion) in extractedPairs)
                        try
                        {
                            var targetFile = Path.Combine(themesAbs, $"{id}.theme.json");
                            if (!File.Exists(targetFile))
                            {
                                File.WriteAllBytes(targetFile, bytes);
                                continue;
                            }

                            if (!ShouldOverwriteDiskTheme(targetFile, embeddedVersion))
                                continue;

                            TryBackupThemeFile(targetFile);
                            File.WriteAllBytes(targetFile, bytes);
                        }
                        catch (Exception ex)
                        {
                            RitsuLibFramework.Logger.Warn(
                                $"[ShellTheme] Could not extract embedded theme '{id}' to '{themesAbs}': {ex}");
                        }

                    try
                    {
                        foreach (var path in Directory.EnumerateFiles(themesAbs, "*.theme.json",
                                     SearchOption.TopDirectoryOnly))
                            try
                            {
                                using var fs = File.OpenRead(path);
                                var diskDoc = RitsuShellThemeDocument.Deserialize(fs);
                                if (diskDoc == null || string.IsNullOrWhiteSpace(diskDoc.Id))
                                    continue;

                                var did = diskDoc.Id.Trim().ToLowerInvariant();
                                map[did] = diskDoc;
                            }
                            catch (Exception ex)
                            {
                                RitsuLibFramework.Logger.Warn(
                                    $"[ShellTheme] Could not load disk theme '{path}': {ex}");
                            }
                    }
                    catch (Exception ex)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[ShellTheme] Could not enumerate disk themes in '{themesAbs}': {ex}");
                    }
                }

                _byId = map;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to build a merged, reference-resolved <see cref="RitsuShellTheme" /> snapshot for
        ///         <paramref name="themeId" />, including registered mod defaults.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试为 <paramref name="themeId" /> 构建已合并并完成引用解析的
        ///         <see cref="RitsuShellTheme" /> 快照，其中包含已注册的模组默认令牌。
        ///     </para>
        /// </summary>
        /// <param name="themeId">
        ///     <para xml:lang="en">
        ///         The case-insensitive theme identifier. A blank value selects <c>default</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         不区分大小写的主题标识符；空白值选择 <c>default</c>。
        ///     </para>
        /// </param>
        /// <param name="modRegistrations">
        ///     <para xml:lang="en">The registered mod token contributions to merge before theme documents.</para>
        ///     <para xml:lang="zh-CN">在主题文档之前合并的已注册模组令牌贡献。</para>
        /// </param>
        /// <param name="resolvedId">
        ///     <para xml:lang="en">
        ///         Receives the normalized requested identifier, including when the build fails.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         接收规范化后的请求标识符，包括构建失败时。
        ///     </para>
        /// </param>
        /// <param name="theme">
        ///     <para xml:lang="en">
        ///         Receives the completed snapshot on success, or <see langword="null" /> on failure.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         成功时接收构建完成的快照；失败时为 <see langword="null" />。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if the target exists, its inheritance chain is valid, and all token
        ///         references resolve; otherwise, <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若目标存在、继承链有效且所有令牌引用均可解析，则为 <see langword="true" />；否则为
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryBuildSnapshot(string themeId,
            IReadOnlyList<RitsuShellThemeModRegistration> modRegistrations,
            out string resolvedId, out RitsuShellTheme? theme)
        {
            resolvedId = string.IsNullOrWhiteSpace(themeId)
                ? DefaultThemeId
                : themeId.Trim().ToLowerInvariant();
            theme = null;
            var catalog = GetLoadedCatalogSnapshot();

            if (!catalog.TryGetValue(resolvedId, out var leaf))
                return false;

            if (!TryResolveInheritanceChain(catalog, leaf, out var chain))
                return false;

            var root = new Dictionary<string, object?>(StringComparer.Ordinal);
            var extensions = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

            foreach (var reg in modRegistrations)
                if (reg.Defaults.HasValue)
                    RitsuShellThemeMerger.MergeInto(root, reg.Defaults.Value);

            foreach (var doc in chain)
            {
                if (doc.Core.HasValue) MergeBranch(root, "core", doc.Core.Value);
                if (doc.Semantic.HasValue) MergeBranch(root, "semantic", doc.Semantic.Value);
                if (doc.Components.HasValue) MergeBranch(root, "components", doc.Components.Value);

                if (doc.Extensions != null)
                    foreach (var pair in doc.Extensions)
                        extensions[pair.Key] = pair.Value;

                MergeScopeIfPresent(root, doc, "global");
                MergeScopeIfPresent(root, doc, "shell");
                MergeScopeIfPresent(root, doc, "modSettings");
                if (doc.Scopes == null) continue;
                {
                    foreach (var pair in
                             doc.Scopes.Where(pair => pair.Key.StartsWith("mod:", StringComparison.Ordinal)))
                        MergeScopeBlock(root, pair.Value, extensions);
                }
            }

            var errors = new List<string>();
            RitsuShellThemeReferenceResolver.ResolveAll(root, errors);
            if (errors.Count > 0)
            {
                foreach (var error in errors)
                    RitsuLibFramework.Logger.Warn($"[ShellTheme:{resolvedId}] {error}");
                return false;
            }

            theme = RitsuShellThemeBuilder.Build(resolvedId, root, extensions);
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to replace one disk theme file with its embedded counterpart.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试使用对应的内嵌主题替换一个磁盘主题文件。
        ///     </para>
        /// </summary>
        /// <param name="themeId">
        ///     <para xml:lang="en">
        ///         The case-insensitive theme identifier to restore. A blank value selects <c>default</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         要恢复的不区分大小写主题标识符；空白值选择 <c>default</c>。
        ///     </para>
        /// </param>
        /// <param name="restoredPath">
        ///     <para xml:lang="en">
        ///         Receives the absolute path of the restored file on success, or an empty string on failure.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         成功时接收已恢复文件的绝对路径；失败时为空字符串。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if an embedded counterpart exists and is written successfully;
        ///         otherwise, <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若存在内嵌对应项且成功写入，则为 <see langword="true" />；否则为
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryRestoreDiskThemeFromEmbedded(string themeId, out string restoredPath)
        {
            restoredPath = "";
            var requestedId = string.IsNullOrWhiteSpace(themeId)
                ? DefaultThemeId
                : themeId.Trim().ToLowerInvariant();
            if (!TryLoadEmbeddedThemeBytes(requestedId, out var bytes))
                return false;
            if (!RitsuShellThemePaths.TryEnsureShellThemesDirectory(out var themesAbs))
                return false;
            try
            {
                restoredPath = Path.Combine(themesAbs, $"{requestedId}.theme.json");
                File.WriteAllBytes(restoredPath, bytes);
                InvalidateCache();
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ShellTheme] Could not restore embedded theme '{requestedId}': {ex}");
                restoredPath = "";
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to replace every existing disk theme that has an embedded counterpart.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试替换所有存在内嵌对应项的现有磁盘主题。
        ///     </para>
        /// </summary>
        /// <param name="restoredCount">
        ///     <para xml:lang="en">
        ///         Receives the number of files replaced before the method completes or fails.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         接收方法完成或失败前已替换的文件数。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if enumeration and all required writes complete; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若枚举及所有必要写入均完成，则为 <see langword="true" />；否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryRestoreAllExistingDiskThemesFromEmbedded(out int restoredCount)
        {
            restoredCount = 0;
            if (!RitsuShellThemePaths.TryEnsureShellThemesDirectory(out var themesAbs))
                return false;

            try
            {
                foreach (var (id, bytes) in EnumerateEmbeddedThemeDocuments())
                {
                    var targetFile = Path.Combine(themesAbs, $"{id}.theme.json");
                    if (!File.Exists(targetFile))
                        continue;

                    File.WriteAllBytes(targetFile, bytes);
                    restoredCount++;
                }

                InvalidateCache();
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ShellTheme] Could not restore existing disk themes in '{themesAbs}': {ex}");
                return false;
            }
        }

        private static void MergeBranch(Dictionary<string, object?> root, string branchName, JsonElement branch)
        {
            if (branch.ValueKind != JsonValueKind.Object)
                return;
            if (!root.TryGetValue(branchName, out var existing) ||
                existing is not Dictionary<string, object?> existingGroup)
            {
                existingGroup = new(StringComparer.Ordinal);
                root[branchName] = existingGroup;
            }

            RitsuShellThemeMerger.MergeInto(existingGroup, branch);
        }

        private static void MergeScopeIfPresent(Dictionary<string, object?> root, RitsuShellThemeDocument doc,
            string scopeId)
        {
            if (doc.Scopes == null || !doc.Scopes.TryGetValue(scopeId, out var scope))
                return;
            MergeScopeBlock(root, scope, null);
        }

        private static void MergeScopeBlock(Dictionary<string, object?> root, JsonElement scope,
            Dictionary<string, JsonElement>? extensions)
        {
            if (scope.ValueKind != JsonValueKind.Object)
                return;

            foreach (var prop in scope.EnumerateObject())
                switch (prop.Name)
                {
                    case "core":
                    case "semantic":
                    case "components":
                        MergeBranch(root, prop.Name, prop.Value);
                        break;
                    case "extensions":
                        if (extensions != null && prop.Value.ValueKind == JsonValueKind.Object)
                            foreach (var ext in prop.Value.EnumerateObject())
                                extensions[ext.Name] = ext.Value;
                        break;
                }
        }

        private static bool TryResolveInheritanceChain(
            IReadOnlyDictionary<string, RitsuShellThemeDocument> catalog,
            RitsuShellThemeDocument leaf,
            out List<RitsuShellThemeDocument> chain)
        {
            chain = [];
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var stack = new Stack<RitsuShellThemeDocument>();
            var cur = leaf;
            while (true)
            {
                var id = cur.Id.Trim().ToLowerInvariant();
                if (!visiting.Add(id))
                    return false;

                stack.Push(cur);
                if (string.IsNullOrWhiteSpace(cur.Inherits))
                    break;

                var p = cur.Inherits!.Trim().ToLowerInvariant();
                if (!catalog.TryGetValue(p, out var parent))
                    return false;
                cur = parent;
            }

            while (stack.Count > 0)
                chain.Add(stack.Pop());

            return true;
        }

        private static IReadOnlyDictionary<string, RitsuShellThemeDocument> GetLoadedCatalogSnapshot()
        {
            while (true)
            {
                EnsureLoaded();
                lock (Gate)
                {
                    if (_byId != null)
                        return _byId;
                }
            }
        }

        private static bool TryLoadEmbeddedThemeBytes(string normalizedThemeId, out byte[] bytes)
        {
            bytes = [];
            var asm = typeof(RitsuShellThemeCatalog).Assembly;
            foreach (var manifestName in asm.GetManifestResourceNames())
            {
                if (!manifestName.EndsWith(".theme.json", StringComparison.OrdinalIgnoreCase))
                    continue;
                try
                {
                    using var stream = asm.GetManifestResourceStream(manifestName);
                    if (stream == null)
                        continue;
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    var candidateBytes = ms.ToArray();
                    var doc = RitsuShellThemeDocument.Deserialize(new MemoryStream(candidateBytes));
                    if (doc == null || string.IsNullOrWhiteSpace(doc.Id))
                        continue;
                    var id = doc.Id.Trim().ToLowerInvariant();
                    if (!string.Equals(id, normalizedThemeId, StringComparison.Ordinal))
                        continue;
                    bytes = candidateBytes;
                    return true;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[ShellTheme] Could not inspect embedded theme resource '{manifestName}': {ex}");
                }
            }

            return false;
        }

        private static IEnumerable<(string Id, byte[] Bytes)> EnumerateEmbeddedThemeDocuments()
        {
            var asm = typeof(RitsuShellThemeCatalog).Assembly;
            foreach (var manifestName in asm.GetManifestResourceNames())
            {
                if (!manifestName.EndsWith(".theme.json", StringComparison.OrdinalIgnoreCase))
                    continue;

                byte[] bytes;
                RitsuShellThemeDocument? doc;
                try
                {
                    using var stream = asm.GetManifestResourceStream(manifestName);
                    if (stream == null)
                        continue;
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    bytes = ms.ToArray();
                    doc = RitsuShellThemeDocument.Deserialize(new MemoryStream(bytes));
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[ShellTheme] Could not enumerate embedded theme resource '{manifestName}': {ex}");
                    continue;
                }

                if (doc == null || string.IsNullOrWhiteSpace(doc.Id))
                    continue;

                yield return (doc.Id.Trim().ToLowerInvariant(), bytes);
            }
        }

        private static int NormalizeThemeVersion(RitsuShellThemeDocument? doc)
        {
            if (doc?.ThemeVersion is > 0 and var explicitVersion)
                return explicitVersion;
            return doc?.ThemeFormatVersion is > 0 and var formatVersion ? formatVersion : 0;
        }

        private static bool ShouldOverwriteDiskTheme(string path, int embeddedVersion)
        {
            if (embeddedVersion <= 0)
                return false;
            try
            {
                using var fs = File.OpenRead(path);
                var diskDoc = RitsuShellThemeDocument.Deserialize(fs);
                var diskVersion = NormalizeThemeVersion(diskDoc);
                return embeddedVersion > diskVersion;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ShellTheme] Could not read disk theme version from '{path}'; the embedded copy will replace it: {ex}");
                return true;
            }
        }

        private static void TryBackupThemeFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                var backupPath = BuildTimestampedBackupPath(path);
                File.Copy(path, backupPath, false);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ShellTheme] Could not back up disk theme '{path}' before replacement: {ex}");
            }
        }

        private static string BuildTimestampedBackupPath(string originalPath)
        {
            var attempt = 0;
            while (attempt <= 100)
            {
                var candidate = attempt == 0
                    ? $"{originalPath}.backup.{DateTime.UtcNow:yyyyMMddHHmmssfff}"
                    : $"{originalPath}.backup.{DateTime.UtcNow:yyyyMMddHHmmssfff}.{attempt}";
                if (!File.Exists(candidate))
                    return candidate;

                attempt++;
            }

            return $"{originalPath}.backup.{DateTime.UtcNow:yyyyMMddHHmmssfff}.fallback";
        }
    }
}
