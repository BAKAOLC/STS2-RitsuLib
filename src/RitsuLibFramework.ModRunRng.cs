using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.RunRngs;
using StsRng = MegaCrit.Sts2.Core.Random.Rng;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an independent per-run RNG stream for a mod.</para>
        ///     <para xml:lang="zh-CN">获取模组独立的每局游戏 RNG 流。</para>
        /// </summary>
        public static StsRng GetModRunRng(RunState runState, string modId, string streamId)
        {
            return ModRunRngRegistry.Get(runState, modId, streamId);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets a mod's independent per-run RNG stream from a player's run.</para>
        ///     <para xml:lang="zh-CN">从玩家所属的一局游戏获取模组独立的每局 RNG 流。</para>
        /// </summary>
        public static StsRng GetModRunRng(Player player, string modId, string streamId)
        {
            ArgumentNullException.ThrowIfNull(player);
            return player.RunState is not RunState runState
                ? throw new InvalidOperationException("Player does not belong to a concrete RunState.")
                : GetModRunRng(runState, modId, streamId);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets an independent per-player RNG stream for a mod.</para>
        ///     <para xml:lang="zh-CN">获取模组独立的每位玩家 RNG 流。</para>
        /// </summary>
        public static StsRng GetModPlayerRng(Player player, string modId, string streamId)
        {
            return ModRunRngRegistry.Get(player, modId, streamId);
        }
    }
}
