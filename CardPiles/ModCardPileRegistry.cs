using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Content;
using STS2RitsuLib.Utils;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Per-mod registration surface for custom <see cref="CardPile" />s. Mirrors the conventions used by
    ///     <c>ModKeywordRegistry</c>: ids are mod-qualified via <see cref="ModContentRegistry.GetQualifiedCardPileId" />,
    ///     <see cref="PileType" /> values are deterministically minted with
    ///     <see cref="DynamicEnumValueMinter{TEnum}" />, and registrations freeze at
    ///     <c>ModelDb.Init</c>.
    ///     自定义 <see cref="CardPile" /> 的 per-mod 注册入口。遵循
    ///     <c>ModKeywordRegistry</c> 使用的约定：id 通过 <see cref="ModContentRegistry.GetQualifiedCardPileId" /> 加上 mod 限定，
    ///     <see cref="PileType" /> 值通过
    ///     <see cref="DynamicEnumValueMinter{TEnum}" /> 确定性生成，注册会在
    ///     <c>ModelDb.Init</c> 时冻结。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A single global <see cref="DynamicEnumValueMinter{TEnum}" /> reserves the high value band
    ///         (<c>[0x4000_0000, 0x7FFF_FFFF]</c>), strictly above any plausible vanilla growth. Ritsulib and
    ///         baselib use different hash families (XxHash32 vs MD5) so their minted values do not collide
    ///         numerically even when used side-by-side.
    ///     </para>
    ///     <para>
    ///         Consumers reach the registry through <see cref="For" /> with their own mod id; the registry
    ///         instance is a thin per-mod façade — definitions live in a single process-wide map keyed by the
    ///         normalized id and minted value, so cross-mod lookups by id remain possible.
    ///     </para>
    ///     <para>
    ///         单个全局 <see cref="DynamicEnumValueMinter{TEnum}" /> 会保留高值区间
    ///         （<c>[0x4000_0000, 0x7FFF_FFFF]</c>），严格高于任何合理的原版增长。Ritsulib 和
    ///         baselib 使用不同 hash 家族（XxHash32 与 MD5），因此即使并排使用，它们生成的数值也不会
    ///         在数值上冲突。
    ///     </para>
    ///     <para>
    ///         使用方通过 <see cref="For" /> 和自己的 mod id 取得注册表；该 registry
    ///         实例只是很薄的 per-mod facade；definition 存在单个进程级 map 中，按
    ///         normalized id 和 minted value 索引，因此仍可按 id 进行跨 mod 查找。
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

        private readonly Logger _logger;
        private readonly string _modId;
        private string? _freezeReason;

        private ModCardPileRegistry(string modId)
        {
            _modId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
        }

        /// <summary>
        ///     True after the framework freezes pile registration (at <c>ModelDb.Init</c>).
        ///     框架冻结 pile 注册后为 true（在 <c>ModelDb.Init</c> 时）。
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     Returns the singleton registry for <paramref name="modId" />, creating it on first use.
        ///     返回 <paramref name="modId" /> 的单例注册表，首次使用时创建。
        /// </summary>
        /// <param name="modId">
        ///     Owning mod id.
        ///     所属 mod id。
        /// </param>
        public static ModCardPileRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

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
        ///     Freezes all registries. Called from the core lifecycle patch immediately before
        ///     <c>ModelDb.Init</c> so every subsequent mint / register attempt throws.
        ///     冻结所有注册表。核心生命周期补丁会在
        ///     <c>ModelDb.Init</c> 之前立即调用它，因此之后每次分配/注册尝试都会抛出异常。
        /// </summary>
        /// <param name="reason">
        ///     Human-readable context appended to log messages.
        ///     附加到日志消息中的人类可读上下文。
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
        ///     Registers a card pile owned by this registry's mod. The id is mod-qualified via
        ///     <see cref="ModContentRegistry.GetQualifiedCardPileId" /> — producing the ritsulib-standard
        ///     <c>MODID_CARDPILE_LOCALSTEM</c> shape (uppercase, three segments), also used as the
        ///     <c>static_hover_tips</c> key stem. Passing the same <paramref name="localStem" /> from the same mod returns the
        ///     existing definition.
        ///     注册由此 registry 的 mod 拥有的 card pile。id 会通过
        ///     <see cref="ModContentRegistry.GetQualifiedCardPileId" /> 加上 mod 限定，生成 ritsulib 标准
        ///     <c>MODID_CARDPILE_LOCALSTEM</c> 形状（大写、三段），也用作
        ///     <c>static_hover_tips</c> key stem。同一 mod 传入相同 <paramref name="localStem" /> 会返回
        ///     现有 definition。
        /// </summary>
        /// <param name="localStem">
        ///     Local identifier, unique within this mod.
        ///     此 mod 内唯一的本地标识符。
        /// </param>
        /// <param name="spec">
        ///     Pile metadata (scope, style, localization, icon).
        ///     牌堆元数据（scope、style、localization、icon）。
        /// </param>
        public ModCardPileDefinition RegisterOwned(string localStem, ModCardPileSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localStem);
            ArgumentNullException.ThrowIfNull(spec);

            var id = ModContentRegistry.GetQualifiedCardPileId(_modId, localStem);
            return RegisterCore(id, spec);
        }

        /// <summary>
        ///     Registers a card pile using a raw global id. Prefer <see cref="RegisterOwned" /> to keep ids
        ///     mod-scoped.
        ///     使用原始全局 id 注册卡牌牌堆。优先使用 <see cref="RegisterOwned" /> 以保持 id
        ///     在 mod 作用域内。
        /// </summary>
        /// <param name="id">
        ///     Global id; collisions across mods are rejected.
        ///     Global id；跨 mod 冲突会被拒绝。
        /// </param>
        /// <param name="spec">
        ///     Pile metadata.
        ///     牌堆元数据。
        /// </param>
        public ModCardPileDefinition Register(string id, ModCardPileSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(spec);

            return RegisterCore(id, spec);
        }

        /// <summary>
        ///     Resolves an existing definition by id (the registry does not mint on lookup).
        ///     按 id 解析现有定义（registry 不会在 lookup 时 mint）。
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
        ///     Returns the definition for <paramref name="id" /> or throws <see cref="KeyNotFoundException" />.
        ///     返回 <paramref name="id" /> 的 definition，或抛出 <see cref="KeyNotFoundException" />。
        /// </summary>
        public static ModCardPileDefinition Get(string id)
        {
            return TryGet(id, out var definition)
                ? definition
                : throw new KeyNotFoundException($"Card pile '{NormalizeId(id)}' is not registered.");
        }

        /// <summary>
        ///     Resolves which mod registered <paramref name="pileId" />, if any.
        ///     解析 <paramref name="pileId" /> 由哪个 mod 注册（如果有）。
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
        ///     Resolves the definition for an already minted <see cref="PileType" /> value, returning false for
        ///     vanilla enum members and for values never produced by this registry.
        ///     为已 mint 的 <see cref="PileType" /> 值解析定义；对
        ///     原版 enum 成员以及此 registry 从未生成过的值返回 false。
        /// </summary>
        public static bool TryGetByPileType(PileType value, out ModCardPileDefinition definition)
        {
            lock (SyncRoot)
            {
                return DefinitionsByPileType.TryGetValue(value, out definition!);
            }
        }

        /// <summary>
        ///     Whether <paramref name="value" /> was minted by this registry.
        ///     <paramref name="value" /> 是否由此 registry mint。
        /// </summary>
        public static bool IsModPileType(PileType value)
        {
            lock (SyncRoot)
            {
                return DefinitionsByPileType.ContainsKey(value);
            }
        }

        /// <summary>
        ///     Returns the deterministic <see cref="PileType" /> minted for <paramref name="id" />.
        ///     The id does not need to be registered.
        ///     返回为 <paramref name="id" /> 确定性 minted 的 <see cref="PileType" />。
        ///     该 id 不需要已注册。
        /// </summary>
        public static PileType GetPileType(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return DynamicEnumValueRegistry<PileType>.GetValue(id);
        }

        /// <summary>
        ///     Resolves the deterministic <see cref="PileType" /> minted for <paramref name="id" />.
        ///     The id does not need to be registered.
        ///     解析为 <paramref name="id" /> 确定性 minted 的 <see cref="PileType" />。
        ///     该 id 不需要已注册。
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
        ///     Resolves either a registered mod card-pile id or a vanilla <see cref="PileType" /> enum name.
        ///     Mod ids take precedence when a string could match both.
        ///     解析已注册的 mod 卡牌牌堆 id 或原版 <see cref="PileType" /> enum 名称。
        ///     当字符串两者都能匹配时，mod id 优先。
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
        ///     Tries to resolve the string id that minted <paramref name="value" />.
        ///     尝试解析 mint 出 <paramref name="value" /> 的字符串 id。
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
        ///     Convenience wrapper that mirrors <c>ModKeywordRegistry.CreateHoverTip</c>: builds a
        ///     <see cref="HoverTip" /> from the registered <see cref="ModCardPileDefinition" /> with icon and
        ///     localized title / description.
        ///     与 <c>ModKeywordRegistry.CreateHoverTip</c> 对应的便捷包装：使用已注册的
        ///     <see cref="ModCardPileDefinition" /> 构建带图标和
        ///     本地化 title / description 的 <see cref="HoverTip" />。
        /// </summary>
        /// <param name="id">
        ///     Normalized pile id.
        ///     normalized pile id。
        /// </param>
        public static HoverTip CreateHoverTip(string id)
        {
            return !TryGet(id, out var definition)
                ? throw new KeyNotFoundException($"Card pile '{NormalizeId(id)}' is not registered.")
                : ModCardPileHoverTipFactory.Create(definition);
        }

        /// <summary>
        ///     Snapshot of all registered definitions, stable-ordered by id.
        ///     所有已注册 definition 的快照，按 id 稳定排序。
        /// </summary>
        public static ModCardPileDefinition[] GetDefinitionsSnapshot()
        {
            lock (SyncRoot)
            {
                return Definitions.Values
                    .OrderBy(def => def.Id, StringComparer.Ordinal)
                    .ToArray();
            }
        }

        /// <summary>
        ///     Snapshot of every definition that should own a UI button / container of <paramref name="style" />.
        ///     所有应拥有 <paramref name="style" /> UI button / container 的定义快照。
        /// </summary>
        internal static ModCardPileDefinition[] GetDefinitionsByStyle(ModCardPileUiStyle style)
        {
            lock (SyncRoot)
            {
                return Definitions.Values
                    .Where(def => def.Style == style)
                    .OrderBy(def => def.Id, StringComparer.Ordinal)
                    .ToArray();
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
                spec.FlightStartPositionResolver);

            lock (SyncRoot)
            {
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
            }

            _logger.Info($"[CardPiles] Registered pile: {normalizedId} (PileType=0x{(int)pileType:X8}, "
                         + $"Style={spec.Style}, Scope={spec.Scope})");
            return definition;
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
