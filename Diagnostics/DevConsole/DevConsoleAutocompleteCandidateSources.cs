using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Mirrors vanilla dev-console candidate pools for catalog building.
    /// </summary>
    internal static class DevConsoleAutocompleteCandidateSources
    {
        public static IEnumerable<string> GetCardEntryIds()
        {
            return ModelDb.AllCards.Select(c => c.Id.Entry);
        }

        public static IEnumerable<string> GetPotionEntryIds()
        {
            return ModelDb.AllPotions.Select(p => p.Id.Entry);
        }

        public static IEnumerable<string> GetRelicEntryIds()
        {
            return ModelDb.AllRelics.Select(r => r.Id.Entry);
        }

        public static IEnumerable<string> GetEventEntryIds()
        {
            return ModelDb.AllEvents.Concat(ModelDb.AllAncients).Select(e => e.Id.Entry).Distinct();
        }

        public static IEnumerable<string> GetEncounterEntryIds()
        {
            return ModelDb.AllEncounters.Select(e => e.Id.Entry);
        }

        public static IEnumerable<string> GetAfflictionEntryIds()
        {
            return ModelDb.DebugAfflictions.Select(a => a.Id.Entry);
        }

        public static IEnumerable<string> GetEnchantmentEntryIds()
        {
            return ModelDb.DebugEnchantments.Select(e => e.Id.Entry);
        }

        /// <summary>
        ///     Same pool as the vanilla <c>power</c> command autocomplete.
        /// </summary>
        public static IEnumerable<string> GetPowerEntryIds()
        {
            return ModelDb.AllAbstractModelSubtypes
                .Where(t => t.IsSubclassOf(typeof(PowerModel)))
                .Select(ModelDb.DebugPower)
                .Select(p => p.Id.Entry);
        }

        public static IEnumerable<string> GetMonsterEntryIds()
        {
            return ModelDb.Monsters.Select(m => m.Id.Entry);
        }

        public static IEnumerable<string> GetEpochEntryIds()
        {
            return EpochModel.AllEpochIds;
        }

        public static IEnumerable<(string EntryId, LocString Title)> EnumerateLocalizedModelTitles()
        {
            foreach (var card in ModelDb.AllCards)
                yield return GetTitleCandidate(card);

            foreach (var potion in ModelDb.AllPotions)
                yield return GetTitleCandidate(potion);

            foreach (var relic in ModelDb.AllRelics)
                yield return GetTitleCandidate(relic);

            foreach (var encounter in ModelDb.AllEncounters)
                yield return GetTitleCandidate(encounter);

            foreach (var affliction in ModelDb.DebugAfflictions)
                yield return GetTitleCandidate(affliction);

            foreach (var enchantment in ModelDb.DebugEnchantments)
                yield return GetTitleCandidate(enchantment);

            foreach (var ancient in ModelDb.AllAncients)
                yield return GetTitleCandidate(ancient);

            foreach (var evt in ModelDb.AllEvents)
                yield return GetTitleCandidate(evt);

            foreach (var act in ModelDb.Acts)
                yield return GetTitleCandidate(act);

            foreach (var powerType in ModelDb.AllAbstractModelSubtypes.Where(t => t.IsSubclassOf(typeof(PowerModel))))
            {
                var power = ModelDb.DebugPower(powerType);
                yield return GetTitleCandidate(power);
            }

            foreach (var monster in ModelDb.Monsters)
                yield return GetTitleCandidate(monster);

            foreach (var epochId in EpochModel.AllEpochIds)
                yield return (epochId, new("epochs", epochId + ".title"));
        }

        private static (string EntryId, LocString Title) GetTitleCandidate(AbstractModel model)
        {
            return (model.Id.Entry, ResolveTitleForAutocomplete(model));
        }

        private static LocString ResolveTitleForAutocomplete(
            AbstractModel model,
            HashSet<Type>? visitedModelTypes = null)
        {
            if (model is not ITemporaryPower temporaryPower)
                return DefaultTitleForAutocomplete(model);

            visitedModelTypes ??= [];
            if (!visitedModelTypes.Add(model.GetType()))
                return DefaultTitleForAutocomplete(model);

            try
            {
                var origin = temporaryPower.OriginModel;
                return ReferenceEquals(origin, model)
                    ? DefaultTitleForAutocomplete(model)
                    : ResolveTitleForAutocomplete(origin, visitedModelTypes);
            }
            catch
            {
                return DefaultTitleForAutocomplete(model);
            }
        }

        private static LocString DefaultTitleForAutocomplete(AbstractModel model)
        {
            return model switch
            {
                CardModel => new("cards", model.Id.Entry + ".title"),
                PotionModel => new("potions", model.Id.Entry + ".title"),
                RelicModel => new("relics", model.Id.Entry + ".title"),
                EncounterModel => new("encounters", model.Id.Entry + ".title"),
                AfflictionModel => new("afflictions", model.Id.Entry + ".title"),
                EnchantmentModel => new("enchantments", model.Id.Entry + ".title"),
                AncientEventModel => new("ancients", model.Id.Entry + ".title"),
                EventModel => new("events", model.Id.Entry + ".title"),
                ActModel => new("acts", model.Id.Entry + ".title"),
                PowerModel => new("powers", model.Id.Entry + ".title"),
                MonsterModel => new("monsters", model.Id.Entry + ".name"),
                OrbModel => new("orbs", model.Id.Entry + ".title"),
                CharacterModel => new("characters", model.Id.Entry + ".title"),
                ModifierModel => new("modifiers", model.Id.Entry + ".title"),
                _ => new(model.Id.Category.ToLowerInvariant(), model.Id.Entry + ".title"),
            };
        }
    }
}
