using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Content;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Models;
using STS2RitsuLib.Ui.Catalog;
using STS2RitsuLib.Ui.Overlay;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class RitsuDebugToolsPanel
    {
        private Control CreateCardCatalog()
        {
            var cards = ModelDb.AllCards.OrderBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase).ToArray();
            var byId = cards.ToDictionary(static card => card.Id.ToString(), StringComparer.Ordinal);
            var poolFilter = CreateCardPoolFilter(cards, byId, out var defaultPoolOptionId);
            var filters = new[]
            {
                poolFilter,
                EnumFilter(
                    "type",
                    L("ritsulib.debugTools.filter.type", "Type"),
                    cards.Select(static card => card.Type).Distinct(),
                    EnumLabel,
                    (item, value) => byId[item.Id].Type == value),
                EnumFilter(
                    "rarity",
                    L("ritsulib.debugTools.filter.rarity", "Rarity"),
                    cards.Select(static card => card.Rarity).Distinct(),
                    EnumLabel,
                    (item, value) => byId[item.Id].Rarity == value),
                CreateContentSourceFilter(cards, byId),
            };
            return new RitsuDebugCardCatalog(
                L("ritsulib.debugTools.search.cards", "Search cards by name or ID"),
                [
                    .. cards.Select(card => new RitsuDebugCardCatalogEntry(
                        new(
                            card.Id.ToString(),
                            SafeTitle(card),
                            $"{EnumLabel(card.Type)} · {EnumLabel(card.Rarity)} · " +
                            $"{ContentSourceDisplayLabel(ContentSourceResolver.Resolve(card))} · {card.Id}",
                            $"{card.Type} {card.Rarity} {ContentSourceSearchText(card)}",
                            badge: CardCost(card)),
                        CreateCardPreviewModel(card),
                        card,
                        () => CreateCardDetail(card))),
                ],
                filters,
                defaultFilterId: defaultPoolOptionId == null ? null : poolFilter.Id,
                defaultFilterOptionId: defaultPoolOptionId);
        }

        private RitsuCatalogFilter CreateCardPoolFilter(
            IReadOnlyCollection<CardModel> cards,
            IReadOnlyDictionary<string, CardModel> cardsById,
            out string? defaultPoolOptionId)
        {
            var cardIds = cards.Select(static card => card.Id).ToHashSet();
            var characterLabels = ModelDb.AllCharacters
                .GroupBy(static character => character.CardPool.Id.ToString(), StringComparer.Ordinal)
                .ToDictionary(
                    static group => group.Key,
                    group => SafeTitle(group.First()),
                    StringComparer.Ordinal);
            var pools = ModelDb.AllCardPools
                .Where(pool => pool.AllCardIds.Any(cardIds.Contains))
                .Select(pool => new
                {
                    Model = pool,
                    Id = pool.Id.ToString(),
                    Label = characterLabels.GetValueOrDefault(pool.Id.ToString(), CardPoolLabel(pool.Title)),
                })
                .OrderBy(pool => characterLabels.ContainsKey(pool.Id) ? 0 : 1)
                .ThenBy(static pool => pool.Label, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();

            defaultPoolOptionId = null;
            if (TryGetTargetPlayer(out var player))
            {
                var playerPoolId = player.Character?.CardPool.Id.ToString();
                if (playerPoolId != null && pools.Any(pool => pool.Id == playerPoolId))
                    defaultPoolOptionId = playerPoolId;
            }

            return new(
                "pool",
                L("ritsulib.debugTools.filter.pool", "Card pool"),
                L("ritsulib.debugTools.filter.all", "All"),
                [
                    .. pools.Select(pool => new RitsuCatalogFilterOption(
                        pool.Id,
                        pool.Label,
                        item => pool.Model.AllCardIds.Contains(cardsById[item.Id].Id))),
                ]);
        }

        private static string CardPoolLabel(string poolTitle)
        {
            return L($"ritsulib.debugTools.cardPool.{poolTitle}", poolTitle);
        }

        private Control CreatePileCardCatalog()
        {
            if (!TryGetTargetPlayer(out var player))
                return EmptyBrowser(L("ritsulib.debugTools.noRun", "Start a run to use state tools."));
            var entries = GetPileCardEntries(player);
            _pileCardSnapshotHash = GetPileCardSnapshotHash(entries);

            if (entries.Length == 0)
                return EmptyBrowser(L("ritsulib.debugTools.empty.pileCards",
                    "The selected player has no cards in a supported pile."));

            var filter = EnumFilter(
                "pile",
                L("ritsulib.debugTools.filter.pile", "Pile"),
                RitsuDebugCardActions.GetMutablePileTypes(),
                PileLabel,
                (item, value) => item.Id.StartsWith(
                    $"{RitsuDebugCardActions.GetPileToken(value)}:",
                    StringComparison.Ordinal));
            return new RitsuDebugCardCatalog(
                L("ritsulib.debugTools.search.pileCards", "Search the target player's cards"),
                CreatePileCardCatalogEntries(entries),
                [filter],
                primaryFilterId: filter.Id,
                primaryFilterBreakBeforeOptionId: nameof(PileType.Deck),
                primaryDefaultsToAll: true,
                primaryAllMatches: IsDefaultPileCardEntry);
        }

        private Control CreateRelicCatalog()
        {
            var models = ModelDb.AllRelics.OrderBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase).ToArray();
            var byId = models.ToDictionary(static model => model.Id.ToString(), StringComparer.Ordinal);
            var rarityFilter = EnumFilter(
                "rarity",
                L("ritsulib.debugTools.filter.rarity", "Rarity"),
                models.Select(static model => model.Rarity).Distinct(),
                EnumLabel,
                (item, value) => byId[item.Id].Rarity == value);
            var browser = Browser(
                L("ritsulib.debugTools.search.relics", "Search relics by name or ID"),
                item => CreateRelicDetail(byId[item.Id]),
                [rarityFilter, CreateContentSourceFilter(models, byId)],
                RitsuCatalogPresentation.Grid,
                detailWidth: 520f);
            browser.SetItems([
                .. models.Select(model => ModelItem(
                    model,
                    EnumLabel(model.Rarity),
                    () => model.Icon)),
            ]);
            return CreateRelicWorkspace(models, byId, browser);
        }

        private Control CreatePotionCatalog()
        {
            var models = ModelDb.AllPotions.OrderBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase).ToArray();
            var byId = models.ToDictionary(static model => model.Id.ToString(), StringComparer.Ordinal);
            var rarityFilter = EnumFilter(
                "rarity",
                L("ritsulib.debugTools.filter.rarity", "Rarity"),
                models.Select(static model => model.Rarity).Distinct(),
                EnumLabel,
                (item, value) => byId[item.Id].Rarity == value);
            var browser = Browser(
                L("ritsulib.debugTools.search.potions", "Search potions by name or ID"),
                item => CreatePotionDetail(byId[item.Id]),
                [rarityFilter, CreateContentSourceFilter(models, byId)],
                RitsuCatalogPresentation.Grid);
            browser.SetItems([
                .. models.Select(model => ModelItem(
                    model,
                    EnumLabel(model.Rarity),
                    () => model.Image)),
            ]);
            return CreatePotionWorkspace(models, byId, browser);
        }

        private Control CreatePowerCatalog()
        {
            var models = ModelDb.AllPowers.OrderBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase).ToArray();
            var byId = models.ToDictionary(static model => model.Id.ToString(), StringComparer.Ordinal);
            var typeFilter = EnumFilter(
                "type",
                L("ritsulib.debugTools.filter.type", "Type"),
                models.Select(static model => model.Type).Distinct(),
                EnumLabel,
                (item, value) => byId[item.Id].Type == value);
            var browser = Browser(
                L("ritsulib.debugTools.search.powers", "Search powers by name or ID"),
                item => CreatePowerDetail(byId[item.Id]),
                [typeFilter, CreateContentSourceFilter(models, byId)],
                RitsuCatalogPresentation.Grid,
                detailWidth: 520f);
            browser.SetItems([
                .. models.Select(model => ModelItem(
                    model,
                    EnumLabel(model.Type),
                    () => model.Icon,
                    PowerTypeAccent(model.Type))),
            ]);
            return CreatePowerWorkspace(browser);
        }

        private Control CreateOrbCatalog()
        {
            var models = ModelDb.Orbs.OrderBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase).ToArray();
            var byId = models.ToDictionary(static model => model.Id.ToString(), StringComparer.Ordinal);
            var browser = Browser(
                L("ritsulib.debugTools.search.orbs", "Search orbs by name or ID"),
                item => CreateOrbDetail(byId[item.Id]),
                [CreateContentSourceFilter(models, byId)],
                RitsuCatalogPresentation.Grid,
                detailWidth: 520f);
            browser.SetItems([
                .. models.Select(model => ModelItem(
                    model,
                    L("ritsulib.debugTools.orbs.model", "Orb"),
                    () => model.Icon)),
            ]);
            return CreateOrbWorkspace(models, browser);
        }

        private RitsuCatalogBrowser CreateCombatantCatalog()
        {
            var players = GetPlayers();
            var creatures = CombatManager.Instance.DebugOnlyGetState()?.Creatures
                .Where(IsVisibleCombatant)
                .OrderBy(static creature => creature.CombatId)
                .ToArray() ?? [];
            var filter = new RitsuCatalogFilter(
                "combatantType",
                L("ritsulib.debugTools.filter.combatantType", "Type"),
                L("ritsulib.debugTools.filter.all", "All"),
                [
                    new("players", L("ritsulib.debugTools.filter.players", "Players"),
                        item => item.Id.StartsWith("player:", StringComparison.Ordinal)),
                    new("pets", L("ritsulib.debugTools.filter.pets", "Pets"),
                        item => ResolveCurrentCreature(item)?.IsPet == true),
                    new("monsters", L("ritsulib.debugTools.filter.monsters", "Monsters"),
                        item => ResolveCurrentCreature(item) is { IsPlayer: false, IsPet: false }),
                ]);
            var browser = Browser(
                L("ritsulib.debugTools.search.combatants", "Search players and combat creatures"),
                item =>
                {
                    if (ResolveCurrentPlayer(item) is { } player)
                        return CreatePlayerDetail(player);
                    return ResolveCurrentCreature(item) is { } creature
                        ? CreateCreatureDetail(creature)
                        : EmptyBrowser(L("ritsulib.debugTools.targetChanged",
                            "The selected target is no longer available."));
                },
                [filter],
                RitsuCatalogPresentation.Grid,
                220f,
                detailWidth: 640f);
            browser.SetItems(CreateCombatantCatalogItems(players, creatures));
            return browser;

            static Creature? ResolveCurrentCreature(RitsuCatalogItem item)
            {
                return item.Id.StartsWith("creature:", StringComparison.Ordinal) &&
                       uint.TryParse(item.Id.AsSpan("creature:".Length), out var combatId)
                    ? RitsuDebugCombatActions.FindCreature(combatId)
                    : null;
            }

            static Player? ResolveCurrentPlayer(RitsuCatalogItem item)
            {
                return item.Id.StartsWith("player:", StringComparison.Ordinal) &&
                       ulong.TryParse(item.Id.AsSpan("player:".Length), out var netId)
                    ? GetPlayers().FirstOrDefault(player => player.NetId == netId)
                    : null;
            }
        }

        private static PileCardEntry[] GetPileCardEntries(Player player)
        {
            var entries = new List<PileCardEntry>();
            foreach (var pileType in RitsuDebugCardActions.GetMutablePileTypes())
            {
                var pile = RitsuDebugCardActions.GetExistingPile(player, pileType);
                if (pile == null)
                    continue;
                for (var index = 0; index < pile.Cards.Count; index++)
                {
                    var card = pile.Cards[index];
                    entries.Add(new(
                        pileType,
                        index,
                        card,
                        RitsuDebugCardActions.GetCombatCardId(card)));
                }
            }

            return [.. entries];
        }

        private RitsuDebugCardCatalogEntry[] CreatePileCardCatalogEntries(
            IEnumerable<PileCardEntry> entries)
        {
            return
            [
                .. entries.Select(entry => new RitsuDebugCardCatalogEntry(
                    new(
                        entry.StableId,
                        SafeTitle(entry.Card),
                        $"{PileLabel(entry.PileType)} #{entry.Index + 1} · {entry.Card.Id}",
                        $"{RitsuDebugCardActions.GetPileToken(entry.PileType)} {entry.Card.Type} {entry.Card.Rarity}",
                        badge: entry.Card.CurrentUpgradeLevel > 0 ? $"+{entry.Card.CurrentUpgradeLevel}" : null),
                    CreateCardPreviewModel(entry.Card),
                    entry.Card,
                    () => CreatePileCardDetail(entry),
                    GetCardStateHash(entry.Card))),
            ];
        }

        private static int GetPileCardSnapshotHash(IReadOnlyList<PileCardEntry> entries)
        {
            var hash = new HashCode();
            foreach (var entry in entries)
            {
                hash.Add(entry.StableId, StringComparer.Ordinal);
                hash.Add(GetCardStateHash(entry.Card));
            }

            return hash.ToHashCode();
        }

        private static bool IsDefaultPileCardEntry(RitsuCatalogItem item)
        {
            return RitsuDebugCardActions.GetMutablePileTypes()
                .Where(static pile => !RitsuDebugCardActions.IsRunStatePile(pile))
                .Any(pile => item.Id.StartsWith(
                    $"{RitsuDebugCardActions.GetPileToken(pile)}:",
                    StringComparison.Ordinal));
        }

        private static int GetCardStateHash(CardModel card)
        {
            var hash = new HashCode();
            hash.Add(card.CurrentUpgradeLevel);
            hash.Add(card.BaseReplayCount);
            hash.Add(card.EnergyCost.CostsX);
            hash.Add(card.EnergyCost.Canonical);
            hash.Add(card.EnergyCost.GetWithModifiers(CostModifiers.None));
            hash.Add(card.CanonicalStarCost);
            hash.Add(card.Enchantment?.Id.ToString(), StringComparer.Ordinal);
            hash.Add(card.Enchantment?.Amount);
            hash.Add(card.Affliction?.Id.ToString(), StringComparer.Ordinal);
            hash.Add(card.Affliction?.Amount);
            hash.Add(SafeCardDescription(card), StringComparer.Ordinal);
            hash.Add(GetCapabilityStateHash(card));
            hash.Add(GetCapabilityStateHash(card.Enchantment));
            hash.Add(GetCapabilityStateHash(card.Affliction));
            var localKeywords = card.GetKeywordsWithSources(KeywordSources.Local);
            hash.Add(localKeywords.Contains(CardKeyword.Exhaust));
            hash.Add(localKeywords.Contains(CardKeyword.Ethereal));
            hash.Add(localKeywords.Contains(CardKeyword.Unplayable));
            foreach (var (key, value) in card.DynamicVars.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
            {
                hash.Add(key, StringComparer.Ordinal);
                hash.Add(value.BaseValue);
            }

            return hash.ToHashCode();
        }

        private static RitsuCatalogItem[] CreateCombatantCatalogItems(
            IReadOnlyList<Player> players,
            IEnumerable<Creature> creatures)
        {
            var playerAccent = RitsuShellTheme.Current.Component.TextButton.Accent.Fg;
            var petAccent = PositiveAccent();
            var monsterAccent = RitsuShellTheme.Current.Component.TextButton.Danger.Fg;
            var ownerLabels = players
                .Select((player, index) => (player.NetId, Label: PlayerLabel(player, index)))
                .ToDictionary(static entry => entry.NetId, static entry => entry.Label);
            var nonPlayers = creatures
                .Where(static creature => !creature.IsPlayer)
                .OrderBy(static creature => creature.IsPet ? 0 : 1)
                .ThenBy(creature => creature.PetOwner == null
                    ? int.MaxValue
                    : FindPlayerIndex(creature.PetOwner))
                .ThenBy(static creature => creature.CombatId)
                .ToArray();
            return
            [
                .. players.Select((player, index) => new RitsuCatalogItem(
                    $"player:{player.NetId}",
                    PlayerLabel(player, index),
                    $"{L("ritsulib.debugTools.player", "Player")} · {PlayerVitals(player)}",
                    $"{player.Character.Id} {player.NetId}",
                    icon: RitsuDebugToolsIcons.Get(RitsuDebugToolsGlyph.Players, 32, playerAccent),
                    badge: player.NetId == RunManager.Instance.NetService?.NetId
                        ? L("ritsulib.debugTools.local", "Local")
                        : null,
                    accentColor: playerAccent)),
                .. nonPlayers.Select(creature => new RitsuCatalogItem(
                    $"creature:{creature.CombatId!.Value}",
                    creature.Name,
                    creature.PetOwner is { } owner
                        ? string.Format(
                            L("ritsulib.debugTools.petSummary", "Pet · Owner: {0} · {1}"),
                            ownerLabels.GetValueOrDefault(owner.NetId, owner.Character.Id.ToString()),
                            creature.ModelId)
                        : $"{L("ritsulib.debugTools.monster", "Monster")} · {creature.ModelId}",
                    creature.PetOwner is { } petOwner
                        ? $"{creature.ModelId} {creature.LogName} {petOwner.NetId} " +
                          ownerLabels.GetValueOrDefault(petOwner.NetId, petOwner.Character.Id.ToString())
                        : $"{creature.ModelId} {creature.LogName}",
                    icon: RitsuDebugToolsIcons.Get(
                        creature.IsPet ? RitsuDebugToolsGlyph.Paw : RitsuDebugToolsGlyph.Monsters,
                        32,
                        creature.IsPet ? petAccent : monsterAccent),
                    badge: CreatureVitals(creature),
                    accentColor: creature.IsPet ? petAccent : monsterAccent)),
            ];

            int FindPlayerIndex(Player player)
            {
                for (var index = 0; index < players.Count; index++)
                    if (players[index].NetId == player.NetId)
                        return index;
                return int.MaxValue;
            }
        }

        private RitsuCatalogBrowser CreateEncounterCatalog()
        {
            var models = ModelDb.AllEncounters.OrderBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase).ToArray();
            var byId = models.ToDictionary(static model => model.Id.ToString(), StringComparer.Ordinal);
            var acts = OrderedActs();
            var tierItemIds = BuildEncounterTierItemIds(
                models,
                static encounter => [encounter.Id.ToString()]);
            var browser = Browser(
                L("ritsulib.debugTools.search.encounters", "Search encounters by name or ID"),
                item => CreateEncounterDetail(byId[item.Id]),
                [
                    CreateActFilter(
                        acts,
                        static act => act.AllEncounters.Select(encounter => encounter.Id.ToString())),
                    CreateEncounterTierFilter(tierItemIds),
                ],
                presentation: RitsuCatalogPresentation.Grid,
                gridTileMinimumWidth: 240f,
                gridTileHeight: 88f,
                detailWidth: 460f);
            browser.SetItems([
                .. models.Select(model =>
                {
                    var monsters = GetEncounterMonsters(model);
                    var monsterNames = monsters.Select(SafeTitle).ToArray();
                    var summary = monsterNames.Length == 0
                        ? RoomLabel(model.RoomType)
                        : $"{RoomLabel(model.RoomType)} · {string.Join(", ", monsterNames.Take(2))}";
                    return new RitsuCatalogItem(
                        model.Id.ToString(),
                        SafeTitle(model),
                        summary,
                        $"{model.Id.Category} {string.Join(' ', monsterNames)}",
                        tooltip: BuildCatalogTooltip(
                            SafeTitle(model),
                            model.Id.ToString(),
                            RoomLabel(model.RoomType),
                            monsterNames.Length == 0
                                ? null
                                : string.Format(
                                    L("ritsulib.debugTools.possibleMonsters", "Possible enemies: {0}"),
                                    string.Join(", ", monsterNames))));
                }),
            ]);
            return browser;
        }

        private RitsuCatalogBrowser CreateMonsterCatalog()
        {
            var models = ModelDb.Monsters
                .DistinctBy(static model => model.Id)
                .OrderBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            var byId = models.ToDictionary(static model => model.Id.ToString(), StringComparer.Ordinal);
            var acts = OrderedActs();
            var tierItemIds = BuildEncounterTierItemIds(
                ModelDb.AllEncounters,
                encounter => GetEncounterMonsters(encounter).Select(monster => monster.Id.ToString()));
            var browser = Browser(
                L("ritsulib.debugTools.search.monsters", "Search monsters by name or ID"),
                item => CreateMonsterDetail(byId[item.Id]),
                [
                    CreateActFilter(
                        acts,
                        static act => act.AllMonsters.Select(monster => monster.Id.ToString())),
                    CreateEncounterTierFilter(tierItemIds),
                ],
                presentation: RitsuCatalogPresentation.Grid,
                gridTileMinimumWidth: 220f,
                gridTileHeight: 84f,
                detailWidth: 440f);
            browser.SetItems([
                .. models.Select(model => new RitsuCatalogItem(
                    model.Id.ToString(),
                    SafeTitle(model),
                    MonsterVitals(model),
                    model.Id.Category,
                    tooltip: BuildCatalogTooltip(
                        SafeTitle(model),
                        model.Id.ToString(),
                        MonsterVitals(model)))),
            ]);
            return browser;
        }

        private static ActModel[] OrderedActs()
        {
            return
            [
                .. ModelDb.Acts
                    .OrderBy(static act => act.Index)
                    .ThenBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase),
            ];
        }

        private static RitsuCatalogFilter CreateActFilter(
            IEnumerable<ActModel> acts,
            Func<ActModel, IEnumerable<string>> itemIdsFactory)
        {
            return new(
                "act",
                L("ritsulib.debugTools.filter.act", "Act"),
                L("ritsulib.debugTools.filter.allActs", "All acts"),
                [
                    .. acts.Select(act =>
                    {
                        var itemIds = itemIdsFactory(act).ToHashSet(StringComparer.Ordinal);
                        return new RitsuCatalogFilterOption(
                            act.Id.ToString(),
                            SafeTitle(act),
                            item => itemIds.Contains(item.Id));
                    }),
                ]);
        }

        private static Dictionary<EncounterTier, HashSet<string>> BuildEncounterTierItemIds(
            IEnumerable<EncounterModel> encounters,
            Func<EncounterModel, IEnumerable<string>> itemIdsFactory)
        {
            var itemIdsByTier = Enum.GetValues<EncounterTier>()
                .ToDictionary(static tier => tier, static _ => new HashSet<string>(StringComparer.Ordinal));
            foreach (var encounter in encounters)
            {
                var tier = EncounterTierFor(encounter);
                if (!tier.HasValue)
                    continue;
                itemIdsByTier[tier.Value].UnionWith(itemIdsFactory(encounter));
            }

            return itemIdsByTier;
        }

        private static EncounterTier? EncounterTierFor(EncounterModel encounter)
        {
            return encounter.RoomType switch
            {
                RoomType.Monster when encounter.IsWeak => EncounterTier.Weak,
                RoomType.Monster => EncounterTier.Strong,
                RoomType.Elite => EncounterTier.Elite,
                RoomType.Boss => EncounterTier.Boss,
                _ => null,
            };
        }

        private static RitsuCatalogFilter CreateEncounterTierFilter(
            IReadOnlyDictionary<EncounterTier, HashSet<string>> itemIdsByTier)
        {
            return new(
                "encounterTier",
                L("ritsulib.debugTools.filter.encounterTier", "Enemy tier"),
                L("ritsulib.debugTools.filter.allTiers", "All tiers"),
                [
                    TierOption(EncounterTier.Weak, "weak", "Weak"),
                    TierOption(EncounterTier.Strong, "strong", "Strong"),
                    TierOption(EncounterTier.Elite, "elite", "Elite"),
                    TierOption(EncounterTier.Boss, "boss", "Boss"),
                ]);

            RitsuCatalogFilterOption TierOption(EncounterTier tier, string id, string fallback)
            {
                return new(
                    id,
                    L($"ritsulib.debugTools.filter.{id}", fallback),
                    item => itemIdsByTier[tier].Contains(item.Id));
            }
        }

        private RitsuCatalogBrowser CreateRoomCatalog()
        {
            var roomTypes = Enum.GetValues<RoomType>()
                .Where(static roomType => roomType != RoomType.Unassigned)
                .ToArray();
            var byId = roomTypes.ToDictionary(static roomType => roomType.ToString(), StringComparer.Ordinal);
            var browser = Browser(
                L("ritsulib.debugTools.search.rooms", "Search room types"),
                item => CreateRoomDetail(byId[item.Id]),
                presentation: RitsuCatalogPresentation.Grid,
                gridTileMinimumWidth: 150f,
                gridTileHeight: 86f);
            browser.SetItems([
                .. roomTypes.Select(roomType => new RitsuCatalogItem(
                    roomType.ToString(),
                    RoomLabel(roomType),
                    null,
                    roomType.ToString(),
                    tooltip: BuildCatalogTooltip(RoomLabel(roomType), roomType.ToString()))),
            ]);
            return browser;
        }

        private RitsuCatalogBrowser CreateEventCatalog()
        {
            var models = ModelDb.AllEvents.Concat(ModelDb.AllAncients)
                .DistinctBy(static model => model.Id)
                .OrderBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            var byId = models.ToDictionary(static model => model.Id.ToString(), StringComparer.Ordinal);
            var kindFilter = new RitsuCatalogFilter(
                "eventKind",
                L("ritsulib.debugTools.filter.eventKind", "Event kind"),
                L("ritsulib.debugTools.filter.all", "All"),
                [
                    new("events", L("ritsulib.debugTools.filter.events", "Events"),
                        item => byId[item.Id] is not AncientEventModel),
                    new("ancients", L("ritsulib.debugTools.filter.ancients", "Ancients"),
                        item => byId[item.Id] is AncientEventModel),
                ]);
            var browser = Browser(
                L("ritsulib.debugTools.search.events", "Search events by name or ID"),
                item => CreateEventDetail(byId[item.Id]),
                [
                    CreateActFilter(
                        OrderedActs(),
                        static act => act.AllEvents
                            .Select(model => model.Id.ToString())
                            .Concat(act.AllAncients.Select(model => model.Id.ToString()))),
                    kindFilter,
                ],
                RitsuCatalogPresentation.Grid,
                180f,
                90f);
            browser.SetItems([
                .. models.Select(model => ModelItem(
                    model,
                    model is AncientEventModel
                        ? L("ritsulib.debugTools.ancient", "Ancient")
                        : L("ritsulib.debugTools.event", "Event"),
                    null)),
            ]);
            return browser;
        }

        private static RitsuCatalogBrowser Browser(
            string searchPlaceholder,
            Func<RitsuCatalogItem, Control> detailFactory,
            IReadOnlyList<RitsuCatalogFilter>? filters = null,
            RitsuCatalogPresentation presentation = RitsuCatalogPresentation.List,
            float gridTileMinimumWidth = 112f,
            float gridTileHeight = 104f,
            RitsuCatalogDetailPresentation detailPresentation = RitsuCatalogDetailPresentation.Drawer,
            float catalogWidth = 260f,
            float detailWidth = 360f,
            float rowHeight = 52f)
        {
            return new(new()
            {
                SearchPlaceholder = searchPlaceholder,
                EmptyText = L("ritsulib.debugTools.noMatches", "No matching items"),
                DetailPlaceholderText = L("ritsulib.debugTools.selectItem", "Select an item to view actions"),
                DetailUnavailableText = L("ritsulib.debugTools.detailsUnavailable",
                    "Details are unavailable for this item."),
                MinimumHeight = 460f,
                CatalogWidth = catalogWidth,
                DetailMinimumWidth = detailWidth,
                RowHeight = rowHeight,
                Presentation = presentation,
                DetailPresentation = detailPresentation,
                GridTileMinimumWidth = gridTileMinimumWidth,
                GridTileHeight = gridTileHeight,
                DetailFactory = detailFactory,
            }, filters);
        }

        private static RitsuCatalogItem ModelItem(
            AbstractModel model,
            string category,
            Func<Texture2D?>? iconFactory,
            Color? accentColor = null)
        {
            var source = ContentSourceResolver.Resolve(model);
            return new(
                model.Id.ToString(),
                SafeTitle(model),
                $"{category} · {ContentSourceDisplayLabel(source)} · {model.Id}",
                $"{model.Id.Category} {source.ModId} {source.DisplayName}",
                badge: category,
                iconFactory: iconFactory,
                accentColor: accentColor);
        }

        private static Color PowerTypeAccent(PowerType type)
        {
            return type switch
            {
                PowerType.Buff => PositiveAccent(),
                PowerType.Debuff => RitsuShellTheme.Current.Component.TextButton.Danger.Fg,
                _ => RitsuShellTheme.Current.Text.LabelSecondary,
            };
        }

        private static Color PositiveAccent()
        {
            return new(0.3f, 0.8f, 0.56f);
        }

        private static RitsuCatalogFilter CreateContentSourceFilter<TModel>(
            IReadOnlyCollection<TModel> models,
            IReadOnlyDictionary<string, TModel> modelsById)
            where TModel : AbstractModel
        {
            var sourceByItemId = models.ToDictionary(
                static model => model.Id.ToString(),
                static model => ContentSourceResolver.Resolve(model),
                StringComparer.Ordinal);
            var sources = sourceByItemId.Values
                .DistinctBy(static source => source.ModId, StringComparer.OrdinalIgnoreCase)
                .OrderBy(static source => string.Equals(source.ModId, "Vanilla", StringComparison.OrdinalIgnoreCase)
                    ? 0
                    : 1)
                .ThenBy(ContentSourceDisplayLabel, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            return new(
                "source",
                L("ritsulib.debugTools.filter.source", "Source"),
                L("ritsulib.debugTools.filter.allSources", "All sources"),
                [
                    .. sources.Select(source => new RitsuCatalogFilterOption(
                        source.ModId,
                        ContentSourceDisplayLabel(source),
                        item => modelsById.TryGetValue(item.Id, out var model) &&
                                string.Equals(ContentSourceResolver.Resolve(model).ModId, source.ModId,
                                    StringComparison.OrdinalIgnoreCase))),
                ]);
        }

        private static string ContentSourceSearchText(AbstractModel model)
        {
            var source = ContentSourceResolver.Resolve(model);
            return $"{source.ModId} {source.DisplayName}";
        }

        private static string ContentSourceDisplayLabel(ContentSourceDescriptor source)
        {
            var displayName = string.IsNullOrWhiteSpace(source.DisplayName) ? source.ModId : source.DisplayName;
            return string.Equals(displayName, source.ModId, StringComparison.OrdinalIgnoreCase)
                ? displayName
                : $"{displayName} ({source.ModId})";
        }

        private static MonsterModel[] GetEncounterMonsters(EncounterModel encounter)
        {
            try
            {
                return
                [
                    .. encounter.AllPossibleMonsters
                        .Where(static monster => monster != null)
                        .DistinctBy(static monster => monster.Id)
                        .Take(12),
                ];
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugToolsUi] Could not list monsters for encounter '{encounter.Id}': {ex}");
                return [];
            }
        }

        private static string BuildCatalogTooltip(params string?[] lines)
        {
            return string.Join('\n', lines.Where(static line => !string.IsNullOrWhiteSpace(line)));
        }

        private static RitsuCatalogFilter EnumFilter<TValue>(
            string id,
            string label,
            IEnumerable<TValue> values,
            Func<TValue, string> labelFactory,
            Func<RitsuCatalogItem, TValue, bool> matches)
            where TValue : struct, Enum
        {
            return new(
                id,
                label,
                L("ritsulib.debugTools.filter.all", "All"),
                [
                    .. values.OrderBy(static value => Convert.ToInt32(value))
                        .Select(value => new RitsuCatalogFilterOption(
                            value.ToString(),
                            labelFactory(value),
                            item => matches(item, value))),
                ]);
        }

        private static string EnumLabel<TValue>(TValue value)
            where TValue : struct, Enum
        {
            return L($"ritsulib.debugTools.enum.{typeof(TValue).Name}.{value}", value.ToString());
        }

        private static string PileLabel(PileType pileType)
        {
            if (!ModCardPileRegistry.TryGetByPileType(pileType, out var definition))
                return EnumLabel(pileType);
            try
            {
                var title = definition.Title.GetFormattedText()?.Trim();
                return string.IsNullOrWhiteSpace(title) ? definition.Id : title;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                return definition.Id;
            }
        }

        private static string RoomLabel(RoomType roomType)
        {
            if (roomType == RoomType.Map)
                return L("ritsulib.debugTools.room.map", "Map");
            var key = roomType switch
            {
                RoomType.Monster => "ROOM_ENEMY",
                RoomType.Elite => "ROOM_ELITE",
                RoomType.Boss => "ROOM_BOSS",
                RoomType.Treasure => "ROOM_TREASURE",
                RoomType.Shop => "ROOM_MERCHANT",
                RoomType.Event => "ROOM_EVENT",
                RoomType.RestSite => "ROOM_REST",
                _ => null,
            };
            return key == null
                ? roomType.ToString()
                : new LocString("static_hover_tips", $"{key}.title").GetFormattedText();
        }

        private static bool IsVisibleCombatant(Creature creature)
        {
            return creature.CombatId.HasValue && (creature.IsPlayer || !creature.IsDead);
        }

        private static string SafeTitle(AbstractModel model)
        {
            try
            {
                if (model.TryResolveTitle(out var title))
                {
                    var formatted = title.GetFormattedText()?.Trim();
                    if (!string.IsNullOrWhiteSpace(formatted))
                        return formatted;
                }
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[DebugToolsUi] Could not resolve title for '{model.Id}': {ex.Message}");
            }

            return model.Id.Entry;
        }

        private static string SafeDescription(Func<string?> descriptionFactory)
        {
            try
            {
                return (descriptionFactory() ?? string.Empty).Trim();
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[DebugToolsUi] Could not resolve model description: {ex.Message}");
                return string.Empty;
            }
        }

        private static string SafeCardDescription(CardModel card)
        {
            var description = SafeDescription(() => CreateCardPreviewModel(card).GetDescriptionForPile(PileType.None));
            return string.IsNullOrWhiteSpace(description)
                ? L("ritsulib.debugTools.descriptionUnavailable",
                    "A description preview is unavailable for this card.")
                : description;
        }

        private static string PlayerVitals(Player player)
        {
            return string.Format(
                L("ritsulib.debugTools.playerVitals", "HP {0}/{1} · Gold {2}"),
                player.Creature.CurrentHp,
                player.Creature.MaxHp,
                player.Gold);
        }

        private static string CreatureVitals(Creature creature)
        {
            return string.Format(
                L("ritsulib.debugTools.creatureVitals", "HP {0}/{1} · Block {2}"),
                creature.CurrentHp,
                creature.MaxHp,
                creature.Block);
        }

        private static string CreatureDetailDescription(Creature creature)
        {
            if (creature.PetOwner is not { } owner)
                return creature.LogName;
            var players = GetPlayers();
            var ownerIndex = Array.FindIndex(players, player => player.NetId == owner.NetId);
            var ownerLabel = ownerIndex >= 0
                ? PlayerLabel(owner, ownerIndex)
                : owner.Character.Id.ToString();
            return string.Format(
                L("ritsulib.debugTools.petOwnerDetail", "Owner: {0} · {1}"),
                ownerLabel,
                creature.LogName);
        }

        private static string MonsterVitals(MonsterModel monster)
        {
            return string.Format(
                L("ritsulib.debugTools.monsterVitals", "Starting HP {0}–{1}"),
                monster.MinInitialHp,
                monster.MaxInitialHp);
        }

        private static CardModel CreateCardPreviewModel(CardModel card)
        {
            if (card is not MadScience { TinkerTimeType: CardType.None })
                return card;
            var clone = (MadScience)card.MutableClone();
            clone.TinkerTimeType = CardType.Attack;
            return clone;
        }

        private static string CardCost(CardModel card)
        {
            if (card.EnergyCost.CostsX)
                return "X";
            if (card.EnergyCost.Canonical >= 0)
                return card.EnergyCost.Canonical.ToString();
            if (card.CanonicalStarCost >= 0)
                return $"★{card.CanonicalStarCost}";
            return "—";
        }

        private enum EncounterTier
        {
            Weak,
            Strong,
            Elite,
            Boss,
        }

        private readonly record struct PileCardEntry(
            PileType PileType,
            int Index,
            CardModel Card,
            uint? CombatCardId)
        {
            internal string StableId => CombatCardId.HasValue
                ? $"{RitsuDebugCardActions.GetPileToken(PileType)}:combat:{CombatCardId.Value}"
                : $"{RitsuDebugCardActions.GetPileToken(PileType)}:state:{Index}:{Card.Id}";
        }
    }
}
