using System.Text.Json;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Loads built-in (embedded) and disk-side <c>.theme.json</c> documents into a per-id cache, then
    ///     resolves inheritance, scope overlays, and references when asked to build a snapshot.
    /// </summary>
    public static class RitsuShellThemeCatalog
    {
        private const string DefaultThemeId = "default";

        private static readonly Lock Gate = new();

        private static Dictionary<string, RitsuShellThemeDocument>? _byId;

        /// <summary>
        ///     Sorted list of theme ids currently in the catalog (lowercase).
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
        /// </summary>
        public static void EnsureLoaded()
        {
            lock (Gate)
            {
                if (_byId != null)
                    return;

                var map = new Dictionary<string, RitsuShellThemeDocument>(StringComparer.Ordinal);
                var asm = typeof(RitsuShellThemeCatalog).Assembly;
                var extractedPairs = new List<(string Id, byte[] Bytes)>();

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
                    extractedPairs.Add((id, bytes));
                }

                if (RitsuShellThemePaths.TryEnsureShellThemesDirectory(out var themesAbs))
                {
                    foreach (var (id, bytes) in extractedPairs)
                        try
                        {
                            var targetFile = Path.Combine(themesAbs, $"{id}.theme.json");
                            if (!File.Exists(targetFile))
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
        /// </summary>
        /// <param name="themeId">Target theme id (case-insensitive). Empty falls back to <c>default</c>.</param>
        /// <param name="modRegistrations">Registered mod token contributions (default trees and extension blobs).</param>
        /// <param name="resolvedId">Resolved id used for <see cref="RitsuShellTheme.Id" />.</param>
        /// <param name="theme">Built snapshot when successful.</param>
        /// <returns><see langword="true" /> if both target and <c>default</c> chain are loadable.</returns>
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
        /// </summary>
        /// <param name="themeId">Theme id to restore (case-insensitive, empty falls back to default).</param>
        /// <param name="restoredPath">Absolute path of the restored file when successful.</param>
        /// <returns><see langword="true" /> when the embedded source exists and was written to disk.</returns>
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
    }
}
