using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.Combat.AttackHits.Patches
{
    /// <summary>
    ///     Redirects the per-hit damage call inside <see cref="AttackCommand.Execute" /> through
    ///     <see cref="AttackHitHook" />.
    ///     将 <see cref="AttackCommand.Execute" /> 内每段的伤害调用重定向到 <see cref="AttackHitHook" />。
    /// </summary>
    internal sealed class AttackCommandExecuteHitHookPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_attack_command_execute_hit_hooks";

        public static string Description => "Dispatch RitsuLib before/after hooks for every AttackCommand hit";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [PatchTarget.AsyncMethod<AttackCommand>(nameof(AttackCommand.Execute), typeof(PlayerChoiceContext))];
        }

        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            const string operation = "[AttackHitHook] Redirect per-hit CreatureCmd.Damage call";
            var stateMachineType = __originalMethod.DeclaringType;
            var damageMethod = AccessTools.Method(
                typeof(CreatureCmd),
                nameof(CreatureCmd.Damage),
                [
                    typeof(PlayerChoiceContext),
                    typeof(IEnumerable<Creature>),
                    typeof(decimal),
                    typeof(ValueProp),
                    typeof(Creature),
                    typeof(CardModel),
                ]);
            var wrapperMethod = AccessTools.Method(
                typeof(AttackHitHook),
                nameof(AttackHitHook.DamageWithAttackHitHooks),
                [
                    typeof(PlayerChoiceContext),
                    typeof(IEnumerable<Creature>),
                    typeof(decimal),
                    typeof(ValueProp),
                    typeof(Creature),
                    typeof(CardModel),
                    typeof(AttackCommand),
                    typeof(int),
                    typeof(decimal),
                ]);

            var rewriter = HarmonyIlRewriter.From(instructions);
            if (damageMethod == null)
            {
                RitsuLibFramework.Logger.Warn($"{operation}: Could not resolve CreatureCmd.Damage target method.");
                return rewriter.Instructions();
            }

            if (wrapperMethod == null)
            {
                RitsuLibFramework.Logger.Warn($"{operation}: Could not resolve AttackHitHook wrapper method.");
                return rewriter.Instructions();
            }

            if (stateMachineType == null)
            {
                RitsuLibFramework.Logger.Warn($"{operation}: Missing async state machine type.");
                return rewriter.Instructions();
            }

            if (!TryResolveStateFields(stateMachineType, out var attackField, out var hitIndexField,
                    out var totalHitCountField))
            {
                RitsuLibFramework.Logger.Warn(
                    $"{operation}: Could not resolve AttackCommand.Execute state fields on {stateMachineType.FullName}; per-hit hooks skipped.");
                return rewriter.Instructions();
            }

            var report = HarmonyAsyncIl.ReplaceAwaitedCalls(
                rewriter,
                operation,
                damageMethod,
                _ =>
                [
                    HarmonyIl.Ldarg(0),
                    HarmonyIl.Ldfld(attackField),
                    HarmonyIl.Ldarg(0),
                    HarmonyIl.Ldfld(hitIndexField),
                    HarmonyIl.Ldarg(0),
                    HarmonyIl.Ldfld(totalHitCountField),
                    HarmonyIl.Call(wrapperMethod),
                ],
                code => code.Any(instruction => HarmonyIl.IsCallTo(instruction, wrapperMethod)));
            if (!report.Succeeded || report.Applied != 1)
                RitsuLibFramework.Logger.Warn(report.Describe());

            return rewriter.InstructionsChecked(operation);
        }

        private static bool TryResolveStateFields(
            Type stateMachineType,
            out FieldInfo attackField,
            out FieldInfo hitIndexField,
            out FieldInfo totalHitCountField)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fields = stateMachineType.GetFields(flags);
            attackField = fields.FirstOrDefault(static field => field.FieldType == typeof(AttackCommand))!;
            hitIndexField = fields.FirstOrDefault(static field =>
                field.FieldType == typeof(int) && field.Name.Contains("<i>", StringComparison.Ordinal))!;
            totalHitCountField = fields.FirstOrDefault(static field =>
                field.FieldType == typeof(decimal) &&
                field.Name.Contains("<attackCount>", StringComparison.Ordinal))!;

            return attackField != null && hitIndexField != null && totalHitCountField != null;
        }
    }
}
