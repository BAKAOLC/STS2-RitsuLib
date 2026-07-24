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
    ///     Appends registered mod rules into <see cref="AncientEventModel" /> initial options after vanilla generation.
    ///     原版生成后，将已注册 mod 规则追加到 <see cref="AncientEventModel" /> 初始选项中。
    /// </summary>
    internal class AncientEventInitialOptionsRegistryPatch : IPatchMethod
    {
        private static readonly MethodInfo RelicOptionMethod = typeof(AncientEventModel)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Single(static method =>
            {
                if (method.Name != "RelicOption" || method.IsGenericMethodDefinition)
                    return false;

                var parameters = method.GetParameters();
                return parameters.Length == 3
                       && parameters[0].ParameterType == typeof(RelicModel)
                       && parameters[1].ParameterType == typeof(string)
                       && parameters[2].ParameterType == typeof(string);
            });

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
            return (EventOption)RelicOptionMethod.Invoke(ancient, [relic, "INITIAL", null])!;
        }

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_generatedOptions")]
        private static extern ref List<EventOption>? GeneratedOptions(AncientEventModel instance);
    }
}
