using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     Base <see cref="StoryModel" /> that derives its id from <see cref="StoryKey" /> and composes epochs from CLR types.
    /// </summary>
    public abstract class ModStoryTemplate : StoryModel
    {
        /// <inheritdoc />
        protected sealed override string Id => StringHelper.Slugify(StoryKey);

        /// <inheritdoc />
        public sealed override EpochModel[] Epochs => EpochTypes
            .Select(ResolveEpoch)
            .ToArray();

        /// <summary>
        ///     Human-readable story key slugified into the model id.
        /// </summary>
        protected abstract string StoryKey { get; }

        /// <summary>
        ///     Ordered CLR types of epochs belonging to this story; each must resolve via <see cref="EpochModel.GetId(Type)" />.
        /// </summary>
        protected abstract IEnumerable<Type> EpochTypes { get; }

        private static EpochModel ResolveEpoch(Type epochType)
        {
            ArgumentNullException.ThrowIfNull(epochType);
            return EpochModel.Get(EpochModel.GetId(epochType));
        }
    }
}
