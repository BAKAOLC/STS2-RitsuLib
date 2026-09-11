namespace STS2RitsuLib.Compat
{
    internal sealed record RitsuModPresentationInfo(
        string Id,
        string Name,
        string? Author,
        string? Version,
        string? Description,
        string? ModImagePath,
        int Rank);
}
