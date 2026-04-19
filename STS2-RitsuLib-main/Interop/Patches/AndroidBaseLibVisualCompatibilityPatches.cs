using System.Collections.Concurrent;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Godot;

namespace STS2RitsuLib.Interop.Patches
{
    internal static class AndroidBaseLibVisualCompatHelper
    {
        private static readonly MethodInfo? MonsterCreateVisualsMethod =
            typeof(MonsterModel).GetMethod(
                nameof(MonsterModel.CreateVisuals),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private sealed class CompatAccessors
        {
            public PropertyInfo? CustomVisualsPathProperty { get; init; }
            public PropertyInfo? CustomVisualPathProperty { get; init; }
            public PropertyInfo? VisualsPathProperty { get; init; }
            public MethodInfo? SetupCustomAnimationStatesMethod { get; init; }
        }

        private static readonly ConcurrentDictionary<Type, CompatAccessors> AccessorCache = new();
        private static readonly ConcurrentDictionary<string, byte> LoggedVisualConversions = new();
        private static readonly ConcurrentDictionary<string, byte> LoggedAnimatorConversions = new();

        internal static bool TryCreateCreatureVisuals(
            object model,
            bool includeBaseVisualsPath,
            out NCreatureVisuals? visuals)
        {
            visuals = null;

            if (!TryResolveVisualScenePath(model, includeBaseVisualsPath, out var scenePath, out var sourcePropertyName))
                return false;

            try
            {
                visuals = RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(scenePath);
                if (visuals == null)
                    return false;

                LogVisualConversionOnce(model.GetType(), sourcePropertyName, scenePath);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[AndroidCompat] Failed to convert visuals for '{model.GetType().FullName}' from '{scenePath}': {ex.GetBaseException().Message}");
                return false;
            }
        }

        internal static bool TryCreateCreatureVisuals(object model, out NCreatureVisuals? visuals)
        {
            return TryCreateCreatureVisuals(model, false, out visuals);
        }

        internal static bool TryCreateAnimator(object model, MegaSprite controller, out CreatureAnimator? animator)
        {
            animator = null;

            var setupMethod = GetAccessors(model.GetType()).SetupCustomAnimationStatesMethod;
            if (setupMethod == null)
                return false;

            try
            {
                animator = setupMethod.Invoke(model, [controller]) as CreatureAnimator;
                if (animator == null)
                    return false;

                LogAnimatorConversionOnce(model.GetType());
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[AndroidCompat] Failed to create custom animator for '{model.GetType().FullName}': {ex.GetBaseException().Message}");
                return false;
            }
        }

        private static CompatAccessors GetAccessors(Type type)
        {
            return AccessorCache.GetOrAdd(type, static currentType =>
            {
                const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                var customVisualsPathProperty = currentType.GetProperty("CustomVisualsPath", instanceFlags);
                if (customVisualsPathProperty?.PropertyType != typeof(string))
                    customVisualsPathProperty = null;

                var visualPathProperty = currentType.GetProperty("CustomVisualPath", instanceFlags);
                if (visualPathProperty?.PropertyType != typeof(string))
                    visualPathProperty = null;

                var visualsPathProperty = currentType.GetProperty("VisualsPath", instanceFlags);
                if (visualsPathProperty?.PropertyType != typeof(string))
                    visualsPathProperty = null;

                var setupMethod = currentType.GetMethod("SetupCustomAnimationStates", instanceFlags, [typeof(MegaSprite)]);
                if (setupMethod != null && !typeof(CreatureAnimator).IsAssignableFrom(setupMethod.ReturnType))
                    setupMethod = null;

                return new CompatAccessors
                {
                    CustomVisualsPathProperty = customVisualsPathProperty,
                    CustomVisualPathProperty = visualPathProperty,
                    VisualsPathProperty = visualsPathProperty,
                    SetupCustomAnimationStatesMethod = setupMethod,
                };
            });
        }

        private static bool TryResolveVisualScenePath(
            object model,
            bool includeBaseVisualsPath,
            out string scenePath,
            out string sourcePropertyName)
        {
            var accessors = GetAccessors(model.GetType());

            if (TryGetScenePath(model, accessors.CustomVisualsPathProperty, out scenePath, out sourcePropertyName))
                return true;

            if (TryGetScenePath(model, accessors.CustomVisualPathProperty, out scenePath, out sourcePropertyName))
                return true;

            if (includeBaseVisualsPath &&
                TryGetScenePath(model, accessors.VisualsPathProperty, out scenePath, out sourcePropertyName))
            {
                return true;
            }

            scenePath = string.Empty;
            sourcePropertyName = string.Empty;
            return false;
        }

        private static bool TryGetScenePath(
            object model,
            PropertyInfo? property,
            out string scenePath,
            out string sourcePropertyName)
        {
            sourcePropertyName = property?.Name ?? string.Empty;
            scenePath = property?.GetValue(model) as string ?? string.Empty;
            return !string.IsNullOrWhiteSpace(scenePath);
        }

        private static void LogVisualConversionOnce(Type type, string sourcePropertyName, string scenePath)
        {
            if (!LoggedVisualConversions.TryAdd($"{type.FullName}:{sourcePropertyName}:{scenePath}", 0))
                return;

            RitsuLibFramework.Logger.Info(
                $"[AndroidCompat] Routed visuals for '{type.FullName}' through RitsuGodotNodeFactories via '{sourcePropertyName}': {scenePath}");
        }

        private static void LogAnimatorConversionOnce(Type type)
        {
            if (!LoggedAnimatorConversions.TryAdd(type.FullName ?? type.Name, 0))
                return;

            RitsuLibFramework.Logger.Info(
                $"[AndroidCompat] Routed custom animator for '{type.FullName}' through reflective BaseLib compatibility.");
        }

        internal static bool ShouldForceMonsterCreateVisualsFallback()
        {
            return MonsterCreateVisualsMethod != null &&
                   AndroidBaseLibLegacyGeneratedCompatHelper.HasLegacyCompatOwner(MonsterCreateVisualsMethod);
        }
    }

