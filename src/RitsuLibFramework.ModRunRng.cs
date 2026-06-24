using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.RunRngs;
using StsRng = MegaCrit.Sts2.Core.Random.Rng;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     Gets an independent per-run RNG stream for a mod.
        ///     为 Mod 获取一条独立的按跑局 RNG 流。
        /// </summary>
        public static StsRng GetModRunRng(RunState runState, string modId, string streamId)
        {
            return ModRunRngRegistry.Get(runState, modId, streamId);
        }

        /// <summary>
        ///     Gets an independent per-run RNG stream for a mod using a player's run.
        ///     使用玩家所属跑局为 Mod 获取一条独立的按跑局 RNG 流。
        /// </summary>
        public static StsRng GetModRunRng(Player player, string modId, string streamId)
        {
            ArgumentNullException.ThrowIfNull(player);
            return player.RunState is not RunState runState
                ? throw new InvalidOperationException("Player does not belong to a concrete RunState.")
                : GetModRunRng(runState, modId, streamId);
        }

        /// <summary>
        ///     Gets an independent per-player RNG stream for a mod.
        ///     为 Mod 获取一条独立的按玩家 RNG 流。
        /// </summary>
        public static StsRng GetModPlayerRng(Player player, string modId, string streamId)
        {
            return ModRunRngRegistry.Get(player, modId, streamId);
        }
    }
}
