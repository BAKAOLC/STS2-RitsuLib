using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Interop.Patches
{
    /// <summary>
    ///     Injects model types that declare <see cref="SavedPropertyAttribute" /> before
    ///     <see cref="SavedProperties.FromInternal" /> executes so modded save properties are recognized by
    ///     <see cref="SavedPropertiesTypeCache" />.
    /// </summary>
    public sealed class SavedPropertiesTypeCacheInjectionPatch : IPatchMethod
    {
        private static readonly Lock Gate = new();
        private static readonly HashSet<Type> ProcessedTypes = [];

        /// <inheritdoc />
        public static string PatchId => "ritsulib_saved_properties_type_cache_injection";

        /// <inheritdoc />
        public static string Description =>
            "On-demand SavedPropertiesTypeCache injection for modded models with SavedProperty";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(SavedProperties), nameof(SavedProperties.FromInternal))];
        }

        /// <summary>
        ///     Injects cache entries for model types with <see cref="SavedPropertyAttribute" /> before serialization.
        /// </summary>
        /// <param name="model">Current model instance being serialized.</param>
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
