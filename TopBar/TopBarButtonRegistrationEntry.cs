using STS2RitsuLib.Content;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     Declarative top-bar-button row for content packs: register with <see cref="ModTopBarButtonRegistry" /> in
    ///     one call.
    /// </summary>
    public sealed record TopBarButtonRegistrationEntry
    {
        /// <summary>
        ///     Creates a raw global top-bar-button registration row.
        /// </summary>
        /// <param name="id">Registered button id.</param>
        /// <param name="spec">Button metadata and click behavior.</param>
        public TopBarButtonRegistrationEntry(string id, ModTopBarButtonSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(spec);

            Id = id;
            Spec = spec;
        }

        /// <summary>
        ///     Registered button id.
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Button metadata and click behavior applied during registration.
        /// </summary>
        public ModTopBarButtonSpec Spec { get; }

        /// <summary>
        ///     Registers this row against <paramref name="registry" />.
        /// </summary>
        public void Register(ModTopBarButtonRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);
            registry.Register(Id, Spec);
        }

        /// <summary>
        ///     Builds an owned button id via <see cref="ModContentRegistry.GetQualifiedTopBarButtonId" /> and registers it.
        /// </summary>
        public static TopBarButtonRegistrationEntry Owned(
            string modId,
            string localButtonStem,
            ModTopBarButtonSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localButtonStem);
            ArgumentNullException.ThrowIfNull(spec);

            return new(ModContentRegistry.GetQualifiedTopBarButtonId(modId, localButtonStem), spec);
        }
    }
}
