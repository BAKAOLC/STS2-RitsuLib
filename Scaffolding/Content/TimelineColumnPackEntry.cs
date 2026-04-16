using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Timeline;
using STS2RitsuLib.Timeline.Scaffolding;
using STS2RitsuLib.Unlocks.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Declarative registration for one <see cref="StoryModel" /> column: epoch order, per-epoch unlock bindings, and
    ///     <see cref="ModTimelineRegistry.RegisterStory{TStory}" /> — in a single fluent block instead of many separate
    ///     <see cref="IModContentPackEntry" /> rows.
    /// </summary>
    public sealed class TimelineColumnPackEntry<TStory> : IModContentPackEntry
        where TStory : StoryModel, new()
    {
        private readonly Action<TimelineColumnBuilder<TStory>> _configure;

        /// <summary>
        ///     Creates an entry that runs <paramref name="configure" /> when the content pack is applied.
        /// </summary>
        public TimelineColumnPackEntry(Action<TimelineColumnBuilder<TStory>> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            _configure = configure;
        }

        /// <inheritdoc />
        public void Apply(ModContentPackContext context)
        {
            var builder = new TimelineColumnBuilder<TStory>(context);
            _configure(builder);
            builder.Run();
        }
    }

    /// <summary>
    ///     Fluent builder for <see cref="TimelineColumnPackEntry{TStory}" />.
    /// </summary>
    public sealed class TimelineColumnBuilder<TStory>
        where TStory : StoryModel, new()
    {
        private readonly ModContentPackContext _context;
        private readonly List<Action> _steps = [];

        internal TimelineColumnBuilder(ModContentPackContext context)
        {
            _context = context;
        }

        internal void Run()
        {
            foreach (var step in _steps)
                step();
        }

        /// <summary>
        ///     Registers <typeparamref name="TEpoch" /> on the story column, then optional slot configuration (pool defaults,
        ///     gated lists, etc.). For <see cref="ModEpochTemplate" /> epochs, timeline layout must be registered before freeze;
        ///     when using this builder, the typical approach is to call
        ///     <see cref="EpochSlotBuilder{TEpoch}.TimelineSlot" /> or <see cref="EpochSlotBuilder{TEpoch}.AutoTimelineSlot" />
        ///     inside <paramref name="slot" /> (or register elsewhere before freeze, e.g. ModContentPackBuilder ModEpoch*
        ///     helpers), so apply-time validation can run (conflicts with vanilla throw at apply time).
        ///     Execution order matches call order; later <c>RequireEpoch</c> for the same model overrides earlier ones.
        /// </summary>
        public TimelineColumnBuilder<TStory> Epoch<TEpoch>(Action<EpochSlotBuilder<TEpoch>>? slot = null)
            where TEpoch : EpochModel, new()
        {
            if (slot != null)
            {
                var b = new EpochSlotBuilder<TEpoch>(_context);
                slot(b);
                foreach (var step in b.DrainSteps())
                    _steps.Add(step);
            }

            _steps.Add(() => _context.Timeline.RegisterStoryEpoch<TStory, TEpoch>());
            return this;
        }

        /// <summary>
        ///     Registers <typeparamref name="TStory" /> for vanilla story discovery (call once at the end of the column).
        /// </summary>
        public TimelineColumnBuilder<TStory> RegisterStory()
        {
            _steps.Add(() => _context.Timeline.RegisterStory<TStory>());
            return this;
        }
    }

    /// <summary>
    ///     Per-epoch unlock hooks for the callback passed to
    ///     <see cref="TimelineColumnBuilder{TStory}" /><c>.Epoch&lt;TEpoch&gt;(...)</c>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         You can call these methods multiple times inside one epoch slot; they run in order. A later
    ///         <c>RequireEpoch</c> for the same model overwrites the earlier epoch binding.
    ///     </para>
    ///     <para>
    ///         Anything registered through <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" /> is gated by
    ///         <see cref="Unlocks.ModUnlockRegistry.FilterUnlocked{TModel}" /> /
    ///         <see cref="Unlocks.ModUnlockRegistry.IsUnlocked" />.
    ///         Integrations include <see cref="CharacterUnlockFilterPatch" />, <see cref="SharedAncientUnlockFilterPatch" />,
    ///         <see cref="CardUnlockFilterPatch" />, <see cref="RelicUnlockFilterPatch" />,
    ///         <see cref="PotionUnlockFilterPatch" />,
    ///         and <see cref="GeneratedRoomEventUnlockFilterPatch" />.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             Cards · gate an entire pool behind this epoch: <c>RequireAllCardsInPool&lt;TCardPool&gt;()</c> (only
    ///             <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" />; does not register
    ///             <see cref="ModEpochGatedContentRegistry" />).
    ///         </item>
    ///         <item>
    ///             Cards · explicit list + pack-declared unlock UI: <c>Cards(types)</c>; whole pool into registry:
    ///             <c>CardsFromPool&lt;TCardPool&gt;()</c>.
    ///         </item>
    ///         <item>Relics · whole pool: <c>RequireAllRelicsInPool&lt;TRelicPool&gt;()</c>.</item>
    ///         <item>Relics · explicit or pool + registry: <c>Relics(types)</c>, <c>RelicsFromPool&lt;TRelicPool&gt;()</c>.</item>
    ///         <item>
    ///             Potions · whole pool: <c>RequireAllPotionsInPool&lt;TPotionPool&gt;()</c> (<c>RequireEpoch</c> only; not
    ///             <see cref="ModEpochGatedContentRegistry" />).
    ///         </item>
    ///         <item>
    ///             Potions · explicit types: <c>Potions(types)</c>. For timeline potion presentation, subclass
    ///             <see cref="PotionUnlockEpochTemplate" /> for your <see cref="EpochModel" /> and implement
    ///             <c>PotionTypes</c>;
    ///             keep those CLR types aligned with <c>Potions</c> / <c>RequireEpoch</c> (this method already applies
    ///             <c>RequireEpoch</c>).
    ///         </item>
    ///     </list>
    /// </remarks>
    public sealed class EpochSlotBuilder<TEpoch>
        where TEpoch : EpochModel, new()
    {
        private readonly ModContentPackContext _context;
        private readonly List<Action> _pending = [];
        private bool? _axisIconEnabled;
        private bool _axisIconRuleSet;
        private string? _axisIconTexturePath;
        private Action? _layoutRegistration;

        internal EpochSlotBuilder(ModContentPackContext context)
        {
            _context = context;
        }

        /// <summary>
        ///     Reserves a fixed <see cref="EpochEra" /> column and <c>EraPosition</c> for this epoch. Conflicts with vanilla
        ///     or other mods throw at registration time.
        /// </summary>
        public EpochSlotBuilder<TEpoch> TimelineSlot(EpochEra era, int eraPosition)
        {
            var modId = _context.ModId;
            _layoutRegistration = () =>
                ModTimelineLayoutRegistry.RegisterTimelineSlot(typeof(TEpoch), era, eraPosition, modId);
            return this;
        }

        /// <summary>
        ///     Reserves the lowest free <c>EraPosition</c> in <paramref name="era" /> after seeding vanilla occupancy.
        /// </summary>
        public EpochSlotBuilder<TEpoch> AutoTimelineSlot(EpochEra era)
        {
            var modId = _context.ModId;
            _layoutRegistration = () =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlot(typeof(TEpoch), era, modId);
            return this;
        }

        /// <summary>
        ///     Reserves a column strictly to the left of <paramref name="anchorEra" /> (smaller era int), preferring a new
        ///     root cell at position 0 — use for a mod story “root” before the rest of your column content.
        /// </summary>
        public EpochSlotBuilder<TEpoch> AutoTimelineSlotBeforeColumn(EpochEra anchorEra)
        {
            var modId = _context.ModId;
            _layoutRegistration = () =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotBeforeEraColumn(typeof(TEpoch), anchorEra, modId);
            return this;
        }

        /// <inheritdoc cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlotBeforeEpochColumn" />
        public EpochSlotBuilder<TEpoch> AutoTimelineSlotBeforeEpochColumn<TReferenceEpoch>()
            where TReferenceEpoch : EpochModel, new()
        {
            var modId = _context.ModId;
            _layoutRegistration = () =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotBeforeEpochColumn(typeof(TEpoch),
                    typeof(TReferenceEpoch), modId);
            return this;
        }

        /// <summary>
        ///     Reserves a column strictly to the right of <paramref name="anchorEra" /> (larger era int).
        /// </summary>
        public EpochSlotBuilder<TEpoch> AutoTimelineSlotAfterColumn(EpochEra anchorEra)
        {
            var modId = _context.ModId;
            _layoutRegistration = () =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotAfterEraColumn(typeof(TEpoch), anchorEra, modId);
            return this;
        }

        /// <inheritdoc cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlotAfterEpochColumn" />
        public EpochSlotBuilder<TEpoch> AutoTimelineSlotAfterEpochColumn<TReferenceEpoch>()
            where TReferenceEpoch : EpochModel, new()
        {
            var modId = _context.ModId;
            _layoutRegistration = () =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotAfterEpochColumn(typeof(TEpoch),
                    typeof(TReferenceEpoch), modId);
            return this;
        }

        /// <summary>
        ///     Reserves a slot in the same era column as <paramref name="anchorEra" />, using the first free position in
        ///     that column.
        /// </summary>
        public EpochSlotBuilder<TEpoch> AutoTimelineSlotInColumn(EpochEra anchorEra)
        {
            var modId = _context.ModId;
            _layoutRegistration = () =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotInEraColumn(typeof(TEpoch), anchorEra, modId);
            return this;
        }

        /// <summary>
        ///     Reserves a slot in the same era column as <typeparamref name="TReferenceEpoch" />, using the first free
        ///     position in that column.
        /// </summary>
        public EpochSlotBuilder<TEpoch> AutoTimelineSlotInEpochColumn<TReferenceEpoch>()
            where TReferenceEpoch : EpochModel, new()
        {
            var modId = _context.ModId;
            _layoutRegistration = () =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotInEpochColumn(typeof(TEpoch),
                    typeof(TReferenceEpoch), modId);
            return this;
        }

        /// <summary>
        ///     Hides the axis icon for this epoch's resolved era column.
        /// </summary>
        public EpochSlotBuilder<TEpoch> DisableEraAxisIcon()
        {
            _axisIconRuleSet = true;
            _axisIconEnabled = false;
            return this;
        }

        /// <summary>
        ///     Enables the axis icon for this epoch's resolved era column.
        /// </summary>
        public EpochSlotBuilder<TEpoch> EnableEraAxisIcon()
        {
            _axisIconRuleSet = true;
            _axisIconEnabled = true;
            return this;
        }

        /// <summary>
        ///     Overrides axis icon texture for this epoch's resolved era column.
        /// </summary>
        public EpochSlotBuilder<TEpoch> EraAxisIcon(string texturePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(texturePath);
            _axisIconRuleSet = true;
            _axisIconEnabled = true;
            _axisIconTexturePath = texturePath;
            return this;
        }

        internal List<Action> DrainSteps()
        {
            var copy = new List<Action>();
            if (_layoutRegistration != null)
                copy.Add(_layoutRegistration);
            if (_axisIconRuleSet)
                copy.Add(ApplyEraIconRule);
            copy.AddRange(_pending);
            _pending.Clear();
            _layoutRegistration = null;
            _axisIconRuleSet = false;
            _axisIconEnabled = null;
            _axisIconTexturePath = null;
            return copy;
        }

        private void ApplyEraIconRule()
        {
            var era = ModTimelineLayoutRegistry.ResolveEra(typeof(TEpoch));
            ModTimelineEraIconRegistry.Configure(era, _axisIconEnabled, _axisIconTexturePath);
        }

        /// <summary>
        ///     Every <see cref="CardModel" /> in <typeparamref name="TPool" /> for this mod requires
        ///     <typeparamref name="TEpoch" />
        ///     (no <see cref="ModEpochGatedContentRegistry" /> row — for default “whole pool until character” style gates).
        /// </summary>
        public EpochSlotBuilder<TEpoch> RequireAllCardsInPool<TPool>()
            where TPool : CardPoolModel
        {
            _pending.Add(() => ModEpochGatedContentPackHelper.ApplyRequireAllPoolCards<TEpoch, TPool>(_context));
            return this;
        }

        /// <summary>
        ///     Every <see cref="RelicModel" /> in <typeparamref name="TPool" /> requires <typeparamref name="TEpoch" />.
        /// </summary>
        public EpochSlotBuilder<TEpoch> RequireAllRelicsInPool<TPool>()
            where TPool : RelicPoolModel
        {
            _pending.Add(() => ModEpochGatedContentPackHelper.ApplyRequireAllPoolRelics<TEpoch, TPool>(_context));
            return this;
        }

        /// <summary>
        ///     Every <see cref="PotionModel" /> in <typeparamref name="TPool" /> requires <typeparamref name="TEpoch" />.
        /// </summary>
        public EpochSlotBuilder<TEpoch> RequireAllPotionsInPool<TPool>()
            where TPool : PotionPoolModel
        {
            _pending.Add(() => ModEpochGatedContentPackHelper.ApplyRequireAllPoolPotions<TEpoch, TPool>(_context));
            return this;
        }

        /// <summary>
        ///     Explicit card types for unlock UI + <c>RequireEpoch</c> (see <see cref="PackDeclaredCardUnlockEpochTemplate" />).
        /// </summary>
        public EpochSlotBuilder<TEpoch> Cards(IReadOnlyList<Type> types)
        {
            ArgumentNullException.ThrowIfNull(types);
            _pending.Add(() =>
                ModEpochGatedContentPackHelper.ApplyExplicitTypes<TEpoch>(_context, types, []));
            return this;
        }

        /// <summary>
        ///     Explicit relic types for unlock UI + <c>RequireEpoch</c>.
        /// </summary>
        public EpochSlotBuilder<TEpoch> Relics(IReadOnlyList<Type> types)
        {
            ArgumentNullException.ThrowIfNull(types);
            _pending.Add(() =>
                ModEpochGatedContentPackHelper.ApplyExplicitTypes<TEpoch>(_context, [], types));
            return this;
        }

        /// <summary>
        ///     Explicit potion types — <c>RequireEpoch</c> only (no <see cref="ModEpochGatedContentRegistry" /> row).
        ///     Pair with <see cref="PotionUnlockEpochTemplate" /> on the epoch if you need timeline potion unlock presentation.
        /// </summary>
        public EpochSlotBuilder<TEpoch> Potions(IReadOnlyList<Type> types)
        {
            ArgumentNullException.ThrowIfNull(types);
            _pending.Add(() => ModEpochGatedContentPackHelper.ApplyExplicitPotions<TEpoch>(_context, types));
            return this;
        }

        /// <summary>
        ///     All relics registered in <typeparamref name="TRelicPool" /> for this mod — registry + <c>RequireEpoch</c>.
        /// </summary>
        public EpochSlotBuilder<TEpoch> RelicsFromPool<TRelicPool>()
            where TRelicPool : RelicPoolModel
        {
            _pending.Add(() => ModEpochGatedContentPackHelper.ApplyRelicsFromPool<TEpoch, TRelicPool>(_context));
            return this;
        }

        /// <summary>
        ///     All cards registered in <typeparamref name="TCardPool" /> for this mod — registry + <c>RequireEpoch</c>.
        /// </summary>
        public EpochSlotBuilder<TEpoch> CardsFromPool<TCardPool>()
            where TCardPool : CardPoolModel
        {
            _pending.Add(() => ModEpochGatedContentPackHelper.ApplyCardsFromPool<TEpoch, TCardPool>(_context));
            return this;
        }
    }
}
