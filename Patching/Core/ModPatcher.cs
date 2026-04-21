using System.Reflection;
using System.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Patching.Core
{
    /// <summary>
    ///     Owns one Harmony instance: registers static and dynamic patches, applies them, and can roll back.
    /// </summary>
    /// <param name="patcherId">Harmony id (must be unique per logical patcher).</param>
    /// <param name="logger">Logger used for patch diagnostics.</param>
    /// <param name="patcherName">Optional display name included in log prefix.</param>
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
        /// </summary>
        public string PatcherId => patcherId;

        /// <summary>
        ///     Human-readable patcher label for logs.
        /// </summary>
        public string PatcherName => patcherName;

        /// <summary>
        ///     Logger associated with this patcher.
        /// </summary>
        public Logger Logger => logger;

        /// <summary>
        ///     Count of registered static <see cref="ModPatchInfo" /> entries.
        /// </summary>
        public int RegisteredPatchCount => _registeredPatches.Count;

        /// <summary>
        ///     Count of registered <see cref="DynamicPatchInfo" /> entries.
        /// </summary>
        public int RegisteredDynamicPatchCount => _registeredDynamicPatches.Count;

        /// <summary>
        ///     Number of patches currently marked applied in internal state.
        /// </summary>
        public int AppliedPatchCount => _patchedStatus.Count(kvp => kvp.Value);

        /// <summary>
        ///     Snapshot of static patch registrations.
        /// </summary>
        public IReadOnlyList<ModPatchInfo> RegisteredPatches => _registeredPatches;

        /// <summary>
        ///     True after <see cref="PatchAll" /> succeeds without rolling back.
        /// </summary>
        public bool IsApplied { get; private set; }

        /// <summary>
        ///     Queues a static patch; throws if <see cref="IsApplied" /> is already true.
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
        /// </summary>
        public void RegisterPatches(params ReadOnlySpan<ModPatchInfo> patches)
        {
            foreach (var patch in patches) RegisterPatch(patch);
        }

        /// <summary>
        ///     Queues a dynamic patch (resolved <see cref="MethodBase" /> + Harmony methods).
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
        /// </summary>
        public void RegisterDynamicPatches(params ReadOnlySpan<DynamicPatchInfo> dynamicPatches)
        {
            foreach (var patch in dynamicPatches) RegisterDynamicPatch(patch);
        }

        /// <summary>
        ///     Registers and immediately applies dynamic patches; optionally rolls back all Harmony patches on critical
        ///     failure.
        /// </summary>
        /// <returns>False when any critical patch fails and rollback was requested or needed.</returns>
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
                var (success, errorMessage, exception) = ApplyDynamicPatch(patch);

                if (success)
                {
                    successCount++;
                    logger.Info($"{_logPrefix}[{(patch.IsCritical ? "Critical" : "Optional")}] {patch.Id} - Success ✓");
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

            logger.Info($"{_logPrefix}Dynamic patch application complete: {successCount}/{patches.Length} succeeded");

            if (failureCount > 0)
                logger.Warn(
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
        /// </summary>
        /// <returns>True when no critical patch failed.</returns>
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
                    logger.Warn(
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
        ///     Removes all applied patches tracked by this instance from the underlying Harmony id.
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

                    logger.Info(result.Ignored
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

            if (failureCount > 0) logger.Warn($"{_logPrefix}{failureCount} patch(es) failed");

            if (criticalFailureCount == 0) return true;
            logger.Error($"{_logPrefix}{criticalFailureCount} critical patch(es) failed, mod loading blocked");
            return false;
        }

        private static MethodInfo? GetOriginalMethod(ModPatchInfo modPatchInfo)
        {
            if (modPatchInfo.ParameterTypes != null)
                return modPatchInfo.TargetType.GetMethod(
                    modPatchInfo.MethodName,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    modPatchInfo.ParameterTypes,
                    null
                );

            return modPatchInfo.TargetType.GetMethod(
                modPatchInfo.MethodName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );
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
