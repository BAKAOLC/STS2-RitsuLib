using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Provides guarded entry points for refreshing the game's native run-music controller.</para>
    ///     <para xml:lang="zh-CN">提供带状态检查的游戏原生跑局音乐控制器刷新入口。</para>
    /// </summary>
    public static class AudioVanillaBridge
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Requests native updates for act music selection, room-progress track state, and ambience while
        ///         a run is active.
        ///     </para>
        ///     <para xml:lang="zh-CN">跑局进行中时，请求原生控制器更新章节音乐选择、房间进度曲目状态和环境音。</para>
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
        ///     <para xml:lang="en">
        ///         Requests native updates for room-progress track state and ambience without reevaluating act
        ///         music selection.
        ///     </para>
        ///     <para xml:lang="zh-CN">请求原生控制器更新房间进度曲目状态和环境音，但不重新评估章节音乐选择。</para>
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
