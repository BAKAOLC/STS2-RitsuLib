namespace STS2RitsuLib.Search.Pinyin
{
    internal sealed record PinyinSearchDataSource(
        string UnicodeVersion,
        Uri SourceUri,
        long ExpectedLength,
        string ExpectedSha256)
    {
        internal static PinyinSearchDataSource Current { get; } = new(
            "17.0.0",
            new("https://www.unicode.org/Public/17.0.0/ucd/Unihan.zip"),
            8_518_517,
            "F7A48B2B545ACFAA77B2D607AE28747404CE02BAEFEE16396C5D2D7A8EF34B5E");
    }
}
