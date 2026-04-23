namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     Interface for patch classes that can generate their own ModPatchInfo
    ///     Supports patching multiple targets with the same logic
    /// </summary>
    public interface IPatchMethod
    {
        /// <summary>
        ///     The unique identifier prefix for this patch
        /// </summary>
        static abstract string PatchId { get; }

        /// <summary>
        ///     Whether this patch is critical (default: true)
        /// </summary>
        static virtual bool IsCritical => true;

        /// <summary>
        ///     Description of what this patch does
        /// </summary>
        static virtual string Description => "Patch";

        /// <summary>
        ///     Get all patch targets (Type + MethodName combinations)
        /// </summary>
        static abstract ModPatchTarget[] GetTargets();

        /// <summary>
        ///     Create ModPatchInfo array for all targets
        /// </summary>
        static ModPatchInfo[] CreatePatchInfos<TPatch>() where TPatch : IPatchMethod
        {
            var targets = TPatch.GetTargets();
            var patchInfos = new ModPatchInfo[targets.Length];

            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                var id = targets.Length == 1
                    ? TPatch.PatchId
                    : $"{TPatch.PatchId}_{target.TargetType.Name}_{target.MethodName}";

                patchInfos[i] = new(
                    id,
                    target.TargetType,
                    target.MethodName,
                    typeof(TPatch),
                    TPatch.IsCritical,
                    $"{TPatch.Description} -> {target}",
                    target.ParameterTypes,
                    target.IgnoreIfMissing,
                    target.HarmonyMethodType
                );
            }

            return patchInfos;
        }
    }
}
