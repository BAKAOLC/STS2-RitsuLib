using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Characters.Visuals.Definition
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines data-driven merchant and rest-site visuals, allowing mods to omit dedicated <c>tscn</c>
    ///         scenes. Instances can be built through <see cref="CharacterWorldProceduralVisualSetBuilder" /> or
    ///         <see cref="ModCharacterWorldSceneVisuals" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义由数据驱动的商人和休息处形象，使模组无需提供专用 <c>tscn</c> 场景。可以通过
    ///         <see cref="CharacterWorldProceduralVisualSetBuilder" /> 或
    ///         <see cref="ModCharacterWorldSceneVisuals" /> 创建实例。
    ///     </para>
    /// </summary>
    /// <param name="Merchant">
    ///     <para xml:lang="en">The merchant-room visual definition.</para>
    ///     <para xml:lang="zh-CN">商人房间形象定义。</para>
    /// </param>
    /// <param name="RestSite">
    ///     <para xml:lang="en">The rest-site visual definition.</para>
    ///     <para xml:lang="zh-CN">休息处形象定义。</para>
    /// </param>
    public sealed record CharacterWorldProceduralVisualSet(
        CharacterMerchantWorldDefinition? Merchant = null,
        CharacterRestSiteWorldDefinition? RestSite = null);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines merchant-room visuals through textures or frame sequences in a <see cref="VisualCueSet" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过 <see cref="VisualCueSet" /> 中的纹理或帧序列定义商人房间形象。
    ///     </para>
    /// </summary>
    /// <param name="CueSet">
    ///     <para xml:lang="en">The visual cues keyed by animation name.</para>
    ///     <para xml:lang="zh-CN">以动画名称为键的形象提示集合。</para>
    /// </param>
    public sealed record CharacterMerchantWorldDefinition(VisualCueSet CueSet);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines rest-site visuals whose cue keys correspond to the base game's act-specific Spine loop names.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义休息处形象，其提示键与游戏本体各章节对应的 Spine 循环动画名称一致。
    ///     </para>
    /// </summary>
    /// <param name="CueSet">
    ///     <para xml:lang="en">
    ///         The visual cues, typically including <c>overgrowth_loop</c>, <c>hive_loop</c>, and
    ///         <c>glory_loop</c>.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         形象提示集合，通常包含 <c>overgrowth_loop</c>、<c>hive_loop</c> 和 <c>glory_loop</c>。
    ///     </para>
    /// </param>
    public sealed record CharacterRestSiteWorldDefinition(VisualCueSet CueSet);
}
