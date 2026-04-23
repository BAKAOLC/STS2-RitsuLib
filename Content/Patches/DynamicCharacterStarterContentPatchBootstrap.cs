using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    /// <summary>
    ///     Applies dynamic Harmony postfixes so <see cref="ModContentRegistry" /> character-starter registrations merge
    ///     into every concrete <see cref="CharacterModel" /> (vanilla and mod) before <see cref="ModelDb.Init" /> caches
    ///     content.
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
        /// </summary>
        public static void Prefix()
        {
            DynamicCharacterStarterContentPatcher.EnsurePatched();
        }
    }
}
