using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Saves
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Resolves acts and characters referenced by run history, using the base game's deprecated-model placeholders
    ///         when the owning mod is unavailable.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         解析游戏历史引用的章节与角色；当所属模组不可用时，改用原版游戏的已弃用模型占位项。
    ///     </para>
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
                ". Using DeprecatedCharacter for preview UI.");
            return SaveUtil.CharacterOrDeprecated(id);
        }

        internal static ActModel ActForRunHistory(ModelId id)
        {
            var act = ModelDb.GetByIdOrNull<ActModel>(id);
            if (act != null)
                return act;

            RitsuLibFramework.Logger.Warn(
                "[Saves] Run history references act not in ModelDb (mod likely unloaded): " + id +
                ". Using DeprecatedAct for section header.");
            return SaveUtil.ActOrDeprecated(id);
        }
    }
}
