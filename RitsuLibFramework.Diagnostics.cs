using System.Reflection;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        internal static FrameworkRuntimeSnapshot CaptureRuntimeSnapshot()
        {
            lock (SyncRoot)
            {
                var patchers = Enum.GetValues<FrameworkPatcherArea>()
                    .Select(area =>
                    {
                        if (!FrameworkPatchersByArea.TryGetValue(area, out var patcher))
                            return new(area.ToString(), false, false, 0, 0, 0);

                        return new FrameworkPatcherSnapshot(
                            area.ToString(),
                            true,
                            patcher.IsApplied,
                            patcher.RegisteredPatchCount,
                            patcher.RegisteredDynamicPatchCount,
                            patcher.AppliedPatchCount);
                    })
                    .ToArray();

                return new(
                    IsInitialized,
                    IsActive,
                    _profileServicesInitialized,
                    HasRegisteredModSettings,
                    _lifecycleObservers.Length,
                    RegisteredScriptAssemblies.Count,
                    patchers);
            }
        }

        internal static FrameworkPatchBindingSnapshot[] CapturePatchBindingSnapshot()
        {
            lock (SyncRoot)
            {
                return Enum.GetValues<FrameworkPatcherArea>()
                    .SelectMany(area =>
                    {
                        if (!FrameworkPatchersByArea.TryGetValue(area, out var patcher))
                            return [];

                        return patcher.RegisteredPatches
                            .Select(patchInfo => CreatePatchBindingSnapshot(area, patcher, patchInfo))
                            .ToArray();
                    })
                    .ToArray();
            }
        }

        private static FrameworkPatchBindingSnapshot CreatePatchBindingSnapshot(
            FrameworkPatcherArea area,
            ModPatcher patcher,
            ModPatchInfo patchInfo)
        {
            var originalMethod = ResolveOriginalMethod(patchInfo);
            var expectedPatchMethods = ResolvePatchMethods(patchInfo.PatchType);

            return new(
                area.ToString(),
                patcher.PatcherId,
                patchInfo.Id,
                patchInfo.PatchType.FullName ?? patchInfo.PatchType.Name,
                patchInfo.IsCritical,
                originalMethod,
                expectedPatchMethods);
        }

        private static MethodInfo? ResolveOriginalMethod(ModPatchInfo patchInfo)
        {
            if (patchInfo.ParameterTypes != null)
                return patchInfo.TargetType.GetMethod(
                    patchInfo.MethodName,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    patchInfo.ParameterTypes,
                    null);

            return patchInfo.TargetType.GetMethod(
                patchInfo.MethodName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static MethodInfo[] ResolvePatchMethods(Type patchType)
        {
            return
            [
                .. ResolvePatchMethod(patchType, "Prefix"),
                .. ResolvePatchMethod(patchType, "Postfix"),
                .. ResolvePatchMethod(patchType, "Transpiler"),
                .. ResolvePatchMethod(patchType, "Finalizer"),
            ];
        }

        private static IEnumerable<MethodInfo> ResolvePatchMethod(Type patchType, string methodName)
        {
            var method = patchType.GetMethod(methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return method == null ? [] : [method];
        }
    }

    internal readonly record struct FrameworkRuntimeSnapshot(
        bool IsInitialized,
        bool IsActive,
        bool ProfileServicesInitialized,
        bool HasRegisteredModSettings,
        int LifecycleObserverCount,
        int RegisteredScriptAssemblyCount,
        FrameworkPatcherSnapshot[] PatcherAreas);

    internal readonly record struct FrameworkPatcherSnapshot(
        string AreaName,
        bool IsRegistered,
        bool IsApplied,
        int RegisteredPatchCount,
        int RegisteredDynamicPatchCount,
        int AppliedPatchCount);

    internal readonly record struct FrameworkPatchBindingSnapshot(
        string AreaName,
        string PatcherId,
        string PatchId,
        string PatchTypeName,
        bool IsCritical,
        MethodInfo? OriginalMethod,
        MethodInfo[] ExpectedPatchMethods);
}
