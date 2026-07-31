using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Timeline
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Collects ordered epoch CLR types for each concrete <see cref="StoryModel" /> type.
    ///         <see cref="ModTimelineRegistry.RegisterStoryEpoch{TStory,TEpoch}" /> adds entries, and
    ///         <see cref="Scaffolding.ModStoryTemplate" /> reads them instead of using a hard-coded type list.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为每个具体 <see cref="StoryModel" /> 类型收集有序的纪元 CLR 类型。
    ///         <see cref="ModTimelineRegistry.RegisterStoryEpoch{TStory,TEpoch}" /> 会添加条目，
    ///         <see cref="Scaffolding.ModStoryTemplate" /> 则读取这些条目，而非使用硬编码的类型列表。
    ///     </para>
    /// </summary>
    public static class ModStoryEpochBindings
    {
        private static readonly Lock Sync = new();

        private static readonly Dictionary<Type, List<Type>> StoryToEpochs = [];

        private static readonly Dictionary<Type, Type> EpochToStory = [];

        private static bool _frozen;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends <paramref name="epochType" /> to <paramref name="storyType" />'s column in registration order.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按注册顺序将 <paramref name="epochType" /> 追加到 <paramref name="storyType" /> 的列中。
        ///     </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">
        ///         Registration is frozen, the epoch is already present in this story, or the epoch belongs to another story.
        ///     </para>
        ///     <para xml:lang="zh-CN">注册已被冻结、该纪元已存在于此故事中，或该纪元已属于另一个故事。</para>
        /// </exception>
        public static void Append(Type storyType, Type epochType)
        {
            ArgumentNullException.ThrowIfNull(storyType);
            ArgumentNullException.ThrowIfNull(epochType);

            if (!typeof(StoryModel).IsAssignableFrom(storyType) ||
                storyType.IsAbstract ||
                storyType.IsInterface)
                throw new ArgumentException($"Type '{storyType.FullName}' must be a concrete StoryModel subtype.",
                    nameof(storyType));

            if (!typeof(EpochModel).IsAssignableFrom(epochType) ||
                epochType.IsAbstract ||
                epochType.IsInterface)
                throw new ArgumentException($"Type '{epochType.FullName}' must be a concrete EpochModel subtype.",
                    nameof(epochType));

            lock (Sync)
            {
                ThrowIfFrozen();

                if (EpochToStory.TryGetValue(epochType, out var owner) && owner != storyType)
                    throw new InvalidOperationException(
                        $"Epoch type '{epochType.Name}' is already bound to story '{owner.Name}'; cannot bind to '{storyType.Name}'.");

                if (!StoryToEpochs.TryGetValue(storyType, out var list)) list = [];

                if (list.Contains(epochType))
                    throw new InvalidOperationException(
                        $"Epoch type '{epochType.Name}' is already listed for story '{storyType.Name}'.");

                EpochToStory[epochType] = storyType;
                StoryToEpochs.TryAdd(storyType, list);
                list.Add(epochType);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the ordered epoch types for a concrete story type, or an empty list when none are bound.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回具体故事类型的有序纪元类型；没有绑定时返回空列表。</para>
        /// </summary>
        public static IReadOnlyList<Type> GetOrderedEpochTypes(Type storyConcreteType)
        {
            ArgumentNullException.ThrowIfNull(storyConcreteType);

            lock (Sync)
            {
                return StoryToEpochs.TryGetValue(storyConcreteType, out var list)
                    ? list.ToArray()
                    : [];
            }
        }

        internal static void Freeze()
        {
            lock (Sync)
            {
                _frozen = true;
            }
        }

        private static void ThrowIfFrozen()
        {
            if (_frozen)
                throw new InvalidOperationException(
                    "Story–epoch bindings are frozen; register before model initialization.");
        }
    }
}
