using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="OrbModel" /> for mods: keyword hover tips, dimmed UI color default, and
    ///     <see cref="IModOrbAssetOverrides" /> paths and optional <see cref="TryCreateOrbSprite" />.
    ///     Mod 充能球的基础 <see cref="OrbModel" />：提供关键词悬浮提示、变暗 UI 颜色默认值、
    ///     <see cref="IModOrbAssetOverrides" /> 路径，以及可选的 <see cref="TryCreateOrbSprite" />。
    /// </summary>
    public abstract class ModOrbTemplate : OrbModel, IModOrbAssetOverrides, IModOrbSpriteFactory
    {
        /// <summary>
        ///     Keyword ids surfaced on this orb's hover tips. <b>Display-only</b>: unlike
        ///     <see cref="ModCardTemplate.RegisteredKeywordIds" />, this does <b>not</b> participate in any
        ///     gameplay keyword set (vanilla <see cref="OrbModel" /> has no <c>Keywords</c>/<c>CardKeyword</c>
        ///     storage) — each id is looked up in <see cref="ModKeywordRegistry" /> purely to render a hover tip
        ///     via <c>ToHoverTips()</c>. Use it for visual documentation; gameplay behaviour must be implemented
        ///     explicitly in the orb's own logic.
        ///     要显示在此充能球悬停提示上的关键词 id。<b>仅用于显示</b>：不同于 <see cref="ModCardTemplate.RegisteredKeywordIds" />，它<b>不会</b>
        ///     参与任何游戏逻辑关键词集合（原版 <see cref="OrbModel" /> 没有 <c>Keywords</c>/<c>CardKeyword</c> 存储）- 每个 id 只会通过
        ///     <see cref="ModKeywordRegistry" /> 查找，用来通过 <c>ToHoverTips()</c> 渲染悬停提示。请将它用于视觉说明；游戏行为必须在充能球自身逻辑中显式实现。
        /// </summary>
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];

        /// <summary>
        ///     Additional hover tips merged after keyword expansion.
        ///     合并在关键词展开之后的额外悬浮提示。
        /// </summary>
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        /// <inheritdoc />
        protected sealed override IEnumerable<IHoverTip> ExtraHoverTips =>
            AdditionalHoverTips
                .Concat(RegisteredKeywordIds.ToHoverTips())
                .Concat(this.GetModKeywordHoverTips())
                .ToArray();

        /// <inheritdoc />
        public override Color DarkenedColor => Colors.DarkSlateGray;

        /// <inheritdoc />
        public virtual OrbAssetProfile AssetProfile => OrbAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomIconPath => AssetProfile.IconPath;

        /// <inheritdoc />
        public virtual string? CustomVisualsScenePath => AssetProfile.VisualsScenePath;

        Node2D? IModOrbSpriteFactory.TryCreateOrbSprite()
        {
            return TryCreateOrbSprite();
        }

        /// <summary>
        ///     Non-null node replaces the scene from <see cref="CustomVisualsScenePath" />; provide Spine and animations
        ///     compatible with <c>CreateSprite</c> callers if required.
        ///     返回非 null 节点时，会替代来自 <see cref="CustomVisualsScenePath" /> 的场景；如有需要，请提供与
        ///     <c>CreateSprite</c> 调用方兼容的 Spine 和动画。
        /// </summary>
        protected virtual Node2D? TryCreateOrbSprite()
        {
            return null;
        }
    }
}
