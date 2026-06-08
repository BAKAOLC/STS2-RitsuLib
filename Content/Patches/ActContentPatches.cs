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
    internal class DynamicActContentPatchBootstrap : IPatchMethod
    {
        public static string PatchId => "dynamic_act_content_patch_bootstrap";

        public static string Description =>
            "Dynamically patch all loaded ActModel implementations for registered events, ancients, and encounters";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), nameof(ModelDb.Init))];
        }

        public static void Prefix()
        {
            DynamicActContentPatcher.EnsurePatched();
        }
    }
}
