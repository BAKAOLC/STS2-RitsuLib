using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Builders;
using STS2RitsuLib.Patching.Core;

namespace STS2RitsuLib.Content
{
    internal static class DynamicCharacterStarterContentPatcher
    {
        private static readonly Lock SyncRoot = new();
        private static bool _patched;

        internal static void EnsurePatched()
        {
            lock (SyncRoot)
            {
                if (_patched)
                    return;

                var logger = RitsuLibFramework.Logger;
                var characterTypes = ReflectionHelper.GetSubtypes<CharacterModel>()
                    .Concat(ReflectionHelper.GetSubtypesInMods<CharacterModel>())
                    .Distinct()
                    .ToArray();

                var builder = new DynamicPatchBuilder("dynamic_character_starter");
                var deckPostfix =
                    DynamicPatchBuilder.FromMethod(typeof(DynamicCharacterStarterContentPatcher),
                        nameof(StartingDeckPostfix));
                var relicPostfix =
                    DynamicPatchBuilder.FromMethod(typeof(DynamicCharacterStarterContentPatcher),
                        nameof(StartingRelicsPostfix));
                var potionPostfix =
                    DynamicPatchBuilder.FromMethod(typeof(DynamicCharacterStarterContentPatcher),
                        nameof(StartingPotionsPostfix));

                var queuedGetters = new HashSet<MethodInfo>();

                foreach (var characterType in characterTypes)
                {
                    TryAddCharacterPropertyGetter(
                        builder,
                        characterType,
                        nameof(CharacterModel.StartingDeck),
                        deckPostfix,
                        queuedGetters,
                        logger);
                    TryAddCharacterPropertyGetter(
                        builder,
                        characterType,
                        nameof(CharacterModel.StartingRelics),
                        relicPostfix,
                        queuedGetters,
                        logger);
                    TryAddCharacterPropertyGetter(
                        builder,
                        characterType,
                        nameof(CharacterModel.StartingPotions),
                        potionPostfix,
                        queuedGetters,
                        logger);
                }

                if (!RitsuLibFramework
                        .GetFrameworkPatcher(RitsuLibFramework.FrameworkPatcherArea.ContentRegistry)
                        .ApplyDynamic(builder))
                    throw new InvalidOperationException("Failed to apply dynamic character starter content patches.");

                _patched = true;
                logger.Info(
                    $"[Content] Dynamic character starter patching initialized for {characterTypes.Length} character type(s).");
            }
        }

        private static void TryAddCharacterPropertyGetter(
            DynamicPatchBuilder builder,
            Type concreteCharacterType,
            string propertyName,
            HarmonyMethod postfix,
            HashSet<MethodInfo> queuedGetters,
            Logger logger)
        {
            var getter = FindDeclaredPropertyGetter(concreteCharacterType, propertyName);
            if (getter == null || !queuedGetters.Add(getter))
                return;

            try
            {
                builder.Add(
                    getter,
                    postfix: postfix,
                    description: $"Patch character starter merge {getter.DeclaringType?.Name}.{propertyName}");
            }
            catch (Exception ex)
            {
                queuedGetters.Remove(getter);
                logger.Warn(
                    $"[Content] Could not patch '{getter.DeclaringType?.Name}.{propertyName}' for starter merge: {ex.Message}");
            }
        }

        private static MethodInfo? FindDeclaredPropertyGetter(Type concreteCharacterType, string propertyName)
        {
            for (var walk = concreteCharacterType;
                 walk != null && typeof(CharacterModel).IsAssignableFrom(walk);
                 walk = walk.BaseType)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                           BindingFlags.DeclaredOnly;
                var prop = walk.GetProperty(propertyName, flags);
                if (prop?.GetMethod != null)
                    return prop.GetMethod;
            }

            return null;
        }

        // ReSharper disable InconsistentNaming
        private static void StartingDeckPostfix(CharacterModel __instance, ref IEnumerable<CardModel> __result)
            // ReSharper restore InconsistentNaming
        {
            var extraTypes = ModContentRegistry.GetRegisteredCharacterStarterCards(__instance.GetType());
            if (extraTypes.Length == 0)
                return;

            __result = __result
                .Concat(extraTypes.Select(t => ModelDb.GetById<CardModel>(ModelDb.GetId(t))))
                .ToList();
        }

        // ReSharper disable InconsistentNaming
        private static void StartingRelicsPostfix(CharacterModel __instance, ref IReadOnlyList<RelicModel> __result)
            // ReSharper restore InconsistentNaming
        {
            var extraTypes = ModContentRegistry.GetRegisteredCharacterStarterRelics(__instance.GetType());
            if (extraTypes.Length == 0)
                return;

            __result = __result
                .Concat(extraTypes.Select(t => ModelDb.GetById<RelicModel>(ModelDb.GetId(t))))
                .ToList();
        }

        // ReSharper disable InconsistentNaming
        private static void StartingPotionsPostfix(CharacterModel __instance, ref IReadOnlyList<PotionModel> __result)
            // ReSharper restore InconsistentNaming
        {
            var extraTypes = ModContentRegistry.GetRegisteredCharacterStarterPotions(__instance.GetType());
            if (extraTypes.Length == 0)
                return;

            __result = __result
                .Concat(extraTypes.Select(t => ModelDb.GetById<PotionModel>(ModelDb.GetId(t))))
                .ToList();
        }
    }
}
