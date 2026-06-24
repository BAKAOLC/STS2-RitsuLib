using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Compat
{
    /// <summary>
    ///     Epoch-related game-mode checks on <see cref="SerializableRun" /> and the active <see cref="IRunState" />.
    ///     <see cref="SerializableRun" /> 和活动 <see cref="IRunState" /> 上与纪元相关的游戏模式检查。
    /// </summary>
    internal static class Sts2RunGameModeCompat
    {
        internal static bool IsStandardSerializableRunForEpochUnlocks(SerializableRun run)
        {
            return !run.GameMode.AreAchievementsAndEpochsLocked();
        }

        internal static bool AreMidRunEpochsLockedFor(Player localPlayer)
        {
            ArgumentNullException.ThrowIfNull(localPlayer);
            return localPlayer.RunState.GameMode.AreAchievementsAndEpochsLocked();
        }
    }
}
