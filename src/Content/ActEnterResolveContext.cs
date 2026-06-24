using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     Inputs for <see cref="ModContentRegistry" /> act-enter resolution (force / pool eligibility / weights). Passed when
    ///     <see cref="MegaCrit.Sts2.Core.Runs.RunManager.EnterAct" /> runs for <see cref="EnteringActIndex" />.
    ///     <see cref="ModContentRegistry" /> 章节进入解析（强制 / 池资格 / 权重）的输入。
    ///     <see cref="MegaCrit.Sts2.Core.Runs.RunManager.EnterAct" /> 为 <see cref="EnteringActIndex" /> 运行时传入。
    /// </summary>
    public readonly record struct ActEnterResolveContext(
        RunManager RunManager,
        RunState RunState,
        int EnteringActIndex,
        Rng Rng,
        UnlockState UnlockState,
        bool IsMultiplayer);
}
