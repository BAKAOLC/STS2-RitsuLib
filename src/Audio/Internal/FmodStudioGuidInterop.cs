namespace STS2RitsuLib.Audio.Internal
{
    internal static class FmodStudioGuidInterop
    {
        /// <summary>
        ///     <para xml:lang="en">Normalizes a GUID to the braced lowercase form required by the bundled FMOD GDExtension parser.</para>
        ///     <para xml:lang="zh-CN">将 GUID 规范化为随游戏提供的 FMOD GDExtension 解析器所需的带花括号小写形式。</para>
        /// </summary>
        internal static bool TryNormalizeForAddon(string raw, out string bracedLowercase)
        {
            bracedLowercase = string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var trimmed = raw.Trim();
            if (trimmed is ['{', _, ..] && trimmed[^1] == '}')
                trimmed = trimmed[1..^1].Trim();

            if (!Guid.TryParse(trimmed, out var guid))
                return false;

            bracedLowercase = guid.ToString("B");
            return true;
        }
    }
}
