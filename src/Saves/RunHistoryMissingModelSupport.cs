using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;

namespace STS2RitsuLib.Saves
{
    /// <summary>
    ///     Run history UI calls <see cref="ModelDb.GetById{T}" /> for acts/characters. When a mod is unloaded, those ids
    ///     are missing and vanilla throws; we fall back so the screen can render like deprecated event/encounter handling.
    ///     跑局历史 UI 会为章节/角色调用 <see cref="ModelDb.GetById{T}" />。当 mod 被卸载时，这些 id
    ///     会缺失且原版会抛出；我们提供 fallback，使界面能像处理已弃用事件/遭遇一样渲染。
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
