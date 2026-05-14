namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     Interface for patch classes that can generate their own ModPatchInfo
    ///     Supports patching multiple targets with the same logic
    ///     用于可生成自身 ModPatchInfo 的 patch 类的接口
    ///     支持用相同逻辑 patch 多个目标
    /// </summary>
    public interface IPatchMethod
    {
        /// <summary>
        ///     The unique identifier prefix for this patch
        ///     此 patch 的唯一标识符前缀
        /// </summary>
        static abstract string PatchId { get; }

        /// <summary>
        ///     Whether this patch is critical (default: true)
        ///     此 patch 是否关键（默认：true）
        /// </summary>
        static virtual bool IsCritical => true;

        /// <summary>
        ///     Description of what this patch does
        ///     此 patch 作用的描述
        /// </summary>
        static virtual string Description => "Patch";

        /// <summary>
        ///     Get all patch targets (Type + MethodName combinations)
        ///     获取所有 patch 目标（Type + MethodName 组合）。
        /// </summary>
        static abstract ModPatchTarget[] GetTargets();

        /// <summary>
        ///     Create ModPatchInfo array for all targets.
        ///     为所有目标创建 ModPatchInfo 数组。
        /// </summary>
        /// <remarks>
        ///     When <see cref="GetTargets" /> lists more than one entry with the same <see cref="ModPatchTarget.TargetType" />
        ///     and <see cref="ModPatchTarget.MethodName" /> (e.g. multiple <c>.ctor</c> overloads), the generated
        ///     <see cref="ModPatchInfo.Id" /> appends <c>__1</c>, <c>__2</c>, … in source order so
        ///     <see cref="Patching.Core.ModPatcher.RegisterPatch(ModPatchInfo)" /> does not treat later rows as duplicates.
        ///     当 <see cref="GetTargets" /> 列出多个具有相同 <see cref="ModPatchTarget.TargetType" />
        ///     和 <see cref="ModPatchTarget.MethodName" /> 的条目（例如多个 <c>.ctor</c> 重载）时，生成的
        ///     <see cref="ModPatchInfo.Id" /> 会按源顺序追加 <c>__1</c>、<c>__2</c>、…，使
        ///     <see cref="Patching.Core.ModPatcher.RegisterPatch(ModPatchInfo)" /> 不会把后续行视为重复项。
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
