using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Timeline;
using STS2RitsuLib.Timeline.Scaffolding;
using STS2RitsuLib.Unlocks.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Declares one <see cref="StoryModel" /> timeline column, including its epoch order, per-epoch unlock bindings,
    ///         and story registration, as one content-pack entry.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将一个 <see cref="StoryModel" /> 时间线列的纪元顺序、逐纪元解锁绑定和故事注册声明为一个内容包条目。
    ///     </para>
    /// </summary>
    public sealed class TimelineColumnPackEntry<TStory> : IModContentPackEntry
        where TStory : StoryModel, new()
    {
        private readonly Action<TimelineColumnBuilder<TStory>> _configure;

        /// <summary>
        ///     <para xml:lang="en">Creates an entry that invokes <paramref name="configure" /> when the content pack is applied.</para>
        ///     <para xml:lang="zh-CN">创建一个在应用内容包时调用 <paramref name="configure" /> 的条目。</para>
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
    ///     <para xml:lang="en">Fluently queues the registrations performed by <see cref="TimelineColumnPackEntry{TStory}" />.</para>
    ///     <para xml:lang="zh-CN">以流式方式排入 <see cref="TimelineColumnPackEntry{TStory}" /> 要执行的注册操作。</para>
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
        ///     <para xml:lang="en">
        ///         Queues optional slot and unlock configuration, then registers <typeparamref name="TEpoch" /> in this story
        ///         column. For <see cref="ModEpochTemplate" /> epochs, register a timeline layout before the timeline freezes,
        ///         normally through <paramref name="slot" />; conflicts with base-game layout are reported during application.
        ///         Operations run in call order, and a later epoch requirement for the same model replaces an earlier one.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         先排入可选的槽位和解锁配置，再将 <typeparamref name="TEpoch" /> 注册到此故事列。对于
        ///         <see cref="ModEpochTemplate" /> 纪元，必须在时间线冻结前注册布局，通常应通过
        ///         <paramref name="slot" /> 完成；与游戏本体布局冲突会在应用时报告。操作按调用顺序执行，
        ///         同一模型后续的纪元要求会替代较早的要求。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Queues <typeparamref name="TStory" /> for base-game story discovery; call it once at the end of
        ///         the column.
        ///     </para>
        ///     <para xml:lang="zh-CN">排入 <typeparamref name="TStory" /> 的游戏本体故事发现注册；应在列末尾调用一次。</para>
        /// </summary>
        public TimelineColumnBuilder<TStory> RegisterStory()
        {
            _steps.Add(() => _context.Timeline.RegisterStory<TStory>());
            return this;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Configures timeline placement, axis icons, and unlock bindings for one epoch callback.</para>
    ///     <para xml:lang="zh-CN">为一个纪元回调配置时间线位置、轴图标和解锁绑定。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         You can call these methods multiple times inside one epoch slot; they run in order. A later
    ///         <c>RequireEpoch</c> for the same model replaces the earlier epoch binding.
    ///         Content registered through <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" /> is gated by
    ///         <see cref="Unlocks.ModUnlockRegistry.FilterUnlocked{TModel}" /> /
    ///         <see cref="Unlocks.ModUnlockRegistry.IsUnlocked" />. Integrations include
    ///         <see cref="CharacterUnlockFilterPatch" />, <see cref="SharedAncientUnlockFilterPatch" />,
    ///         <see cref="CardUnlockFilterPatch" />, <see cref="RelicUnlockFilterPatch" />,
    ///         <see cref="PotionUnlockFilterPatch" />, and <see cref="GeneratedRoomEventUnlockFilterPatch" />.
    ///         <list type="bullet">
    ///             <item>
    ///                 Cards · gate an entire pool behind this epoch: <c>RequireAllCardsInPool&lt;TCardPool&gt;()</c> (only
    ///                 <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" />; does not register
    ///                 <see cref="ModEpochGatedContentRegistry" />).
    ///             </item>
    ///             <item>
    ///                 Cards · explicit list + pack-declared unlock UI: <c>Cards(types)</c>; whole pool into the registry:
    ///                 <c>CardsFromPool&lt;TCardPool&gt;()</c>.
    ///             </item>
    ///             <item>
    ///                 Relics · whole pool: <c>RequireAllRelicsInPool&lt;TRelicPool&gt;()</c>.
    ///             </item>
    ///             <item>
    ///                 Relics · explicit types or pool + registry: <c>Relics(types)</c>,
    ///                 <c>RelicsFromPool&lt;TRelicPool&gt;()</c>.
    ///             </item>
    ///             <item>
    ///                 Potions · whole pool: <c>RequireAllPotionsInPool&lt;TPotionPool&gt;()</c> (<c>RequireEpoch</c> only;
    ///                 does
    ///                 not register <see cref="ModEpochGatedContentRegistry" />).
    ///             </item>
    ///             <item>
    ///                 Potions · explicit types: <c>Potions(types)</c>. For timeline potion presentation, subclass
    ///                 <see cref="PotionUnlockEpochTemplate" /> for your <see cref="EpochModel" /> and implement
    ///                 <c>PotionTypes</c>; keep those CLR types aligned with <c>Potions</c> / <c>RequireEpoch</c> (this
    ///                 method already applies <c>RequireEpoch</c>).
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可以在一个纪元槽位内多次调用这些方法；它们会按顺序运行。后续针对同一模型的
    ///         <c>RequireEpoch</c> 会替换较早的纪元绑定。
    ///         通过 <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" /> 注册的内容，会受到
    ///         <see cref="Unlocks.ModUnlockRegistry.FilterUnlocked{TModel}" /> /
    ///         <see cref="Unlocks.ModUnlockRegistry.IsUnlocked" /> 门控。集成点包括
    ///         <see cref="CharacterUnlockFilterPatch" />、<see cref="SharedAncientUnlockFilterPatch" />、
    ///         <see cref="CardUnlockFilterPatch" />、<see cref="RelicUnlockFilterPatch" />、
    ///         <see cref="PotionUnlockFilterPatch" /> 和 <see cref="GeneratedRoomEventUnlockFilterPatch" />。
    ///         <list type="bullet">
    ///             <item>
    ///                 卡牌 · 将整个池门控在此纪元之后：<c>RequireAllCardsInPool&lt;TCardPool&gt;()</c>（仅注册
    ///                 <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" />；不会注册
    ///                 <see cref="ModEpochGatedContentRegistry" />）。
    ///             </item>
    ///             <item>
    ///                 卡牌 · 显式列表加包声明的解锁界面：<c>Cards(types)</c>；将整个池加入注册表：
    ///                 <c>CardsFromPool&lt;TCardPool&gt;()</c>。
    ///             </item>
    ///             <item>
    ///                 遗物 · 整个池：<c>RequireAllRelicsInPool&lt;TRelicPool&gt;()</c>。
    ///             </item>
    ///             <item>
    ///                 遗物 · 显式类型或池加注册表：<c>Relics(types)</c>、
    ///                 <c>RelicsFromPool&lt;TRelicPool&gt;()</c>。
    ///             </item>
    ///             <item>
    ///                 药水 · 整个池：<c>RequireAllPotionsInPool&lt;TPotionPool&gt;()</c>（仅应用 <c>RequireEpoch</c>；
    ///                 不注册 <see cref="ModEpochGatedContentRegistry" />）。
    ///             </item>
    ///             <item>
    ///                 药水 · 显式类型：<c>Potions(types)</c>。如需时间线药水表现，请为你的
    ///                 <see cref="EpochModel" /> 派生 <see cref="PotionUnlockEpochTemplate" /> 并实现
    ///                 <c>PotionTypes</c>；保持这些 CLR 类型与 <c>Potions</c> / <c>RequireEpoch</c> 对齐（此方法已
    ///                 应用 <c>RequireEpoch</c>）。
    ///             </item>
    ///         </list>
    ///     </para>
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
        ///     <para xml:lang="en">
        ///         Queues a fixed <see cref="EpochEra" /> column and <c>EraPosition</c>; registration conflicts
        ///         throw when applied.
        ///     </para>
        ///     <para xml:lang="zh-CN">排入固定的 <see cref="EpochEra" /> 列和 <c>EraPosition</c>；注册冲突会在应用时抛出。</para>
        /// </summary>
        public EpochSlotBuilder<TEpoch> TimelineSlot(EpochEra era, int eraPosition)
        {
            var modId = _context.ModId;
            _layoutRegistration = () =>
                ModTimelineLayoutRegistry.RegisterTimelineSlot(typeof(TEpoch), era, eraPosition, modId);
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues the lowest free <c>EraPosition</c> in <paramref name="era" /> after base-game occupancy
        ///         is seeded.
        ///     </para>
        ///     <para xml:lang="zh-CN">在设定游戏本体占用后，排入 <paramref name="era" /> 中最低的空闲 <c>EraPosition</c>。</para>
        /// </summary>
        public EpochSlotBuilder<TEpoch> AutoTimelineSlot(EpochEra era)
        {
            var modId = _context.ModId;
            _layoutRegistration = () =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlot(typeof(TEpoch), era, modId);
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues a column left of <paramref name="anchorEra" />, preferring a new root cell at position
        ///         zero.
        ///     </para>
        ///     <para xml:lang="zh-CN">排入位于 <paramref name="anchorEra" /> 左侧的列，优先选择位置零的新根单元格。</para>
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
        ///     <para xml:lang="en">Queues a column strictly to the right of <paramref name="anchorEra" />.</para>
        ///     <para xml:lang="zh-CN">排入严格位于 <paramref name="anchorEra" /> 右侧的列。</para>
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
        ///     <para xml:lang="en">Queues the first free position in the same era column as <paramref name="anchorEra" />.</para>
        ///     <para xml:lang="zh-CN">排入与 <paramref name="anchorEra" /> 相同的时代列中的第一个空闲位置。</para>
        /// </summary>
        public EpochSlotBuilder<TEpoch> AutoTimelineSlotInColumn(EpochEra anchorEra)
        {
            var modId = _context.ModId;
            _layoutRegistration = () =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotInEraColumn(typeof(TEpoch), anchorEra, modId);
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Queues the first free position in <typeparamref name="TReferenceEpoch" />'s era column.</para>
        ///     <para xml:lang="zh-CN">排入 <typeparamref name="TReferenceEpoch" /> 所在时代列中的第一个空闲位置。</para>
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
        ///     <para xml:lang="en">Queues disabling the axis icon for this epoch's resolved era column.</para>
        ///     <para xml:lang="zh-CN">排入禁用此纪元解析出的时代列轴图标的操作。</para>
        /// </summary>
        public EpochSlotBuilder<TEpoch> DisableEraAxisIcon()
        {
            _axisIconRuleSet = true;
            _axisIconEnabled = false;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Queues enabling the axis icon for this epoch's resolved era column.</para>
        ///     <para xml:lang="zh-CN">排入启用此纪元解析出的时代列轴图标的操作。</para>
        /// </summary>
        public EpochSlotBuilder<TEpoch> EnableEraAxisIcon()
        {
            _axisIconRuleSet = true;
            _axisIconEnabled = true;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues an enabled axis icon with <paramref name="texturePath" /> for this epoch's resolved era
        ///         column.
        ///     </para>
        ///     <para xml:lang="zh-CN">为此纪元解析出的时代列排入使用 <paramref name="texturePath" /> 的已启用轴图标。</para>
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
        ///     <para xml:lang="en">
        ///         Queues an epoch requirement for every mod-owned <see cref="CardModel" /> in
        ///         <typeparamref name="TPool" /> without adding a gated-content registry entry.
        ///     </para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TPool" /> 中属于该模组的每张 <see cref="CardModel" /> 排入纪元要求，不添加受限内容注册表条目。</para>
        /// </summary>
        public EpochSlotBuilder<TEpoch> RequireAllCardsInPool<TPool>()
            where TPool : CardPoolModel
        {
            _pending.Add(() => ModEpochGatedContentPackHelper.ApplyRequireAllPoolCards<TEpoch, TPool>(_context));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues an epoch requirement for every mod-owned <see cref="RelicModel" /> in
        ///         <typeparamref name="TPool" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TPool" /> 中属于该模组的每件 <see cref="RelicModel" /> 排入纪元要求。</para>
        /// </summary>
        public EpochSlotBuilder<TEpoch> RequireAllRelicsInPool<TPool>()
            where TPool : RelicPoolModel
        {
            _pending.Add(() => ModEpochGatedContentPackHelper.ApplyRequireAllPoolRelics<TEpoch, TPool>(_context));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues an epoch requirement for every mod-owned <see cref="PotionModel" /> in
        ///         <typeparamref name="TPool" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TPool" /> 中属于该模组的每瓶 <see cref="PotionModel" /> 排入纪元要求。</para>
        /// </summary>
        public EpochSlotBuilder<TEpoch> RequireAllPotionsInPool<TPool>()
            where TPool : PotionPoolModel
        {
            _pending.Add(() => ModEpochGatedContentPackHelper.ApplyRequireAllPoolPotions<TEpoch, TPool>(_context));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Queues explicit card types for unlock UI and their epoch requirements.</para>
        ///     <para xml:lang="zh-CN">为解锁界面和对应的纪元要求排入显式卡牌类型。</para>
        /// </summary>
        public EpochSlotBuilder<TEpoch> Cards(IReadOnlyList<Type> types)
        {
            ArgumentNullException.ThrowIfNull(types);
            _pending.Add(() =>
                ModEpochGatedContentPackHelper.ApplyExplicitTypes<TEpoch>(_context, types, []));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Queues explicit relic types for unlock UI and their epoch requirements.</para>
        ///     <para xml:lang="zh-CN">为解锁界面和对应的纪元要求排入显式遗物类型。</para>
        /// </summary>
        public EpochSlotBuilder<TEpoch> Relics(IReadOnlyList<Type> types)
        {
            ArgumentNullException.ThrowIfNull(types);
            _pending.Add(() =>
                ModEpochGatedContentPackHelper.ApplyExplicitTypes<TEpoch>(_context, [], types));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues epoch requirements for explicit potion types without a gated-content registry entry. Use
        ///         <see cref="PotionUnlockEpochTemplate" /> when the timeline must present those potion unlocks.
        ///     </para>
        ///     <para xml:lang="zh-CN">为显式药水类型排入纪元要求，不添加受限内容注册表条目。若时间线需要展示这些药水解锁，请使用 <see cref="PotionUnlockEpochTemplate" />。</para>
        /// </summary>
        public EpochSlotBuilder<TEpoch> Potions(IReadOnlyList<Type> types)
        {
            ArgumentNullException.ThrowIfNull(types);
            _pending.Add(() => ModEpochGatedContentPackHelper.ApplyExplicitPotions<TEpoch>(_context, types));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues the mod's relics in <typeparamref name="TRelicPool" /> for both the gated-content
        ///         registry and epoch requirements.
        ///     </para>
        ///     <para xml:lang="zh-CN">将该模组在 <typeparamref name="TRelicPool" /> 中的遗物排入受限内容注册表和纪元要求。</para>
        /// </summary>
        public EpochSlotBuilder<TEpoch> RelicsFromPool<TRelicPool>()
            where TRelicPool : RelicPoolModel
        {
            _pending.Add(() => ModEpochGatedContentPackHelper.ApplyRelicsFromPool<TEpoch, TRelicPool>(_context));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues the mod's cards in <typeparamref name="TCardPool" /> for both the gated-content registry
        ///         and epoch requirements.
        ///     </para>
        ///     <para xml:lang="zh-CN">将该模组在 <typeparamref name="TCardPool" /> 中的卡牌排入受限内容注册表和纪元要求。</para>
        /// </summary>
        public EpochSlotBuilder<TEpoch> CardsFromPool<TCardPool>()
            where TCardPool : CardPoolModel
        {
            _pending.Add(() => ModEpochGatedContentPackHelper.ApplyCardsFromPool<TEpoch, TCardPool>(_context));
            return this;
        }
    }
}
