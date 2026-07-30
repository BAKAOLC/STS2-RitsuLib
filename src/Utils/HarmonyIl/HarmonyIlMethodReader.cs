using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     <para xml:lang="en">Decodes a method body into Harmony <see cref="CodeInstruction" /> values and resolves branch targets.</para>
    ///     <para xml:lang="zh-CN">将方法体解码为 Harmony <see cref="CodeInstruction" /> 值，并解析分支目标。</para>
    /// </summary>
    internal static class HarmonyIlMethodReader
    {
        private static readonly OpCode[] SingleByteOpCodes = new OpCode[0x100];
        private static readonly OpCode[] MultiByteOpCodes = new OpCode[0x100];

        static HarmonyIlMethodReader()
        {
            foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.GetValue(null) is not OpCode opCode)
                    continue;

                var value = unchecked((ushort)opCode.Value);
                if (value < 0x100)
                    SingleByteOpCodes[value] = opCode;
                else if ((value & 0xff00) == 0xfe00)
                    MultiByteOpCodes[value & 0xff] = opCode;
            }
        }

        public static IReadOnlyList<CodeInstruction> Read(MethodBase method)
        {
            var body = method.GetMethodBody() ??
                       throw new NotSupportedException("The method does not have an IL body.");
            var bytes = body.GetILAsByteArray() ??
                        throw new NotSupportedException("The method does not expose IL bytes.");
            var decoded = Decode(method, body, bytes);
            ResolveBranches(decoded);
            return [.. decoded.Select(static instruction => instruction.Instruction)];
        }

        private static List<DecodedInstruction> Decode(
            MethodBase method,
            MethodBody body,
            byte[] bytes)
        {
            var module = method.Module;
            var typeArguments = method.DeclaringType?.GetGenericArguments();
            var methodArguments = method is MethodInfo methodInfo
                ? methodInfo.GetGenericArguments()
                : Type.EmptyTypes;
            var decoded = new List<DecodedInstruction>();
            var position = 0;

            while (position < bytes.Length)
            {
                var offset = position;
                var opCode = ReadOpCode(bytes, ref position);
                var operand = ReadOperand(
                    bytes,
                    ref position,
                    opCode,
                    module,
                    body.LocalVariables,
                    typeArguments,
                    methodArguments);
                decoded.Add(new(offset, new(opCode, operand)));
            }

            return decoded;
        }

        private static OpCode ReadOpCode(byte[] bytes, ref int position)
        {
            var first = bytes[position++];
            var opCode = first == 0xfe
                ? MultiByteOpCodes[bytes[position++]]
                : SingleByteOpCodes[first];
            return opCode.Size == 0
                ? throw new BadImageFormatException($"Unknown IL opcode at offset {position - 1}.")
                : opCode;
        }

        private static object? ReadOperand(
            byte[] bytes,
            ref int position,
            OpCode opCode,
            Module module,
            IList<LocalVariableInfo> locals,
            Type[]? typeArguments,
            Type[] methodArguments)
        {
            switch (opCode.OperandType)
            {
                case OperandType.InlineNone:
                    return null;
                case OperandType.ShortInlineI:
                    return opCode == OpCodes.Ldc_I4_S
                        ? unchecked((sbyte)bytes[position++])
                        : bytes[position++];
                case OperandType.InlineI:
                    return ReadInt32(bytes, ref position);
                case OperandType.InlineI8:
                    var longValue = BitConverter.ToInt64(bytes, position);
                    position += sizeof(long);
                    return longValue;
                case OperandType.ShortInlineR:
                    var floatValue = BitConverter.ToSingle(bytes, position);
                    position += sizeof(float);
                    return floatValue;
                case OperandType.InlineR:
                    var doubleValue = BitConverter.ToDouble(bytes, position);
                    position += sizeof(double);
                    return doubleValue;
                case OperandType.InlineString:
                    return module.ResolveString(ReadInt32(bytes, ref position));
                case OperandType.InlineMethod:
                    return module.ResolveMethod(
                        ReadInt32(bytes, ref position),
                        typeArguments,
                        methodArguments);
                case OperandType.InlineField:
                    return module.ResolveField(
                        ReadInt32(bytes, ref position),
                        typeArguments,
                        methodArguments);
                case OperandType.InlineType:
                    return module.ResolveType(
                        ReadInt32(bytes, ref position),
                        typeArguments,
                        methodArguments);
                case OperandType.InlineTok:
                    return module.ResolveMember(
                        ReadInt32(bytes, ref position),
                        typeArguments,
                        methodArguments);
                case OperandType.InlineSig:
                    return module.ResolveSignature(ReadInt32(bytes, ref position));
                case OperandType.ShortInlineVar:
                    return ResolveVariable(bytes[position++], locals);
                case OperandType.InlineVar:
                    var variableIndex = BitConverter.ToUInt16(bytes, position);
                    position += sizeof(ushort);
                    return ResolveVariable(variableIndex, locals);
                case OperandType.ShortInlineBrTarget:
                    return new BranchTarget(
                        position + 1 + unchecked((sbyte)bytes[position++]));
                case OperandType.InlineBrTarget:
                    return new BranchTarget(position + sizeof(int) + ReadInt32(bytes, ref position));
                case OperandType.InlineSwitch:
                    var count = ReadInt32(bytes, ref position);
                    var baseOffset = position + count * sizeof(int);
                    var targets = new BranchTarget[count];
                    for (var i = 0; i < count; i++)
                        targets[i] = new(baseOffset + ReadInt32(bytes, ref position));
                    return targets;
                default:
                    throw new NotSupportedException(
                        $"Unsupported IL operand type {opCode.OperandType}.");
            }
        }

        private static void ResolveBranches(IReadOnlyList<DecodedInstruction> decoded)
        {
            if (decoded.Count == 0)
                return;

            var generator = new DynamicMethod(
                    "RitsuLibHarmonyIlLabels",
                    typeof(void),
                    Type.EmptyTypes)
                .GetILGenerator();
            var targetOffsets = decoded
                .SelectMany(static item => item.Instruction.operand switch
                {
                    BranchTarget target => [target.Offset],
                    BranchTarget[] targets => targets.Select(static target => target.Offset),
                    _ => [],
                })
                .Distinct()
                .ToArray();
            var labels = targetOffsets.ToDictionary(
                static offset => offset,
                _ => generator.DefineLabel());
            var byOffset = decoded.ToDictionary(static item => item.Offset);

            foreach (var (offset, label) in labels)
            {
                if (!byOffset.TryGetValue(offset, out var target))
                    throw new BadImageFormatException(
                        $"IL branch target offset {offset} does not identify an instruction.");
                target.Instruction.labels.Add(label);
            }

            foreach (var item in decoded)
                item.Instruction.operand = item.Instruction.operand switch
                {
                    BranchTarget target => labels[target.Offset],
                    BranchTarget[] targets => targets
                        .Select(target => labels[target.Offset])
                        .ToArray(),
                    var operand => operand,
                };
        }

        private static int ReadInt32(byte[] bytes, ref int position)
        {
            var value = BitConverter.ToInt32(bytes, position);
            position += sizeof(int);
            return value;
        }

        private static object ResolveVariable(
            int index,
            IList<LocalVariableInfo> locals)
        {
            return index < locals.Count ? locals[index] : index;
        }

        private sealed record DecodedInstruction(
            int Offset,
            CodeInstruction Instruction);

        private readonly record struct BranchTarget(int Offset);
    }
}
