using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines an optional monster combat-visual scene-path override. Mods may use
    ///         <see cref="ModMonsterTemplate" /> or implement this interface on a <see cref="MonsterModel" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义可选的怪物战斗视觉场景路径覆盖。模组可以使用 <see cref="ModMonsterTemplate" />，
    ///         或在 <see cref="MonsterModel" /> 上实现此接口。
    ///     </para>
    /// </summary>
    public interface IModMonsterAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the monster asset profile.</para>
        ///     <para xml:lang="zh-CN">获取怪物资源配置。</para>
        /// </summary>
        MonsterAssetProfile AssetProfile => MonsterAssetProfile.Empty;

        /// <summary>
        ///     <para xml:lang="en">Gets the combat-visual <see cref="PackedScene" /> path override.</para>
        ///     <para xml:lang="zh-CN">获取战斗视觉 <see cref="PackedScene" /> 路径覆盖。</para>
        /// </summary>
        string? CustomVisualsPath => AssetProfile.VisualsScenePath;
    }

    /// <summary>
    ///     <para xml:lang="en">Applies custom combat-visual scene paths to <see cref="MonsterModel.VisualsPath" />.</para>
    ///     <para xml:lang="zh-CN">将自定义战斗视觉场景路径应用到 <see cref="MonsterModel.VisualsPath" />。</para>
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
        ///     <para xml:lang="en">Applies an available monster combat-visual path override.</para>
        ///     <para xml:lang="zh-CN">应用可用的怪物战斗视觉路径覆盖。</para>
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
