namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Immutable registration data for a mod keyword (localization tables, keys, optional icon).
    /// </summary>
    /// <param name="ModId">Owning mod manifest id.</param>
    /// <param name="Id">Normalized keyword id (lowercase).</param>
    /// <param name="TitleTable">Localization table for the title.</param>
    /// <param name="TitleKey">Key for the title string.</param>
    /// <param name="DescriptionTable">Localization table for the body text.</param>
    /// <param name="DescriptionKey">Key for the description string.</param>
    /// <param name="IconPath">Optional Godot resource path for hover icon.</param>
    public sealed record ModKeywordDefinition(
        string ModId,
        string Id,
        string TitleTable,
        string TitleKey,
        string DescriptionTable,
        string DescriptionKey,
        string? IconPath = null);
}
