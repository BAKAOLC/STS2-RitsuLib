using System.Reflection;
using System.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Patching.Core
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Owns one Harmony instance and manages the registration, application, and removal of static and dynamic patches.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         持有一个 Harmony 实例，并管理静态和动态补丁的注册、应用与移除。
    ///     </para>
    /// </summary>
    /// <param name="patcherId">
    ///     <para xml:lang="en">Harmony ID, which must be unique for each logical patcher.</para>
    ///     <para xml:lang="zh-CN">Harmony ID；每个逻辑补丁器必须使用唯一值。</para>
    /// </param>
    /// <param name="logger">
    ///     <para xml:lang="en">Logger used for patch diagnostics.</para>
    ///     <para xml:lang="zh-CN">用于记录补丁诊断信息的日志器。</para>
    /// </param>
    /// <param name="patcherName">
    ///     <para xml:lang="en">Optional display name included in the log prefix.</para>
    ///     <para xml:lang="zh-CN">包含在日志前缀中的可选显示名称。</para>
    /// </param>
    public class ModPatcher(string patcherId, Logger logger, string patcherName = "")
    {
        private readonly Dictionary<string, IDisposable> _dynamicPatchLifetimeLeases = [];
        private readonly Harmony _harmony = new(patcherId);

        private readonly string _logPrefix =
            string.IsNullOrEmpty(patcherName) ? "[Patcher] " : $"[Patcher - {patcherName}] ";

        private readonly Dictionary<string, bool> _patchedStatus = [];
        private readonly List<DynamicPatchInfo> _registeredDynamicPatches = [];
        private readonly List<ModPatchInfo> _registeredPatches = [];

        /// <summary>
        ///     <para xml:lang="en">Gets the Harmony ID supplied to the constructor.</para>
        ///     <para xml:lang="zh-CN">获取传入构造函数的 Harmony ID。</para>
        /// </summary>
        public string PatcherId => patcherId;

        /// <summary>
        ///     <para xml:lang="en">Gets the patcher's display name.</para>
        ///     <para xml:lang="zh-CN">获取补丁器的显示名称。</para>
        /// </summary>
        public string PatcherName => patcherName;

        /// <summary>
        ///     <para xml:lang="en">Gets the logger associated with this patcher.</para>
        ///     <para xml:lang="zh-CN">获取与此补丁器关联的日志器。</para>
        /// </summary>
        public Logger Logger => logger;

        /// <summary>
        ///     <para xml:lang="en">Gets the number of registered static patches.</para>
        ///     <para xml:lang="zh-CN">获取已注册静态补丁的数量。</para>
        /// </summary>
        public int RegisteredPatchCount => _registeredPatches.Count;

        /// <summary>
        ///     <para xml:lang="en">Gets the number of registered dynamic patches.</para>
        ///     <para xml:lang="zh-CN">获取已注册动态补丁的数量。</para>
        /// </summary>
        public int RegisteredDynamicPatchCount => _registeredDynamicPatches.Count;

        /// <summary>
        ///     <para xml:lang="en">Gets the number of patches currently marked as applied.</para>
        ///     <para xml:lang="zh-CN">获取当前标记为已应用的补丁数量。</para>
        /// </summary>
        public int AppliedPatchCount => _patchedStatus.Count(kvp => kvp.Value);

        /// <summary>
        ///     <para xml:lang="en">Gets the registered static patches.</para>
        ///     <para xml:lang="zh-CN">获取已注册的静态补丁。</para>
        /// </summary>
        public IReadOnlyList<ModPatchInfo> RegisteredPatches => _registeredPatches;

        /// <summary>
        ///     <para xml:lang="en">Gets whether <see cref="PatchAll" /> completed without a critical failure.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="PatchAll" /> 是否已完成且未发生严重失败。</para>
        /// </summary>
        public bool IsApplied { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a static patch. Duplicate IDs are skipped; registration after <see cref="PatchAll" />
        ///         succeeds throws <see cref="InvalidOperationException" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册静态补丁。重复的 ID 会被跳过；在 <see cref="PatchAll" /> 成功后注册会抛出
        ///         <see cref="InvalidOperationException" />。
        ///     </para>
        /// </summary>
        public void RegisterPatch(ModPatchInfo modPatchInfo)
        {
            if (IsApplied)
            {
                logger.ErrorNoTrace(
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
        ///     <para xml:lang="en">Registers each patch in <paramref name="patches" />.</para>
        ///     <para xml:lang="zh-CN">注册 <paramref name="patches" /> 中的每个补丁。</para>
        /// </summary>
        public void RegisterPatches(params ReadOnlySpan<ModPatchInfo> patches)
        {
            foreach (var patch in patches) RegisterPatch(patch);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a dynamic patch. Duplicate IDs are skipped.</para>
        ///     <para xml:lang="zh-CN">注册动态补丁。重复的 ID 会被跳过。</para>
        /// </summary>
        public void RegisterDynamicPatch(DynamicPatchInfo dynamicPatchInfo)
        {
            ArgumentNullException.ThrowIfNull(dynamicPatchInfo);
            TryRegisterDynamicPatch(dynamicPatchInfo);
        }

        private bool TryRegisterDynamicPatch(DynamicPatchInfo dynamicPatchInfo)
        {
            ArgumentNullException.ThrowIfNull(dynamicPatchInfo);

            if (_registeredDynamicPatches.Any(p => p.Id == dynamicPatchInfo.Id))
            {
                logger.Warn(
                    $"{_logPrefix}Dynamic patch '{dynamicPatchInfo.Id}' already registered, skipping duplicate");
                return false;
            }

            _registeredDynamicPatches.Add(dynamicPatchInfo);
            logger.Debug(
                $"{_logPrefix}Registered dynamic patch: {dynamicPatchInfo.Id} - {dynamicPatchInfo.Description}");
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Registers each patch in <paramref name="dynamicPatches" />.</para>
        ///     <para xml:lang="zh-CN">注册 <paramref name="dynamicPatches" /> 中的每个补丁。</para>
        /// </summary>
        public void RegisterDynamicPatches(params ReadOnlySpan<DynamicPatchInfo> dynamicPatches)
        {
            foreach (var patch in dynamicPatches) RegisterDynamicPatch(patch);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers and immediately applies previously unregistered dynamic patch IDs. Duplicate IDs are
        ///         skipped. When a critical patch fails and <paramref name="rollbackOnCriticalFailure" /> is
        ///         <see langword="true" />, attempts to remove all patches owned by this patcher.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册并立即应用此前未注册 ID 的动态补丁，重复 ID 会被跳过。严重补丁失败且
        ///         <paramref name="rollbackOnCriticalFailure" /> 为 <see langword="true" /> 时，尝试移除此
        ///         补丁器拥有的所有补丁。
        ///     </para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="false" /> when any critical patch fails; otherwise <see langword="true" />.</para>
        ///     <para xml:lang="zh-CN">任何严重补丁失败时为 <see langword="false" />；否则为 <see langword="true" />。</para>
        /// </returns>
        public bool ApplyDynamicPatches(IEnumerable<DynamicPatchInfo> dynamicPatches,
            bool rollbackOnCriticalFailure = false)
        {
            ArgumentNullException.ThrowIfNull(dynamicPatches);

            var candidates = dynamicPatches.ToArray();
            if (candidates.Length == 0)
                return true;

            var patches = candidates.Where(TryRegisterDynamicPatch).ToArray();
            if (patches.Length == 0)
                return true;

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
                logger.ErrorNoTrace(sb.ToString());
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
        ///     <para xml:lang="en">
        ///         Applies all registered static patches once. If a critical patch fails, calls <see cref="UnpatchAll" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         应用所有已注册的静态补丁一次。严重补丁失败时调用 <see cref="UnpatchAll" />。
        ///     </para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when no critical patch fails; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">没有严重补丁失败时为 <see langword="true" />；否则为 <see langword="false" />。</para>
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
                logger.ErrorNoTrace($"{_logPrefix}Critical patch(es) failed, rolling back all patches...");
                UnpatchAll();
                IsApplied = false;
            }

            return success;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies additional static patches after <see cref="PatchAll" />, such as patches that must wait for
        ///         <c>ModelDb.Init</c> on Android. Individual failures are logged.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <see cref="PatchAll" /> 之后应用额外的静态补丁，例如 Android 上必须等待
        ///         <c>ModelDb.Init</c> 的补丁。单个补丁的失败会记录到日志。
        ///     </para>
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
                logger.ErrorNoTrace(
                    $"{_logPrefix}[Late][{importance}] {modPatchInfo.Id} failed: {result.ErrorMessage}");
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Removes matching Harmony patches owned by another Harmony ID from
        ///         <paramref name="originalMethod" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从 <paramref name="originalMethod" /> 移除由另一个 Harmony ID 拥有且符合条件的补丁。
        ///     </para>
        /// </summary>
        /// <param name="originalMethod">
        ///     <para xml:lang="en">Original method whose patch list is inspected.</para>
        ///     <para xml:lang="zh-CN">要检查补丁列表的原始方法。</para>
        /// </param>
        /// <param name="owner">
        ///     <para xml:lang="en">Harmony owner ID to remove.</para>
        ///     <para xml:lang="zh-CN">要移除的 Harmony 所有者 ID。</para>
        /// </param>
        /// <param name="patchDeclaringType">
        ///     <para xml:lang="en">Optional declaring-type filter for patch methods.</para>
        ///     <para xml:lang="zh-CN">可选的补丁方法声明类型筛选条件。</para>
        /// </param>
        /// <param name="patchMethodName">
        ///     <para xml:lang="en">Optional patch-method name filter.</para>
        ///     <para xml:lang="zh-CN">可选的补丁方法名称筛选条件。</para>
        /// </param>
        /// <param name="patchType">
        ///     <para xml:lang="en">Harmony patch kind to inspect; <see cref="HarmonyPatchType.All" /> inspects every kind.</para>
        ///     <para xml:lang="zh-CN">要检查的 Harmony 补丁类型；<see cref="HarmonyPatchType.All" /> 表示检查所有类型。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The number of patch methods removed.</para>
        ///     <para xml:lang="zh-CN">已移除的补丁方法数量。</para>
        /// </returns>
        public int UnpatchExternalPatches(
            MethodBase originalMethod,
            string owner,
            Type? patchDeclaringType = null,
            string? patchMethodName = null,
            HarmonyPatchType patchType = HarmonyPatchType.All)
        {
            ArgumentNullException.ThrowIfNull(originalMethod);
            ArgumentException.ThrowIfNullOrWhiteSpace(owner);

            var patchInfo = Harmony.GetPatchInfo(originalMethod);
            if (patchInfo == null)
            {
                logger.Info($"{_logPrefix}No Harmony patches found on {FormatMethod(originalMethod)}");
                return 0;
            }

            var patches = EnumeratePatches(patchInfo, patchType)
                .Where(patch => MatchesPatch(patch, owner, patchDeclaringType, patchMethodName))
                .ToArray();

            foreach (var patch in patches) _harmony.Unpatch(originalMethod, patch.PatchMethod);

            logger.Info(
                $"{_logPrefix}Removed {patches.Length} external patch(es) from {FormatMethod(originalMethod)} owned by '{owner}'");
            return patches.Length;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves <paramref name="target" /> and removes matching external Harmony patches. A missing target
        ///         can be ignored.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析 <paramref name="target" /> 并移除符合条件的外部 Harmony 补丁。缺失的目标可被忽略。
        ///     </para>
        /// </summary>
        public int UnpatchExternalPatches(
            ModPatchTarget target,
            string owner,
            Type? patchDeclaringType = null,
            string? patchMethodName = null,
            HarmonyPatchType patchType = HarmonyPatchType.All,
            bool ignoreIfTargetMissing = true)
        {
            ArgumentNullException.ThrowIfNull(target);

            var originalMethod = PatchTargetMethodResolver.Resolve(target);
            if (originalMethod != null)
                return UnpatchExternalPatches(
                    originalMethod,
                    owner,
                    patchDeclaringType,
                    patchMethodName,
                    patchType);

            var message = $"{_logPrefix}External unpatch target not found: {target}";
            if (!ignoreIfTargetMissing && !target.IgnoreIfMissing)
                throw new MissingMethodException(target.TargetType.FullName, target.MethodName);
            logger.Info(message);
            return 0;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to remove every applied patch tracked by this instance from its Harmony ID. Failed
        ///         removals remain marked as applied so a later call can retry them.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试从此实例的 Harmony ID 移除其跟踪的所有已应用补丁。移除失败的补丁会继续标记为已应用，
        ///         以便之后再次调用时重试。
        ///     </para>
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
            var preserveAppliedStateOnFailure = IsApplied;
            var failureCount = 0;

            foreach (var patchInfo in _registeredPatches.Where(patchInfo =>
                         _patchedStatus.GetValueOrDefault(patchInfo.Id, false)))
                try
                {
                    var originalMethod = GetOriginalMethod(patchInfo);
                    if (originalMethod == null)
                    {
                        failureCount++;
                        logger.ErrorNoTrace(
                            $"{_logPrefix}✗ Failed to remove patch: {patchInfo.Id} - original method unavailable");
                        continue;
                    }

                    _harmony.Unpatch(originalMethod, HarmonyPatchType.All, _harmony.Id);
                    _patchedStatus[patchInfo.Id] = false;
                    logger.Debug($"{_logPrefix}✓ Removed patch: {patchInfo.Id}");
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    failureCount++;
                    logger.ErrorNoTrace($"{_logPrefix}✗ Failed to remove patch: {patchInfo.Id} - {ex}");
                }

            foreach (var patchInfo in _registeredDynamicPatches.Where(patchInfo =>
                         _patchedStatus.GetValueOrDefault(patchInfo.Id, false)))
                try
                {
                    _harmony.Unpatch(patchInfo.OriginalMethod, HarmonyPatchType.All, _harmony.Id);
                    ReleaseDynamicPatchLifetime(patchInfo.Id);
                    _patchedStatus[patchInfo.Id] = false;
                    logger.Debug($"{_logPrefix}✓ Removed dynamic patch: {patchInfo.Id}");
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    failureCount++;
                    logger.ErrorNoTrace($"{_logPrefix}✗ Failed to remove dynamic patch: {patchInfo.Id} - {ex}");
                }

            IsApplied = failureCount > 0 && preserveAppliedStateOnFailure;
            if (failureCount == 0)
                logger.Info($"{_logPrefix}All patches removed");
            else
                logger.ErrorNoTrace(
                    $"{_logPrefix}Patch removal incomplete: {failureCount} patch(es) remain available for retry");
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
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                _patchedStatus[modPatchInfo.Id] = false;
                return ModPatchResult.CreateFailure(modPatchInfo, ex.Message, ex);
            }
        }

        private (bool Success, string ErrorMessage, Exception? Exception) ApplyDynamicPatch(
            DynamicPatchInfo dynamicPatchInfo)
        {
            IDisposable? lifetimeLease = null;
            var lifetimeStored = false;
            try
            {
                if (!dynamicPatchInfo.HasPatchMethods)
                {
                    _patchedStatus[dynamicPatchInfo.Id] = false;
                    return (false, $"No valid patch methods found for dynamic patch '{dynamicPatchInfo.Id}'", null);
                }

                lifetimeLease = dynamicPatchInfo.AcquireLifetimeLease();
                if (lifetimeLease != null)
                {
                    _dynamicPatchLifetimeLeases.Add(dynamicPatchInfo.Id, lifetimeLease);
                    lifetimeStored = true;
                }

                _harmony.Patch(
                    dynamicPatchInfo.OriginalMethod,
                    dynamicPatchInfo.Prefix,
                    dynamicPatchInfo.Postfix,
                    dynamicPatchInfo.Transpiler,
                    dynamicPatchInfo.Finalizer);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                if (lifetimeStored &&
                    _dynamicPatchLifetimeLeases.Remove(dynamicPatchInfo.Id, out var storedLifetimeLease))
                    storedLifetimeLease.Dispose();
                else
                    lifetimeLease?.Dispose();

                _patchedStatus[dynamicPatchInfo.Id] = false;
                return (false, ex.Message, ex);
            }

            _patchedStatus[dynamicPatchInfo.Id] = true;
            logger.Debug(
                $"{_logPrefix}[{(dynamicPatchInfo.IsCritical ? "Critical" : "Optional")}] {dynamicPatchInfo.Id} - Success ✓");
            return (true, string.Empty, null);
        }

        private void ReleaseDynamicPatchLifetime(string patchId)
        {
            if (_dynamicPatchLifetimeLeases.Remove(patchId, out var lifetimeLease))
                lifetimeLease.Dispose();
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

                    if (result.Ignored)
                        logger.Info(
                            $"{_logPrefix}[{importance}] {result.ModPatchInfo.Id} - Ignored: {result.ErrorMessage}");
                    else
                        logger.Debug($"{_logPrefix}[{importance}] {result.ModPatchInfo.Id} - Success ✓");
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
                    logger.ErrorNoTrace(failureLog.ToString());
                }
            }

            logger.Info(
                $"{_logPrefix}Patch application complete: {successCount - ignoredCount} applied, {ignoredCount} ignored, {failureCount} failed, {results.Length} total");

            if (failureCount > 0) logger.ErrorNoTrace($"{_logPrefix}{failureCount} patch(es) failed");

            if (criticalFailureCount == 0) return true;
            logger.ErrorNoTrace($"{_logPrefix}{criticalFailureCount} critical patch(es) failed, mod loading blocked");
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

        private static IEnumerable<Patch> EnumeratePatches(Patches patchInfo, HarmonyPatchType patchType)
        {
            if (patchType is HarmonyPatchType.All or HarmonyPatchType.Prefix)
                foreach (var patch in patchInfo.Prefixes)
                    yield return patch;

            if (patchType is HarmonyPatchType.All or HarmonyPatchType.Postfix)
                foreach (var patch in patchInfo.Postfixes)
                    yield return patch;

            if (patchType is HarmonyPatchType.All or HarmonyPatchType.Transpiler)
                foreach (var patch in patchInfo.Transpilers)
                    yield return patch;

            // ReSharper disable once InvertIf
            if (patchType is HarmonyPatchType.All or HarmonyPatchType.Finalizer)
                foreach (var patch in patchInfo.Finalizers)
                    yield return patch;
        }

        private static bool MatchesPatch(
            Patch patch,
            string owner,
            Type? patchDeclaringType,
            string? patchMethodName)
        {
            if (patch.owner != owner)
                return false;

            if (patchDeclaringType != null && patch.PatchMethod.DeclaringType != patchDeclaringType)
                return false;

            return string.IsNullOrEmpty(patchMethodName) || patch.PatchMethod.Name == patchMethodName;
        }

        private static string FormatMethod(MethodBase method)
        {
            return $"{method.DeclaringType?.FullName}.{method.Name}";
        }

        /// <summary>
        ///     <para xml:lang="en">Warns when a patch type does not implement <see cref="IPatchMethod" />.</para>
        ///     <para xml:lang="zh-CN">补丁类型未实现 <see cref="IPatchMethod" /> 时记录警告。</para>
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
