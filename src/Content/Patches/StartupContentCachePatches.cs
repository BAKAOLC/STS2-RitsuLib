using System.Collections;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    internal static class StartupContentCache
    {
        private static readonly FieldInfo
            PoolRegistrations = RequiredField(typeof(ModHelper), "_moddedContentForPools");

        private static readonly Type PoolContentType = PoolRegistrations.FieldType.GetGenericArguments()[1];
        private static readonly FieldInfo PoolFrozen = RequiredField(PoolContentType, "isFrozen");
        private static readonly FieldInfo PoolModels = RequiredField(PoolContentType, "modelsToAdd");

        private static readonly FieldInfo[] CardPoolFields =
        [
            RequiredField(typeof(CardPoolModel), "_allCards"),
            RequiredField(typeof(CardPoolModel), "_allCardIds"),
        ];

        private static readonly FieldInfo[] RelicPoolFields =
        [
            RequiredField(typeof(RelicPoolModel), "_relics"),
            RequiredField(typeof(RelicPoolModel), "_allRelicIds"),
        ];

        private static readonly FieldInfo[] PotionPoolFields =
        [
            RequiredField(typeof(PotionPoolModel), "_allPotions"),
            RequiredField(typeof(PotionPoolModel), "_allPotionIds"),
        ];

        private static readonly FieldInfo CardPool = RequiredField(typeof(CardModel), "_pool");
        private static readonly FieldInfo ModelDbContent = RequiredField(typeof(ModelDb), "_contentById");

        private static readonly FieldInfo[] ModelDbCollections =
        [
            .. typeof(ModelDb)
                .GetFields(BindingFlags.Static | BindingFlags.NonPublic)
                .Where(static field => !field.IsInitOnly && typeof(IEnumerable).IsAssignableFrom(field.FieldType)),
        ];

        internal static void InvalidatePool(AbstractModel pool)
        {
            var fields = pool switch
            {
                CardPoolModel => CardPoolFields,
                RelicPoolModel => RelicPoolFields,
                PotionPoolModel => PotionPoolFields,
                _ => [],
            };
            foreach (var field in fields)
                field.SetValue(pool, null);
        }

        internal static void ReopenPool(Type poolType)
        {
            if (PoolRegistrations.GetValue(null) is not IDictionary registrations ||
                registrations[poolType] is not { } registration)
                return;

            PoolFrozen.SetValue(registration, false);
            if (PoolModels.GetValue(registration) == null)
                PoolModels.SetValue(registration, new List<Type>());
        }

        internal static void PrepareRegistration(Type poolType)
        {
            ReopenPool(poolType);
            foreach (var model in GetCanonicalModels())
            {
                if (model.GetType() == poolType)
                    InvalidatePool(model);
                if (model is CardModel)
                    CardPool.SetValue(model, null);
            }

            InvalidateModelDbCollections();
        }

        internal static void FinishRegistration()
        {
            foreach (var model in GetCanonicalModels())
            {
                InvalidatePool(model);
                if (model is CardModel)
                    CardPool.SetValue(model, null);
            }

            InvalidateModelDbCollections();
        }

        private static void InvalidateModelDbCollections()
        {
            foreach (var field in ModelDbCollections)
                field.SetValue(null, null);
        }

        private static IEnumerable<AbstractModel> GetCanonicalModels()
        {
            return ((Dictionary<ModelId, AbstractModel>)ModelDbContent.GetValue(null)!).Values;
        }

        private static FieldInfo RequiredField(Type type, string name)
        {
            return AccessTools.DeclaredField(type, name) ?? throw new MissingFieldException(type.FullName, name);
        }
    }

    internal sealed class StartupPoolCachePatch : IPatchMethod
    {
        public static string PatchId => "startup_pool_cache";

        public static string Description =>
            "Keep early pool queries from freezing content registration or caching partial content";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardPoolModel), nameof(CardPoolModel.AllCards), MethodType.Getter),
                new(typeof(CardPoolModel), nameof(CardPoolModel.AllCardIds), MethodType.Getter),
                new(typeof(RelicPoolModel), nameof(RelicPoolModel.AllRelics), MethodType.Getter),
                new(typeof(RelicPoolModel), nameof(RelicPoolModel.AllRelicIds), MethodType.Getter),
                new(typeof(PotionPoolModel), nameof(PotionPoolModel.AllPotions), MethodType.Getter),
                new(typeof(PotionPoolModel), nameof(PotionPoolModel.AllPotionIds), MethodType.Getter),
            ];
        }

        public static void Prefix(AbstractModel __instance, out bool __state)
        {
            __state = !ModContentRegistry.IsFrozen;
            if (__state)
                StartupContentCache.InvalidatePool(__instance);
        }

        public static void Finalizer(AbstractModel __instance, bool __state)
        {
            if (!__state || ModContentRegistry.IsFrozen)
                return;

            StartupContentCache.InvalidatePool(__instance);
            StartupContentCache.ReopenPool(__instance.GetType());
        }
    }

    internal sealed class StartupPoolRegistrationPatch : IPatchMethod
    {
        public static string PatchId => "startup_pool_registration";
        public static string Description => "Refresh early pool snapshots before accepting additional mod content";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModHelper), nameof(ModHelper.AddModelToPool), [typeof(Type), typeof(Type)])];
        }

        public static void Prefix(Type poolType)
        {
            if (!ModContentRegistry.IsFrozen)
                StartupContentCache.PrepareRegistration(poolType);
        }
    }
}
