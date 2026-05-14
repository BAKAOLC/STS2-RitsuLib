namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        /// <summary>
        ///     Canonical character entry key for replacement / programmatic maps: trimmed,
        ///     <see cref="string.ToUpperInvariant" />. Lookup APIs also probe a legacy lowercase bucket from older
        ///     registrations.
        ///     替换/程序化映射使用的规范角色条目键：去除首尾空白后
        ///     调用 <see cref="string.ToUpperInvariant" />。查找 API 也会探测旧版
        ///     注册留下的遗留小写桶。
        /// </summary>
        public static string NormalizeCharacterAssetEntryKey(string characterEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            return characterEntry.Trim().ToUpperInvariant();
        }

        /// <summary>
        ///     Canonical model <c>Id.Entry</c> segment for programmatic owned-visual rows. Matching against live models
        ///     remains ordinal-ignore-case.
        ///     程序化所属视觉行使用的规范模型 <c>Id.Entry</c> 段。与实时模型匹配时
        ///     仍使用 ordinal-ignore-case。
        /// </summary>
        public static string NormalizeOwnedModelIdEntry(string modelIdEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modelIdEntry);
            return modelIdEntry.Trim().ToUpperInvariant();
        }
    }
}
