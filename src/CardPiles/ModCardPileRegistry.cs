using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Content;
using STS2RitsuLib.Utils;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers custom card piles for one mod and resolves their definitions and dynamic
    ///         <see cref="PileType" /> values.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为单个模组注册自定义卡牌牌堆，并解析其定义与动态 <see cref="PileType" /> 值。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         A single global <see cref="DynamicEnumValueMinter{TEnum}" /> reserves the high value band
    ///         (<c>[0x4000_0000, 0x7FFF_FFFF]</c>). Registrations are shared process-wide and freeze before
    ///         model initialization.
    ///     </para>
    ///     <para xml:lang="en">
    ///         Use <see cref="For" /> with the owning mod ID. Definitions remain available to global lookup by
    ///         pile ID or dynamic value.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         单个全局 <see cref="DynamicEnumValueMinter{TEnum}" /> 会保留高值区间
    ///         （<c>[0x4000_0000, 0x7FFF_FFFF]</c>）。注册信息在进程内共享，并会在模型初始化前冻结。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使用 <see cref="For" /> 和所属模组 ID 获取注册表。定义仍可通过牌堆 ID 或动态值进行全局查找。
    ///     </para>
    /// </remarks>
    public sealed class ModCardPileRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModCardPileRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ModCardPileDefinition> Definitions =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<PileType, ModCardPileDefinition> DefinitionsByPileType = [];

        private static ModCardPileDefinition[] _combatDefinitionsSnapshot = [];
        private static ModCardPileDefinition[] _definitionsSnapshot = [];

        private readonly Logger _logger;
        private readonly string _modId;
        private string? _freezeReason;

        private ModCardPileRegistry(string modId)
        {
            _modId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets whether card-pile registration has been frozen.</para>
        ///     <para xml:lang="zh-CN">获取卡牌牌堆注册是否已冻结。</para>
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the registry for <paramref name="modId" />, creating it on first use.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="modId" /> 的注册表，并在首次使用时创建。</para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The owning mod ID.</para>
        ///     <para xml:lang="zh-CN">所属模组 ID。</para>
        /// </param>
        public static ModCardPileRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            modId = modId.Trim();

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var existing))
                    return existing;

                var created = new ModCardPileRegistry(modId);
                Registries[modId] = created;
                return created;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Freezes card-pile registration for the remainder of the process.</para>
        ///     <para xml:lang="zh-CN">在当前进程的剩余生命周期内冻结卡牌牌堆注册。</para>
        /// </summary>
        /// <param name="reason">
        ///     <para xml:lang="en">The reason reported in logs and late-registration errors.</para>
        ///     <para xml:lang="zh-CN">写入日志和延迟注册错误的原因。</para>
        /// </param>
        internal static void FreezeRegistrations(string reason)
        {
            ModCardPileRegistry[] snapshot;
            lock (SyncRoot)
            {
                if (IsFrozen)
                    return;

                IsFrozen = true;
                foreach (var registry in Registries.Values)
                    registry._freezeReason = reason;

                snapshot = [.. Registries.Values];
            }

            foreach (var registry in snapshot)
                registry._logger.Info($"[CardPiles] Pile registration is now frozen ({reason}).");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a card pile owned by this registry's mod. Repeating the same local stem for the
        ///         same mod returns the existing definition.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册由此注册表所属模组拥有的卡牌牌堆。同一模组重复注册相同本地名称时返回现有定义。
        ///     </para>
        /// </summary>
        /// <param name="localStem">
        ///     <para xml:lang="en">The identifier local to the owning mod.</para>
        ///     <para xml:lang="zh-CN">所属模组内的本地标识符。</para>
        /// </param>
        /// <param name="spec">
        ///     <para xml:lang="en">The pile configuration.</para>
        ///     <para xml:lang="zh-CN">牌堆配置。</para>
        /// </param>
        public ModCardPileDefinition RegisterOwned(string localStem, ModCardPileSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localStem);
            ArgumentNullException.ThrowIfNull(spec);

            var id = ModContentRegistry.GetQualifiedCardPileId(_modId, localStem);
            return RegisterCore(id, spec);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a card pile with a global ID. Prefer <see cref="RegisterOwned" /> for mod-qualified
        ///         IDs.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用全局 ID 注册卡牌牌堆。需要模组限定 ID 时优先使用 <see cref="RegisterOwned" />。
        ///     </para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">The global pile ID. Cross-mod ownership conflicts are rejected.</para>
        ///     <para xml:lang="zh-CN">全局牌堆 ID。不同模组间的所有权冲突会被拒绝。</para>
        /// </param>
        /// <param name="spec">
        ///     <para xml:lang="en">The pile configuration.</para>
        ///     <para xml:lang="zh-CN">牌堆配置。</para>
        /// </param>
        public ModCardPileDefinition Register(string id, ModCardPileSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(spec);

            return RegisterCore(id, spec);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get a registered definition by pile ID.</para>
        ///     <para xml:lang="zh-CN">尝试按牌堆 ID 获取已注册定义。</para>
        /// </summary>
        public static bool TryGet(string id, out ModCardPileDefinition definition)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            lock (SyncRoot)
            {
                return Definitions.TryGetValue(NormalizeId(id), out definition!);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the registered definition for <paramref name="id" />.</para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="id" /> 的已注册定义。</para>
        /// </summary>
        public static ModCardPileDefinition Get(string id)
        {
            return TryGet(id, out var definition)
                ? definition
                : throw new KeyNotFoundException($"Card pile '{NormalizeId(id)}' is not registered.");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the registered mod card-pile definition represented by a <see cref="PileType" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <see cref="PileType" /> 所表示的已注册模组卡牌牌堆定义。
        ///     </para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">The dynamic card-pile value to resolve.</para>
        ///     <para xml:lang="zh-CN">要解析的动态卡牌牌堆值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The registered mod card-pile definition.</para>
        ///     <para xml:lang="zh-CN">已注册的模组卡牌牌堆定义。</para>
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        ///     <para xml:lang="en"><paramref name="value" /> is not a registered mod card pile.</para>
        ///     <para xml:lang="zh-CN"><paramref name="value" /> 不是已注册的模组卡牌牌堆。</para>
        /// </exception>
        public static ModCardPileDefinition Get(PileType value)
        {
            return TryGetByPileType(value, out var definition)
                ? definition
                : throw new KeyNotFoundException(
                    $"PileType '0x{(int)value:X8}' is not a registered mod card pile.");
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get the ID of the mod that registered <paramref name="pileId" />.</para>
        ///     <para xml:lang="zh-CN">尝试获取注册 <paramref name="pileId" /> 的模组 ID。</para>
        /// </summary>
        public static bool TryGetOwnerModId(string pileId, out string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pileId);

            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(NormalizeId(pileId), out var def))
                {
                    modId = def.ModId;
                    return true;
                }
            }

            modId = string.Empty;
            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to get the registered definition represented by <paramref name="value" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试获取 <paramref name="value" /> 所表示的已注册定义。
        ///     </para>
        /// </summary>
        public static bool TryGetByPileType(PileType value, out ModCardPileDefinition definition)
        {
            lock (SyncRoot)
            {
                return DefinitionsByPileType.TryGetValue(value, out definition!);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether <paramref name="value" /> represents a registered mod card pile.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="value" /> 是否表示已注册的模组卡牌牌堆。
        ///     </para>
        /// </summary>
        public static bool IsModPileType(PileType value)
        {
            lock (SyncRoot)
            {
                return DefinitionsByPileType.ContainsKey(value);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the deterministic <see cref="PileType" /> for <paramref name="id" />. The ID does
        ///         not need to be registered.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="id" /> 对应的确定性 <see cref="PileType" />。该 ID 无需已注册。
        ///     </para>
        /// </summary>
        public static PileType GetPileType(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return DynamicEnumValueRegistry<PileType>.GetValue(id);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to return the deterministic <see cref="PileType" /> for <paramref name="id" />. The ID
        ///         does not need to be registered.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试返回 <paramref name="id" /> 对应的确定性 <see cref="PileType" />。该 ID 无需已注册。
        ///     </para>
        /// </summary>
        public static bool TryGetPileType(string id, out PileType value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            try
            {
                value = DynamicEnumValueRegistry<PileType>.GetValue(id);
                return true;
            }
            catch (InvalidOperationException)
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to resolve a registered pile ID, vanilla <see cref="PileType" /> name, or deterministic
        ///         dynamic value. Registered pile IDs take precedence.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试解析已注册牌堆 ID、原版 <see cref="PileType" /> 名称或确定性动态值。已注册牌堆 ID 优先。
        ///     </para>
        /// </summary>
        public static bool TryResolvePileType(string idOrEnumName, out PileType value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(idOrEnumName);

            if (!TryGet(idOrEnumName, out var definition))
                return Enum.TryParse(idOrEnumName.Trim(), true, out value) || TryGetPileType(idOrEnumName, out value);
            value = definition.PileType;
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to get the registered pile ID represented by <paramref name="value" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试获取 <paramref name="value" /> 所表示的已注册牌堆 ID。
        ///     </para>
        /// </summary>
        public static bool TryGetId(PileType value, out string id)
        {
            lock (SyncRoot)
            {
                if (DefinitionsByPileType.TryGetValue(value, out var def))
                {
                    id = def.Id;
                    return true;
                }
            }

            id = string.Empty;
            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a hover tip for the registered pile with the specified ID.</para>
        ///     <para xml:lang="zh-CN">为指定 ID 的已注册牌堆创建悬停提示。</para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">The registered pile ID.</para>
        ///     <para xml:lang="zh-CN">已注册牌堆 ID。</para>
        /// </param>
        public static HoverTip CreateHoverTip(string id)
        {
            return !TryGet(id, out var definition)
                ? throw new KeyNotFoundException($"Card pile '{NormalizeId(id)}' is not registered.")
                : ModCardPileHoverTipFactory.Create(definition);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a <see cref="HoverTip" /> for the registered mod card pile represented by
        ///         <paramref name="value" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="value" /> 所表示的已注册模组卡牌牌堆创建 <see cref="HoverTip" />。
        ///     </para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">The dynamic card-pile value to present.</para>
        ///     <para xml:lang="zh-CN">要显示的动态卡牌牌堆值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A hover tip using the registered pile presentation.</para>
        ///     <para xml:lang="zh-CN">使用已注册牌堆呈现信息的悬停提示。</para>
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        ///     <para xml:lang="en"><paramref name="value" /> is not a registered mod card pile.</para>
        ///     <para xml:lang="zh-CN"><paramref name="value" /> 不是已注册的模组卡牌牌堆。</para>
        /// </exception>
        public static HoverTip CreateHoverTip(PileType value)
        {
            return ModCardPileHoverTipFactory.Create(Get(value));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a snapshot of all registered definitions ordered by ID using ordinal comparison.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回所有已注册定义的快照，并按 ID 使用序号比较排序。
        ///     </para>
        /// </summary>
        public static ModCardPileDefinition[] GetDefinitionsSnapshot()
        {
            return [.. Volatile.Read(ref _definitionsSnapshot)];
        }

        internal static ModCardPileDefinition[] GetCombatDefinitionsSnapshot()
        {
            return Volatile.Read(ref _combatDefinitionsSnapshot);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns definitions that use <paramref name="style" />, ordered by ID using ordinal
        ///         comparison.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回使用 <paramref name="style" /> 的定义，并按 ID 使用序号比较排序。
        ///     </para>
        /// </summary>
        internal static ModCardPileDefinition[] GetDefinitionsByStyle(ModCardPileUiStyle style)
        {
            lock (SyncRoot)
            {
                return
                [
                    .. Definitions.Values
                        .Where(def => def.Style == style)
                        .OrderBy(def => def.Id, StringComparer.Ordinal),
                ];
            }
        }

        private ModCardPileDefinition RegisterCore(string id, ModCardPileSpec spec)
        {
            EnsureMutable("register card piles");

            var normalizedId = NormalizeId(id);
            var pileType = DynamicEnumValueRegistry<PileType>.Register(_modId, normalizedId).Value;

            var definition = new ModCardPileDefinition(
                _modId,
                normalizedId,
                pileType,
                spec.Scope,
                spec.Style,
                spec.Anchor,
                spec.IconPath,
                spec.Hotkeys,
                spec.CardShouldBeVisible,
                spec.OnOpen,
                spec.HoverTipScreenOffset,
                spec.HoverTipPlacement,
                spec.VisibleWhen,
                spec.FlightTargetPositionResolver,
                spec.FlightStartPositionResolver,
                spec.View,
                spec.ExtraHand);

            lock (SyncRoot)
            {
                EnsureMutable("register card piles");

                if (Definitions.TryGetValue(normalizedId, out var existing))
                {
                    if (!ReferenceEquals(existing.ModId, definition.ModId)
                        && !StringComparer.OrdinalIgnoreCase.Equals(existing.ModId, definition.ModId))
                        throw new InvalidOperationException(
                            $"Card pile '{normalizedId}' is already registered by mod '{existing.ModId}'; "
                            + $"mod '{definition.ModId}' cannot re-register it.");

                    return existing;
                }

                Definitions[normalizedId] = definition;
                DefinitionsByPileType[pileType] = definition;
                RebuildDefinitionSnapshotsLocked();
            }

            _logger.Info($"[CardPiles] Registered pile: {normalizedId} (PileType=0x{(int)pileType:X8}, "
                         + $"Style={spec.Style}, Scope={spec.Scope})");
            return definition;
        }

        private static void RebuildDefinitionSnapshotsLocked()
        {
            var definitions = Definitions.Values
                .OrderBy(definition => definition.Id, StringComparer.Ordinal)
                .ToArray();
            Volatile.Write(ref _definitionsSnapshot, definitions);
            Volatile.Write(ref _combatDefinitionsSnapshot,
                [.. definitions.Where(definition => definition.Scope == ModCardPileScope.CombatOnly)]);
        }

        private void EnsureMutable(string operation)
        {
            if (!IsFrozen)
                return;

            throw new InvalidOperationException(
                $"Cannot {operation} after pile registration has been frozen ({_freezeReason ?? "unknown"}). "
                + "Register piles from your mod initializer before model initialization.");
        }

        // The registry dictionaries use StringComparer.OrdinalIgnoreCase so we do not force a case here —
        // RegisterOwned emits the canonical uppercase form (MODID_CARDPILE_LOCAL) via
        // ModContentRegistry.GetQualifiedCardPileId and Register(string, ...) preserves whatever shape
        // the caller chose. Loc keys use the same id string (vanilla `DRAW_PILE.title` style in static_hover_tips).
        private static string NormalizeId(string id)
        {
            return id.Trim();
        }
    }
}
