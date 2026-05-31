using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     A local variable reference decoded from Harmony IL.
    ///     从 Harmony IL 解码出的本地变量引用。
    /// </summary>
    public readonly record struct HarmonyIlLocalRef(int Index, LocalBuilder? Builder = null, Type? LocalType = null)
    {
        /// <summary>
        ///     True when the local variable type is known.
        ///     已知本地变量类型时为 true。
        /// </summary>
        public bool HasKnownType => LocalType != null;

        /// <summary>
        ///     Creates a load instruction for this local.
        ///     为此本地变量创建读取指令。
        /// </summary>
        public CodeInstruction Load()
        {
            return HarmonyIl.LoadLocal(this);
        }

        /// <summary>
        ///     Creates a store instruction for this local.
        ///     为此本地变量创建存储指令。
        /// </summary>
        public CodeInstruction Store()
        {
            return HarmonyIl.StoreLocal(this);
        }

        /// <summary>
        ///     Returns true when both references point at the same local index.
        ///     当两个引用指向同一本地变量索引时返回 true。
        /// </summary>
        public bool IsSameLocal(HarmonyIlLocalRef other)
        {
            return Index == other.Index;
        }
    }

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
        ///     Converts a local-load instruction to the corresponding local-store instruction.
        ///     将本地变量读取指令转换为对应的本地变量存储指令。
        /// </summary>
        public static CodeInstruction StoreLocalFromLoad(CodeInstruction load)
        {
            ArgumentNullException.ThrowIfNull(load);

            if (load.opcode == OpCodes.Ldloc_0) return new(OpCodes.Stloc_0);
            if (load.opcode == OpCodes.Ldloc_1) return new(OpCodes.Stloc_1);
            if (load.opcode == OpCodes.Ldloc_2) return new(OpCodes.Stloc_2);
            if (load.opcode == OpCodes.Ldloc_3) return new(OpCodes.Stloc_3);
            if (load.opcode == OpCodes.Ldloc_S) return new(OpCodes.Stloc_S, load.operand);
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (load.opcode == OpCodes.Ldloc) return new(OpCodes.Stloc, load.operand);

            throw new ArgumentException($"Instruction '{load}' is not a ldloc instruction.", nameof(load));
        }

        /// <summary>
        ///     Creates a local-load instruction from a decoded local reference.
        ///     根据已解码的本地变量引用创建读取指令。
        /// </summary>
        public static CodeInstruction LoadLocal(HarmonyIlLocalRef local)
        {
            return local.Builder != null ? new(OpCodes.Ldloc_S, local.Builder) : Ldloc(local.Index);
        }

        /// <summary>
        ///     Creates a local-store instruction from a decoded local reference.
        ///     根据已解码的本地变量引用创建存储指令。
        /// </summary>
        public static CodeInstruction StoreLocal(HarmonyIlLocalRef local)
        {
            return local.Builder != null ? new(OpCodes.Stloc_S, local.Builder) : Stloc(local.Index);
        }

        /// <summary>
        ///     Returns true when both instructions reference the same local variable.
        ///     当两条指令引用同一本地变量时返回 true。
        /// </summary>
        public static bool SameLocal(CodeInstruction left, CodeInstruction right)
        {
            return TryGetLocal(left, out var leftLocal) &&
                   TryGetLocal(right, out var rightLocal) &&
                   leftLocal.IsSameLocal(rightLocal);
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
        ///     Matches a local-load instruction for the supplied local reference.
        ///     匹配指定本地变量引用的读取指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdloc(HarmonyIlLocalRef local)
        {
            return instruction => TryGetLocalLoad(instruction, out var actual) && actual.IsSameLocal(local);
        }

        /// <summary>
        ///     Matches a local-store instruction for the supplied local reference.
        ///     匹配指定本地变量引用的存储指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsStloc(HarmonyIlLocalRef local)
        {
            return instruction => TryGetLocalStore(instruction, out var actual) && actual.IsSameLocal(local);
        }

        /// <summary>
        ///     Matches a local-load instruction whose operand exposes the supplied local type.
        ///     匹配 operand 暴露出指定本地变量类型的读取指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdlocOfType(Type localType)
        {
            ArgumentNullException.ThrowIfNull(localType);
            return instruction => TryGetLocalLoad(instruction, out var local) && local.LocalType == localType;
        }

        /// <summary>
        ///     Matches a local-load instruction whose operand exposes the supplied local type.
        ///     匹配 operand 暴露出指定本地变量类型的读取指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdlocOfType<T>()
        {
            return IsLdlocOfType(typeof(T));
        }

        /// <summary>
        ///     Matches a local-store instruction whose operand exposes the supplied local type.
        ///     匹配 operand 暴露出指定本地变量类型的存储指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsStlocOfType(Type localType)
        {
            ArgumentNullException.ThrowIfNull(localType);
            return instruction => TryGetLocalStore(instruction, out var local) && local.LocalType == localType;
        }

        /// <summary>
        ///     Matches a local-store instruction whose operand exposes the supplied local type.
        ///     匹配 operand 暴露出指定本地变量类型的存储指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsStlocOfType<T>()
        {
            return IsStlocOfType(typeof(T));
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
        ///     Matches a call/callvirt instruction using a method predicate.
        ///     使用方法谓词匹配 call/callvirt 指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsCall(Func<MethodInfo, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return instruction => IsAnyCallInstruction(instruction) &&
                                  instruction.operand is MethodInfo method &&
                                  predicate(method);
        }

        /// <summary>
        ///     Matches a call/callvirt to a method declared on the supplied type with the supplied name.
        ///     匹配对指定类型上指定名称方法的 call/callvirt。
        /// </summary>
        public static Func<CodeInstruction, bool> IsCallTo(Type declaringType, string methodName)
        {
            ArgumentNullException.ThrowIfNull(declaringType);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
            return IsCall(method => method.DeclaringType == declaringType &&
                                    string.Equals(method.Name, methodName, StringComparison.Ordinal));
        }

        /// <summary>
        ///     Matches a call/callvirt to a method with the supplied name.
        ///     匹配对指定名称方法的 call/callvirt。
        /// </summary>
        public static Func<CodeInstruction, bool> IsCallTo(string methodName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
            return IsCall(method => string.Equals(method.Name, methodName, StringComparison.Ordinal));
        }

        /// <summary>
        ///     Matches a call/callvirt whose return type is the supplied type.
        ///     匹配返回类型为指定类型的 call/callvirt。
        /// </summary>
        public static Func<CodeInstruction, bool> IsCallReturning(Type returnType)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return IsCall(method => method.ReturnType == returnType);
        }

        /// <summary>
        ///     Matches a call/callvirt whose parameter types match the supplied sequence.
        ///     匹配参数类型序列等于指定序列的 call/callvirt。
        /// </summary>
        public static Func<CodeInstruction, bool> IsCallWithParameters(params Type[] parameterTypes)
        {
            ArgumentNullException.ThrowIfNull(parameterTypes);
            return IsCall(method => method.GetParameters().Select(static parameter => parameter.ParameterType)
                .SequenceEqual(parameterTypes));
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
        ///     Matches any field access instruction for the supplied field.
        ///     匹配对指定字段的任意字段访问指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsFieldAccess(FieldInfo? field = null)
        {
            return instruction => IsFieldAccessInstruction(instruction) &&
                                  (field == null || Equals(instruction.operand, field));
        }

        /// <summary>
        ///     Matches any field access instruction using a field predicate.
        ///     使用字段谓词匹配任意字段访问指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsFieldAccess(Func<FieldInfo, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return instruction => IsFieldAccessInstruction(instruction) &&
                                  instruction.operand is FieldInfo field &&
                                  predicate(field);
        }

        /// <summary>
        ///     Matches any field access instruction whose field type is the supplied type.
        ///     匹配字段类型为指定类型的任意字段访问指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsFieldOfType(Type fieldType)
        {
            ArgumentNullException.ThrowIfNull(fieldType);
            return IsFieldAccess(field => field.FieldType == fieldType);
        }

        /// <summary>
        ///     Matches any field access instruction for a named field on the supplied declaring type.
        ///     匹配指定类型上指定名称字段的任意字段访问指令。
        /// </summary>
        public static Func<CodeInstruction, bool> IsFieldNamed(Type declaringType, string fieldName)
        {
            ArgumentNullException.ThrowIfNull(declaringType);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
            return IsFieldAccess(field => field.DeclaringType == declaringType &&
                                          string.Equals(field.Name, fieldName, StringComparison.Ordinal));
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
        ///     Reads a typed operand from an instruction.
        ///     从指令读取指定类型的 operand。
        /// </summary>
        public static bool TryGetOperand<T>(CodeInstruction instruction, out T operand)
        {
            ArgumentNullException.ThrowIfNull(instruction);
            if (instruction.operand is T typed)
            {
                operand = typed;
                return true;
            }

            operand = default!;
            return false;
        }

        /// <summary>
        ///     Returns true when the instruction operand equals the supplied operand.
        ///     当指令 operand 等于指定 operand 时返回 true。
        /// </summary>
        public static bool OperandEquals(CodeInstruction instruction, object? operand)
        {
            ArgumentNullException.ThrowIfNull(instruction);
            return Equals(instruction.operand, operand);
        }

        /// <summary>
        ///     Returns true when a typed operand satisfies the supplied predicate.
        ///     当指定类型的 operand 满足谓词时返回 true。
        /// </summary>
        public static bool OperandMatches<T>(CodeInstruction instruction, Func<T, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return TryGetOperand<T>(instruction, out var operand) && predicate(operand);
        }

        /// <summary>
        ///     Matches instructions whose typed operand satisfies the supplied predicate.
        ///     匹配指定类型 operand 满足谓词的指令。
        /// </summary>
        public static Func<CodeInstruction, bool> HasOperand<T>(Func<T, bool>? predicate = null)
        {
            return instruction => TryGetOperand<T>(instruction, out var operand) &&
                                  (predicate == null || predicate(operand));
        }

        /// <summary>
        ///     Reads a local reference from a local-load instruction.
        ///     从本地变量读取指令读取本地变量引用。
        /// </summary>
        public static bool TryGetLocalLoad(CodeInstruction instruction, out HarmonyIlLocalRef local)
        {
            if (!TryGetLocalLoadIndex(instruction, out var index))
            {
                local = default;
                return false;
            }

            local = CreateLocalRef(index, instruction.operand);
            return true;
        }

        /// <summary>
        ///     Reads a local reference from a local-store instruction.
        ///     从本地变量存储指令读取本地变量引用。
        /// </summary>
        public static bool TryGetLocalStore(CodeInstruction instruction, out HarmonyIlLocalRef local)
        {
            if (!TryGetLocalStoreIndex(instruction, out var index))
            {
                local = default;
                return false;
            }

            local = CreateLocalRef(index, instruction.operand);
            return true;
        }

        /// <summary>
        ///     Reads a local reference from a local load or store instruction.
        ///     从本地变量读取或存储指令读取本地变量引用。
        /// </summary>
        public static bool TryGetLocal(CodeInstruction instruction, out HarmonyIlLocalRef local)
        {
            return TryGetLocalLoad(instruction, out local) || TryGetLocalStore(instruction, out local);
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
                case LocalVariableInfo local:
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

        private static HarmonyIlLocalRef CreateLocalRef(int index, object? operand)
        {
            return operand switch
            {
                LocalBuilder builder => new(index, builder, builder.LocalType),
                LocalVariableInfo info => new(index, null, info.LocalType),
                _ => new(index),
            };
        }

        private static bool IsAnyCallInstruction(CodeInstruction instruction)
        {
            return instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt;
        }

        private static bool IsFieldAccessInstruction(CodeInstruction instruction)
        {
            return instruction.opcode == OpCodes.Ldfld ||
                   instruction.opcode == OpCodes.Ldflda ||
                   instruction.opcode == OpCodes.Ldsfld ||
                   instruction.opcode == OpCodes.Stfld ||
                   instruction.opcode == OpCodes.Stsfld;
        }
    }
}
