using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Ancients.Options;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Appends registered mod options to an <see cref="AncientEventModel" /> after the base game generates its
    ///         initial options.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在原版游戏生成 <see cref="AncientEventModel" /> 的初始选项后，追加已注册的模组选项。
    ///     </para>
    /// </summary>
    internal class AncientEventInitialOptionsRegistryPatch : IPatchMethod
    {
        public static string PatchId => "ancient_event_initial_options_registry";

        public static string Description =>
            "Append ModAncientOptionRegistry results into AncientEventModel initial option list";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AncientEventModel), "GenerateInitialOptionsWrapper")];
        }

        public static void Postfix(AncientEventModel __instance, ref IReadOnlyList<EventOption> __result)
        {
            ReplaceDebugRelicOptionWithPreparedOption(__instance, ref __result);

            if (ShouldSkipInjection(__result))
                return;

            var mutable = __result as List<EventOption> ?? [.. __result];
            var countBefore = mutable.Count;
            ModAncientOptionRegistry.AppendRegisteredOptions(__instance, mutable);

            if (mutable.Count == countBefore)
                return;

            GeneratedOptions(__instance) = mutable;
            __result = mutable;
        }

        private static bool ShouldSkipInjection(IReadOnlyList<EventOption> options)
        {
            if (options.Count != 1)
                return false;

            var only = options[0];
            return only.IsProceed && string.Equals(only.TextKey, "PROCEED", StringComparison.OrdinalIgnoreCase);
        }

        private static void ReplaceDebugRelicOptionWithPreparedOption(
            AncientEventModel ancient,
            ref IReadOnlyList<EventOption> options)
        {
            if (string.IsNullOrWhiteSpace(ancient.DebugOption) || options.Count == 0)
                return;

            var debugOption = options[0];
            if (debugOption.Relic == null)
                return;

            if (!TextKeyMatchesDebugOption(debugOption.TextKey, ancient.DebugOption))
                return;

            if (!TryPrepareDebugRelicOption(ancient, debugOption.Relic, out var preparedRelic))
                return;

            var replacement = CreateRelicOption(ancient, preparedRelic);
            var mutable = options as List<EventOption> ?? [.. options];
            mutable[0] = replacement;
            GeneratedOptions(ancient) = mutable;
            options = mutable;
        }

        private static bool TextKeyMatchesDebugOption(string textKey, string debugOption)
        {
            return textKey.Contains(debugOption, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryPrepareDebugRelicOption(
            AncientEventModel ancient,
            RelicModel sourceRelic,
            out RelicModel preparedRelic)
        {
            preparedRelic = sourceRelic;

            var owner = ancient.Owner;
            if (owner == null)
                return false;

            var setupMethod = sourceRelic.GetType().GetMethod(
                "SetupForPlayer",
                BindingFlags.Instance | BindingFlags.Public,
                [typeof(Player)]);

            if (setupMethod == null)
                return false;

            preparedRelic = sourceRelic.IsMutable ? sourceRelic : sourceRelic.ToMutable();
            preparedRelic.Owner = owner;

            try
            {
                var result = setupMethod.Invoke(preparedRelic, [owner]);
                return result is not bool boolResult || boolResult;
            }
            catch (TargetInvocationException ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[AncientOption] Failed to prepare debug relic option '{sourceRelic.Id}' for ancient " +
                    $"'{ancient.Id}': {ex.InnerException ?? ex}");
                return false;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[AncientOption] Failed to prepare debug relic option '{sourceRelic.Id}' for ancient " +
                    $"'{ancient.Id}': {ex}");
                return false;
            }
        }

        private static EventOption CreateRelicOption(AncientEventModel ancient, RelicModel relic)
        {
#if STS2_AT_LEAST_0_113_0
            return RelicOption(ancient, relic, "INITIAL");
#else
            return RelicOption(ancient, relic, "INITIAL", null);
#endif
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "RelicOption")]
#if STS2_AT_LEAST_0_113_0
        private static extern EventOption RelicOption(AncientEventModel ancient, RelicModel relic, string pageName);
#else
        private static extern EventOption RelicOption(
            AncientEventModel ancient, RelicModel relic, string pageName, string? customDonePage);
#endif

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_generatedOptions")]
        private static extern ref List<EventOption>? GeneratedOptions(AncientEventModel instance);
    }
}
