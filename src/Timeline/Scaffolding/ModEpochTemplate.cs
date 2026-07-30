using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     Base <see cref="EpochModel" /> for mods with optional <see cref="IModEpochAssetOverrides" /> timeline portrait
    ///     paths. Unlock-epoch templates in this namespace inherit from this type.
    ///     基类 <see cref="EpochModel" /> 供 mod 使用 与 可选 <see cref="IModEpochAssetOverrides" /> timeline portrait
    ///     路径s. 此命名空间中的解锁纪元模板继承自该类型。
    /// </summary>
    public abstract class ModEpochTemplate : EpochModel, IModEpochAssetOverrides
    {
        /// <inheritdoc />
        public sealed override EpochEra Era => ModTimelineLayoutRegistry.ResolveEra(GetType());

        /// <inheritdoc />
        public sealed override int EraPosition => ModTimelineLayoutRegistry.ResolveEraPosition(GetType());

        /// <inheritdoc />
        public virtual EpochAssetProfile AssetProfile => EpochAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomPackedPortraitPath => AssetProfile.PackedPortraitPath;

        /// <inheritdoc />
        public virtual string? CustomBigPortraitPath => AssetProfile.BigPortraitPath;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Validates content passed to the base game's unlock-text formatter, which reads the first three items.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         验证传给原版解锁文本格式化方法的内容；该方法会读取前三项。
        ///     </para>
        /// </summary>
        protected IReadOnlyList<TModel> RequireUnlockPresentationItems<TModel>(
            IReadOnlyList<TModel> items,
            string sourceName)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
            if (items.Count < 3)
                throw new InvalidOperationException(
                    $"Epoch '{Id}' requires at least three {typeof(TModel).Name} entries from {sourceName}; " +
                    "the built-in timeline unlock text reads the first three items.");

            return items;
        }
    }
}
