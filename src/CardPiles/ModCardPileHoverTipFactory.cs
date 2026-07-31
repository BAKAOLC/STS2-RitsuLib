using Godot;
using MegaCrit.Sts2.Core.HoverTips;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Creates base-game <see cref="HoverTip" /> instances from registered mod card-pile metadata.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         根据已注册的模组卡牌牌堆元数据创建游戏原有的 <see cref="HoverTip" /> 实例。
    ///     </para>
    /// </summary>
    public static class ModCardPileHoverTipFactory
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a hover tip using the definition's localized title and description. The icon is loaded
        ///         only when <see cref="ModCardPileDefinition.IconPath" /> is nonblank and names an existing
        ///         Godot resource; otherwise the hover tip has no icon.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用定义中的本地化标题和描述创建悬停提示。仅当
        ///         <see cref="ModCardPileDefinition.IconPath" /> 非空白且指向现有 Godot 资源时加载图标；
        ///         否则悬停提示不带图标。
        ///     </para>
        /// </summary>
        /// <param name="definition">
        ///     <para xml:lang="en">The registered card-pile definition.</para>
        ///     <para xml:lang="zh-CN">已注册的卡牌牌堆定义。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A base-game hover tip backed by the definition's localization keys.</para>
        ///     <para xml:lang="zh-CN">使用该定义本地化键的游戏原有悬停提示。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="definition" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="definition" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static HoverTip Create(ModCardPileDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            Texture2D? icon = null;
            if (!string.IsNullOrWhiteSpace(definition.IconPath)
                && ResourceLoader.Exists(definition.IconPath))
                icon = ResourceLoader.Load<Texture2D>(definition.IconPath);

            return new(definition.Title, definition.Description, icon);
        }
    }
}
