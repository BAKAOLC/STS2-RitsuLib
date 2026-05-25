using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     Small instruction factories and predicates for RitsuLib Harmony transpilers.
    ///     RitsuLib Harmony transpiler 使用的小型指令工厂与谓词。
    /// </summary>
    public static class HarmonyIl
    {
        /// <summary>
        ///     Creates a local-load instruction using the short opcode where possible.
        ///     创建本地变量加载指令；可用时使用短 opcode。
        /// </summary>
        public static CodeInstruction Ldloc(int index)
        {
            return index switch
            {
                0 => new(OpCodes.Ldloc_0),
                1 => new(OpCodes.Ldloc_1),
                2 => new(OpCodes.Ldloc_2),
                3 => new(OpCodes.Ldloc_3),
                >= 0 and <= byte.MaxValue => new(OpCodes.Ldloc_S, (byte)index),
                _ => new(OpCodes.Ldloc, index),
            };
        }

        /// <summary>
        ///     Creates a local-store instruction using the short opcode where possible.
        ///     创建本地变量存储指令；可用时使用短 opcode。
        /// </summary>
        public static CodeInstruction Stloc(int index)
        {
            return index switch
            {
                0 => new(OpCodes.Stloc_0),
                1 => new(OpCodes.Stloc_1),
                2 => new(OpCodes.Stloc_2),
                3 => new(OpCodes.Stloc_3),
                >= 0 and <= byte.MaxValue => new(OpCodes.Stloc_S, (byte)index),
                _ => new(OpCodes.Stloc, index),
            };
        }

        /// <summary>
        ///     Creates an argument-load instruction using the short opcode where possible.
        ///     创建参数加载指令；可用时使用短 opcode。
        /// </summary>
        public static CodeInstruction Ldarg(int index)
        {
            return index switch
            {
                0 => new(OpCodes.Ldarg_0),
                1 => new(OpCodes.Ldarg_1),
                2 => new(OpCodes.Ldarg_2),
                3 => new(OpCodes.Ldarg_3),
                >= 0 and <= byte.MaxValue => new(OpCodes.Ldarg_S, (byte)index),
                _ => new(OpCodes.Ldarg, index),
            };
        }

        /// <summary>
        ///     Creates an int32 constant-load instruction using the shortest opcode.
        ///     创建 32 位整数常量加载指令；使用最短 opcode。
        /// </summary>
        public static CodeInstruction LdcI4(int value)
        {
            return value switch
            {
                -1 => new(OpCodes.Ldc_I4_M1),
                0 => new(OpCodes.Ldc_I4_0),
                1 => new(OpCodes.Ldc_I4_1),
                2 => new(OpCodes.Ldc_I4_2),
                3 => new(OpCodes.Ldc_I4_3),
                4 => new(OpCodes.Ldc_I4_4),
                5 => new(OpCodes.Ldc_I4_5),
                6 => new(OpCodes.Ldc_I4_6),
                7 => new(OpCodes.Ldc_I4_7),
                8 => new(OpCodes.Ldc_I4_8),
                >= sbyte.MinValue and <= sbyte.MaxValue => new(OpCodes.Ldc_I4_S, (sbyte)value),
                _ => new(OpCodes.Ldc_I4, value),
            };
        }

        /// <summary>
        ///     Creates a string-load instruction.
        ///     创建字符串加载指令。
        /// </summary>
        public static CodeInstruction Ldstr(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new(OpCodes.Ldstr, value);
        }

        /// <summary>
        ///     Creates a field-load instruction.
        ///     创建字段加载指令。
        /// </summary>
        public static CodeInstruction Ldfld(FieldInfo field)
        {
            ArgumentNullException.ThrowIfNull(field);
            return new(OpCodes.Ldfld, field);
        }

        /// <summary>
        ///     Creates a field-address load instruction.
        ///     创建字段地址加载指令。
        /// </summary>
        public static CodeInstruction Ldflda(FieldInfo field)
        {
            ArgumentNullException.ThrowIfNull(field);
            return new(OpCodes.Ldflda, field);
        }

        /// <summary>
        ///     Creates a static-field-load instruction.
        ///     创建静态字段加载指令。
        /// </summary>
        public static CodeInstruction Ldsfld(FieldInfo field)
        {
            ArgumentNullException.ThrowIfNull(field);
            return new(OpCodes.Ldsfld, field);
        }

        /// <summary>
        ///     Creates a field-store instruction.
        ///     创建字段存储指令。
        /// </summary>
        public static CodeInstruction Stfld(FieldInfo field)
        {
            ArgumentNullException.ThrowIfNull(field);
            return new(OpCodes.Stfld, field);
        }

        /// <summary>
        ///     Creates a static-field-store instruction.
        ///     创建静态字段存储指令。
        /// </summary>
        public static CodeInstruction Stsfld(FieldInfo field)
        {
            ArgumentNullException.ThrowIfNull(field);
            return new(OpCodes.Stsfld, field);
        }

        /// <summary>
        ///     Creates a call instruction.
        ///     创建 call 指令。
        /// </summary>
        public static CodeInstruction Call(MethodInfo method)
        {
            ArgumentNullException.ThrowIfNull(method);
            return new(OpCodes.Call, method);
        }

        /// <summary>
        ///     Creates a callvirt instruction.
        ///     创建 callvirt 指令。
        /// </summary>
        public static CodeInstruction Callvirt(MethodInfo method)
        {
            ArgumentNullException.ThrowIfNull(method);
            return new(OpCodes.Callvirt, method);
        }

        /// <summary>
        ///     Creates an object-construction instruction.
        ///     创建对象构造指令。
        /// </summary>
        public static CodeInstruction Newobj(ConstructorInfo constructor)
        {
            ArgumentNullException.ThrowIfNull(constructor);
            return new(OpCodes.Newobj, constructor);
        }

        /// <summary>
        ///     Creates a null-load instruction.
        ///     创建 null 加载指令。
        /// </summary>
        public static CodeInstruction Ldnull()
        {
            return new(OpCodes.Ldnull);
        }

        /// <summary>
        ///     Creates a duplicate-stack-value instruction.
        ///     创建复制栈顶值指令。
        /// </summary>
        public static CodeInstruction Dup()
        {
            return new(OpCodes.Dup);
        }

        /// <summary>
        ///     Creates a pop instruction.
        ///     创建 pop 指令。
        /// </summary>
        public static CodeInstruction Pop()
        {
            return new(OpCodes.Pop);
        }

        /// <summary>
        ///     Creates a ret instruction.
        ///     创建 ret 指令。
        /// </summary>
        public static CodeInstruction Ret()
        {
            return new(OpCodes.Ret);
        }

        /// <summary>
        ///     Converts a local-store instruction to the corresponding local-load instruction.
        ///     将本地变量存储指令转换为对应的本地变量读取指令。
        /// </summary>
        public static CodeInstruction LoadLocalFromStore(CodeInstruction store)
        {
            ArgumentNullException.ThrowIfNull(store);

            if (store.opcode == OpCodes.Stloc_0) return new(OpCodes.Ldloc_0);
            if (store.opcode == OpCodes.Stloc_1) return new(OpCodes.Ldloc_1);
            if (store.opcode == OpCodes.Stloc_2) return new(OpCodes.Ldloc_2);
            if (store.opcode == OpCodes.Stloc_3) return new(OpCodes.Ldloc_3);
            if (store.opcode == OpCodes.Stloc_S) return new(OpCodes.Ldloc_S, store.operand);
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (store.opcode == OpCodes.Stloc) return new(OpCodes.Ldloc, store.operand);

            throw new ArgumentException($"Instruction '{store}' is not a stloc instruction.", nameof(store));
        }

        /// <summary>
        ///     Matches any instruction.
        ///     匹配任意指令。
        /// </summary>
        public static Func<CodeInstruction, bool> Any()
        {
            return static _ => true;
        }

        /// <summary>
        ///     Negates another instruction predicate.
        ///     对另一个指令谓词取反。
        /// </summary>
        public static Func<CodeInstruction, bool> Not(Func<CodeInstruction, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return instruction => !predicate(instruction);
        }

        /// <summary>
        ///     Matches when any supplied instruction predicate matches.
        ///     任一给定指令谓词匹配时即匹配。
        /// </summary>
        public static Func<CodeInstruction, bool> OneOf(params Func<CodeInstruction, bool>[] predicates)
        {
            ArgumentNullException.ThrowIfNull(predicates);
            return instruction => predicates.Any(predicate => predicate(instruction));
        }

        /// <summary>
        ///     Matches an opcode and optional operand.
        ///     匹配 opcode 和可选 operand。
        /// </summary>
        public static Func<CodeInstruction, bool> Is(OpCode opcode, object? operand = null)
        {
            return instruction => instruction.opcode == opcode &&
                                  (operand == null || Equals(instruction.operand, operand));
        }

        /// <summary>
        ///     Matches an opcode and exact operand, including a null operand.
        ///     匹配 opcode 和精确 operand，包括 null operand。
        /// </summary>
        public static Func<CodeInstruction, bool> IsExact(OpCode opcode, object? operand)
        {
            return instruction => instruction.opcode == opcode && Equals(instruction.operand, operand);
        }

        /// <summary>
        ///     Matches an argument-load instruction.
        ///     匹配参数读取指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdarg(int? index = null)
        {
            return instruction => TryGetArgumentIndex(instruction, out var actual) &&
                                  (index == null || actual == index.Value);
        }

        /// <summary>
        ///     Matches a local-load instruction.
        ///     匹配本地变量读取指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdloc(int? index = null)
        {
            return instruction => TryGetLocalLoadIndex(instruction, out var actual) &&
                                  (index == null || actual == index.Value);
        }

        /// <summary>
        ///     Matches a local-store instruction.
        ///     匹配本地变量存储指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsStloc(int? index = null)
        {
            return instruction => TryGetLocalStoreIndex(instruction, out var actual) &&
                                  (index == null || actual == index.Value);
        }

        /// <summary>
        ///     Matches a string-load instruction.
        ///     匹配字符串加载指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdstr(string? value = null)
        {
            return instruction => instruction.opcode == OpCodes.Ldstr &&
                                  (value == null || (instruction.operand is string s &&
                                                     string.Equals(s, value, StringComparison.Ordinal)));
        }

        /// <summary>
        ///     Matches an int32 constant-load instruction.
        ///     匹配 32 位整数常量加载指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdcI4(int? value = null)
        {
            return instruction => TryGetInt32(instruction, out var actual) &&
                                  (value == null || actual == value.Value);
        }

        /// <summary>
        ///     Matches a call/callvirt to the given method.
        ///     匹配对指定方法的 call/callvirt。
        /// </summary>
        public static Func<CodeInstruction, bool> IsCall(MethodInfo? method)
        {
            return instruction => IsCallTo(instruction, method);
        }

        /// <summary>
        ///     Matches any call/callvirt instruction.
        ///     匹配任意 call/callvirt 指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsAnyCall()
        {
            return static instruction => instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt;
        }

        /// <summary>
        ///     Matches a newobj instruction for the supplied constructor.
        ///     匹配指定构造函数的 newobj 指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsNewobj(ConstructorInfo? constructor = null)
        {
            return instruction => instruction.opcode == OpCodes.Newobj &&
                                  (constructor == null || Equals(instruction.operand, constructor));
        }

        /// <summary>
        ///     Matches a field instruction.
        ///     匹配字段指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsField(OpCode opcode, FieldInfo? field = null)
        {
            return instruction => instruction.opcode == opcode &&
                                  (field == null || Equals(instruction.operand, field));
        }

        /// <summary>
        ///     Matches an instance-field load instruction.
        ///     匹配实例字段读取指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdfld(FieldInfo? field = null)
        {
            return IsField(OpCodes.Ldfld, field);
        }

        /// <summary>
        ///     Matches an instance-field store instruction.
        ///     匹配实例字段存储指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsStfld(FieldInfo? field = null)
        {
            return IsField(OpCodes.Stfld, field);
        }

        /// <summary>
        ///     Matches a static-field load instruction.
        ///     匹配静态字段读取指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdsfld(FieldInfo? field = null)
        {
            return IsField(OpCodes.Ldsfld, field);
        }

        /// <summary>
        ///     Matches a branch instruction.
        ///     匹配分支指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsBranch()
        {
            return static instruction => instruction.Branches(out _);
        }

        /// <summary>
        ///     Matches a ret instruction.
        ///     匹配 ret 指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsRet()
        {
            return static instruction => instruction.opcode == OpCodes.Ret;
        }

        /// <summary>
        ///     Returns true when the instruction calls the supplied method via call or callvirt.
        ///     当指令通过 call 或 callvirt 调用指定方法时返回 true。
        /// </summary>
        public static bool IsCallTo(CodeInstruction instruction, MethodInfo? method)
        {
            return method != null
                   && (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt)
                   && instruction.operand is MethodInfo called
                   && called == method;
        }

        /// <summary>
        ///     Returns true when the instruction calls the supplied generic method definition.
        ///     当指令调用指定泛型方法定义时返回 true。
        /// </summary>
        public static bool IsCallToGenericDefinition(CodeInstruction instruction, MethodInfo? genericDefinition)
        {
            return genericDefinition != null
                   && genericDefinition.IsGenericMethodDefinition
                   && (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt)
                   && instruction.operand is MethodInfo { IsGenericMethod: true } called
                   && called.GetGenericMethodDefinition() == genericDefinition;
        }

        /// <summary>
        ///     Returns true when the instruction calls a method declared on <paramref name="declaringType" /> with
        ///     <paramref name="methodName" />.
        ///     当指令调用 <paramref name="declaringType" /> 上名为 <paramref name="methodName" /> 的方法时返回 true。
        /// </summary>
        public static bool IsCallNamed(CodeInstruction instruction, Type declaringType, string methodName)
        {
            ArgumentNullException.ThrowIfNull(declaringType);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            return (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt)
                   && instruction.operand is MethodInfo called
                   && called.DeclaringType == declaringType
                   && string.Equals(called.Name, methodName, StringComparison.Ordinal);
        }

        /// <summary>
        ///     Returns true when the instruction loads the supplied 32-bit integer constant.
        ///     当指令加载指定 32 位整数常量时返回 true。
        /// </summary>
        public static bool LoadsInt32(CodeInstruction instruction, int value)
        {
            return TryGetInt32(instruction, out var actual) && actual == value;
        }

        /// <summary>
        ///     Reads the argument index from an argument-load instruction.
        ///     从参数读取指令中读取参数索引。
        /// </summary>
        public static bool TryGetArgumentIndex(CodeInstruction instruction, out int index)
        {
            if (instruction.opcode == OpCodes.Ldarg_0)
            {
                index = 0;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldarg_1)
            {
                index = 1;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldarg_2)
            {
                index = 2;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldarg_3)
            {
                index = 3;
                return true;
            }

            // ReSharper disable once InvertIf
            if (instruction.opcode != OpCodes.Ldarg && instruction.opcode != OpCodes.Ldarg_S)
            {
                index = -1;
                return false;
            }

            return TryGetNumericIndex(instruction.operand, out index);
        }

        /// <summary>
        ///     Reads the local index from a local-load instruction.
        ///     从本地变量读取指令中读取本地变量索引。
        /// </summary>
        public static bool TryGetLocalLoadIndex(CodeInstruction instruction, out int index)
        {
            if (instruction.opcode == OpCodes.Ldloc_0)
            {
                index = 0;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldloc_1)
            {
                index = 1;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldloc_2)
            {
                index = 2;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldloc_3)
            {
                index = 3;
                return true;
            }

            // ReSharper disable once InvertIf
            if (instruction.opcode != OpCodes.Ldloc && instruction.opcode != OpCodes.Ldloc_S)
            {
                index = -1;
                return false;
            }

            return TryGetNumericIndex(instruction.operand, out index);
        }

        /// <summary>
        ///     Reads the local index from a local-store instruction.
        ///     从本地变量存储指令中读取本地变量索引。
        /// </summary>
        public static bool TryGetLocalStoreIndex(CodeInstruction instruction, out int index)
        {
            if (instruction.opcode == OpCodes.Stloc_0)
            {
                index = 0;
                return true;
            }

            if (instruction.opcode == OpCodes.Stloc_1)
            {
                index = 1;
                return true;
            }

            if (instruction.opcode == OpCodes.Stloc_2)
            {
                index = 2;
                return true;
            }

            if (instruction.opcode == OpCodes.Stloc_3)
            {
                index = 3;
                return true;
            }

            // ReSharper disable once InvertIf
            if (instruction.opcode != OpCodes.Stloc && instruction.opcode != OpCodes.Stloc_S)
            {
                index = -1;
                return false;
            }

            return TryGetNumericIndex(instruction.operand, out index);
        }

        /// <summary>
        ///     Reads the int32 constant from an integer-load instruction.
        ///     从整数加载指令中读取 32 位整数常量。
        /// </summary>
        public static bool TryGetInt32(CodeInstruction instruction, out int value)
        {
            if (instruction.opcode == OpCodes.Ldc_I4 && instruction.operand is int full)
            {
                value = full;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldc_I4_S && instruction.operand is sbyte small)
            {
                value = small;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldc_I4_M1)
            {
                value = -1;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldc_I4_0)
            {
                value = 0;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldc_I4_1)
            {
                value = 1;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldc_I4_2)
            {
                value = 2;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldc_I4_3)
            {
                value = 3;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldc_I4_4)
            {
                value = 4;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldc_I4_5)
            {
                value = 5;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldc_I4_6)
            {
                value = 6;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldc_I4_7)
            {
                value = 7;
                return true;
            }

            if (instruction.opcode == OpCodes.Ldc_I4_8)
            {
                value = 8;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        ///     Returns true when the instruction has Harmony labels or exception blocks.
        ///     当指令带有 Harmony labels 或 exception blocks 时返回 true。
        /// </summary>
        public static bool HasMetadata(CodeInstruction instruction)
        {
            ArgumentNullException.ThrowIfNull(instruction);
            return instruction.labels.Count > 0 || instruction.blocks.Count > 0;
        }

        /// <summary>
        ///     Moves labels and exception blocks from <paramref name="source" /> to the first replacement instruction.
        ///     将 <paramref name="source" /> 的 labels 和 exception blocks 转移到第一条替换指令上。
        /// </summary>
        public static IReadOnlyList<CodeInstruction> MoveMetadataToFirst(
            CodeInstruction source,
            IReadOnlyList<CodeInstruction> replacement)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(replacement);

            if (replacement.Count == 0)
                return replacement;

            var first = replacement[0];
            first.labels.AddRange(source.labels);
            first.blocks.AddRange(source.blocks);
            source.labels.Clear();
            source.blocks.Clear();
            return replacement;
        }

        /// <summary>
        ///     Clones every instruction in order.
        ///     按顺序克隆每条指令。
        /// </summary>
        public static CodeInstruction[] CloneAll(IEnumerable<CodeInstruction> instructions)
        {
            ArgumentNullException.ThrowIfNull(instructions);
            return instructions.Select(static instruction => instruction.Clone()).ToArray();
        }

        private static bool TryGetNumericIndex(object? operand, out int index)
        {
            switch (operand)
            {
                case int i:
                    index = i;
                    return true;
                case byte b:
                    index = b;
                    return true;
                case sbyte sb:
                    index = sb;
                    return true;
                case short s:
                    index = s;
                    return true;
                case ushort us:
                    index = us;
                    return true;
                case LocalBuilder local:
                    index = local.LocalIndex;
                    return true;
                case ParameterInfo parameter:
                    index = parameter.Position;
                    return true;
                default:
                    index = -1;
                    return false;
            }
        }
    }
}
