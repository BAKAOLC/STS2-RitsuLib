using System.Globalization;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Controls whether an orb can be selected by vanilla random-orb generation such as <c>OrbModel.GetRandomOrb</c>.
    ///     控制充能球是否可被原版随机充能球生成（例如 <c>OrbModel.GetRandomOrb</c>）选中。
    /// </summary>
    public interface IModOrbRandomPoolPolicy
    {
        /// <summary>
        ///     When true, RitsuLib adds this registered orb to the random-orb candidate pool. Defaults to false on
        ///     <see cref="ModOrbTemplate" /> so custom orbs do not appear in random effects unless the author opts in.
        ///     为 true 时，RitsuLib 会把此已注册充能球加入随机充能球候选池。<see cref="ModOrbTemplate" />
        ///     默认为 false，因此自定义充能球不会在作者未选择加入时出现在随机效果中。
        /// </summary>
        bool AllowInRandomOrbPool { get; }
    }

    /// <summary>
    ///     Value-label display mode for combat orb nodes. RitsuLib uses the vanilla <c>%PassiveAmount</c> and
    ///     <c>%EvokeAmount</c> labels without repositioning them; a single visible label uses the vanilla single-value
    ///     layout, while two visible labels use the vanilla stacked layout.
    ///     战斗充能球节点的数值标签显示模式。RitsuLib 使用原版 <c>%PassiveAmount</c> 与
    ///     <c>%EvokeAmount</c> 标签，不重新定位；单个可见标签使用原版单数值布局，两个可见标签使用原版堆叠布局。
    /// </summary>
    public enum ModOrbValueDisplayMode
    {
        /// <summary>
        ///     Keep the base game's built-in type checks.
        ///     保留游戏内建的类型判定。
        /// </summary>
        Vanilla = 0,

        /// <summary>
        ///     Hide both passive and evoke value labels.
        ///     隐藏被动和激发两个数值标签。
        /// </summary>
        Hidden = 1,

        /// <summary>
        ///     Match the normal vanilla orb behavior: show the passive value normally and the evoke value while the orb is
        ///     previewed as evoking. Only one label is visible at a time, so it uses the vanilla single-value layout.
        ///     匹配普通原版充能球行为：平时显示被动值；当充能球被预览为激发时显示激发值。一次只显示一个标签，
        ///     因此使用原版单数值布局。
        /// </summary>
        Contextual = 2,

        /// <summary>
        ///     Always show only the passive value text in the vanilla single-value layout.
        ///     始终在原版单数值布局中显示被动值文本。
        /// </summary>
        SinglePassive = 3,

        /// <summary>
        ///     Always show only the evoke value text in the vanilla single-value layout.
        ///     始终在原版单数值布局中显示激发值文本。
        /// </summary>
        SingleEvoke = 4,

        /// <summary>
        ///     Match the vanilla <c>DarkOrb</c> behavior: show passive and evoke value labels together in the vanilla
        ///     stacked layout.
        ///     匹配原版 <c>DarkOrb</c> 行为：在原版堆叠布局中同时显示被动值和激发值标签。
        /// </summary>
        Both = 5,
    }

    /// <summary>
    ///     Controls the passive/evoke value labels rendered on an orb node.
    ///     控制充能球节点上渲染的被动/激发数值标签。
    /// </summary>
    public interface IModOrbValueDisplayPolicy
    {
        /// <summary>
        ///     Label visibility behavior. Use <see cref="ModOrbValueDisplayMode.Vanilla" /> to leave the game decision intact.
        ///     标签可见性行为。使用 <see cref="ModOrbValueDisplayMode.Vanilla" /> 可保留游戏原判定。
        /// </summary>
        ModOrbValueDisplayMode ValueDisplayMode { get; }

        /// <summary>
        ///     Text rendered in the passive label when it is visible.
        ///     被动标签可见时渲染的文本。
        /// </summary>
        string PassiveValueDisplayText { get; }

        /// <summary>
        ///     Text rendered in the evoke label when it is visible.
        ///     激发标签可见时渲染的文本。
        /// </summary>
        string EvokeValueDisplayText { get; }
    }

    /// <summary>
    ///     Base <see cref="OrbModel" /> for mods: keyword hover tips, dimmed UI color default, and
    ///     <see cref="IModOrbAssetOverrides" /> paths and optional <see cref="TryCreateOrbSprite" />.
    ///     Mod 充能球的基础 <see cref="OrbModel" />：提供关键词悬浮提示、变暗 UI 颜色默认值、
    ///     <see cref="IModOrbAssetOverrides" /> 路径，以及可选的 <see cref="TryCreateOrbSprite" />。
    /// </summary>
    public abstract class ModOrbTemplate : OrbModel, IModOrbAssetOverrides, IModOrbSpriteFactory,
        IModOrbRandomPoolPolicy, IModOrbValueDisplayPolicy
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
        [
            .. AdditionalHoverTips,
            .. RegisteredKeywordIds.ToHoverTips(),
            .. this.GetModKeywordHoverTips(),
        ];

        /// <inheritdoc />
        public override Color DarkenedColor => Colors.DarkSlateGray;

        /// <inheritdoc />
        public virtual OrbAssetProfile AssetProfile => OrbAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomIconPath => AssetProfile.IconPath;

        /// <inheritdoc />
        public virtual string? CustomVisualsScenePath => AssetProfile.VisualsScenePath;

        /// <inheritdoc />
        public virtual bool AllowInRandomOrbPool => false;

        Node2D? IModOrbSpriteFactory.TryCreateOrbSprite()
        {
            return TryCreateOrbSprite();
        }

        /// <inheritdoc />
        public virtual ModOrbValueDisplayMode ValueDisplayMode => ModOrbValueDisplayMode.Vanilla;

        /// <inheritdoc />
        public virtual string PassiveValueDisplayText => PassiveVal.ToString("0", CultureInfo.InvariantCulture);

        /// <inheritdoc />
        public virtual string EvokeValueDisplayText => EvokeVal.ToString("0", CultureInfo.InvariantCulture);

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
