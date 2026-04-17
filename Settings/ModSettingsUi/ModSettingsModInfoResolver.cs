using System.IO;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Resolves installed mod manifest fields (name, version, icon) for the settings sidebar header.
    ///     Matches vanilla modding screen: <see cref="ModManifest" /> fields and <c>res://&lt;id&gt;/mod_image.png</c>.
    /// </summary>
    internal static class ModSettingsModInfoResolver
    {
        internal static Mod? TryFindMod(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId))
                return null;

            foreach (var m in Sts2ModManagerCompat.EnumerateModsForManifestLookup())
            {
                if (string.Equals(m.manifest?.id, modId, StringComparison.OrdinalIgnoreCase))
                    return m;
            }

            foreach (var m in Sts2ModManagerCompat.EnumerateModsForManifestLookup())
            {
                if (string.IsNullOrWhiteSpace(m.path))
                    continue;
                var trimmed = m.path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var folder = Path.GetFileName(trimmed);
                if (string.Equals(folder, modId, StringComparison.OrdinalIgnoreCase))
                    return m;
            }

            return null;
        }

        internal static string ResolveTitle(Mod? mod, string modId)
        {
            if (mod?.manifest is ModManifest mm && !string.IsNullOrWhiteSpace(mm.name))
                return mm.name;

            if (mod != null)
            {
                var n = GetManifestMemberString(mod.manifest, "name", "Name");
                if (!string.IsNullOrWhiteSpace(n))
                    return n;
            }

            return ModSettingsLocalization.ResolveModName(modId, modId);
        }

        internal static string? ResolveVersion(Mod? mod)
        {
            if (mod?.manifest is ModManifest mm && !string.IsNullOrWhiteSpace(mm.version))
                return mm.version;
            return mod == null ? null : GetManifestMemberString(mod.manifest, "version", "Version");
        }

        internal static string? ResolveAuthor(Mod? mod)
        {
            if (mod?.manifest is ModManifest mm && !string.IsNullOrWhiteSpace(mm.author))
                return mm.author;
            return mod == null ? null : GetManifestMemberString(mod.manifest, "author", "Author");
        }

        internal static string? ResolveDescription(Mod? mod, int maxLen = 220)
        {
            string? d;
            if (mod?.manifest is ModManifest mm && !string.IsNullOrWhiteSpace(mm.description))
                d = mm.description;
            else
                d = mod == null ? null : GetManifestMemberString(mod.manifest, "description", "Description");
            if (string.IsNullOrWhiteSpace(d))
                return null;
            d = d.Trim().Replace("\r\n", "\n");
            return d.Length <= maxLen ? d : d[..maxLen].TrimEnd() + "…";
        }

        /// <summary>
        ///     Optional manifest icon paths, then vanilla <c>res://&lt;manifest id&gt;/mod_image.png</c>.
        /// </summary>
        internal static Texture2D? TryLoadModIcon(Mod? mod, string modId)
        {
            var fromManifest = TryLoadManifestCustomIcon(mod);
            if (fromManifest != null)
                return fromManifest;

            var id = mod?.manifest is ModManifest mm ? mm.id : null;
            foreach (var key in new[] { id, modId })
            {
                if (string.IsNullOrWhiteSpace(key))
                    continue;
                var tex = TryLoadVanillaModImageRes(key);
                if (tex != null)
                    return tex;
            }

            return null;
        }

        private static Texture2D? TryLoadManifestCustomIcon(Mod? mod)
        {
            if (mod?.manifest == null)
                return null;

            var path = GetManifestMemberString(mod.manifest, "icon", "Icon", "thumbnail", "Thumbnail", "icon_path",
                "iconPath");
            if (string.IsNullOrWhiteSpace(path))
                return null;

            path = path.Trim();
            try
            {
                if (path.StartsWith("res://", StringComparison.Ordinal))
                {
                    if (ResourceLoader.Exists(path))
                        return PreloadManager.Cache.GetAsset<Texture2D>(path);
                    return GD.Load<Texture2D>(path);
                }

                if (File.Exists(path))
                {
                    var img = Image.LoadFromFile(path);
                    if (img != null)
                        return ImageTexture.CreateFromImage(img);
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }

        private static Texture2D? TryLoadVanillaModImageRes(string manifestId)
        {
            var path = $"res://{manifestId}/mod_image.png";
            try
            {
                if (!ResourceLoader.Exists(path))
                    return null;
                return PreloadManager.Cache.GetAsset<Texture2D>(path);
            }
            catch
            {
                try
                {
                    return GD.Load<Texture2D>(path);
                }
                catch
                {
                    return null;
                }
            }
        }

        private static string? GetManifestMemberString(object? manifest, params string[] names)
        {
            if (manifest == null)
                return null;

            var t = manifest.GetType();
            foreach (var name in names)
            {
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p?.GetValue(manifest) is string s && !string.IsNullOrWhiteSpace(s))
                    return s;

                var f = t.GetField(name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (f?.GetValue(manifest) is string s2 && !string.IsNullOrWhiteSpace(s2))
                    return s2;
            }

            return null;
        }
    }
}
