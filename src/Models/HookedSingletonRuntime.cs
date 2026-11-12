using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;
#if !STS2_AT_LEAST_0_113_0
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Modding;
#endif

namespace STS2RitsuLib.Models
{
    internal static class HookedSingletonRuntime
    {
#if !STS2_AT_LEAST_0_113_0
        private static readonly ConditionalWeakTable<RunState, HookedSingletonModel[]> Instances = new();
        private static bool _subscribed;
#endif

        internal static void Create(RunState state)
        {
            var types = ModContentRegistry.GetRunModelTypes();
            if (types.Length == 0)
                return;
#if STS2_AT_LEAST_0_113_0
            var models = types.Select(type =>
                (RunModel)ModelDb.GetById<AbstractModel>(ModelDb.GetId(type)).MutableClone());
            AccessTools.PropertySetter(typeof(RunState), nameof(RunState.RunModels))
                .Invoke(state, [state.RunModels.Concat(models).ToArray()]);
#else
            var models = types.Select(type =>
                (HookedSingletonModel)ModelDb.GetById<AbstractModel>(ModelDb.GetId(type)).MutableClone()).ToArray();
            foreach (var model in models)
                model.BindRun(state);
            Instances.Add(state, models);
            if (_subscribed)
                return;
            ModHelper.SubscribeForRunStateHooks("RitsuLib.HookedSingleton.Run", RunModels);
            ModHelper.SubscribeForCombatStateHooks("RitsuLib.HookedSingleton.Combat", CombatModels);
            _subscribed = true;
#endif
        }

#if !STS2_AT_LEAST_0_113_0
        private static IEnumerable<AbstractModel> RunModels(RunState state)
        {
            return Get(state).Where(model => model.SelectedHookType == HookedSingletonModel.HookType.Run);
        }

        private static IEnumerable<AbstractModel> CombatModels(CombatState state)
        {
            return state.RunState is RunState runState
                ? Get(runState).Where(model => model.SelectedHookType == HookedSingletonModel.HookType.Combat)
                : [];
        }

        internal static void Initialize(RunState state, bool loaded)
        {
            foreach (var model in Get(state))
                if (loaded)
                    model.AfterRunLoaded();
                else
                    model.AfterRunCreated();
        }

        internal static void BindCombat(CombatState state)
        {
            if (state.RunState is not RunState runState)
                return;
            foreach (var model in Get(runState))
                model.BindCombat(state);
        }
#endif

        private static IEnumerable<HookedSingletonModel> Get(RunState state)
        {
#if STS2_AT_LEAST_0_113_0
            return state.RunModels.OfType<HookedSingletonModel>();
#else
            return Instances.TryGetValue(state, out var models) ? models : [];
#endif
        }

        internal static void ClearCombat(RunState? state)
        {
            if (state == null)
                return;
            foreach (var model in Get(state))
                model.BindCombat(null);
        }

        internal static void Release(RunState? state)
        {
            if (state == null)
                return;
            foreach (var model in Get(state))
                model.ReleaseRun();
#if !STS2_AT_LEAST_0_113_0
            Instances.Remove(state);
#endif
        }
    }

    internal sealed class HookedSingletonCreatePatch : IPatchMethod
    {
        public static string PatchId => "hooked_singleton_create_per_run";
        public static string Description => "Create independent registered hook models for each run";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RunState), "CreateShared")];
        }

        public static void Postfix(RunState __result)
        {
            HookedSingletonRuntime.Create(__result);
        }
    }

    internal sealed class HookedSingletonCombatResetPatch : IPatchMethod
    {
        public static string PatchId => "hooked_singleton_combat_reset";
        public static string Description => "Release combat context when combat is reset";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CombatManager), nameof(CombatManager.Reset), [typeof(bool)])];
        }

        public static void Postfix()
        {
            HookedSingletonRuntime.ClearCombat(RunManager.Instance.State);
        }
    }

    internal sealed class HookedSingletonReleasePatch : IPatchMethod
    {
        public static string PatchId => "hooked_singleton_release_run";
        public static string Description => "Release hook model contexts when their run is cleaned up";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RunManager), nameof(RunManager.CleanUp), [typeof(bool)])];
        }

        public static void Prefix(RunManager __instance, out RunState? __state)
        {
            __state = __instance.State;
        }

        public static void Finalizer(RunState? __state)
        {
            HookedSingletonRuntime.Release(__state);
        }
    }

#if !STS2_AT_LEAST_0_113_0
    internal sealed class HookedSingletonCombatContextPatch : IPatchMethod
    {
        public static string PatchId => "hooked_singleton_combat_context";
        public static string Description => "Bind combat to per-run models on older hosts";
        public static bool IsCritical => true;
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CombatManager), nameof(CombatManager.SetUpCombat), [typeof(CombatState)])];
        }
        public static void Postfix(CombatState __0)
        {
            HookedSingletonRuntime.BindCombat(__0);
        }
    }

    internal sealed class HookedSingletonCombatEndPatch : IPatchMethod
    {
        public static string PatchId => "hooked_singleton_combat_end_compat";
        public static string Description => "Release finished combat context on older hosts";
        public static bool IsCritical => true;
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CombatManager), "EndCombatInternal", [])];
        }
        public static void Postfix(ref Task __result)
        {
            __result = Complete(__result, RunManager.Instance.State);
        }
        private static async Task Complete(Task task, RunState? state)
        {
            await task;
            HookedSingletonRuntime.ClearCombat(state);
        }
    }

    internal sealed class HookedSingletonInitializePatch : IPatchMethod
    {
        public static string PatchId => "hooked_singleton_initialize_run_compat";
        public static string Description => "Provide new-run and loaded-run model lifecycle on older hosts";
        public static bool IsCritical => true;
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RunManager), "InitializeNewRun"), new(typeof(RunManager), "InitializeSavedRun")];
        }
        public static void Postfix(RunManager __instance, System.Reflection.MethodBase __originalMethod)
        {
            HookedSingletonRuntime.Initialize(__instance.State!, __originalMethod.Name == "InitializeSavedRun");
        }
    }
#endif
}
