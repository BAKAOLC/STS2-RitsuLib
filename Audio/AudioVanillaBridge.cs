using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Small helper for restoring or refreshing the game's native run music controller state.
    /// </summary>
    public static class AudioVanillaBridge
    {
        /// <summary>
        ///     Rebuilds vanilla run music, track state, and ambience.
        /// </summary>
        public static void RefreshRunMusic()
        {
            var controller = NRunMusicController.Instance;
            if (controller is null || !RunManager.Instance.IsInProgress)
                return;

            controller.UpdateMusic();
            controller.UpdateTrack();
            controller.UpdateAmbience();
        }

        /// <summary>
        ///     Refreshes vanilla track progression and ambience without rebuilding the act music selection.
        /// </summary>
        public static void RefreshTrackAndAmbience()
        {
            var controller = NRunMusicController.Instance;
            if (controller is null || !RunManager.Instance.IsInProgress)
                return;

            controller.UpdateTrack();
            controller.UpdateAmbience();
        }
    }
}
