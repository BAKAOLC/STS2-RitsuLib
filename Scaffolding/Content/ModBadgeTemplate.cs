using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base badge type for mods.
    ///     Mod 徽章的基础类型。
    /// </summary>
    public abstract class ModBadgeTemplate
    {
        /// <summary>
        ///     Whether this badge requires a win.
        ///     此徽章是否需要胜利。
        /// </summary>
        public virtual bool RequiresWin => false;

        /// <summary>
        ///     Whether this badge is multiplayer-only.
        ///     此徽章是否仅限多人模式。
        /// </summary>
        public virtual bool MultiplayerOnly => false;

        /// <summary>
        ///     Optional icon path override for this badge.
        ///     此徽章的可选图标路径覆盖。
        /// </summary>
        public virtual string? CustomBadgeIconPath => null;

        /// <summary>
        ///     Stable badge id derived from template type name.
        ///     从模板类型名派生的稳定徽章 id。
        /// </summary>
        public virtual string Id => BuildDefaultRegistrationId(GetType().Name);

        /// <summary>
        ///     Computes rarity for the current run/player context.
        ///     根据当前跑局/玩家上下文计算稀有度。
        /// </summary>
        public abstract BadgeRarity Rarity(SerializableRun run, SerializablePlayer player);

        /// <summary>
        ///     Whether this badge has been obtained in the current run/player context.
        ///     此徽章在当前跑局/玩家上下文中是否已获得。
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
