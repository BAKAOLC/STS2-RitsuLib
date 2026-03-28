namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Declarative keyword row for content packs: register with a <see cref="ModKeywordRegistry" /> in one call.
    /// </summary>
    /// <param name="Id">Keyword id (normalized on register).</param>
    /// <param name="TitleTable">Title localization table.</param>
    /// <param name="TitleKey">Title localization key.</param>
    /// <param name="DescriptionTable">Description localization table.</param>
    /// <param name="DescriptionKey">Description localization key.</param>
    /// <param name="IconPath">Optional icon resource path.</param>
    public sealed record KeywordRegistrationEntry(
        string Id,
        string TitleTable,
        string TitleKey,
        string DescriptionTable,
        string DescriptionKey,
        string? IconPath = null)
    {
        /// <summary>
        ///     Registers this entry on <paramref name="registry" />.
        /// </summary>
        public void Register(ModKeywordRegistry registry)
        {
            registry.Register(Id, TitleTable, TitleKey, DescriptionTable, DescriptionKey, IconPath);
        }

        /// <summary>
        ///     Builds a <c>card_keywords</c> entry using <paramref name="locKeyPrefix" /> for title/description keys.
        /// </summary>
        /// <param name="id">Keyword id.</param>
        /// <param name="locKeyPrefix">Prefix for <c>{prefix}.title</c> and <c>{prefix}.description</c> keys.</param>
        /// <param name="iconPath">Optional icon path.</param>
        public static KeywordRegistrationEntry Card(string id, string locKeyPrefix, string? iconPath = null)
        {
            return new(
                id,
                "card_keywords",
                $"{locKeyPrefix}.title",
                "card_keywords",
                $"{locKeyPrefix}.description",
                iconPath);
        }
    }
}
