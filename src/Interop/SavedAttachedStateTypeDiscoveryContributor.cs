using System.Reflection;
using HarmonyLib;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Interop
{
    internal sealed class SavedAttachedStateTypeDiscoveryContributor : IModTypeDiscoveryContributor
    {
        public void Contribute(
            Harmony harmony,
            IReadOnlyDictionary<string, Assembly> modAssembliesByManifestId,
            Type modType)
        {
            if (modType.ContainsGenericParameters)
                return;

            foreach (var field in modType.GetFields(
                         BindingFlags.Static |
                         BindingFlags.Public |
                         BindingFlags.NonPublic |
                         BindingFlags.DeclaredOnly))
            {
                if (!IsSavedAttachedStateField(field, modAssembliesByManifestId))
                    continue;

                field.GetValue(null);
            }
        }

        private static bool IsSavedAttachedStateField(
            FieldInfo field,
            IReadOnlyDictionary<string, Assembly> modAssembliesByManifestId)
        {
            Type fieldType;
            try
            {
                fieldType = field.FieldType;
            }
            catch (Exception ex) when (IsFieldTypeResolutionFailure(ex))
            {
                LogFieldTypeResolutionFailure(field, modAssembliesByManifestId, ex);
                return false;
            }

            if (fieldType.ContainsGenericParameters)
                return false;

            return fieldType.IsGenericType &&
                   fieldType.GetGenericTypeDefinition() == typeof(SavedAttachedState<,>);
        }

        private static void LogFieldTypeResolutionFailure(
            FieldInfo field,
            IReadOnlyDictionary<string, Assembly> modAssembliesByManifestId,
            Exception exception)
        {
            var declaringType = field.DeclaringType;
            var assembly = declaringType?.Assembly;
            var modId = assembly == null
                ? "<unknown>"
                : ResolveModId(assembly, modAssembliesByManifestId);
            var assemblyName = assembly?.FullName ?? "<unknown assembly>";
            var assemblyPath = assembly == null ? "<unknown>" : GetAssemblyPath(assembly);
            var typeName = declaringType?.FullName ?? "<unknown type>";

            RitsuLibFramework.Logger.Warn(
                $"[ModTypeDiscoveryHub] Skipped an unresolvable static field while discovering " +
                $"SavedAttachedState registrations. ModId='{modId}', Assembly='{assemblyName}', " +
                $"Path='{assemblyPath}', DeclaringType='{typeName}', Field='{field.Name}', " +
                $"ResolutionFailure='{exception.GetType().Name}: {exception.Message}'");
        }

        private static string ResolveModId(
            Assembly assembly,
            IReadOnlyDictionary<string, Assembly> modAssembliesByManifestId)
        {
            if (Sts2ModManagerCompat.TryGetLoadedModIdForAssembly(assembly, out var modId) ||
                ModTypeDiscoveryHub.TryResolveRegisteredModId(assembly, out modId))
                return modId;

            foreach (var (candidateModId, candidateAssembly) in modAssembliesByManifestId)
                if (candidateAssembly == assembly)
                    return candidateModId;

            return "<unknown>";
        }

        private static string GetAssemblyPath(Assembly assembly)
        {
            if (assembly.IsDynamic)
                return "<dynamic>";

            var location = assembly.Location;
            return string.IsNullOrWhiteSpace(location) ? "<unavailable>" : location;
        }

        private static bool IsFieldTypeResolutionFailure(Exception exception)
        {
            return exception is TypeLoadException or FileNotFoundException or FileLoadException or
                BadImageFormatException;
        }
    }
}
