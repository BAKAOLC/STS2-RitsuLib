using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     <para xml:lang="en">Return-site insertion strategy for generated Harmony IL payload patches.</para>
    ///     <para xml:lang="zh-CN">生成式 Harmony IL 载荷补丁使用的返回点插入策略。</para>
    /// </summary>
    public enum HarmonyIlReturnInsertionMode
    {
        /// <summary>
        ///     <para xml:lang="en">Insert before the first <see cref="OpCodes.Ret" />.</para>
        ///     <para xml:lang="zh-CN">插入到第一条 <see cref="OpCodes.Ret" /> 之前。</para>
        /// </summary>
        BeforeFirstRet,

        /// <summary>
        ///     <para xml:lang="en">Insert before the only <see cref="OpCodes.Ret" />; fail if there is not exactly one return.</para>
        ///     <para xml:lang="zh-CN">插入到唯一一条 <see cref="OpCodes.Ret" /> 之前；如果返回点不是唯一的则失败。</para>
        /// </summary>
        BeforeSingleRet,

        /// <summary>
        ///     <para xml:lang="en">Insert before the last <see cref="OpCodes.Ret" />.</para>
        ///     <para xml:lang="zh-CN">插入到最后一条 <see cref="OpCodes.Ret" /> 之前。</para>
        /// </summary>
        BeforeLastRet,

        /// <summary>
        ///     <para xml:lang="en">Insert before every <see cref="OpCodes.Ret" />.</para>
        ///     <para xml:lang="zh-CN">插入到每条 <see cref="OpCodes.Ret" /> 之前。</para>
        /// </summary>
        BeforeEachRet,
    }

    /// <summary>
    ///     <para xml:lang="en">Handle for a generated IL payload transpiler.</para>
    ///     <para xml:lang="zh-CN">生成式 IL 载荷转译器的句柄。</para>
    /// </summary>
    public sealed class HarmonyIlPayloadTranspilerHandle : IDisposable
    {
        private IDisposable? _lifetimeLease;

        internal HarmonyIlPayloadTranspilerHandle(
            string payloadId,
            HarmonyMethod harmonyMethod,
            IDisposable lifetimeLease)
        {
            PayloadId = payloadId;
            HarmonyMethod = harmonyMethod;
            _lifetimeLease = lifetimeLease;
        }

        /// <summary>
        ///     <para xml:lang="en">Stable payload id used by the generated transpiler method.</para>
        ///     <para xml:lang="zh-CN">生成的转译器方法使用的稳定载荷 ID。</para>
        /// </summary>
        public string PayloadId { get; }

        /// <summary>
        ///     <para xml:lang="en">Harmony method that can be passed as a transpiler.</para>
        ///     <para xml:lang="zh-CN">可作为转译器传给 Harmony 的方法。</para>
        /// </summary>
        public HarmonyMethod HarmonyMethod { get; }

        /// <summary>
        ///     <para xml:lang="en">Removes the payload from the static registry. Call only after the owning Harmony patch is removed.</para>
        ///     <para xml:lang="zh-CN">从静态注册表移除此载荷。仅应在所属 Harmony 补丁已移除后调用。</para>
        /// </summary>
        public void Dispose()
        {
            Interlocked.Exchange(ref _lifetimeLease, null)?.Dispose();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Builds per-payload Harmony transpilers for generated IL rewrites.</para>
    ///     <para xml:lang="zh-CN">为生成式 IL 改写创建逐载荷的 Harmony 转译器。</para>
    /// </summary>
    public static class HarmonyIlPayloadTranspiler
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<string, PayloadRegistration> Payloads = [];
        private static int _nextPayloadId;

        /// <summary>
        ///     <para xml:lang="en">Creates a Harmony transpiler that inserts <paramref name="payload" /> at return sites.</para>
        ///     <para xml:lang="zh-CN">创建一个在返回点插入 <paramref name="payload" /> 的 Harmony 转译器。</para>
        /// </summary>
        public static HarmonyIlPayloadTranspilerHandle CreateReturnInsertion(
            IEnumerable<CodeInstruction> payload,
            string operation = "Harmony IL payload return insertion",
            HarmonyIlReturnInsertionMode mode = HarmonyIlReturnInsertionMode.BeforeSingleRet,
            bool moveLabelsAndBlocksToInserted = false,
            bool validateOutput = true)
        {
            ArgumentNullException.ThrowIfNull(payload);
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);

            var registration = CreateRegistration(
                payload,
                operation,
                mode,
                moveLabelsAndBlocksToInserted,
                validateOutput);
            return new(
                registration.Payload.Id,
                registration.HarmonyMethod,
                registration.Acquire());
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a <see cref="DynamicPatchInfo" /> using a generated return-insertion transpiler.</para>
        ///     <para xml:lang="zh-CN">使用生成式返回点插入转译器创建 <see cref="DynamicPatchInfo" />。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Apply the result through <see cref="STS2RitsuLib.Patching.Core.ModPatcher" />, which owns the generated payload for the
        ///         applied patch's lifetime. For direct Harmony calls, use <see cref="CreateReturnInsertion" /> or
        ///         <see cref="PatchReturnInsertion" /> and retain the returned handle until the patch is removed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         请通过 <see cref="STS2RitsuLib.Patching.Core.ModPatcher" /> 应用返回结果，由补丁器在补丁生效期间持有生成的载荷。直接调用
        ///         Harmony 时，请使用 <see cref="CreateReturnInsertion" /> 或 <see cref="PatchReturnInsertion" />，
        ///         并保留返回的句柄直至补丁被移除。
        ///     </para>
        /// </remarks>
        public static DynamicPatchInfo CreateReturnInsertionPatch(
            string id,
            MethodBase originalMethod,
            IEnumerable<CodeInstruction> payload,
            string? description = null,
            bool isCritical = true,
            string operation = "Harmony IL payload return insertion",
            HarmonyIlReturnInsertionMode mode = HarmonyIlReturnInsertionMode.BeforeSingleRet,
            bool moveLabelsAndBlocksToInserted = false,
            bool validateOutput = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(originalMethod);

            var registration = CreateRegistration(
                payload,
                operation,
                mode,
                moveLabelsAndBlocksToInserted,
                validateOutput);

            var patch = new DynamicPatchInfo(
                id,
                originalMethod,
                transpiler: registration.HarmonyMethod,
                isCritical: isCritical,
                description: description);
            patch.SetLifetimeLeaseFactory(registration.Acquire);
            return patch;
        }

        /// <summary>
        ///     <para xml:lang="en">Applies a generated return-insertion transpiler directly to <paramref name="originalMethod" />.</para>
        ///     <para xml:lang="zh-CN">将生成式返回点插入转译器直接应用到 <paramref name="originalMethod" />。</para>
        /// </summary>
        public static HarmonyIlPayloadTranspilerHandle PatchReturnInsertion(
            Harmony harmony,
            MethodBase originalMethod,
            IEnumerable<CodeInstruction> payload,
            string operation = "Harmony IL payload return insertion",
            HarmonyIlReturnInsertionMode mode = HarmonyIlReturnInsertionMode.BeforeSingleRet,
            bool moveLabelsAndBlocksToInserted = false,
            bool validateOutput = true)
        {
            ArgumentNullException.ThrowIfNull(harmony);
            ArgumentNullException.ThrowIfNull(originalMethod);

            var handle = CreateReturnInsertion(
                payload,
                operation,
                mode,
                moveLabelsAndBlocksToInserted,
                validateOutput);
            try
            {
                harmony.Patch(originalMethod, transpiler: handle.HarmonyMethod);
                return handle;
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }

        private static PayloadRegistration CreateRegistration(
            IEnumerable<CodeInstruction> payload,
            string operation,
            HarmonyIlReturnInsertionMode mode,
            bool moveLabelsAndBlocksToInserted,
            bool validateOutput)
        {
            ArgumentNullException.ThrowIfNull(payload);
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);

            var payloadId = AllocatePayloadId();
            var registeredPayload = new Payload(
                payloadId,
                HarmonyIl.CloneAll(payload),
                operation,
                mode,
                moveLabelsAndBlocksToInserted,
                validateOutput);

            return new(
                registeredPayload,
                new(CreateFactoryMethod(payloadId)));
        }

        private static IEnumerable<CodeInstruction> Transpile(
            IEnumerable<CodeInstruction> instructions,
            string payloadId)
        {
            PayloadRegistration? registration;
            lock (Gate)
            {
                Payloads.TryGetValue(payloadId, out registration);
            }

            if (registration is null)
                throw new InvalidOperationException(
                    $"Harmony IL payload '{payloadId}' is not registered.");

            var payload = registration.Payload;
            var insertion = HarmonyIl.CloneAll(payload.Instructions);
            var rewriter = HarmonyIlRewriter.From(instructions);
            var report = payload.Mode switch
            {
                HarmonyIlReturnInsertionMode.BeforeFirstRet => rewriter.InsertBeforeFirstRet(
                    payload.Operation,
                    insertion,
                    moveLabelsAndBlocksToInserted: payload.MoveLabelsAndBlocksToInserted),
                HarmonyIlReturnInsertionMode.BeforeSingleRet => rewriter.InsertBeforeSingleRet(
                    payload.Operation,
                    insertion,
                    moveLabelsAndBlocksToInserted: payload.MoveLabelsAndBlocksToInserted),
                HarmonyIlReturnInsertionMode.BeforeLastRet => rewriter.InsertBeforeLastRet(
                    payload.Operation,
                    insertion,
                    moveLabelsAndBlocksToInserted: payload.MoveLabelsAndBlocksToInserted),
                HarmonyIlReturnInsertionMode.BeforeEachRet => rewriter.InsertBeforeEachRet(
                    payload.Operation,
                    insertion,
                    moveLabelsAndBlocksToInserted: payload.MoveLabelsAndBlocksToInserted),
                _ => throw new ArgumentOutOfRangeException(nameof(payload.Mode), payload.Mode, null),
            };

            report.RequireSucceeded();
            report.RequireApplied();

            return payload.ValidateOutput
                ? rewriter.InstructionsChecked(payload.Operation)
                : rewriter.Instructions();
        }

        private static string AllocatePayloadId()
        {
            lock (Gate)
            {
                return $"ritsulib_il_payload_{++_nextPayloadId:D6}";
            }
        }

        private static ModuleBuilder CreateFactoryModule(string payloadId)
        {
            var sourceAssembly = typeof(HarmonyIlPayloadTranspiler).Assembly;
            var sourceAssemblyName = sourceAssembly.GetName().Name
                                     ?? throw new InvalidOperationException("RitsuLib assembly has no simple name.");
            var assemblyName = new AssemblyName(
                $"{sourceAssemblyName}.HarmonyIlPayloadFactory.{payloadId}");
            var assembly = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName,
                AssemblyBuilderAccess.RunAndCollect);
            var ignoresAccessChecksTo = typeof(System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute)
                .GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    [typeof(string)],
                    null)
                ?? throw new MissingMethodException(
                    typeof(System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute).FullName,
                    ".ctor");
            assembly.SetCustomAttribute(new(
                ignoresAccessChecksTo,
                [sourceAssemblyName]));
            return assembly.DefineDynamicModule(assemblyName.Name!);
        }

        private static MethodInfo CreateFactoryMethod(string payloadId)
        {
            var factoryModule = CreateFactoryModule(payloadId);
            var type = factoryModule.DefineType(
                $"RitsuLibHarmonyIlPayloadFactory_{payloadId}",
                TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
            var method = type.DefineMethod(
                "CreateTranspiler",
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                typeof(DynamicMethod),
                [typeof(MethodBase)]);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldstr, payloadId);
            il.Emit(OpCodes.Call, AccessTools.DeclaredMethod(
                typeof(HarmonyIlPayloadTranspiler),
                nameof(CreateDynamicTranspiler)));
            il.Emit(OpCodes.Ret);

            return type.CreateType()!.GetMethod(
                       method.Name,
                       BindingFlags.Public | BindingFlags.Static)
                   ?? throw new MissingMethodException(type.FullName, method.Name);
        }

        private static DynamicMethod CreateDynamicTranspiler(string payloadId)
        {
            var method = new DynamicMethod(
                $"RitsuLibHarmonyIlPayloadTranspiler_{payloadId}",
                typeof(IEnumerable<CodeInstruction>),
                [typeof(IEnumerable<CodeInstruction>)],
                typeof(HarmonyIlPayloadTranspiler).Module,
                true);

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, payloadId);
            il.Emit(OpCodes.Call, AccessTools.DeclaredMethod(
                typeof(HarmonyIlPayloadTranspiler),
                nameof(Transpile)));
            il.Emit(OpCodes.Ret);
            return method;
        }

        private sealed record Payload(
            string Id,
            IReadOnlyList<CodeInstruction> Instructions,
            string Operation,
            HarmonyIlReturnInsertionMode Mode,
            bool MoveLabelsAndBlocksToInserted,
            bool ValidateOutput);

        private sealed class PayloadRegistration(Payload payload, HarmonyMethod harmonyMethod)
        {
            private int _leaseCount;

            public Payload Payload { get; } = payload;

            public HarmonyMethod HarmonyMethod { get; } = harmonyMethod;

            public IDisposable Acquire()
            {
                lock (Gate)
                {
                    if (_leaseCount == int.MaxValue)
                        throw new InvalidOperationException(
                            $"Harmony IL payload '{Payload.Id}' has too many active owners.");
                    if (_leaseCount == 0)
                        Payloads.Add(Payload.Id, this);
                    _leaseCount++;
                }

                return new PayloadLease(this);
            }

            private void Release()
            {
                lock (Gate)
                {
                    if (_leaseCount <= 0)
                        throw new InvalidOperationException(
                            $"Harmony IL payload '{Payload.Id}' has no active owner.");

                    _leaseCount--;
                    if (_leaseCount == 0)
                        Payloads.Remove(Payload.Id);
                }
            }

            private sealed class PayloadLease(PayloadRegistration registration) : IDisposable
            {
                private PayloadRegistration? _registration = registration;

                public void Dispose()
                {
                    Interlocked.Exchange(ref _registration, null)?.Release();
                }
            }
        }
    }
}
