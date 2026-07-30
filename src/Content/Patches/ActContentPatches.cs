using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies dynamic content patches to all loaded act types before <see cref="ModelDb" />
    ///         initializes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <see cref="ModelDb" /> 初始化前向所有已加载章节类型应用动态内容补丁。
    ///     </para>
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
