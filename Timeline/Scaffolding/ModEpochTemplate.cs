using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     Base <see cref="EpochModel" /> for mods with optional <see cref="IModEpochAssetOverrides" /> timeline portrait
    ///     paths. Unlock-epoch templates in this namespace inherit from this type.
    /// </summary>
    public abstract class ModEpochTemplate : EpochModel, IModEpochAssetOverrides
    {
        /// <inheritdoc />
        public virtual EpochAssetProfile AssetProfile => EpochAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomPackedPortraitPath => AssetProfile.PackedPortraitPath;

        /// <inheritdoc />
        public virtual string? CustomBigPortraitPath => AssetProfile.BigPortraitPath;
    }
}