    internal class AndroidBaseLibMonsterCreateVisualsCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "android_baselib_monster_create_visuals_compat";

        public static bool IsCritical => false;

        public static string Description =>
            "Convert BaseLib-style custom monster visuals through RitsuGodotNodeFactories on Android";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(MonsterModel), nameof(MonsterModel.CreateVisuals))];
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(MonsterModel __instance, ref NCreatureVisuals __result)
        {
            if (!OperatingSystem.IsAndroid())
                return true;

            var includeBaseVisualsPath = AndroidBaseLibVisualCompatHelper.ShouldForceMonsterCreateVisualsFallback();

            if (!AndroidBaseLibVisualCompatHelper.TryCreateCreatureVisuals(
                    __instance,
                    includeBaseVisualsPath,
                    out var visuals) || visuals == null)
            {
                return true;
            }

            __result = visuals;
            return false;
        }
    }

    internal class AndroidBaseLibCharacterCreateVisualsCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "android_baselib_character_create_visuals_compat";

        public static bool IsCritical => false;

        public static string Description =>
            "Convert BaseLib-style custom character visuals through RitsuGodotNodeFactories on Android";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals))];
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(CharacterModel __instance, ref NCreatureVisuals __result)
        {
            if (!OperatingSystem.IsAndroid())
                return true;

            if (!AndroidBaseLibVisualCompatHelper.TryCreateCreatureVisuals(__instance, out var visuals) || visuals == null)
            {
                return true;
            }

            __result = visuals;
            return false;
        }
    }

    internal class AndroidBaseLibMonsterGenerateAnimatorCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "android_baselib_monster_generate_animator_compat";

        public static bool IsCritical => false;

        public static string Description =>
            "Call BaseLib-style custom monster animator setup on Android";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(MonsterModel), nameof(MonsterModel.GenerateAnimator))];
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(MonsterModel __instance, MegaSprite controller, ref CreatureAnimator __result)
        {
            if (!OperatingSystem.IsAndroid())
                return true;

            if (!AndroidBaseLibVisualCompatHelper.TryCreateAnimator(__instance, controller, out var animator) || animator == null)
                return true;

            __result = animator;
            return false;
        }
    }

    internal class AndroidBaseLibCharacterGenerateAnimatorCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "android_baselib_character_generate_animator_compat";

        public static bool IsCritical => false;

        public static string Description =>
            "Call BaseLib-style custom character animator setup on Android";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.GenerateAnimator))];
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(CharacterModel __instance, MegaSprite controller, ref CreatureAnimator __result)
        {
            if (!OperatingSystem.IsAndroid())
                return true;

            if (!AndroidBaseLibVisualCompatHelper.TryCreateAnimator(__instance, controller, out var animator) || animator == null)
                return true;

            __result = animator;
            return false;
        }
    }
}
