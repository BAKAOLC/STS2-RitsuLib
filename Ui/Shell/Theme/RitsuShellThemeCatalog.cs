using System.Text.Json;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Loads built-in (embedded) and disk-side <c>.theme.json</c> documents into a per-id cache, then
    ///     resolves inheritance, scope overlays, and references when asked to build a snapshot.
    ///     将内置（嵌入式）和磁盘侧 <c>.theme.json</c> 文档加载到按 id 划分的缓存，然后
    ///     在请求构建快照时解析继承、作用域覆盖和引用。
    /// </summary>
    public static class RitsuShellThemeCatalog
    {
        private const string DefaultThemeId = "default";

        private static readonly Lock Gate = new();

        private static Dictionary<string, RitsuShellThemeDocument>? _byId;

        /// <summary>
        ///     Sorted list of theme ids currently in the catalog (lowercase).
        ///     排序列表 of theme ids 当前在目录中 (小写)。
        /// </summary>
        public static IReadOnlyList<string> RegisteredThemeIds
        {
            get
            {
                EnsureLoaded();
                var keys = _byId!.Keys.ToArray();
                Array.Sort(keys, StringComparer.Ordinal);
                return keys;
            }
        }

        /// <summary>
        ///     Drops the in-memory cache so the next call to <see cref="EnsureLoaded" /> reloads all themes.
        ///     丢弃内存缓存，使下一次调用 <see cref="EnsureLoaded" /> 时重新加载所有主题。
        /// </summary>
        public static void InvalidateCache()
        {
            lock (Gate)
            {
                _byId = null;
            }
        }

        /// <summary>
        ///     Loads themes from the assembly manifest and the on-disk themes directory; extracts missing
        ///     embedded themes to disk so they show up next to user-authored themes.
        ///     从程序集清单和磁盘主题目录加载主题；将缺失的
        ///     嵌入式主题提取到磁盘，使它们显示在用户编写的主题旁边。
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
                        catch
                        {
                            // Best-effort: missing extraction does not invalidate the embedded copy.
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
                            catch
                            {
                                // Skip invalid or partially written theme files.
                            }
                    }
                    catch
                    {
                        // Directory enumeration may fail on some hosts; embedded themes remain available.
                    }
                }

                _byId = map;
            }
        }

        /// <summary>
        ///     Builds a fully-merged, reference-resolved <see cref="RitsuShellTheme" /> snapshot for
        ///     <paramref name="themeId" />, merging mod-registered defaults along the way.
        ///     为 <paramref name="themeId" /> 构建完全合并、已解析引用的 <see cref="RitsuShellTheme" /> 快照，
        ///     并在过程中合并 mod 注册的默认值。
        /// </summary>
        /// <param name="themeId">
        ///     Target theme id (case-insensitive). Empty falls back to <c>default</c>.
        ///     目标主题 id (不区分大小写). 为空时回退到 <c>default</c>。
        /// </param>
        /// <param name="modRegistrations">
        ///     Registered mod token contributions (default trees and extension blobs).
        ///     已注册的 mod 令牌贡献（默认树和扩展 blob）。
        /// </param>
        /// <param name="resolvedId">
        ///     Resolved id used for <see cref="RitsuShellTheme.Id" />.
        ///     用于 <see cref="RitsuShellTheme.Id" /> 的解析后 id。
        /// </param>
        /// <param name="theme">
        ///     Built snapshot when successful.
        ///     构建出的快照 成功时。
        /// </param>
        /// <returns>
        ///     <see langword="true" /> if both target and <c>default</c> chain are loadable.
        ///     <see langword="true" /> if 目标和 <c>default</c> 链都可加载。
        /// </returns>
        public static bool TryBuildSnapshot(string themeId,
            IReadOnlyList<RitsuShellThemeModRegistration> modRegistrations,
            out string resolvedId, out RitsuShellTheme? theme)
        {
            resolvedId = string.IsNullOrWhiteSpace(themeId)
                ? DefaultThemeId
                : themeId.Trim().ToLowerInvariant();
            theme = null;
            EnsureLoaded();
            if (_byId == null)
                return false;

            if (!_byId.TryGetValue(resolvedId, out var leaf))
                return false;

            if (!TryResolveInheritanceChain(leaf, out var chain))
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

            theme = RitsuShellThemeBuilder.Build(resolvedId, root, extensions);
            return true;
        }

        /// <summary>
        ///     Overwrites one disk theme file with its embedded built-in counterpart.
        ///     用对应的嵌入式内置主题覆盖一个磁盘主题文件。
        /// </summary>
        /// <param name="themeId">
        ///     Theme id to restore (case-insensitive, empty falls back to default).
        ///     要恢复的主题 id (不区分大小写, empty 回退到 default)。
        /// </param>
        /// <param name="restoredPath">
        ///     Absolute path of the restored file when successful.
        ///     成功时恢复文件的绝对路径。
        /// </param>
        /// <returns>
        ///     <see langword="true" /> when the embedded source exists and was written to disk.
        ///     当嵌入式源存在并已写入磁盘时，为 <see langword="true" />。
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
            catch
            {
                restoredPath = "";
                return false;
            }
        }

        /// <summary>
        ///     Resets all existing disk theme files that have embedded counterparts.
        ///     重置 all 现有磁盘主题文件 that have 嵌入式对应项。
        /// </summary>
        /// <param name="restoredCount">
        ///     How many disk theme files were overwritten.
        ///     数量： 磁盘主题文件s 被覆盖。
        /// </param>
        /// <returns>
        ///     <see langword="true" /> if the operation completed without fatal setup failures.
        ///     如果操作完成且没有致命设置失败，则为 <see langword="true" />。
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
            catch
            {
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

        private static bool TryResolveInheritanceChain(RitsuShellThemeDocument leaf,
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
                if (_byId == null || !_byId.TryGetValue(p, out var parent))
                    return false;
                cur = parent;
            }

            while (stack.Count > 0)
                chain.Add(stack.Pop());

            return true;
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
                catch
                {
                    // Ignore malformed embedded resources and continue probing.
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
                catch
                {
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
            catch
            {
                // Invalid disk content should be replaced by a valid embedded release copy.
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
            catch
            {
                // Best-effort backup: do not block theme recovery if backup fails.
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
