using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Timeline
{
    /// <summary>
    ///     Collects ordered epoch CLR types per concrete <see cref="MegaCrit.Sts2.Core.Timeline.StoryModel" /> type, filled by
    ///     <see cref="ModTimelineRegistry.RegisterStoryEpoch{TStory, TEpoch}" />. <see cref="Scaffolding.ModStoryTemplate" />
    ///     reads this list instead of a hard-coded <c>EpochTypes</c> override.
    ///     按具体 <see cref="MegaCrit.Sts2.Core.Timeline.StoryModel" /> 类型收集有序纪元 CLR 类型，由
    ///     <see cref="ModTimelineRegistry.RegisterStoryEpoch{TStory, TEpoch}" /> 填充。
    ///     <see cref="Scaffolding.ModStoryTemplate" />
    ///     会读取此列表，而不是硬编码的 <c>EpochTypes</c> override。
    /// </summary>
    public static class ModStoryEpochBindings
    {
        private static readonly Lock Sync = new();

        private static readonly Dictionary<Type, List<Type>> StoryToEpochs = [];

        private static readonly Dictionary<Type, Type> EpochToStory = [];

        private static bool _frozen;

        /// <summary>
        ///     Appends <paramref name="epochType" /> to <paramref name="storyType" />'s column order (registration order).
        ///     将 <paramref name="epochType" /> 追加到 <paramref name="storyType" /> 的列顺序中（注册顺序）。
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     Frozen, duplicate epoch in the same story, or epoch already bound to
        ///     Frozen, duplicate epoch in the same story, 或 epoch already bound to
        ///     another story.
        ///     中文说明：another story.
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

                EpochToStory[epochType] = storyType;

                if (!StoryToEpochs.TryGetValue(storyType, out var list))
                {
                    list = [];
                    StoryToEpochs[storyType] = list;
                }

                if (list.Contains(epochType))
                    throw new InvalidOperationException(
                        $"Epoch type '{epochType.Name}' is already listed for story '{storyType.Name}'.");

                list.Add(epochType);
            }
        }

        /// <summary>
        ///     Ordered epoch types for a concrete story type, or empty when none were bound.
        ///     具体 story 类型的有序纪元类型；未绑定时为空。
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
