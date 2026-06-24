using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     One structural IL validation issue.
    ///     一个结构性 IL 验证问题。
    /// </summary>
    public readonly record struct HarmonyIlValidationIssue(int Index, string Message);

    /// <summary>
    ///     Structural validation report for a Harmony instruction list.
    ///     Harmony 指令列表的结构性验证报告。
    /// </summary>
    public sealed class HarmonyIlValidationReport
    {
        internal HarmonyIlValidationReport(string operation, IReadOnlyList<HarmonyIlValidationIssue> issues)
        {
            Operation = operation;
            Issues = issues;
        }

        /// <summary>
        ///     Operation name supplied by the caller.
        ///     调用方提供的操作名称。
        /// </summary>
        public string Operation { get; }

        /// <summary>
        ///     Validation issues found in the instruction list.
        ///     在指令列表中发现的验证问题。
        /// </summary>
        public IReadOnlyList<HarmonyIlValidationIssue> Issues { get; }

        /// <summary>
        ///     True when no structural issues were found.
        ///     未发现结构性问题时为 true。
        /// </summary>
        public bool IsValid => Issues.Count == 0;

        /// <summary>
        ///     Throws when the report contains structural issues.
        ///     当报告包含结构性问题时抛出异常。
        /// </summary>
        public void ThrowIfInvalid()
        {
            if (IsValid)
                return;

            var preview = string.Join("; ", Issues.Take(5).Select(static issue => $"#{issue.Index}: {issue.Message}"));
            throw new InvalidOperationException($"{Operation} produced invalid IL: {preview}");
        }
    }

    /// <summary>
    ///     Structural checks for Harmony instruction lists produced by wrapper-based transpilers.
    ///     面向包装式 transpiler 输出的 Harmony 指令列表结构检查。
    /// </summary>
    public static class HarmonyIlValidation
    {
        /// <summary>
        ///     Validates branch labels and common reflection operands.
        ///     验证分支 label 与常见反射 operand。
        /// </summary>
        public static HarmonyIlValidationReport Validate(
            IReadOnlyList<CodeInstruction> code,
            string operation = "Harmony IL rewrite")
        {
            ArgumentNullException.ThrowIfNull(code);

            var issues = new List<HarmonyIlValidationIssue>();
            var definedLabels = new HashSet<Label>();
            for (var i = 0; i < code.Count; i++)
            {
                if (code[i] == null)
                {
                    issues.Add(new(i, "Instruction is null."));
                    continue;
                }

                foreach (var label in code[i].labels)
                    definedLabels.Add(label);
            }

            for (var i = 0; i < code.Count; i++)
            {
                var instruction = code[i];
                if (instruction == null)
                    continue;

                ValidateOperand(i, instruction, definedLabels, issues);
            }

            return new(operation, issues);
        }

        /// <summary>
        ///     Returns true when a matched span can be replaced without moving labels or exception blocks from the
        ///     interior of the span.
        ///     当匹配区间可在不移动区间内部 labels 或 exception blocks 的情况下安全替换时返回 true。
        /// </summary>
        public static bool CanReplaceSpan(
            IReadOnlyList<CodeInstruction> code,
            HarmonyIlMatch match,
            out string? reason)
        {
            ArgumentNullException.ThrowIfNull(code);

            if (match.Index < 0 || match.Length <= 0 || match.EndExclusive > code.Count)
            {
                reason = $"Invalid match span index={match.Index}, length={match.Length}, codeCount={code.Count}.";
                return false;
            }

            for (var i = match.Index + 1; i < match.EndExclusive; i++)
            {
                if (!HarmonyIl.HasMetadata(code[i]))
                    continue;

                reason =
                    $"Instruction #{i} inside the replacement span has labels or exception blocks. " +
                    "Use a narrower anchor, insert around the span, or rewrite the pattern to preserve control-flow metadata.";
                return false;
            }

            reason = null;
            return true;
        }

        private static void ValidateOperand(
            int index,
            CodeInstruction instruction,
            HashSet<Label> definedLabels,
            List<HarmonyIlValidationIssue> issues)
        {
            var opcode = instruction.opcode;
            if (opcode == OpCodes.Call && instruction.operand is not MethodBase)
            {
                issues.Add(new(index, "call operand must be MethodBase."));
                return;
            }

            if (opcode == OpCodes.Callvirt && instruction.operand is not MethodInfo)
            {
                issues.Add(new(index, "callvirt operand must be MethodInfo."));
                return;
            }

            if (opcode == OpCodes.Newobj && instruction.operand is not ConstructorInfo)
            {
                issues.Add(new(index, "newobj operand must be ConstructorInfo."));
                return;
            }

            if ((opcode == OpCodes.Ldfld || opcode == OpCodes.Ldflda || opcode == OpCodes.Ldsfld ||
                 opcode == OpCodes.Stfld || opcode == OpCodes.Stsfld) &&
                instruction.operand is not FieldInfo)
            {
                issues.Add(new(index, $"{opcode} operand must be FieldInfo."));
                return;
            }

            if (opcode == OpCodes.Ldstr && instruction.operand is not string)
            {
                issues.Add(new(index, "ldstr operand must be string."));
                return;
            }

            if (opcode == OpCodes.Ldc_I4 && instruction.operand is not int)
            {
                issues.Add(new(index, "ldc.i4 operand must be int."));
                return;
            }

            if (opcode == OpCodes.Ldc_I4_S && instruction.operand is not sbyte)
            {
                issues.Add(new(index, "ldc.i4.s operand must be sbyte."));
                return;
            }

            switch (opcode.OperandType)
            {
                case OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget:
                    ValidateBranchTarget(index, instruction.operand, definedLabels, issues);
                    return;
                case OperandType.InlineSwitch:
                    ValidateSwitchTargets(index, instruction.operand, definedLabels, issues);
                    break;
            }
        }

        private static void ValidateBranchTarget(
            int index,
            object? operand,
            HashSet<Label> definedLabels,
            List<HarmonyIlValidationIssue> issues)
        {
            if (operand is not Label label)
            {
                issues.Add(new(index, "Branch operand must be a Label."));
                return;
            }

            if (!definedLabels.Contains(label))
                issues.Add(new(index, "Branch target label is not attached to any instruction."));
        }

        private static void ValidateSwitchTargets(
            int index,
            object? operand,
            HashSet<Label> definedLabels,
            List<HarmonyIlValidationIssue> issues)
        {
            if (operand is not Label[] labels)
            {
                issues.Add(new(index, "switch operand must be a Label array."));
                return;
            }

            issues.AddRange(from label in labels
                where !definedLabels.Contains(label)
                select new HarmonyIlValidationIssue(index, "Switch target label is not attached to any instruction."));
        }
    }
}
