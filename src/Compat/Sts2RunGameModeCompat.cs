using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
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
            return !run.GameMode.AreAchievementsAndEpochsLocked();
        }

        internal static bool AreMidRunEpochsLockedFor(Player localPlayer)
        {
            ArgumentNullException.ThrowIfNull(localPlayer);
            return localPlayer.RunState.GameMode.AreAchievementsAndEpochsLocked();
        }
    }
}
