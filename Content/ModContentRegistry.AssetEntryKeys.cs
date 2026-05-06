namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        /// <summary>
        ///     Canonical character entry key for replacement / programmatic maps: trimmed,
        ///     <see cref="string.ToUpperInvariant" />. Lookup APIs also probe a legacy lowercase bucket from older
        ///     registrations.
        /// </summary>
        public static string NormalizeCharacterAssetEntryKey(string characterEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            return characterEntry.Trim().ToUpperInvariant();
        }

        /// <summary>
        ///     Canonical model <c>Id.Entry</c> segment for programmatic owned-visual rows. Matching against live models
        ///     remains ordinal-ignore-case.
        /// </summary>
        public static string NormalizeOwnedModelIdEntry(string modelIdEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modelIdEntry);
            return modelIdEntry.Trim().ToUpperInvariant();
        }
    }
}
