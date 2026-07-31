using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">Provides optional image and outline replacements for a mod potion.</para>
    ///     <para xml:lang="zh-CN">提供模组药水可选的图像与轮廓替换。</para>
    /// </summary>
    public interface IModPotionAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the structured potion asset profile.</para>
        ///     <para xml:lang="zh-CN">获取结构化药水资源配置。</para>
        /// </summary>
        PotionAssetProfile AssetProfile { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the potion-image replacement path.</para>
        ///     <para xml:lang="zh-CN">获取药水图像替换路径。</para>
        /// </summary>
        string? CustomImagePath { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the potion-outline replacement path.</para>
        ///     <para xml:lang="zh-CN">获取药水轮廓替换路径。</para>
        /// </summary>
        string? CustomOutlinePath { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a base <see cref="PotionModel" /> for mods with keyword hover tips and potion asset overrides.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为模组提供基础 <see cref="PotionModel" />，支持关键词悬浮提示和药水资源替换。
    ///     </para>
    /// </summary>
    public abstract class ModPotionTemplate : PotionModel, IModPotionAssetOverrides
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
        public sealed override IEnumerable<IHoverTip> ExtraHoverTips =>
        [
            .. AdditionalHoverTips,
            .. RegisteredKeywordIds.ToHoverTips(),
            .. this.GetModKeywordHoverTips(),
        ];

        /// <inheritdoc />
        public virtual PotionAssetProfile AssetProfile => PotionAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomImagePath => AssetProfile.ImagePath;

        /// <inheritdoc />
        public virtual string? CustomOutlinePath => AssetProfile.OutlinePath;
    }
}
