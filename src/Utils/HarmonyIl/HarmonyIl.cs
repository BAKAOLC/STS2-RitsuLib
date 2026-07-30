using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     <para xml:lang="en">A local variable reference decoded from Harmony IL.</para>
    ///     <para xml:lang="zh-CN">从 Harmony IL 解码出的本地变量引用。</para>
    /// </summary>
    public readonly record struct HarmonyIlLocalRef(int Index, LocalBuilder? Builder = null, Type? LocalType = null)
    {
        /// <summary>
        ///     <para xml:lang="en">True when the local variable type is known.</para>
        ///     <para xml:lang="zh-CN">已知本地变量类型时为 true。</para>
        /// </summary>
        public bool HasKnownType => LocalType != null;

        /// <summary>
        ///     <para xml:lang="en">Creates a load instruction for this local.</para>
        ///     <para xml:lang="zh-CN">为此本地变量创建读取指令。</para>
        /// </summary>
        public CodeInstruction Load()
        {
            return HarmonyIl.LoadLocal(this);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a store instruction for this local.</para>
        ///     <para xml:lang="zh-CN">为此本地变量创建存储指令。</para>
        /// </summary>
        public CodeInstruction Store()
        {
            return HarmonyIl.StoreLocal(this);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns true when both references point at the same local index.</para>
        ///     <para xml:lang="zh-CN">当两个引用指向同一本地变量索引时返回 true。</para>
        /// </summary>
        public bool IsSameLocal(HarmonyIlLocalRef other)
        {
            return Index == other.Index;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Small instruction factories and predicates for RitsuLib Harmony transpilers.</para>
    ///     <para xml:lang="zh-CN">供 RitsuLib Harmony 转译器使用的小型指令工厂与谓词。</para>
    /// </summary>
    public static class HarmonyIl
    {
        /// <summary>
        ///     <para xml:lang="en">Returns a required reflected method or throws a consistent IL rewrite error.</para>
        ///     <para xml:lang="zh-CN">返回必需的反射方法，或抛出统一的 IL 改写错误。</para>
        /// </summary>
        public static MethodInfo RequireMethod(MethodInfo? method, string operation)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);
            return method ?? throw new MissingMethodException($"{operation}: required method could not be resolved.");
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a local-load instruction using the short opcode where possible.</para>
        ///     <para xml:lang="zh-CN">创建本地变量加载指令；可用时使用短操作码。</para>
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
        ///     <para xml:lang="en">Creates a local-store instruction using the short opcode where possible.</para>
        ///     <para xml:lang="zh-CN">创建本地变量存储指令；可用时使用短操作码。</para>
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
        ///     <para xml:lang="en">Creates an argument-load instruction using the short opcode where possible.</para>
        ///     <para xml:lang="zh-CN">创建参数加载指令；可用时使用短操作码。</para>
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
        ///     <para xml:lang="en">Creates an int32 constant-load instruction using the shortest opcode.</para>
        ///     <para xml:lang="zh-CN">创建 32 位整数常量加载指令；使用最短操作码。</para>
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
        ///     <para xml:lang="en">Creates a string-load instruction.</para>
        ///     <para xml:lang="zh-CN">创建字符串加载指令。</para>
        /// </summary>
        public static CodeInstruction Ldstr(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new(OpCodes.Ldstr, value);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a field-load instruction.</para>
        ///     <para xml:lang="zh-CN">创建字段加载指令。</para>
        /// </summary>
        public static CodeInstruction Ldfld(FieldInfo field)
        {
            ArgumentNullException.ThrowIfNull(field);
            return new(OpCodes.Ldfld, field);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a field-address load instruction.</para>
        ///     <para xml:lang="zh-CN">创建字段地址加载指令。</para>
        /// </summary>
        public static CodeInstruction Ldflda(FieldInfo field)
        {
            ArgumentNullException.ThrowIfNull(field);
            return new(OpCodes.Ldflda, field);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a static-field-load instruction.</para>
        ///     <para xml:lang="zh-CN">创建静态字段加载指令。</para>
        /// </summary>
        public static CodeInstruction Ldsfld(FieldInfo field)
        {
            ArgumentNullException.ThrowIfNull(field);
            return new(OpCodes.Ldsfld, field);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a field-store instruction.</para>
        ///     <para xml:lang="zh-CN">创建字段存储指令。</para>
        /// </summary>
        public static CodeInstruction Stfld(FieldInfo field)
        {
            ArgumentNullException.ThrowIfNull(field);
            return new(OpCodes.Stfld, field);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a static-field-store instruction.</para>
        ///     <para xml:lang="zh-CN">创建静态字段存储指令。</para>
        /// </summary>
        public static CodeInstruction Stsfld(FieldInfo field)
        {
            ArgumentNullException.ThrowIfNull(field);
            return new(OpCodes.Stsfld, field);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a call instruction.</para>
        ///     <para xml:lang="zh-CN">创建 call 指令。</para>
        /// </summary>
        public static CodeInstruction Call(MethodInfo method)
        {
            ArgumentNullException.ThrowIfNull(method);
            return new(OpCodes.Call, method);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a callvirt instruction.</para>
        ///     <para xml:lang="zh-CN">创建 callvirt 指令。</para>
        /// </summary>
        public static CodeInstruction Callvirt(MethodInfo method)
        {
            ArgumentNullException.ThrowIfNull(method);
            return new(OpCodes.Callvirt, method);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an object-construction instruction.</para>
        ///     <para xml:lang="zh-CN">创建对象构造指令。</para>
        /// </summary>
        public static CodeInstruction Newobj(ConstructorInfo constructor)
        {
            ArgumentNullException.ThrowIfNull(constructor);
            return new(OpCodes.Newobj, constructor);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a null-load instruction.</para>
        ///     <para xml:lang="zh-CN">创建 null 加载指令。</para>
        /// </summary>
        public static CodeInstruction Ldnull()
        {
            return new(OpCodes.Ldnull);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a duplicate-stack-value instruction.</para>
        ///     <para xml:lang="zh-CN">创建复制栈顶值指令。</para>
        /// </summary>
        public static CodeInstruction Dup()
        {
            return new(OpCodes.Dup);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a pop instruction.</para>
        ///     <para xml:lang="zh-CN">创建 pop 指令。</para>
        /// </summary>
        public static CodeInstruction Pop()
        {
            return new(OpCodes.Pop);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a ret instruction.</para>
        ///     <para xml:lang="zh-CN">创建 ret 指令。</para>
        /// </summary>
        public static CodeInstruction Ret()
        {
            return new(OpCodes.Ret);
        }

        /// <summary>
        ///     <para xml:lang="en">Converts a local-store instruction to the corresponding local-load instruction.</para>
        ///     <para xml:lang="zh-CN">将本地变量存储指令转换为对应的本地变量读取指令。</para>
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
        ///     <para xml:lang="en">Converts a local-load instruction to the corresponding local-store instruction.</para>
        ///     <para xml:lang="zh-CN">将本地变量读取指令转换为对应的本地变量存储指令。</para>
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
        ///     <para xml:lang="en">Creates a local-load instruction from a decoded local reference.</para>
        ///     <para xml:lang="zh-CN">根据已解码的本地变量引用创建读取指令。</para>
        /// </summary>
        public static CodeInstruction LoadLocal(HarmonyIlLocalRef local)
        {
            return local.Builder != null ? new(OpCodes.Ldloc_S, local.Builder) : Ldloc(local.Index);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a local-store instruction from a decoded local reference.</para>
        ///     <para xml:lang="zh-CN">根据已解码的本地变量引用创建存储指令。</para>
        /// </summary>
        public static CodeInstruction StoreLocal(HarmonyIlLocalRef local)
        {
            return local.Builder != null ? new(OpCodes.Stloc_S, local.Builder) : Stloc(local.Index);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns true when both instructions reference the same local variable.</para>
        ///     <para xml:lang="zh-CN">当两条指令引用同一本地变量时返回 true。</para>
        /// </summary>
        public static bool SameLocal(CodeInstruction left, CodeInstruction right)
        {
            return TryGetLocal(left, out var leftLocal) &&
                   TryGetLocal(right, out var rightLocal) &&
                   leftLocal.IsSameLocal(rightLocal);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches any instruction.</para>
        ///     <para xml:lang="zh-CN">匹配任意指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> Any()
        {
            return static _ => true;
        }

        /// <summary>
        ///     <para xml:lang="en">Negates another instruction predicate.</para>
        ///     <para xml:lang="zh-CN">对另一个指令谓词取反。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> Not(Func<CodeInstruction, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return instruction => !predicate(instruction);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches when any supplied instruction predicate matches.</para>
        ///     <para xml:lang="zh-CN">任一给定指令谓词匹配时即匹配。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> OneOf(params Func<CodeInstruction, bool>[] predicates)
        {
            ArgumentNullException.ThrowIfNull(predicates);
            return instruction => predicates.Any(predicate => predicate(instruction));
        }

        /// <summary>
        ///     <para xml:lang="en">Matches an opcode and optional operand.</para>
        ///     <para xml:lang="zh-CN">匹配操作码和可选操作数。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> Is(OpCode opcode, object? operand = null)
        {
            return instruction => instruction.opcode == opcode &&
                                  (operand == null || Equals(instruction.operand, operand));
        }

        /// <summary>
        ///     <para xml:lang="en">Matches an opcode and exact operand, including a null operand.</para>
        ///     <para xml:lang="zh-CN">匹配操作码和精确操作数，包括 null 操作数。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsExact(OpCode opcode, object? operand)
        {
            return instruction => instruction.opcode == opcode && Equals(instruction.operand, operand);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches an argument-load instruction.</para>
        ///     <para xml:lang="zh-CN">匹配参数读取指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdarg(int? index = null)
        {
            return instruction => TryGetArgumentIndex(instruction, out var actual) &&
                                  (index == null || actual == index.Value);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a local-load instruction.</para>
        ///     <para xml:lang="zh-CN">匹配本地变量读取指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdloc(int? index = null)
        {
            return instruction => TryGetLocalLoadIndex(instruction, out var actual) &&
                                  (index == null || actual == index.Value);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a local-store instruction.</para>
        ///     <para xml:lang="zh-CN">匹配本地变量存储指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsStloc(int? index = null)
        {
            return instruction => TryGetLocalStoreIndex(instruction, out var actual) &&
                                  (index == null || actual == index.Value);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a local-load instruction for the supplied local reference.</para>
        ///     <para xml:lang="zh-CN">匹配指定本地变量引用的读取指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdloc(HarmonyIlLocalRef local)
        {
            return instruction => TryGetLocalLoad(instruction, out var actual) && actual.IsSameLocal(local);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a local-store instruction for the supplied local reference.</para>
        ///     <para xml:lang="zh-CN">匹配指定本地变量引用的存储指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsStloc(HarmonyIlLocalRef local)
        {
            return instruction => TryGetLocalStore(instruction, out var actual) && actual.IsSameLocal(local);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a local-load instruction whose operand exposes the supplied local type.</para>
        ///     <para xml:lang="zh-CN">匹配其操作数公开指定本地变量类型的读取指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdlocOfType(Type localType)
        {
            ArgumentNullException.ThrowIfNull(localType);
            return instruction => TryGetLocalLoad(instruction, out var local) && local.LocalType == localType;
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a local-load instruction whose operand exposes the supplied local type.</para>
        ///     <para xml:lang="zh-CN">匹配其操作数公开指定本地变量类型的读取指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdlocOfType<T>()
        {
            return IsLdlocOfType(typeof(T));
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a local-store instruction whose operand exposes the supplied local type.</para>
        ///     <para xml:lang="zh-CN">匹配其操作数公开指定本地变量类型的存储指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsStlocOfType(Type localType)
        {
            ArgumentNullException.ThrowIfNull(localType);
            return instruction => TryGetLocalStore(instruction, out var local) && local.LocalType == localType;
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a local-store instruction whose operand exposes the supplied local type.</para>
        ///     <para xml:lang="zh-CN">匹配其操作数公开指定本地变量类型的存储指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsStlocOfType<T>()
        {
            return IsStlocOfType(typeof(T));
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a string-load instruction.</para>
        ///     <para xml:lang="zh-CN">匹配字符串加载指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdstr(string? value = null)
        {
            return instruction => instruction.opcode == OpCodes.Ldstr &&
                                  (value == null || (instruction.operand is string s &&
                                                     string.Equals(s, value, StringComparison.Ordinal)));
        }

        /// <summary>
        ///     <para xml:lang="en">Matches an int32 constant-load instruction.</para>
        ///     <para xml:lang="zh-CN">匹配 32 位整数常量加载指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdcI4(int? value = null)
        {
            return instruction => TryGetInt32(instruction, out var actual) &&
                                  (value == null || actual == value.Value);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a call/callvirt to the given method.</para>
        ///     <para xml:lang="zh-CN">匹配对指定方法的 call/callvirt。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsCall(MethodInfo? method)
        {
            return instruction => IsCallTo(instruction, method);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a call/callvirt instruction using a method predicate.</para>
        ///     <para xml:lang="zh-CN">使用方法谓词匹配 call/callvirt 指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsCall(Func<MethodInfo, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return instruction => IsAnyCallInstruction(instruction) &&
                                  instruction.operand is MethodInfo method &&
                                  predicate(method);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a call/callvirt to a method declared on the supplied type with the supplied name.</para>
        ///     <para xml:lang="zh-CN">匹配对指定类型上指定名称方法的 call/callvirt。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsCallTo(Type declaringType, string methodName)
        {
            ArgumentNullException.ThrowIfNull(declaringType);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
            return IsCall(method => method.DeclaringType == declaringType &&
                                    string.Equals(method.Name, methodName, StringComparison.Ordinal));
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a call/callvirt to a method with the supplied name.</para>
        ///     <para xml:lang="zh-CN">匹配对指定名称方法的 call/callvirt。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsCallTo(string methodName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
            return IsCall(method => string.Equals(method.Name, methodName, StringComparison.Ordinal));
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a call/callvirt whose return type is the supplied type.</para>
        ///     <para xml:lang="zh-CN">匹配返回类型为指定类型的 call/callvirt。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsCallReturning(Type returnType)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return IsCall(method => method.ReturnType == returnType);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a call/callvirt whose parameter types match the supplied sequence.</para>
        ///     <para xml:lang="zh-CN">匹配参数类型序列等于指定序列的 call/callvirt。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsCallWithParameters(params Type[] parameterTypes)
        {
            ArgumentNullException.ThrowIfNull(parameterTypes);
            return IsCall(method => method.GetParameters().Select(static parameter => parameter.ParameterType)
                .SequenceEqual(parameterTypes));
        }

        /// <summary>
        ///     <para xml:lang="en">Matches any call/callvirt instruction.</para>
        ///     <para xml:lang="zh-CN">匹配任意 call/callvirt 指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsAnyCall()
        {
            return static instruction => instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt;
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a newobj instruction for the supplied constructor.</para>
        ///     <para xml:lang="zh-CN">匹配指定构造函数的 newobj 指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsNewobj(ConstructorInfo? constructor = null)
        {
            return instruction => instruction.opcode == OpCodes.Newobj &&
                                  (constructor == null || Equals(instruction.operand, constructor));
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a field instruction.</para>
        ///     <para xml:lang="zh-CN">匹配字段指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsField(OpCode opcode, FieldInfo? field = null)
        {
            return instruction => instruction.opcode == opcode &&
                                  (field == null || Equals(instruction.operand, field));
        }

        /// <summary>
        ///     <para xml:lang="en">Matches an instance-field load instruction.</para>
        ///     <para xml:lang="zh-CN">匹配实例字段读取指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdfld(FieldInfo? field = null)
        {
            return IsField(OpCodes.Ldfld, field);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches an instance-field store instruction.</para>
        ///     <para xml:lang="zh-CN">匹配实例字段存储指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsStfld(FieldInfo? field = null)
        {
            return IsField(OpCodes.Stfld, field);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a static-field load instruction.</para>
        ///     <para xml:lang="zh-CN">匹配静态字段读取指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsLdsfld(FieldInfo? field = null)
        {
            return IsField(OpCodes.Ldsfld, field);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches any field access instruction for the supplied field.</para>
        ///     <para xml:lang="zh-CN">匹配对指定字段的任意字段访问指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsFieldAccess(FieldInfo? field = null)
        {
            return instruction => IsFieldAccessInstruction(instruction) &&
                                  (field == null || Equals(instruction.operand, field));
        }

        /// <summary>
        ///     <para xml:lang="en">Matches any field access instruction using a field predicate.</para>
        ///     <para xml:lang="zh-CN">使用字段谓词匹配任意字段访问指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsFieldAccess(Func<FieldInfo, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return instruction => IsFieldAccessInstruction(instruction) &&
                                  instruction.operand is FieldInfo field &&
                                  predicate(field);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches any field access instruction whose field type is the supplied type.</para>
        ///     <para xml:lang="zh-CN">匹配字段类型为指定类型的任意字段访问指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsFieldOfType(Type fieldType)
        {
            ArgumentNullException.ThrowIfNull(fieldType);
            return IsFieldAccess(field => field.FieldType == fieldType);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches any field access instruction for a named field on the supplied declaring type.</para>
        ///     <para xml:lang="zh-CN">匹配指定类型上指定名称字段的任意字段访问指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsFieldNamed(Type declaringType, string fieldName)
        {
            ArgumentNullException.ThrowIfNull(declaringType);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
            return IsFieldAccess(field => field.DeclaringType == declaringType &&
                                          string.Equals(field.Name, fieldName, StringComparison.Ordinal));
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a branch instruction.</para>
        ///     <para xml:lang="zh-CN">匹配分支指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsBranch()
        {
            return static instruction => instruction.Branches(out _);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches a ret instruction.</para>
        ///     <para xml:lang="zh-CN">匹配 ret 指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> IsRet()
        {
            return static instruction => instruction.opcode == OpCodes.Ret;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns true when the instruction calls the supplied method via call or callvirt.</para>
        ///     <para xml:lang="zh-CN">当指令通过 call 或 callvirt 调用指定方法时返回 true。</para>
        /// </summary>
        public static bool IsCallTo(CodeInstruction instruction, MethodInfo? method)
        {
            return method != null
                   && (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt)
                   && instruction.operand is MethodInfo called
                   && called == method;
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to read the target method from a call/callvirt instruction.</para>
        ///     <para xml:lang="zh-CN">尝试从 call/callvirt 指令读取目标方法。</para>
        /// </summary>
        public static bool TryGetCalledMethod(CodeInstruction instruction, out MethodInfo method)
        {
            ArgumentNullException.ThrowIfNull(instruction);

            if (IsAnyCallInstruction(instruction) && instruction.operand is MethodInfo called)
            {
                method = called;
                return true;
            }

            method = null!;
            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns true when the instruction calls the supplied generic method definition.</para>
        ///     <para xml:lang="zh-CN">当指令调用指定泛型方法定义时返回 true。</para>
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
        ///     <para xml:lang="en">
        ///         Returns true when the instruction calls a method declared on <paramref name="declaringType" />
        ///         with <paramref name="methodName" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">当指令调用 <paramref name="declaringType" /> 上名为 <paramref name="methodName" /> 的方法时返回 true。</para>
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
        ///     <para xml:lang="en">Returns true when the instruction loads the supplied 32-bit integer constant.</para>
        ///     <para xml:lang="zh-CN">当指令加载指定 32 位整数常量时返回 true。</para>
        /// </summary>
        public static bool LoadsInt32(CodeInstruction instruction, int value)
        {
            return TryGetInt32(instruction, out var actual) && actual == value;
        }

        /// <summary>
        ///     <para xml:lang="en">Reads a typed operand from an instruction.</para>
        ///     <para xml:lang="zh-CN">从指令读取指定类型的操作数。</para>
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
        ///     <para xml:lang="en">Returns true when the instruction operand equals the supplied operand.</para>
        ///     <para xml:lang="zh-CN">当指令操作数等于指定操作数时返回 true。</para>
        /// </summary>
        public static bool OperandEquals(CodeInstruction instruction, object? operand)
        {
            ArgumentNullException.ThrowIfNull(instruction);
            return Equals(instruction.operand, operand);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns true when a typed operand satisfies the supplied predicate.</para>
        ///     <para xml:lang="zh-CN">当指定类型的操作数满足谓词时返回 true。</para>
        /// </summary>
        public static bool OperandMatches<T>(CodeInstruction instruction, Func<T, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return TryGetOperand<T>(instruction, out var operand) && predicate(operand);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches instructions whose typed operand satisfies the supplied predicate.</para>
        ///     <para xml:lang="zh-CN">匹配指定类型的操作数满足谓词的指令。</para>
        /// </summary>
        public static Func<CodeInstruction, bool> HasOperand<T>(Func<T, bool>? predicate = null)
        {
            return instruction => TryGetOperand<T>(instruction, out var operand) &&
                                  (predicate == null || predicate(operand));
        }

        /// <summary>
        ///     <para xml:lang="en">Reads a local reference from a local-load instruction.</para>
        ///     <para xml:lang="zh-CN">从本地变量读取指令读取本地变量引用。</para>
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
        ///     <para xml:lang="en">Reads a local reference from a local-store instruction.</para>
        ///     <para xml:lang="zh-CN">从本地变量存储指令读取本地变量引用。</para>
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
        ///     <para xml:lang="en">Reads a local reference from a local load or store instruction.</para>
        ///     <para xml:lang="zh-CN">从本地变量读取或存储指令读取本地变量引用。</para>
        /// </summary>
        public static bool TryGetLocal(CodeInstruction instruction, out HarmonyIlLocalRef local)
        {
            return TryGetLocalLoad(instruction, out local) || TryGetLocalStore(instruction, out local);
        }

        /// <summary>
        ///     <para xml:lang="en">Reads the argument index from an argument-load instruction.</para>
        ///     <para xml:lang="zh-CN">从参数读取指令中读取参数索引。</para>
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
        ///     <para xml:lang="en">Reads the local index from a local-load instruction.</para>
        ///     <para xml:lang="zh-CN">从本地变量读取指令中读取本地变量索引。</para>
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
        ///     <para xml:lang="en">Reads the local index from a local-store instruction.</para>
        ///     <para xml:lang="zh-CN">从本地变量存储指令中读取本地变量索引。</para>
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
        ///     <para xml:lang="en">Reads the int32 constant from an integer-load instruction.</para>
        ///     <para xml:lang="zh-CN">从整数加载指令中读取 32 位整数常量。</para>
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
        ///     <para xml:lang="en">Returns true when the instruction has Harmony labels or exception blocks.</para>
        ///     <para xml:lang="zh-CN">当指令带有 Harmony 标签或异常块时返回 true。</para>
        /// </summary>
        public static bool HasMetadata(CodeInstruction instruction)
        {
            ArgumentNullException.ThrowIfNull(instruction);
            return instruction.labels.Count > 0 || instruction.blocks.Count > 0;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Moves labels and exception blocks from <paramref name="source" /> to the first replacement
        ///         instruction.
        ///     </para>
        ///     <para xml:lang="zh-CN">将 <paramref name="source" /> 的标签和异常块转移到第一条替换指令上。</para>
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
        ///     <para xml:lang="en">Clones every instruction in order.</para>
        ///     <para xml:lang="zh-CN">按顺序克隆每条指令。</para>
        /// </summary>
        public static CodeInstruction[] CloneAll(IEnumerable<CodeInstruction> instructions)
        {
            ArgumentNullException.ThrowIfNull(instructions);
            return [.. instructions.Select(static instruction => instruction.Clone())];
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
