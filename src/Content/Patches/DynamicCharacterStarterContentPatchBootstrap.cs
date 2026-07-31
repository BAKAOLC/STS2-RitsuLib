using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies starter-content patches to all loaded character types before
    ///         <see cref="ModelDb" /> initializes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <see cref="ModelDb" /> 初始化前向所有已加载角色类型应用初始内容补丁。
    ///     </para>
    /// </summary>
    internal sealed class DynamicCharacterStarterContentPatchBootstrap : IPatchMethod
    {
        public static string PatchId => "dynamic_character_starter_content_patch_bootstrap";

        public static string Description =>
            "Patch all CharacterModel starter property getters to merge registry character-starter content";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), nameof(ModelDb.Init))];
        }

        public static void Prefix()
        {
            DynamicCharacterStarterContentPatcher.EnsurePatched();
        }
    }
}
