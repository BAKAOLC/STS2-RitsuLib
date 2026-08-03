using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Models;
using STS2RitsuLib.Ui.Catalog;

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
            };
            return new RitsuDebugCardCatalog(
                L("ritsulib.debugTools.search.cards", "Search cards by name or ID"),
                [
                    .. cards.Select(card => new RitsuDebugCardCatalogEntry(
                        new(
                            card.Id.ToString(),
                            SafeTitle(card),
                            $"{EnumLabel(card.Type)} · {EnumLabel(card.Rarity)} · {card.Id}",
                            $"{card.Type} {card.Rarity}",
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

            if (entries.Length == 0)
                return EmptyBrowser(L("ritsulib.debugTools.empty.pileCards",
                    "The selected player has no cards in a supported pile."));

            var filter = EnumFilter(
                "pile",
                L("ritsulib.debugTools.filter.pile", "Pile"),
                RitsuDebugCardActions.GetMutablePileNames().Select(static name => Enum.Parse<PileType>(name)),
                EnumLabel,
                (item, value) => item.Id.StartsWith($"{value}:", StringComparison.Ordinal));
            return new RitsuDebugCardCatalog(
                L("ritsulib.debugTools.search.pileCards", "Search the target player's cards"),
                CreatePileCardCatalogEntries(entries),
                [filter],
                filter.Id,
                nameof(PileType.Hand),
                nameof(PileType.Deck));
        }

        private RitsuCatalogBrowser CreateRelicCatalog()
        {
            var models = ModelDb.AllRelics.OrderBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase).ToArray();
            var byId = models.ToDictionary(static model => model.Id.ToString(), StringComparer.Ordinal);
            var filter = EnumFilter(
                "rarity",
                L("ritsulib.debugTools.filter.rarity", "Rarity"),
                models.Select(static model => model.Rarity).Distinct(),
                EnumLabel,
                (item, value) => byId[item.Id].Rarity == value);
            var browser = Browser(
                L("ritsulib.debugTools.search.relics", "Search relics by name or ID"),
                item => CreateRelicDetail(byId[item.Id]),
                [filter],
                RitsuCatalogPresentation.Grid);
            browser.SetItems([
                .. models.Select(model => ModelItem(
                    model,
                    EnumLabel(model.Rarity),
                    () => model.Icon)),
            ]);
            return browser;
        }

        private RitsuCatalogBrowser CreatePotionCatalog()
        {
            var models = ModelDb.AllPotions.OrderBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase).ToArray();
            var byId = models.ToDictionary(static model => model.Id.ToString(), StringComparer.Ordinal);
            var filter = EnumFilter(
                "rarity",
                L("ritsulib.debugTools.filter.rarity", "Rarity"),
                models.Select(static model => model.Rarity).Distinct(),
                EnumLabel,
                (item, value) => byId[item.Id].Rarity == value);
            var browser = Browser(
                L("ritsulib.debugTools.search.potions", "Search potions by name or ID"),
                item => CreatePotionDetail(byId[item.Id]),
                [filter],
                RitsuCatalogPresentation.Grid);
            browser.SetItems([
                .. models.Select(model => ModelItem(
                    model,
                    EnumLabel(model.Rarity),
                    () => model.Image)),
            ]);
            return browser;
        }

        private RitsuCatalogBrowser CreatePowerCatalog()
        {
            var models = ModelDb.AllPowers.OrderBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase).ToArray();
            var byId = models.ToDictionary(static model => model.Id.ToString(), StringComparer.Ordinal);
            var filter = EnumFilter(
                "type",
                L("ritsulib.debugTools.filter.type", "Type"),
                models.Select(static model => model.Type).Distinct(),
                EnumLabel,
                (item, value) => byId[item.Id].Type == value);
            var browser = Browser(
                L("ritsulib.debugTools.search.powers", "Search powers by name or ID"),
                item => CreatePowerDetail(byId[item.Id]),
                [filter],
                RitsuCatalogPresentation.Grid,
                detailWidth: 480f);
            browser.SetItems([
                .. models.Select(model => ModelItem(
                    model,
                    EnumLabel(model.Type),
                    () => model.Icon)),
            ]);
            return browser;
        }

        private RitsuCatalogBrowser CreatePlayerCatalog()
        {
            var players = GetPlayers();
            var byId = players.ToDictionary(static player => player.NetId.ToString(), StringComparer.Ordinal);
            var browser = Browser(
                L("ritsulib.debugTools.search.players", "Search players"),
                item => CreatePlayerDetail(byId[item.Id]),
                presentation: RitsuCatalogPresentation.Grid,
                gridTileMinimumWidth: 220f,
                gridTileHeight: 84f,
                detailWidth: 480f);
            browser.SetItems(CreatePlayerCatalogItems(players));
            return browser;
        }

        private RitsuCatalogBrowser CreateCreatureCatalog()
        {
            var creatures = CombatManager.Instance.DebugOnlyGetState()?.Creatures
                .Where(static creature => creature.CombatId.HasValue)
                .OrderBy(static creature => creature.CombatId)
                .ToArray() ?? [];
            var byId = creatures.ToDictionary(
                static creature => creature.CombatId!.Value.ToString(),
                StringComparer.Ordinal);
            var filter = new RitsuCatalogFilter(
                "side",
                L("ritsulib.debugTools.filter.side", "Side"),
                L("ritsulib.debugTools.filter.all", "All"),
                [
                    new("players", L("ritsulib.debugTools.filter.players", "Players"),
                        item => byId[item.Id].IsPlayer),
                    new("nonPlayers", L("ritsulib.debugTools.filter.nonPlayers", "Enemies and summons"),
                        item => !byId[item.Id].IsPlayer),
                ]);
            var browser = Browser(
                L("ritsulib.debugTools.search.creatures", "Search combat creatures"),
                item => CreateCreatureDetail(byId[item.Id]),
                [filter],
                RitsuCatalogPresentation.Grid,
                220f,
                84f,
                detailWidth: 520f);
            browser.SetItems(CreateCreatureCatalogItems(creatures));
            return browser;
        }

        private static PileCardEntry[] GetPileCardEntries(Player player)
        {
            var entries = new List<PileCardEntry>();
            foreach (var pileType in RitsuDebugCardActions.GetMutablePileNames()
                         .Select(static name => Enum.Parse<PileType>(name)))
            {
                var pile = RitsuDebugCardActions.GetPile(player, pileType);
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
                        $"{EnumLabel(entry.PileType)} #{entry.Index + 1} · {entry.Card.Id}",
                        $"{entry.PileType} {entry.Card.Type} {entry.Card.Rarity}",
                        badge: entry.Card.CurrentUpgradeLevel > 0 ? $"+{entry.Card.CurrentUpgradeLevel}" : null),
                    CreateCardPreviewModel(entry.Card),
                    entry.Card,
                    () => CreatePileCardDetail(entry))),
            ];
        }

        private static RitsuCatalogItem[] CreatePlayerCatalogItems(IReadOnlyList<Player> players)
        {
            return
            [
                .. players.Select((player, index) => new RitsuCatalogItem(
                    player.NetId.ToString(),
                    PlayerLabel(player, index),
                    PlayerVitals(player),
                    player.Character.Id.ToString(),
                    badge: player.NetId == RunManager.Instance.NetService?.NetId
                        ? L("ritsulib.debugTools.local", "Local")
                        : null)),
            ];
        }

        private static RitsuCatalogItem[] CreateCreatureCatalogItems(IEnumerable<Creature> creatures)
        {
            return
            [
                .. creatures.Select(creature => new RitsuCatalogItem(
                    creature.CombatId!.Value.ToString(),
                    creature.Name,
                    $"{(creature.IsPlayer
                        ? L("ritsulib.debugTools.player", "Player")
                        : L("ritsulib.debugTools.enemy", "Enemy"))} · {creature.ModelId}",
                    $"{creature.ModelId} {creature.LogName}",
                    badge: CreatureVitals(creature))),
            ];
        }

        private RitsuCatalogBrowser CreateEncounterCatalog()
        {
            var models = ModelDb.AllEncounters.OrderBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase).ToArray();
            var byId = models.ToDictionary(static model => model.Id.ToString(), StringComparer.Ordinal);
            var browser = Browser(
                L("ritsulib.debugTools.search.encounters", "Search encounters by name or ID"),
                item => CreateEncounterDetail(byId[item.Id]),
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
            var browser = Browser(
                L("ritsulib.debugTools.search.monsters", "Search monsters by name or ID"),
                item => CreateMonsterDetail(byId[item.Id]),
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
            var filter = new RitsuCatalogFilter(
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
                [filter],
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
            Func<Texture2D?>? iconFactory)
        {
            return new(
                model.Id.ToString(),
                SafeTitle(model),
                $"{category} · {model.Id}",
                model.Id.Category,
                badge: category,
                iconFactory: iconFactory);
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

        private static string RoomLabel(RoomType roomType)
        {
            var key = roomType switch
            {
                RoomType.Monster => "ROOM_ENEMY",
                RoomType.Elite => "ROOM_ELITE",
                RoomType.Boss => "ROOM_BOSS",
                RoomType.Treasure => "ROOM_TREASURE",
                RoomType.Shop => "ROOM_MERCHANT",
                RoomType.Event => "ROOM_EVENT",
                RoomType.RestSite => "ROOM_REST",
                RoomType.Map => "ROOM_MAP",
                _ => null,
            };
            return key == null
                ? roomType.ToString()
                : new LocString("static_hover_tips", $"{key}.title").GetFormattedText();
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

        private readonly record struct PileCardEntry(
            PileType PileType,
            int Index,
            CardModel Card,
            uint? CombatCardId)
        {
            internal string StableId => CombatCardId.HasValue
                ? $"{PileType}:combat:{CombatCardId.Value}"
                : $"{PileType}:deck:{Index}:{Card.Id}";
        }
    }
}
