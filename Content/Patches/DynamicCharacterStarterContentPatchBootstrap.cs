using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    /// <summary>
    ///     Applies dynamic Harmony postfixes so <see cref="ModContentRegistry" /> character-starter registrations merge
    ///     into every concrete <see cref="CharacterModel" /> (vanilla and mod) before <see cref="ModelDb.Init" /> caches
    ///     content.
    ///     应用动态 Harmony 后置补丁，使 <see cref="ModContentRegistry" /> 角色初始内容注册在
    ///     <see cref="ModelDb.Init" /> 缓存内容前合并进每个具体 <see cref="CharacterModel" />（原版和 mod）。
    /// </summary>
    public sealed class DynamicCharacterStarterContentPatchBootstrap : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "dynamic_character_starter_content_patch_bootstrap";

        /// <inheritdoc />
        public static string Description =>
            "Patch all CharacterModel starter property getters to merge registry character-starter content";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), nameof(ModelDb.Init))];
        }

        /// <summary>
        ///     Ensures starter merge patches are applied for every loaded character type before ModelDb initialization.
        ///     确保在 ModelDb 初始化前，为每个已加载角色类型应用初始内容合并补丁。
        /// </summary>
        public static void Prefix()
        {
            DynamicCharacterStarterContentPatcher.EnsurePatched();
        }
    }
}
