using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;

namespace STS2RitsuLib.Saves
{
    /// <summary>
    ///     Run history UI calls <see cref="ModelDb.GetById{T}" /> for acts/characters. When a mod is unloaded, those ids
    ///     are missing and vanilla throws; we fall back so the screen can render like deprecated event/encounter handling.
    /// </summary>
    internal static class RunHistoryMissingModelSupport
    {
        internal static CharacterModel CharacterForRunHistory(ModelId id)
        {
            var character = ModelDb.GetByIdOrNull<CharacterModel>(id);
            if (character != null)
                return character;

            RitsuLibFramework.Logger.Warn(
                "[Saves] Run history references character not in ModelDb (mod likely unloaded): " + id +
                ". Using Ironclad for preview UI.");
            return ModelDb.Character<Ironclad>();
        }

        internal static ActModel ActForRunHistory(ModelId id)
        {
            var act = ModelDb.GetByIdOrNull<ActModel>(id);
            if (act != null)
                return act;

            RitsuLibFramework.Logger.Warn(
                "[Saves] Run history references act not in ModelDb (mod likely unloaded): " + id +
                ". Using first vanilla act for section header.");
            return ModelDb.Acts.First();
        }
    }
}
