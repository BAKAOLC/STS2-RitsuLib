using System.Reflection;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Timeline.Scaffolding;

namespace STS2RitsuLib.Timeline
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers the <see cref="EpochEra" /> column and <c>EraPosition</c> used to place each
    ///         <see cref="ModEpochTemplate" />. Base-game <see cref="EpochModel" /> instances reserve their cells first,
    ///         preventing
    ///         mod slots from silently overlapping them.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         注册各 <see cref="ModEpochTemplate" /> 在时间线中使用的 <see cref="EpochEra" /> 列和列内
    ///         <c>EraPosition</c>。游戏本体的 <see cref="EpochModel" /> 实例会先占用其格位，避免模组槽位与之静默重叠。
    ///     </para>
    /// </summary>
    public static class ModTimelineLayoutRegistry
    {
        private const int MaxAutoPositionScan = 128;

        /// <summary>
        ///     <para xml:lang="en">Lower bound for scans that place a column before an anchor era.</para>
        ///     <para xml:lang="zh-CN">在锚点时代之前放置列时所用的扫描下限。</para>
        /// </summary>
        private const int MinEraIntScan = -100_000;

        /// <summary>
        ///     <para xml:lang="en">Upper bound for scans that place a column after an anchor era.</para>
        ///     <para xml:lang="zh-CN">在锚点时代之后放置列时所用的扫描上限。</para>
        /// </summary>
        private const int MaxEraIntScan = 100_000;

        private static readonly Lock Sync = new();

        private static readonly Dictionary<Type, TimelineSlotAssignment> LayoutByEpochType = [];

        private static readonly HashSet<(long EraKey, int Position)> Occupied = [];

        private static bool _frozen;

        private static bool _vanillaSeeded;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers an explicit slot, throwing if the cell is occupied by the base game or another mod registration.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册显式槽位；若该格位已被游戏本体或其他模组注册占用，则抛出异常。
        ///     </para>
        /// </summary>
        public static void RegisterTimelineSlot(Type epochType, EpochEra era, int eraPosition, string modId)
        {
            ArgumentNullException.ThrowIfNull(epochType);
            ArgumentOutOfRangeException.ThrowIfNegative(eraPosition);
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            ThrowIfNotModEpochTemplate(epochType);

            lock (Sync)
            {
                ThrowIfFrozen();
                EnsureVanillaOccupancySeededLocked();

                ThrowIfLayoutAlreadyRegistered(epochType);

                var key = ToOccupancyKey(era, eraPosition);
                if (!Occupied.Add(key))
                    throw new InvalidOperationException(
                        $"Timeline slot conflict: era={(int)era} position={eraPosition} is already occupied " +
                        $"(cannot register '{epochType.Name}' for mod '{modId}'). " +
                        "Pick another column (EpochEra) / position, or use AutoTimelineSlot for the first free slot in a column.");

                LayoutByEpochType[epochType] = new(era, eraPosition);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers the lowest nonnegative <c>EraPosition</c> in <paramref name="era" /> that is not occupied by the base
        ///         game or an earlier mod registration.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <paramref name="era" /> 中注册未被游戏本体或先前模组注册占用的最小非负
        ///         <c>EraPosition</c>。
        ///     </para>
        /// </summary>
        public static void RegisterAutoTimelineSlot(Type epochType, EpochEra era, string modId)
        {
            ArgumentNullException.ThrowIfNull(epochType);
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ThrowIfNotModEpochTemplate(epochType);

            lock (Sync)
            {
                ThrowIfFrozen();
                EnsureVanillaOccupancySeededLocked();

                ThrowIfLayoutAlreadyRegistered(epochType);

                for (var p = 0; p < MaxAutoPositionScan; p++)
                {
                    var key = ToOccupancyKey(era, p);
                    if (!Occupied.Add(key))
                        continue;

                    LayoutByEpochType[epochType] = new(era, p);
                    return;
                }

                throw new InvalidOperationException(
                    $"No free timeline position in era {(int)era} for '{epochType.Name}' (mod '{modId}') within 0..{MaxAutoPositionScan - 1}.");
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Places the epoch in the nearest available column before <paramref name="anchorEra" /> by integer order.
        ///         Position zero in each candidate column is preferred; if every candidate root is occupied, the nearest column
        ///         with any free position is used.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按整数顺序将纪元放入 <paramref name="anchorEra" /> 之前最近的可用列。会优先检查各候选列的位置
        ///         0；若所有候选列的根位置均被占用，则使用最近且仍有其他空位的列。
        ///     </para>
        /// </summary>
        public static void RegisterAutoTimelineSlotBeforeEraColumn(Type epochType, EpochEra anchorEra, string modId)
        {
            ArgumentNullException.ThrowIfNull(epochType);
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ThrowIfNotModEpochTemplate(epochType);

            lock (Sync)
            {
                ThrowIfFrozen();
                EnsureVanillaOccupancySeededLocked();
                ThrowIfLayoutAlreadyRegistered(epochType);
                RegisterAutoTimelineSlotBeforeEraColumnLocked(epochType, anchorEra, modId);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Behaves like <see cref="RegisterAutoTimelineSlotBeforeEraColumn" />, using the reference epoch's
        ///         <see cref="EpochModel.Era" /> as the anchor.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         行为与 <see cref="RegisterAutoTimelineSlotBeforeEraColumn" /> 相同，但使用参考纪元的
        ///         <see cref="EpochModel.Era" /> 作为锚点。
        ///     </para>
        /// </summary>
        public static void RegisterAutoTimelineSlotBeforeEpochColumn(Type epochType, Type referenceEpochType,
            string modId)
        {
            ArgumentNullException.ThrowIfNull(epochType);
            ArgumentNullException.ThrowIfNull(referenceEpochType);
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ThrowIfNotModEpochTemplate(epochType);
            ThrowIfNotConcreteEpochModel(referenceEpochType, nameof(referenceEpochType));

            lock (Sync)
            {
                ThrowIfFrozen();
                EnsureVanillaOccupancySeededLocked();
                ThrowIfLayoutAlreadyRegistered(epochType);
                var reference = (EpochModel)Activator.CreateInstance(referenceEpochType)!;
                RegisterAutoTimelineSlotBeforeEraColumnLocked(epochType, reference.Era, modId);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Places the epoch in the nearest available column after <paramref name="anchorEra" /> by integer order.
        ///         Position zero in each candidate column is preferred; if every candidate root is occupied, the nearest column
        ///         with any free position is used.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按整数顺序将纪元放入 <paramref name="anchorEra" /> 之后最近的可用列。会优先检查各候选列的位置
        ///         0；若所有候选列的根位置均被占用，则使用最近且仍有其他空位的列。
        ///     </para>
        /// </summary>
        public static void RegisterAutoTimelineSlotAfterEraColumn(Type epochType, EpochEra anchorEra, string modId)
        {
            ArgumentNullException.ThrowIfNull(epochType);
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ThrowIfNotModEpochTemplate(epochType);

            lock (Sync)
            {
                ThrowIfFrozen();
                EnsureVanillaOccupancySeededLocked();
                ThrowIfLayoutAlreadyRegistered(epochType);
                RegisterAutoTimelineSlotAfterEraColumnLocked(epochType, anchorEra, modId);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Places the epoch after the <see cref="EpochModel.Era" /> of <paramref name="referenceEpochType" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将纪元放在 <paramref name="referenceEpochType" /> 的 <see cref="EpochModel.Era" /> 之后。
        ///     </para>
        /// </summary>
        public static void RegisterAutoTimelineSlotAfterEpochColumn(Type epochType, Type referenceEpochType,
            string modId)
        {
            ArgumentNullException.ThrowIfNull(epochType);
            ArgumentNullException.ThrowIfNull(referenceEpochType);
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ThrowIfNotModEpochTemplate(epochType);
            ThrowIfNotConcreteEpochModel(referenceEpochType, nameof(referenceEpochType));

            lock (Sync)
            {
                ThrowIfFrozen();
                EnsureVanillaOccupancySeededLocked();
                ThrowIfLayoutAlreadyRegistered(epochType);
                var reference = (EpochModel)Activator.CreateInstance(referenceEpochType)!;
                RegisterAutoTimelineSlotAfterEraColumnLocked(epochType, reference.Era, modId);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Places the epoch in <paramref name="anchorEra" />'s column at its first free position.
        ///     </para>
        ///     <para xml:lang="zh-CN">将纪元放入 <paramref name="anchorEra" /> 所在列的第一个空位。</para>
        /// </summary>
        public static void RegisterAutoTimelineSlotInEraColumn(Type epochType, EpochEra anchorEra, string modId)
        {
            RegisterAutoTimelineSlot(epochType, anchorEra, modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Places the epoch in the reference epoch's era column at its first free position.
        ///     </para>
        ///     <para xml:lang="zh-CN">将纪元放入参考纪元所在时代列的第一个空位。</para>
        /// </summary>
        public static void RegisterAutoTimelineSlotInEpochColumn(Type epochType, Type referenceEpochType,
            string modId)
        {
            ArgumentNullException.ThrowIfNull(epochType);
            ArgumentNullException.ThrowIfNull(referenceEpochType);
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ThrowIfNotModEpochTemplate(epochType);
            ThrowIfNotConcreteEpochModel(referenceEpochType, nameof(referenceEpochType));

            lock (Sync)
            {
                ThrowIfFrozen();
                EnsureVanillaOccupancySeededLocked();
                ThrowIfLayoutAlreadyRegistered(epochType);
                var reference = (EpochModel)Activator.CreateInstance(referenceEpochType)!;
                var era = reference.Era;

                for (var p = 0; p < MaxAutoPositionScan; p++)
                {
                    var key = ToOccupancyKey(era, p);
                    if (!Occupied.Add(key))
                        continue;

                    LayoutByEpochType[epochType] = new(era, p);
                    return;
                }

                throw new InvalidOperationException(
                    $"No free timeline position in reference era {(int)era} for '{epochType.Name}' (mod '{modId}') within 0..{MaxAutoPositionScan - 1}.");
            }
        }

        private static void RegisterAutoTimelineSlotBeforeEraColumnLocked(Type epochType, EpochEra anchorEra,
            string modId)
        {
            var anchor = (long)(int)anchorEra;
            for (var ei = anchor - 1; ei >= MinEraIntScan; ei--)
                if (TryClaimFirstFreeInColumnLocked(epochType, (EpochEra)(int)ei, true))
                    return;

            for (var ei = anchor - 1; ei >= MinEraIntScan; ei--)
                if (TryClaimFirstFreeInColumnLocked(epochType, (EpochEra)(int)ei, false))
                    return;

            throw new InvalidOperationException(
                $"No free timeline column before anchor era {anchor} for '{epochType.Name}' (mod '{modId}').");
        }

        private static void RegisterAutoTimelineSlotAfterEraColumnLocked(Type epochType, EpochEra anchorEra,
            string modId)
        {
            var anchor = (long)(int)anchorEra;
            for (var ei = anchor + 1; ei <= MaxEraIntScan; ei++)
                if (TryClaimFirstFreeInColumnLocked(epochType, (EpochEra)(int)ei, true))
                    return;

            for (var ei = anchor + 1; ei <= MaxEraIntScan; ei++)
                if (TryClaimFirstFreeInColumnLocked(epochType, (EpochEra)(int)ei, false))
                    return;

            throw new InvalidOperationException(
                $"No free timeline column after anchor era {anchor} for '{epochType.Name}' (mod '{modId}').");
        }

        private static void ThrowIfNotModEpochTemplate(Type epochType)
        {
            if (epochType.IsAbstract || epochType.IsInterface || epochType.ContainsGenericParameters ||
                epochType.GetConstructor(Type.EmptyTypes) == null ||
                !typeof(ModEpochTemplate).IsAssignableFrom(epochType))
                throw new ArgumentException(
                    $"Type '{epochType.Name}' must be a closed concrete {nameof(ModEpochTemplate)} subtype with a " +
                    "public parameterless constructor to use the layout registry.",
                    nameof(epochType));
        }

        private static void ThrowIfNotConcreteEpochModel(Type epochType, string paramName)
        {
            if (epochType.IsAbstract || epochType.IsInterface || epochType.ContainsGenericParameters ||
                epochType.GetConstructor(Type.EmptyTypes) == null ||
                !typeof(EpochModel).IsAssignableFrom(epochType))
                throw new ArgumentException(
                    $"Type '{epochType.Name}' must be a closed concrete {nameof(EpochModel)} subtype with a public " +
                    "parameterless constructor.",
                    paramName);
        }

        internal static EpochEra ResolveEra(Type epochType)
        {
            lock (Sync)
            {
                if (LayoutByEpochType.TryGetValue(epochType, out var layout))
                    return layout.Era;
            }

            throw new InvalidOperationException(
                $"No timeline layout registered for mod epoch type '{epochType?.Name}'. " +
                "Declare .TimelineSlot(era, position), .AutoTimelineSlot(era), .AutoTimelineSlotBeforeColumn / AfterColumn, " +
                "or AutoTimelineSlotBeforeEpochColumn / AutoTimelineSlotAfterEpochColumn inside TimelineColumnPackEntry.Epoch<TEpoch>(...), " +
                $"or use ModContentPackBuilder ModEpoch* timeline helpers / matching {nameof(ModTimelineLayoutRegistry)} methods before freeze.");
        }

        internal static int ResolveEraPosition(Type epochType)
        {
            lock (Sync)
            {
                if (LayoutByEpochType.TryGetValue(epochType, out var layout))
                    return layout.EraPosition;
            }

            throw new InvalidOperationException(
                $"No timeline layout registered for mod epoch type '{epochType?.Name}'.");
        }

        internal static void FreezeAndValidate()
        {
            lock (Sync)
            {
                if (_frozen)
                    return;

                EnsureVanillaOccupancySeededLocked();
                AssertEveryModEpochTemplateHasLayoutLocked();
                _frozen = true;
            }
        }

        private static void ThrowIfFrozen()
        {
            if (_frozen)
                throw new InvalidOperationException("Timeline layout registration is frozen.");
        }

        private static void EnsureVanillaOccupancySeededLocked()
        {
            if (_vanillaSeeded)
                return;

            foreach (var type in typeof(EpochModel).Assembly.GetTypes())
            {
                if (type is not { IsClass: true } || type.IsAbstract || !typeof(EpochModel).IsAssignableFrom(type))
                    continue;

                var instance = (EpochModel)(Activator.CreateInstance(type)
                                            ?? throw new InvalidOperationException(
                                                $"Could not construct built-in Epoch type '{type.FullName}'."));
                Occupied.Add(ToOccupancyKey(instance.Era, instance.EraPosition));
            }

            _vanillaSeeded = true;
        }

        private static void AssertEveryModEpochTemplateHasLayoutLocked()
        {
            foreach (var type in GetRegisteredEpochTypesFromGameDictionary())
            {
                if (!typeof(ModEpochTemplate).IsAssignableFrom(type))
                    continue;

                if (LayoutByEpochType.ContainsKey(type))
                    continue;

                throw new InvalidOperationException(
                    $"Epoch type '{type.Name}' inherits {nameof(ModEpochTemplate)} but has no timeline layout. " +
                    "Add .TimelineSlot, .AutoTimelineSlot, .AutoTimelineSlotBeforeColumn / AfterColumn, or BeforeEpoch / AfterEpoch in your timeline column pack.");
            }
        }

        private static IEnumerable<Type> GetRegisteredEpochTypesFromGameDictionary()
        {
            var field = typeof(EpochModel).GetField("_typeToIdDictionary", BindingFlags.Static | BindingFlags.NonPublic)
                        ?? throw new MissingFieldException(typeof(EpochModel).FullName, "_typeToIdDictionary");

            return field.GetValue(null) is not Dictionary<Type, string> map
                ? throw new InvalidOperationException("EpochModel._typeToIdDictionary is unavailable.")
                : map.Keys;
        }

        private static void ThrowIfLayoutAlreadyRegistered(Type epochType)
        {
            if (LayoutByEpochType.ContainsKey(epochType))
                throw new InvalidOperationException(
                    $"Timeline layout for epoch type '{epochType.Name}' is already registered.");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Claims the first free slot in <paramref name="era" />. When <paramref name="preferPositionZeroOnly" /> is
        ///         <see langword="true" />, only position zero is considered.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         占用 <paramref name="era" /> 中的第一个空闲槽位。<paramref name="preferPositionZeroOnly" /> 为
        ///         <see langword="true" /> 时只检查位置 0。
        ///     </para>
        /// </summary>
        private static bool TryClaimFirstFreeInColumnLocked(Type epochType, EpochEra era, bool preferPositionZeroOnly)
        {
            var positions = preferPositionZeroOnly
                ? [0]
                : Enumerable.Range(0, MaxAutoPositionScan);

            foreach (var p in positions)
            {
                var key = ToOccupancyKey(era, p);
                if (!Occupied.Add(key))
                    continue;

                LayoutByEpochType[epochType] = new(era, p);
                return true;
            }

            return false;
        }

        private static (long EraKey, int Position) ToOccupancyKey(EpochEra era, int position)
        {
            return ((int)era, position);
        }

        private readonly record struct TimelineSlotAssignment(EpochEra Era, int EraPosition);
    }
}
