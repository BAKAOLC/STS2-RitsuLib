namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a patch type that can create <see cref="ModPatchInfo" /> instances for one or more targets.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义可为一个或多个目标创建 <see cref="ModPatchInfo" /> 实例的补丁类型。
    ///     </para>
    /// </summary>
    public interface IPatchMethod
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the unique ID prefix for this patch.</para>
        ///     <para xml:lang="zh-CN">获取此补丁的唯一 ID 前缀。</para>
        /// </summary>
        static abstract string PatchId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether failure to apply this patch is critical. The default is <see langword="true" />.</para>
        ///     <para xml:lang="zh-CN">获取补丁应用失败是否属于严重错误。默认值为 <see langword="true" />。</para>
        /// </summary>
        static virtual bool IsCritical => true;

        /// <summary>
        ///     <para xml:lang="en">Gets a description of the patch.</para>
        ///     <para xml:lang="zh-CN">获取补丁的描述。</para>
        /// </summary>
        static virtual string Description => "Patch";

        /// <summary>
        ///     <para xml:lang="en">Gets all targets to which the patch applies.</para>
        ///     <para xml:lang="zh-CN">获取此补丁要应用到的所有目标。</para>
        /// </summary>
        static abstract ModPatchTarget[] GetTargets();

        /// <summary>
        ///     <para xml:lang="en">Creates patch metadata for all targets declared by <typeparamref name="TPatch" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TPatch" /> 声明的所有目标创建补丁元数据。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         When <see cref="GetTargets" /> contains multiple entries with the same
        ///         <see cref="ModPatchTarget.TargetType" /> and <see cref="ModPatchTarget.MethodName" />, such as
        ///         several <c>.ctor</c> overloads, the generated <see cref="ModPatchInfo.Id" /> values receive
        ///         <c>__1</c>, <c>__2</c>, and subsequent suffixes in declaration order. This prevents
        ///         <see cref="Patching.Core.ModPatcher.RegisterPatch(ModPatchInfo)" /> from treating them as duplicates.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当 <see cref="GetTargets" /> 包含多个 <see cref="ModPatchTarget.TargetType" /> 和
        ///         <see cref="ModPatchTarget.MethodName" /> 均相同的条目时（例如多个 <c>.ctor</c> 重载），
        ///         生成的 <see cref="ModPatchInfo.Id" /> 会按声明顺序追加 <c>__1</c>、<c>__2</c> 等后缀，
        ///         以免 <see cref="Patching.Core.ModPatcher.RegisterPatch(ModPatchInfo)" /> 将它们视为重复项。
        ///     </para>
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
