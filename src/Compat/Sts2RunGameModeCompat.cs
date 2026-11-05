using MegaCrit.Sts2.Core.Entities.Players;
#if STS2_AT_LEAST_0_113_0
using MegaCrit.Sts2.Core.Helpers;
#endif
#if !STS2_AT_LEAST_0_113_0
using MegaCrit.Sts2.Core.Runs;
#endif
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Compat
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides epoch-related game-mode checks for serialized and active runs.
    ///     </para>
    ///     <para xml:lang="zh-CN">提供针对已序列化及当前游戏局的纪元相关游戏模式检查。</para>
    /// </summary>
    internal static class Sts2RunGameModeCompat
    {
        internal static bool IsStandardSerializableRunForEpochUnlocks(SerializableRun run)
        {
#if STS2_AT_LEAST_0_113_0
            return !GameModeHelper.AreAchievementsAndEpochsLockedForSerializableRun(run);
#else
            return !run.GameMode.AreAchievementsAndEpochsLocked();
#endif
        }

        internal static bool AreMidRunEpochsLockedFor(Player localPlayer)
        {
            ArgumentNullException.ThrowIfNull(localPlayer);
#if STS2_AT_LEAST_0_113_0
            return GameModeHelper.AreAchievementsAndEpochsLockedForRun(localPlayer.RunState);
#else
            return localPlayer.RunState.GameMode.AreAchievementsAndEpochsLocked();
#endif
        }
    }
}
