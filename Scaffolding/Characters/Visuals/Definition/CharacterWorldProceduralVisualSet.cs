using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Characters.Visuals.Definition
{
    /// <summary>
    ///     Data-only definitions for merchant / rest-site visuals so mod authors can skip authoring dedicated
    ///     <c>tscn</c> scenes. Built with <see cref="CharacterWorldProceduralVisualSetBuilder" /> or
    ///     <see cref="ModCharacterWorldSceneVisuals" />.
    ///     商人 / 休息点视觉的纯数据定义，让 mod 作者可以不编写专用 <c>tscn</c> 场景。可通过
    ///     <see cref="CharacterWorldProceduralVisualSetBuilder" /> 或 <see cref="ModCharacterWorldSceneVisuals" /> 构建。
    /// </summary>
    /// <param name="Merchant">
    ///     Merchant-room shell + cues (e.g. <c>relaxed_loop</c>, <c>die</c>).
    ///     商人房间外壳 + cue（例如 <c>relaxed_loop</c>、<c>die</c>）。
    /// </param>
    /// <param name="RestSite">
    ///     Rest-site shell + cues (e.g. <c>overgrowth_loop</c>, <c>hive_loop</c>, <c>glory_loop</c>).
    ///     休息点外壳 + cue（例如 <c>overgrowth_loop</c>、<c>hive_loop</c>、<c>glory_loop</c>）。
    /// </param>
    public sealed record CharacterWorldProceduralVisualSet(
        CharacterMerchantWorldDefinition? Merchant = null,
        CharacterRestSiteWorldDefinition? RestSite = null);

    /// <summary>
    ///     Merchant-room procedural visuals: uses <see cref="VisualCueSet" /> (textures / frame sequences per cue).
    ///     商人房间程序化视觉：使用 <see cref="VisualCueSet" />（逐 cue 贴图 / 帧序列）。
    /// </summary>
    /// <param name="CueSet">
    ///     Texture / frame sequences keyed by animation name.
    ///     以动画名为键的贴图 / 帧序列。
    /// </param>
    public sealed record CharacterMerchantWorldDefinition(VisualCueSet CueSet);

    /// <summary>
    ///     Rest-site procedural visuals: cue keys match vanilla Spine loop names per act.
    ///     休息点程序化视觉：cue 键匹配每个 act 的原版 Spine 循环名。
    /// </summary>
    /// <param name="CueSet">
    ///     Typically <c>overgrowth_loop</c>, <c>hive_loop</c>, <c>glory_loop</c>.
    ///     通常是 <c>overgrowth_loop</c>、<c>hive_loop</c>、<c>glory_loop</c>。
    /// </param>
    public sealed record CharacterRestSiteWorldDefinition(VisualCueSet CueSet);
}
