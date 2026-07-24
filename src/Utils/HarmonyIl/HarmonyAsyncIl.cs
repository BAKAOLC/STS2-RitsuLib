using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     A compiler-generated await site found inside an async state machine <c>MoveNext</c>.
    ///     在 async 状态机 <c>MoveNext</c> 中找到的编译器生成 await 点。
    /// </summary>
    public sealed record HarmonyAsyncAwaitSite(
        int AwaitableProducerIndex,
        int GetAwaiterIndex,
        int AwaiterStoreIndex,
        int IsCompletedIndex,
        int? AwaitOnCompletedIndex,
        int GetResultIndex,
        HarmonyIlLocalRef AwaiterLocal,
        Type AwaitableType,
        Type AwaiterType,
        Type ResultType,
        MethodInfo? AwaitableCall,
        MethodInfo GetAwaiterMethod,
        MethodInfo IsCompletedGetter,
        MethodInfo? AwaitOnCompletedMethod,
        MethodInfo GetResultMethod,
        int? SuspendedState);

    /// <summary>
    ///     Helpers for recognizing and safely rewriting async state-machine await sites.
    ///     用于识别并安全改写 async 状态机 await 点的辅助工具。
    /// </summary>
    public static class HarmonyAsyncIl
    {
        private const int MaxIsCompletedSearchDistance = 12;
        private const int MaxAwaitOnCompletedSearchDistance = 96;

        /// <summary>
        ///     Finds all recognized await sites in a Harmony instruction list.
        ///     在 Harmony 指令列表中查找所有可识别的 await 点。
        /// </summary>
        public static IReadOnlyList<HarmonyAsyncAwaitSite> FindAwaitSites(IEnumerable<CodeInstruction> instructions)
        {
            ArgumentNullException.ThrowIfNull(instructions);
            var code = instructions as IReadOnlyList<CodeInstruction> ?? [.. instructions];
            return FindAwaitSites(code);
        }

        /// <summary>
        ///     Finds all recognized await sites in a Harmony instruction list.
        ///     在 Harmony 指令列表中查找所有可识别的 await 点。
        /// </summary>
        public static IReadOnlyList<HarmonyAsyncAwaitSite> FindAwaitSites(IReadOnlyList<CodeInstruction> code)
        {
            ArgumentNullException.ThrowIfNull(code);

            var sites = new List<HarmonyAsyncAwaitSite>();
            for (var i = 0; i < code.Count; i++)
            {
                if (!TryReadAwaitSite(code, i, out var site))
                    continue;

                sites.Add(site);
                i = Math.Max(i, site.GetResultIndex);
            }

            return sites;
        }

        /// <summary>
        ///     Finds await sites and returns assertion helpers.
        ///     查找 await 点并返回断言辅助对象。
        /// </summary>
        public static HarmonyAsyncAwaitSites FindAwaitSiteMatches(
            IReadOnlyList<CodeInstruction> code,
            string description = "async await sites")
        {
            return new(description, FindAwaitSites(code));
        }

        /// <summary>
        ///     Returns true when the instruction is a call/callvirt to a usable awaitable <c>GetAwaiter</c> method.
        ///     当指令调用可用 awaitable <c>GetAwaiter</c> 方法时返回 true。
        /// </summary>
        public static bool IsGetAwaiterCall(CodeInstruction instruction)
        {
            return TryGetAwaiterMethod(instruction, out _);
        }

        /// <summary>
        ///     Returns true when the instruction is a call/callvirt to an awaiter <c>GetResult</c> method.
        ///     当指令调用 awaiter <c>GetResult</c> 方法时返回 true。
        /// </summary>
        public static bool IsGetResultCall(CodeInstruction instruction)
        {
            return IsCallInstruction(instruction)
                   && instruction.operand is MethodInfo { Name: "GetResult" } method
                   && IsAwaiterType(method.DeclaringType);
        }

        /// <summary>
        ///     Redirects calls that are immediately awaited by the state machine.
        ///     重定向被状态机直接 await 的调用。
        /// </summary>
        public static HarmonyIlRewriteReport RedirectAwaitedCalls(
            HarmonyIlRewriter rewriter,
            string operation,
            MethodInfo fromMethod,
            MethodInfo toMethod,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null)
        {
            ArgumentNullException.ThrowIfNull(rewriter);
            return RedirectAwaitedCalls(
                rewriter.Instructions(),
                operation,
                fromMethod,
                toMethod,
                alreadySatisfied);
        }

        /// <summary>
        ///     Redirects calls that are immediately awaited by the state machine.
        ///     重定向被状态机直接 await 的调用。
        /// </summary>
        public static HarmonyIlRewriteReport RedirectAwaitedCalls(
            IList<CodeInstruction> code,
            string operation,
            MethodInfo fromMethod,
            MethodInfo toMethod,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null)
        {
            ArgumentNullException.ThrowIfNull(code);
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);
            ArgumentNullException.ThrowIfNull(fromMethod);
            ArgumentNullException.ThrowIfNull(toMethod);
            EnsureCompatibleAwaitedCallReplacement(fromMethod, toMethod);

            var before = code.Count;
            if (alreadySatisfied?.Invoke([.. code]) == true)
                return new(operation, 0, 0, before, before, true);

            var matchedIndexes = new List<int>();
            var appliedIndexes = new List<int>();
            foreach (var site in FindAwaitSites(code))
            {
                if (site.AwaitableCall != fromMethod)
                    continue;

                matchedIndexes.Add(site.AwaitableProducerIndex);
                var instruction = code[site.AwaitableProducerIndex];
                instruction.opcode = toMethod.IsStatic ? OpCodes.Call : instruction.opcode;
                instruction.operand = toMethod;
                appliedIndexes.Add(site.AwaitableProducerIndex);
            }

            return new(
                operation,
                matchedIndexes.Count,
                appliedIndexes.Count,
                before,
                code.Count,
                appliedIndexes.Count == 0 && alreadySatisfied?.Invoke([.. code]) == true,
                matchedIndexes,
                appliedIndexes);
        }

        /// <summary>
        ///     Replaces calls that are immediately awaited by the state machine.
        ///     替换被状态机直接 await 的调用。
        /// </summary>
        public static HarmonyIlRewriteReport ReplaceAwaitedCalls(
            HarmonyIlRewriter rewriter,
            string operation,
            MethodInfo fromMethod,
            Func<HarmonyAsyncAwaitSite, IReadOnlyList<CodeInstruction>> buildReplacement,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null)
        {
            ArgumentNullException.ThrowIfNull(rewriter);
            return ReplaceAwaitedCalls(
                rewriter.Instructions(),
                operation,
                fromMethod,
                buildReplacement,
                alreadySatisfied);
        }

        /// <summary>
        ///     Replaces calls that are immediately awaited by the state machine.
        ///     替换被状态机直接 await 的调用。
        /// </summary>
        public static HarmonyIlRewriteReport ReplaceAwaitedCalls(
            IList<CodeInstruction> code,
            string operation,
            MethodInfo fromMethod,
            Func<HarmonyAsyncAwaitSite, IReadOnlyList<CodeInstruction>> buildReplacement,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null)
        {
            ArgumentNullException.ThrowIfNull(code);
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);
            ArgumentNullException.ThrowIfNull(fromMethod);
            ArgumentNullException.ThrowIfNull(buildReplacement);

            var before = code.Count;
            if (alreadySatisfied?.Invoke([.. code]) == true)
                return new(operation, 0, 0, before, before, true);

            var sites = FindAwaitSites(code)
                .Where(site => site.AwaitableCall == fromMethod)
                .ToArray();
            var matchedIndexes = sites.Select(static site => site.AwaitableProducerIndex).ToArray();
            var appliedIndexes = new List<int>();

            foreach (var site in sites.OrderByDescending(static site => site.AwaitableProducerIndex))
            {
                var replacement = buildReplacement(site).ToList();
                ValidateAwaitedCallReplacement(site, replacement);

                HarmonyIl.MoveMetadataToFirst(code[site.AwaitableProducerIndex], replacement);
                code.RemoveAt(site.AwaitableProducerIndex);
                for (var i = 0; i < replacement.Count; i++)
                    code.Insert(site.AwaitableProducerIndex + i, replacement[i]);

                appliedIndexes.Add(site.AwaitableProducerIndex);
            }

            appliedIndexes.Sort();
            return new(
                operation,
                matchedIndexes.Length,
                appliedIndexes.Count,
                before,
                code.Count,
                appliedIndexes.Count == 0 && alreadySatisfied?.Invoke([.. code]) == true,
                matchedIndexes,
                appliedIndexes);
        }

        /// <summary>
        ///     Throws when <paramref name="toMethod" /> cannot replace <paramref name="fromMethod" /> at an await site.
        ///     当 <paramref name="toMethod" /> 不能在 await 点替换 <paramref name="fromMethod" /> 时抛出异常。
        /// </summary>
        public static void EnsureCompatibleAwaitedCallReplacement(MethodInfo fromMethod, MethodInfo toMethod)
        {
            ArgumentNullException.ThrowIfNull(fromMethod);
            ArgumentNullException.ThrowIfNull(toMethod);

            if (fromMethod.ReturnType != toMethod.ReturnType)
                throw new ArgumentException(
                    $"Awaited-call replacement must return exactly {fromMethod.ReturnType}; got {toMethod.ReturnType}.",
                    nameof(toMethod));

            var fromParameters = fromMethod.GetParameters().Select(static parameter => parameter.ParameterType)
                .ToArray();
            var toParameters = toMethod.GetParameters().Select(static parameter => parameter.ParameterType)
                .ToArray();

            if (fromMethod.IsStatic)
            {
                if (!toMethod.IsStatic || !fromParameters.SequenceEqual(toParameters))
                    throw NewSignatureException(fromMethod, toMethod);
                return;
            }

            if (!toMethod.IsStatic)
            {
                if (!fromParameters.SequenceEqual(toParameters))
                    throw NewSignatureException(fromMethod, toMethod);
                return;
            }

            var receiverType = fromMethod.DeclaringType ?? throw new ArgumentException(
                "Cannot redirect an instance method with no declaring type.",
                nameof(fromMethod));
            if (toParameters.Length != fromParameters.Length + 1 ||
                !toParameters[0].IsAssignableFrom(receiverType) ||
                !fromParameters.SequenceEqual(toParameters.Skip(1)))
                throw NewSignatureException(fromMethod, toMethod);
        }

        private static void ValidateAwaitedCallReplacement(
            HarmonyAsyncAwaitSite site,
            IReadOnlyList<CodeInstruction> replacement)
        {
            if (replacement.Count == 0)
                throw new ArgumentException("Awaited-call replacement cannot be empty.", nameof(replacement));

            if (!TryGetPushedType(replacement[^1], out var pushedType) ||
                !site.AwaitableType.IsAssignableFrom(pushedType))
                throw new ArgumentException(
                    $"Awaited-call replacement must leave an awaitable assignable to {site.AwaitableType} on the stack.");
        }

        private static bool TryReadAwaitSite(
            IReadOnlyList<CodeInstruction> code,
            int getAwaiterIndex,
            out HarmonyAsyncAwaitSite site)
        {
            site = null!;
            if (!TryGetAwaiterMethod(code[getAwaiterIndex], out var getAwaiterMethod))
                return false;

            var awaiterType = getAwaiterMethod.ReturnType;
            var awaitableType = getAwaiterMethod.DeclaringType ?? typeof(object);
            var awaitableProducerIndex = FindAwaitableProducerIndex(code, getAwaiterIndex, awaitableType);
            if (awaitableProducerIndex < 0)
                return false;

            if (!TryFindAwaiterStore(code, getAwaiterIndex + 1, out var storeIndex, out var awaiterLocal))
                return false;

            if (!TryFindIsCompletedCheck(
                    code,
                    storeIndex + 1,
                    awaiterLocal,
                    awaiterType,
                    out var isCompletedIndex,
                    out var isCompletedGetter))
                return false;

            var awaitOnCompletedIndex = TryFindAwaitOnCompleted(
                code,
                isCompletedIndex + 1,
                awaiterType,
                out var awaitOnCompletedMethod)
                ? awaitOnCompletedMethod.Index
                : default(int?);

            if (!TryFindGetResult(
                    code,
                    isCompletedIndex + 1,
                    awaiterLocal,
                    awaiterType,
                    out var getResultIndex,
                    out var getResultMethod))
                return false;

            site = new(
                awaitableProducerIndex,
                getAwaiterIndex,
                storeIndex,
                isCompletedIndex,
                awaitOnCompletedIndex,
                getResultIndex,
                awaiterLocal,
                awaitableType,
                awaiterType,
                getResultMethod.ReturnType,
                GetCallOperand(code[awaitableProducerIndex]),
                getAwaiterMethod,
                isCompletedGetter,
                awaitOnCompletedMethod.Method,
                getResultMethod,
                awaitOnCompletedIndex == null
                    ? null
                    : TryFindSuspendedState(code, isCompletedIndex + 1, awaitOnCompletedIndex.Value));
            return true;
        }

        private static bool TryGetAwaiterMethod(CodeInstruction instruction, out MethodInfo getAwaiterMethod)
        {
            if (IsCallInstruction(instruction) &&
                instruction.operand is MethodInfo { Name: "GetAwaiter" } method &&
                IsAwaiterType(method.ReturnType))
            {
                getAwaiterMethod = method;
                return true;
            }

            getAwaiterMethod = null!;
            return false;
        }

        private static bool IsAwaiterType(Type? type)
        {
            if (type == null)
                return false;

            return AccessTools.PropertyGetter(type, "IsCompleted") is { ReturnType: { } isCompletedType } &&
                   isCompletedType == typeof(bool) &&
                   AccessTools.Method(type, "GetResult") != null;
        }

        private static int FindAwaitableProducerIndex(
            IReadOnlyList<CodeInstruction> code,
            int getAwaiterIndex,
            Type awaitableType)
        {
            for (var i = getAwaiterIndex - 1; i >= Math.Max(0, getAwaiterIndex - 12); i--)
                if (TryGetPushedType(code[i], out var pushedType) && awaitableType.IsAssignableFrom(pushedType))
                    return i;

            return -1;
        }

        private static bool TryFindAwaiterStore(
            IReadOnlyList<CodeInstruction> code,
            int startIndex,
            out int storeIndex,
            out HarmonyIlLocalRef awaiterLocal)
        {
            for (var i = startIndex; i < Math.Min(code.Count, startIndex + 4); i++)
            {
                if (!HarmonyIl.TryGetLocalStore(code[i], out awaiterLocal))
                    continue;

                storeIndex = i;
                return true;
            }

            storeIndex = -1;
            awaiterLocal = default;
            return false;
        }

        private static bool TryFindIsCompletedCheck(
            IReadOnlyList<CodeInstruction> code,
            int startIndex,
            HarmonyIlLocalRef awaiterLocal,
            Type awaiterType,
            out int isCompletedIndex,
            out MethodInfo isCompletedGetter)
        {
            var end = Math.Min(code.Count, startIndex + MaxIsCompletedSearchDistance);
            for (var i = startIndex; i < end; i++)
            {
                if (!IsAwaiterIsCompletedCall(code[i], awaiterType, out isCompletedGetter))
                    continue;
                if (i == 0 || !LoadsLocalOrAddress(code[i - 1], awaiterLocal))
                    continue;

                isCompletedIndex = i;
                return true;
            }

            isCompletedIndex = -1;
            isCompletedGetter = null!;
            return false;
        }

        private static bool TryFindAwaitOnCompleted(
            IReadOnlyList<CodeInstruction> code,
            int startIndex,
            Type awaiterType,
            out (int Index, MethodInfo Method) method)
        {
            var end = Math.Min(code.Count, startIndex + MaxAwaitOnCompletedSearchDistance);
            for (var i = startIndex; i < end; i++)
            {
                if (code[i].opcode != OpCodes.Call ||
                    code[i].operand is not MethodInfo { Name: "AwaitUnsafeOnCompleted" or "AwaitOnCompleted" } called)
                    continue;

                if (called.IsGenericMethod)
                {
                    var genericArguments = called.GetGenericArguments();
                    if (genericArguments.Length > 0 && genericArguments[0] != awaiterType)
                        continue;
                }

                method = (i, called);
                return true;
            }

            method = default;
            return false;
        }

        private static bool TryFindGetResult(
            IReadOnlyList<CodeInstruction> code,
            int startIndex,
            HarmonyIlLocalRef awaiterLocal,
            Type awaiterType,
            out int getResultIndex,
            out MethodInfo getResultMethod)
        {
            for (var i = startIndex; i < code.Count; i++)
            {
                if (!IsAwaiterGetResultCall(code[i], awaiterType, out getResultMethod))
                    continue;
                if (i == 0 || !LoadsLocalOrAddress(code[i - 1], awaiterLocal))
                    continue;

                getResultIndex = i;
                return true;
            }

            getResultIndex = -1;
            getResultMethod = null!;
            return false;
        }

        private static bool IsAwaiterIsCompletedCall(
            CodeInstruction instruction,
            Type awaiterType,
            out MethodInfo isCompletedGetter)
        {
            if (IsCallInstruction(instruction) &&
                instruction.operand is MethodInfo { Name: "get_IsCompleted", ReturnType: { } returnType } method &&
                returnType == typeof(bool) &&
                method.DeclaringType == awaiterType)
            {
                isCompletedGetter = method;
                return true;
            }

            isCompletedGetter = null!;
            return false;
        }

        private static bool IsAwaiterGetResultCall(
            CodeInstruction instruction,
            Type awaiterType,
            out MethodInfo getResultMethod)
        {
            if (IsCallInstruction(instruction) &&
                instruction.operand is MethodInfo { Name: "GetResult" } method &&
                method.DeclaringType == awaiterType)
            {
                getResultMethod = method;
                return true;
            }

            getResultMethod = null!;
            return false;
        }

        private static bool LoadsLocalOrAddress(CodeInstruction instruction, HarmonyIlLocalRef local)
        {
            return (HarmonyIl.TryGetLocalLoad(instruction, out var load) && load.IsSameLocal(local)) ||
                   (TryGetLocalAddress(instruction, out var address) && address.IsSameLocal(local));
        }

        private static bool TryGetLocalAddress(CodeInstruction instruction, out HarmonyIlLocalRef local)
        {
            if (instruction.opcode != OpCodes.Ldloca && instruction.opcode != OpCodes.Ldloca_S)
            {
                local = default;
                return false;
            }

            local = instruction.operand switch
            {
                LocalBuilder builder => new(builder.LocalIndex, builder, builder.LocalType),
                LocalVariableInfo info => new(info.LocalIndex, null, info.LocalType),
                int index => new(index),
                byte index => new(index),
                sbyte index => new(index),
                short index => new(index),
                ushort index => new(index),
                _ => default,
            };
            return local.Index >= 0;
        }

        private static bool TryGetPushedType(CodeInstruction instruction, out Type type)
        {
            switch (instruction.operand)
            {
                case MethodInfo method when IsCallInstruction(instruction):
                    type = method.ReturnType;
                    return type != typeof(void);
                case ConstructorInfo constructor when instruction.opcode == OpCodes.Newobj:
                    type = constructor.DeclaringType!;
                    return type != null;
                case FieldInfo field when instruction.opcode == OpCodes.Ldfld || instruction.opcode == OpCodes.Ldsfld:
                    type = field.FieldType;
                    return true;
                case LocalBuilder local when HarmonyIl.TryGetLocalLoad(instruction, out _):
                    type = local.LocalType;
                    return true;
                case LocalVariableInfo local when HarmonyIl.TryGetLocalLoad(instruction, out _):
                    type = local.LocalType;
                    return true;
                default:
                    type = null!;
                    return false;
            }
        }

        private static MethodInfo? GetCallOperand(CodeInstruction instruction)
        {
            return IsCallInstruction(instruction) && instruction.operand is MethodInfo method
                ? method
                : null;
        }

        private static int? TryFindSuspendedState(IReadOnlyList<CodeInstruction> code, int startIndex, int endIndex)
        {
            for (var i = endIndex; i >= startIndex; i--)
                if (HarmonyIl.TryGetInt32(code[i], out var state) && state >= 0)
                    return state;

            return null;
        }

        private static bool IsCallInstruction(CodeInstruction instruction)
        {
            return instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt;
        }

        private static ArgumentException NewSignatureException(MethodInfo fromMethod, MethodInfo toMethod)
        {
            return new(
                $"Replacement method '{toMethod}' cannot consume the stack shape produced for awaited call '{fromMethod}'.");
        }
    }

    /// <summary>
    ///     A collection of async await-site matches with assertion helpers.
    ///     带断言辅助方法的 async await 点匹配集合。
    /// </summary>
    public sealed class HarmonyAsyncAwaitSites
    {
        internal HarmonyAsyncAwaitSites(string description, IEnumerable<HarmonyAsyncAwaitSite> items)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentNullException.ThrowIfNull(items);
            Description = description;
            Items = [.. items];
        }

        /// <summary>
        ///     Human-readable description used in assertion errors.
        ///     断言错误中使用的可读描述。
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///     Matched await sites.
        ///     已匹配的 await 点。
        /// </summary>
        public IReadOnlyList<HarmonyAsyncAwaitSite> Items { get; }

        /// <summary>
        ///     Number of matched await sites.
        ///     匹配到的 await 点数量。
        /// </summary>
        public int Count => Items.Count;

        /// <summary>
        ///     Returns true when at least one await site exists.
        ///     存在至少一个 await 点时返回 true。
        /// </summary>
        public bool Any => Items.Count > 0;

        /// <summary>
        ///     Requires exactly one await site and returns it.
        ///     要求恰好一个 await 点并返回它。
        /// </summary>
        public HarmonyAsyncAwaitSite RequireSingle()
        {
            return Items.Count == 1 ? Items[0] : throw NewCountException("exactly 1");
        }

        /// <summary>
        ///     Requires an exact await-site count.
        ///     要求精确 await 点数量。
        /// </summary>
        public HarmonyAsyncAwaitSites RequireExactly(int count)
        {
            return Items.Count == count ? this : throw NewCountException($"exactly {count}");
        }

        /// <summary>
        ///     Requires at least <paramref name="count" /> await sites.
        ///     要求至少 <paramref name="count" /> 个 await 点。
        /// </summary>
        public HarmonyAsyncAwaitSites RequireAtLeast(int count)
        {
            return Items.Count >= count ? this : throw NewCountException($"at least {count}");
        }

        /// <summary>
        ///     Returns a compact diagnostic string.
        ///     返回紧凑诊断字符串。
        /// </summary>
        public string Describe()
        {
            return $"{Description}: count={Items.Count}, getAwaiterIndexes=[" +
                   $"{string.Join(", ", Items.Select(static site => site.GetAwaiterIndex))}]";
        }

        private InvalidOperationException NewCountException(string expected)
        {
            return new($"{Description} matched {Items.Count} await site(s), expected {expected}. " +
                       $"getAwaiterIndexes=[{string.Join(", ", Items.Select(static site => site.GetAwaiterIndex))}].");
        }
    }
}
