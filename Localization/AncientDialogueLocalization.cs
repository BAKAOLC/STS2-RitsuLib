using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Localization
{
    /// <summary>
    ///     Loads ancient dialogue lines from localization tables and merges them into <c>AncientDialogueSet</c>
    ///     instances for mod characters.
    /// </summary>
    public static class AncientDialogueLocalization
    {
        private const string AncientLocTable = "ancients";
        private const string ArchitectKey = "THE_ARCHITECT";
        private const string AttackKeySuffix = "-attack";
        private const string VisitIndexKeySuffix = "-visit";

        /// <summary>
        ///     Builds the localization key prefix for a given ancient and character entry id
        ///     (e.g. <c>{ancient}.talk.{character}.</c>).
        /// </summary>
        public static string BaseLocKey(string ancientEntry, string characterEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ancientEntry);
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            return $"{ancientEntry}.talk.{characterEntry}.";
        }

        /// <summary>
        ///     Reads all dialogue sequences for an ancient and character from the <c>ancients</c> localization table.
        /// </summary>
        public static List<AncientDialogue> GetDialoguesForCharacter(string ancientEntry, CharacterModel character)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ancientEntry);
            ArgumentNullException.ThrowIfNull(character);
            return GetDialoguesForKey(AncientLocTable, BaseLocKey(ancientEntry, character.Id.Entry));
        }

        /// <summary>
        ///     Reads all dialogue sequences under <paramref name="baseKey" /> from the specified
        ///     <paramref name="locTable" />.
        /// </summary>
        public static List<AncientDialogue> GetDialoguesForKey(string locTable, string baseKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(locTable);
            ArgumentException.ThrowIfNullOrWhiteSpace(baseKey);

            var dialogues = new List<AncientDialogue>();
            var isArchitect = baseKey.StartsWith(ArchitectKey, StringComparison.OrdinalIgnoreCase);

            var dialogueIndex = 0;
            var visitIndex = 0;

            while (DialogueExists(locTable, baseKey, dialogueIndex))
            {
                visitIndex = ResolveVisitIndex(locTable, baseKey, dialogueIndex, visitIndex, isArchitect);

                var sfxPaths = new List<string>();
                var lineKey = ExistingLine(locTable, baseKey, dialogueIndex, sfxPaths.Count);
                while (lineKey != null)
                {
                    sfxPaths.Add(GetSfxPath(locTable, lineKey));
                    lineKey = ExistingLine(locTable, baseKey, dialogueIndex, sfxPaths.Count);
                }

                var endAttackers = ResolveArchitectAttackers(locTable, baseKey, dialogueIndex, isArchitect);

                dialogues.Add(new(sfxPaths.ToArray())
                {
                    VisitIndex = visitIndex,
                    EndAttackers = endAttackers,
                });

                dialogueIndex++;
            }

            return dialogues;
        }

        /// <summary>
        ///     Builds a full <see cref="AncientDialogueSet" /> for a mod ancient by scanning the <c>ancients</c> localization
        ///     table (<c>{id}.talk.firstVisitEver.*</c>, <c>{id}.talk.ANY.*</c>, and per-vanilla-character
        ///     <c>{id}.talk.&lt;Character&gt;.*</c>). Lines and SFX keys follow the same rules as
        ///     <see cref="GetDialoguesForKey" />.
        /// </summary>
        /// <remarks>
        ///     Dialogue entries for characters registered in <see cref="ModContentRegistry" /> are omitted here so
        ///     The <c>PopulateLocKeys</c> prefix patch in this library can append them once via
        ///     <see cref="AppendCharacterDialogues" />
        ///     without duplicating lines.
        /// </remarks>
        public static AncientDialogueSet BuildDialogueSetForModAncient(string ancientEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ancientEntry);

            var modCharacterEntries = ModContentRegistry.GetModCharacters()
                .Select(static c => c.Id.Entry)
                .ToHashSet(StringComparer.Ordinal);

            var firstVisitSequences = GetDialoguesForKey(AncientLocTable, BaseLocKey(ancientEntry, "firstVisitEver"));
            var firstVisitEver = firstVisitSequences.Count > 0 ? firstVisitSequences[0] : null;

            var characterDialogues = new Dictionary<string, IReadOnlyList<AncientDialogue>>();
            foreach (var character in ModelDb.AllCharacters)
            {
                if (modCharacterEntries.Contains(character.Id.Entry))
                    continue;

                var forCharacter = GetDialoguesForKey(AncientLocTable, BaseLocKey(ancientEntry, character.Id.Entry));
                if (forCharacter.Count > 0)
                    characterDialogues[character.Id.Entry] = forCharacter;
            }

            var agnostic = GetDialoguesForKey(AncientLocTable, BaseLocKey(ancientEntry, "ANY"));

            return new()
            {
                FirstVisitEverDialogue = firstVisitEver,
                CharacterDialogues = characterDialogues,
                AgnosticDialogues = agnostic,
            };
        }

        /// <summary>
        ///     Appends localization-defined dialogues for each <paramref name="characters" /> entry to
        ///     <paramref name="dialogueSet" /> for <paramref name="ancientEntry" />.
        /// </summary>
        /// <returns>The number of <c>AncientDialogue</c> instances added.</returns>
        public static int AppendCharacterDialogues(
            AncientDialogueSet dialogueSet,
            string ancientEntry,
            IEnumerable<CharacterModel> characters)
        {
            ArgumentNullException.ThrowIfNull(dialogueSet);
            ArgumentException.ThrowIfNullOrWhiteSpace(ancientEntry);
            ArgumentNullException.ThrowIfNull(characters);

            var added = 0;

            foreach (var character in characters)
            {
                if (character == null)
                    continue;

                var newDialogues = GetDialoguesForCharacter(ancientEntry, character);
                if (newDialogues.Count == 0)
                    continue;

                var characterEntry = character.Id.Entry;
                var currentDialogues = dialogueSet.CharacterDialogues.GetValueOrDefault(characterEntry, []);
                dialogueSet.CharacterDialogues[characterEntry] = [.. currentDialogues, .. newDialogues];
                added += newDialogues.Count;
            }

            return added;
        }

        private static string GetSfxPath(string locTable, string dialogueLoc)
        {
            return LocString.GetIfExists(locTable, dialogueLoc + ".sfx")?.GetRawText() ?? string.Empty;
        }

        private static int ResolveVisitIndex(string locTable, string baseKey, int dialogueIndex, int currentVisitIndex,
            bool isArchitect)
        {
            if (isArchitect)
                currentVisitIndex = dialogueIndex;
            else
                currentVisitIndex = dialogueIndex switch
                {
                    0 => 0,
                    1 => 1,
                    2 => 4,
                    _ => currentVisitIndex + 3,
                };

            var visitLoc = LocString.GetIfExists(locTable, $"{baseKey}{dialogueIndex}{VisitIndexKeySuffix}");
            if (visitLoc != null)
                currentVisitIndex = int.Parse(visitLoc.GetRawText());

            return currentVisitIndex;
        }

        private static ArchitectAttackers ResolveArchitectAttackers(
            string locTable,
            string baseKey,
            int dialogueIndex,
            bool isArchitect)
        {
            if (!isArchitect)
                return ArchitectAttackers.None;

            var attackString = LocString.GetIfExists(locTable, $"{baseKey}{dialogueIndex}{AttackKeySuffix}");
            return Enum.TryParse(attackString?.GetRawText(), true, out ArchitectAttackers result)
                ? result
                : ArchitectAttackers.Architect;
        }

        private static bool DialogueExists(string locTable, string baseKey, int index)
        {
            return LocString.Exists(locTable, $"{baseKey}{index}-0.ancient") ||
                   LocString.Exists(locTable, $"{baseKey}{index}-0r.ancient") ||
                   LocString.Exists(locTable, $"{baseKey}{index}-0.char") ||
                   LocString.Exists(locTable, $"{baseKey}{index}-0r.char");
        }

        private static string? ExistingLine(string locTable, string baseKey, int dialogueIndex, int lineIndex)
        {
            var locEntry = $"{baseKey}{dialogueIndex}-{lineIndex}r.ancient";
            if (LocString.Exists(locTable, locEntry)) return locEntry;

            locEntry = $"{baseKey}{dialogueIndex}-{lineIndex}r.char";
            if (LocString.Exists(locTable, locEntry)) return locEntry;

            locEntry = $"{baseKey}{dialogueIndex}-{lineIndex}.ancient";
            if (LocString.Exists(locTable, locEntry)) return locEntry;

            locEntry = $"{baseKey}{dialogueIndex}-{lineIndex}.char";
            return LocString.Exists(locTable, locEntry) ? locEntry : null;
        }
    }
}
