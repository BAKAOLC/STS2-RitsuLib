namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Game-routed FMOD Studio API (vanilla <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" />).
    ///     游戏路由的 FMOD Studio API（原版 <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" />）。
    /// </summary>
    public static class GameFmod
    {
        /// <summary>
        ///     Vanilla-routed FMOD API (singleton <see cref="GameFmodAudioService" />).
        ///     原版路由的 FMOD API（单例 <see cref="GameFmodAudioService" />）。
        /// </summary>
        public static IGameFmodAudio Studio => GameFmodAudioService.Shared;

        /// <summary>
        ///     Higher-level playback API with typed handles and lifecycle scoping.
        ///     带类型化句柄和生命周期作用域的高级播放 API。
        /// </summary>
        public static IGameAudio Playback => GameAudioService.Shared;
    }
}
