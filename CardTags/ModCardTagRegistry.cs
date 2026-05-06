using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Utils;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib.CardTags
{
    /// <summary>
    ///     Per-mod registration surface for custom <see cref="CardTag" /> values. Ids follow
    ///     <see cref="ModContentRegistry.GetQualifiedCardTagId" />; numeric values are minted with
    ///     <see cref="DynamicEnumValueMinter{TEnum}" /> in the same reserved band as keywords and card piles.
    /// </summary>
    public sealed class ModCardTagRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModCardTagRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ModCardTagDefinition> Definitions =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<CardTag, ModCardTagDefinition> DefinitionsByCardTag = [];

        private static readonly DynamicEnumValueMinter<CardTag> CardTagMinter = new();

        private readonly Logger _logger;
        private readonly string _modId;
        private string? _freezeReason;

        private ModCardTagRegistry(string modId)
        {
            _modId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
        }

        /// <summary>
        ///     True after the framework freezes tag registration (at <c>ModelDb.Init</c>).
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     Returns the singleton registry for <paramref name="modId" />, creating it on first use.
        /// </summary>
        public static ModCardTagRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var existing))
                    return existing;

                var created = new ModCardTagRegistry(modId);
                Registries[modId] = created;
                return created;
            }
        }

        internal static void FreezeRegistrations(string reason)
        {
            ModCardTagRegistry[] snapshot;
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
                registry._logger.Info($"[CardTags] Card tag registration is now frozen ({reason}).");
        }

        /// <summary>
        ///     Registers a tag owned by this registry’s mod using <see cref="ModContentRegistry.GetQualifiedCardTagId" />.
        /// </summary>
        public ModCardTagDefinition RegisterOwned(string localTagStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localTagStem);

            var id = ModContentRegistry.GetQualifiedCardTagId(_modId, localTagStem);
            return RegisterCore(id);
        }

        /// <summary>
        ///     Registers a tag with a raw global id. Prefer <see cref="RegisterOwned" /> for mod-scoped ids.
        /// </summary>
        public ModCardTagDefinition Register(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            return RegisterCore(id);
        }

        /// <summary>
        ///     Resolves which mod registered <paramref name="tagId" />, if any.
        /// </summary>
        public static bool TryGetOwnerModId(string tagId, out string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tagId);

            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(NormalizeId(tagId), out var def))
                {
                    modId = def.ModId;
                    return true;
                }
            }

            modId = string.Empty;
            return false;
        }

        /// <summary>
        ///     Tries to resolve a definition by qualified or raw id.
        /// </summary>
        public static bool TryGet(string id, out ModCardTagDefinition definition)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            lock (SyncRoot)
            {
                return Definitions.TryGetValue(NormalizeId(id), out definition!);
            }
        }

        /// <summary>
        ///     Returns the definition for <paramref name="id" /> or throws <see cref="KeyNotFoundException" />.
        /// </summary>
        public static ModCardTagDefinition Get(string id)
        {
            return TryGet(id, out var definition)
                ? definition
                : throw new KeyNotFoundException($"Card tag '{NormalizeId(id)}' is not registered.");
        }

        /// <summary>
        ///     Reverse lookup for a minted <see cref="CardTag" /> value.
        /// </summary>
        public static bool TryGetByCardTag(CardTag value, out ModCardTagDefinition definition)
        {
            lock (SyncRoot)
            {
                return DefinitionsByCardTag.TryGetValue(value, out definition!);
            }
        }

        /// <summary>
        ///     Whether <paramref name="value" /> was minted by this registry (not a vanilla literal).
        /// </summary>
        public static bool IsModCardTag(CardTag value)
        {
            lock (SyncRoot)
            {
                return DefinitionsByCardTag.ContainsKey(value);
            }
        }

        /// <summary>
        ///     Resolves the minted <see cref="CardTag" /> for <paramref name="id" />.
        /// </summary>
        public static bool TryGetCardTag(string id, out CardTag value)
        {
            if (TryGet(id, out var definition))
            {
                value = definition.CardTagValue;
                return true;
            }

            value = CardTag.None;
            return false;
        }

        /// <summary>
        ///     Returns the minted <see cref="CardTag" /> for <paramref name="id" /> or throws.
        /// </summary>
        public static CardTag GetCardTag(string id)
        {
            return Get(id).CardTagValue;
        }

        /// <summary>
        ///     Tries to resolve the string id that minted <paramref name="value" />.
        /// </summary>
        public static bool TryGetId(CardTag value, out string id)
        {
            lock (SyncRoot)
            {
                if (DefinitionsByCardTag.TryGetValue(value, out var def))
                {
                    id = def.Id;
                    return true;
                }
            }

            id = string.Empty;
            return false;
        }

        /// <summary>
        ///     Snapshot of all registered card-tag definitions, stable-ordered by id.
        /// </summary>
        public static ModCardTagDefinition[] GetDefinitionsSnapshot()
        {
            lock (SyncRoot)
            {
                return Definitions.Values
                    .OrderBy(def => def.Id, StringComparer.Ordinal)
                    .ToArray();
            }
        }

        private ModCardTagDefinition RegisterCore(string id)
        {
            EnsureMutable("register card tags");

            var normalizedId = NormalizeId(id);
            var cardTagValue = CardTagMinter.Mint(normalizedId);
            var definition = new ModCardTagDefinition(_modId, normalizedId, cardTagValue);

            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(normalizedId, out var existing))
                {
                    if (!StringComparer.OrdinalIgnoreCase.Equals(existing.ModId, definition.ModId))
                        throw new InvalidOperationException(
                            $"Card tag '{normalizedId}' is already registered by mod '{existing.ModId}'; "
                            + $"mod '{definition.ModId}' cannot re-register it.");

                    return existing;
                }

                Definitions[normalizedId] = definition;
                DefinitionsByCardTag[cardTagValue] = definition;
            }

            _logger.Info($"[CardTags] Registered tag: {normalizedId} (CardTag=0x{(int)cardTagValue:X8})");
            return definition;
        }

        private void EnsureMutable(string operation)
        {
            if (!IsFrozen)
                return;

            throw new InvalidOperationException(
                $"Cannot {operation} after card tag registration has been frozen ({_freezeReason ?? "unknown"}). "
                + "Register tags from your mod initializer before model initialization.");
        }

        private static string NormalizeId(string id)
        {
            return id.Trim();
        }
    }
}
