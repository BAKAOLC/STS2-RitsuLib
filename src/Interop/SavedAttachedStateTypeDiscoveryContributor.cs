using System.Reflection;
using HarmonyLib;
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
                if (!IsSavedAttachedStateField(field))
                    continue;

                field.GetValue(null);
            }
        }

        private static bool IsSavedAttachedStateField(FieldInfo field)
        {
            var fieldType = field.FieldType;
            if (fieldType.ContainsGenericParameters)
                return false;

            return fieldType.IsGenericType &&
                   fieldType.GetGenericTypeDefinition() == typeof(SavedAttachedState<,>);
        }
    }
}
