using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Interop.Patches
{
    public sealed class SavedPropertiesTypeCacheInjectionPatch : IPatchMethod
    {
        private static readonly Lock Gate = new();
        private static readonly HashSet<Type> ProcessedTypes = [];

        public static string PatchId => "ritsulib_saved_properties_type_cache_injection";

        public static string Description =>
            "Inject mod model types with SavedProperty into SavedPropertiesTypeCache on demand";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(SavedProperties), nameof(SavedProperties.FromInternal))];
        }

        public static void Prefix(object model)
        {
            if (model is not AbstractModel abstractModel)
                return;

            var modelType = abstractModel.GetType();
            lock (Gate)
            {
                if (!ProcessedTypes.Add(modelType))
                    return;
            }

            if (!HasSavedProperty(modelType))
                return;

            if (SavedPropertiesTypeCache.GetJsonPropertiesForType(modelType) != null)
                return;

            SavedPropertiesTypeCache.InjectTypeIntoCache(modelType);
        }

        private static bool HasSavedProperty(Type modelType)
        {
            return modelType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(property => property.GetCustomAttribute<SavedPropertyAttribute>() != null);
        }
    }
}
