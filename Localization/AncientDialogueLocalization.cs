using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Localization
{
    public static class AncientDialogueLocalization
    {
        private const string AncientLocTable = "ancients";
        private const string ArchitectKey = "THE_ARCHITECT";
        private const string AttackKeySuffix = "-attack";
        private const string VisitIndexKeySuffix = "-visit";

        public static string BaseLocKey(string ancientEntry, string characterEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ancientEntry);
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            return $"{ancientEntry}.talk.{characterEntry}.";
        }

        public static List<AncientDialogue> GetDialoguesForCharacter(string ancientEntry, CharacterModel character)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ancientEntry);
            ArgumentNullException.ThrowIfNull(character);
            return GetDialoguesForKey(AncientLocTable, BaseLocKey(ancientEntry, character.Id.Entry));
        }

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
            if (LocString.Exists(locTable, locEntry)) return locEntry;

            return null;
        }
    }
}
