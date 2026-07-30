using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides run state, random number generation, and unlock state to act-entry resolvers.
    ///     </para>
    ///     <para xml:lang="zh-CN">向章节进入解析器提供一局游戏状态、随机数生成器与解锁状态。</para>
    /// </summary>
    public readonly record struct ActEnterResolveContext(
        RunManager RunManager,
        RunState RunState,
        int EnteringActIndex,
        Rng Rng,
        UnlockState UnlockState,
        bool IsMultiplayer);
}
