using System.Reflection;
using System.Runtime.CompilerServices;

namespace STS2RitsuLib.Compat
{
    internal static class CommonIncompatibleModRegistry
    {
        private static readonly ConditionalWeakTable<Assembly, InspectionResult> InspectionCache = new();

        private static readonly IReadOnlyList<Definition> Definitions =
        [
            new("SpeedX",
                ["Ppdev.Sts2Compat.GameCompat", "SpeedX.MainFile", "SpeedX.Scripts.TurboRuntimeGuards"]),
            new("QuickLink",
                ["Ppdev.Sts2Compat.GameCompat", "QuickLink.MainFile", "QuickLink.Scripts.NetworkSignalManager"]),
            new("Rewind",
                ["Ppdev.Sts2Compat.GameCompat", "Rewind.MainFile", "Rewind.Scripts.TurnRewindManager"]),
            new("DamageMeter",
                ["Ppdev.Sts2Compat.GameCompat", "DamageMeter.MainFile", "DamageMeter.Scripts.CombatDataCollector"]),
            new("ModConfig",
            [
                "ModConfig.MainFile", "ModConfig.ModConfigApi", "ModConfig.SettingsTabInjector",
                "ModConfig.KeyCaptureNode",
            ]),
        ];

        internal static bool IsMatch(IEnumerable<Assembly> assemblies)
        {
            return assemblies.Any(assembly => InspectionCache.GetValue(assembly, InspectAssembly).IsMatch);
        }

        private static InspectionResult InspectAssembly(Assembly assembly)
        {
            return new(Definitions.Any(definition => definition.Matches(assembly)));
        }

        private sealed record Definition(
            string AssemblyName,
            IReadOnlyList<string> RequiredTypeNames)
        {
            internal bool Matches(Assembly assembly)
            {
                return string.Equals(assembly.GetName().Name, AssemblyName, StringComparison.Ordinal) &&
                       RequiredTypeNames.All(typeName => assembly.GetType(typeName, false) != null);
            }
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        private sealed record InspectionResult(bool IsMatch);
    }
}
