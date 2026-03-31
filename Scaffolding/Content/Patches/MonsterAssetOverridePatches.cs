using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Optional creature visuals scene path (vanilla <c>MonsterModel.VisualsPath</c>); use
    ///     <see cref="ModMonsterTemplate" /> or implement on a mod <see cref="MonsterModel" />.
    /// </summary>
    public interface IModMonsterAssetOverrides
    {
        /// <summary>
        ///     Path bundle; <c>Custom*</c> properties mirror these fields unless overridden.
        /// </summary>
        MonsterAssetProfile AssetProfile => MonsterAssetProfile.Empty;

        /// <summary>
        ///     Override packed scene path for combat creature visuals.
        /// </summary>
        string? CustomVisualsPath => AssetProfile.VisualsScenePath;
    }

    /// <summary>
    ///     Patches <see cref="MonsterModel.VisualsPath" /> for <see cref="IModMonsterAssetOverrides" />.
    /// </summary>
    public class MonsterVisualsPathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_monster_visuals_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod monsters to override VisualsPath";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(MonsterModel), "get_VisualsPath")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModMonsterAssetOverrides.CustomVisualsPath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(MonsterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModMonsterAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomVisualsPath,
                nameof(IModMonsterAssetOverrides.CustomVisualsPath));
        }
    }
}
