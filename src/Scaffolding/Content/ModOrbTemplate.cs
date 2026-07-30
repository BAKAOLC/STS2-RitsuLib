using System.Globalization;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">Controls whether an orb participates in base-game random-orb selection.</para>
    ///     <para xml:lang="zh-CN">控制充能球是否参与游戏本体的随机充能球选择。</para>
    /// </summary>
    public interface IModOrbRandomPoolPolicy
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether RitsuLib adds this registered orb to the random candidate pool.
        ///         <see cref="ModOrbTemplate" /> defaults to <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 RitsuLib 是否将此已注册充能球加入随机候选池。<see cref="ModOrbTemplate" /> 默认为
        ///         <see langword="false" />。
        ///     </para>
        /// </summary>
        bool AllowInRandomOrbPool { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Specifies which passive and evoke value labels are visible on a combat orb. Existing
    ///         <c>%PassiveAmount</c> and <c>%EvokeAmount</c> layouts are preserved.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         指定战斗充能球显示哪些被动和激发数值标签，并保留现有 <c>%PassiveAmount</c> 与
    ///         <c>%EvokeAmount</c> 布局。
    ///     </para>
    /// </summary>
    public enum ModOrbValueDisplayMode
    {
        /// <summary>
        ///     <para xml:lang="en">Preserves the base game's built-in type checks.</para>
        ///     <para xml:lang="zh-CN">保留游戏本体的内置类型判定。</para>
        /// </summary>
        Vanilla = 0,

        /// <summary>
        ///     <para xml:lang="en">Hides both value labels.</para>
        ///     <para xml:lang="zh-CN">隐藏两个数值标签。</para>
        /// </summary>
        Hidden = 1,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Shows the passive value normally and the evoke value while evoking is previewed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通常显示被动值，预览激发时改为显示激发值。
        ///     </para>
        /// </summary>
        Contextual = 2,

        /// <summary>
        ///     <para xml:lang="en">Shows only the passive value.</para>
        ///     <para xml:lang="zh-CN">只显示被动值。</para>
        /// </summary>
        SinglePassive = 3,

        /// <summary>
        ///     <para xml:lang="en">Shows only the evoke value.</para>
        ///     <para xml:lang="zh-CN">只显示激发值。</para>
        /// </summary>
        SingleEvoke = 4,

        /// <summary>
        ///     <para xml:lang="en">Shows both values in the base stacked layout used by <c>DarkOrb</c>.</para>
        ///     <para xml:lang="zh-CN">使用 <c>DarkOrb</c> 的游戏本体堆叠布局同时显示两个数值。</para>
        /// </summary>
        Both = 5,
    }

    /// <summary>
    ///     <para xml:lang="en">Controls the passive and evoke value labels rendered on an orb node.</para>
    ///     <para xml:lang="zh-CN">控制充能球节点显示的被动和激发数值标签。</para>
    /// </summary>
    public interface IModOrbValueDisplayPolicy
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the label-visibility mode.</para>
        ///     <para xml:lang="zh-CN">获取标签可见性模式。</para>
        /// </summary>
        ModOrbValueDisplayMode ValueDisplayMode { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the text shown in the passive-value label.</para>
        ///     <para xml:lang="zh-CN">获取被动值标签显示的文本。</para>
        /// </summary>
        string PassiveValueDisplayText { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the text shown in the evoke-value label.</para>
        ///     <para xml:lang="zh-CN">获取激发值标签显示的文本。</para>
        /// </summary>
        string EvokeValueDisplayText { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a base <see cref="OrbModel" /> for mods with keyword hover tips, asset replacements, random-pool
    ///         policy, value-label policy, and optional runtime sprite creation.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为模组提供基础 <see cref="OrbModel" />，支持关键词悬浮提示、资源替换、随机池策略、数值标签策略，
    ///         以及可选的运行时精灵创建。
    ///     </para>
    /// </summary>
    public abstract class ModOrbTemplate : OrbModel, IModOrbAssetOverrides, IModOrbSpriteFactory,
        IModOrbRandomPoolPolicy, IModOrbValueDisplayPolicy
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets display-only keyword IDs resolved through <see cref="ModKeywordRegistry" /> for hover tips.
        ///         They do not add gameplay keyword behavior.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取仅用于显示的关键词 ID；这些 ID 通过 <see cref="ModKeywordRegistry" /> 解析为悬浮提示，
        ///         不会添加任何关键词游戏行为。
        ///     </para>
        /// </summary>
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];

        /// <summary>
        ///     <para xml:lang="en">Gets additional hover tips.</para>
        ///     <para xml:lang="zh-CN">获取额外悬浮提示。</para>
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
        ///     <para xml:lang="en">
        ///         Returns a runtime-created orb sprite, or <see langword="null" /> to load
        ///         <see cref="CustomVisualsScenePath" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回运行时创建的充能球精灵；返回 <see langword="null" /> 时加载
        ///         <see cref="CustomVisualsScenePath" />。
        ///     </para>
        /// </summary>
        protected virtual Node2D? TryCreateOrbSprite()
        {
            return null;
        }
    }
}
