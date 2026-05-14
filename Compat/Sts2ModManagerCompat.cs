using MegaCrit.Sts2.Core.Modding;

namespace STS2RitsuLib.Compat
{
    /// <summary>
    ///     Central entry for enumerating the host <see cref="ModManager" />'s mod lists.
    ///     枚举宿主 <see cref="ModManager" /> 的 mod 列表的中央入口。
    /// </summary>
    internal static class Sts2ModManagerCompat
    {
        internal static IEnumerable<Mod> EnumerateLoadedModsWithAssembly()
        {
            return ModManager.GetLoadedMods();
        }

        /// <summary>
        ///     All registered mods (including disabled / not loaded), for manifest name/description lookup.
        ///     所有已注册 mod（包括禁用/未加载的 mod），用于清单名称/描述查找。
        /// </summary>
        internal static IEnumerable<Mod> EnumerateModsForManifestLookup()
        {
            return ModManager.Mods;
        }
    }
}
