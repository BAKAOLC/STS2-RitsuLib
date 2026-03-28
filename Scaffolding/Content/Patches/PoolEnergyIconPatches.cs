using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Implement on a pool model to override the large energy icon path resolved from
    ///     <see cref="EnergyIconHelper.GetPath(string)" />.
    /// </summary>
    public interface IModBigEnergyIconPool
    {
        /// <summary>
        ///     Custom large energy icon path for this pool’s <see cref="MegaCrit.Sts2.Core.Models.IPoolModel.EnergyColorName" />.
        /// </summary>
        string? BigEnergyIconPath { get; }
    }

    /// <summary>
    ///     Prefixes <see cref="EnergyIconHelper.GetPath(string)" /> so pools implementing <see cref="IModBigEnergyIconPool" />
    ///     can replace the resolved big icon path.
    /// </summary>
    public class EnergyIconHelperPathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "energy_icon_helper_big_icon_override";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Allow mod pools to override the large energy icon path resolved by EnergyIconHelper";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(EnergyIconHelper), nameof(EnergyIconHelper.GetPath), [typeof(string)]),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Delegates to <see cref="ModBigEnergyIconHelper.TryOverridePath" /> to substitute a cached pool override.
        /// </summary>
        public static bool Prefix(string prefix, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ModBigEnergyIconHelper.TryOverridePath(prefix, ref __result);
        }
    }

    internal static class ModBigEnergyIconHelper
    {
        private static Dictionary<string, string>? _cache;

        public static bool TryOverridePath(string prefix, ref string result)
        {
            _cache ??= BuildCache();

            if (!_cache.TryGetValue(prefix, out var path))
                return true;

            result = path;
            return false;
        }

        private static Dictionary<string, string> BuildCache()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var character in ModContentRegistry.GetModCharacters())
                AddPoolIfMapped(dict, character.CardPool);

            foreach (var pool in ModelDb.AllCards.Select(c => c.Pool).Distinct())
                AddPoolIfMapped(dict, pool);

            foreach (var pool in ModelDb.AllRelics.Select(r => r.Pool).Distinct())
                AddPoolIfMapped(dict, pool);

            foreach (var pool in ModelDb.AllPotions.Select(p => p.Pool).Distinct())
                AddPoolIfMapped(dict, pool);

            return dict;
        }

        private static void AddPoolIfMapped(Dictionary<string, string> dict, IPoolModel pool)
        {
            if (pool is not IModBigEnergyIconPool mapped)
                return;

            if (string.IsNullOrWhiteSpace(mapped.BigEnergyIconPath))
                return;

            if (!AssetPathDiagnostics.Exists(mapped.BigEnergyIconPath!, pool,
                    nameof(IModBigEnergyIconPool.BigEnergyIconPath)))
                return;

            dict.TryAdd(pool.EnergyColorName, mapped.BigEnergyIconPath!);
        }
    }
}
