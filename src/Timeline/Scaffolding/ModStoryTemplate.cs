using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a <see cref="StoryModel" /> base whose ID is derived from <see cref="StoryKey" />. Its epoch order
    ///         comes from <see cref="ModTimelineRegistry.RegisterStoryEpoch{TStory,TEpoch}" /> or
    ///         <see cref="TimelineColumnPackEntry{TStory}" />, rather than an overridden type list.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供从 <see cref="StoryKey" /> 派生 ID 的 <see cref="StoryModel" /> 基类。其纪元顺序来自
    ///         <see cref="ModTimelineRegistry.RegisterStoryEpoch{TStory,TEpoch}" /> 或
    ///         <see cref="TimelineColumnPackEntry{TStory}" />，而不是由子类重写类型列表。
    ///     </para>
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
        ///     <para xml:lang="en">Gets the human-readable story key used to derive the model ID.</para>
        ///     <para xml:lang="zh-CN">获取用于派生模型 ID 的可读故事键。</para>
        /// </summary>
        protected abstract string StoryKey { get; }

        private static EpochModel ResolveEpoch(Type epochType)
        {
            ArgumentNullException.ThrowIfNull(epochType);
            return EpochModel.Get(EpochModel.GetId(epochType));
        }
    }
}
