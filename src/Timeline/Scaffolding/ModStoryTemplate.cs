using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     Base <see cref="StoryModel" /> that derives its id from <see cref="StoryKey" />. Epoch order comes from
    ///     <see cref="ModTimelineRegistry.RegisterStoryEpoch{TStory, TEpoch}" /> (or
    ///     <see cref="TimelineColumnPackEntry{TStory}" />),
    ///     not from an overridden type list.
    ///     基类 <see cref="StoryModel" /> 其 id 派生自 <see cref="StoryKey" />. 纪元顺序来自
    ///     <see cref="TimelineColumnPackEntry{TStory}" />),
    ///     而不是来自重写的类型列表。
    /// </summary>
    public abstract class ModStoryTemplate : StoryModel
    {
        /// <inheritdoc />
        protected sealed override string Id => StringHelper.Slugify(StoryKey);

        /// <inheritdoc />
        public sealed override EpochModel[] Epochs =>
        [
            .. ModStoryEpochBindings
                .GetOrderedEpochTypes(GetType())
                .Select(ResolveEpoch),
        ];

        /// <summary>
        ///     Human-readable story key slugified into the model id.
        ///     会转换为模型 id 的人类可读故事键。
        /// </summary>
        protected abstract string StoryKey { get; }

        private static EpochModel ResolveEpoch(Type epochType)
        {
            ArgumentNullException.ThrowIfNull(epochType);
            return EpochModel.Get(EpochModel.GetId(epochType));
        }
    }
}
