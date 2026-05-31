#if !STS2_AT_LEAST_0_104_0
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Models.Capabilities.Patches
{
    /// <summary>
    ///     Inserts opt-in model-backed capabilities into vanilla run/combat hook listener streams.
    ///     将 opt-in 的基于模型组件插入原版跑局/战斗 hook listener 流。
    /// </summary>
    internal static class ModelCapabilityHookListenerPatches
    {
        internal sealed class RunStateHookListenersPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_run_hook_listeners";

            public static string Description => "Insert model capabilities into run hook listener streams";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(RunState), nameof(RunState.IterateHookListeners), [typeof(CombatStateCompat)])];
            }

            // ReSharper disable InconsistentNaming
            public static void Postfix(ref IEnumerable<AbstractModel> __result)
                // ReSharper restore InconsistentNaming
            {
                __result = ModelCapabilityHookListeners.ExpandOwnerHookListeners(__result);
            }
        }

        internal sealed class CombatStateHookListenersPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_combat_hook_listeners";

            public static string Description => "Insert model capabilities into combat hook listener streams";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CombatState), nameof(CombatState.IterateHookListeners), Type.EmptyTypes)];
            }

            // ReSharper disable InconsistentNaming
            public static void Postfix(ref IEnumerable<AbstractModel> __result)
                // ReSharper restore InconsistentNaming
            {
                __result = ModelCapabilityHookListeners.ExpandOwnerHookListeners(__result);
            }
        }
    }
}
