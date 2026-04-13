using System.Reflection;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Timeline.Scaffolding;

namespace STS2RitsuLib.Timeline
{
    /// <summary>
    ///     Central registration for <see cref="ModEpochTemplate" /> timeline placement: <see cref="EpochEra" /> column and
    ///     <c>EraPosition</c> within that column. Vanilla <see cref="EpochModel" /> instances are pre-seeded so mod slots
    ///     cannot silently overlap base-game cells.
    /// </summary>
    public static class ModTimelineLayoutRegistry
    {
        private const int MaxAutoPositionScan = 128;

        /// <summary>Scan downward when placing a column strictly before an anchor era (horizontal / enum int order).</summary>
        private const int MinEraIntScan = -100_000;

        /// <summary>Scan upward when placing a column strictly after an anchor era.</summary>
        private const int MaxEraIntScan = 100_000;

        private static readonly Lock Sync = new();

        private static readonly Dictionary<Type, TimelineSlotAssignment> LayoutByEpochType = [];

        private static readonly HashSet<(long EraKey, int Position)> Occupied = [];

        private static bool _frozen;

        private static bool _vanillaSeeded;

        /// <summary>
        ///     Registers an explicit slot. Throws if the cell is already used by vanilla or another mod registration.
        /// </summary>
        public static void RegisterTimelineSlot(Type epochType, EpochEra era, int eraPosition, string modId)
        {
            ArgumentNullException.ThrowIfNull(epochType);
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            if (!typeof(ModEpochTemplate).IsAssignableFrom(epochType))
                throw new ArgumentException(
                    $"Type '{epochType.Name}' must inherit {nameof(ModEpochTemplate)} to use the layout registry.",
                    nameof(epochType));

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
        ///     Registers the lowest non-negative <c>EraPosition</c> in <paramref name="era" /> that is not occupied by
        ///     vanilla or prior mod registrations.
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
        ///     Places this epoch in the leftmost timeline column whose <see cref="EpochEra" /> integer is strictly less than
        ///     <paramref name="anchorEra" />, preferring <c>EraPosition == 0</c> (a dedicated “root” column), then any free
        ///     slot in that column. Matches vanilla <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Timeline.NTimelineScreen" />
        ///     column ordering (smaller era int = further left).
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
        ///     Same as <see cref="RegisterAutoTimelineSlotBeforeEraColumn" /> but the anchor is the reference epoch’s
        ///     <see cref="EpochModel.Era" /> (its column).
        /// </summary>
        public static void RegisterAutoTimelineSlotBeforeEpochColumn(Type epochType, Type referenceEpochType,
            string modId)
        {
            ArgumentNullException.ThrowIfNull(epochType);
            ArgumentNullException.ThrowIfNull(referenceEpochType);
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ThrowIfNotModEpochTemplate(epochType);
            if (referenceEpochType.IsAbstract || !typeof(EpochModel).IsAssignableFrom(referenceEpochType))
                throw new ArgumentException(
                    $"Type '{referenceEpochType.Name}' must be a concrete {nameof(EpochModel)}.",
                    nameof(referenceEpochType));

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
        ///     Places this epoch in the rightmost practical column with era int strictly greater than
        ///     <paramref name="anchorEra" /> (scanning upward from <c>anchor + 1</c>), preferring position 0.
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
        ///     Anchor is <see cref="EpochModel.Era" /> of <paramref name="referenceEpochType" />.
        /// </summary>
        public static void RegisterAutoTimelineSlotAfterEpochColumn(Type epochType, Type referenceEpochType,
            string modId)
        {
            ArgumentNullException.ThrowIfNull(epochType);
            ArgumentNullException.ThrowIfNull(referenceEpochType);
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ThrowIfNotModEpochTemplate(epochType);
            if (referenceEpochType.IsAbstract || !typeof(EpochModel).IsAssignableFrom(referenceEpochType))
                throw new ArgumentException(
                    $"Type '{referenceEpochType.Name}' must be a concrete {nameof(EpochModel)}.",
                    nameof(referenceEpochType));

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
        ///     Places this epoch into the same era column as <paramref name="anchorEra" />, using the first free position.
        /// </summary>
        public static void RegisterAutoTimelineSlotInEraColumn(Type epochType, EpochEra anchorEra, string modId)
        {
            RegisterAutoTimelineSlot(epochType, anchorEra, modId);
        }

        /// <summary>
        ///     Places this epoch into the same era column as <paramref name="referenceEpochType" />, using the first free
        ///     position.
        /// </summary>
        public static void RegisterAutoTimelineSlotInEpochColumn(Type epochType, Type referenceEpochType,
            string modId)
        {
            ArgumentNullException.ThrowIfNull(epochType);
            ArgumentNullException.ThrowIfNull(referenceEpochType);
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ThrowIfNotModEpochTemplate(epochType);
            if (referenceEpochType.IsAbstract || !typeof(EpochModel).IsAssignableFrom(referenceEpochType))
                throw new ArgumentException(
                    $"Type '{referenceEpochType.Name}' must be a concrete {nameof(EpochModel)}.",
                    nameof(referenceEpochType));

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
            var anchor = (int)anchorEra;
            for (var ei = anchor - 1; ei >= MinEraIntScan; ei--)
                if (TryClaimFirstFreeInColumnLocked(epochType, (EpochEra)ei, true))
                    return;

            for (var ei = anchor - 1; ei >= MinEraIntScan; ei--)
                if (TryClaimFirstFreeInColumnLocked(epochType, (EpochEra)ei, false))
                    return;

            throw new InvalidOperationException(
                $"No free timeline column before anchor era {anchor} for '{epochType.Name}' (mod '{modId}').");
        }

        private static void RegisterAutoTimelineSlotAfterEraColumnLocked(Type epochType, EpochEra anchorEra,
            string modId)
        {
            var anchor = (int)anchorEra;
            for (var ei = anchor + 1; ei <= MaxEraIntScan; ei++)
                if (TryClaimFirstFreeInColumnLocked(epochType, (EpochEra)ei, true))
                    return;

            for (var ei = anchor + 1; ei <= MaxEraIntScan; ei++)
                if (TryClaimFirstFreeInColumnLocked(epochType, (EpochEra)ei, false))
                    return;

            throw new InvalidOperationException(
                $"No free timeline column after anchor era {anchor} for '{epochType.Name}' (mod '{modId}').");
        }

        private static void ThrowIfNotModEpochTemplate(Type epochType)
        {
            if (!typeof(ModEpochTemplate).IsAssignableFrom(epochType))
                throw new ArgumentException(
                    $"Type '{epochType.Name}' must inherit {nameof(ModEpochTemplate)} to use the layout registry.",
                    nameof(epochType));
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

                try
                {
                    var inst = (EpochModel)Activator.CreateInstance(type)!;
                    Occupied.Add(ToOccupancyKey(inst.Era, inst.EraPosition));
                }
                catch
                {
                    // Source-generated or unusual epoch types may not be default-constructible; skip.
                }
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
        ///     Claims the first free slot in <paramref name="era" />; if <paramref name="preferPositionZeroOnly" />, only
        ///     tries position 0.
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
