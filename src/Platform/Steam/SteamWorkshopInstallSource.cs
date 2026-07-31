using System.Reflection;

namespace STS2RitsuLib.Platform.Steam
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Identifies Steam Workshop item IDs from assembly or file paths beneath Steam's Workshop content
    ///         directory.
    ///     </para>
    ///     <para xml:lang="zh-CN">从 Steam 创意工坊内容目录下的程序集或文件路径识别创意工坊物品 ID。</para>
    /// </summary>
    internal static class SteamWorkshopInstallSource
    {
        internal static bool IsAssemblyLoadedFromSteamWorkshop(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            return TryGetWorkshopItemIdFromAssembly(assembly, out _);
        }

        internal static bool IsAssemblyLoadedFromSteamWorkshopItem(Assembly assembly, ulong itemId)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            return TryGetWorkshopItemIdFromAssembly(assembly, out var loadedItemId) && loadedItemId == itemId;
        }

        internal static bool TryGetWorkshopItemIdFromAssembly(Assembly assembly, out ulong itemId)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            try
            {
                if (TryGetWorkshopItemIdFromPath(assembly.Location, out itemId))
                    return true;
            }
            catch (NotSupportedException)
            {
                // Dynamic assemblies may not expose a location.
            }

            try
            {
                return TryGetWorkshopItemIdFromPath(assembly.ManifestModule.FullyQualifiedName, out itemId);
            }
            catch (NotSupportedException)
            {
                itemId = 0;
                return false;
            }
        }

        internal static bool IsPathLoadedFromSteamWorkshop(string? path)
        {
            return TryGetWorkshopItemIdFromPath(path, out _);
        }

        internal static bool IsPathLoadedFromSteamWorkshopItem(string? path, ulong itemId)
        {
            return TryGetWorkshopItemIdFromPath(path, out var loadedItemId) && loadedItemId == itemId;
        }

        internal static bool TryGetWorkshopItemIdFromPath(string? path, out ulong itemId)
        {
            itemId = 0;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch
            {
                fullPath = path;
            }

            var parts = fullPath
                .Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var i = 0; i + 4 < parts.Length; i++)
                if (string.Equals(parts[i], "steamapps", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(parts[i + 1], "workshop", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(parts[i + 2], "content", StringComparison.OrdinalIgnoreCase) &&
                    ulong.TryParse(parts[i + 4], out itemId))
                    return true;

            itemId = 0;
            return false;
        }
    }
}
