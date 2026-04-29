using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base badge type for mods.
    /// </summary>
    public abstract class ModBadgeTemplate
    {
        /// <summary>
        ///     Whether this badge requires a win.
        /// </summary>
        public virtual bool RequiresWin => false;

        /// <summary>
        ///     Whether this badge is multiplayer-only.
        /// </summary>
        public virtual bool MultiplayerOnly => false;

        /// <summary>
        ///     Optional icon path override for this badge.
        /// </summary>
        public virtual string? CustomBadgeIconPath => null;

        /// <summary>
        ///     Stable badge id derived from template type name.
        /// </summary>
        public virtual string Id => BuildDefaultRegistrationId(GetType().Name);

        /// <summary>
        ///     Computes rarity for the current run/player context.
        /// </summary>
        public abstract BadgeRarity Rarity(SerializableRun run, SerializablePlayer player);

        /// <summary>
        ///     Whether this badge has been obtained in the current run/player context.
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
