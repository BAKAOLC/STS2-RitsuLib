using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    /// <summary>
    ///     Bootstrap dynamic act patching after all mods are loaded but before ModelDb begins caching content.
    ///     This avoids hardcoding base-game acts and supports act/map mods from other assemblies.
    ///     在所有 mod 加载完成后、ModelDb 开始缓存内容前引导动态章节补丁。
    ///     这避免了硬编码基础游戏章节，并支持来自其它程序集的章节/地图 mod。
    /// </summary>
    public class DynamicActContentPatchBootstrap : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "dynamic_act_content_patch_bootstrap";

        /// <inheritdoc />
        public static string Description =>
            "Dynamically patch all loaded ActModel implementations for registered events and ancients";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), nameof(ModelDb.Init))];
        }

        /// <summary>
        ///     Ensures dynamic Harmony patches are applied to every concrete <see cref="ActModel" /> before
        ///     <see cref="ModelDb.Init" /> runs.
        ///     确保在 <see cref="ModelDb.Init" /> 运行前，对每个具体 <see cref="ActModel" /> 应用
        ///     动态 Harmony 补丁。
        /// </summary>
        public static void Prefix()
        {
            DynamicActContentPatcher.EnsurePatched();
        }
    }
}
