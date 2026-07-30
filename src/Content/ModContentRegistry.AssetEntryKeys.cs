namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Normalizes a character entry key by trimming it and converting it to invariant uppercase.
        ///     </para>
        ///     <para xml:lang="zh-CN">通过去除首尾空白并转换为固定区域性大写来规范化角色条目键。</para>
        /// </summary>
        public static string NormalizeCharacterAssetEntryKey(string characterEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            return characterEntry.Trim().ToUpperInvariant();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Normalizes a model <c>Id.Entry</c> segment by trimming it and converting it to invariant
        ///         uppercase.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过去除首尾空白并转换为固定区域性大写来规范化模型 <c>Id.Entry</c> 段。
        ///     </para>
        /// </summary>
        public static string NormalizeOwnedModelIdEntry(string modelIdEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modelIdEntry);
            return modelIdEntry.Trim().ToUpperInvariant();
        }
    }
}
