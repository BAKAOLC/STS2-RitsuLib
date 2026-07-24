using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Validation;
using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Saves
{
    /// <summary>
    ///     Holds progress-save entries whose model IDs are unavailable in the current mod set so they can be written
    ///     back without participating in runtime progress logic.
    /// </summary>
    public sealed class PreservedProgressRecords
    {
        private static readonly ConditionalWeakTable<ProgressState, PreservedProgressRecords> RecordsByProgress = new();
        private static readonly HashSet<string> KnownAchievementNames = BuildKnownAchievementNames();

        private static readonly FieldInfo? ValidationErrorsField =
            typeof(DeserializationContext).GetField("_errors", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<AncientStats> _ancientStats = [];
        private readonly List<CardStats> _cardStats = [];

        private readonly List<CharacterStats> _characterStats = [];
        private readonly List<ModelId> _discoveredActs = [];
        private readonly List<ModelId> _discoveredCards = [];
        private readonly List<ModelId> _discoveredEvents = [];
        private readonly List<ModelId> _discoveredPotions = [];
        private readonly List<ModelId> _discoveredRelics = [];
        private readonly List<EncounterStats> _encounterStats = [];
        private readonly List<EnemyStats> _enemyStats = [];
        private readonly List<SerializableEpoch> _epochs = [];
        private readonly List<SerializableUnlockedAchievement> _unlockedAchievements = [];

        private ModelId? _pendingCharacterUnlock;

        private bool HasAny =>
            _characterStats.Count > 0 ||
            _cardStats.Count > 0 ||
            _encounterStats.Count > 0 ||
            _enemyStats.Count > 0 ||
            _ancientStats.Count > 0 ||
            _discoveredCards.Count > 0 ||
            _discoveredRelics.Count > 0 ||
            _discoveredPotions.Count > 0 ||
            _discoveredEvents.Count > 0 ||
            _discoveredActs.Count > 0 ||
            _epochs.Count > 0 ||
            _unlockedAchievements.Count > 0 ||
            IsSavableModelId(_pendingCharacterUnlock);

        internal static PreservedProgressRecords? Capture(SerializableProgress save)
        {
            ArgumentNullException.ThrowIfNull(save);

            var records = new PreservedProgressRecords();
            records.CaptureCharacterStats(save.CharStats);
            records.CaptureCardStats(save.CardStats);
            records.CaptureEncounterStats(save.EncounterStats);
            records.CaptureEnemyStats(save.EnemyStats);
            records.CaptureAncientStats(save.AncientStats);
            records.CaptureDiscoveredSet<CardModel>(save.DiscoveredCards, records._discoveredCards);
            records.CaptureDiscoveredSet<RelicModel>(save.DiscoveredRelics, records._discoveredRelics);
            records.CaptureDiscoveredSet<PotionModel>(save.DiscoveredPotions, records._discoveredPotions);
            records.CaptureDiscoveredSet<EventModel>(save.DiscoveredEvents, records._discoveredEvents);
            records.CaptureDiscoveredSet<ActModel>(save.DiscoveredActs, records._discoveredActs);
            records.CaptureEpochs(save.Epochs);
            records.CaptureAchievements(save.UnlockedAchievements);

            if (IsUnknownModel<CharacterModel>(save.PendingCharacterUnlock))
                records._pendingCharacterUnlock = save.PendingCharacterUnlock;

            return records.HasAny ? records : null;
        }

        internal static void Attach(ProgressState? progress, PreservedProgressRecords? records)
        {
            if (progress == null || records is not { HasAny: true })
                return;

            RecordsByProgress.Remove(progress);
            RecordsByProgress.Add(progress, records);
            RitsuLibFramework.Logger.Info($"[Saves] Preserving unavailable progress records: {records.FormatCounts()}");
        }

        internal int SuppressExpectedWarnings(DeserializationContext ctx)
        {
            if (ValidationErrorsField?.GetValue(ctx) is not List<ValidationError> errors)
                return 0;

            var removed = errors.RemoveAll(IsExpectedPreservedWarning);
            if (removed > 0)
                RitsuLibFramework.Logger.Info(
                    $"[Saves] Suppressed {removed} expected progress validation warning(s) for unavailable preserved records");

            return removed;
        }

        internal static void MergeInto(ProgressState? progress, SerializableProgress? save)
        {
            if (progress == null || save == null)
                return;

            if (RecordsByProgress.TryGetValue(progress, out var records))
                records.MergeInto(save);
        }

        internal static void MergeSerializableProgressRecords(SerializableProgress target, SerializableProgress source)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(source);

            MergeCharacterStats(target.CharStats, source.CharStats);
            MergeCardStats(target.CardStats, source.CardStats);
            MergeEncounterStats(target.EncounterStats, source.EncounterStats);
            MergeEnemyStats(target.EnemyStats, source.EnemyStats);
            MergeAncientStats(target.AncientStats, source.AncientStats);
            AppendMissingIds(target.DiscoveredCards, source.DiscoveredCards);
            AppendMissingIds(target.DiscoveredRelics, source.DiscoveredRelics);
            AppendMissingIds(target.DiscoveredPotions, source.DiscoveredPotions);
            AppendMissingIds(target.DiscoveredEvents, source.DiscoveredEvents);
            AppendMissingIds(target.DiscoveredActs, source.DiscoveredActs);
            MergeEpochs(target.Epochs, source.Epochs);
            MergeAchievements(target.UnlockedAchievements, source.UnlockedAchievements);

            if (target.PendingCharacterUnlock == ModelId.none && IsSavableModelId(source.PendingCharacterUnlock))
                target.PendingCharacterUnlock = source.PendingCharacterUnlock;
        }

        private void CaptureCharacterStats(List<CharacterStats> source)
        {
            foreach (var stats in source.Where(stats => IsUnknownModel<CharacterModel>(stats.Id)))
                _characterStats.Add(Clone(stats));
        }

        private void CaptureCardStats(List<CardStats> source)
        {
            foreach (var stats in source.Where(stats => IsUnknownModel<CardModel>(stats.Id)))
                _cardStats.Add(Clone(stats));
        }

        private void CaptureEncounterStats(List<EncounterStats> source)
        {
            foreach (var stats in source)
            {
                if (!IsSavableModelId(stats.Id))
                    continue;

                var missingEncounter = IsUnknownModel<EncounterModel>(stats.Id);
                if (missingEncounter)
                {
                    _encounterStats.Add(Clone(stats));
                    continue;
                }

                var missingFightStats = stats.FightStats
                    .Where(static fight => IsUnknownModel<CharacterModel>(fight.Character))
                    .Select(Clone)
                    .ToList();
                if (missingFightStats.Count > 0)
                    _encounterStats.Add(new() { Id = stats.Id, FightStats = missingFightStats });
            }
        }

        private void CaptureEnemyStats(List<EnemyStats> source)
        {
            foreach (var stats in source)
            {
                if (!IsSavableModelId(stats.Id))
                    continue;

                var missingEnemy = IsUnknownModel<MonsterModel>(stats.Id);
                if (missingEnemy)
                {
                    _enemyStats.Add(Clone(stats));
                    continue;
                }

                var missingFightStats = stats.FightStats
                    .Where(static fight => IsUnknownModel<CharacterModel>(fight.Character))
                    .Select(Clone)
                    .ToList();
                if (missingFightStats.Count > 0)
                    _enemyStats.Add(new() { Id = stats.Id, FightStats = missingFightStats });
            }
        }

        private void CaptureAncientStats(List<AncientStats> source)
        {
            foreach (var stats in source)
            {
                if (!IsSavableModelId(stats.Id))
                    continue;

                var missingAncient = IsUnknownModel<EventModel>(stats.Id);
                if (missingAncient)
                {
                    _ancientStats.Add(Clone(stats));
                    continue;
                }

                var missingCharacterStats = stats.CharStats
                    .Where(static charStats => IsUnknownModel<CharacterModel>(charStats.Character))
                    .Select(Clone)
                    .ToList();
                if (missingCharacterStats.Count > 0)
                    _ancientStats.Add(new() { Id = stats.Id, CharStats = missingCharacterStats });
            }
        }

        private void CaptureDiscoveredSet<TModel>(List<ModelId> source, List<ModelId> target)
            where TModel : AbstractModel
        {
            foreach (var id in source.Where(id => IsUnknownModel<TModel>(id) && !target.Contains(id)))
                target.Add(id);
        }

        private void CaptureEpochs(List<SerializableEpoch> source)
        {
            foreach (var epoch in source.Where(epoch => !string.IsNullOrWhiteSpace(epoch.Id) &&
                                                        !EpochModel.IsValid(epoch.Id) &&
                                                        Enum.IsDefined(epoch.State) &&
                                                        epoch.State >= EpochState.NotObtained).Where(epoch =>
                         _epochs.All(e => e.Id != epoch.Id)))
                _epochs.Add(Clone(epoch));
        }

        private void CaptureAchievements(List<SerializableUnlockedAchievement>? source)
        {
            if (source == null)
                return;

            foreach (var achievement in source.Where(achievement =>
                         !string.IsNullOrWhiteSpace(achievement.Achievement) &&
                         !KnownAchievementNames.Contains(achievement.Achievement)).Where(achievement =>
                         _unlockedAchievements.All(a => a.Achievement != achievement.Achievement)))
                _unlockedAchievements.Add(Clone(achievement));
        }

        private void MergeInto(SerializableProgress save)
        {
            AppendMissingById(save.CharStats, _characterStats, static stats => stats.Id, Clone);
            AppendMissingById(save.CardStats, _cardStats, static stats => stats.Id, Clone);
            MergeEncounterStats(save.EncounterStats);
            MergeEnemyStats(save.EnemyStats);
            MergeAncientStats(save.AncientStats);
            AppendMissingIds(save.DiscoveredCards, _discoveredCards);
            AppendMissingIds(save.DiscoveredRelics, _discoveredRelics);
            AppendMissingIds(save.DiscoveredPotions, _discoveredPotions);
            AppendMissingIds(save.DiscoveredEvents, _discoveredEvents);
            AppendMissingIds(save.DiscoveredActs, _discoveredActs);
            AppendMissingById(save.Epochs, _epochs, static epoch => epoch.Id, Clone);
            AppendMissingById(save.UnlockedAchievements, _unlockedAchievements,
                static achievement => achievement.Achievement, Clone);

            var pendingCharacterUnlock = _pendingCharacterUnlock;
            if (save.PendingCharacterUnlock == ModelId.none &&
                pendingCharacterUnlock != null &&
                pendingCharacterUnlock != ModelId.none)
                save.PendingCharacterUnlock = pendingCharacterUnlock;
        }

        private void MergeEncounterStats(List<EncounterStats> target)
        {
            foreach (var preserved in _encounterStats)
            {
                var existing = target.FirstOrDefault(stats => stats.Id == preserved.Id);
                if (existing == null)
                    target.Add(Clone(preserved));
                else
                    AppendMissingById(existing.FightStats, preserved.FightStats,
                        static fight => fight.Character, Clone);
            }
        }

        private void MergeEnemyStats(List<EnemyStats> target)
        {
            foreach (var preserved in _enemyStats)
            {
                var existing = target.FirstOrDefault(stats => stats.Id == preserved.Id);
                if (existing == null)
                    target.Add(Clone(preserved));
                else
                    AppendMissingById(existing.FightStats, preserved.FightStats,
                        static fight => fight.Character, Clone);
            }
        }

        private void MergeAncientStats(List<AncientStats> target)
        {
            foreach (var preserved in _ancientStats)
            {
                var existing = target.FirstOrDefault(stats => stats.Id == preserved.Id);
                if (existing == null)
                    target.Add(Clone(preserved));
                else
                    AppendMissingById(existing.CharStats, preserved.CharStats,
                        static stats => stats.Character, Clone);
            }
        }

        private string FormatCounts()
        {
            return string.Join(", ", new[]
            {
                FormatCount("characters", _characterStats.Count),
                FormatCount("cards", _cardStats.Count),
                FormatCount("encounters", _encounterStats.Count),
                FormatCount("enemies", _enemyStats.Count),
                FormatCount("ancients", _ancientStats.Count),
                FormatCount("discoveries",
                    _discoveredCards.Count + _discoveredRelics.Count + _discoveredPotions.Count +
                    _discoveredEvents.Count + _discoveredActs.Count),
                FormatCount("epochs", _epochs.Count),
                FormatCount("achievements", _unlockedAchievements.Count),
                FormatCount("pendingUnlock", IsSavableModelId(_pendingCharacterUnlock) ? 1 : 0),
            }.Where(static part => part.Length > 0));
        }

        private static string FormatCount(string label, int count)
        {
            return count > 0 ? $"{label}={count}" : "";
        }

        private bool IsExpectedPreservedWarning(ValidationError error)
        {
            if (error.IsFatal || !error.Message.StartsWith("Unknown ", StringComparison.Ordinal))
                return false;

            var modelIds = GetPreservedModelIdStrings();
            if (modelIds.Any(id => error.Message.Contains(id, StringComparison.Ordinal))) return true;

            if (_epochs.Select(static epoch => epoch.Id)
                .Any(epochId => error.Message.Contains(epochId, StringComparison.Ordinal))) return true;

            return _unlockedAchievements.Select(static item => item.Achievement).Any(achievement =>
                error.Message.Contains('"' + achievement + '"', StringComparison.Ordinal));
        }

        private HashSet<string> GetPreservedModelIdStrings()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            AddIds(ids, _characterStats.Select(static stats => stats.Id));
            AddIds(ids, _cardStats.Select(static stats => stats.Id));
            AddIds(ids, _encounterStats.Select(static stats => stats.Id));
            AddIds(ids, _enemyStats.Select(static stats => stats.Id));
            AddIds(ids, _ancientStats.Select(static stats => stats.Id));
            AddIds(ids, _encounterStats.SelectMany(static stats =>
                stats.FightStats.Select(static fight => fight.Character)));
            AddIds(ids,
                _enemyStats.SelectMany(static stats => stats.FightStats.Select(static fight => fight.Character)));
            AddIds(ids, _ancientStats.SelectMany(static stats =>
                stats.CharStats.Select(static charStats => charStats.Character)));
            AddIds(ids, _discoveredCards);
            AddIds(ids, _discoveredRelics);
            AddIds(ids, _discoveredPotions);
            AddIds(ids, _discoveredEvents);
            AddIds(ids, _discoveredActs);

            if (_pendingCharacterUnlock is { } pendingCharacterUnlock &&
                pendingCharacterUnlock != ModelId.none)
                ids.Add(pendingCharacterUnlock.ToString());

            return ids;
        }

        private static void AddIds(HashSet<string> target, IEnumerable<ModelId?> source)
        {
            foreach (var id in source)
                if (id != null && id != ModelId.none)
                    target.Add(id.ToString());
        }

        private static bool IsUnknownModel<TModel>(ModelId? id)
            where TModel : AbstractModel
        {
            if (id == null || id == ModelId.none)
                return false;

            return ModelDb.GetByIdOrNull<TModel>(id) == null;
        }

        private static bool IsSavableModelId(ModelId? id)
        {
            return id != null && id != ModelId.none;
        }

        private static void AppendMissingIds(List<ModelId> target, IEnumerable<ModelId> source)
        {
            foreach (var id in source)
                if (!target.Contains(id))
                    target.Add(id);
        }

        private static void MergeCharacterStats(List<CharacterStats> target, IEnumerable<CharacterStats> source)
        {
            foreach (var incoming in source)
            {
                if (!IsSavableModelId(incoming.Id))
                    continue;

                var existing = target.FirstOrDefault(stats => stats.Id == incoming.Id);
                if (existing == null)
                {
                    target.Add(Clone(incoming));
                    continue;
                }

                existing.TotalWins = Math.Max(existing.TotalWins, incoming.TotalWins);
                existing.TotalLosses = Math.Max(existing.TotalLosses, incoming.TotalLosses);
                existing.Playtime = Math.Max(existing.Playtime, incoming.Playtime);
                existing.MaxAscension = Math.Max(existing.MaxAscension, incoming.MaxAscension);
                existing.PreferredAscension = Math.Max(existing.PreferredAscension, incoming.PreferredAscension);
                existing.BestWinStreak = Math.Max(existing.BestWinStreak, incoming.BestWinStreak);
                existing.CurrentWinStreak = Math.Max(existing.CurrentWinStreak, incoming.CurrentWinStreak);
                existing.FastestWinTime = MergeFastestWinTime(existing.FastestWinTime, incoming.FastestWinTime);
                MergeBadges(existing.Badges, incoming.Badges);
            }
        }

        private static void MergeCardStats(List<CardStats> target, IEnumerable<CardStats> source)
        {
            foreach (var incoming in source)
            {
                if (!IsSavableModelId(incoming.Id))
                    continue;

                var existing = target.FirstOrDefault(stats => stats.Id == incoming.Id);
                if (existing == null)
                {
                    target.Add(Clone(incoming));
                    continue;
                }

                existing.TimesPicked = Math.Max(existing.TimesPicked, incoming.TimesPicked);
                existing.TimesSkipped = Math.Max(existing.TimesSkipped, incoming.TimesSkipped);
                existing.TimesWon = Math.Max(existing.TimesWon, incoming.TimesWon);
                existing.TimesLost = Math.Max(existing.TimesLost, incoming.TimesLost);
            }
        }

        private static void MergeEncounterStats(List<EncounterStats> target, IEnumerable<EncounterStats> source)
        {
            foreach (var incoming in source)
            {
                if (!IsSavableModelId(incoming.Id))
                    continue;

                var existing = target.FirstOrDefault(stats => stats.Id == incoming.Id);
                if (existing == null)
                    target.Add(Clone(incoming));
                else
                    MergeFightStats(existing.FightStats, incoming.FightStats);
            }
        }

        private static void MergeEnemyStats(List<EnemyStats> target, IEnumerable<EnemyStats> source)
        {
            foreach (var incoming in source)
            {
                if (!IsSavableModelId(incoming.Id))
                    continue;

                var existing = target.FirstOrDefault(stats => stats.Id == incoming.Id);
                if (existing == null)
                    target.Add(Clone(incoming));
                else
                    MergeFightStats(existing.FightStats, incoming.FightStats);
            }
        }

        private static void MergeAncientStats(List<AncientStats> target, IEnumerable<AncientStats> source)
        {
            foreach (var incoming in source)
            {
                if (!IsSavableModelId(incoming.Id))
                    continue;

                var existing = target.FirstOrDefault(stats => stats.Id == incoming.Id);
                if (existing == null)
                    target.Add(Clone(incoming));
                else
                    MergeAncientCharacterStats(existing.CharStats, incoming.CharStats);
            }
        }

        private static void MergeFightStats(List<FightStats> target, IEnumerable<FightStats> source)
        {
            foreach (var incoming in source)
            {
                if (!IsSavableModelId(incoming.Character))
                    continue;

                var existing = target.FirstOrDefault(stats => stats.Character == incoming.Character);
                if (existing == null)
                {
                    target.Add(Clone(incoming));
                    continue;
                }

                existing.Wins = Math.Max(existing.Wins, incoming.Wins);
                existing.Losses = Math.Max(existing.Losses, incoming.Losses);
            }
        }

        private static void MergeAncientCharacterStats(
            List<AncientCharacterStats> target,
            IEnumerable<AncientCharacterStats> source)
        {
            foreach (var incoming in source)
            {
                if (!IsSavableModelId(incoming.Character))
                    continue;

                var existing = target.FirstOrDefault(stats => stats.Character == incoming.Character);
                if (existing == null)
                {
                    target.Add(Clone(incoming));
                    continue;
                }

                existing.Wins = Math.Max(existing.Wins, incoming.Wins);
                existing.Losses = Math.Max(existing.Losses, incoming.Losses);
            }
        }

        private static void MergeBadges(List<BadgeStats> target, IEnumerable<BadgeStats> source)
        {
            foreach (var incoming in source)
            {
                var existing = target.FirstOrDefault(stats =>
                    string.Equals(stats.Id, incoming.Id, StringComparison.Ordinal) &&
                    stats.Rarity == incoming.Rarity);
                if (existing == null)
                    target.Add(Clone(incoming));
                else
                    existing.Count = Math.Max(existing.Count, incoming.Count);
            }
        }

        private static void MergeEpochs(List<SerializableEpoch> target, IEnumerable<SerializableEpoch> source)
        {
            foreach (var incoming in source)
            {
                if (string.IsNullOrWhiteSpace(incoming.Id))
                    continue;

                var existing = target.FirstOrDefault(epoch => epoch.Id == incoming.Id);
                if (existing == null)
                {
                    target.Add(Clone(incoming));
                    continue;
                }

                if (incoming.State > existing.State)
                    existing.State = incoming.State;
                if (incoming.ObtainDate > 0 && (existing.ObtainDate == 0 || incoming.ObtainDate < existing.ObtainDate))
                    existing.ObtainDate = incoming.ObtainDate;
            }
        }

        private static void MergeAchievements(
            List<SerializableUnlockedAchievement> target,
            IEnumerable<SerializableUnlockedAchievement> source)
        {
            foreach (var incoming in source)
            {
                if (string.IsNullOrWhiteSpace(incoming.Achievement))
                    continue;

                var existing = target.FirstOrDefault(achievement =>
                    string.Equals(achievement.Achievement, incoming.Achievement, StringComparison.Ordinal));
                if (existing == null)
                    target.Add(Clone(incoming));
            }
        }

        private static long MergeFastestWinTime(long existing, long incoming)
        {
            if (existing == -1)
                return incoming;
            return incoming == -1 ? existing : Math.Min(existing, incoming);
        }

        private static void AppendMissingById<T, TKey>(
            List<T> target,
            IEnumerable<T> source,
            Func<T, TKey?> keySelector,
            Func<T, T> clone)
        {
            var existing = target
                .Select(keySelector)
                .Where(static key => key != null)
                .ToHashSet();

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var item in source)
            {
                var key = keySelector(item);
                if (key == null || !existing.Add(key))
                    continue;

                target.Add(clone(item));
            }
        }

        private static CharacterStats Clone(CharacterStats stats)
        {
            return new()
            {
                Id = stats.Id,
                MaxAscension = stats.MaxAscension,
                PreferredAscension = stats.PreferredAscension,
                TotalWins = stats.TotalWins,
                TotalLosses = stats.TotalLosses,
                FastestWinTime = stats.FastestWinTime,
                BestWinStreak = stats.BestWinStreak,
                CurrentWinStreak = stats.CurrentWinStreak,
                Playtime = stats.Playtime,
                Badges = [.. stats.Badges.Select(Clone)],
            };
        }

        private static CardStats Clone(CardStats stats)
        {
            return new()
            {
                Id = stats.Id,
                TimesPicked = stats.TimesPicked,
                TimesSkipped = stats.TimesSkipped,
                TimesWon = stats.TimesWon,
                TimesLost = stats.TimesLost,
            };
        }

        private static EncounterStats Clone(EncounterStats stats)
        {
            return new()
            {
                Id = stats.Id,
                FightStats = [.. stats.FightStats.Select(Clone)],
            };
        }

        private static EnemyStats Clone(EnemyStats stats)
        {
            return new()
            {
                Id = stats.Id,
                FightStats = [.. stats.FightStats.Select(Clone)],
            };
        }

        private static AncientStats Clone(AncientStats stats)
        {
            return new()
            {
                Id = stats.Id,
                CharStats = [.. stats.CharStats.Select(Clone)],
            };
        }

        private static FightStats Clone(FightStats stats)
        {
            return new()
            {
                Character = stats.Character,
                Wins = stats.Wins,
                Losses = stats.Losses,
            };
        }

        private static AncientCharacterStats Clone(AncientCharacterStats stats)
        {
            return new()
            {
                Character = stats.Character,
                Wins = stats.Wins,
                Losses = stats.Losses,
            };
        }

        private static BadgeStats Clone(BadgeStats stats)
        {
            return new()
            {
                Id = stats.Id,
                Count = stats.Count,
                Rarity = stats.Rarity,
            };
        }

        private static SerializableEpoch Clone(SerializableEpoch epoch)
        {
            return new(epoch.Id, epoch.State)
            {
                ObtainDate = epoch.ObtainDate,
            };
        }

        private static SerializableUnlockedAchievement Clone(SerializableUnlockedAchievement achievement)
        {
            return new()
            {
                Achievement = achievement.Achievement,
                UnlockTime = achievement.UnlockTime,
            };
        }

        private static HashSet<string> BuildKnownAchievementNames()
        {
            return Enum.GetValues<Achievement>()
                .Select(value => JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString()))
                .ToHashSet(StringComparer.Ordinal);
        }
    }
}
