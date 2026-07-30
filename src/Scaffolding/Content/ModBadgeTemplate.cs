using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">Provides a base badge type for mods.</para>
    ///     <para xml:lang="zh-CN">提供模组徽章的基础类型。</para>
    /// </summary>
    public abstract class ModBadgeTemplate
    {
        /// <summary>
        ///     <para xml:lang="en">Gets whether obtaining this badge requires a victory.</para>
        ///     <para xml:lang="zh-CN">获取获得此徽章是否需要取得胜利。</para>
        /// </summary>
        public virtual bool RequiresWin => false;

        /// <summary>
        ///     <para xml:lang="en">Gets whether this badge is available only in multiplayer.</para>
        ///     <para xml:lang="zh-CN">获取此徽章是否仅在多人模式中可用。</para>
        /// </summary>
        public virtual bool MultiplayerOnly => false;

        /// <summary>
        ///     <para xml:lang="en">Gets the optional badge-icon replacement path.</para>
        ///     <para xml:lang="zh-CN">获取可选的徽章图标替换路径。</para>
        /// </summary>
        public virtual string? CustomBadgeIconPath => null;

        /// <summary>
        ///     <para xml:lang="en">Gets the stable badge ID derived from the template type name.</para>
        ///     <para xml:lang="zh-CN">获取根据模板类型名称生成的稳定徽章 ID。</para>
        /// </summary>
        public virtual string Id => BuildDefaultRegistrationId(GetType().Name);

        /// <summary>
        ///     <para xml:lang="en">Returns the badge rarity for the supplied run and player.</para>
        ///     <para xml:lang="zh-CN">返回指定游戏记录和玩家对应的徽章稀有度。</para>
        /// </summary>
        public abstract BadgeRarity Rarity(SerializableRun run, SerializablePlayer player);

        /// <summary>
        ///     <para xml:lang="en">Returns whether the player obtained this badge in the supplied run.</para>
        ///     <para xml:lang="zh-CN">返回玩家是否在指定游戏记录中获得此徽章。</para>
        /// </summary>
        public abstract bool IsObtained(SerializableRun run, SerializablePlayer player);

        internal static string BuildDefaultRegistrationId(string typeName)
        {
            return string.IsNullOrWhiteSpace(typeName)
                ? string.Empty
                : ModContentRegistry.NormalizePublicStem(typeName);
        }
    }
}
