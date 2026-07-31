using System.Globalization;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Localization
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Loads ancient-event dialogue lines from localization tables and adds them to
    ///         <c>AncientDialogueSet</c> instances for modded characters.
    ///     </para>
    ///     <para xml:lang="zh-CN">从本地化表加载先古之民事件对话，并将其添加到模组角色的 <c>AncientDialogueSet</c> 实例中。</para>
    /// </summary>
    public static class AncientDialogueLocalization
    {
        private const string AncientLocTable = "ancients";
        private const string ArchitectKey = "THE_ARCHITECT";
        private const string ArchitectBaseKeyPrefix = ArchitectKey + ".talk.";
        private const string AttackKeySuffix = "-attack";
        private const string StartAttackKeySuffix = "-startattack";
        private const string EndAttackKeySuffix = "-endattack";
        private const string VisitIndexKeySuffix = "-visit";

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds the <c>{ancient}.talk.{character}.</c> localization-key prefix for an ancient event and
        ///         character entry ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">为先古之民事件和角色条目 ID 构建 <c>{ancient}.talk.{character}.</c> 本地化键前缀。</para>
        /// </summary>
        public static string BaseLocKey(string ancientEntry, string characterEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ancientEntry);
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            return $"{ancientEntry}.talk.{characterEntry}.";
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Reads the contiguous, zero-based dialogue sequences for an ancient event and character from the
        ///         <c>ancients</c> localization table.
        ///     </para>
        ///     <para xml:lang="zh-CN">从 <c>ancients</c> 本地化表读取指定先古之民事件和角色从零开始、连续编号的对话序列。</para>
        /// </summary>
        public static List<AncientDialogue> GetDialoguesForCharacter(string ancientEntry, CharacterModel character)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ancientEntry);
            ArgumentNullException.ThrowIfNull(character);
            return GetDialoguesForKey(AncientLocTable, BaseLocKey(ancientEntry, character.Id.Entry));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Reads dialogue sequences under <paramref name="baseKey" /> from <paramref name="locTable" />.
        ///         Sequence and line indices must each be contiguous and start at zero; scanning stops at the first missing index.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从 <paramref name="locTable" /> 读取 <paramref name="baseKey" />
        ///         下的对话序列。序列索引和行索引均须从零开始且连续；遇到首个缺失索引时即停止扫描。
        ///     </para>
        /// </summary>
        public static List<AncientDialogue> GetDialoguesForKey(string locTable, string baseKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(locTable);
            ArgumentException.ThrowIfNullOrWhiteSpace(baseKey);

            var dialogues = new List<AncientDialogue>();
            var isArchitect = baseKey.StartsWith(ArchitectBaseKeyPrefix, StringComparison.OrdinalIgnoreCase);

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

                var (startAttackers, endAttackers) =
                    ResolveArchitectAttackers(locTable, baseKey, dialogueIndex, isArchitect);

                dialogues.Add(new([.. sfxPaths])
                {
                    VisitIndex = visitIndex,
                    StartAttackers = startAttackers,
                    EndAttackers = endAttackers,
                });

                dialogueIndex++;
            }

            return dialogues;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds an <see cref="AncientDialogueSet" /> for a modded ancient event from the <c>ancients</c>
        ///         localization table. It reads the first <c>{id}.talk.firstVisitEver.*</c> sequence, all <c>{id}.talk.ANY.*</c>
        ///         sequences, and sequences for each base-game character.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         根据 <c>ancients</c> 本地化表为模组先古之民事件构建 <see cref="AncientDialogueSet" />。该方法读取首个
        ///         <c>{id}.talk.firstVisitEver.*</c> 序列、全部 <c>{id}.talk.ANY.*</c> 序列，以及每个原版角色的对话序列。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Characters registered in <see cref="ModContentRegistry" /> are intentionally omitted.
        ///         RitsuLib's <c>PopulateLocKeys</c> prefix adds their entries once through
        ///         <see cref="AppendCharacterDialogues" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         此处会有意跳过在 <see cref="ModContentRegistry" /> 中注册的角色；RitsuLib 的 <c>PopulateLocKeys</c> 前置补丁会通过
        ///         <see cref="AppendCharacterDialogues" /> 添加一次这些角色的条目。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Appends localization-defined dialogues for <paramref name="characters" /> to
        ///         <paramref name="dialogueSet" /> for <paramref name="ancientEntry" />. Existing dialogues are retained; this
        ///         method does not remove duplicates.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <paramref name="characters" /> 在 <paramref name="ancientEntry" /> 下由本地化定义的对话追加到
        ///         <paramref name="dialogueSet" />。已有对话会被保留，且此方法不会移除重复项。
        ///     </para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The number of <c>AncientDialogue</c> instances appended.</para>
        ///     <para xml:lang="zh-CN">追加的 <c>AncientDialogue</c> 实例数。</para>
        /// </returns>
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
            if (visitLoc == null)
                return currentVisitIndex;

            var rawVisitIndex = visitLoc.GetRawText();
            if (int.TryParse(rawVisitIndex, NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out var parsedVisitIndex)
                && parsedVisitIndex >= 0)
                return parsedVisitIndex;

            AncientDialogueMissingWarnings.WarnOnce(
                $"invalid_visit:{locTable}:{visitLoc.LocEntryKey}:{rawVisitIndex}",
                $"[Ancient] Ignoring invalid visit index '{rawVisitIndex}' in " +
                $"'{locTable}:{visitLoc.LocEntryKey}'; expected a non-negative integer.");
            return currentVisitIndex;
        }

        private static (ArchitectAttackers StartAttackers, ArchitectAttackers EndAttackers) ResolveArchitectAttackers(
            string locTable,
            string baseKey,
            int dialogueIndex,
            bool isArchitect)
        {
            if (!isArchitect)
                return (ArchitectAttackers.None, ArchitectAttackers.None);

            var startAttackers = ArchitectAttackers.None;
            var endAttackers = ArchitectAttackers.Architect;

            TryOverrideAttackers(
                locTable,
                $"{baseKey}{dialogueIndex}{AttackKeySuffix}",
                ref endAttackers);
            TryOverrideAttackers(
                locTable,
                $"{baseKey}{dialogueIndex}{StartAttackKeySuffix}",
                ref startAttackers);
            TryOverrideAttackers(
                locTable,
                $"{baseKey}{dialogueIndex}{EndAttackKeySuffix}",
                ref endAttackers);

            return (startAttackers, endAttackers);
        }

        private static void TryOverrideAttackers(
            string locTable,
            string key,
            ref ArchitectAttackers attackers)
        {
            var locString = LocString.GetIfExists(locTable, key);
            if (locString == null)
                return;

            var rawAttackers = locString.GetRawText();
            if (Enum.TryParse(rawAttackers, true, out ArchitectAttackers parsed)
                && Enum.IsDefined(parsed))
            {
                attackers = parsed;
                return;
            }

            AncientDialogueMissingWarnings.WarnOnce(
                $"invalid_attackers:{locTable}:{key}:{rawAttackers}",
                $"[Ancient] Ignoring invalid Architect attackers value '{rawAttackers}' in '{locTable}:{key}'. " +
                $"Expected one of: {string.Join(", ", Enum.GetNames<ArchitectAttackers>())}.");
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
