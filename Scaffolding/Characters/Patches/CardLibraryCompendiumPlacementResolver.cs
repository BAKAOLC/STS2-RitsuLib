using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using STS2RitsuLib.Content;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    internal static class CardLibraryCompendiumPlacementResolver
    {
        private const int MaxRelaxationRounds = 64;

        internal static string StableKeyForCharacter(string modelIdEntry)
        {
            return $"CMP_CHAR:{modelIdEntry}";
        }

        internal static string StableKeyForShared(string stableId)
        {
            return $"CMP_SHARED:{stableId}";
        }

        internal static List<PlannedRow> BuildPlannedRows(
            IReadOnlyList<CharacterModel> modCharacters,
            IReadOnlyList<CardLibraryCompendiumSharedPoolFilterRegistration> sharedRegs,
            Logger log)
        {
            var seq = 0;
            var rows = (from character in modCharacters
                where character is not IModCharacterVanillaSelectionPolicy { HideInCardLibraryCompendium: true }
                let rules = GetCharacterPlacementRules(character)
                select new PlannedRow
                {
                    StableKey = StableKeyForCharacter(character.Id.Entry),
                    Character = character,
                    Shared = null,
                    ActiveRules = rules,
                    IsCharacter = true,
                    AppendToStripEnd = false,
                    RegistrationSequence = seq++,
                    ResolvedPool = character.CardPool,
                }).ToList();

            foreach (var reg in sharedRegs)
            {
                CardPoolModel pool;
                try
                {
                    pool = ModelDb.GetById<CardPoolModel>(ModelDb.GetId(reg.CardPoolType));
                }
                catch (Exception ex)
                {
                    log.Warn(
                        $"[CardLibrary] Skipping compendium filter '{reg.StableId}': could not resolve {reg.CardPoolType.Name} — {ex.Message}");
                    continue;
                }

                var hasPlacementRules = reg.PlacementRules is { Count: > 0 };
                var rules = hasPlacementRules
                    ? reg.PlacementRules!
                    : [];
                rows.Add(new()
                {
                    StableKey = StableKeyForShared(reg.StableId),
                    Character = null,
                    Shared = reg,
                    ActiveRules = rules,
                    IsCharacter = false,
                    AppendToStripEnd = false,
                    RegistrationSequence = seq++,
                    ResolvedPool = pool,
                });
            }

            return rows;
        }

        private static IReadOnlyList<CardLibraryCompendiumPlacementRule> GetCharacterPlacementRules(
            CharacterModel character)
        {
            if (character is not IModCharacterCardLibraryCompendiumPlacement placement)
                return CardLibraryCompendiumPlacementDefaults.DefaultCharacterRowRules;
            var custom = placement.CardLibraryCompendiumPlacementRules;
            return custom is { Count: > 0 } ? custom : CardLibraryCompendiumPlacementDefaults.DefaultCharacterRowRules;
        }

        /// <summary>
        ///     Resolves logical insertion indices in <paramref name="strip" />'s coordinate space, applies mod–mod
        ///     constraints, classifies “append to strip end” for shared rows, then <b>stable-sorts</b> mod rows
        ///     (append-end last, else by <see cref="PlannedRow.EffectiveTarget" />, then registration order).
        ///     Physical <see cref="InsertRowsInOrder" /> reuses the same <paramref name="strip" /> to seed the
        ///     working sibling list.
        /// </summary>
        internal static void AssignTargetsAndSort(
            NCardLibrary library,
            Node filterParent,
            CardLibraryCompendiumStripSnapshot strip,
            List<PlannedRow> rows,
            Logger log)
        {
            if (rows.Count == 0)
                return;

            var maxSlot = strip.Count;
            var keyToRow = rows.ToDictionary(static r => r.StableKey, StringComparer.Ordinal);

            foreach (var row in rows)
                row.EffectiveTarget = row.IsCharacter
                    ? ResolveInitialCharacterTarget(library, filterParent, strip, row.ActiveRules)
                    : ResolveInitialSharedTarget(library, filterParent, strip, row.ActiveRules);

            var constraints = CollectModConstraints(rows, log);
            RelaxTargets(keyToRow, maxSlot, constraints, log);

            foreach (var row in rows)
                row.EffectiveTarget = Math.Clamp(row.EffectiveTarget, 0, maxSlot);

            foreach (var row in rows.Where(static row => !row.IsCharacter))
            {
                if (row.ActiveRules.Count == 0)
                {
                    row.AppendToStripEnd = true;
                    continue;
                }

                var vanillaResolved = TryFirstMatchingVanillaRule(library, filterParent, strip, row.ActiveRules, out _);
                var hadModConstraint = constraints.Exists(c => c.FromKey == row.StableKey);
                row.AppendToStripEnd = !vanillaResolved && !hadModConstraint;
            }

            SortPlannedRowsForStableInsertion(rows);
        }

        private static void SortPlannedRowsForStableInsertion(List<PlannedRow> rows)
        {
            rows.Sort(static (a, b) =>
            {
                var aEnd = a is { IsCharacter: false, AppendToStripEnd: true } ? 1 : 0;
                var bEnd = b is { IsCharacter: false, AppendToStripEnd: true } ? 1 : 0;
                if (aEnd != bEnd)
                    return aEnd.CompareTo(bEnd);
                var c = a.EffectiveTarget.CompareTo(b.EffectiveTarget);
                return c != 0 ? c : a.RegistrationSequence.CompareTo(b.RegistrationSequence);
            });
        }

        private static int ResolveInitialCharacterTarget(
            NCardLibrary library,
            Node filterParent,
            CardLibraryCompendiumStripSnapshot strip,
            IReadOnlyList<CardLibraryCompendiumPlacementRule> rules)
        {
            return TryFirstMatchingVanillaRule(library, filterParent, strip, rules, out var idx)
                ? Math.Clamp(idx, 0, strip.Count)
                : ResolveDefaultCharacterFallback(library, filterParent, strip);
        }

        private static int ResolveInitialSharedTarget(
            NCardLibrary library,
            Node filterParent,
            CardLibraryCompendiumStripSnapshot strip,
            IReadOnlyList<CardLibraryCompendiumPlacementRule> rules)
        {
            if (TryFirstMatchingVanillaRule(library, filterParent, strip, rules, out var idx))
                return Math.Clamp(idx, 0, strip.Count);

            if (rules.Count == 0)
                return strip.Count;

            return HasAnyModAnchorRule(rules) ? 0 : strip.Count;
        }

        private static bool HasAnyModAnchorRule(IReadOnlyList<CardLibraryCompendiumPlacementRule> rules)
        {
            foreach (var r in rules)
            {
                if (!string.IsNullOrWhiteSpace(r.ModCharacterModelIdEntry))
                    return true;
                if (!string.IsNullOrWhiteSpace(r.ModSharedCompendiumFilterStableId))
                    return true;
            }

            return false;
        }

        private static bool TryFirstMatchingVanillaRule(
            NCardLibrary library,
            Node filterParent,
            CardLibraryCompendiumStripSnapshot strip,
            IReadOnlyList<CardLibraryCompendiumPlacementRule> rules,
            out int targetIndex)
        {
            foreach (var rule in rules)
            {
                if (string.IsNullOrWhiteSpace(rule.VanillaFilterAnchorUniqueName))
                    continue;

                if (library.GetNodeOrNull<NCardPoolFilter>(rule.VanillaFilterAnchorUniqueName) is not { } anchor)
                    continue;
                if (anchor.GetParent() != filterParent)
                    continue;
                if (!strip.TryGetIndexOfNode(anchor, out var idx))
                    continue;

                targetIndex = rule.Relation == CardLibraryCompendiumFilterInsertRelation.Before ? idx : idx + 1;
                return true;
            }

            targetIndex = 0;
            return false;
        }

        private static int ResolveDefaultCharacterFallback(
            NCardLibrary library,
            Node filterParent,
            CardLibraryCompendiumStripSnapshot strip)
        {
            return TryFirstMatchingVanillaRule(
                library,
                filterParent,
                strip,
                CardLibraryCompendiumPlacementDefaults.DefaultCharacterRowRules,
                out var idx)
                ? Math.Clamp(idx, 0, strip.Count)
                : strip.Count;
        }

        private static List<ModConstraint> CollectModConstraints(
            List<PlannedRow> rows,
            Logger log)
        {
            var list = new List<ModConstraint>();
            foreach (var row in rows)
            foreach (var rule in row.ActiveRules)
                if (!string.IsNullOrWhiteSpace(rule.ModCharacterModelIdEntry))
                {
                    var entry = rule.ModCharacterModelIdEntry.Trim();
                    var match = rows.FirstOrDefault(r =>
                        r.Character is not null &&
                        string.Equals(r.Character.Id.Entry, entry, StringComparison.OrdinalIgnoreCase));
                    if (match is null)
                    {
                        log.Warn(
                            $"[CardLibrary] Placement rule on '{row.StableKey}' references unknown mod character entry " +
                            $"'{rule.ModCharacterModelIdEntry}' (that edge is ignored).");
                        continue;
                    }

                    list.Add(new(row.StableKey, match.StableKey,
                        rule.Relation == CardLibraryCompendiumFilterInsertRelation.After));
                }
                else if (!string.IsNullOrWhiteSpace(rule.ModSharedCompendiumFilterStableId))
                {
                    var sid = rule.ModSharedCompendiumFilterStableId.Trim();
                    var match = rows.FirstOrDefault(r =>
                        r.Shared is not null &&
                        string.Equals(r.Shared.StableId, sid, StringComparison.OrdinalIgnoreCase));
                    if (match is null)
                    {
                        log.Warn(
                            $"[CardLibrary] Placement rule on '{row.StableKey}' references unknown shared filter " +
                            $"'{rule.ModSharedCompendiumFilterStableId}'.");
                        continue;
                    }

                    list.Add(new(row.StableKey, match.StableKey,
                        rule.Relation == CardLibraryCompendiumFilterInsertRelation.After));
                }

            return list;
        }

        private static void RelaxTargets(
            IReadOnlyDictionary<string, PlannedRow> keyToRow,
            int maxSlot,
            List<ModConstraint> constraints,
            Logger log)
        {
            if (constraints.Count == 0)
                return;

            for (var round = 0; round < MaxRelaxationRounds; round++)
            {
                var passChanged = false;
                foreach (var c in constraints)
                {
                    if (!keyToRow.TryGetValue(c.FromKey, out var fromRow) ||
                        !keyToRow.TryGetValue(c.OtherKey, out var otherRow))
                        continue;

                    var before = fromRow.EffectiveTarget;
                    fromRow.EffectiveTarget = c.ItemMustBeAfterOther
                        ? Math.Max(fromRow.EffectiveTarget, otherRow.EffectiveTarget + 1)
                        : Math.Min(fromRow.EffectiveTarget, otherRow.EffectiveTarget - 1);

                    fromRow.EffectiveTarget = Math.Clamp(fromRow.EffectiveTarget, 0, maxSlot);
                    if (fromRow.EffectiveTarget != before)
                        passChanged = true;
                }

                if (!passChanged)
                    return;
            }

            log.Warn(
                "[CardLibrary] Compendium placement constraints did not stabilize within " +
                $"{MaxRelaxationRounds} relaxation rounds; order may be ambiguous. Check for cycles or conflicting rules.");
        }

        /// <summary>
        ///     Inserts built mod pool-filter nodes using the working list seeded from
        ///     <paramref name="strip" />.<see cref="CardLibraryCompendiumStripSnapshot.OriginalSiblingsInOrder" />,
        ///     in the order established by <see cref="SortPlannedRowsForStableInsertion" />.
        /// </summary>
        internal static void InsertRowsInOrder(
            Node filterParent,
            CardLibraryCompendiumStripSnapshot strip,
            List<PlannedRow> orderedRows)
        {
            var siblingOrder = new List<Node>(strip.OriginalSiblingsInOrder);

            for (var i = 0; i < orderedRows.Count; i++)
            {
                var row = orderedRows[i];
                var filter = row.BuiltFilter;
                if (filter is null)
                    continue;

                int at;
                if (row is { IsCharacter: false, AppendToStripEnd: true })
                {
                    at = siblingOrder.Count;
                }
                else
                {
                    var tieBoost = 0;
                    for (var j = 0; j < i; j++)
                        if (orderedRows[j].EffectiveTarget == row.EffectiveTarget
                            && orderedRows[j] is not { IsCharacter: false, AppendToStripEnd: true })
                            tieBoost++;

                    at = Math.Clamp(row.EffectiveTarget + tieBoost, 0, siblingOrder.Count);
                }

                siblingOrder.Insert(at, filter);
                filterParent.AddChild(filter, true);
                filterParent.MoveChild(filter, at);
            }
        }

        internal sealed class PlannedRow
        {
            public required string StableKey { get; init; }
            public CharacterModel? Character { get; init; }
            public CardLibraryCompendiumSharedPoolFilterRegistration? Shared { get; init; }
            public required IReadOnlyList<CardLibraryCompendiumPlacementRule> ActiveRules { get; init; }
            public required bool IsCharacter { get; init; }
            public bool AppendToStripEnd { get; set; }
            public required int RegistrationSequence { get; init; }
            public int EffectiveTarget { get; set; }
            public NCardPoolFilter? BuiltFilter { get; set; }
            public CardPoolModel? ResolvedPool { get; set; }
        }

        private readonly record struct ModConstraint(string FromKey, string OtherKey, bool ItemMustBeAfterOther);
    }
}
