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
                yield return (card.Id.Entry, card.TitleLocString);

            foreach (var potion in ModelDb.AllPotions)
                yield return (potion.Id.Entry, potion.Title);

            foreach (var relic in ModelDb.AllRelics)
                yield return (relic.Id.Entry, relic.Title);

            foreach (var encounter in ModelDb.AllEncounters)
                yield return (encounter.Id.Entry, encounter.Title);

            foreach (var affliction in ModelDb.DebugAfflictions)
                yield return (affliction.Id.Entry, affliction.Title);

            foreach (var enchantment in ModelDb.DebugEnchantments)
                yield return (enchantment.Id.Entry, enchantment.Title);

            foreach (var ancient in ModelDb.AllAncients)
                yield return (ancient.Id.Entry, ancient.Title);

            foreach (var evt in ModelDb.AllEvents)
                yield return (evt.Id.Entry, evt.Title);

            foreach (var act in ModelDb.Acts)
                yield return (act.Id.Entry, act.Title);

            foreach (var power in ModelDb.AllAbstractModelSubtypes.Where(t => t.IsSubclassOf(typeof(PowerModel))))
                yield return (ModelDb.DebugPower(power).Id.Entry, ModelDb.DebugPower(power).Title);

            foreach (var monster in ModelDb.Monsters)
                yield return (monster.Id.Entry, monster.Title);

            foreach (var epochId in EpochModel.AllEpochIds)
                yield return (epochId, new("epochs", epochId + ".title"));
        }
    }
}
