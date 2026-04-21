namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Game-routed FMOD Studio API (vanilla <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" />).
    /// </summary>
    public static class GameFmod
    {
        /// <summary>
        ///     Vanilla-routed FMOD API (singleton <see cref="GameFmodAudioService" />).
        /// </summary>
        public static IGameFmodAudio Studio => GameFmodAudioService.Shared;

        /// <summary>
        ///     Higher-level playback API with typed handles and lifecycle scoping.
        /// </summary>
        public static IGameAudio Playback => GameAudioService.Shared;
    }
}
