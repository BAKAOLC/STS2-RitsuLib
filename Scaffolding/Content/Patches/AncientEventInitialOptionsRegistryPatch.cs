using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Ancients.Options;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Appends registered mod rules into <see cref="AncientEventModel" /> initial options after vanilla generation.
    /// </summary>
    public class AncientEventInitialOptionsRegistryPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<AncientEventModel, List<EventOption>?> GeneratedOptionsRef =
            AccessTools.FieldRefAccess<AncientEventModel, List<EventOption>?>("_generatedOptions");

        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "ancient_event_initial_options_registry";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Append ModAncientOptionRegistry results into AncientEventModel initial option list";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AncientEventModel), "GenerateInitialOptionsWrapper")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Appends matching registered options after vanilla generated initial options are materialized.
        /// </summary>
        public static void Postfix(AncientEventModel __instance, ref IReadOnlyList<EventOption> __result)
            // ReSharper restore InconsistentNaming
        {
            if (ShouldSkipInjection(__result))
                return;

            var mutable = __result as List<EventOption> ?? __result.ToList();
            var countBefore = mutable.Count;
            ModAncientOptionRegistry.AppendRegisteredOptions(__instance, mutable);

            if (mutable.Count == countBefore)
                return;

            GeneratedOptionsRef(__instance) = mutable;
            __result = mutable;
        }

        private static bool ShouldSkipInjection(IReadOnlyList<EventOption> options)
        {
            if (options.Count != 1)
                return false;

            var only = options[0];
            return only.IsProceed && string.Equals(only.TextKey, "PROCEED", StringComparison.OrdinalIgnoreCase);
        }
    }
}
