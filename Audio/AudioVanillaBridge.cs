using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Small helper for restoring or refreshing the game's native run music controller state.
    ///     用于恢复或刷新游戏原生跑局音乐控制器状态的小型 helper。
    /// </summary>
    public static class AudioVanillaBridge
    {
        /// <summary>
        ///     Rebuilds vanilla run music, track state, and ambience.
        ///     重建原版跑局音乐、曲目状态和环境音。
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
        ///     刷新原版曲目推进和环境音，而不重建章节音乐选择。
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
