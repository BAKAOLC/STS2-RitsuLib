namespace STS2RitsuLib.Keywords
{
    public sealed record ModKeywordDefinition(
        string ModId,
        string Id,
        string TitleTable,
        string TitleKey,
        string DescriptionTable,
        string DescriptionKey,
        string? IconPath = null);
}
