using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Optional creature visuals scene path (vanilla <c>MonsterModel.VisualsPath</c>); use
    ///     <see cref="ModMonsterTemplate" /> or implement on a mod <see cref="MonsterModel" />.
    ///     可选生物视觉场景路径（原版 <c>MonsterModel.VisualsPath</c>）；使用
    ///     <see cref="ModMonsterTemplate" />，或在 mod <see cref="MonsterModel" /> 上实现。
    /// </summary>
    public interface IModMonsterAssetOverrides
    {
        /// <summary>
        ///     Path bundle; <c>Custom*</c> properties mirror these fields unless overridden.
        ///     路径包；除非被覆盖，否则 <c>Custom*</c> 属性会映射这些字段。
        /// </summary>
        MonsterAssetProfile AssetProfile => MonsterAssetProfile.Empty;

        /// <summary>
        ///     Override packed scene path for combat creature visuals.
        ///     覆盖战斗生物视觉的 packed scene 路径。
        /// </summary>
        string? CustomVisualsPath => AssetProfile.VisualsScenePath;
    }

    /// <summary>
    ///     Patches <see cref="MonsterModel.VisualsPath" /> for <see cref="IModMonsterAssetOverrides" />.
    ///     为 <see cref="IModMonsterAssetOverrides" /> 修补<see cref="MonsterModel.VisualsPath" />。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class MonsterVisualsPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_monster_visuals_path";
        public static string Description => "Allow mod monsters to override VisualsPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(MonsterModel), "VisualsPath", MethodType.Getter)];
        }

        /// <summary>
        ///     Supplies <see cref="IModMonsterAssetOverrides.CustomVisualsPath" /> when the resource exists.
        ///     当资源存在时提供 <see cref="IModMonsterAssetOverrides.CustomVisualsPath" />。
        /// </summary>
        public static bool Prefix(MonsterModel __instance, ref string __result)
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModMonsterAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomVisualsPath,
                nameof(IModMonsterAssetOverrides.CustomVisualsPath));
        }
    }
}
