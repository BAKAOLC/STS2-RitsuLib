#if !STS2_AT_LEAST_0_104_0
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Models.Capabilities.Patches
{
    /// <summary>
    ///     Inserts opt-in model-backed capabilities into vanilla run/combat hook listener streams.
    ///     将 opt-in 的基于模型能力插入原版跑局/战斗 hook listener 流。
    /// </summary>
    internal static class ModelCapabilityHookListenerPatches
    {
        internal sealed class RunStateHookListenersPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_run_hook_listeners";

            public static string Description => "Insert model capabilities into run hook listener streams";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(RunState), nameof(RunState.IterateHookListeners), [typeof(CombatStateCompat)])];
            }

            public static void Postfix(ref IEnumerable<AbstractModel> __result)
            {
                __result = ModelCapabilityHookListeners.ExpandOwnerHookListeners(__result);
            }
        }

        internal sealed class CombatStateHookListenersPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_combat_hook_listeners";

            public static string Description => "Insert model capabilities into combat hook listener streams";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CombatState), nameof(CombatState.IterateHookListeners), Type.EmptyTypes)];
            }

            public static void Postfix(ref IEnumerable<AbstractModel> __result)
            {
                __result = ModelCapabilityHookListeners.ExpandOwnerHookListeners(__result);
            }
        }

        internal sealed class HookPlayerChoiceContextConstructorPatch : IPatchMethod
        {
            private static readonly FieldInfo LocalPlayerIdField =
                AccessTools.Field(typeof(HookPlayerChoiceContext), "_localPlayerId")!;

            private static readonly FieldInfo TaskAssignedCompletionSourceField =
                AccessTools.Field(typeof(HookPlayerChoiceContext), "_taskAssignedCompletionSource")!;

            private static readonly FieldInfo? PausedBeforeTaskAssignedCompletionSourceField =
                AccessTools.Field(typeof(HookPlayerChoiceContext), "_pausedBeforeTaskAssignedCompletionSource");

            private static readonly FieldInfo PausedCompletionSourceField =
                AccessTools.Field(typeof(HookPlayerChoiceContext), "_pausedCompletionSource")!;

            private static readonly FieldInfo GameActionTypeField =
                AccessTools.Field(typeof(HookPlayerChoiceContext), "_gameActionType")!;

            private static readonly FieldInfo SourceField =
                AccessTools.Field(typeof(HookPlayerChoiceContext), "<Source>k__BackingField")!;

            private static readonly FieldInfo OwnerField =
                AccessTools.Field(typeof(HookPlayerChoiceContext), "<Owner>k__BackingField")!;

            public static string PatchId => "ritsulib_model_capability_hook_choice_context";

            public static string Description =>
                "Resolve model-capability owners when vanilla hook choice contexts are created";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(
                        typeof(HookPlayerChoiceContext),
                        ".ctor",
                        [typeof(AbstractModel), typeof(ulong), typeof(CombatStateCompat), typeof(GameActionType)],
                        MethodType.Constructor),
                ];
            }

            public static bool Prefix(
                HookPlayerChoiceContext __instance,
                AbstractModel source,
                ulong localPlayerId,
                CombatStateCompat combatState,
                GameActionType gameActionType)
            {
                LocalPlayerIdField.SetValue(__instance, localPlayerId);
                TaskAssignedCompletionSourceField.SetValue(__instance, new TaskCompletionSource());
                PausedBeforeTaskAssignedCompletionSourceField?.SetValue(__instance, new TaskCompletionSource());
                PausedCompletionSourceField.SetValue(__instance, new TaskCompletionSource());
                SourceField.SetValue(__instance, source);
                OwnerField.SetValue(__instance, ResolveOwner(source, combatState));
                __instance.PushModel(source);
                GameActionTypeField.SetValue(__instance, gameActionType);

                return false;
            }

            private static Player? ResolveOwner(AbstractModel source, CombatStateCompat combatState)
            {
                var contextSource = source is IModelCapability { Owner: { } capabilityOwner }
                    ? capabilityOwner
                    : source;

                return contextSource switch
                {
                    CardModel card => card.Owner,
                    RelicModel relic => relic.Owner,
                    PotionModel potion => potion.Owner,
                    AfflictionModel affliction => affliction.Card?.Owner,
                    EnchantmentModel enchantment => enchantment.Card?.Owner,
                    PowerModel { Owner: { } creature } => creature.IsPlayer
                        ? creature.Player
                        : combatState.Players.Count > 0
                            ? combatState.Players[0]
                            : null,
                    _ => null,
                };
            }
        }
    }
}
