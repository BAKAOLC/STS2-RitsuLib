using STS2RitsuLib.Scaffolding.Characters;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        private static CharacterAssetProfile? _registeredGlobalCharacterAssetReplacement;

        private static readonly Dictionary<string, CharacterAssetProfile> RegisteredCharacterAssetReplacements =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Registers global asset overrides applied to all characters. Character-specific overrides still win.
        /// </summary>
        public void RegisterGlobalCharacterAssetReplacement(CharacterAssetProfile assetProfile)
        {
            ArgumentNullException.ThrowIfNull(assetProfile);

            EnsureMutable("register global character asset replacement");

            lock (SyncRoot)
            {
                _registeredGlobalCharacterAssetReplacement = _registeredGlobalCharacterAssetReplacement == null
                    ? assetProfile
                    : CharacterAssetProfiles.Merge(_registeredGlobalCharacterAssetReplacement, assetProfile);
            }

            _logger.Info("[Content] Registered global character asset replacement.");
        }

        /// <summary>
        ///     Registers asset overrides for any character id (vanilla or mod), merged field-by-field with existing
        ///     registrations. Later calls win for non-null fields.
        /// </summary>
        public void RegisterCharacterAssetReplacement(string characterEntry, CharacterAssetProfile assetProfile)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            ArgumentNullException.ThrowIfNull(assetProfile);

            EnsureMutable($"register character asset replacement '{characterEntry}'");
            var normalizedEntry = characterEntry.Trim().ToLowerInvariant();

            lock (SyncRoot)
            {
                if (RegisteredCharacterAssetReplacements.TryGetValue(normalizedEntry, out var existing))
                    RegisteredCharacterAssetReplacements[normalizedEntry] =
                        CharacterAssetProfiles.Merge(existing, assetProfile);
                else
                    RegisteredCharacterAssetReplacements[normalizedEntry] = assetProfile;
            }

            _logger.Info($"[Content] Registered character asset replacement for '{normalizedEntry}'.");
        }

        /// <summary>
        ///     Returns merged registered asset overrides for <paramref name="characterEntry" />, if any.
        /// </summary>
        internal static bool TryGetRegisteredCharacterAssetReplacement(
            string characterEntry,
            out CharacterAssetProfile assetProfile)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);

            lock (SyncRoot)
            {
                return RegisteredCharacterAssetReplacements.TryGetValue(characterEntry.Trim(), out assetProfile!);
            }
        }

        /// <summary>
        ///     Returns global asset overrides, if any.
        /// </summary>
        internal static bool TryGetGlobalCharacterAssetReplacement(out CharacterAssetProfile assetProfile)
        {
            lock (SyncRoot)
            {
                if (_registeredGlobalCharacterAssetReplacement == null)
                {
                    assetProfile = CharacterAssetProfile.Empty;
                    return false;
                }

                assetProfile = _registeredGlobalCharacterAssetReplacement;
                return true;
            }
        }

        /// <summary>
        ///     Returns effective overrides for a character: global baseline merged with character-specific overrides.
        /// </summary>
        internal static bool TryGetEffectiveCharacterAssetReplacement(
            string characterEntry,
            out CharacterAssetProfile assetProfile)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);

            var hasGlobal = TryGetGlobalCharacterAssetReplacement(out var globalProfile);
            var hasCharacter = TryGetRegisteredCharacterAssetReplacement(characterEntry, out var characterProfile);

            if (!hasGlobal && !hasCharacter)
            {
                assetProfile = CharacterAssetProfile.Empty;
                return false;
            }

            assetProfile = hasGlobal && hasCharacter
                ? CharacterAssetProfiles.Merge(globalProfile, characterProfile)
                : hasCharacter
                    ? characterProfile
                    : globalProfile;
            return true;
        }

        /// <summary>
        ///     Well-known base-game character ids for
        ///     <see cref="RegisterCharacterAssetReplacement(string,CharacterAssetProfile)" />.
        /// </summary>
        public static class VanillaCharacterIds
        {
            /// <summary>Vanilla Ironclad character id.</summary>
            public const string Ironclad = "ironclad";

            /// <summary>Vanilla Silent character id.</summary>
            public const string Silent = "silent";

            /// <summary>Vanilla Defect character id.</summary>
            public const string Defect = "defect";

            /// <summary>Vanilla Regent character id.</summary>
            public const string Regent = "regent";

            /// <summary>Vanilla Necrobinder character id.</summary>
            public const string Necrobinder = "necrobinder";
        }
    }
}
