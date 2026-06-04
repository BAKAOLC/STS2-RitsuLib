using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Interop.Patches
{
    /// <summary>
    ///     Injects all loaded mod model types that declare <see cref="SavedPropertyAttribute" /> at a deterministic
    ///     initialization point so multiplayer peers build the same <see cref="SavedPropertiesTypeCache" /> net-id table.
    ///     在确定性的初始化点注入所有声明 <see cref="SavedPropertyAttribute" /> 的已加载 mod 模型类型，
    ///     使多人对等端构建相同的 <see cref="SavedPropertiesTypeCache" /> net-id 表。
    /// </summary>
    public sealed class SavedPropertiesTypeCacheInjectionPatch : IPatchMethod
    {
        private static readonly Lock Gate = new();
        private static bool _completed;

        /// <inheritdoc />
        public static string PatchId => "ritsulib_saved_properties_type_cache_injection";

        /// <inheritdoc />
        public static string Description =>
            "Deterministic SavedPropertiesTypeCache injection for modded models with SavedProperty";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(LocManager), nameof(LocManager.Initialize))];
        }

        /// <summary>
        ///     Injects cache entries after mod type-discovery contributors have had a chance to register content.
        ///     在 mod 类型发现贡献器有机会注册内容后注入缓存条目。
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Prefix()
        {
            lock (Gate)
            {
                if (_completed)
                    return;
                _completed = true;
            }

            var modelTypes = GetModModelTypesWithSavedProperties().ToArray();
            if (modelTypes.Length == 0)
                return;

            var beforeCount = GetPropertyNameCount();
            var injectedTypes = 0;

            foreach (var modelType in modelTypes)
            {
                if (SavedPropertiesTypeCache.GetJsonPropertiesForType(modelType) != null)
                    continue;

                SavedPropertiesTypeCache.InjectTypeIntoCache(modelType);
                injectedTypes++;
            }

            RefreshNetIdBitSize();

            var afterCount = GetPropertyNameCount();
            if (injectedTypes > 0 || afterCount != beforeCount)
                RitsuLibFramework.Logger.Info(
                    $"[SavedProperties] Injected {injectedTypes} mod model type(s); property net IDs: {beforeCount} -> {afterCount}, bit size {SavedPropertiesTypeCache.NetIdBitSize}.");
        }

        private static bool HasSavedProperty(Type modelType)
        {
            return modelType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(property => property.GetCustomAttribute<SavedPropertyAttribute>() != null);
        }

        private static IEnumerable<Type> GetModModelTypesWithSavedProperties()
        {
            return ModManager.GetLoadedMods()
                .Select(static mod => new
                {
                    ModId = mod.manifest?.id ?? mod.assembly?.GetName().Name ?? mod.path,
                    mod.assembly,
                })
                .Where(static mod => mod.assembly != null)
                .OrderBy(static mod => mod.ModId, StringComparer.Ordinal)
                .ThenBy(static mod => mod.assembly!.FullName, StringComparer.Ordinal)
                .SelectMany(static mod =>
                    AssemblyTypeScanHelper.GetLoadableTypes(mod.assembly!, RitsuLibFramework.Logger))
                .Where(static type =>
                    type is { IsAbstract: false, IsInterface: false } &&
                    typeof(AbstractModel).IsAssignableFrom(type) &&
                    HasSavedProperty(type))
                .Distinct()
                .OrderBy(static type => type.Assembly.GetName().Name, StringComparer.Ordinal)
                .ThenBy(static type => type.Assembly.FullName, StringComparer.Ordinal)
                .ThenBy(static type => type.FullName ?? type.Name, StringComparer.Ordinal);
        }

        private static int GetPropertyNameCount()
        {
            return GetNetIdToPropertyNameMap()?.Count ?? 0;
        }

        private static void RefreshNetIdBitSize()
        {
            var count = GetPropertyNameCount();
            if (count <= 0)
                return;

            var newBitSize = Mathf.CeilToInt(Mathf.Log(count) / Mathf.Log(2));
            AccessTools.Property(typeof(SavedPropertiesTypeCache), nameof(SavedPropertiesTypeCache.NetIdBitSize))
                ?.SetValue(null, newBitSize);
        }

        private static List<string>? GetNetIdToPropertyNameMap()
        {
            return AccessTools.DeclaredField(typeof(SavedPropertiesTypeCache), "_netIdToPropertyNameMap")
                ?.GetValue(null) as List<string>;
        }
    }
}
