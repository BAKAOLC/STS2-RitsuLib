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
    /// </remarks>
    public sealed class ModCardPileRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModCardPileRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ModCardPileDefinition> Definitions =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<PileType, ModCardPileDefinition> DefinitionsByPileType = [];

        private static readonly DynamicEnumValueMinter<PileType> PileTypeMinter = new();

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
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     Returns the singleton registry for <paramref name="modId" />, creating it on first use.
        /// </summary>
        /// <param name="modId">Owning mod id.</param>
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
        /// </summary>
        /// <param name="reason">Human-readable context appended to log messages.</param>
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
        ///     <c>MODID_CARDPILE_LOCALSTEM</c> shape (uppercase, three segments) that doubles as the default
        ///     <c>LocStem</c>. Passing the same <paramref name="localStem" /> from the same mod returns the
        ///     existing definition.
        /// </summary>
        /// <param name="localStem">Local identifier, unique within this mod.</param>
        /// <param name="spec">Pile metadata (scope, style, localization, icon).</param>
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
        /// </summary>
        /// <param name="id">Global id; collisions across mods are rejected.</param>
        /// <param name="spec">Pile metadata.</param>
        public ModCardPileDefinition Register(string id, ModCardPileSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(spec);

            return RegisterCore(id, spec);
        }

        /// <summary>
        ///     Resolves an existing definition by id (the registry does not mint on lookup).
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
        ///     Resolves the definition for an already minted <see cref="PileType" /> value, returning false for
        ///     vanilla enum members and for values never produced by this registry.
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
        /// </summary>
        public static bool IsModPileType(PileType value)
        {
            lock (SyncRoot)
            {
                return DefinitionsByPileType.ContainsKey(value);
            }
        }

        /// <summary>
        ///     Returns the minted <see cref="PileType" /> for <paramref name="id" /> or throws.
        /// </summary>
        public static PileType GetPileType(string id)
        {
            return TryGet(id, out var definition)
                ? definition.PileType
                : throw new KeyNotFoundException($"Card pile '{NormalizeId(id)}' is not registered.");
        }

        /// <summary>
        ///     Convenience wrapper that mirrors <c>ModKeywordRegistry.CreateHoverTip</c>: builds a
        ///     <see cref="HoverTip" /> from the registered <see cref="ModCardPileDefinition" /> with icon and
        ///     localized title / description.
        /// </summary>
        /// <param name="id">Normalized pile id.</param>
        public static HoverTip CreateHoverTip(string id)
        {
            return !TryGet(id, out var definition)
                ? throw new KeyNotFoundException($"Card pile '{NormalizeId(id)}' is not registered.")
                : ModCardPileHoverTipFactory.Create(definition);
        }

        /// <summary>
        ///     Snapshot of all registered definitions, stable-ordered by id.
        /// </summary>
        internal static ModCardPileDefinition[] GetDefinitionsSnapshot()
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
            var pileType = PileTypeMinter.Mint(normalizedId);

            var definition = new ModCardPileDefinition(
                _modId,
                normalizedId,
                pileType,
                spec.Scope,
                spec.Style,
                spec.Anchor,
                spec.IconPath,
                spec.LocStem,
                spec.Hotkeys,
                spec.CardShouldBeVisible,
                spec.OnOpen);

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
        // the caller chose. Keeping the caller's case means LocStem defaults line up with the visible id
        // and the vanilla `DRAW_PILE.title` convention in static_hover_tips.
        private static string NormalizeId(string id)
        {
            return id.Trim();
        }
    }
}
