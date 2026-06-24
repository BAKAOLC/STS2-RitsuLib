using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     Return-site insertion strategy for generated Harmony IL payload patches.
    ///     生成式 Harmony IL payload patch 使用的返回点插入策略。
    /// </summary>
    public enum HarmonyIlReturnInsertionMode
    {
        /// <summary>
        ///     Insert before the first <see cref="OpCodes.Ret" />.
        ///     插入到第一条 <see cref="OpCodes.Ret" /> 之前。
        /// </summary>
        BeforeFirstRet,

        /// <summary>
        ///     Insert before the only <see cref="OpCodes.Ret" />; fail if there is not exactly one return.
        ///     插入到唯一一条 <see cref="OpCodes.Ret" /> 之前；如果返回点不是唯一的则失败。
        /// </summary>
        BeforeSingleRet,

        /// <summary>
        ///     Insert before the last <see cref="OpCodes.Ret" />.
        ///     插入到最后一条 <see cref="OpCodes.Ret" /> 之前。
        /// </summary>
        BeforeLastRet,

        /// <summary>
        ///     Insert before every <see cref="OpCodes.Ret" />.
        ///     插入到每条 <see cref="OpCodes.Ret" /> 之前。
        /// </summary>
        BeforeEachRet,
    }

    /// <summary>
    ///     Handle for a generated IL payload transpiler.
    ///     生成式 IL payload transpiler 的句柄。
    /// </summary>
    public sealed class HarmonyIlPayloadTranspilerHandle : IDisposable
    {
        internal HarmonyIlPayloadTranspilerHandle(string payloadId, HarmonyMethod harmonyMethod)
        {
            PayloadId = payloadId;
            HarmonyMethod = harmonyMethod;
        }

        /// <summary>
        ///     Stable payload id used by the generated transpiler method.
        ///     生成的 transpiler 方法使用的稳定 payload id。
        /// </summary>
        public string PayloadId { get; }

        /// <summary>
        ///     Harmony method that can be passed as a transpiler.
        ///     可作为 transpiler 传给 Harmony 的方法。
        /// </summary>
        public HarmonyMethod HarmonyMethod { get; }

        /// <summary>
        ///     Removes the payload from the static registry. Call only after the owning Harmony patch is removed.
        ///     从静态注册表移除此 payload。仅应在所属 Harmony patch 已移除后调用。
        /// </summary>
        public void Dispose()
        {
            HarmonyIlPayloadTranspiler.Remove(PayloadId);
        }
    }

    /// <summary>
    ///     Builds per-payload Harmony transpilers for generated IL rewrites.
    ///     为生成式 IL 改写创建逐 payload 的 Harmony transpiler。
    /// </summary>
    public static class HarmonyIlPayloadTranspiler
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<string, Payload> Payloads = [];
        private static int _nextPayloadId;

        /// <summary>
        ///     Creates a Harmony transpiler that inserts <paramref name="payload" /> at return sites.
        ///     创建一个在返回点插入 <paramref name="payload" /> 的 Harmony transpiler。
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

            var payloadId = AllocatePayloadId();
            var snapshot = HarmonyIl.CloneAll(payload);
            lock (Gate)
            {
                Payloads.Add(payloadId, new(
                    payloadId,
                    snapshot,
                    operation,
                    mode,
                    moveLabelsAndBlocksToInserted,
                    validateOutput));
            }

            return new(payloadId, new(CreateDynamicTranspiler(payloadId)));
        }

        /// <summary>
        ///     Creates a <see cref="DynamicPatchInfo" /> using a generated return-insertion transpiler.
        ///     使用生成式返回点插入 transpiler 创建 <see cref="DynamicPatchInfo" />。
        /// </summary>
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

            var handle = CreateReturnInsertion(
                payload,
                operation,
                mode,
                moveLabelsAndBlocksToInserted,
                validateOutput);

            return new(
                id,
                originalMethod,
                transpiler: handle.HarmonyMethod,
                isCritical: isCritical,
                description: description);
        }

        /// <summary>
        ///     Applies a generated return-insertion transpiler directly to <paramref name="originalMethod" />.
        ///     将生成式返回点插入 transpiler 直接应用到 <paramref name="originalMethod" />。
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
            harmony.Patch(originalMethod, transpiler: handle.HarmonyMethod);
            return handle;
        }

        internal static void Remove(string payloadId)
        {
            lock (Gate)
            {
                Payloads.Remove(payloadId);
            }
        }

        private static IEnumerable<CodeInstruction> Transpile(
            IEnumerable<CodeInstruction> instructions,
            string payloadId)
        {
            Payload? payload;
            lock (Gate)
            {
                Payloads.TryGetValue(payloadId, out payload);
            }

            if (payload is null)
                throw new InvalidOperationException(
                    $"Harmony IL payload '{payloadId}' is not registered.");

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
    }
}
