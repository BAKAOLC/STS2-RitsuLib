using STS2RitsuLib.Content;

namespace STS2RitsuLib.CardTags
{
    /// <summary>
    ///     Declarative card-tag row for content packs: register with <see cref="ModCardTagRegistry" /> in one call.
    /// </summary>
    public sealed record CardTagRegistrationEntry(string Id)
    {
        /// <summary>
        ///     Registers this entry on <paramref name="registry" />.
        /// </summary>
        public void Register(ModCardTagRegistry registry)
        {
            registry.Register(Id);
        }

        /// <summary>
        ///     Builds an owned tag id via <see cref="ModContentRegistry.GetQualifiedCardTagId" /> and registers it.
        /// </summary>
        public static CardTagRegistrationEntry Owned(string modId, string localTagStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localTagStem);

            return new(ModContentRegistry.GetQualifiedCardTagId(modId, localTagStem));
        }
    }
}
