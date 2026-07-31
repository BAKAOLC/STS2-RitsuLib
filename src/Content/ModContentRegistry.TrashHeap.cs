using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <typeparamref name="TCard" /> as a candidate for the Trash Heap event's Grab option.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <typeparamref name="TCard" /> 注册为“垃圾堆”事件“拿取”选项的候选卡牌。
        ///     </para>
        /// </summary>
        public void RegisterTrashHeapCard<TCard>()
            where TCard : CardModel
        {
            RegisterTrashHeapCard(typeof(TCard));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="cardType" /> as a candidate for the Trash Heap event's Grab option.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <paramref name="cardType" /> 注册为“垃圾堆”事件“拿取”选项的候选卡牌。
        ///     </para>
        /// </summary>
        public void RegisterTrashHeapCard(Type cardType)
        {
            ArgumentNullException.ThrowIfNull(cardType);
            EnsureMutable($"register Trash Heap card '{cardType.Name}'");
            EnsureModelType(cardType, typeof(CardModel), nameof(cardType));

            if (!TrashHeapContentRegistry.RegisterCard(cardType, ModId))
            {
                _logger.Debug($"[TrashHeap] Skipping duplicate card registration: {cardType.FullName}");
                return;
            }

            _logger.Info($"[TrashHeap] Registered card candidate: {cardType.FullName}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <typeparamref name="TRelic" /> as a candidate for the Trash Heap event's Dive In
        ///         option.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <typeparamref name="TRelic" /> 注册为“垃圾堆”事件“深入翻找”选项的候选遗物。
        ///     </para>
        /// </summary>
        public void RegisterTrashHeapRelic<TRelic>()
            where TRelic : RelicModel
        {
            RegisterTrashHeapRelic(typeof(TRelic));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="relicType" /> as a candidate for the Trash Heap event's Dive In
        ///         option.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <paramref name="relicType" /> 注册为“垃圾堆”事件“深入翻找”选项的候选遗物。
        ///     </para>
        /// </summary>
        public void RegisterTrashHeapRelic(Type relicType)
        {
            ArgumentNullException.ThrowIfNull(relicType);
            EnsureMutable($"register Trash Heap relic '{relicType.Name}'");
            EnsureModelType(relicType, typeof(RelicModel), nameof(relicType));

            if (!TrashHeapContentRegistry.RegisterRelic(relicType, ModId))
            {
                _logger.Debug($"[TrashHeap] Skipping duplicate relic registration: {relicType.FullName}");
                return;
            }

            _logger.Info($"[TrashHeap] Registered relic candidate: {relicType.FullName}");
        }
    }
}
