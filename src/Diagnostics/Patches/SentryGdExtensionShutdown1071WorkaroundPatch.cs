#if STS2_AT_LEAST_0_107_1

using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.Diagnostics.Patches
{
    /// <summary>
    ///     Temporary workaround for STS2 0.107.1+ native crashes when modded startup shuts down Sentry's
    ///     GDExtension before process exit. Remove this patch after the host game fixes the shutdown path.
    /// </summary>
    internal sealed class SentryGdExtensionShutdown1071WorkaroundPatch : IPatchMethod
    {
        private const string BaseLibSkipPatchTypeName =
            "BaseLib.Patches.Fixes.SkipSentryShutdownPatch";

        private const string BaseLibSkipMethodName = "SkipShutdown";
        private static readonly Version AffectedHostMinVersion = new(0, 107, 1);

        private static readonly MethodInfo SkipMethod =
            AccessTools.DeclaredMethod(typeof(SentryGdExtensionShutdown1071WorkaroundPatch),
                nameof(SkipNativeGdExtensionShutdown));

        private static int _logged;

        public static string PatchId => "sentry_gdextension_shutdown_01071_workaround";

        public static string Description =>
            "Skip STS2 0.107.1+ native Sentry GDExtension shutdown while preserving .NET Sentry shutdown";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(SentryService), nameof(SentryService.Shutdown), Type.EmptyTypes, true)];
        }

        [HarmonyBefore(Const.BaseLibHarmonyId)]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!IsAffectedHost())
                return instructions;

            var rewriter = HarmonyIlRewriter.From(instructions);
            const string operation = "[SentryCompat] Redirect native Sentry GDExtension shutdown";
            var report = rewriter.RedirectCalls(
                operation,
                static method => IsGodotObjectCall(method) ? SkipMethod : null,
                static code => code.Any(HarmonyIl.IsCall(IsEquivalentSkipCall)));
            report.RequireExactSitesOrAlreadySatisfied();

            return rewriter.InstructionsChecked(operation);
        }

        private static bool IsAffectedHost()
        {
            return Sts2HostVersion.Numeric >= AffectedHostMinVersion;
        }

        private static bool IsGodotObjectCall(MethodInfo method)
        {
            return method.DeclaringType == typeof(GodotObject) &&
                   method.Name == nameof(GodotObject.Call) &&
                   method.GetParameters().Select(static parameter => parameter.ParameterType)
                       .SequenceEqual([typeof(StringName), typeof(Variant[])]);
        }

        private static bool IsEquivalentSkipCall(MethodInfo method)
        {
            if (method == SkipMethod)
                return true;

            return string.Equals(method.DeclaringType?.FullName, BaseLibSkipPatchTypeName,
                       StringComparison.Ordinal) &&
                   string.Equals(method.Name, BaseLibSkipMethodName, StringComparison.Ordinal) &&
                   method.IsStatic &&
                   method.ReturnType == typeof(Variant) &&
                   method.GetParameters().Select(static parameter => parameter.ParameterType)
                       .SequenceEqual([typeof(GodotObject), typeof(StringName), typeof(Variant[])]);
        }

        private static Variant SkipNativeGdExtensionShutdown(GodotObject instance, StringName method, Variant[] args)
        {
            if (Interlocked.Exchange(ref _logged, 1) != 0)
                return default;

            PatchLog.For<SentryGdExtensionShutdown1071WorkaroundPatch>().Info(
                "[SentryCompat] Skipped native Sentry GDExtension shutdown for STS2 0.107.1+ workaround.");
            return default;
        }
    }
}

#endif
