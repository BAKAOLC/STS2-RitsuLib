namespace STS2RitsuLib.Compat
{
    /// <summary>
    ///     Configure minimum host versions for API branches. When <see cref="Sts2HostVersion.Numeric" /> is known and
    ///     compares to these, RitsuLib picks the matching path; when host version is unknown, behavior falls back to
    ///     reflection on the loaded <c>sts2</c> assembly.
    ///     <para />
    ///     Set non-null values when you know the first Steam / <c>release_info.json</c> version that shipped each API.
    ///     配置 API 分支的最低宿主版本。当 <see cref="Sts2HostVersion.Numeric" /> 已知并且
    ///     可与这些阈值比较时，RitsuLib 会选择匹配路径；宿主版本未知时，行为回退为
    ///     对已加载的 <c>sts2</c> 程序集进行反射。
    ///     对已加载的 <c>sts2</c> 程序集进行反射。
    ///     <para />
    ///     当你知道每个 API 首次随 Steam
    ///     <c>release_info.json</c> 版本发布时，请设置非 null 值。
    /// </summary>
    internal static class Sts2ApiFeatureThresholds;
}
