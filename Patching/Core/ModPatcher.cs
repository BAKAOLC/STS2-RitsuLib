using System.Reflection;
using System.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Patching.Core
{
    /// <summary>
    ///     Owns one Harmony instance: registers static and dynamic patches, applies them, and can roll back.
    ///     持有一个 Harmony 实例：注册静态和动态补丁、应用补丁，并可回滚。
    /// </summary>
    /// <param name="patcherId">
    ///     Harmony id (must be unique per logical patcher).
    ///     Harmony id（每个逻辑补丁器必须唯一）。
    /// </param>
    /// <param name="logger">
    ///     Logger used for patch diagnostics.
    ///     用于补丁诊断的日志器。
    /// </param>
    /// <param name="patcherName">
    ///     Optional display name included in log prefix.
    ///     可选显示名称，会包含在日志前缀中。
    /// </param>
    public class ModPatcher(string patcherId, Logger logger, string patcherName = "")
    {
        private readonly Harmony _harmony = new(patcherId);

        private readonly string _logPrefix =
            string.IsNullOrEmpty(patcherName) ? "[Patcher] " : $"[Patcher - {patcherName}] ";

        private readonly Dictionary<string, bool> _patchedStatus = [];
        private readonly List<DynamicPatchInfo> _registeredDynamicPatches = [];
        private readonly List<ModPatchInfo> _registeredPatches = [];

        /// <summary>
        ///     Harmony instance id passed to the constructor.
        ///     传入构造函数的 Harmony 实例 id。
        /// </summary>
        public string PatcherId => patcherId;

        /// <summary>
        ///     Human-readable patcher label for logs.
        ///     用于日志的人类可读补丁器标签。
        /// </summary>
        public string PatcherName => patcherName;

        /// <summary>
        ///     Logger associated with this patcher.
        ///     与此补丁器关联的日志器。
        /// </summary>
        public Logger Logger => logger;

        /// <summary>
        ///     Count of registered static <see cref="ModPatchInfo" /> entries.
        ///     已注册静态 <see cref="ModPatchInfo" /> 条目的数量。
        /// </summary>
        public int RegisteredPatchCount => _registeredPatches.Count;

        /// <summary>
        ///     Count of registered <see cref="DynamicPatchInfo" /> entries.
        ///     已注册 <see cref="DynamicPatchInfo" /> 条目的数量。
        /// </summary>
        public int RegisteredDynamicPatchCount => _registeredDynamicPatches.Count;

        /// <summary>
        ///     Number of patches currently marked applied in internal state.
        ///     内部状态中当前标记为已应用的补丁数量。
        /// </summary>
        public int AppliedPatchCount => _patchedStatus.Count(kvp => kvp.Value);

        /// <summary>
        ///     Snapshot of static patch registrations.
        ///     静态补丁注册的快照。
        /// </summary>
        public IReadOnlyList<ModPatchInfo> RegisteredPatches => _registeredPatches;

        /// <summary>
        ///     True after <see cref="PatchAll" /> succeeds without rolling back.
        ///     <see cref="PatchAll" /> 成功且未回滚后为 True。
        /// </summary>
        public bool IsApplied { get; private set; }

        /// <summary>
        ///     Queues a static patch; throws if <see cref="IsApplied" /> is already true.
        ///     将静态 patch 加入队列；如果 <see cref="IsApplied" /> 已为 true，则抛出异常。
        /// </summary>
        public void RegisterPatch(ModPatchInfo modPatchInfo)
        {
            if (IsApplied)
            {
                logger.Error(
                    $"{_logPrefix}Cannot register patch '{modPatchInfo.Id}': Patches have already been applied");
                throw new InvalidOperationException("Cannot register patches after they have been applied");
            }

            if (_registeredPatches.Any(p => p.Id == modPatchInfo.Id))
            {
                logger.Warn($"{_logPrefix}Patch '{modPatchInfo.Id}' already registered, skipping duplicate");
                return;
            }

            ValidatePatchType(modPatchInfo);
            PatchLog.Bind(modPatchInfo.PatchType, logger);

            _registeredPatches.Add(modPatchInfo);
            logger.Debug($"{_logPrefix}Registered patch: {modPatchInfo.Id} - {modPatchInfo.Description}");
        }

        /// <summary>
        ///     Calls <see cref="RegisterPatch" /> for each entry in <paramref name="patches" />.
        ///     调用 <see cref="RegisterPatch" />，针对 <paramref name="patches" /> 中的每个条目。
        /// </summary>
        public void RegisterPatches(params ReadOnlySpan<ModPatchInfo> patches)
        {
            foreach (var patch in patches) RegisterPatch(patch);
        }

        /// <summary>
        ///     Queues a dynamic patch (resolved <see cref="MethodBase" /> + Harmony methods).
        ///     将动态 patch（已解析的 <see cref="MethodBase" /> + Harmony 方法）加入队列。
        /// </summary>
        public void RegisterDynamicPatch(DynamicPatchInfo dynamicPatchInfo)
        {
            ArgumentNullException.ThrowIfNull(dynamicPatchInfo);

            if (_registeredDynamicPatches.Any(p => p.Id == dynamicPatchInfo.Id))
            {
                logger.Warn(
                    $"{_logPrefix}Dynamic patch '{dynamicPatchInfo.Id}' already registered, skipping duplicate");
                return;
            }

            _registeredDynamicPatches.Add(dynamicPatchInfo);
            logger.Debug(
                $"{_logPrefix}Registered dynamic patch: {dynamicPatchInfo.Id} - {dynamicPatchInfo.Description}");
        }

        /// <summary>
        ///     Calls <see cref="RegisterDynamicPatch" /> for each entry.
        ///     对每个条目调用 <see cref="RegisterDynamicPatch" />。
        /// </summary>
        public void RegisterDynamicPatches(params ReadOnlySpan<DynamicPatchInfo> dynamicPatches)
        {
            foreach (var patch in dynamicPatches) RegisterDynamicPatch(patch);
        }

        /// <summary>
        ///     Registers and immediately applies dynamic patches; optionally rolls back all Harmony patches on critical
        ///     failure.
        ///     注册并立即应用动态补丁；可选地在关键失败时回滚所有 Harmony 补丁。
        /// </summary>
        /// <returns>
        ///     False when any critical patch fails and rollback was requested or needed.
        ///     当任何关键补丁失败且请求或需要回滚时返回 false。
        /// </returns>
        public bool ApplyDynamicPatches(IEnumerable<DynamicPatchInfo> dynamicPatches,
            bool rollbackOnCriticalFailure = false)
        {
            ArgumentNullException.ThrowIfNull(dynamicPatches);

            var patches = dynamicPatches.ToArray();
            if (patches.Length == 0)
                return true;

            RegisterDynamicPatches(patches);

            logger.Info($"{_logPrefix}Applying {patches.Length} dynamic patch(es)...");

            var successCount = 0;
            var failureCount = 0;
            var criticalFailureCount = 0;

            foreach (var patch in patches)
            {
                logger.Debug(
                    $"{_logPrefix}[{(patch.IsCritical ? "Critical" : "Optional")}] {patch.Id} - Begin");
                var (success, errorMessage, exception) = ApplyDynamicPatch(patch);

                if (success)
                {
                    successCount++;
                    logger.Debug(
                        $"{_logPrefix}[{(patch.IsCritical ? "Critical" : "Optional")}] {patch.Id} - Success ✓");
                    continue;
                }

                failureCount++;
                if (patch.IsCritical)
                    criticalFailureCount++;

                var sb = new StringBuilder();
                sb.AppendLine($"{_logPrefix}[{(patch.IsCritical ? "Critical" : "Optional")}] {patch.Id} - Failed ✗");
                if (exception != null)
                    sb.Append($"Exception: {exception}");
                else
                    sb.Append($"Error: {errorMessage}");
                logger.Error(sb.ToString());
            }

            logger.Info(
                $"{_logPrefix}Dynamic patch application complete: {successCount}/{patches.Length} succeeded");

            if (failureCount > 0)
                logger.ErrorNoTrace(
                    criticalFailureCount > 0
                        ? $"{_logPrefix}{failureCount} dynamic patch(es) failed, including {criticalFailureCount} critical failure(s)"
                        : $"{_logPrefix}{failureCount} dynamic patch(es) failed, but no critical failures");

            if (criticalFailureCount == 0)
                return true;

            if (rollbackOnCriticalFailure)
                UnpatchAll();

            return false;
        }

        /// <summary>
        ///     Applies all registered static patches once; on critical failure calls <see cref="UnpatchAll" />.
        ///     应用所有已注册静态 patch 一次；发生关键失败时调用 <see cref="UnpatchAll" />。
        /// </summary>
        /// <returns>
        ///     True when no critical patch failed.
        ///     没有关键补丁失败时返回 true。
        /// </returns>
        public bool PatchAll()
        {
            if (IsApplied)
            {
                logger.Warn($"{_logPrefix}Patches have already been applied, skipping");
                return true;
            }

            logger.Info($"{_logPrefix}Applying {_registeredPatches.Count} patches...");
            var results = new ModPatchResult[_registeredPatches.Count];
            for (var i = 0; i < _registeredPatches.Count; i++)
                results[i] = ApplyPatch(_registeredPatches[i]);
            var success = ProcessPatchResults(results);
            var ignoredCount = results.Count(result => result.Ignored);
            var failedCount = results.Count(result => !result.Success);

            if (success)
            {
                IsApplied = true;
                if (ignoredCount == 0 && failedCount == 0)
                    logger.Info($"{_logPrefix}All patches applied successfully");
                else if (failedCount == 0)
                    logger.Info(
                        $"{_logPrefix}All required patches applied; {ignoredCount} optional patch target(s) were ignored");
                else
                    logger.ErrorNoTrace(
                        $"{_logPrefix}Critical patches succeeded, but some optional patches failed to apply");
            }
            else
            {
                logger.Error($"{_logPrefix}Critical patch(es) failed, rolling back all patches...");
                UnpatchAll();
                IsApplied = false;
            }

            return success;
        }

        /// <summary>
        ///     Applies additional static patches after <see cref="PatchAll" /> (e.g. Android hosts that must wait until
        ///     <c>ModelDb.Init</c> completes). Failures are logged; optional patches do not throw.
        ///     在 <see cref="PatchAll" /> 之后应用额外静态 patch（例如必须等到
        ///     <c>ModelDb.Init</c> 完成的 Android 主机）。失败会记录日志；可选 patch 不会抛出。
        /// </summary>
        public void ApplyLateStaticPatches(ReadOnlySpan<ModPatchInfo> patches)
        {
            if (!IsApplied)
                throw new InvalidOperationException(
                    $"{nameof(PatchAll)} must complete before applying late static patches.");

            foreach (var modPatchInfo in patches)
            {
                if (_patchedStatus.GetValueOrDefault(modPatchInfo.Id, false))
                    continue;

                var result = ApplyPatch(modPatchInfo);
                if (result.Success)
                    continue;

                var importance = modPatchInfo.IsCritical ? "Critical" : "Optional";
                if (modPatchInfo.IsCritical)
                    logger.Error(
                        $"{_logPrefix}[Late][{importance}] {modPatchInfo.Id} failed: {result.ErrorMessage}");
                else
                    logger.ErrorNoTrace(
                        $"{_logPrefix}[Late][{importance}] {modPatchInfo.Id} failed: {result.ErrorMessage}");
            }
        }

        /// <summary>
        ///     Removes all applied patches tracked by this instance from the underlying Harmony id.
        ///     从底层 Harmony id 移除此实例跟踪的所有已应用补丁。
        /// </summary>
        public void UnpatchAll()
        {
            if (_registeredPatches.Count == 0 && _registeredDynamicPatches.Count == 0)
            {
                logger.Debug($"{_logPrefix}No patches registered, skipping unpatch");
                return;
            }

            var appliedCount =
                _registeredPatches.Count(patchInfo => _patchedStatus.GetValueOrDefault(patchInfo.Id, false)) +
                _registeredDynamicPatches.Count(patchInfo => _patchedStatus.GetValueOrDefault(patchInfo.Id, false));

            if (appliedCount == 0)
            {
                logger.Debug($"{_logPrefix}No patches applied, skipping unpatch");
                IsApplied = false;
                return;
            }

            logger.Info($"{_logPrefix}Removing {appliedCount} applied patches...");

            foreach (var patchInfo in _registeredPatches.Where(patchInfo =>
                         _patchedStatus.GetValueOrDefault(patchInfo.Id, false)))
                try
                {
                    var originalMethod = GetOriginalMethod(patchInfo);
                    if (originalMethod == null) continue;
                    _harmony.Unpatch(originalMethod, HarmonyPatchType.All, _harmony.Id);
                    _patchedStatus[patchInfo.Id] = false;
                    logger.Info($"{_logPrefix}✓ Removed patch: {patchInfo.Id}");
                }
                catch (Exception ex)
                {
                    logger.Error($"{_logPrefix}✗ Failed to remove patch: {patchInfo.Id} - {ex.Message}");
                }

            foreach (var patchInfo in _registeredDynamicPatches.Where(patchInfo =>
                         _patchedStatus.GetValueOrDefault(patchInfo.Id, false)))
                try
                {
                    _harmony.Unpatch(patchInfo.OriginalMethod, HarmonyPatchType.All, _harmony.Id);
                    _patchedStatus[patchInfo.Id] = false;
                    logger.Info($"{_logPrefix}✓ Removed dynamic patch: {patchInfo.Id}");
                }
                catch (Exception ex)
                {
                    logger.Error($"{_logPrefix}✗ Failed to remove dynamic patch: {patchInfo.Id} - {ex.Message}");
                }

            IsApplied = false;
            logger.Info($"{_logPrefix}All patches removed");
        }

        private ModPatchResult ApplyPatch(ModPatchInfo modPatchInfo)
        {
            logger.Debug(
                $"{_logPrefix}[{(modPatchInfo.IsCritical ? "Critical" : "Optional")}] {modPatchInfo.Id} - Begin");
            try
            {
                var originalMethod = GetOriginalMethod(modPatchInfo);
                if (originalMethod == null)
                {
                    _patchedStatus[modPatchInfo.Id] = false;
                    if (modPatchInfo.IgnoreIfTargetMissing)
                        return ModPatchResult.CreateIgnored(
                            modPatchInfo,
                            $"Target method not found but patch is marked ignorable: {modPatchInfo.TargetType.Name}.{modPatchInfo.MethodName}");

                    return ModPatchResult.CreateFailure(
                        modPatchInfo,
                        $"Target method not found: {modPatchInfo.TargetType.Name}.{modPatchInfo.MethodName}"
                    );
                }

                var prefix = GetPatchMethod(modPatchInfo.PatchType, "Prefix");
                var postfix = GetPatchMethod(modPatchInfo.PatchType, "Postfix");
                var transpiler = GetPatchMethod(modPatchInfo.PatchType, "Transpiler");
                var finalizer = GetPatchMethod(modPatchInfo.PatchType, "Finalizer");

                if (prefix == null && postfix == null && transpiler == null && finalizer == null)
                {
                    _patchedStatus[modPatchInfo.Id] = false;
                    return ModPatchResult.CreateFailure(
                        modPatchInfo,
                        $"No valid patch methods found in {modPatchInfo.PatchType.Name}"
                    );
                }

                _harmony.Patch(
                    originalMethod,
                    prefix != null ? new HarmonyMethod(prefix) : null,
                    postfix != null ? new HarmonyMethod(postfix) : null,
                    transpiler != null ? new HarmonyMethod(transpiler) : null,
                    finalizer != null ? new HarmonyMethod(finalizer) : null
                );

                _patchedStatus[modPatchInfo.Id] = true;
                logger.Debug(
                    $"{_logPrefix}[{(modPatchInfo.IsCritical ? "Critical" : "Optional")}] {modPatchInfo.Id} - Success ✓");
                return ModPatchResult.CreateSuccess(modPatchInfo);
            }
            catch (Exception ex)
            {
                _patchedStatus[modPatchInfo.Id] = false;
                return ModPatchResult.CreateFailure(modPatchInfo, ex.Message, ex);
            }
        }

        private (bool Success, string ErrorMessage, Exception? Exception) ApplyDynamicPatch(
            DynamicPatchInfo dynamicPatchInfo)
        {
            try
            {
                if (!dynamicPatchInfo.HasPatchMethods)
                {
                    _patchedStatus[dynamicPatchInfo.Id] = false;
                    return (false, $"No valid patch methods found for dynamic patch '{dynamicPatchInfo.Id}'", null);
                }

                _harmony.Patch(
                    dynamicPatchInfo.OriginalMethod,
                    dynamicPatchInfo.Prefix,
                    dynamicPatchInfo.Postfix,
                    dynamicPatchInfo.Transpiler,
                    dynamicPatchInfo.Finalizer);

                _patchedStatus[dynamicPatchInfo.Id] = true;
                logger.Debug(
                    $"{_logPrefix}[{(dynamicPatchInfo.IsCritical ? "Critical" : "Optional")}] {dynamicPatchInfo.Id} - Success ✓");
                return (true, string.Empty, null);
            }
            catch (Exception ex)
            {
                _patchedStatus[dynamicPatchInfo.Id] = false;
                return (false, ex.Message, ex);
            }
        }

        private bool ProcessPatchResults(ReadOnlySpan<ModPatchResult> results)
        {
            var successCount = 0;
            var ignoredCount = 0;
            var failureCount = 0;
            var criticalFailureCount = 0;

            var sortedResults = results.ToArray()
                .OrderBy(r => r.Success)
                .ThenByDescending(r => r.ModPatchInfo.IsCritical)
                .ThenBy(r => r.ModPatchInfo.Id);

            foreach (var result in sortedResults)
            {
                var importance = result.ModPatchInfo.IsCritical ? "Critical" : "Optional";

                if (result.Success)
                {
                    successCount++;
                    if (result.Ignored)
                        ignoredCount++;

                    logger.Debug(result.Ignored
                        ? $"{_logPrefix}[{importance}] {result.ModPatchInfo.Id} - Ignored (target missing)"
                        : $"{_logPrefix}[{importance}] {result.ModPatchInfo.Id} - Success ✓");
                }
                else
                {
                    failureCount++;
                    if (result.ModPatchInfo.IsCritical)
                        criticalFailureCount++;

                    var failureLog = new StringBuilder();
                    failureLog.AppendLine($"{_logPrefix}[{importance}] {result.ModPatchInfo.Id} - Failed ✗");
                    failureLog.AppendLine($"{_logPrefix}  Description: {result.ModPatchInfo.Description}");
                    failureLog.AppendLine($"{_logPrefix}  Error: {result.ErrorMessage}");
                    if (result.Exception != null)
                        failureLog.Append($"{_logPrefix}  Exception: {result.Exception}");
                    logger.Error(failureLog.ToString());
                }
            }

            logger.Info(
                $"{_logPrefix}Patch application complete: {successCount - ignoredCount} applied, {ignoredCount} ignored, {failureCount} failed, {results.Length} total");

            if (failureCount > 0) logger.ErrorNoTrace($"{_logPrefix}{failureCount} patch(es) failed");

            if (criticalFailureCount == 0) return true;
            logger.Error($"{_logPrefix}{criticalFailureCount} critical patch(es) failed, mod loading blocked");
            return false;
        }

        private static MethodBase? GetOriginalMethod(ModPatchInfo modPatchInfo)
        {
            return PatchTargetMethodResolver.Resolve(modPatchInfo);
        }

        private static MethodInfo? GetPatchMethod(Type patchType, string methodName)
        {
            return patchType.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );
        }

        /// <summary>
        ///     Validate that patch type implements IPatchMethod interface (optional but recommended)
        ///     验证 patch 类型是否实现 IPatchMethod 接口（可选但推荐）
        /// </summary>
        private void ValidatePatchType(ModPatchInfo modPatchInfo)
        {
            var patchType = modPatchInfo.PatchType;
            var implementsIPatchMethod = patchType.GetInterfaces()
                .Any(i => i.Name == nameof(IPatchMethod) ||
                          (i.IsGenericType && i.GetGenericTypeDefinition().GetInterfaces()
                              .Any(gi => gi.Name == nameof(IPatchMethod))));

            if (!implementsIPatchMethod)
                logger.Warn(
                    $"{_logPrefix}Patch type '{patchType.Name}' does not implement IPatchMethod interface. " +
                    "Consider implementing IPatchMethod interfaces for better type safety and IDE support.");
        }
    }
}
