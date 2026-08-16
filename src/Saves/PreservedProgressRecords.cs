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
    ///     <para xml:lang="en">
    ///         Preserves progress-save entries whose model IDs are unavailable with the current set of mods, allowing the
    ///         entries to be written back without exposing them to runtime progress logic.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         保留模型 ID 在当前模组集合中不可用的进度存档条目，使其不会参与运行时进度逻辑，但仍可在保存时写回。
    ///     </para>
    /// </summary>
    public sealed class PreservedProgressRecords
    {
        private static readonly ConditionalWeakTable<ProgressState, PreservedProgressRecords> RecordsByProgress = [];
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

            var preservedIdentifiers = GetPreservedIdentifierStrings();
            var preservedAchievements = _unlockedAchievements
                .Select(static item => item.Achievement)
                .ToHashSet(StringComparer.Ordinal);
            var removed = errors.RemoveAll(error =>
                IsExpectedPreservedWarning(error, preservedIdentifiers, preservedAchievements));
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

        internal static bool MergeUnavailableRecords(SerializableProgress target, SerializableProgress source)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(source);

            var records = Capture(source);
            if (records == null)
                return false;

            records.MergeInto(target);
            return true;
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
            var existing = target.ToHashSet();
            target.AddRange(source.Where(IsUnknownModel<TModel>).Where(existing.Add));
        }

        private void CaptureEpochs(List<SerializableEpoch> source)
        {
            var existing = _epochs
                .Select(static epoch => epoch.Id)
                .ToHashSet(StringComparer.Ordinal);
            foreach (var epoch in source)
            {
                if (string.IsNullOrWhiteSpace(epoch.Id) ||
                    EpochModel.IsValid(epoch.Id) ||
                    !Enum.IsDefined(epoch.State) ||
                    epoch.State < EpochState.NotObtained ||
                    !existing.Add(epoch.Id))
                    continue;

                _epochs.Add(Clone(epoch));
            }
        }

        private void CaptureAchievements(List<SerializableUnlockedAchievement>? source)
        {
            if (source == null)
                return;

            var existing = _unlockedAchievements
                .Select(static achievement => achievement.Achievement)
                .ToHashSet(StringComparer.Ordinal);
            foreach (var achievement in source)
            {
                if (string.IsNullOrWhiteSpace(achievement.Achievement) ||
                    KnownAchievementNames.Contains(achievement.Achievement) ||
                    !existing.Add(achievement.Achievement))
                    continue;

                _unlockedAchievements.Add(Clone(achievement));
            }
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

        private static bool IsExpectedPreservedWarning(
            ValidationError error,
            HashSet<string> preservedIdentifiers,
            HashSet<string> preservedAchievements)
        {
            if (error.IsFatal || !error.Message.StartsWith("Unknown ", StringComparison.Ordinal))
                return false;

            const string achievementPrefix = "Unknown achievement \"";
            if (error.Message.StartsWith(achievementPrefix, StringComparison.Ordinal))
            {
                var achievementEnd = error.Message.LastIndexOf("\" at index ", StringComparison.Ordinal);
                return achievementEnd > achievementPrefix.Length &&
                       preservedAchievements.Contains(error.Message[achievementPrefix.Length..achievementEnd]);
            }

            var identifierStart = error.Message.IndexOf(": ", StringComparison.Ordinal);
            if (identifierStart < 0)
                return false;

            identifierStart += 2;
            var identifierEnd = error.Message.Length;
            const string removingSuffix = ", removing";
            const string resettingSuffix = ", resetting to none";
            if (error.Message.EndsWith(removingSuffix, StringComparison.Ordinal))
                identifierEnd -= removingSuffix.Length;
            else if (error.Message.EndsWith(resettingSuffix, StringComparison.Ordinal))
                identifierEnd -= resettingSuffix.Length;

            return preservedIdentifiers.Contains(error.Message[identifierStart..identifierEnd]);
        }

        private HashSet<string> GetPreservedIdentifierStrings()
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
            foreach (var epoch in _epochs)
                ids.Add(epoch.Id);

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
            var existing = target.ToHashSet();
            foreach (var id in source)
                if (existing.Add(id))
                    target.Add(id);
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
