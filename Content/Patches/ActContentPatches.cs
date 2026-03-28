using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    /// <summary>
    ///     Bootstrap dynamic act patching after all mods are loaded but before ModelDb begins caching content.
    ///     This avoids hardcoding base-game acts and supports act/map mods from other assemblies.
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
        /// </summary>
        public static void Prefix()
        {
            DynamicActContentPatcher.EnsurePatched();
        }
    }
}
