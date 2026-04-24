using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Compat
{
    /// <summary>
    ///     Epoch-related game-mode checks on <see cref="SerializableRun" /> and the active <see cref="IRunState" />.
    /// </summary>
    internal static class Sts2RunGameModeCompat
    {
        internal static bool IsStandardSerializableRunForEpochUnlocks(SerializableRun run)
        {
            return run.GameMode == GameMode.Standard;
        }

        internal static bool AreMidRunEpochsLockedFor(Player localPlayer)
        {
            ArgumentNullException.ThrowIfNull(localPlayer);
            return localPlayer.RunState.GameMode != GameMode.Standard;
        }
    }
}
