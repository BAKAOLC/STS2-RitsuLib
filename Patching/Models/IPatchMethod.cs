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
        ///     Create ModPatchInfo array for all targets.
        /// </summary>
        /// <remarks>
        ///     When <see cref="GetTargets" /> lists more than one entry with the same <see cref="ModPatchTarget.TargetType" />
        ///     and <see cref="ModPatchTarget.MethodName" /> (e.g. multiple <c>.ctor</c> overloads), the generated
        ///     <see cref="ModPatchInfo.Id" /> appends <c>__1</c>, <c>__2</c>, … in source order so
        ///     <see cref="Patching.Core.ModPatcher.RegisterPatch(ModPatchInfo)" /> does not treat later rows as duplicates.
        /// </remarks>
        static ModPatchInfo[] CreatePatchInfos<TPatch>() where TPatch : IPatchMethod
        {
            var targets = TPatch.GetTargets();
            var patchInfos = new ModPatchInfo[targets.Length];

            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                string id;
                if (targets.Length == 1)
                {
                    id = TPatch.PatchId;
                }
                else
                {
                    var baseId = $"{TPatch.PatchId}_{target.TargetType.Name}_{target.MethodName}";
                    var sameDeclaringAndName = targets.Count(t =>
                        t.TargetType == target.TargetType && t.MethodName == target.MethodName);

                    if (sameDeclaringAndName > 1)
                    {
                        var ordinal = 0;
                        for (var j = 0; j <= i; j++)
                            if (targets[j].TargetType == target.TargetType &&
                                targets[j].MethodName == target.MethodName)
                                ordinal++;

                        id = $"{baseId}__{ordinal}";
                    }
                    else
                    {
                        id = baseId;
                    }
                }

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
