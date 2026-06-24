using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Rngs;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Checksums;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Networking.StateDivergence
{
    internal static class StateDivergenceDiagnosticReportBuilder
    {
        private const int MaxNestedSavedCardDepth = 3;
        private const string MinionLibComponentBlobPropertyName = "MinionLibComponentStateBlob";

        public static StateDivergenceDiagnosticReport Build(
            StateDivergenceTrackedState local,
            StateDivergenceMessage remoteMessage,
            ulong remotePeerId,
            string role)
        {
            var remote = new StateDivergenceTrackedState(
                remoteMessage.senderChecksum,
                L("value.remoteContext", "Remote divergence message"),
                remoteMessage.senderCombatState);
            var localSupplement = StateDivergenceSupplementPayloadCodec.CreateLocalSnapshot(local.Checksum);
            var hasRemoteSupplement = StateDivergenceSupplementStore.TryTake(remote.Checksum, out var remoteSupplement);

            var sections = BuildSections(local, remote, remotePeerId, role, localSupplement,
                hasRemoteSupplement ? remoteSupplement : null, false);
            var exportSections = BuildSections(local, remote, remotePeerId, role, localSupplement,
                hasRemoteSupplement ? remoteSupplement : null, true);
            var hasLocalContentMods =
                ContentModInventoryPayloadCodec.TryDecode(localSupplement.ContentMods, out var localContentMods);
            IReadOnlyList<ContentModInventoryEntry> remoteContentMods = [];
            var hasRemoteContentMods = hasRemoteSupplement &&
                                       ContentModInventoryPayloadCodec.TryDecode(remoteSupplement.ContentMods,
                                           out remoteContentMods);
            var issueCount = sections.Sum(s => s.Rows.Count);
            if (issueCount == 0)
                sections.Add(new(
                    L("section.hiddenState.title", "No visible state fields differ"),
                    L("section.hiddenState.description",
                        "The checksum differs, but the serialized combat state fields available to the diagnostic view match. This usually points to state outside NetFullCombatState, a serialization-only difference, or a mod-owned hidden state."),
                    false,
                    []));

            return new(
                L("title", "State divergence diagnostics"),
                F("summary", "{0} differing field(s) found for checksum #{1}.", issueCount, local.Checksum.id),
                role,
                remotePeerId,
                new(local.Checksum.id, local.Checksum.checksum, local.Context),
                new(remote.Checksum.id, remote.Checksum.checksum, remote.Context),
                sections,
                exportSections,
                hasLocalContentMods ? localContentMods : [],
                hasRemoteContentMods ? remoteContentMods : [],
                hasRemoteContentMods,
                localSupplement.Progress,
                hasRemoteSupplement ? remoteSupplement.Progress : null,
                local.FullState.ToString(),
                remote.FullState.ToString());
        }

        private static List<StateDivergenceDiagnosticSection> BuildSections(
            StateDivergenceTrackedState local,
            StateDivergenceTrackedState remote,
            ulong remotePeerId,
            string role,
            StateDivergenceSupplementPayload localSupplement,
            StateDivergenceSupplementPayload? remoteSupplement,
            bool includeMatching)
        {
            return
            [
                BuildOverview(local, remote, remotePeerId, role, includeMatching),
                BuildProtocolMaps(localSupplement, remoteSupplement, includeMatching),
                BuildSynchronizers(local.FullState, remote.FullState, includeMatching),
                BuildCreatures(local.FullState, remote.FullState, includeMatching),
                BuildPlayers(local.FullState, remote.FullState, includeMatching),
                BuildRng(local.FullState, remote.FullState, includeMatching),
                BuildRelicGrabBags(local.FullState, remote.FullState, includeMatching),
            ];
        }

        private static StateDivergenceDiagnosticSection BuildProtocolMaps(
            StateDivergenceSupplementPayload local,
            StateDivergenceSupplementPayload? remote,
            bool includeMatching)
        {
            var rows = new List<StateDivergenceDiagnosticRow>();

            if (remote == null)
            {
                rows.Add(new(
                    "savedProperties.supplement",
                    L("value.present", "Present"),
                    L("value.missing", "Missing"),
                    L("detail.missingSupplement",
                        "The remote peer did not include a RitsuLib state-divergence supplement payload.")));
            }
            else
            {
                AddIfDifferent(rows, "savedProperties.netIdBitSize",
                    local.SavedPropertyNetIdBitSize, remote.SavedPropertyNetIdBitSize, includeMatching);
                AddIfDifferent(rows, "savedProperties.count",
                    local.SavedPropertyNames.Count, remote.SavedPropertyNames.Count, includeMatching);
                AddIfDifferent(rows, "savedProperties.mapHash",
                    FormatHash(local.SavedPropertyMapHash), FormatHash(remote.SavedPropertyMapHash), includeMatching);
                AddSavedPropertyMapRows(rows, local.SavedPropertyNames, remote.SavedPropertyNames, includeMatching);
            }

            return new(
                L("section.protocolMaps.title", "Protocol maps"),
                L("section.protocolMaps.description",
                    "RitsuLib divergence supplement data that affects packet decoding, including SavedProperty net-id maps."),
                rows.Count == 0,
                rows);
        }

        private static void AddSavedPropertyMapRows(
            ICollection<StateDivergenceDiagnosticRow> rows,
            IReadOnlyList<string> local,
            IReadOnlyList<string> remote,
            bool includeMatching)
        {
            var count = Math.Max(local.Count, remote.Count);
            var localLines = new List<string>();
            var remoteLines = new List<string>();

            for (var i = 0; i < count; i++)
            {
                var l = i < local.Count ? local[i] : L("value.missing", "Missing");
                var r = i < remote.Count ? remote[i] : L("value.missing", "Missing");
                if (!includeMatching && string.Equals(l, r, StringComparison.Ordinal))
                    continue;

                localLines.Add($"{i:0000}  {l}");
                remoteLines.Add($"{i:0000}  {r}");
            }

            if (localLines.Count == 0)
                return;

            rows.Add(new(
                "savedProperties.netIdMap",
                string.Join(Environment.NewLine, localLines),
                string.Join(Environment.NewLine, remoteLines),
                F("detail.savedPropertyMapMismatch", "{0} SavedProperty net-id slot(s) differ.",
                    localLines.Count)));
        }

        private static StateDivergenceDiagnosticSection BuildOverview(
            StateDivergenceTrackedState local,
            StateDivergenceTrackedState remote,
            ulong remotePeerId,
            string role,
            bool includeMatching)
        {
            var rows = new List<StateDivergenceDiagnosticRow>();
            AddIfDifferent(rows, "checksum.id", local.Checksum.id, remote.Checksum.id, includeMatching);
            AddIfDifferent(rows, "checksum.value", local.Checksum.checksum, remote.Checksum.checksum,
                includeMatching);
            rows.Add(new("context.local", local.Context, remote.Context,
                L("detail.context",
                    "Local context is recorded by this peer; remote context is not carried by the vanilla divergence message.")));
            rows.Add(new("network.remotePeer", remotePeerId.ToString(), role));
            return new(
                L("section.overview.title", "Overview"),
                L("section.overview.description", "Checksum identity, checksum value, and network context."),
                false,
                rows);
        }

        private static StateDivergenceDiagnosticSection BuildSynchronizers(
            NetFullCombatState local,
            NetFullCombatState remote,
            bool includeMatching)
        {
            var rows = new List<StateDivergenceDiagnosticRow>();
            AddIfDifferent(rows, "choices.nextChoiceIds", Join(local.nextChoiceIds), Join(remote.nextChoiceIds),
                includeMatching);
#if STS2_AT_LEAST_0_106_0
            AddIfDifferent(rows, "rewards.nextRewardIds", Join(local.nextRewardIds), Join(remote.nextRewardIds),
                includeMatching);
#endif
            AddIfDifferent(rows, "actions.lastExecutedActionId", Format(local.lastExecutedActionId),
                Format(remote.lastExecutedActionId), includeMatching);
            AddIfDifferent(rows, "actions.lastExecutedHookId", Format(local.lastExecutedHookId),
                Format(remote.lastExecutedHookId), includeMatching);
            return new(
                L("section.sync.title", "Sync markers"),
                L("section.sync.description", "Choice IDs, reward IDs, and the last executed action or hook."),
                rows.Count == 0,
                rows);
        }

        private static StateDivergenceDiagnosticSection BuildCreatures(
            NetFullCombatState local,
            NetFullCombatState remote,
            bool includeMatching)
        {
            var rows = new List<StateDivergenceDiagnosticRow>();
            AddIfDifferent(rows, "creatures.count", local.Creatures.Count, remote.Creatures.Count, includeMatching);

            var count = Math.Max(local.Creatures.Count, remote.Creatures.Count);
            for (var i = 0; i < count; i++)
            {
                if (i >= local.Creatures.Count || i >= remote.Creatures.Count)
                {
                    rows.Add(new($"creatures[{i}]", DescribeAt(local.Creatures, i), DescribeAt(remote.Creatures, i)));
                    continue;
                }

                var l = local.Creatures[i];
                var r = remote.Creatures[i];
                var path = $"creatures[{i}]";
                AddIfDifferent(rows, path + ".identity", CreatureIdentity(l), CreatureIdentity(r), includeMatching);
                AddIfDifferent(rows, path + ".currentHp", l.currentHp, r.currentHp, includeMatching);
                AddIfDifferent(rows, path + ".maxHp", l.maxHp, r.maxHp, includeMatching);
                AddIfDifferent(rows, path + ".block", l.block, r.block, includeMatching);
                AddIfDifferent(rows, path + ".powers", FormatPowers(l.powers), FormatPowers(r.powers),
                    includeMatching);
            }

            return new(
                L("section.creatures.title", "Creatures"),
                L("section.creatures.description", "Monster/player combat HP, block, and powers."),
                rows.Count == 0,
                rows);
        }

        private static StateDivergenceDiagnosticSection BuildPlayers(
            NetFullCombatState local,
            NetFullCombatState remote,
            bool includeMatching)
        {
            var rows = new List<StateDivergenceDiagnosticRow>();
            AddIfDifferent(rows, "players.count", local.Players.Count, remote.Players.Count, includeMatching);

            var count = Math.Max(local.Players.Count, remote.Players.Count);
            for (var i = 0; i < count; i++)
            {
                if (i >= local.Players.Count || i >= remote.Players.Count)
                {
                    rows.Add(new($"players[{i}]", DescribeAt(local.Players, i), DescribeAt(remote.Players, i)));
                    continue;
                }

                var l = local.Players[i];
                var r = remote.Players[i];
                var path = $"players[{i}]";
                AddIfDifferent(rows, path + ".playerId", l.playerId, r.playerId, includeMatching);
                AddIfDifferent(rows, path + ".character", Format(l.characterId), Format(r.characterId),
                    includeMatching);
#if STS2_AT_LEAST_0_106_0
                AddIfDifferent(rows, path + ".turnNumber", l.turnNumber, r.turnNumber, includeMatching);
                AddIfDifferent(rows, path + ".phase", l.phase, r.phase, includeMatching);
#else
                AddIfDifferent(rows, path + ".maxStars", l.maxStars, r.maxStars, includeMatching);
#endif
                AddIfDifferent(rows, path + ".energy", l.energy, r.energy, includeMatching);
                AddIfDifferent(rows, path + ".stars", l.stars, r.stars, includeMatching);
                AddIfDifferent(rows, path + ".maxPotionCount", l.maxPotionCount, r.maxPotionCount,
                    includeMatching);
                AddIfDifferent(rows, path + ".gold", l.gold, r.gold, includeMatching);
                AddIfDifferent(rows, path + ".potions", Join(l.potions.Select(p => Format(p.id))),
                    Join(r.potions.Select(p => Format(p.id))), includeMatching);
                CompareRelics(rows, path + ".relics", l.relics, r.relics, includeMatching);
                AddIfDifferent(rows, path + ".orbs", Join(l.orbs.Select(FormatOrb)),
                    Join(r.orbs.Select(FormatOrb)), includeMatching);
                ComparePiles(rows, path, l.piles, r.piles, includeMatching);
            }

            return new(
                L("section.players.title", "Players"),
                L("section.players.description", "Player combat resources, inventory, orbs, and card piles."),
                rows.Count == 0,
                rows);
        }

        private static void ComparePiles(
            ICollection<StateDivergenceDiagnosticRow> rows,
            string playerPath,
            IReadOnlyList<NetFullCombatState.CombatPileState> local,
            IReadOnlyList<NetFullCombatState.CombatPileState> remote,
            bool includeMatching)
        {
            AddIfDifferent(rows, playerPath + ".piles.count", local.Count, remote.Count, includeMatching);

            var pileTypes = local.Select(p => p.pileType)
                .Concat(remote.Select(p => p.pileType))
                .Distinct()
                .OrderBy(p => p.ToString());
            foreach (var pileType in pileTypes)
            {
                var l = local.FirstOrDefault(p => EqualityComparer<object>.Default.Equals(p.pileType, pileType));
                var r = remote.FirstOrDefault(p => EqualityComparer<object>.Default.Equals(p.pileType, pileType));
                var items = BuildCardListItems(l.cards ?? [], r.cards ?? [], includeMatching);
                if (!includeMatching && !HasVisibleModelListDifference(items))
                    continue;

                rows.Add(new(
                    $"{playerPath}.piles.{pileType}",
                    FormatModelListSide(items, true),
                    FormatModelListSide(items, false),
                    F("detail.pileSummary", "Local: {0}; remote: {1}; first mismatch: {2}.",
                        FormatCardCount(l.cards?.Count ?? 0),
                        FormatCardCount(r.cards?.Count ?? 0),
                        FormatFirstMismatch(items)),
                    StateDivergenceDiagnosticRowKind.ModelList,
                    items));
            }
        }

        private static void CompareRelics(
            ICollection<StateDivergenceDiagnosticRow> rows,
            string path,
            IReadOnlyList<NetFullCombatState.RelicState> local,
            IReadOnlyList<NetFullCombatState.RelicState> remote,
            bool includeMatching)
        {
            var items = BuildRelicListItems(local, remote, includeMatching);
            if (!includeMatching && !HasVisibleModelListDifference(items))
                return;

            rows.Add(new(
                path,
                FormatModelListSide(items, true),
                FormatModelListSide(items, false),
                F("detail.modelListSummary", "Local: {0}; remote: {1}; first mismatch: {2}.",
                    FormatItemCount(local.Count),
                    FormatItemCount(remote.Count),
                    FormatFirstMismatch(items)),
                StateDivergenceDiagnosticRowKind.ModelList,
                items));
        }

        private static StateDivergenceDiagnosticSection BuildRng(
            NetFullCombatState local,
            NetFullCombatState remote,
            bool includeMatching)
        {
            var rows = new List<StateDivergenceDiagnosticRow>();
            AddIfDifferent(rows, "rng.run.seed", local.Rng.Seed ?? "", remote.Rng.Seed ?? "", includeMatching);
            CompareCounters(rows, "rng.run.counters", local.Rng.Counters, remote.Rng.Counters, includeMatching);

            var count = Math.Max(local.Players.Count, remote.Players.Count);
            for (var i = 0; i < count; i++)
            {
                if (i >= local.Players.Count || i >= remote.Players.Count)
                    continue;

                AddIfDifferent(rows, $"players[{i}].rng.seed", local.Players[i].rngSet.Seed,
                    remote.Players[i].rngSet.Seed, includeMatching);
                CompareCounters(rows, $"players[{i}].rng.counters", local.Players[i].rngSet.Counters,
                    remote.Players[i].rngSet.Counters, includeMatching);
            }

            return new(
                L("section.rng.title", "RNG"),
                L("section.rng.description", "Run and player RNG seeds and counters."),
                rows.Count == 0,
                rows);
        }

        private static StateDivergenceDiagnosticSection BuildRelicGrabBags(
            NetFullCombatState local,
            NetFullCombatState remote,
            bool includeMatching)
        {
            var rows = new List<StateDivergenceDiagnosticRow>();
            var count = Math.Max(local.Players.Count, remote.Players.Count);
            for (var i = 0; i < count; i++)
            {
                if (i >= local.Players.Count || i >= remote.Players.Count)
                    continue;

                var l = local.Players[i].relicGrabBag.RelicIdLists;
                var r = remote.Players[i].relicGrabBag.RelicIdLists;
                foreach (var rarity in l.Keys.Concat(r.Keys).Distinct().OrderBy(k => k.ToString()))
                {
                    l.TryGetValue(rarity, out var lIds);
                    r.TryGetValue(rarity, out var rIds);
                    AddIfDifferent(rows, $"players[{i}].relicGrabBag.{rarity}",
                        Join((lIds ?? []).Select(Format)),
                        Join((rIds ?? []).Select(Format)),
                        includeMatching);
                }
            }

            return new(
                L("section.relicGrabBag.title", "Relic grab bags"),
                L("section.relicGrabBag.description", "Remaining relic pools by rarity."),
                true,
                rows);
        }

        private static void CompareCounters<TKey>(
            ICollection<StateDivergenceDiagnosticRow> rows,
            string path,
            IReadOnlyDictionary<TKey, int> local,
            IReadOnlyDictionary<TKey, int> remote,
            bool includeMatching)
            where TKey : notnull
        {
            foreach (var key in local.Keys.Concat(remote.Keys).Distinct().OrderBy(k => k.ToString()))
            {
                local.TryGetValue(key, out var l);
                remote.TryGetValue(key, out var r);
                AddIfDifferent(rows, $"{path}.{key}", l, r, includeMatching);
            }
        }

        private static void AddIfDifferent(
            ICollection<StateDivergenceDiagnosticRow> rows,
            string path,
            object? local,
            object? remote,
            bool includeMatching = false)
        {
            var l = Format(local);
            var r = Format(remote);
            if (includeMatching || !string.Equals(l, r, StringComparison.Ordinal))
                rows.Add(new(path, l, r));
        }

        private static string FormatPowers(IReadOnlyList<NetFullCombatState.PowerState>? powers)
        {
            return Join((powers ?? []).Select(p => $"{Format(p.id)}:{p.amount}"));
        }

        private static string CreatureIdentity(NetFullCombatState.CreatureState state)
        {
            if (state.monsterId != null)
                return Format(state.monsterId);
            if (state.playerId.HasValue)
                return "player:" + state.playerId.Value;
            return "<unknown>";
        }

        private static IReadOnlyList<StateDivergenceDiagnosticModelListItem> BuildCardListItems(
            IReadOnlyList<NetFullCombatState.CardState> local,
            IReadOnlyList<NetFullCombatState.CardState> remote,
            bool includeMatching)
        {
            var items = new List<StateDivergenceDiagnosticModelListItem>();
            var count = Math.Max(local.Count, remote.Count);
            for (var i = 0; i < count; i++)
            {
                var hasLocal = i < local.Count;
                var hasRemote = i < remote.Count;
                var localSummary = hasLocal ? FormatCardSummary(local[i]) : L("value.missing", "Missing");
                var remoteSummary = hasRemote ? FormatCardSummary(remote[i]) : L("value.missing", "Missing");
                var differences = hasLocal && hasRemote && ModelIdsEqual(local[i].card.Id, remote[i].card.Id)
                    ? CompareCardState(local[i], remote[i], "", includeMatching)
                    : [];

                items.Add(new(i.ToString("00"), localSummary, remoteSummary, differences));
            }

            return items;
        }

        private static IReadOnlyList<StateDivergenceDiagnosticModelListItem> BuildRelicListItems(
            IReadOnlyList<NetFullCombatState.RelicState> local,
            IReadOnlyList<NetFullCombatState.RelicState> remote,
            bool includeMatching)
        {
            var items = new List<StateDivergenceDiagnosticModelListItem>();
            var count = Math.Max(local.Count, remote.Count);
            for (var i = 0; i < count; i++)
            {
                var hasLocal = i < local.Count;
                var hasRemote = i < remote.Count;
                var localSummary = hasLocal ? FormatRelicSummary(local[i]) : L("value.missing", "Missing");
                var remoteSummary = hasRemote ? FormatRelicSummary(remote[i]) : L("value.missing", "Missing");
                var differences = hasLocal && hasRemote && ModelIdsEqual(local[i].relic.Id, remote[i].relic.Id)
                    ? CompareRelicState(local[i], remote[i], "", includeMatching)
                    : [];

                items.Add(new(i.ToString("00"), localSummary, remoteSummary, differences));
            }

            return items;
        }

        private static bool HasVisibleModelListDifference(
            IReadOnlyList<StateDivergenceDiagnosticModelListItem> items)
        {
            return items.Any(item =>
                item.Differences.Count > 0 ||
                !string.Equals(item.LocalSummary, item.RemoteSummary, StringComparison.Ordinal));
        }

        private static IReadOnlyList<StateDivergenceDiagnosticFieldDifference> CompareCardState(
            NetFullCombatState.CardState local,
            NetFullCombatState.CardState remote,
            string prefix,
            bool includeMatching,
            int savedPropertyDepth = 0)
        {
            var differences = new List<StateDivergenceDiagnosticFieldDifference>();
            AddFieldIfDifferent(differences, prefix + "upgrade",
                local.card.CurrentUpgradeLevel, remote.card.CurrentUpgradeLevel, includeMatching);
            AddFieldIfDifferent(differences, prefix + "floor",
                FormatNullableInt(local.card.FloorAddedToDeck), FormatNullableInt(remote.card.FloorAddedToDeck),
                includeMatching);
            AddFieldIfDifferent(differences, prefix + "energyCost",
                FormatNullableInt(local.energyCost), FormatNullableInt(remote.energyCost), includeMatching);
            AddFieldIfDifferent(differences, prefix + "affliction.id",
                Format(local.affliction), Format(remote.affliction), includeMatching);
            AddFieldIfDifferent(differences, prefix + "affliction.count",
                local.afflictionCount, remote.afflictionCount, includeMatching);
            AddFieldIfDifferent(differences, prefix + "keywords",
                Join(local.keywords), Join(remote.keywords), includeMatching);
            CompareEnchantment(differences, prefix + "enchantment", local.card.Enchantment, remote.card.Enchantment,
                includeMatching);
            CompareSavedProperties(differences, prefix + "props", local.card.Props, remote.card.Props,
                savedPropertyDepth, includeMatching);
            return differences;
        }

        private static IReadOnlyList<StateDivergenceDiagnosticFieldDifference> CompareRelicState(
            NetFullCombatState.RelicState local,
            NetFullCombatState.RelicState remote,
            string prefix,
            bool includeMatching)
        {
            var differences = new List<StateDivergenceDiagnosticFieldDifference>();
            AddFieldIfDifferent(differences, prefix + "floor",
                FormatNullableInt(local.relic.FloorAddedToDeck), FormatNullableInt(remote.relic.FloorAddedToDeck),
                includeMatching);
            CompareSavedProperties(differences, prefix + "props", local.relic.Props, remote.relic.Props, 0,
                includeMatching);
            return differences;
        }

        private static void CompareEnchantment(
            ICollection<StateDivergenceDiagnosticFieldDifference> differences,
            string path,
            SerializableEnchantment? local,
            SerializableEnchantment? remote,
            bool includeMatching)
        {
            if (local == null || remote == null || !ModelIdsEqual(local.Id, remote.Id))
            {
                AddFieldIfDifferent(differences, path, FormatEnchantment(local), FormatEnchantment(remote),
                    includeMatching);
                return;
            }

            AddFieldIfDifferent(differences, path + ".amount", local.Amount, remote.Amount, includeMatching);
            CompareSavedProperties(differences, path + ".props", local.Props, remote.Props, 0, includeMatching);
        }

        private static void CompareSavedProperties(
            ICollection<StateDivergenceDiagnosticFieldDifference> differences,
            string path,
            SavedProperties? local,
            SavedProperties? remote,
            int depth,
            bool includeMatching)
        {
            if (local == null || remote == null)
            {
                AddFieldIfDifferent(differences, path, FormatSavedProperties(local), FormatSavedProperties(remote),
                    includeMatching);
                return;
            }

            CompareSavedPropertyBucket(differences, path, local.ints, remote.ints, value => Format(value),
                includeMatching);
            CompareSavedPropertyBucket(differences, path, local.bools, remote.bools, value => Format(value),
                includeMatching);
            CompareSavedPropertyBucket(differences, path, local.strings, remote.strings, Format, includeMatching);
            CompareIntArrayProperties(differences, path, local.intArrays, remote.intArrays, includeMatching);
            CompareSavedPropertyBucket(differences, path, local.modelIds, remote.modelIds, Format, includeMatching);
            CompareSavedCardProperties(differences, path, local.cards, remote.cards, depth, includeMatching);
            CompareSavedCardArrayProperties(differences, path, local.cardArrays, remote.cardArrays, depth,
                includeMatching);
        }

        private static void CompareSavedPropertyBucket<T>(
            ICollection<StateDivergenceDiagnosticFieldDifference> differences,
            string path,
            IReadOnlyList<SavedProperties.SavedProperty<T>>? local,
            IReadOnlyList<SavedProperties.SavedProperty<T>>? remote,
            Func<T?, string> format,
            bool includeMatching)
        {
            var names = (local?.Select(p => p.name) ?? [])
                .Concat(remote?.Select(p => p.name) ?? [])
                .Distinct()
                .OrderBy(name => name, StringComparer.Ordinal);

            foreach (var name in names)
            {
                var hasLocal = TryGetSavedProperty(local, name, out var localValue);
                var hasRemote = TryGetSavedProperty(remote, name, out var remoteValue);
                AddFieldIfDifferent(differences, path + "." + name,
                    hasLocal ? format(localValue) : L("value.missing", "Missing"),
                    hasRemote ? format(remoteValue) : L("value.missing", "Missing"),
                    includeMatching);
            }
        }

        private static void CompareIntArrayProperties(
            ICollection<StateDivergenceDiagnosticFieldDifference> differences,
            string path,
            IReadOnlyList<SavedProperties.SavedProperty<int[]>>? local,
            IReadOnlyList<SavedProperties.SavedProperty<int[]>>? remote,
            bool includeMatching)
        {
            var names = (local?.Select(p => p.name) ?? [])
                .Concat(remote?.Select(p => p.name) ?? [])
                .Distinct()
                .OrderBy(name => name, StringComparer.Ordinal);

            foreach (var name in names)
            {
                var hasLocal = TryGetSavedProperty(local, name, out var localValue);
                var hasRemote = TryGetSavedProperty(remote, name, out var remoteValue);
                AddFieldIfDifferent(differences, path + "." + name,
                    hasLocal ? FormatIntArrayProperty(name, localValue) : L("value.missing", "Missing"),
                    hasRemote ? FormatIntArrayProperty(name, remoteValue) : L("value.missing", "Missing"),
                    includeMatching);
            }
        }

        private static void CompareSavedCardProperties(
            ICollection<StateDivergenceDiagnosticFieldDifference> differences,
            string path,
            IReadOnlyList<SavedProperties.SavedProperty<SerializableCard>>? local,
            IReadOnlyList<SavedProperties.SavedProperty<SerializableCard>>? remote,
            int depth,
            bool includeMatching)
        {
            var names = (local?.Select(p => p.name) ?? [])
                .Concat(remote?.Select(p => p.name) ?? [])
                .Distinct()
                .OrderBy(name => name, StringComparer.Ordinal);

            foreach (var name in names)
            {
                var hasLocal = TryGetSavedProperty(local, name, out var localCard);
                var hasRemote = TryGetSavedProperty(remote, name, out var remoteCard);
                CompareSavedCard(differences, path + "." + name,
                    hasLocal ? localCard : null,
                    hasRemote ? remoteCard : null,
                    depth,
                    includeMatching);
            }
        }

        private static void CompareSavedCardArrayProperties(
            ICollection<StateDivergenceDiagnosticFieldDifference> differences,
            string path,
            IReadOnlyList<SavedProperties.SavedProperty<SerializableCard[]>>? local,
            IReadOnlyList<SavedProperties.SavedProperty<SerializableCard[]>>? remote,
            int depth,
            bool includeMatching)
        {
            var names = (local?.Select(p => p.name) ?? [])
                .Concat(remote?.Select(p => p.name) ?? [])
                .Distinct()
                .OrderBy(name => name, StringComparer.Ordinal);

            foreach (var name in names)
            {
                var hasLocal = TryGetSavedProperty(local, name, out var localCards);
                var hasRemote = TryGetSavedProperty(remote, name, out var remoteCards);
                var l = hasLocal ? localCards : [];
                var r = hasRemote ? remoteCards : [];
                var count = Math.Max(l.Length, r.Length);
                for (var i = 0; i < count; i++)
                    CompareSavedCard(differences, $"{path}.{name}[{i}]",
                        i < l.Length ? l[i] : null,
                        i < r.Length ? r[i] : null,
                        depth,
                        includeMatching);
            }
        }

        private static void CompareSavedCard(
            ICollection<StateDivergenceDiagnosticFieldDifference> differences,
            string path,
            SerializableCard? local,
            SerializableCard? remote,
            int depth,
            bool includeMatching)
        {
            if (local == null || remote == null || !ModelIdsEqual(local.Id, remote.Id) ||
                depth >= MaxNestedSavedCardDepth)
            {
                AddFieldIfDifferent(differences, path, FormatSerializableCard(local), FormatSerializableCard(remote),
                    includeMatching);
                return;
            }

            var nestedLocal = new NetFullCombatState.CardState { card = local };
            var nestedRemote = new NetFullCombatState.CardState { card = remote };
            foreach (var difference in CompareCardState(nestedLocal, nestedRemote, path + ".", includeMatching,
                             depth + 1)
                         .Where(d => !d.Path.EndsWith(".energyCost", StringComparison.Ordinal) &&
                                     !d.Path.Contains(".affliction.", StringComparison.Ordinal) &&
                                     !d.Path.EndsWith(".keywords", StringComparison.Ordinal)))
                differences.Add(difference);
        }

        private static bool TryGetSavedProperty<T>(
            IReadOnlyList<SavedProperties.SavedProperty<T>>? properties,
            string name,
            out T value)
        {
            if (properties != null)
                foreach (var property in properties)
                {
                    if (!string.Equals(property.name, name, StringComparison.Ordinal))
                        continue;

                    value = property.value;
                    return true;
                }

            value = default!;
            return false;
        }

        private static void AddFieldIfDifferent(
            ICollection<StateDivergenceDiagnosticFieldDifference> differences,
            string path,
            object? local,
            object? remote,
            bool includeMatching = false)
        {
            var l = Format(local);
            var r = Format(remote);
            if (includeMatching || !string.Equals(l, r, StringComparison.Ordinal))
                differences.Add(new(path, l, r));
        }

        private static string FormatCardSummary(NetFullCombatState.CardState state)
        {
            var pieces = new List<string> { Format(state.card.Id) };
            if (state.card.CurrentUpgradeLevel != 0)
                pieces.Add("+" + state.card.CurrentUpgradeLevel);
            if (state.card.FloorAddedToDeck.HasValue)
                pieces.Add("floor=" + state.card.FloorAddedToDeck.Value);
            if (state.energyCost.HasValue)
                pieces.Add("cost=" + state.energyCost.Value);
            if (state.affliction != null)
                pieces.Add("affliction=" + Format(state.affliction) + ":" + state.afflictionCount);
            if (state.keywords is { Count: > 0 })
                pieces.Add("keywords=" + Join(state.keywords));
            if (state.card.Enchantment != null)
                pieces.Add("enchant=" + FormatEnchantment(state.card.Enchantment));
            return string.Join(" ", pieces);
        }

        private static string FormatModelListSide(
            IReadOnlyList<StateDivergenceDiagnosticModelListItem> items,
            bool local)
        {
            return items.Count == 0
                ? L("value.empty", "<empty>")
                : string.Join(Environment.NewLine,
                    items.Select(item => $"{item.Index}  {(local ? item.LocalSummary : item.RemoteSummary)}"));
        }

        private static string FormatCardCount(int count)
        {
            return F("value.cardCount", "{0} card(s)", count);
        }

        private static string FormatItemCount(int count)
        {
            return F("value.itemCount", "{0} item(s)", count);
        }

        private static string FormatFirstMismatch(
            IReadOnlyList<StateDivergenceDiagnosticModelListItem> items)
        {
            foreach (var item in items)
                if (item.Differences.Count > 0 ||
                    !string.Equals(item.LocalSummary, item.RemoteSummary, StringComparison.Ordinal))
                    return F("value.cardIndex", "index {0}", item.Index);

            return L("value.none", "<none>");
        }

        private static string FormatRelicSummary(NetFullCombatState.RelicState state)
        {
            var pieces = new List<string> { Format(state.relic.Id) };
            if (state.relic.FloorAddedToDeck.HasValue)
                pieces.Add("floor=" + state.relic.FloorAddedToDeck.Value);
            return string.Join(" ", pieces);
        }

        private static string FormatSerializableCard(SerializableCard? card)
        {
            return card == null ? L("value.missing", "Missing") : FormatCardSummary(new() { card = card });
        }

        private static string FormatEnchantment(SerializableEnchantment? enchantment)
        {
            return enchantment == null
                ? L("value.none", "<none>")
                : $"{Format(enchantment.Id)} amount={enchantment.Amount}";
        }

        private static string FormatSavedProperties(SavedProperties? properties)
        {
            return properties == null ? L("value.none", "<none>") : properties.ToString();
        }

        private static string FormatNullableInt(int? value)
        {
            return value.HasValue ? value.Value.ToString() : L("value.none", "<none>");
        }

        private static string FormatHash(uint value)
        {
            return "0x" + value.ToString("X8");
        }

        private static string FormatIntArrayProperty(int[]? value)
        {
            return FormatIntArrayProperty("", value);
        }

        private static string FormatIntArrayProperty(string name, int[]? value)
        {
            if (value == null)
                return L("value.none", "<none>");

            if (string.Equals(name, MinionLibComponentBlobPropertyName, StringComparison.Ordinal))
                return TryFormatMinionLibComponents(value) ??
                       $"MinionLib components blob len={value.Length} hash=0x{StableHash(value):X8} raw={FormatIntArray(value)}";

            return FormatIntArray(value);
        }

        private static string FormatIntArray(int[] value)
        {
            return "[" + string.Join(", ", value) + "]";
        }

        private static string? TryFormatMinionLibComponents(int[] blob)
        {
            try
            {
                var serializerType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType("MinionLib.Component.Core.CardComponentStateSerializer"))
                    .FirstOrDefault(type => type != null);
                var deserialize = serializerType?.GetMethod("Deserialize",
                    BindingFlags.Public | BindingFlags.Static);
                if (deserialize == null)
                    return null;

                if (deserialize.Invoke(null, [blob, null]) is not IEnumerable components)
                    return null;

                var lines = new List<string> { "Components:" };
                var count = 0;
                foreach (var component in components)
                {
                    count++;
                    lines.AddRange(FormatMinionLibComponent(component));
                }

                if (count == 0)
                    lines.Add("  [Empty]");

                return string.Join(Environment.NewLine, lines);
            }
            catch
            {
                return null;
            }
        }

        private static IEnumerable<string> FormatMinionLibComponent(object component)
        {
            var type = component.GetType();
            var componentId = type.GetProperty("ComponentId", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(component)
                ?.ToString();
            yield return "  - " + (string.IsNullOrWhiteSpace(componentId) ? type.Name : componentId);

            var stateProperties = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(prop => prop.CanRead &&
                               prop.GetIndexParameters().Length == 0 &&
                               prop.GetCustomAttributes(true).Any(IsMinionLibComponentStateAttribute))
                .OrderBy(prop => prop.Name, StringComparer.Ordinal);
            foreach (var property in stateProperties)
            {
                object? value;
                try
                {
                    value = property.GetValue(component);
                }
                catch
                {
                    value = "<unreadable>";
                }

                yield return $"    {property.Name}: {FormatReflectedValue(value)}";
            }
        }

        private static bool IsMinionLibComponentStateAttribute(object attribute)
        {
            return attribute.GetType().FullName
                ?.StartsWith("MinionLib.Component.Core.ComponentStateAttribute", StringComparison.Ordinal) == true;
        }

        private static string FormatReflectedValue(object? value)
        {
            return value switch
            {
                null => L("value.none", "<none>"),
                string text => text,
                IEnumerable enumerable => "[" +
                                          string.Join(", ", enumerable.Cast<object?>().Select(FormatReflectedValue)) +
                                          "]",
                _ => value.ToString() ?? "",
            };
        }

        private static uint StableHash(IEnumerable<int> values)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var value in values)
                {
                    hash ^= (uint)value;
                    hash *= 16777619u;
                }

                return hash;
            }
        }

        private static string FormatOrb(NetFullCombatState.OrbState state)
        {
            return $"{Format(state.id)} passive={state.passive} evoke={state.evoke}";
        }

        private static string DescribeAt<T>(IReadOnlyList<T> list, int index)
        {
            return index < list.Count ? Format(list[index]) : L("value.missing", "Missing");
        }

        private static string Join<T>(IEnumerable<T>? values)
        {
            if (values == null)
                return "";

            var text = string.Join(", ", values.Select(v => Format(v)));
            return text.Length == 0 ? L("value.empty", "<empty>") : text;
        }

        private static string Format(object? value)
        {
            return value switch
            {
                null => L("value.none", "<none>"),
                ModelId id => FormatModelId(id),
                RelicRarity rarity => rarity.ToString(),
                RunRngType rng => rng.ToString(),
                PlayerRngType rng => rng.ToString(),
                _ => value.ToString() ?? "",
            };
        }

        private static bool ModelIdsEqual(ModelId? local, ModelId? remote)
        {
            return string.Equals(local?.ToString(), remote?.ToString(), StringComparison.Ordinal);
        }

        private static string FormatModelId(ModelId id)
        {
            var idText = id.ToString();
            var name = TryGetLocalModelTitle(id);
            return string.IsNullOrWhiteSpace(name) ? idText : $"{idText} ({name})";
        }

        private static string? TryGetLocalModelTitle(ModelId id)
        {
            try
            {
                var model = ModelDb.GetByIdOrNull<AbstractModel>(id);
                if (model == null)
                    return null;

                var title = model.GetType().GetProperty("Title")?.GetValue(model);
                var text = title switch
                {
                    string s => s,
                    LocString loc => loc.GetFormattedText(),
                    _ => null,
                };

                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
            catch
            {
                return null;
            }
        }

        private static string L(string key, string fallback)
        {
            return StateDivergenceDiagnosticsLocalization.Get(key, fallback);
        }

        private static string F(string key, string fallback, params object?[] args)
        {
            return StateDivergenceDiagnosticsLocalization.Format(key, fallback, args);
        }
    }
}
