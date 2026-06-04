using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Rngs;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Checksums;

namespace STS2RitsuLib.Networking.StateDivergence
{
    internal static class StateDivergenceDiagnosticReportBuilder
    {
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

            var sections = new List<StateDivergenceDiagnosticSection>
            {
                BuildOverview(local, remote, remotePeerId, role),
                BuildSynchronizers(local.FullState, remote.FullState),
                BuildCreatures(local.FullState, remote.FullState),
                BuildPlayers(local.FullState, remote.FullState),
                BuildRng(local.FullState, remote.FullState),
                BuildRelicGrabBags(local.FullState, remote.FullState),
            };

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
                local.FullState.ToString(),
                remote.FullState.ToString());
        }

        private static StateDivergenceDiagnosticSection BuildOverview(
            StateDivergenceTrackedState local,
            StateDivergenceTrackedState remote,
            ulong remotePeerId,
            string role)
        {
            var rows = new List<StateDivergenceDiagnosticRow>();
            AddIfDifferent(rows, "checksum.id", local.Checksum.id, remote.Checksum.id);
            AddIfDifferent(rows, "checksum.value", local.Checksum.checksum, remote.Checksum.checksum);
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
            NetFullCombatState remote)
        {
            var rows = new List<StateDivergenceDiagnosticRow>();
            AddIfDifferent(rows, "choices.nextChoiceIds", Join(local.nextChoiceIds), Join(remote.nextChoiceIds));
#if STS2_AT_LEAST_0_106_0
            AddIfDifferent(rows, "rewards.nextRewardIds", Join(local.nextRewardIds), Join(remote.nextRewardIds));
#endif
            AddIfDifferent(rows, "actions.lastExecutedActionId", Format(local.lastExecutedActionId),
                Format(remote.lastExecutedActionId));
            AddIfDifferent(rows, "actions.lastExecutedHookId", Format(local.lastExecutedHookId),
                Format(remote.lastExecutedHookId));
            return new(
                L("section.sync.title", "Sync markers"),
                L("section.sync.description", "Choice IDs, reward IDs, and the last executed action or hook."),
                rows.Count == 0,
                rows);
        }

        private static StateDivergenceDiagnosticSection BuildCreatures(
            NetFullCombatState local,
            NetFullCombatState remote)
        {
            var rows = new List<StateDivergenceDiagnosticRow>();
            AddIfDifferent(rows, "creatures.count", local.Creatures.Count, remote.Creatures.Count);

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
                AddIfDifferent(rows, path + ".identity", CreatureIdentity(l), CreatureIdentity(r));
                AddIfDifferent(rows, path + ".currentHp", l.currentHp, r.currentHp);
                AddIfDifferent(rows, path + ".maxHp", l.maxHp, r.maxHp);
                AddIfDifferent(rows, path + ".block", l.block, r.block);
                AddIfDifferent(rows, path + ".powers", FormatPowers(l.powers), FormatPowers(r.powers));
            }

            return new(
                L("section.creatures.title", "Creatures"),
                L("section.creatures.description", "Monster/player combat HP, block, and powers."),
                rows.Count == 0,
                rows);
        }

        private static StateDivergenceDiagnosticSection BuildPlayers(
            NetFullCombatState local,
            NetFullCombatState remote)
        {
            var rows = new List<StateDivergenceDiagnosticRow>();
            AddIfDifferent(rows, "players.count", local.Players.Count, remote.Players.Count);

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
                AddIfDifferent(rows, path + ".playerId", l.playerId, r.playerId);
                AddIfDifferent(rows, path + ".character", Format(l.characterId), Format(r.characterId));
#if STS2_AT_LEAST_0_106_0
                AddIfDifferent(rows, path + ".turnNumber", l.turnNumber, r.turnNumber);
                AddIfDifferent(rows, path + ".phase", l.phase, r.phase);
#else
                AddIfDifferent(rows, path + ".maxStars", l.maxStars, r.maxStars);
#endif
                AddIfDifferent(rows, path + ".energy", l.energy, r.energy);
                AddIfDifferent(rows, path + ".stars", l.stars, r.stars);
                AddIfDifferent(rows, path + ".maxPotionCount", l.maxPotionCount, r.maxPotionCount);
                AddIfDifferent(rows, path + ".gold", l.gold, r.gold);
                AddIfDifferent(rows, path + ".potions", Join(l.potions.Select(p => Format(p.id))),
                    Join(r.potions.Select(p => Format(p.id))));
                AddIfDifferent(rows, path + ".relics", Join(l.relics.Select(FormatRelic)),
                    Join(r.relics.Select(FormatRelic)));
                AddIfDifferent(rows, path + ".orbs", Join(l.orbs.Select(FormatOrb)),
                    Join(r.orbs.Select(FormatOrb)));
                ComparePiles(rows, path, l.piles, r.piles);
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
            IReadOnlyList<NetFullCombatState.CombatPileState> remote)
        {
            AddIfDifferent(rows, playerPath + ".piles.count", local.Count, remote.Count);

            var pileTypes = local.Select(p => p.pileType)
                .Concat(remote.Select(p => p.pileType))
                .Distinct()
                .OrderBy(p => p.ToString());
            foreach (var pileType in pileTypes)
            {
                var l = local.FirstOrDefault(p => EqualityComparer<object>.Default.Equals(p.pileType, pileType));
                var r = remote.FirstOrDefault(p => EqualityComparer<object>.Default.Equals(p.pileType, pileType));
                var lCards = (l.cards ?? []).Select(FormatCard).ToList();
                var rCards = (r.cards ?? []).Select(FormatCard).ToList();
                if (lCards.SequenceEqual(rCards))
                    continue;

                rows.Add(new(
                    $"{playerPath}.piles.{pileType}",
                    FormatPileCards(lCards),
                    FormatPileCards(rCards),
                    F("detail.pileSummary", "Local: {0}; remote: {1}; first mismatch: {2}.",
                        FormatCardCount(lCards.Count),
                        FormatCardCount(rCards.Count),
                        FormatFirstMismatch(lCards, rCards))));
            }
        }

        private static StateDivergenceDiagnosticSection BuildRng(
            NetFullCombatState local,
            NetFullCombatState remote)
        {
            var rows = new List<StateDivergenceDiagnosticRow>();
            AddIfDifferent(rows, "rng.run.seed", local.Rng.Seed ?? "", remote.Rng.Seed ?? "");
            CompareCounters(rows, "rng.run.counters", local.Rng.Counters, remote.Rng.Counters);

            var count = Math.Max(local.Players.Count, remote.Players.Count);
            for (var i = 0; i < count; i++)
            {
                if (i >= local.Players.Count || i >= remote.Players.Count)
                    continue;

                AddIfDifferent(rows, $"players[{i}].rng.seed", local.Players[i].rngSet.Seed,
                    remote.Players[i].rngSet.Seed);
                CompareCounters(rows, $"players[{i}].rng.counters", local.Players[i].rngSet.Counters,
                    remote.Players[i].rngSet.Counters);
            }

            return new(
                L("section.rng.title", "RNG"),
                L("section.rng.description", "Run and player RNG seeds and counters."),
                rows.Count == 0,
                rows);
        }

        private static StateDivergenceDiagnosticSection BuildRelicGrabBags(
            NetFullCombatState local,
            NetFullCombatState remote)
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
                        Join((rIds ?? []).Select(Format)));
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
            IReadOnlyDictionary<TKey, int> remote)
            where TKey : notnull
        {
            foreach (var key in local.Keys.Concat(remote.Keys).Distinct().OrderBy(k => k.ToString()))
            {
                local.TryGetValue(key, out var l);
                remote.TryGetValue(key, out var r);
                AddIfDifferent(rows, $"{path}.{key}", l, r);
            }
        }

        private static void AddIfDifferent(
            ICollection<StateDivergenceDiagnosticRow> rows,
            string path,
            object? local,
            object? remote)
        {
            var l = Format(local);
            var r = Format(remote);
            if (!string.Equals(l, r, StringComparison.Ordinal))
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

        private static string FormatCard(NetFullCombatState.CardState state)
        {
            var pieces = new List<string> { Format(state.card.Id) };
            if (state.card.CurrentUpgradeLevel != 0)
                pieces.Add("+" + state.card.CurrentUpgradeLevel);
            if (state.energyCost.HasValue)
                pieces.Add("cost=" + state.energyCost.Value);
            if (state.affliction != null)
                pieces.Add("affliction=" + Format(state.affliction) + ":" + state.afflictionCount);
            if (state.keywords is { Count: > 0 })
                pieces.Add("keywords=" + Join(state.keywords));
            if (state.card.Props != null)
                pieces.Add("props=" + state.card.Props);
            return string.Join(" ", pieces);
        }

        private static string FormatPileCards(IReadOnlyList<string> cards)
        {
            return cards.Count == 0
                ? L("value.empty", "<empty>")
                : string.Join(Environment.NewLine, cards.Select((card, i) => $"{i:00}  {card}"));
        }

        private static string FormatCardCount(int count)
        {
            return F("value.cardCount", "{0} card(s)", count);
        }

        private static string FormatFirstMismatch(IReadOnlyList<string> local, IReadOnlyList<string> remote)
        {
            var count = Math.Max(local.Count, remote.Count);
            for (var i = 0; i < count; i++)
            {
                var l = i < local.Count ? local[i] : null;
                var r = i < remote.Count ? remote[i] : null;
                if (!string.Equals(l, r, StringComparison.Ordinal))
                    return F("value.cardIndex", "index {0}", i);
            }

            return L("value.none", "<none>");
        }

        private static string FormatRelic(NetFullCombatState.RelicState state)
        {
            var pieces = new List<string> { Format(state.relic.Id) };
            if (state.relic.FloorAddedToDeck.HasValue)
                pieces.Add("floor=" + state.relic.FloorAddedToDeck.Value);
            if (state.relic.Props != null)
                pieces.Add("props=" + state.relic.Props);
            return string.Join(" ", pieces);
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
