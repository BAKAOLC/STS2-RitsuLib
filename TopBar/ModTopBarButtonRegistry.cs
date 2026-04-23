using STS2RitsuLib.Content;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     Per-mod registration surface for extra top-bar buttons. Mirrors the ergonomics of
    ///     <c>ModCardPileRegistry</c> (fluent <c>For(modId).RegisterOwned(localStem, spec)</c>), but is
    ///     fully decoupled from the card-pile subsystem — the button only knows how to show an icon,
    ///     expose a hover-tip and run a click callback.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Ids follow the ritsulib <c>MODID_CATEGORY_TYPENAME</c> public-entry convention via
    ///         <see cref="ModContentRegistry.GetQualifiedTopBarButtonId" /> (middle segment fixed to
    ///         <c>TOPBARBUTTON</c>). The registered id doubles as the default <c>LocStem</c> used to
    ///         resolve <c>static_hover_tips</c> title / description keys.
    ///     </para>
    ///     <para>
    ///         Registrations do not need to be frozen alongside <c>ModelDb</c>: top-bar buttons are mounted
    ///         when the top bar node is ready, which happens after model init. The registry therefore
    ///         simply de-duplicates by id (same mod re-registering the same stem returns the existing
    ///         definition).
    ///     </para>
    /// </remarks>
    public sealed class ModTopBarButtonRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModTopBarButtonRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ModTopBarButtonDefinition> Definitions =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Logger _logger;
        private readonly string _modId;

        private ModTopBarButtonRegistry(string modId)
        {
            _modId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
        }

        /// <summary>Returns the singleton registry for <paramref name="modId" />, creating it on first use.</summary>
        public static ModTopBarButtonRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var existing))
                    return existing;

                var created = new ModTopBarButtonRegistry(modId);
                Registries[modId] = created;
                return created;
            }
        }

        /// <summary>
        ///     Registers a top-bar button owned by this registry's mod. The id is produced by
        ///     <see cref="ModContentRegistry.GetQualifiedTopBarButtonId" /> — passing the same
        ///     <paramref name="localStem" /> twice returns the existing definition.
        /// </summary>
        public ModTopBarButtonDefinition RegisterOwned(string localStem, ModTopBarButtonSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localStem);
            ArgumentNullException.ThrowIfNull(spec);

            var id = ModContentRegistry.GetQualifiedTopBarButtonId(_modId, localStem);
            return RegisterCore(id, spec);
        }

        /// <summary>
        ///     Registers a top-bar button using a raw global id. Prefer <see cref="RegisterOwned" /> to
        ///     keep ids mod-scoped.
        /// </summary>
        public ModTopBarButtonDefinition Register(string id, ModTopBarButtonSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(spec);

            return RegisterCore(id, spec);
        }

        /// <summary>Looks up a definition by id; returns false when the id is unknown.</summary>
        public static bool TryGet(string id, out ModTopBarButtonDefinition definition)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            lock (SyncRoot)
            {
                return Definitions.TryGetValue(id.Trim(), out definition!);
            }
        }

        /// <summary>
        ///     Snapshot of every registered button, ordered by <see cref="ModTopBarButtonDefinition.Order" />
        ///     then id for stability.
        /// </summary>
        public static ModTopBarButtonDefinition[] GetDefinitionsSnapshot()
        {
            lock (SyncRoot)
            {
                return Definitions.Values
                    .OrderBy(def => def.Order)
                    .ThenBy(def => def.Id, StringComparer.Ordinal)
                    .ToArray();
            }
        }

        private ModTopBarButtonDefinition RegisterCore(string id, ModTopBarButtonSpec spec)
        {
            var normalizedId = id.Trim();
        if (spec.OnClick == null)
            throw new InvalidOperationException(
                $"Top-bar button '{normalizedId}' must provide a non-null OnClick handler.");

            var definition = new ModTopBarButtonDefinition(
                _modId,
                normalizedId,
                spec.IconPath,
                spec.LocStem,
                spec.Order,
                spec.Offset,
                spec.OnClick,
                spec.VisibleWhen,
                spec.IsOpenWhen,
                spec.CountProvider);

            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(normalizedId, out var existing))
                {
                    if (!StringComparer.OrdinalIgnoreCase.Equals(existing.ModId, definition.ModId))
                        throw new InvalidOperationException(
                            $"Top-bar button '{normalizedId}' is already registered by mod '{existing.ModId}'; "
                            + $"mod '{definition.ModId}' cannot re-register it.");

                    return existing;
                }

                Definitions[normalizedId] = definition;
            }

            _logger.Info($"[TopBar] Registered top-bar button: {normalizedId} (Order={spec.Order})");
            return definition;
        }
    }
}
